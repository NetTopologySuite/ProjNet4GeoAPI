// <copyright file="ManagedCoordinateSystemDefinitionProvider.cs" company="NetTopologySuite - Team">
// Copyright (c) NetTopologySuite - Team. All rights reserved.
// </copyright>

namespace ProjNet.Data
{
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems;
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
        public IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems()
        {
            return GetManagedCoordinateSystems();
        }

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <returns>The computed value.</returns>
        public IEnumerable<KeyValuePair<int, string>> GetDefinitions()
        {
            foreach (var coordinateSystem in GetManagedCoordinateSystems())
            {
                yield return new KeyValuePair<int, string>(coordinateSystem.Key, coordinateSystem.Value.WKT);
            }
        }

        private static IEnumerable<KeyValuePair<int, CoordinateSystem>> GetManagedCoordinateSystems()
        {
            var yieldedSrids = new HashSet<int>();
            foreach (var coordinateSystem in EpsgCoordinateSystemFactory.GetCoordinateSystems())
            {
                if (!yieldedSrids.Add(coordinateSystem.Key))
                {
                    continue;
                }

                yield return coordinateSystem;
            }
        }
    }
}
