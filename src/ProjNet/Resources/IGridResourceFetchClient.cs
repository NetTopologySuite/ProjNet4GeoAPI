// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

/// <summary>
/// Defines a client that can fetch a named grid resource and write it to a local file.
/// </summary>
internal interface IGridResourceFetchClient
{
    /// <summary>
    /// Attempts to fetch the specified grid resource and save it to <paramref name="targetFilePath"/>.
    /// </summary>
    /// <param name="gridName">The logical name or remote identifier of the grid resource.</param>
    /// <param name="targetFilePath">The local file path where the fetched grid should be written.</param>
    /// <returns><see langword="true"/> when the resource was successfully fetched and written; otherwise <see langword="false"/>.</returns>
    bool TryFetch(string gridName, string targetFilePath);
}
