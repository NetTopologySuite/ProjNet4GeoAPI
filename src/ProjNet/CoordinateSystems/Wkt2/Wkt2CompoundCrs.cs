using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Compound CRS element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2CompoundCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>COMPOUNDCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="components">Component CRS list.</param>
        public Wkt2CompoundCrs(string keyword, string name, IEnumerable<Wkt2CrsBase> components)
            : base(name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            Components = new List<Wkt2CrsBase>(components ?? throw new ArgumentNullException(nameof(components)));
        }

        /// <summary>
        /// Gets the WKT2 keyword.
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the component CRSs.
        /// </summary>
        public List<Wkt2CrsBase> Components { get; }

        /// <summary>
        /// Serializes the CRS back to WKT2.
        /// </summary>
        public override string ToWkt2String()
        {
            return IO.CoordinateSystems.CoordinateSystemWkt2Writer.Write(this);
        }
    }
}
