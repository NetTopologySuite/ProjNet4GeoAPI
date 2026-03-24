// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Represents the documented type.
/// </summary>
internal sealed class GridResourceResolverOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GridResourceResolverOptions"/> class.
    /// </summary>
    /// <param name="localDirectories">The localDirectories value.</param>
    /// <param name="cacheDirectory">The cacheDirectory value.</param>
    /// <param name="mode">The mode value.</param>
    internal GridResourceResolverOptions(IEnumerable<string> localDirectories, string cacheDirectory, GridResourceResolutionMode mode)
    {
        if (localDirectories is null)
        {
            throw new ArgumentNullException(nameof(localDirectories));
        }

        this.LocalDirectories = localDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .ToArray();
        this.CacheDirectory = string.IsNullOrWhiteSpace(cacheDirectory) ? null : Path.GetFullPath(cacheDirectory);
        this.Mode = mode;
    }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal string CacheDirectory { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal IReadOnlyList<string> LocalDirectories { get; }

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    internal GridResourceResolutionMode Mode { get; }
}
