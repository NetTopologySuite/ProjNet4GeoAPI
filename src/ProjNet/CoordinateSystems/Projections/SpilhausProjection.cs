// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Spilhaus projection (<c>spilhaus</c>).
/// </summary>
/// <remarks>
/// <para>The Spilhaus projection is an ocean-centered world map derived from an oblique
/// Adams world-in-square construction. Its distinctive appearance comes from fixed
/// centering and rotation parameters chosen to place the world ocean into a continuous
/// layout.</para>
/// <para>The formulation was independently verified against Spilhaus reference material
/// and later comparative documentation, including the 2023 <i>Scientific Data</i>
/// article on Spilhaus ocean maps. The delegation to the Adams world-in-square basis
/// together with the fixed <c>lon0</c>, <c>lat0</c>, <c>azimuth</c>, and
/// <c>rotation</c> parameters matches the implementation here.</para>
/// </remarks>
/// <seealso href="https://blog.map-projections.net/spilhaus-projections-a-quintet-of-world-ocean-maps">Map Projections blog: Spilhaus projections.</seealso>
/// <seealso href="https://www.nature.com/articles/s41597-023-02309-6">Scientific Data (2023): Spilhaus ocean maps.</seealso>
internal sealed class SpilhausProjection : MapProjection
{
    private const double DefaultLon0Degrees = 66.94970198d;
    private const double DefaultLat0Degrees = -49.56371678d;
    private const double DefaultAzimuthDegrees = 40.17823482d;
    private const double DefaultRotationDegrees = 45d;

    private readonly double sinAlpha;
    private readonly double cosAlpha;
    private readonly double beta;
    private readonly double lambda0;
    private readonly double conformalDistortion;
    private readonly double cosRot;
    private readonly double sinRot;
    private readonly double lon0;
    private readonly AdamsWorldInSquare2Projection adamsWs2;
    private readonly MapProjection adamsWs2Inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpilhausProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public SpilhausProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpilhausProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public SpilhausProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Spilhaus";

        double lon0Degrees = this.Parameters.GetOptionalParameterValue("central_meridian", DefaultLon0Degrees, "longitude_of_center");
        double lat0Degrees = this.Parameters.GetOptionalParameterValue("latitude_of_origin", DefaultLat0Degrees, "latitude_of_center");
        this.lon0 = DegreesToRadians(lon0Degrees);
        double phi0 = DegreesToRadians(lat0Degrees);

        double azimuth = DegreesToRadians(this.Parameters.GetOptionalParameterValue("azi", DefaultAzimuthDegrees));
        double rotation = DegreesToRadians(this.Parameters.GetOptionalParameterValue("rot", DefaultRotationDegrees));

        Sincos(rotation, out this.sinRot, out this.cosRot);

        double conformalLatCenter = this.ToConformalLatitude(phi0);
        this.sinAlpha = -Math.Cos(conformalLatCenter) * Math.Cos(azimuth);
        this.cosAlpha = Math.Sqrt(Math.Max(0d, 1d - (this.sinAlpha * this.sinAlpha)));
        this.lambda0 = Math.Atan2(Math.Tan(azimuth), -Math.Sin(conformalLatCenter));
        this.beta = PI + Math.Atan2(-Math.Sin(azimuth), -Math.Tan(conformalLatCenter));

        double sinPhi0 = Math.Sin(phi0);
        this.conformalDistortion =
            Math.Cos(phi0) /
            Math.Sqrt(1d - (this.es * sinPhi0 * sinPhi0)) /
            Math.Cos(conformalLatCenter);

        this.adamsWs2 = new AdamsWorldInSquare2Projection(CreateUnitAdamsParameters());
        this.adamsWs2Inverse = (MapProjection)this.adamsWs2.Inverse();
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new SpilhausProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.lon0);
        double phiConformal = this.ToConformalLatitude(lat);
        double cosPhiConformal = Math.Cos(phiConformal);
        double sinPhiConformal = Math.Sin(phiConformal);

        double cosLambda = Math.Cos(lambda - this.lambda0);
        double sinLambda = Math.Sin(lambda - this.lambda0);

        double phiAdams = Asinz((this.sinAlpha * sinPhiConformal) - (this.cosAlpha * cosPhiConformal * cosLambda));
        double lambdaAdams = Adjust_lon(
            this.beta +
            Math.Atan2(
                cosPhiConformal * sinLambda,
                (this.sinAlpha * cosPhiConformal * cosLambda) + (this.cosAlpha * sinPhiConformal)));

        this.AdamsForward(lambdaAdams, phiAdams, out double xAdams, out double yAdams);

        double factor = this.conformalDistortion * this.scaleFactor;
        double xUnit = -((xAdams * this.cosRot) + (yAdams * this.sinRot)) * factor;
        double yUnit = -((xAdams * -this.sinRot) + (yAdams * this.cosRot)) * factor;

        lon = this.semiMajor * xUnit;
        lat = this.semiMajor * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double factor = 1d / (this.conformalDistortion * this.scaleFactor);
        double xUnit = x / this.semiMajor;
        double yUnit = y / this.semiMajor;

        double xAdams = -((xUnit * this.cosRot) + (yUnit * -this.sinRot)) * factor;
        double yAdams = -((xUnit * this.sinRot) + (yUnit * this.cosRot)) * factor;

        this.AdamsInverse(xAdams, yAdams, out double lambdaAdams, out double phiAdams);

        double cosPhiAdams = Math.Cos(phiAdams);
        double sinPhiAdams = Math.Sin(phiAdams);
        double cosLambdaAdams = Math.Cos(lambdaAdams - this.beta);
        double sinLambdaAdams = Math.Sin(lambdaAdams - this.beta);

        double chi = Asinz((this.sinAlpha * sinPhiAdams) + (this.cosAlpha * cosPhiAdams * cosLambdaAdams));
        double lambda =
            this.lambda0 +
            Math.Atan2(
                cosPhiAdams * sinLambdaAdams,
                (this.sinAlpha * cosPhiAdams * cosLambdaAdams) - (this.cosAlpha * sinPhiAdams));

        x = Adjust_lon(this.lon0 + lambda);
        y = this.FromConformalLatitude(chi);
    }

    private static IEnumerable<ProjectionParameter> CreateUnitAdamsParameters()
    {
        return
        [
            new ProjectionParameter("semi_major", 1d),
            new ProjectionParameter("semi_minor", 1d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
            new ProjectionParameter("unit", 1d),
        ];
    }

    private void AdamsForward(double lambda, double phi, out double x, out double y)
    {
        double lon = RadiansToDegrees(lambda);
        double lat = RadiansToDegrees(phi);
        double z = 0d;
        this.adamsWs2.Transform(ref lon, ref lat, ref z);
        x = lon;
        y = lat;
    }

    private void AdamsInverse(double x, double y, out double lambda, out double phi)
    {
        double xDeg = x;
        double yDeg = y;
        double z = 0d;
        this.adamsWs2Inverse.Transform(ref xDeg, ref yDeg, ref z);
        lambda = DegreesToRadians(xDeg);
        phi = DegreesToRadians(yDeg);
    }

    private double ToConformalLatitude(double phi)
    {
        if (this.e < Epsln)
        {
            return phi;
        }

        double ts = Tsfnz(this.e, phi, Math.Sin(phi));
        return HalfPi - (2d * Math.Atan(ts));
    }

    private double FromConformalLatitude(double chi)
    {
        if (this.e < Epsln)
        {
            return chi;
        }

        double ts = Math.Tan(0.5d * (HalfPi - chi));
        return Phi2z(this.e, ts, out _);
    }
}
