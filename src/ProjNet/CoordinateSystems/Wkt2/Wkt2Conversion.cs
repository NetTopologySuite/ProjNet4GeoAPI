using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Conversion element as used in WKT2 projected CRSs.
    /// </summary>
    [Serializable]
    public sealed class Wkt2Conversion
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">Conversion name.</param>
        /// <param name="methodName">Method name.</param>
        public Wkt2Conversion(string name, string methodName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }

        /// <summary>
        /// Gets the conversion name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the conversion method name.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets the conversion parameters.
        /// </summary>
        public List<Wkt2Parameter> Parameters { get; } = new List<Wkt2Parameter>();

        /// <summary>
        /// Gets or sets the conversion identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }

        /// <summary>
        /// Gets or sets an optional remark.
        /// </summary>
        public string Remark { get; set; }
    }
}
