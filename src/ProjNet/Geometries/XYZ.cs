namespace ProjNet.Geometries
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A tuple of X-, Y- and Z-ordinate values, laid out in that order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XYZ : IEquatable<XYZ>
    {
        /// <summary>
        /// The X-ordinate value.
        /// </summary>
        public double X;

        /// <summary>
        /// The Y-ordinate value.
        /// </summary>
        public double Y;

        /// <summary>
        /// The Z-ordinate value.
        /// </summary>
        public double Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZ"/> struct.
        /// </summary>
        /// <param name="x">The value for <see cref="X"/>.</param>
        /// <param name="y">The value for <see cref="Y"/>.</param>
        /// <param name="z">The value for <see cref="Z"/>.</param>
        public XYZ(double x, double y, double z) =>
            (this.X, this.Y, this.Z) = (x, y, z);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is XYZ other && this.Equals(other);

        /// <inheritdoc />
        public bool Equals(XYZ other) => (this.X, this.Y, this.Z).Equals((other.X, other.Y, other.Z));

        /// <inheritdoc />
        public override int GetHashCode() => (this.X, this.Y, this.Z).GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"({this.X}, {this.Y}, {this.Z})";
    }
}
