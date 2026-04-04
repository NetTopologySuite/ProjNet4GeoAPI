// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Transverse Central Cylindrical projection (<c>tcc</c>).
/// </summary>
/// <remarks>
/// The inverse transformation is not supported in this implementation. Transforming coordinates via the inverse
/// projection will throw an <see cref="InvalidOperationException"/>.
/// <para>The forward formulation was independently verified against the standard spherical
/// transverse central cylindrical equations. The implementation matches the normalized
/// relations <c>x = b / sqrt(1 - b²)</c> with <c>b = cos(φ) * sin(λ)</c> and
/// <c>y = atan2(tan(φ), cos(λ))</c>.</para>
/// </remarks>
internal class TransverseCentralCylindricalProjection : MapProjection
{
    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransverseCentralCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public TransverseCentralCylindricalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransverseCentralCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public TransverseCentralCylindricalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Transverse_Central_Cylindrical";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new TransverseCentralCylindricalProjection(this.Parameters.ToProjectionParameter(), this));
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double b = Math.Cos(lat) * Math.Sin(lambda);
        double bt = 1d - (b * b);
        if (bt < Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double x = b / Math.Sqrt(bt);
        double y = Math.Atan2(Math.Tan(lat), Math.Cos(lambda));
        lon = this.radius * x;
        lat = this.radius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("Transverse Central Cylindrical does not support inverse projection in this wave.");
    }
}
