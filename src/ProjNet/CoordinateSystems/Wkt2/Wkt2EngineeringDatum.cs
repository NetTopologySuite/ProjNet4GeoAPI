using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Engineering datum element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2EngineeringDatum
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">Datum keyword (e.g. <c>EDATUM</c>).</param>
        /// <param name="name">Datum name.</param>
        public Wkt2EngineeringDatum(string keyword, string name)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            Name = name ?? throw new ArgumentNullException(nameof(name));
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
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }

        /// <summary>
        /// Gets or sets an optional remark.
        /// </summary>
        public string Remark { get; set; }
    }
}
