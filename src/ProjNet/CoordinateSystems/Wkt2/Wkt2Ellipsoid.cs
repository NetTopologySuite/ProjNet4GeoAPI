using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Ellipsoid element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2Ellipsoid
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">Ellipsoid name.</param>
        /// <param name="semiMajorAxis">Semi-major axis length.</param>
        /// <param name="inverseFlattening">Inverse flattening.</param>
        public Wkt2Ellipsoid(string name, double semiMajorAxis, double inverseFlattening)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SemiMajorAxis = semiMajorAxis;
            InverseFlattening = inverseFlattening;
        }

        /// <summary>
        /// Gets the ellipsoid name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the semi-major axis.
        /// </summary>
        public double SemiMajorAxis { get; }

        /// <summary>
        /// Gets the inverse flattening.
        /// </summary>
        public double InverseFlattening { get; }

        /// <summary>
        /// Gets or sets an optional length unit.
        /// </summary>
        public Wkt2Unit LengthUnit { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
