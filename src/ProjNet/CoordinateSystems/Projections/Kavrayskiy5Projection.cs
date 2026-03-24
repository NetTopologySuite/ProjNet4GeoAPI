// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Kavrayskiy V projection (<c>kav5</c>).
/// </summary>
[Serializable]
internal sealed class Kavrayskiy5Projection : StsProjectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Kavrayskiy5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Kavrayskiy5Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Kavrayskiy5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Kavrayskiy5Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(parameters, inverse, "Kavrayskiy_V", 1.50488d, 1.35439d, false)
    {
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new Kavrayskiy5Projection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }
}
