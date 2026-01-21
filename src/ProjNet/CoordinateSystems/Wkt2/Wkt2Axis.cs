using System;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Axis element as used in WKT2.
    /// </summary>
    [Serializable]
    public sealed class Wkt2Axis
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">Axis name.</param>
        /// <param name="direction">Axis direction (e.g. <c>north</c>, <c>east</c>).</param>
        public Wkt2Axis(string name, string direction)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
        }

        /// <summary>
        /// Gets the axis name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the axis direction.
        /// </summary>
        public string Direction { get; }

        /// <summary>
        /// Gets or sets the axis order.
        /// </summary>
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets an optional axis unit.
        /// </summary>
        public Wkt2Unit Unit { get; set; }

        /// <summary>
        /// Gets or sets an optional identifier.
        /// </summary>
        public Wkt2Id Id { get; set; }
    }
}
