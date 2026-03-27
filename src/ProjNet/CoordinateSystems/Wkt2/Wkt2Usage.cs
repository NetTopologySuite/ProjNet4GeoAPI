using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Represents a WKT2 USAGE element containing scope, area, and bounding box metadata.
    /// </summary>
    [Serializable]
    public sealed class Wkt2Usage
    {
        /// <summary>
        /// Gets or sets the scope description.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the area description.
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// Gets or sets the bounding box.
        /// </summary>
        public Wkt2BBox BBox { get; set; }
    }
}
