// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

    /// <summary>
/// Implements the Adams World in a Square I projection (<c>adams_ws1</c>).
/// </summary>
/// <remarks>
/// Adams World in a Square I is the first full-world specialization of
/// <see cref="AdamsProjectionBase"/>. It applies Adams' conformal square construction to the
/// entire world with the World-in-a-Square I arrangement. Inverse projection is not
/// supported in this implementation.
/// </remarks>
internal sealed class AdamsWorldInSquare1Projection : AdamsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsWorldInSquare1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AdamsWorldInSquare1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsWorldInSquare1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AdamsWorldInSquare1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Adams_World_In_A_Square_I", AdamsMode.AdamsWs1)
    {
    }

    /// <inheritdoc />
    protected override bool HasInverseSupport => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.GetOrCreateInverse(() => new AdamsWorldInSquare1Projection(this.Parameters.ToProjectionParameter(), this));
    }
}
