// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.Resources;

/// <summary>
/// Represents a fetch client that never downloads grid resources.
/// </summary>
internal sealed class NoOpGridResourceFetchClient : IGridResourceFetchClient
{
    /// <inheritdoc />
    public bool TryFetch(string gridName, string targetFilePath) => false;
}
