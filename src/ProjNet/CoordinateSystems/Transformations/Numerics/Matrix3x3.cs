// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Represents an immutable 3×3 matrix with <see cref="double"/> precision.
/// </summary>
[Serializable]
internal readonly record struct Matrix3x3(
    double M00,
    double M01,
    double M02,
    double M10,
    double M11,
    double M12,
    double M20,
    double M21,
    double M22)
{
    internal static Matrix3x3 Identity { get; } = new Matrix3x3(
        1d,
        0d,
        0d,
        0d,
        1d,
        0d,
        0d,
        0d,
        1d);

    internal static Matrix3x3 Zero { get; } = new Matrix3x3(
        0d,
        0d,
        0d,
        0d,
        0d,
        0d,
        0d,
        0d,
        0d);

    internal bool IsIdentity => this == Identity;

    internal bool IsZero => this == Zero;

    internal Matrix3x3 Transpose()
    {
        return new Matrix3x3(
            this.M00,
            this.M10,
            this.M20,
            this.M01,
            this.M11,
            this.M21,
            this.M02,
            this.M12,
            this.M22);
    }

    public static Matrix3x3 operator *(Matrix3x3 left, Matrix3x3 right)
    {
        return new Matrix3x3(
            (left.M00 * right.M00) + (left.M01 * right.M10) + (left.M02 * right.M20),
            (left.M00 * right.M01) + (left.M01 * right.M11) + (left.M02 * right.M21),
            (left.M00 * right.M02) + (left.M01 * right.M12) + (left.M02 * right.M22),
            (left.M10 * right.M00) + (left.M11 * right.M10) + (left.M12 * right.M20),
            (left.M10 * right.M01) + (left.M11 * right.M11) + (left.M12 * right.M21),
            (left.M10 * right.M02) + (left.M11 * right.M12) + (left.M12 * right.M22),
            (left.M20 * right.M00) + (left.M21 * right.M10) + (left.M22 * right.M20),
            (left.M20 * right.M01) + (left.M21 * right.M11) + (left.M22 * right.M21),
            (left.M20 * right.M02) + (left.M21 * right.M12) + (left.M22 * right.M22));
    }

    public static Vector3D operator *(Matrix3x3 matrix, Vector3D vector)
    {
        return new Vector3D(
            (matrix.M00 * vector.X) + (matrix.M01 * vector.Y) + (matrix.M02 * vector.Z),
            (matrix.M10 * vector.X) + (matrix.M11 * vector.Y) + (matrix.M12 * vector.Z),
            (matrix.M20 * vector.X) + (matrix.M21 * vector.Y) + (matrix.M22 * vector.Z));
    }
}
