// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Adams World in a Square I projection (<c>adams_ws1</c>).
/// </summary>
[Serializable]
internal sealed class AdamsWorldInSquareIProjection : AdamsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsWorldInSquareIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AdamsWorldInSquareIProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsWorldInSquareIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AdamsWorldInSquareIProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Adams_World_In_A_Square_I", AdamsMode.AdamsWs1)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new AdamsWorldInSquareIProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
