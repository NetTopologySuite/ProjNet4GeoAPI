// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Adams World in a Square II projection (<c>adams_ws2</c>).
/// </summary>
[Serializable]
internal sealed class AdamsWorldInSquareIIProjection : AdamsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsWorldInSquareIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AdamsWorldInSquareIIProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsWorldInSquareIIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AdamsWorldInSquareIIProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse, "Adams_World_In_A_Square_II", AdamsMode.AdamsWs2)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new AdamsWorldInSquareIIProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
