namespace ProjNet.Data
{
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems;

    /// <summary>
    /// Internal provider contract for managed coordinate systems emitted as structured objects.
    /// </summary>
    internal interface IManagedCoordinateSystemProvider
    {
        /// <summary>
        /// Gets coordinate system objects keyed by SRID.
        /// </summary>
        /// <returns>Coordinate system objects.</returns>
        IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems();
    }
}
