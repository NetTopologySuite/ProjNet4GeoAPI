// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Identifies the simple conic projection variant.
/// </summary>
internal enum SimpleConicType
{
    /// <summary>
    /// Euler projection variant.
    /// </summary>
    Euler = 0,

    /// <summary>
    /// Murdoch projection variant I.
    /// </summary>
    Murdoch1 = 1,

    /// <summary>
    /// Murdoch projection variant II.
    /// </summary>
    Murdoch2 = 2,

    /// <summary>
    /// Murdoch projection variant III.
    /// </summary>
    Murdoch3 = 3,

    /// <summary>
    /// Tissot projection variant.
    /// </summary>
    Tissot = 4,

    /// <summary>
    /// Vitkovsky projection variant I.
    /// </summary>
    Vitkovsky1 = 5,
}
