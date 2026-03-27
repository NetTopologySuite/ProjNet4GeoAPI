using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Represents a WKT2 CONCATENATEDOPERATION element.
    /// </summary>
    [Serializable]
    public sealed class Wkt2ConcatenatedOperation : Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">The operation name.</param>
        public Wkt2ConcatenatedOperation(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Gets or sets the version text.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the source CRS.
        /// </summary>
        public Wkt2CrsBase SourceCrs { get; set; }

        /// <summary>
        /// Gets or sets the target CRS.
        /// </summary>
        public Wkt2CrsBase TargetCrs { get; set; }

        /// <summary>
        /// Gets the ordered list of operation steps.
        /// </summary>
        public List<Wkt2CoordinateOperation> Steps { get; } = new List<Wkt2CoordinateOperation>();

        /// <summary>
        /// Gets or sets the operation accuracy.
        /// </summary>
        public double? OperationAccuracy { get; set; }

        /// <inheritdoc />
        public override string ToWkt2String()
        {
            return IO.CoordinateSystems.CoordinateSystemWkt2Writer.Write(this);
        }
    }
}
