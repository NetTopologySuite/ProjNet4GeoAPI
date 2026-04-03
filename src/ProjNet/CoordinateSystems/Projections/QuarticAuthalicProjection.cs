// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Quartic Authalic projection (<c>qua_aut</c>).
/// </summary>
/// <remarks>
/// Quartic Authalic is a spherical equal-area member of the STS family implemented by
/// <see cref="StsProjectionBase"/>. This specialization fixes the family constants to
/// <c>p = 2</c> and <c>q = 2</c> with sine-mode scaling, yielding the quartic-authalic
/// relations commonly listed in Snyder's survey of pseudocylindrical projections.
/// </remarks>
internal sealed class QuarticAuthalicProjection : StsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuarticAuthalicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public QuarticAuthalicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuarticAuthalicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public QuarticAuthalicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Quartic_Authalic", 2d, 2d, false)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new QuarticAuthalicProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }
}
