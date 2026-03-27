// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable

namespace ProjNet.Resources;

/// <summary>
/// Specifies how grid resources are resolved by <see cref="GridResourceResolver"/>.
/// </summary>
internal enum GridResourceResolutionMode
{
    /// <summary>
    /// Resolves grids only from locally available files.
    /// </summary>
    LocalOnly = 0,

    /// <summary>
    /// Resolves grids locally first, then falls back to network retrieval.
    /// </summary>
    LocalThenNetwork = 1,
}
