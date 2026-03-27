using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Coordinate system element as used in WKT2 (<c>CS</c> plus axes and units).
    /// </summary>
    [Serializable]
    public sealed class Wkt2CoordinateSystem
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="type">Coordinate system type (e.g. <c>ellipsoidal</c>, <c>cartesian</c>).</param>
        /// <param name="dimension">Number of dimensions.</param>
        public Wkt2CoordinateSystem(string type, int dimension)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Dimension = dimension;
        }

        /// <summary>
        /// Gets the coordinate system type.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the dimension.
        /// </summary>
        public int Dimension { get; }

        /// <summary>
        /// Gets the axes.
        /// </summary>
        public List<Wkt2Axis> Axes { get; } = new List<Wkt2Axis>();

        /// <summary>
        /// Gets or sets the coordinate system unit.
        /// </summary>
        public Wkt2Unit Unit { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
