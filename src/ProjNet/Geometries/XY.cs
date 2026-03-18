namespace ProjNet.Geometries
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A pair of X- and Y-ordinates, laid out in that order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XY : IEquatable<XY>
    {
        /// <summary>
        /// The x-ordinate value.
        /// </summary>
        public double X;

        /// <summary>
        /// The y-ordinate value.
        /// </summary>
        public double Y;

        /// <summary>
        /// Initializes a new instance of the <see cref="XY"/> struct.
        /// </summary>
        /// <param name="x">The value for <see cref="X"/>.</param>
        /// <param name="y">The value for <see cref="Y"/>.</param>
        public XY(double x, double y) =>
            (this.X, this.Y) = (x, y);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is XY other && this.Equals(other);

        /// <inheritdoc />
        public bool Equals(XY other) => (this.X, this.Y).Equals((other.X, other.Y));

        /// <inheritdoc />
        public override int GetHashCode() => (this.X, this.Y).GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"({this.X}, {this.Y})";
    }
}
