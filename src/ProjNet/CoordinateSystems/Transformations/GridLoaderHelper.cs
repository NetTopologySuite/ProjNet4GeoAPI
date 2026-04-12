// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Shared helper for loading one or more grid files into read-only collections.
/// </summary>
internal static class GridLoaderHelper
{
    /// <summary>
    /// Loads zero or more grid items from each non-empty path in <paramref name="paths"/>.
    /// </summary>
    /// <typeparam name="T">Loaded grid item type.</typeparam>
    /// <param name="paths">Ordered grid paths to load.</param>
    /// <param name="argumentName">Argument name used when reporting failures.</param>
    /// <param name="emptyMessage">Exception message used when no grid items could be loaded.</param>
    /// <param name="loader">Per-path loader callback.</param>
    /// <param name="comparison">Optional comparison used to sort the loaded items before materializing the result.</param>
    /// <returns>A read-only collection containing the loaded grid items.</returns>
    internal static ReadOnlyCollection<T> LoadMulti<T>(
        IReadOnlyList<string> paths,
        string argumentName,
        string emptyMessage,
        Func<string, IEnumerable<T>> loader,
        Comparison<T>? comparison = null)
    {
        paths = ArgumentGuard.ThrowIfNull(paths, argumentName);
        argumentName = ArgumentGuard.ThrowIfNullOrWhiteSpace(argumentName, nameof(argumentName));
        emptyMessage = ArgumentGuard.ThrowIfNullOrWhiteSpace(emptyMessage, nameof(emptyMessage));
        loader = ArgumentGuard.ThrowIfNull(loader, nameof(loader));

        var loadedItems = new List<T>(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            loadedItems.AddRange(loader(path));
        }

        if (loadedItems.Count == 0)
        {
            ArgumentGuard.ThrowArgument(emptyMessage, argumentName);
        }

        if (comparison is not null)
        {
            loadedItems.Sort(comparison);
        }

        return new ReadOnlyCollection<T>(loadedItems);
    }
}
