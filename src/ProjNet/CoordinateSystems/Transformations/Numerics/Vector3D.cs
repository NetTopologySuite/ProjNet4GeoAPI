// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations.Numerics;

using System;

/// <summary>
/// Represents an immutable 3D vector with <see cref="double"/> precision.
/// </summary>
[Serializable]
internal readonly struct Vector3D(double x, double y, double z)
{
    /// <summary>
    /// Gets the X component.
    /// </summary>
    internal double X { get; } = x;

    /// <summary>
    /// Gets the Y component.
    /// </summary>
    internal double Y { get; } = y;

    /// <summary>
    /// Gets the Z component.
    /// </summary>
    internal double Z { get; } = z;

    public static Vector3D operator +(Vector3D left, Vector3D right)
    {
        return new Vector3D(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vector3D operator -(Vector3D left, Vector3D right)
    {
        return new Vector3D(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Vector3D operator -(Vector3D value)
    {
        return new Vector3D(-value.X, -value.Y, -value.Z);
    }

    public static Vector3D operator *(Vector3D value, double scalar)
    {
        return new Vector3D(value.X * scalar, value.Y * scalar, value.Z * scalar);
    }

    public static Vector3D operator *(double scalar, Vector3D value)
    {
        return value * scalar;
    }

    public static Vector3D operator /(Vector3D value, double scalar)
    {
        return new Vector3D(value.X / scalar, value.Y / scalar, value.Z / scalar);
    }
}
