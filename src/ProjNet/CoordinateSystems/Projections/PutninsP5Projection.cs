// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Putnins P5 projection (<c>putp5</c>).
/// </summary>
/// <remarks>
/// Putnins P5 is one of the spherical projections published by Reinholds Putnins in 1934.
/// Its longitude scale follows the family form
/// <c>A - B * sqrt(1 + D * φ²)</c>, with parameter values that can also be specialized
/// for the prime variant.
/// </remarks>
internal class PutninsP5Projection : MapProjection
{
    private const double C = 1.01346d;
    private const double D = 1.2158542d;
    private const double DefaultA = 2d;
    private const double DefaultB = 1d;

    private readonly double a;
    private readonly double b;

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP5Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP5Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Putnins_P5";
        this.a = this.Parameters.GetOptionalParameterValue("putp5_a", DefaultA);
        this.b = this.Parameters.GetOptionalParameterValue("putp5_b", DefaultB);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new PutninsP5Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = C * lambda * (this.a - (this.b * Math.Sqrt(1d + (D * lat * lat))));
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
        double denominator = C * (this.a - (this.b * Math.Sqrt(1d + (D * phi * phi))));
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
