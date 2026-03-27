using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Bound CRS element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2BoundCrs : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">CRS keyword (e.g. <c>BOUNDCRS</c>).</param>
        /// <param name="name">CRS name.</param>
        /// <param name="sourceCrs">Source CRS.</param>
        /// <param name="targetCrs">Target CRS.</param>
        /// <param name="transformation">Abridged transformation description.</param>
        public Wkt2BoundCrs(string keyword, string name, Wkt2CrsBase sourceCrs, Wkt2CrsBase targetCrs, Wkt2AbridgedTransformation transformation)
            : base(name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            SourceCrs = sourceCrs ?? throw new ArgumentNullException(nameof(sourceCrs));
            TargetCrs = targetCrs ?? throw new ArgumentNullException(nameof(targetCrs));
            Transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
        }

        /// <summary>
        /// Gets the WKT2 keyword.
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the source CRS.
        /// </summary>
        public Wkt2CrsBase SourceCrs { get; }

        /// <summary>
        /// Gets the target CRS.
        /// </summary>
        public Wkt2CrsBase TargetCrs { get; }

        /// <summary>
        /// Gets the abridged transformation.
        /// </summary>
        public Wkt2AbridgedTransformation Transformation { get; }

        /// <summary>
        /// Serializes the CRS back to WKT2.
        /// </summary>
        public override string ToWkt2String()
        {
            return IO.CoordinateSystems.CoordinateSystemWkt2Writer.Write(this);
        }
    }
}
