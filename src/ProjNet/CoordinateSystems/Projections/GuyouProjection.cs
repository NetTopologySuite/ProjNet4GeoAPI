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
/// Guyou is the historical conformal square projection derived from
/// <see cref="AdamsProjectionBase"/>. This implementation uses the shared Adams-family
/// square construction with the Guyou-specific domain and orientation. Inverse
/// projection is not supported in this implementation.
/// </remarks>
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
    public override MathTransform Inverse()
    {
        this.inverse ??= new GuyouProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }
}
