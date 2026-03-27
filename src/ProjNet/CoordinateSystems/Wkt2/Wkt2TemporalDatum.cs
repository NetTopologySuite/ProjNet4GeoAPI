using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Temporal datum element as used in WKT2 (TDATUM / TIMEDATUM).
    /// </summary>
    [Serializable]
    public sealed class Wkt2TemporalDatum
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">Datum keyword (e.g. <c>TDATUM</c> or <c>TIMEDATUM</c>).</param>
        /// <param name="name">Datum name.</param>
        public Wkt2TemporalDatum(string keyword, string name)
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
        /// Gets or sets the calendar type.
        /// </summary>
        public string Calendar { get; set; }

        /// <summary>
        /// Gets or sets the time origin.
        /// </summary>
        public string TimeOrigin { get; set; }

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
