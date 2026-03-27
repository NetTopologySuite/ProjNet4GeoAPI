using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Projected CRS element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2ProjCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>PROJCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="baseCrs">Base geographic CRS.</param>
        /// <param name="conversion">Conversion.</param>
        /// <param name="coordinateSystem">Coordinate system.</param>
        public Wkt2ProjCrs(string keyword, string name, Wkt2GeogCrs baseCrs, Wkt2Conversion conversion, Wkt2CoordinateSystem coordinateSystem)
            : base(name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            BaseCrs = baseCrs ?? throw new ArgumentNullException(nameof(baseCrs));
            Conversion = conversion ?? throw new ArgumentNullException(nameof(conversion));
            CoordinateSystem = coordinateSystem ?? throw new ArgumentNullException(nameof(coordinateSystem));
        }

        /// <summary>
        /// Gets the WKT2 keyword.
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the base geographic CRS.
        /// </summary>
        public Wkt2GeogCrs BaseCrs { get; }

        /// <summary>
        /// Gets the defining conversion (map projection).
        /// </summary>
        public Wkt2Conversion Conversion { get; }

        /// <summary>
        /// Gets the projected CRS coordinate system.
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
