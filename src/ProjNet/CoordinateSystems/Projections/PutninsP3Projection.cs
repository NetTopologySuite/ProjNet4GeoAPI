// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Putnins P3 projection (<c>putp3</c>).
/// </summary>
/// <remarks>
/// Putnins P3 is one of the spherical projections published by Reinholds Putnins in 1934.
/// It is a parameterized cylindrical-like form with the fixed coefficient
/// <c>C = 0.79788456</c> and a configurable quadratic latitude damping term.
/// </remarks>
internal class PutninsP3Projection : MapProjection
{
    private const double C = 0.79788456d;
    private const double DefaultA = 4d * 0.1013211836d;

    private readonly double a;

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP3Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP3Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Putnins_P3";
        this.a = this.Parameters.GetOptionalParameterValue("putp3_a", DefaultA);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new PutninsP3Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = C * lambda * (1d - (this.a * lat * lat));
        double y = C * lat;

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;

        double phi = yy / C;
        double denominator = C * (1d - (this.a * phi * phi));
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
