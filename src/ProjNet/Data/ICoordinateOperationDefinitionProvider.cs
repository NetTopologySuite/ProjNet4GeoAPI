// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

using System.Collections.Generic;

/// <summary>
/// Provides coordinate operation definitions from a backing catalog.
/// </summary>
internal interface ICoordinateOperationDefinitionProvider
{
    /// <summary>
    /// Gets the coordinate operation definitions.
    /// </summary>
    /// <returns>A sequence of coordinate operation definitions.</returns>
    IEnumerable<CoordinateOperationDefinition> GetDefinitions();
}
