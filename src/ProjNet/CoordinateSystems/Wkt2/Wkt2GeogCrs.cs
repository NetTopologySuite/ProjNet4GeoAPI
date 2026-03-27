using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Geographic CRS element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2GeogCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>GEOGCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="datum">Geodetic datum.</param>
        /// <param name="coordinateSystem">Coordinate system.</param>
        public Wkt2GeogCrs(string keyword, string name, Wkt2GeodeticDatum datum, Wkt2CoordinateSystem coordinateSystem)
            : base(name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            Datum = datum ?? throw new ArgumentNullException(nameof(datum));
            CoordinateSystem = coordinateSystem ?? throw new ArgumentNullException(nameof(coordinateSystem));
        }

        /// <summary>
        /// Gets the WKT2 keyword.
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the datum.
        /// </summary>
        public Wkt2GeodeticDatum Datum { get; }

        /// <summary>
        /// Gets or sets the prime meridian.
        /// </summary>
        public Wkt2PrimeMeridian PrimeMeridian { get; set; }

        /// <summary>
        /// Gets the coordinate system.
        /// </summary>
        public Wkt2CoordinateSystem CoordinateSystem { get; }

        /// <summary>
        /// Serializes the CRS back to WKT2.
        /// </summary>
        public override string ToWkt2String()
        {
            return IO.CoordinateSystems.CoordinateSystemWkt2Writer.Write(this);
        }
    }
}
