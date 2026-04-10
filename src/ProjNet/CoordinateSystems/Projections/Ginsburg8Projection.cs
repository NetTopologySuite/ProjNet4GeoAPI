// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Ginsburg VIII projection (<c>gins8</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported in this implementation.
/// <para>The forward formulation was independently verified against the standard Ginsburg
/// VIII polynomial approximation. The implementation matches the latitude series
/// <c>y = φ * (1 + φ² / 12)</c> and the longitude scaling that combines latitude and
/// quartic longitude damping.</para>
/// </remarks>
internal class Ginsburg8Projection : MapProjection
{
    private const double Cl = 0.000952426d;
    private const double Cp = 0.162388d;
    private const double C12 = 0.08333333333333333d;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ginsburg8Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Ginsburg8Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Ginsburg8Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Ginsburg8Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Ginsburg_VIII";
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new Ginsburg8Projection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = lat * lat;
        double y = lat * (1d + (t * C12));
        double x = lambda * (1d - (Cp * t));
        t = lambda * lambda;
        x *= 0.87d - (Cl * t * t);

        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Ginsburg VIII does not support inverse projection in this wave.");
    }
}
