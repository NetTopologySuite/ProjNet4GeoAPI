// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents a source/target SRID pair for dictionary lookups.
/// </summary>
internal readonly record struct SridPair
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SridPair"/> struct.
    /// </summary>
    /// <param name="sourceSrid">The source SRID.</param>
    /// <param name="targetSrid">The target SRID.</param>
    internal SridPair(int sourceSrid, int targetSrid)
    {
        this.SourceSrid = sourceSrid;
        this.TargetSrid = targetSrid;
    }

    /// <summary>
    /// Gets the source SRID.
    /// </summary>
    internal int SourceSrid { get; }

    /// <summary>
    /// Gets the target SRID.
    /// </summary>
    internal int TargetSrid { get; }
}

