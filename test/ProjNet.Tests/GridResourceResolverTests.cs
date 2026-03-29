// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.IO;
using ProjNet.Resources;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class GridResourceResolverTests
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryResolveWithLocalGridFileResolvesWithoutNetwork()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            string localGridPath = Path.Combine(localDirectory, "sample.gsb");
            File.WriteAllText(localGridPath, "local-grid");

            var fetchClient = new RecordingFetchClient();
            var options = new GridResourceResolverOptions(new[] { localDirectory }, null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, fetchClient);

            bool resolved = resolver.TryResolve("sample.gsb", out string? resolvedPath);

            Assert.True(resolved);
            Assert.NotNull(resolvedPath);
            Assert.Equal(localGridPath, resolvedPath);
            Assert.Equal(0, fetchClient.Calls);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryResolveWithLocalOnlyModeDoesNotCallNetworkFetcher()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            var fetchClient = new RecordingFetchClient();
            var options = new GridResourceResolverOptions(new[] { localDirectory }, null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, fetchClient);

            bool resolved = resolver.TryResolve("missing.gsb", out string? _);

            Assert.False(resolved);
            Assert.Equal(0, fetchClient.Calls);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryResolveWithNetworkModeDownloadsToCacheAndReusesCachedFile()
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

            bool firstResolved = resolver.TryResolve("network-grid.gsb", out string? firstPath);
            bool secondResolved = resolver.TryResolve("network-grid.gsb", out string? secondPath);

            Assert.True(firstResolved);
            Assert.True(secondResolved);
            Assert.NotNull(firstPath);
            Assert.NotNull(secondPath);
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

        internal Action<string>? OnFetch { get; set; }

        public bool TryFetch(string gridName, string targetFilePath)
        {
            this.Calls++;
            this.OnFetch?.Invoke(targetFilePath);
            return this.OnFetch is not null;
        }
    }
}
