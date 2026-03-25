// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Holds the configuration options used by <see cref="GridResourceResolver"/>.
/// </summary>
internal sealed class GridResourceResolverOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GridResourceResolverOptions"/> class.
    /// </summary>
    /// <param name="localDirectories">Directories to search for grid files; blank or null entries are ignored.</param>
    /// <param name="cacheDirectory">Directory used to store network-fetched grid files; <see langword="null"/> or whitespace disables network caching.</param>
    /// <param name="mode">Resolution mode controlling whether network retrieval is attempted.</param>
    internal GridResourceResolverOptions(IEnumerable<string> localDirectories, string cacheDirectory, GridResourceResolutionMode mode)
    {
        ArgumentGuard.ThrowIfNull(localDirectories, nameof(localDirectories));

        this.LocalDirectories = localDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToArray();
        this.CacheDirectory = string.IsNullOrWhiteSpace(cacheDirectory) ? null : Path.GetFullPath(cacheDirectory);
        this.Mode = mode;
    }

    /// <summary>
    /// Gets the absolute path of the directory used to cache network-fetched grid files, or <see langword="null"/> when network caching is disabled.
    /// </summary>
    internal string CacheDirectory { get; }

    /// <summary>
    /// Gets the ordered list of absolute local directory paths searched during grid resolution.
    /// </summary>
    internal IReadOnlyList<string> LocalDirectories { get; }

    /// <summary>
    /// Gets the resolution mode that controls whether network retrieval is attempted after local search.
    /// </summary>
    internal GridResourceResolutionMode Mode { get; }
}
