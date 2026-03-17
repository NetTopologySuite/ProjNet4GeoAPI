using System.Collections.Generic;
using ProjNet.CoordinateSystems;

namespace ProjNet.Data
{
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
            yield return new KeyValuePair<int, string>(4326, GeographicCoordinateSystem.WGS84.WKT);
            yield return new KeyValuePair<int, string>(3857, ProjectedCoordinateSystem.WebMercator.WKT);
        }
    }
}
