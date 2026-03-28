// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Lagrange projection (<c>lagrng</c>).
/// </summary>
[Serializable]
internal class LagrangeProjection : MapProjection
{
    private const double Tolerance = 1e-10d;

    private readonly double radius;
    private readonly double inverseRadius;
    private readonly double a1;
    private readonly double a2;
    private readonly double hrw;
    private readonly double hw;
    private readonly double rw;
    private readonly double w;

    /// <summary>
    /// Initializes a new instance of the <see cref="LagrangeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LagrangeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LagrangeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public LagrangeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(MergeDefaults(parameters), inverse)
    {
        this.Name = "Lagrange";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;

        this.w = this.Parameters.GetOptionalParameterValue("W", 2d);
        if (this.w <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for W: it should be > 0");
        }

        this.hw = 0.5d * this.w;
        this.rw = 1d / this.w;
        this.hrw = 0.5d * this.rw;
        double sinPhi1 = Math.Sin(DegreesToRadians(this.Parameters.GetParameterValue("lat_1")));
        if (Math.Abs(Math.Abs(sinPhi1) - 1d) < Tolerance)
        {
            ArgumentGuard.ThrowArgument("Invalid value for lat_1: |lat_1| should be < 90°");
        }

        this.a1 = Math.Pow((1d - sinPhi1) / (1d + sinPhi1), this.hrw);
        this.a2 = this.a1 * this.a1;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new LagrangeProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double sinPhi = Math.Sin(lat);
        double x;
        double y;
        if (Math.Abs(Math.Abs(sinPhi) - 1d) < Tolerance)
        {
            x = 0d;
            y = lat < 0d ? -2d : 2d;
        }
        else
        {
            double v = this.a1 * Math.Pow((1d + sinPhi) / (1d - sinPhi), this.hrw);
            double lambdaScaled = lambda * this.rw;
            double c = (0.5d * (v + (1d / v))) + Math.Cos(lambdaScaled);
            if (c < Tolerance)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            x = 2d * Math.Sin(lambdaScaled) / c;
            y = (v - (1d / v)) / c;
        }

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double lambda;
        double phi;
        if (Math.Abs(Math.Abs(yy) - 2d) < Tolerance)
        {
            phi = yy < 0d ? -HalfPi : HalfPi;
            lambda = 0d;
        }
        else
        {
            double x2 = xx * xx;
            double y2p = 2d + yy;
            double y2m = 2d - yy;
            double c = (y2p * y2m) - x2;
            if (Math.Abs(c) < Tolerance)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            phi =
                (2d * Math.Atan(Math.Pow(
                    ((y2p * y2p) + x2) / (this.a2 * ((y2m * y2m) + x2)),
                    this.hw)))
                - HalfPi;
            lambda = this.w * Math.Atan2(4d * xx, c);
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }

    private static List<ProjectionParameter> MergeDefaults(IEnumerable<ProjectionParameter> parameters)
    {
        List<ProjectionParameter> merged = CloneParametersList(parameters);
        bool hasLat1 = false;
        bool hasW = false;
        for (int i = 0; i < merged.Count; i++)
        {
            if (merged[i].Name.Equals("lat_1", StringComparison.OrdinalIgnoreCase))
            {
                hasLat1 = true;
            }
            else if (merged[i].Name.Equals("W", StringComparison.OrdinalIgnoreCase))
            {
                hasW = true;
            }
        }

        if (!hasLat1)
        {
            merged.Add(new ProjectionParameter("lat_1", 0d));
        }

        if (!hasW)
        {
            merged.Add(new ProjectionParameter("W", 2d));
        }

        return merged;
    }
}
