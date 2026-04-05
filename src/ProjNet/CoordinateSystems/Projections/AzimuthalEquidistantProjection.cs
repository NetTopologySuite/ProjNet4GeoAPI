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
internal class AzimuthalEquidistantProjection : MapProjection
{
    private const int MaxGeodesicIterations = 100;
    private const double GeodesicTolerance = 1e-12;
    private const double PathologicalTolerance = 1e-14;

    private readonly bool ellipsoidal;
    private readonly ProjectionMode mode;
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double sinPhi0;
    private readonly double cosPhi0;
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
        this.mode = DetermineMode(this.latOrigin);
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1.0 / this.radius;
        Sincos(this.latOrigin, out this.sinPhi0, out this.cosPhi0);
        this.flattening = (this.semiMajor - this.semiMinor) / this.semiMajor;
        this.eccentricityPrimeSquared = ((this.semiMajor * this.semiMajor) - (this.semiMinor * this.semiMinor)) / (this.semiMinor * this.semiMinor);

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

    private static double ComputeDeltaSigma(double bCoeff, double sinSigma, double cosSigma, double cos2SigmaM)
    {
        double cos2SigmaMSquared = cos2SigmaM * cos2SigmaM;
        double sinSigmaSquared = sinSigma * sinSigma;
        double firstTerm = cosSigma * (-1d + (2d * cos2SigmaMSquared));
        double secondTerm = (bCoeff / 6d) * cos2SigmaM * (-3d + (4d * sinSigmaSquared)) * (-3d + (4d * cos2SigmaMSquared));
        double bracket = cos2SigmaM + ((bCoeff / 4d) * (firstTerm - secondTerm));
        return bCoeff * sinSigma * bracket;
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

    private static double NormalizeLongitude(double longitude)
    {
        return Math.Atan2(Math.Sin(longitude), Math.Cos(longitude));
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

            double rho = this.radius * (HalfPi + phi);
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

                x = 0d;
                y = 0d;
                return;
            }

            double c = Math.Acos(cosC);
            double k = c / Math.Sin(c);
            x = this.radius * k * cosPhi * sinLambda;
            y = this.radius * k * sinPhi;
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

            x = 0d;
            y = 0d;
            return;
        }

        double cOblique = Math.Acos(cosCOblique);
        double kOblique = cOblique / Math.Sin(cOblique);
        x = this.radius * kOblique * cosPhi * sinLambda;
        y = this.radius * kOblique * ((this.cosPhi0 * sinPhi) - (this.sinPhi0 * cosPhiCosLambda));
    }

    private void InverseSpherical(double xMeter, double yMeter, out double lon, out double lat)
    {
        double x = xMeter * this.inverseRadius;
        double y = yMeter * this.inverseRadius;

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

        x = this.radius * rho * Math.Sin(lambda);
        y = this.radius * rho * cosineLambda;
    }

    private void InverseEllipsoidalPolar(ProjectionMode polarMode, double xMeter, double yMeter, out double lon, out double lat)
    {
        double x = xMeter * this.inverseRadius;
        double y = yMeter * this.inverseRadius;
        double rho = Hypot(x, y);

        lat = this.Inv_mlfn(
            polarMode == ProjectionMode.NorthPole
                ? this.meridionalPoleDistance - rho
                : this.meridionalPoleDistance + rho);

        lon = Adjust_lon(this.centralMeridian + Math.Atan2(x, polarMode == ProjectionMode.NorthPole ? -y : y));
    }

    private void ForwardEllipsoidalGeneral(double lambda, double phi, out double x, out double y)
    {
        if (Math.Abs(lambda) < Eps10 && Math.Abs(phi - this.latOrigin) < Eps10)
        {
            x = 0d;
            y = 0d;
            return;
        }

        if (!this.TryVincentyInverse(this.latOrigin, 0d, phi, lambda, out double distance, out double azimuth))
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
            ArgumentGuard.ThrowArgument("Scale factor must be non-zero for ellipsoidal Azimuthal Equidistant inverse.");
        }

        double azimuth = Math.Atan2(xMeter, yMeter);
        double distance = rho / this.scaleFactor;
        if (!this.TryVincentyDirect(this.latOrigin, 0d, azimuth, distance, out double phi, out double lambda))
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(xMeter), "Coordinate is outside the valid Azimuthal Equidistant domain.");
        }

        lon = Adjust_lon(this.centralMeridian + lambda);
        lat = phi;
    }

    private bool TryVincentyInverse(double latitude1, double longitude1, double latitude2, double longitude2, out double distance, out double azimuth)
    {
        distance = 0d;
        azimuth = 0d;

        if (Math.Abs(latitude1 - latitude2) < GeodesicTolerance && Math.Abs(longitude1 - longitude2) < GeodesicTolerance)
        {
            return true;
        }

        double oneMinusF = 1d - this.flattening;
        double tanU1 = oneMinusF * Math.Tan(latitude1);
        double tanU2 = oneMinusF * Math.Tan(latitude2);
        double u1 = Math.Atan(tanU1);
        double u2 = Math.Atan(tanU2);
        double sinU1 = Math.Sin(u1);
        double cosU1 = Math.Cos(u1);
        double sinU2 = Math.Sin(u2);
        double cosU2 = Math.Cos(u2);

        double l = NormalizeLongitude(longitude2 - longitude1);
        double lambda = l;

        for (int iteration = 0; iteration < MaxGeodesicIterations; iteration++)
        {
            double sinLambda = Math.Sin(lambda);
            double cosLambda = Math.Cos(lambda);

            double term1 = cosU2 * sinLambda;
            double term2 = (cosU1 * sinU2) - (sinU1 * cosU2 * cosLambda);
            double sinSigma = Math.Sqrt((term1 * term1) + (term2 * term2));
            if (sinSigma < GeodesicTolerance)
            {
                return true;
            }

            double cosSigma = (sinU1 * sinU2) + (cosU1 * cosU2 * cosLambda);
            double sigma = Math.Atan2(sinSigma, cosSigma);
            double sinAlpha = (cosU1 * cosU2 * sinLambda) / sinSigma;
            double cosSqAlpha = 1d - (sinAlpha * sinAlpha);
            double cos2SigmaM = cosSqAlpha < GeodesicTolerance
                ? 0d
                : cosSigma - ((2d * sinU1 * sinU2) / cosSqAlpha);

            double c = (this.flattening / 16d) * cosSqAlpha * (4d + (this.flattening * (4d - (3d * cosSqAlpha))));
            double lambdaPrevious = lambda;
            lambda = l + ((1d - c) * this.flattening * sinAlpha * (sigma + (c * sinSigma * (cos2SigmaM + (c * cosSigma * (-1d + (2d * cos2SigmaM * cos2SigmaM)))))));
            if (Math.Abs(lambda - lambdaPrevious) <= GeodesicTolerance)
            {
                double uSq = cosSqAlpha * this.eccentricityPrimeSquared;
                double aCoeff = 1d + ((uSq / 16384d) * (4096d + (uSq * (-768d + (uSq * (320d - (175d * uSq)))))));
                double bCoeff = (uSq / 1024d) * (256d + (uSq * (-128d + (uSq * (74d - (47d * uSq))))));
                double deltaSigma = ComputeDeltaSigma(bCoeff, sinSigma, cosSigma, cos2SigmaM);

                distance = this.semiMinor * aCoeff * (sigma - deltaSigma);
                azimuth = Math.Atan2(
                    cosU2 * Math.Sin(lambda),
                    (cosU1 * sinU2) - (sinU1 * cosU2 * Math.Cos(lambda)));
                return true;
            }
        }

        return false;
    }

    private bool TryVincentyDirect(double latitude1, double longitude1, double azimuth1, double distance, out double latitude2, out double longitude2)
    {
        latitude2 = latitude1;
        longitude2 = longitude1;

        double oneMinusF = 1d - this.flattening;
        double tanU1 = oneMinusF * Math.Tan(latitude1);
        double u1 = Math.Atan(tanU1);
        double sinU1 = Math.Sin(u1);
        double cosU1 = Math.Cos(u1);
        double sinAlpha1 = Math.Sin(azimuth1);
        double cosAlpha1 = Math.Cos(azimuth1);

        double sigma1 = Math.Atan2(tanU1, cosAlpha1);
        double sinAlpha = cosU1 * sinAlpha1;
        double cosSqAlpha = 1d - (sinAlpha * sinAlpha);
        double uSq = cosSqAlpha * this.eccentricityPrimeSquared;
        double aCoeff = 1d + ((uSq / 16384d) * (4096d + (uSq * (-768d + (uSq * (320d - (175d * uSq)))))));
        double bCoeff = (uSq / 1024d) * (256d + (uSq * (-128d + (uSq * (74d - (47d * uSq))))));

        double sigma = distance / (this.semiMinor * aCoeff);
        for (int iteration = 0; iteration < MaxGeodesicIterations; iteration++)
        {
            double cos2SigmaM = Math.Cos((2d * sigma1) + sigma);
            double sinSigma = Math.Sin(sigma);
            double cosSigma = Math.Cos(sigma);
            double deltaSigma = ComputeDeltaSigma(bCoeff, sinSigma, cosSigma, cos2SigmaM);
            double sigmaPrevious = sigma;
            sigma = (distance / (this.semiMinor * aCoeff)) + deltaSigma;
            if (Math.Abs(sigma - sigmaPrevious) <= GeodesicTolerance)
            {
                double sinSigmaFinal = Math.Sin(sigma);
                double cosSigmaFinal = Math.Cos(sigma);
                double tmp = (sinU1 * sinSigmaFinal) - (cosU1 * cosSigmaFinal * cosAlpha1);

                latitude2 = Math.Atan2(
                    (sinU1 * cosSigmaFinal) + (cosU1 * sinSigmaFinal * cosAlpha1),
                    oneMinusF * Math.Sqrt((sinAlpha * sinAlpha) + (tmp * tmp)));

                double lambda = Math.Atan2(
                    sinSigmaFinal * sinAlpha1,
                    (cosU1 * cosSigmaFinal) - (sinU1 * sinSigmaFinal * cosAlpha1));

                double c = (this.flattening / 16d) * cosSqAlpha * (4d + (this.flattening * (4d - (3d * cosSqAlpha))));
                double l = lambda - ((1d - c) * this.flattening * sinAlpha * (sigma + (c * sinSigmaFinal * (cos2SigmaM + (c * cosSigmaFinal * (-1d + (2d * cos2SigmaM * cos2SigmaM)))))));
                longitude2 = NormalizeLongitude(longitude1 + l);
                return true;
            }
        }

        return false;
    }
}
