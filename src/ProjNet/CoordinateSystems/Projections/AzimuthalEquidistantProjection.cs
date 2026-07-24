// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Azimuthal Equidistant map projection.
/// </summary>
/// <remarks>
/// <para>The Azimuthal Equidistant projection preserves both distance and direction from
/// the projection centre. All points on the map are at proportionally correct distances
/// from the centre, and the azimuth (bearing) from the centre to any other point is
/// correctly represented. This implementation supports both spherical and ellipsoidal
/// formulations for forward and inverse transformations.</para>
/// <para>The spherical formulation was independently verified against Snyder, "Map
/// Projections - A Working Manual" (USGS Professional Paper 1395, 1987), section 25,
/// Azimuthal Equidistant. The published forward scale factor <c>k = c / sin(c)</c> and
/// inverse recovery from <c>c = ρ / R</c> match the equatorial, oblique, and polar
/// aspect branches implemented here. The ellipsoidal extension uses direct and inverse
/// geodesic solvers for the general case together with meridional-arc handling for
/// polar aspects, matching the rigorous azimuthal-equidistant geodesic method carried
/// by EPSG method 1125 and documented by PROJ for <c>aeqd</c>.</para>
/// </remarks>
/// <seealso href="https://epsg.io/1125-method">EPSG method 1125: Azimuthal Equidistant.</seealso>
/// <seealso href="https://pubs.usgs.gov/publication/pp1395">USGS Professional Paper 1395: Map Projections - A Working Manual.</seealso>
/// <seealso href="https://proj.org/en/stable/operations/projections/aeqd.html">PROJ documentation: Azimuthal Equidistant.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Azimuthal_equidistant_projection">Wikipedia: Azimuthal equidistant projection.</seealso>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 3, Sect. 3.2.4, pp. 105-107.</seealso>
internal sealed class AzimuthalEquidistantProjection : MapProjection
{
    private const double PathologicalTolerance = 1e-14d;

    private readonly bool ellipsoidal;
    private readonly bool guam;
    private readonly ProjectionMode mode;
    private readonly double sinPhi0;
    private readonly double cosPhi0;
    private readonly double meridionalOriginDistance;
    private readonly double meridionalPoleDistance;
    private readonly double flattening;
    private readonly double eccentricityPrimeSquared;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzimuthalEquidistantProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AzimuthalEquidistantProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzimuthalEquidistantProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AzimuthalEquidistantProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Azimuthal_Equidistant";
        this.ellipsoidal = this.es > 0d;
        this.guam = this.ellipsoidal && Math.Abs(this.Parameters.GetOptionalParameterValue("guam", 0d)) > 0d;
        this.mode = DetermineMode(this.latOrigin);
        Sincos(this.latOrigin, out this.sinPhi0, out this.cosPhi0);
        this.flattening = (this.semiMajor - this.semiMinor) / this.semiMajor;
        this.eccentricityPrimeSquared = ((this.semiMajor * this.semiMajor) - (this.semiMinor * this.semiMinor)) / (this.semiMinor * this.semiMinor);
        if (this.guam)
        {
            this.meridionalOriginDistance = this.Mlfn(this.latOrigin, this.sinPhi0, this.cosPhi0);
        }

        if (this.ellipsoidal && (this.mode == ProjectionMode.NorthPole || this.mode == ProjectionMode.SouthPole))
        {
            double poleLatitude = this.mode == ProjectionMode.NorthPole ? HalfPi : -HalfPi;
            double poleSine = this.mode == ProjectionMode.NorthPole ? 1d : -1d;
            this.meridionalPoleDistance = this.Mlfn(poleLatitude, poleSine, 0d);
        }
    }

    private enum ProjectionMode
    {
        NorthPole,
        SouthPole,
        Equatorial,
        Oblique,
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new AzimuthalEquidistantProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);

        if (this.ellipsoidal)
        {
            if (this.guam)
            {
                this.ForwardEllipsoidalGuam(lambda, lat, out lon, out lat);
                return;
            }

            if (this.mode == ProjectionMode.NorthPole || this.mode == ProjectionMode.SouthPole)
            {
                this.ForwardEllipsoidalPolar(this.mode, lambda, lat, out lon, out lat);
                return;
            }

            this.ForwardEllipsoidalGeneral(lambda, lat, out lon, out lat);
            return;
        }

        this.ForwardSpherical(lambda, lat, out lon, out lat);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        if (this.ellipsoidal)
        {
            if (this.guam)
            {
                this.InverseEllipsoidalGuam(x, y, out x, out y);
                return;
            }

            if (this.mode == ProjectionMode.NorthPole || this.mode == ProjectionMode.SouthPole)
            {
                this.InverseEllipsoidalPolar(this.mode, x, y, out x, out y);
                return;
            }

            this.InverseEllipsoidalGeneral(x, y, out x, out y);
            return;
        }

        this.InverseSpherical(x, y, out x, out y);
    }

    private static ProjectionMode DetermineMode(double latitudeOfOrigin)
    {
        if (Math.Abs(Math.Abs(latitudeOfOrigin) - HalfPi) < Eps10)
        {
            return latitudeOfOrigin < 0d ? ProjectionMode.SouthPole : ProjectionMode.NorthPole;
        }

        if (Math.Abs(latitudeOfOrigin) < Eps10)
        {
            return ProjectionMode.Equatorial;
        }

        return ProjectionMode.Oblique;
    }

    private void ForwardSpherical(double lambda, double phi, out double x, out double y)
    {
        if (this.mode == ProjectionMode.NorthPole || this.mode == ProjectionMode.SouthPole)
        {
            double cosineLambda = Math.Cos(lambda);
            if (this.mode == ProjectionMode.NorthPole)
            {
                phi = -phi;
                cosineLambda = -cosineLambda;
            }

            if (Math.Abs(phi - HalfPi) < Eps10)
            {
                ArgumentGuard.ThrowArgumentOutOfRange(nameof(phi), "Coordinate is outside the valid Azimuthal Equidistant domain.");
            }

            double rho = this.SphericalRadius * (HalfPi + phi);
            x = rho * Math.Sin(lambda);
            y = rho * cosineLambda;
            return;
        }

        double cosPhi = Math.Cos(phi);
        double sinPhi = Math.Sin(phi);
        double cosLambda = Math.Cos(lambda);
        double sinLambda = Math.Sin(lambda);

        if (this.mode == ProjectionMode.Equatorial)
        {
            double cosC = cosPhi * cosLambda;
            if (Math.Abs(Math.Abs(cosC) - 1d) < PathologicalTolerance)
            {
                if (cosC < 0d)
                {
                    ArgumentGuard.ThrowArgumentOutOfRange(nameof(phi), "Coordinate is outside the valid Azimuthal Equidistant domain.");
                }

                this.ForwardEllipsoidalGeneral(lambda, phi, out x, out y);
                return;
            }

            double c = Math.Acos(cosC);
            double k = c / Math.Sin(c);
            x = this.SphericalRadius * k * cosPhi * sinLambda;
            y = this.SphericalRadius * k * sinPhi;
            return;
        }

        double cosPhiCosLambda = cosPhi * cosLambda;
        double cosCOblique = (this.sinPhi0 * sinPhi) + (this.cosPhi0 * cosPhiCosLambda);
        if (Math.Abs(Math.Abs(cosCOblique) - 1d) < PathologicalTolerance)
        {
            if (cosCOblique < 0d)
            {
                ArgumentGuard.ThrowArgumentOutOfRange(nameof(phi), "Coordinate is outside the valid Azimuthal Equidistant domain.");
            }

            this.ForwardEllipsoidalGeneral(lambda, phi, out x, out y);
            return;
        }

        double cOblique = Math.Acos(cosCOblique);
        double kOblique = cOblique / Math.Sin(cOblique);
        x = this.SphericalRadius * kOblique * cosPhi * sinLambda;
        y = this.SphericalRadius * kOblique * ((this.cosPhi0 * sinPhi) - (this.sinPhi0 * cosPhiCosLambda));
    }

    private void InverseSpherical(double xMeter, double yMeter, out double lon, out double lat)
    {
        double x = xMeter * this.InverseSphericalRadius;
        double y = yMeter * this.InverseSphericalRadius;

        double rho = Hypot(x, y);
        if (rho > PI)
        {
            if ((rho - Eps10) > PI)
            {
                ArgumentGuard.ThrowArgumentOutOfRange(nameof(xMeter), "Coordinate is outside the valid Azimuthal Equidistant domain.");
            }

            rho = PI;
        }
        else if (rho < Eps10)
        {
            lon = this.centralMeridian;
            lat = this.latOrigin;
            return;
        }

        if (this.mode == ProjectionMode.NorthPole)
        {
            lat = HalfPi - rho;
            lon = Adjust_lon(this.centralMeridian + Math.Atan2(x, -y));
            return;
        }

        if (this.mode == ProjectionMode.SouthPole)
        {
            lat = rho - HalfPi;
            lon = Adjust_lon(this.centralMeridian + Math.Atan2(x, y));
            return;
        }

        double sinc = Math.Sin(rho);
        double cosc = Math.Cos(rho);
        if (this.mode == ProjectionMode.Equatorial)
        {
            lat = Asinz(y * sinc / rho);
            double xAdjusted = x * sinc;
            double yAdjusted = cosc * rho;
            lon = Adjust_lon(this.centralMeridian + (yAdjusted == 0d ? 0d : Math.Atan2(xAdjusted, yAdjusted)));
            return;
        }

        lat = Asinz((cosc * this.sinPhi0) + ((y * sinc * this.cosPhi0) / rho));
        double yTerm = (cosc - (this.sinPhi0 * Math.Sin(lat))) * rho;
        double xTerm = x * sinc * this.cosPhi0;
        lon = Adjust_lon(this.centralMeridian + (yTerm == 0d ? 0d : Math.Atan2(xTerm, yTerm)));
    }

    private void ForwardEllipsoidalPolar(ProjectionMode polarMode, double lambda, double phi, out double x, out double y)
    {
        double cosPhi = Math.Cos(phi);
        double sinPhi = Math.Sin(phi);
        double rho = Math.Abs(this.meridionalPoleDistance - this.Mlfn(phi, sinPhi, cosPhi));
        double cosineLambda = Math.Cos(lambda);
        if (polarMode == ProjectionMode.NorthPole)
        {
            cosineLambda = -cosineLambda;
        }

        x = this.SphericalRadius * rho * Math.Sin(lambda);
        y = this.SphericalRadius * rho * cosineLambda;
    }

    private void ForwardEllipsoidalGuam(double lambda, double phi, out double x, out double y)
    {
        double cosPhi = Math.Cos(phi);
        double sinPhi = Math.Sin(phi);
        double t = 1d / Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
        x = this.SphericalRadius * lambda * cosPhi * t;
        y = this.SphericalRadius * ((this.Mlfn(phi, sinPhi, cosPhi) - this.meridionalOriginDistance) + (0.5d * lambda * lambda * cosPhi * sinPhi * t));
    }

    private void InverseEllipsoidalPolar(ProjectionMode polarMode, double xMeter, double yMeter, out double lon, out double lat)
    {
        double x = xMeter * this.InverseSphericalRadius;
        double y = yMeter * this.InverseSphericalRadius;
        double rho = Hypot(x, y);

        lat = this.Inv_mlfn(
            polarMode == ProjectionMode.NorthPole
                ? this.meridionalPoleDistance - rho
                : this.meridionalPoleDistance + rho);

        lon = Adjust_lon(this.centralMeridian + Math.Atan2(x, polarMode == ProjectionMode.NorthPole ? -y : y));
    }

    private void InverseEllipsoidalGuam(double xMeter, double yMeter, out double lon, out double lat)
    {
        if (this.scaleFactor == 0d)
        {
            ProjectionThrowHelper.ThrowInvalidOperation("Scale factor must be non-zero for Guam Azimuthal Equidistant inverse.");
        }

        double x = xMeter * this.InverseSphericalRadius;
        double y = yMeter * this.InverseSphericalRadius;
        double xSquaredHalf = 0.5d * x * x;
        lat = this.latOrigin;
        double t = 0d;
        for (int i = 0; i < 3; i++)
        {
            t = this.e * Math.Sin(lat);
            t = Math.Sqrt(1d - (t * t));
            lat = this.Inv_mlfn(this.meridionalOriginDistance + y - (xSquaredHalf * Math.Tan(lat) * t));
        }

        lon = Adjust_lon(this.centralMeridian + ((x * t) / Math.Cos(lat)));
    }

    private void ForwardEllipsoidalGeneral(double lambda, double phi, out double x, out double y)
    {
        if (Math.Abs(lambda) < Eps10 && Math.Abs(phi - this.latOrigin) < Eps10)
        {
            x = 0d;
            y = 0d;
            return;
        }

        if (!EllipsoidalGeodesic.TryVincentyInverse(this.semiMinor, this.flattening, this.eccentricityPrimeSquared, this.latOrigin, 0d, phi, lambda, out double distance, out double azimuth))
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(phi), "Coordinate is outside the valid Azimuthal Equidistant domain.");
        }

        double scaledDistance = distance * this.scaleFactor;
        x = scaledDistance * Math.Sin(azimuth);
        y = scaledDistance * Math.Cos(azimuth);
    }

    private void InverseEllipsoidalGeneral(double xMeter, double yMeter, out double lon, out double lat)
    {
        double rho = Hypot(xMeter, yMeter);
        if (rho < Eps10)
        {
            lon = this.centralMeridian;
            lat = this.latOrigin;
            return;
        }

        if (this.scaleFactor == 0d)
        {
            ProjectionThrowHelper.ThrowInvalidOperation("Scale factor must be non-zero for ellipsoidal Azimuthal Equidistant inverse.");
        }

        double azimuth = Math.Atan2(xMeter, yMeter);
        double distance = rho / this.scaleFactor;
        if (!EllipsoidalGeodesic.TryVincentyDirect(this.semiMinor, this.flattening, this.eccentricityPrimeSquared, this.latOrigin, 0d, azimuth, distance, out double phi, out double lambda))
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(xMeter), "Coordinate is outside the valid Azimuthal Equidistant domain.");
        }

        lon = Adjust_lon(this.centralMeridian + lambda);
        lat = phi;
    }
}
