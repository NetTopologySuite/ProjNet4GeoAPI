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
    public sealed class ManagedCoordinateSystemDefinitionProvider : ICoordinateSystemDefinitionProvider
    {
        /// <inheritdoc />
        public IEnumerable<KeyValuePair<int, string>> GetDefinitions()
        {
            var yieldedSrids = new HashSet<int>();
            yieldedSrids.Add(4326);
            yieldedSrids.Add(3857);
            yield return new KeyValuePair<int, string>(4326, GeographicCoordinateSystem.WGS84.WKT);
            yield return new KeyValuePair<int, string>(3857, ProjectedCoordinateSystem.WebMercator.WKT);

            bool yieldedAny = false;
            foreach (var definition in EpsgGeneratedCatalog.GetCoordinateSystemDefinitions())
            {
                yieldedAny = true;
                if (yieldedSrids.Contains(definition.Key))
                    continue;

                yieldedSrids.Add(definition.Key);
                yield return definition;
            }

            if (!yieldedAny)
            {
                yield break;
            }
        }
    }
}
