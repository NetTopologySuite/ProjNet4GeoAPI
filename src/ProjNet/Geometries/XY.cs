// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

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

        /// <summary>
        /// Performs the documented operation.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns>The computed value.</returns>
        public bool Equals(XY other) => (this.X, this.Y).Equals((other.X, other.Y));

        /// <summary>
        /// Compares two <see cref="XY"/> values for equality.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> when both values are equal; otherwise <see langword="false"/>.</returns>
        public static bool operator ==(XY left, XY right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="XY"/> values for inequality.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> when values differ; otherwise <see langword="false"/>.</returns>
        public static bool operator !=(XY left, XY right) => !left.Equals(right);

        /// <inheritdoc />
        public override int GetHashCode() => (this.X, this.Y).GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"({this.X}, {this.Y})";
    }
}
