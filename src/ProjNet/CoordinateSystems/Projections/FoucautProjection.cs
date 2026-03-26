// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Foucaut projection (<c>fouc</c>).
/// </summary>
[Serializable]
internal sealed class FoucautProjection : StsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FoucautProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public FoucautProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FoucautProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public FoucautProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse, "Foucaut", 2d, 2d, true)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new FoucautProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
