// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Implements the spherical Apian Globular I projection (<c>apian</c>).
/// </summary>
/// <remarks>
/// <para>Apian Globular I is a historical sixteenth-century member of the
/// <see cref="BaconProjection"/> family. It reuses the shared globular construction
/// without Bacon latitude scaling and without the Ortelius wide-longitude branch.</para>
/// <para>This derived class represents Petrus Apianus's 1524 first globular world
/// projection and delegates the shared circular-arc meridian construction to
/// <see cref="BaconProjection"/> with the Bacon and Ortelius special cases disabled.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/apian.html">PROJ documentation: Apian Globular I.</seealso>
internal sealed class ApianProjection : BaconProjection
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
