// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

/// <summary>
/// Specifies the direction of a coordinate transformation.
/// </summary>
public enum GieDirection
{
    /// <summary>
    /// Executes the forward projection direction.
    /// </summary>
    Forward = 0,

    /// <summary>
    /// Executes the inverse projection direction.
    /// </summary>
    Inverse = 1,
}
