// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.Resources;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Represents the documented type.
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
    /// <param name="options">The options value.</param>
    /// <param name="fetchClient">The fetchClient value.</param>
    internal GridResourceResolver(GridResourceResolverOptions options, IGridResourceFetchClient fetchClient = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.fetchClient = fetchClient ?? DefaultFetchClient;
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridName">The gridName value.</param>
    /// <param name="resolvedPath">The resolvedPath value.</param>
    /// <returns>The computed value.</returns>
    internal bool TryResolve(string gridName, out string resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(gridName))
        {
            throw new ArgumentException("Grid name must not be empty.", nameof(gridName));
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

    private void RememberResolvedPath(string gridName, string resolvedPath)
    {
        lock (this.sync)
        {
            this.resolvedPathByGridName[gridName] = resolvedPath;
        }
    }

    private bool TryResolveFromCache(string gridName, out string resolvedPath)
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

    private bool TryResolveFromLocalSources(string gridName, out string resolvedPath)
    {
        if (Path.IsPathRooted(gridName) && File.Exists(gridName))
        {
            resolvedPath = Path.GetFullPath(gridName);
            return true;
        }

        string fileName = Path.GetFileName(gridName);
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

    private bool TryResolveFromNetwork(string gridName, out string resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(this.options.CacheDirectory))
        {
            resolvedPath = null;
            return false;
        }

        Directory.CreateDirectory(this.options.CacheDirectory);
        string fileName = Path.GetFileName(gridName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            resolvedPath = null;
            return false;
        }

        string targetPath = Path.Combine(this.options.CacheDirectory, fileName);
        if (File.Exists(targetPath))
        {
            resolvedPath = targetPath;
            return true;
        }

        if (!this.fetchClient.TryFetch(gridName, targetPath) || !File.Exists(targetPath))
        {
            resolvedPath = null;
            return false;
        }

        resolvedPath = targetPath;
        return true;
    }
}
