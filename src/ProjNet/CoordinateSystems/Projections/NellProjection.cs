// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Nell projection (<c>nell</c>).
/// </summary>
/// <remarks>
/// Nell is a spherical pseudocylindrical projection with straight parallels and curved
/// meridians. The implementation solves the auxiliary relation
/// <c>φ' + sin(φ') = 2 * sin(φ)</c> and then evaluates
/// <c>x = 0.5 * λ * (1 + cos(φ'))</c>, <c>y = φ'</c>.
/// </remarks>
internal sealed class NellProjection : MapProjection
{
    private const int Iterations = 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="NellProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public NellProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NellProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public NellProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Nell";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new NellProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double k = 2d * Math.Sin(lat);
        double phiSquared = lat * lat;
        double phi = lat * (1.00371d + (phiSquared * (-0.0935382d + (phiSquared * -0.011412d))));
        for (int i = Iterations; i > 0; i--)
        {
            double denominator = 1d + Math.Cos(phi);
            if (Math.Abs(denominator) <= Eps10)
            {
                break;
            }

            double v = (phi + Math.Sin(phi) - k) / denominator;
            phi -= v;
            if (Math.Abs(v) < Eps7)
            {
                break;
            }
        }

        double x = 0.5d * lambda * (1d + Math.Cos(phi));
        double y = phi;
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double denominator = 1d + Math.Cos(yy);
        if (Math.Abs(denominator) <= Eps10)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double lambda = (2d * xx) / denominator;
        double phi = Asinz(0.5d * (yy + Math.Sin(yy)));
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
