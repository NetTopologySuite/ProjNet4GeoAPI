// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Two Point Equidistant projection (<c>tpeqd</c>).
/// </summary>
[Serializable]
internal sealed class TwoPointEquidistantProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double cp1;
    private readonly double sp1;
    private readonly double cp2;
    private readonly double sp2;
    private readonly double ccs;
    private readonly double cs;
    private readonly double sc;
    private readonly double r2z0;
    private readonly double z02;
    private readonly double dlam2;
    private readonly double hz0;
    private readonly double thz0;
    private readonly double rhshz0;
    private readonly double ca;
    private readonly double sa;
    private readonly double lp;
    private readonly double lamc;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoPointEquidistantProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public TwoPointEquidistantProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoPointEquidistantProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public TwoPointEquidistantProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Two_Point_Equidistant";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        double phi1 = DegreesToRadians(this.Parameters.GetParameterValue("lat_1", "standard_parallel_1"));
        double lam1 = DegreesToRadians(this.Parameters.GetParameterValue("lon_1"));
        double phi2 = DegreesToRadians(this.Parameters.GetParameterValue("lat_2", "standard_parallel_2"));
        double lam2 = DegreesToRadians(this.Parameters.GetParameterValue("lon_2"));

        if (Math.Abs(phi1 - phi2) < Eps10 && Math.Abs(Adjust_lon(lam1 - lam2)) < Eps10)
        {
            ArgumentGuard.ThrowArgument("Invalid value for lat_1/lon_1/lat_2/lon_2: the 2 points should be distinct.");
        }

        this.centralMeridian = Adjust_lon(0.5d * (lam1 + lam2));
        double rawDlam2 = Adjust_lon(lam2 - lam1);
        this.cp1 = Math.Cos(phi1);
        this.cp2 = Math.Cos(phi2);
        this.sp1 = Math.Sin(phi1);
        this.sp2 = Math.Sin(phi2);
        this.cs = this.cp1 * this.sp2;
        this.sc = this.sp1 * this.cp2;
        this.ccs = this.cp1 * this.cp2 * Math.Sin(rawDlam2);

        double cp2SinDlam = this.cp2 * Math.Sin(rawDlam2);
        double csMinusScCosDlam = this.cs - (this.sc * Math.Cos(rawDlam2));
        double z0 = Math.Atan2(
            Math.Sqrt((cp2SinDlam * cp2SinDlam) + (csMinusScCosDlam * csMinusScCosDlam)),
            (this.sp1 * this.sp2) + (this.cp1 * this.cp2 * Math.Cos(rawDlam2)));
        if (Math.Abs(z0) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Invalid value for lat_1 and lat_2: their absolute value should be < 90°.");
        }

        this.hz0 = 0.5d * z0;
        double a12 = Math.Atan2(cp2SinDlam, csMinusScCosDlam);
        double pp = Asinz(this.cp1 * Math.Sin(a12));
        this.ca = Math.Cos(pp);
        this.sa = Math.Sin(pp);
        this.lp = Adjust_lon(Math.Atan2(this.cp1 * Math.Cos(a12), this.sp1) - this.hz0);
        this.dlam2 = rawDlam2 * 0.5d;
        this.lamc = HalfPi - Math.Atan2(Math.Sin(a12) * this.sp1, Math.Cos(a12)) - this.dlam2;
        this.thz0 = Math.Tan(this.hz0);
        this.rhshz0 = 0.5d / Math.Sin(this.hz0);
        this.r2z0 = 0.5d / z0;
        this.z02 = z0 * z0;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new TwoPointEquidistantProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        double sp = Math.Sin(phi);
        double cp = Math.Cos(phi);
        double dl1 = lambda + this.dlam2;
        double dl2 = lambda - this.dlam2;
        double z1 = Math.Acos(Clamp((this.sp1 * sp) + (this.cp1 * cp * Math.Cos(dl1)), -1d, 1d));
        double z2 = Math.Acos(Clamp((this.sp2 * sp) + (this.cp2 * cp * Math.Cos(dl2)), -1d, 1d));
        double z1Squared = z1 * z1;
        double z2Squared = z2 * z2;

        double t = z1Squared - z2Squared;
        double xUnit = this.r2z0 * t;
        t = this.z02 - t;
        double yRadicand = (4d * this.z02 * z2Squared) - (t * t);
        if (yRadicand < -1e-12d)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double yUnit = this.r2z0 * Math.Sqrt(Math.Max(0d, yRadicand));
        if ((this.ccs * sp) - (cp * ((this.cs * Math.Sin(dl1)) - (this.sc * Math.Sin(dl2)))) < 0d)
        {
            yUnit = -yUnit;
        }

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        double cz1 = Math.Cos(Hypot(yUnit, xUnit + this.hz0));
        double cz2 = Math.Cos(Hypot(yUnit, xUnit - this.hz0));
        double s = cz1 + cz2;
        double d = cz1 - cz2;
        double lambda = -Math.Atan2(d, s * this.thz0);
        double phi = Math.Acos(Clamp(Hypot(this.thz0 * s, d) * this.rhshz0, -1d, 1d));
        if (yUnit < 0d)
        {
            phi = -phi;
        }

        double sp = Math.Sin(phi);
        double cp = Math.Cos(phi);
        lambda -= this.lp;
        double cosLambda = Math.Cos(lambda);
        double phiOut = Asinz((this.sa * sp) + (this.ca * cp * cosLambda));
        double lambdaOut = Math.Atan2(cp * Math.Sin(lambda), (this.sa * cp * cosLambda) - (this.ca * sp)) + this.lamc;

        x = Adjust_lon(this.centralMeridian + lambdaOut);
        y = phiOut;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }
}
