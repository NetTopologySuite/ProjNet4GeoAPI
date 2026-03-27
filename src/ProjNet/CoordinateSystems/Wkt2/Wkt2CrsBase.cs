using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Base class for WKT2 CRS model objects.
    /// </summary>
    [Serializable]
    public abstract class Wkt2CrsBase
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">CRS name.</param>
        protected Wkt2CrsBase(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the CRS name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the CRS identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }

        /// <summary>
        /// Gets or sets an optional remark.
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Gets the list of usage metadata for this CRS.
        /// </summary>
        public List<Wkt2Usage> Usages { get; } = new List<Wkt2Usage>();

        /// <summary>
        /// Serializes the model back to a WKT2 string.
        /// </summary>
        public abstract string ToWkt2String();
    }
}
