// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Resolves named grid resources to local file paths, searching local directories first
/// and optionally falling back to network retrieval via an <see cref="IGridResourceFetchClient"/>.
/// </summary>
internal sealed class GridResourceResolver
{
    private static readonly IGridResourceFetchClient DefaultFetchClient = new NoOpGridResourceFetchClient();

    private readonly IGridResourceFetchClient fetchClient;
    private readonly GridResourceResolverOptions options;
    private readonly Dictionary<string, string> resolvedPathByGridName = new(StringComparer.OrdinalIgnoreCase);
    private readonly object sync = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GridResourceResolver"/> class.
    /// </summary>
    /// <param name="options">Resolution options including local search directories and cache settings.</param>
    /// <param name="fetchClient">Optional fetch client used for network retrieval; defaults to a no-op client when <see langword="null"/>.</param>
    internal GridResourceResolver(GridResourceResolverOptions options, IGridResourceFetchClient? fetchClient = null)
    {
        this.options = ArgumentGuard.ThrowIfNull(options, nameof(options));
        this.fetchClient = fetchClient ?? DefaultFetchClient;
    }

    /// <summary>
    /// Attempts to resolve a named grid resource to an absolute local file path.
    /// </summary>
    /// <remarks>
    /// Resolution order: in-memory cache, local file system (rooted path or configured directories),
    /// and network retrieval when <see cref="GridResourceResolutionMode.LocalThenNetwork"/> is active.
    /// Successful resolutions are cached for subsequent calls.
    /// </remarks>
    /// <param name="gridName">The grid resource name or rooted file path to resolve.</param>
    /// <param name="resolvedPath">The absolute local path when resolution succeeds; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the grid was located; otherwise <see langword="false"/>.</returns>
    internal bool TryResolve(string gridName, [NotNullWhen(true)] out string? resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(gridName))
        {
            ArgumentGuard.ThrowArgument("Grid name must not be empty.", nameof(gridName));
        }

        if (this.TryResolveFromCache(gridName, out resolvedPath))
        {
            return true;
        }

        if (this.TryResolveFromLocalSources(gridName, out resolvedPath))
        {
            this.RememberResolvedPath(gridName, resolvedPath);
            return true;
        }

        if (this.options.Mode == GridResourceResolutionMode.LocalThenNetwork && this.TryResolveFromNetwork(gridName, out resolvedPath))
        {
            this.RememberResolvedPath(gridName, resolvedPath);
            return true;
        }

        resolvedPath = null;
        return false;
    }

    /// <summary>
    /// Asynchronously attempts to resolve a named grid resource to an absolute local file path.
    /// </summary>
    /// <remarks>
    /// Resolution order: in-memory cache, local file system (rooted path or configured directories),
    /// and network retrieval via <see cref="IGridResourceFetchClient.TryFetchAsync"/> when
    /// <see cref="GridResourceResolutionMode.LocalThenNetwork"/> is active.
    /// Successful resolutions are cached for subsequent calls.
    /// </remarks>
    /// <param name="gridName">The grid resource name or rooted file path to resolve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The absolute local path when resolution succeeds; otherwise <see langword="null"/>.</returns>
    internal async Task<string?> TryResolveAsync(string gridName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gridName))
        {
            ArgumentGuard.ThrowArgument("Grid name must not be empty.", nameof(gridName));
        }

        if (this.TryResolveFromCache(gridName, out string? resolvedPath))
        {
            return resolvedPath;
        }

        if (this.TryResolveFromLocalSources(gridName, out resolvedPath))
        {
            this.RememberResolvedPath(gridName, resolvedPath);
            return resolvedPath;
        }

        if (this.options.Mode == GridResourceResolutionMode.LocalThenNetwork)
        {
            resolvedPath = await this.TryResolveFromNetworkAsync(gridName, cancellationToken).ConfigureAwait(false);
            if (resolvedPath is not null)
            {
                this.RememberResolvedPath(gridName, resolvedPath);
                return resolvedPath;
            }
        }

        return null;
    }

    private static string GetGridFileName(string gridName)
    {
        string normalizedGridName = gridName.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFileName(normalizedGridName);
    }

    private void RememberResolvedPath(string gridName, string resolvedPath)
    {
        lock (this.sync)
        {
            this.resolvedPathByGridName[gridName] = resolvedPath;
        }
    }

    private bool TryResolveFromCache(string gridName, [NotNullWhen(true)] out string? resolvedPath)
    {
        lock (this.sync)
        {
            if (this.resolvedPathByGridName.TryGetValue(gridName, out resolvedPath))
            {
                if (File.Exists(resolvedPath))
                {
                    return true;
                }

                this.resolvedPathByGridName.Remove(gridName);
            }
        }

        resolvedPath = null;
        return false;
    }

    private bool TryResolveFromLocalSources(string gridName, [NotNullWhen(true)] out string? resolvedPath)
    {
        if (Path.IsPathRooted(gridName) && File.Exists(gridName))
        {
            resolvedPath = Path.GetFullPath(gridName);
            return true;
        }

        string fileName = GetGridFileName(gridName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            resolvedPath = null;
            return false;
        }

        foreach (string localDirectory in this.options.LocalDirectories)
        {
            string candidatePath = Path.Combine(localDirectory, fileName);
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            resolvedPath = candidatePath;
            return true;
        }

        resolvedPath = null;
        return false;
    }

    private bool TryResolveFromNetwork(string gridName, [NotNullWhen(true)] out string? resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(this.options.CacheDirectory))
        {
            resolvedPath = null;
            return false;
        }

        Directory.CreateDirectory(this.options.CacheDirectory);
        string fileName = GetGridFileName(gridName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            resolvedPath = null;
            return false;
        }

        string targetPath = Path.Combine(this.options.CacheDirectory, fileName);
        if (File.Exists(targetPath))
        {
            if (GridResourceCacheManifest.IsValid(targetPath))
            {
                resolvedPath = targetPath;
                return true;
            }

            GridResourceCacheManifest.DeleteArtifacts(targetPath);
        }

        if (!this.fetchClient.TryFetch(gridName, targetPath) || !GridResourceCacheManifest.IsValid(targetPath))
        {
            GridResourceCacheManifest.DeleteArtifacts(targetPath);
            resolvedPath = null;
            return false;
        }

        resolvedPath = targetPath;
        return true;
    }

    private async Task<string?> TryResolveFromNetworkAsync(string gridName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(this.options.CacheDirectory))
        {
            return null;
        }

        Directory.CreateDirectory(this.options.CacheDirectory);
        string fileName = GetGridFileName(gridName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string targetPath = Path.Combine(this.options.CacheDirectory, fileName);
        if (File.Exists(targetPath))
        {
            if (GridResourceCacheManifest.IsValid(targetPath))
            {
                return targetPath;
            }

            GridResourceCacheManifest.DeleteArtifacts(targetPath);
        }

        if (!await this.fetchClient.TryFetchAsync(gridName, targetPath, cancellationToken).ConfigureAwait(false) || !GridResourceCacheManifest.IsValid(targetPath))
        {
            GridResourceCacheManifest.DeleteArtifacts(targetPath);
            return null;
        }

        return targetPath;
    }
}
