using System;
using System.Runtime.InteropServices;

namespace ProjNet.Geometries
{
    /// <summary>
    /// A tuple of X-, Y- and Z-ordinate values, laid out in that order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XYZ : IEquatable<XYZ>
    {
        /// <summary>
        /// The X-ordinate value
        /// </summary>
        public double X;

        /// <summary>
        /// The Y-ordinate value
        /// </summary>
        public double Y;

        /// <summary>
        /// The Z-ordinate value
        /// </summary>
        public double Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZ"/> struct.
        /// </summary>
        /// <param name="x">The value for <see cref="X"/>.</param>
        /// <param name="y">The value for <see cref="Y"/>.</param>
        /// <param name="z">The value for <see cref="Z"/>.</param>
        public XYZ(double x, double y, double z) =>
            (X, Y, Z) = (x, y, z);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is XYZ other && Equals(other);

        /// <inheritdoc />
        public bool Equals(XYZ other) => (X, Y, Z).Equals((other.X, other.Y, other.Z));

        /// <inheritdoc />
        public override int GetHashCode() => (X, Y, Z).GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
