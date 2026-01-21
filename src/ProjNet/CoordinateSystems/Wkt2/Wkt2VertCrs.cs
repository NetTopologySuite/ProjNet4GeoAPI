using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Vertical CRS element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2VertCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>VERTCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="datum">Vertical datum.</param>
        /// <param name="coordinateSystem">Coordinate system.</param>
        public Wkt2VertCrs(string keyword, string name, Wkt2VerticalDatum datum, Wkt2CoordinateSystem coordinateSystem)
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
        /// Gets the vertical datum.
        /// </summary>
        public Wkt2VerticalDatum Datum { get; }

        /// <summary>
        /// Gets the vertical CRS coordinate system.
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
