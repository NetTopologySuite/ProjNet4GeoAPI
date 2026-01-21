using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Parametric CRS element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2ParametricCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>PARAMETRICCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="datum">Parametric datum.</param>
        /// <param name="coordinateSystem">Coordinate system.</param>
        public Wkt2ParametricCrs(string keyword, string name, Wkt2ParametricDatum datum, Wkt2CoordinateSystem coordinateSystem)
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
        /// Gets the parametric datum.
        /// </summary>
        public Wkt2ParametricDatum Datum { get; }

        /// <summary>
        /// Gets the parametric CRS coordinate system.
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
