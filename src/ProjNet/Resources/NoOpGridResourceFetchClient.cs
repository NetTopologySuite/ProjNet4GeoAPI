// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a fetch client that never downloads grid resources.
/// </summary>
public sealed class NoOpGridResourceFetchClient : IGridResourceFetchClient
{
    /// <inheritdoc />
    public bool TryFetch(string gridName, string targetFilePath) => false;

    /// <inheritdoc />
    public Task<bool> TryFetchAsync(string gridName, string targetFilePath, CancellationToken cancellationToken = default) => Task.FromResult(false);
}
