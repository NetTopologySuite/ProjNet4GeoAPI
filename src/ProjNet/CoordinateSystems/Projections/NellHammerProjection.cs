// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Nell-Hammer projection (<c>nell_h</c>).
/// </summary>
internal class NellHammerProjection : MapProjection
{
    private const int Iterations = 9;
    private const double Epsilon = 1e-7d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="NellHammerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NellHammerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NellHammerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NellHammerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Nell_Hammer";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new NellHammerProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = 0.5d * lambda * (1d + Math.Cos(lat));
        double y = 2d * (lat - Math.Tan(0.5d * lat));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;

        double p = 0.5d * yy;
        double phi = 0d;
        int i = Iterations;
        for (; i > 0; i--)
        {
            double c = Math.Cos(0.5d * phi);
            double denominator = 1d - (0.5d / (c * c));
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phi - Math.Tan(phi / 2d) - p) / denominator;
            phi -= v;
            if (Math.Abs(v) < Epsilon)
            {
                break;
            }
        }

        double lambda;
        if (i == 0)
        {
            phi = p < 0d ? -HalfPi : HalfPi;
            lambda = 2d * xx;
        }
        else
        {
            double denominator = 1d + Math.Cos(phi);
            if (Math.Abs(denominator) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }

            lambda = (2d * xx) / denominator;
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
