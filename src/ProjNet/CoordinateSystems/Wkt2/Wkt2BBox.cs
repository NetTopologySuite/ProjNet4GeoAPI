using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Represents a WKT2 bounding box with south, west, north, east bounds.
    /// </summary>
    [Serializable]
    public sealed class Wkt2BBox
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Wkt2BBox"/> class.
        /// </summary>
        /// <param name="south">The southern latitude bound.</param>
        /// <param name="west">The western longitude bound.</param>
        /// <param name="north">The northern latitude bound.</param>
        /// <param name="east">The eastern longitude bound.</param>
        public Wkt2BBox(double south, double west, double north, double east)
        {
            South = south;
            West = west;
            North = north;
            East = east;
        }

        /// <summary>
        /// Gets the southern latitude bound.
        /// </summary>
        public double South { get; }

        /// <summary>
        /// Gets the western longitude bound.
        /// </summary>
        public double West { get; }

        /// <summary>
        /// Gets the northern latitude bound.
        /// </summary>
        public double North { get; }

        /// <summary>
        /// Gets the eastern longitude bound.
        /// </summary>
        public double East { get; }
    }
}
