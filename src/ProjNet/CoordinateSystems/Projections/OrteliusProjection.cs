// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Implements the spherical Ortelius Oval projection (<c>ortel</c>).
/// </summary>
[Serializable]
internal class OrteliusProjection : BaconProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrteliusProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public OrteliusProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrteliusProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public OrteliusProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse, bacon: false, ortelius: true, "Ortelius_Oval")
    {
    }
}
