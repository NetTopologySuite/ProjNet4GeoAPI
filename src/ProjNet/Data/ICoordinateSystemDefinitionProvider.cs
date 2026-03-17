using System.Collections.Generic;

namespace ProjNet.Data
{
    /// <summary>
    /// Provides managed coordinate system definitions used to initialize <see cref="CoordinateSystemServices"/>.
    /// </summary>
    public interface ICoordinateSystemDefinitionProvider
    {
        /// <summary>
        /// Gets coordinate system definitions keyed by SRID.
        /// </summary>
        /// <returns>Coordinate system definitions.</returns>
        IEnumerable<KeyValuePair<int, string>> GetDefinitions();
    }
}
