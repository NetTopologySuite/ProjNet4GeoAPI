// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the Adams Hemisphere in a Square projection (<c>adams_hemi</c>).
/// </summary>
[Serializable]
internal sealed class AdamsHemisphereInSquareProjection : AdamsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsHemisphereInSquareProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AdamsHemisphereInSquareProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdamsHemisphereInSquareProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AdamsHemisphereInSquareProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, "Adams_Hemisphere_In_A_Square", AdamsMode.AdamsHemi)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new AdamsHemisphereInSquareProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
