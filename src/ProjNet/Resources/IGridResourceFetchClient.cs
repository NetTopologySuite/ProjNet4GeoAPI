// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a client that can fetch a named grid resource and write it to a local file.
/// </summary>
public interface IGridResourceFetchClient
{
    /// <summary>
    /// Attempts to fetch the specified grid resource and save it to <paramref name="targetFilePath"/>.
    /// </summary>
    /// <param name="gridName">The logical name or remote identifier of the grid resource.</param>
    /// <param name="targetFilePath">The local file path where the fetched grid should be written.</param>
    /// <returns><see langword="true"/> when the resource was successfully fetched and written; otherwise <see langword="false"/>.</returns>
    bool TryFetch(string gridName, string targetFilePath);

    /// <summary>
    /// Asynchronously attempts to fetch the specified grid resource and save it to <paramref name="targetFilePath"/>.
    /// </summary>
    /// <param name="gridName">The logical name or remote identifier of the grid resource.</param>
    /// <param name="targetFilePath">The local file path where the fetched grid should be written.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> when the resource was successfully fetched and written; otherwise <see langword="false"/>.</returns>
    Task<bool> TryFetchAsync(string gridName, string targetFilePath, CancellationToken cancellationToken = default);
}
