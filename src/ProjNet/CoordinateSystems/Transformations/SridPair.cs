// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Represents a source/target SRID pair for dictionary lookups.
/// </summary>
internal readonly struct SridPair : IEquatable<SridPair>
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
