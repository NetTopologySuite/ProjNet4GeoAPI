// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Gnomonic map projection (<c>gnom</c>).
/// </summary>
/// <remarks>
/// The Gnomonic projection is a perspective azimuthal projection from the center of the
/// sphere onto a tangent plane. All great circles (geodesics) project as straight lines.
/// Points at or beyond 90° angular distance from the projection center cannot be projected
/// and produce <see cref="double.NaN"/> output coordinates.
/// <para>The formulation was independently verified against the Wikipedia article
/// "Gnomonic projection" and Eric W. Weisstein's MathWorld entry "Gnomonic Projection".
/// The perspective scale <c>k = 1 / cos(c)</c> together with the azimuthal forward and
/// inverse relations based on the angular distance <c>c</c> match the implementation here.</para>
/// <para>The ellipsoidal extension follows the PROJ <c>gnom</c> implementation and Karney's
/// geodesic formulation, using the reduced length <c>m12</c> and geodesic scale <c>M12</c>
/// so that <c>ρ = m12 / M12</c>. The inverse uses Newton iteration on geodesic distance,
/// matching PROJ's stabilized small- and large-<c>ρ</c> updates.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Gnomonic_projection">Wikipedia: Gnomonic projection.</seealso>
/// <seealso href="https://mathworld.wolfram.com/GnomonicProjection.html">MathWorld: Gnomonic Projection.</seealso>
/// <seealso href="https://proj.org/en/stable/operations/projections/gnom.html">PROJ documentation: Gnomonic.</seealso>
/// <seealso>Bugayevskiy &amp; Snyder (1995), "Map Projections: A Reference Manual", Ch. 3, Sect. 3.3.1, pp. 109-115.</seealso>
internal class GnomonicProjection : MapProjection
{
    private const int MaxInverseIterations = 10;
    private const double InverseDistanceTolerance = 1.4901161193847656e-10d;

    private readonly bool ellipsoidal;
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double sinPhi0;
    private readonly double cosPhi0;
    private readonly double flattening;
    private readonly double eccentricityPrimeSquared;
    private readonly double meridionalOriginDistance;

    /// <summary>
    /// Initializes a new instance of the <see cref="GnomonicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GnomonicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GnomonicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GnomonicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Gnomonic";
        this.ellipsoidal = this.es > 0d;
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1.0 / this.radius;
        Sincos(this.latOrigin, out this.sinPhi0, out this.cosPhi0);
        this.flattening = (this.semiMajor - this.semiMinor) / this.semiMajor;
        this.eccentricityPrimeSquared = ((this.semiMajor * this.semiMajor) - (this.semiMinor * this.semiMinor)) / (this.semiMinor * this.semiMinor);
        this.meridionalOriginDistance = this.Mlfn(this.latOrigin, this.sinPhi0, this.cosPhi0);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GnomonicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (this.ellipsoidal)
        {
            this.ForwardEllipsoidal(ref lon, ref lat);
            return;
        }

        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinPhi = Math.Sin(lat);
        double cosPhi = Math.Cos(lat);
        double cosLambda = Math.Cos(lambda);

        double cosC = (this.sinPhi0 * sinPhi) + (this.cosPhi0 * cosPhi * cosLambda);
        if (cosC <= Eps10)
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        double k = 1d / cosC;
        lon = this.radius * k * cosPhi * Math.Sin(lambda);
        lat = this.radius * k * ((this.cosPhi0 * sinPhi) - (this.sinPhi0 * cosPhi * cosLambda));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        if (this.ellipsoidal)
        {
            this.InverseEllipsoidal(ref x, ref y);
            return;
        }

        double rho = Hypot(x, y);
        if (rho <= Eps10)
        {
            x = this.centralMeridian;
            y = this.latOrigin;
            return;
        }

        double c = Math.Atan(rho * this.inverseRadius);
        double sinC = Math.Sin(c);
        double cosC = Math.Cos(c);

        double phi = Math.Asin(ProjectionConstants.Clamp((cosC * this.sinPhi0) + ((y * sinC * this.cosPhi0) / rho), -1d, 1d));
        double lambda = Math.Atan2(x * sinC, (rho * this.cosPhi0 * cosC) - (y * this.sinPhi0 * sinC));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private void ForwardEllipsoidal(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        if (Math.Abs(lambda) <= 1e-14d)
        {
            double sinPhi = Math.Sin(lat);
            double cosPhi = Math.Cos(lat);
            double meridionalDistance = this.semiMajor * Math.Abs(this.Mlfn(lat, sinPhi, cosPhi) - this.meridionalOriginDistance);
            double meridionalAzimuth = lat >= this.latOrigin ? 0d : Math.PI;

            if (!EllipsoidalGeodesic.TrySolvePositionReducedLengthAndScale(
                this.semiMajor,
                this.semiMinor,
                this.flattening,
                this.es,
                this.eccentricityPrimeSquared,
                this.latOrigin,
                0d,
                meridionalAzimuth,
                meridionalDistance,
                out _,
                out _,
                out double meridionalReducedLength,
                out double meridionalGeodesicScale))
            {
                lon = double.NaN;
                lat = double.NaN;
                return;
            }

            if (meridionalGeodesicScale <= 0d)
            {
                lon = double.NaN;
                lat = double.NaN;
                return;
            }

            lon = 0d;
            lat = this.scaleFactor * (meridionalReducedLength / meridionalGeodesicScale) * Math.Cos(meridionalAzimuth);
            return;
        }

        if (!EllipsoidalGeodesic.TryVincentyInverse(
            this.semiMinor,
            this.flattening,
            this.eccentricityPrimeSquared,
            this.latOrigin,
            0d,
            lat,
            lambda,
            out double distance,
            out double azimuth))
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        if (!EllipsoidalGeodesic.TrySolvePositionReducedLengthAndScale(
            this.semiMajor,
            this.semiMinor,
            this.flattening,
            this.es,
            this.eccentricityPrimeSquared,
            this.latOrigin,
            0d,
            azimuth,
            distance,
            out _,
            out _,
            out double reducedLength,
            out double geodesicScale))
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        if (geodesicScale <= 0d)
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        double rho = this.scaleFactor * (reducedLength / geodesicScale);
        lon = rho * Math.Sin(azimuth);
        lat = rho * Math.Cos(azimuth);
    }

    private void InverseEllipsoidal(ref double x, ref double y)
    {
        double rhoProjected = Hypot(x, y);
        if (rhoProjected <= Eps10)
        {
            x = this.centralMeridian;
            y = this.latOrigin;
            return;
        }

        double azimuth = Math.Atan2(x, y);
        double rho = rhoProjected / this.scaleFactor;
        bool little = rho <= this.semiMajor;
        double iterationRho = little ? rho : 1d / rho;
        double distance = this.semiMajor * Math.Atan(rho * this.inverseRadius);
        double tolerance = InverseDistanceTolerance * this.semiMajor;

        for (int iteration = 0; iteration < MaxInverseIterations; iteration++)
        {
            if (!EllipsoidalGeodesic.TrySolvePositionReducedLengthAndScale(
                this.semiMajor,
                this.semiMinor,
                this.flattening,
                this.es,
                this.eccentricityPrimeSquared,
                this.latOrigin,
                0d,
                azimuth,
                distance,
                out _,
                out _,
                out double reducedLength,
                out double geodesicScale))
            {
                x = double.NaN;
                y = double.NaN;
                return;
            }

            double deltaDistance = little
                ? (reducedLength - (iterationRho * geodesicScale)) * geodesicScale
                : ((iterationRho * reducedLength) - geodesicScale) * reducedLength;

            distance -= deltaDistance;
            if (Math.Abs(deltaDistance) < tolerance)
            {
                if (!EllipsoidalGeodesic.TrySolvePositionReducedLengthAndScale(
                    this.semiMajor,
                    this.semiMinor,
                    this.flattening,
                    this.es,
                    this.eccentricityPrimeSquared,
                    this.latOrigin,
                    0d,
                    azimuth,
                    distance,
                    out double latitude,
                    out double longitude,
                    out _,
                    out _))
                {
                    x = double.NaN;
                    y = double.NaN;
                    return;
                }

                x = Math.Abs(Math.Abs(latitude) - HalfPi) < 1e-9d
                    ? this.centralMeridian
                    : Adjust_lon(this.centralMeridian + longitude);
                y = latitude;
                return;
            }
        }

        x = double.NaN;
        y = double.NaN;
    }
}
