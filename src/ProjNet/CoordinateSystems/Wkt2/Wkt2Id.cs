using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Identifier element as used in WKT2 (e.g. <c>ID["EPSG",4326]</c>).
    /// </summary>
    [Serializable]
    public sealed class Wkt2Id
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="authority">Authority name.</param>
        /// <param name="code">Authority code.</param>
        public Wkt2Id(string authority, string code)
        {
            Authority = authority ?? throw new ArgumentNullException(nameof(authority));
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        /// <summary>
        /// Gets the authority name.
        /// </summary>
        public string Authority { get; }

        /// <summary>
        /// Gets the authority code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets or sets an optional URI.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets an optional version string.
        /// </summary>
        public string Version { get; set; }
    }
}
