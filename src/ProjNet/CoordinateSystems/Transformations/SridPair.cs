// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

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
