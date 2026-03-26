// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.Data;

using System.Collections.Generic;

/// <summary>
/// Provides managed coordinate system definitions used to initialize <see cref="CoordinateSystemServices"/>.
/// </summary>
public interface ICoordinateSystemDefinitionProvider
{
    /// <summary>
    /// Gets coordinate system definitions keyed by SRID.
    /// </summary>
    /// <returns>Coordinate system definitions.</returns>
    IEnumerable<KeyValuePair<int, string>> GetDefinitions();
}
