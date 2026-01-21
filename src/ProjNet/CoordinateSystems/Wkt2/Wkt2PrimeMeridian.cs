using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Prime meridian element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2PrimeMeridian
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">Prime meridian name.</param>
        /// <param name="longitude">Longitude value.</param>
        public Wkt2PrimeMeridian(string name, double longitude)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Longitude = longitude;
        }

        /// <summary>
        /// Gets the prime meridian name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the longitude.
        /// </summary>
        public double Longitude { get; }

        /// <summary>
        /// Gets or sets an optional angle unit.
        /// </summary>
        public Wkt2Unit AngleUnit { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
