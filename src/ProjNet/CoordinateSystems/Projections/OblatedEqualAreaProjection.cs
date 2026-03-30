// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Oblated Equal Area projection (<c>oea</c>).
/// </summary>
internal sealed class OblatedEqualAreaProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double theta;
    private readonly double m;
    private readonly double n;
    private readonly double twoRM;
    private readonly double twoRN;
    private readonly double rm;
    private readonly double rn;
    private readonly double hm;
    private readonly double hn;
    private readonly double cp0;
    private readonly double sp0;

    /// <summary>
    /// Initializes a new instance of the <see cref="OblatedEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public OblatedEqualAreaProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OblatedEqualAreaProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public OblatedEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Oblated_Equal_Area";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.n = this.Parameters.GetParameterValue("n");
        if (this.n <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for n: it should be > 0.");
        }

        this.m = this.Parameters.GetParameterValue("m");
        if (this.m <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for m: it should be > 0.");
        }

        this.theta = DegreesToRadians(this.Parameters.GetOptionalParameterValue("theta", 0d));
        this.sp0 = Math.Sin(this.latOrigin);
        this.cp0 = Math.Cos(this.latOrigin);
        this.rn = 1d / this.n;
        this.rm = 1d / this.m;
        this.twoRN = 2d * this.rn;
        this.twoRM = 2d * this.rm;
        this.hm = 0.5d * this.m;
        this.hn = 0.5d * this.n;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new OblatedEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double cp = Math.Cos(lat);
        double sp = Math.Sin(lat);
        double cl = Math.Cos(lambda);
        double az = Math.Atan2(cp * Math.Sin(lambda), (this.cp0 * sp) - (this.sp0 * cp * cl)) + this.theta;
        double cosCentral = ProjectionConstants.Clamp((this.sp0 * sp) + (this.cp0 * cp * cl), -1d, 1d);
        double shz = Math.Sin(0.5d * Math.Acos(cosCentral));
        double mAngle = Asinz(shz * Math.Sin(az));

        double denominator = Math.Cos(mAngle * this.twoRM);
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double nAngle = Asinz((shz * Math.Cos(az) * Math.Cos(mAngle)) / denominator);
        double cosNScaled = Math.Cos(nAngle * this.twoRN);
        if (Math.Abs(cosNScaled) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double yUnit = this.n * Math.Sin(nAngle * this.twoRN);
        double xUnit = this.m * Math.Sin(mAngle * this.twoRM) * Math.Cos(nAngle) / cosNScaled;

        lon = this.radius * xUnit;
        lat = this.radius * yUnit;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xUnit = x * this.inverseRadius;
        double yUnit = y * this.inverseRadius;

        double nArg = ProjectionConstants.Clamp(yUnit * this.rn, -1d, 1d);
        double nAngle = this.hn * Math.Asin(nArg);
        double cosN = Math.Cos(nAngle);
        if (Math.Abs(cosN) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double mArg = ProjectionConstants.Clamp(xUnit * this.rm * Math.Cos(nAngle * this.twoRN) / cosN, -1d, 1d);
        double mAngle = this.hm * Math.Asin(mArg);

        double xp = 2d * Math.Sin(mAngle);
        double cosM = Math.Cos(mAngle);
        if (Math.Abs(cosM) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double yp = 2d * Math.Sin(nAngle) * Math.Cos(mAngle * this.twoRM) / cosM;
        double az = Math.Atan2(xp, yp) - this.theta;
        double caz = Math.Cos(az);
        double z = 2d * Math.Asin(ProjectionConstants.Clamp(0.5d * Hypot(xp, yp), -1d, 1d));
        double sz = Math.Sin(z);
        double cz = Math.Cos(z);

        double phi = Asinz((this.sp0 * cz) + (this.cp0 * sz * caz));
        double lambda = Math.Atan2(sz * Math.Sin(az), (this.cp0 * cz) - (this.sp0 * sz * caz));

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
