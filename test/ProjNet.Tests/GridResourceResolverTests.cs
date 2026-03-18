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

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using ProjNet.Resources;
using Xunit;

public class GridResourceResolverTests
{
    [Fact]
    public void TryResolve_WithLocalGridFile_ResolvesWithoutNetwork()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            string localGridPath = Path.Combine(localDirectory, "sample.gsb");
            File.WriteAllText(localGridPath, "local-grid");

            var fetchClient = new RecordingFetchClient();
            var options = new GridResourceResolverOptions(new[] { localDirectory }, null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, fetchClient);

            bool resolved = resolver.TryResolve("sample.gsb", out string resolvedPath);

            Assert.True(resolved);
            Assert.Equal(localGridPath, resolvedPath);
            Assert.Equal(0, fetchClient.Calls);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    [Fact]
    public void TryResolve_WithLocalOnlyMode_DoesNotCallNetworkFetcher()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            var fetchClient = new RecordingFetchClient();
            var options = new GridResourceResolverOptions(new[] { localDirectory }, null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, fetchClient);

            bool resolved = resolver.TryResolve("missing.gsb", out string _);

            Assert.False(resolved);
            Assert.Equal(0, fetchClient.Calls);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    [Fact]
    public void TryResolve_WithNetworkMode_DownloadsToCacheAndReusesCachedFile()
    {
        string localDirectory = CreateTemporaryDirectory();
        string cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var fetchClient = new RecordingFetchClient
            {
                OnFetch = path => File.WriteAllText(path, "downloaded-grid"),
            };
            var options = new GridResourceResolverOptions(new[] { localDirectory }, cacheDirectory, GridResourceResolutionMode.LocalThenNetwork);
            var resolver = new GridResourceResolver(options, fetchClient);

            bool firstResolved = resolver.TryResolve("network-grid.gsb", out string firstPath);
            bool secondResolved = resolver.TryResolve("network-grid.gsb", out string secondPath);

            Assert.True(firstResolved);
            Assert.True(secondResolved);
            Assert.Equal(firstPath, secondPath);
            Assert.True(File.Exists(firstPath));
            Assert.Equal(1, fetchClient.Calls);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
            Directory.Delete(cacheDirectory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "projnet-grid-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class RecordingFetchClient : IGridResourceFetchClient
    {
        internal int Calls { get; private set; }

        internal Action<string> OnFetch { get; set; }

        public bool TryFetch(string gridName, string targetFilePath)
        {
            this.Calls++;
            this.OnFetch?.Invoke(targetFilePath);
            return this.OnFetch != null;
        }
    }
}
