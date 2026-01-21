using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Parameter element as used in WKT2 conversions/transformations.
    /// </summary>
    [Serializable]
    public sealed class Wkt2Parameter
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        public Wkt2Parameter(string name, double value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
