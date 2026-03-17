// <copyright file="SridPair.cs" company="NetTopologySuite - Team">
// Copyright (c) NetTopologySuite - Team. All rights reserved.
// </copyright>

namespace ProjNet.CoordinateSystems.Transformations
{
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

        /// <inheritdoc />
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
}
