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
/// <remarks>
/// Nell-Hammer is a spherical pseudocylindrical compromise projection that combines the
/// Nell longitude scale with a Hammer-style latitude spacing. Its forward form is
/// <c>x = 0.5 * λ * (1 + cos(φ))</c>,
/// <c>y = 2 * (φ - tan(φ / 2))</c>, and the inverse recovers <c>φ</c> by Newton
/// iteration.
/// </remarks>
internal sealed class NellHammerProjection : MapProjection
{
    private const int Iterations = 9;

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
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        double p = 0.5d * yy;
        double phi = 0d;
        int iteration = Iterations;
        for (; iteration > 0; iteration--)
        {
            double c = Math.Cos(0.5d * phi);
            double denominator = 1d - (0.5d / (c * c));
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phi - Math.Tan(phi / 2d) - p) / denominator;
            phi -= v;
            if (Math.Abs(v) < Eps7)
            {
                break;
            }
        }

        double lambda = (2d * xx) / (1d + Math.Cos(phi));
        if (iteration == 0)
        {
            phi = p < 0d ? -HalfPi : HalfPi;
            lambda = 2d * xx;
        }
        else
        {
            if (Math.Abs(1d + Math.Cos(phi)) <= Eps10)
            {
                ArgumentGuard.ThrowArgument("Input data outside projection domain.");
            }
        }

        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
