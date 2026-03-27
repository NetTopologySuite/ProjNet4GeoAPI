// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents a source/target SRID pair for dictionary lookups.
/// </summary>
/// <param name="sourceSrid">The source SRID.</param>
/// <param name="targetSrid">The target SRID.</param>
internal readonly record struct SridPair(int sourceSrid, int targetSrid)
{
    /// <summary>
    /// Gets the source SRID.
    /// </summary>
    internal int SourceSrid { get; } = sourceSrid;

    /// <summary>
    /// Gets the target SRID.
    /// </summary>
    internal int TargetSrid { get; } = targetSrid;
}
