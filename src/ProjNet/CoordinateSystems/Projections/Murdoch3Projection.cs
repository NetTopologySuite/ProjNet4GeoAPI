// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Murdoch III projection (<c>murd3</c>).
/// </summary>
[Serializable]
internal sealed class Murdoch3Projection : SimpleConicProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Murdoch3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Murdoch3Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Murdoch3Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Murdoch3Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse, SimpleConicType.Murdoch3, "Murdoch_III")
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Murdoch3Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}

