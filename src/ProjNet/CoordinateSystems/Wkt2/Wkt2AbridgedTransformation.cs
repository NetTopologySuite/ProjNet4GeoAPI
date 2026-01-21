using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Abridged transformation as used in WKT2 <c>BOUNDCRS</c>.
    /// </summary>
    [Serializable]
    public sealed class Wkt2AbridgedTransformation
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">Transformation name.</param>
        /// <param name="methodName">Transformation method name.</param>
        public Wkt2AbridgedTransformation(string name, string methodName)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        }

        /// <summary>
        /// Gets the transformation name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the transformation method name.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets the transformation parameters.
        /// </summary>
        public List<Wkt2Parameter> Parameters { get; } = new List<Wkt2Parameter>();

        /// <summary>
        /// Gets or sets the transformation identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }

        /// <summary>
        /// Gets or sets an optional remark.
        /// </summary>
        public string Remark { get; set; }
    }
}
