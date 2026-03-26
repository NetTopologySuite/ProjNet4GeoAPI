// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.Data;

/// <summary>
/// Defines the operation kind represented by a catalog entry.
/// </summary>
internal enum CoordinateOperationKind : byte
{
    /// <summary>
    /// A direct transformation between source and target coordinate systems.
    /// </summary>
    Transformation = 0,

    /// <summary>
    /// A chained operation composed from multiple individual operations.
    /// </summary>
    ConcatenatedOperation = 1,

    /// <summary>
    /// A time-dependent point motion operation.
    /// </summary>
    PointMotionOperation = 2,
}
