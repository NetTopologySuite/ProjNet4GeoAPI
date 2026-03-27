// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

using System.Collections.Generic;

/// <summary>
/// Internal provider contract for managed coordinate systems emitted as structured objects.
/// </summary>
internal interface IManagedCoordinateSystemProvider
{
    /// <summary>
    /// Gets coordinate system objects keyed by SRID.
    /// </summary>
    /// <returns>Coordinate system objects.</returns>
    IEnumerable<CoordinateSystemEntry> GetCoordinateSystems();
}
