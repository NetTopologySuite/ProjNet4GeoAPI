// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Represents a source/target SRID pair for dictionary lookups.
/// </summary>
internal readonly struct SridPair(int sourceSrid, int targetSrid) : IEquatable<SridPair>
{
    /// <summary>
    /// Gets the source SRID.
    /// </summary>
    internal int SourceSrid { get; } = sourceSrid;

    /// <summary>
    /// Gets the target SRID.
    /// </summary>
    internal int TargetSrid { get; } = targetSrid;

    /// <summary>
    /// Compares this pair with another SRID pair.
    /// </summary>
    /// <param name="other">The pair to compare.</param>
    /// <returns><see langword="true"/> when source and target SRIDs are equal.</returns>
    public bool Equals(SridPair other)
    {
        return this.SourceSrid == other.SourceSrid
            && this.TargetSrid == other.TargetSrid;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is SridPair other && this.Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (this.SourceSrid * 397) ^ this.TargetSrid;
        }
    }
}
