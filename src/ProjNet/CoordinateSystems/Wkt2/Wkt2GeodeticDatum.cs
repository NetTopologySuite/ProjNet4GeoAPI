using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Geodetic datum element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2GeodeticDatum
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">Datum keyword (e.g. <c>DATUM</c> or <c>TRF</c>).</param>
        /// <param name="name">Datum name.</param>
        /// <param name="ellipsoid">Associated ellipsoid.</param>
        public Wkt2GeodeticDatum(string keyword, string name, Wkt2Ellipsoid ellipsoid)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Ellipsoid = ellipsoid ?? throw new ArgumentNullException(nameof(ellipsoid));
        }

        /// <summary>
        /// Gets the datum keyword.
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the datum name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ellipsoid.
        /// </summary>
        public Wkt2Ellipsoid Ellipsoid { get; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
