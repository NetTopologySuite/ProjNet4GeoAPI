// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Euler projection (<c>euler</c>).
/// </summary>
/// <remarks>
/// Euler is a simple spherical conic specialization of <see cref="SimpleConicProjectionBase"/>.
/// Its numerical behavior is fully determined by the shared simple-conic equations together
/// with the Euler-specific cone constant and reference-radius terms.
/// </remarks>
internal sealed class EulerProjection : SimpleConicProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EulerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public EulerProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EulerProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public EulerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, SimpleConicType.Euler, "Euler")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new EulerProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }
}
