// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data;

using System.Collections.Generic;
using ProjNet.Data.Generated;

/// <summary>
/// Provides managed, runtime-independent defaults for core coordinate system definitions.
/// </summary>
/// <remarks>
/// This provider intentionally avoids runtime SQLite/native dependencies.
/// It is the baseline managed packaging implementation and can be replaced by a generated provider in later phases.
/// </remarks>
public sealed class ManagedCoordinateSystemDefinitionProvider : ICoordinateSystemDefinitionProvider, IManagedCoordinateSystemProvider
{
    /// <inheritdoc/>
    public IEnumerable<CoordinateSystemEntry> GetCoordinateSystems()
    {
        return GetManagedCoordinateSystems();
    }

    /// <summary>
    /// Enumerates all coordinate system definitions as WKT-encoded <see cref="CoordinateSystemDefinition"/> instances.
    /// </summary>
    /// <returns>A sequence of <see cref="CoordinateSystemDefinition"/> instances for all known coordinate systems.</returns>
    public IEnumerable<CoordinateSystemDefinition> GetDefinitions()
    {
        foreach (CoordinateSystemEntry entry in GetManagedCoordinateSystems())
        {
            yield return new CoordinateSystemDefinition(entry.Srid, entry.CoordinateSystem.WKT);
        }
    }

    private static IEnumerable<CoordinateSystemEntry> GetManagedCoordinateSystems()
    {
        var yieldedSrids = new HashSet<int>();
        foreach (CoordinateSystemEntry entry in EpsgCoordinateSystemFactory.GetCoordinateSystems())
        {
            if (!yieldedSrids.Add(entry.Srid))
            {
                continue;
            }

            yield return entry;
        }
    }
}
