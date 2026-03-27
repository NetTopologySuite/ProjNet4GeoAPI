// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.Geometries;

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
    public override bool Equals(object? obj) => obj is XY other && this.Equals(other);

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>The computed value.</returns>
    public bool Equals(XY other) => (this.X, this.Y).Equals((other.X, other.Y));

    /// <inheritdoc />
    public override int GetHashCode() => (this.X, this.Y).GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"({this.X}, {this.Y})";
}
