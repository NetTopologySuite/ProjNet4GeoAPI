// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Central Cylindrical projection (<c>cc</c>).
/// </summary>
/// <remarks>
/// <para>Central Cylindrical is a spherical perspective cylindrical projection obtained
/// by projecting from the center of the sphere onto a tangent cylinder. Its compact
/// forward form is <c>x = λ</c>, <c>y = tan(φ)</c>, so the poles are outside the
/// projection domain.</para>
/// <para>The projection is mathematically trivial and was independently checked against
/// PROJ's <c>cc</c> description and standard cartographic references. It is neither
/// conformal nor equal-area and is primarily useful as a didactic perspective
/// construction rather than as a practical mapping method.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/cc.html">PROJ documentation: Central Cylindrical.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Central_cylindrical_projection">Wikipedia: Central cylindrical projection.</seealso>
internal class CentralCylindricalProjection : MapProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CentralCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CentralCylindricalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CentralCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CentralCylindricalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Central_Cylindrical";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new CentralCylindricalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        if (Math.Abs(Math.Abs(lat) - HalfPi) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        lon = this.SphericalRadius * lambda;
        lat = this.SphericalRadius * Math.Tan(lat);
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        x = Adjust_lon(this.centralMeridian + xx);
        y = Math.Atan(yy);
    }
}
