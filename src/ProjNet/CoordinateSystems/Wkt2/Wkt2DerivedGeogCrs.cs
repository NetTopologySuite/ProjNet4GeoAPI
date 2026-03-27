using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Derived geographic CRS element as used in WKT2 (DERIVEDGEOGCRS).
    /// </summary>
    [Serializable]
    public sealed class Wkt2DerivedGeogCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>DERIVEDGEOGCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="baseCrs">Base geographic CRS.</param>
        /// <param name="derivingConversion">Deriving conversion.</param>
        /// <param name="coordinateSystem">Coordinate system.</param>
        public Wkt2DerivedGeogCrs(string keyword, string name, Wkt2GeogCrs baseCrs, Wkt2Conversion derivingConversion, Wkt2CoordinateSystem coordinateSystem)
            : base(name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            BaseCrs = baseCrs ?? throw new ArgumentNullException(nameof(baseCrs));
            DerivingConversion = derivingConversion ?? throw new ArgumentNullException(nameof(derivingConversion));
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
        /// Gets the deriving conversion.
        /// </summary>
        public Wkt2Conversion DerivingConversion { get; }

        /// <summary>
        /// Gets the derived geographic CRS coordinate system.
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
