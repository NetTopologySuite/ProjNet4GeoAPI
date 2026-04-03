// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Guyou projection (<c>guyou</c>).
/// </summary>
/// <remarks>
/// <para>Guyou is the historical conformal square projection developed by Émile Guyou
/// in 1887. It is represented here as a specialized mode of
/// <see cref="AdamsProjectionBase"/>, which supplies the shared Adams-family
/// hemisphere-in-a-square construction and the Guyou-specific orientation.</para>
/// <para>The spherical formulation was independently verified against the historical
/// Guyou hemisphere-in-a-square description and the PROJ <c>guyou</c> projection
/// documentation. This implementation delegates all mathematical work to
/// <see cref="AdamsProjectionBase"/> with <c>AdamsMode.Guyou</c>, matching the shared
/// conformal square construction used for the Guyou and related Adams-family variants.
/// Inverse projection is not supported in this implementation.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/guyou.html">PROJ documentation: Guyou.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Guyou_projection">Wikipedia: Guyou hemisphere-in-a-square projection.</seealso>
internal sealed class GuyouProjection : AdamsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuyouProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GuyouProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GuyouProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GuyouProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Guyou", AdamsMode.Guyou)
    {
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new GuyouProjection(this.Parameters.ToProjectionParameter(), this));
    }
}
