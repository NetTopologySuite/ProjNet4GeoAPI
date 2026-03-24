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

