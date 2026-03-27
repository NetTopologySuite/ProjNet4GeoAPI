// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Peirce quincuncial projection (<c>peirce_q</c>).
/// </summary>
[Serializable]
internal sealed class PeirceQuincuncialProjection : AdamsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeirceQuincuncialProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PeirceQuincuncialProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PeirceQuincuncialProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PeirceQuincuncialProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Peirce_Quincuncial", AdamsMode.PeirceQ)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new PeirceQuincuncialProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
