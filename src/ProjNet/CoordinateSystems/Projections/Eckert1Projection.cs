// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Eckert I projection (<c>eck1</c>).
/// </summary>
/// <remarks>
/// Eckert I is a spherical pseudocylindrical projection with straight parallels and a linear
/// reduction of the meridian lengths toward the poles. The formulation was independently
/// verified against the modern summary in the Wikipedia article "Eckert projection" and
/// Max Eckert's 1906 description of the family. The scale term
/// <c>x = 0.9213177319 * λ * (1 - |φ| / π)</c> together with
/// <c>y = 0.9213177319 * φ</c> matches the implementation here.
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Eckert_projection">Wikipedia: Eckert projection family.</seealso>
internal class Eckert1Projection : MapProjection
{
    private const double Fc = 0.92131773192356127802d;
    private const double Rp = 0.31830988618379067154d;

    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Eckert1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Eckert1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Eckert_I";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Eckert1Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = Fc * lambda * (1d - (Rp * Math.Abs(lat)));
        double y = Fc * lat;

        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.inverseRadius;
        double yy = y * this.inverseRadius;
        double phi = yy / Fc;
        double denominator = Fc * (1d - (Rp * Math.Abs(phi)));
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
