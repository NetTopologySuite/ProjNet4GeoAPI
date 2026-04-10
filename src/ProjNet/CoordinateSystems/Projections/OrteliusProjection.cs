// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Implements the spherical Ortelius Oval projection (<c>ortel</c>).
/// </summary>
/// <remarks>
/// <para>Ortelius Oval is a historical sixteenth-century member of the
/// <see cref="BaconProjection"/> family. It uses the shared globular formulation
/// together with the Ortelius-specific branch for longitudes beyond ±90°.</para>
/// <para>This derived class represents Abraham Ortelius's oval world-map style,
/// popularized in <i>Theatrum Orbis Terrarum</i> from 1570 onward, and delegates the
/// shared front-hemisphere construction to <see cref="BaconProjection"/> while
/// enabling the wide-longitude branch that distinguishes the Ortelius variant from
/// the Apian form.</para>
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/projections/ortel.html">PROJ documentation: Ortelius Oval.</seealso>
/// <seealso href="https://en.wikipedia.org/wiki/Ortelius_oval_projection">Wikipedia: Ortelius oval projection.</seealso>
internal sealed class OrteliusProjection : BaconProjection
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
