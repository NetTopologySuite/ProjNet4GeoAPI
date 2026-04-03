// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Vitkovsky I projection (<c>vitk1</c>).
/// </summary>
/// <remarks>
/// Vitkovsky I is a simple spherical conic specialization of
/// <see cref="SimpleConicProjectionBase"/>. Its numerical behavior follows the shared
/// simple-conic equations with the Vitkovsky-specific tangent-derived cone constant.
/// </remarks>
internal sealed class Vitkovsky1Projection : SimpleConicProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Vitkovsky1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Vitkovsky1Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vitkovsky1Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Vitkovsky1Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, SimpleConicType.Vitkovsky1, "Vitkovsky_I")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new Vitkovsky1Projection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }
}
