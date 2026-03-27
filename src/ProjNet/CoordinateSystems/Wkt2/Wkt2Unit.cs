using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Unit element as used in WKT2 (e.g. <c>ANGLEUNIT</c>, <c>LENGTHUNIT</c>).
    /// </summary>
    [Serializable]
    public sealed class Wkt2Unit
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="keyword">Unit keyword (e.g. <c>ANGLEUNIT</c>).</param>
        /// <param name="name">Unit name.</param>
        /// <param name="conversionFactor">Conversion factor to the SI base unit.</param>
        public Wkt2Unit(string keyword, string name, double conversionFactor)
        {
            Keyword = keyword ?? throw new ArgumentNullException(nameof(keyword));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ConversionFactor = conversionFactor;
        }

        /// <summary>
        /// Gets the unit keyword.
        /// </summary>
        public string Keyword { get; }

        /// <summary>
        /// Gets the unit name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the conversion factor.
        /// </summary>
        public double ConversionFactor { get; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
