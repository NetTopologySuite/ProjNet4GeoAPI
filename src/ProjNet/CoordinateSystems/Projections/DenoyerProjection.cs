// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Denoyer Semi-Elliptical projection (<c>denoy</c>).
/// </summary>
/// <remarks>
/// The Denoyer Semi-Elliptical projection is a compromise pseudocylindrical projection
/// intended for atlas use. Inverse projection is not supported.
/// <para>The forward formulation was independently verified against the standard Denoyer
/// semi-elliptical equation. The implementation matches the cosine longitude scaling with
/// the published polynomial-in-<c>|lambda|</c> and latitude modulation terms.</para>
/// </remarks>
internal class DenoyerProjection : MapProjection
{
    private const double C0 = 0.95d;
    private const double C1 = -0.08333333333333333333d;
    private const double C3 = 0.00166666666666666666d;
    private const double D1 = 0.9d;
    private const double D5 = 0.03d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenoyerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public DenoyerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenoyerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public DenoyerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Denoyer_Semi_Elliptical";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new DenoyerProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double x = lambda;
        double absLambda = Math.Abs(lambda);
        x *= Math.Cos(
            (C0 + (absLambda * (C1 + ((absLambda * absLambda) * C3))))
            * (lat * (D1 + (D5 * (lat * lat * lat * lat)))));

        lon = this.radius * x;
        lat = this.radius * lat;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Denoyer does not support inverse projection in this wave.");
    }
}
