using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Represents a single coordinate operation used as a STEP in a concatenated operation.
    /// </summary>
    [Serializable]
    public sealed class Wkt2CoordinateOperation
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">The WKT2 keyword (CONVERSION, COORDINATEOPERATION, etc.).</param>
        /// <param name="name">The operation name.</param>
        public Wkt2CoordinateOperation(string keyword, string name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the WKT2 keyword (CONVERSION, COORDINATEOPERATION, etc.).
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets the operation parameters.
        /// </summary>
        public List<Wkt2Parameter> Parameters { get; } = new List<Wkt2Parameter>();

        /// <summary>
        /// Gets or sets the source CRS (for COORDINATEOPERATION).
        /// </summary>
        public Wkt2CrsBase SourceCrs { get; set; }

        /// <summary>
        /// Gets or sets the target CRS (for COORDINATEOPERATION).
        /// </summary>
        public Wkt2CrsBase TargetCrs { get; set; }

        /// <summary>
        /// Gets or sets the operation accuracy.
        /// </summary>
        public double? OperationAccuracy { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }

        /// <summary>
        /// Gets or sets a remark.
        /// </summary>
        public string Remark { get; set; }
    }
}
