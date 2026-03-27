// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Implements the spherical Apian Globular I projection (<c>apian</c>).
/// </summary>
[Serializable]
internal class ApianProjection : BaconProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApianProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public ApianProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApianProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public ApianProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, bacon: false, ortelius: false, "Apian_Globular_I")
    {
    }
}
