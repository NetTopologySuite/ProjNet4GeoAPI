// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

/// <summary>
/// Represents the documented type.
/// </summary>
internal interface IGridResourceFetchClient
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridName">The gridName value.</param>
    /// <param name="targetFilePath">The targetFilePath value.</param>
    /// <returns>The computed value.</returns>
    bool TryFetch(string gridName, string targetFilePath);
}
