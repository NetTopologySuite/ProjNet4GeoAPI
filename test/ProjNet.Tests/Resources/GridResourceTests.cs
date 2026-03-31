// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProjNet.Resources;
using Xunit;

/// <summary>
/// Tests for <see cref="NoOpGridResourceFetchClient"/> and <see cref="GridResourceResolver"/> async behavior.
/// </summary>
public class GridResourceTests
{
    // ---- NoOpGridResourceFetchClient ----

    /// <summary>
    /// Verifies that <see cref="NoOpGridResourceFetchClient.TryFetch"/> always returns <see langword="false"/>.
    /// </summary>
    [Fact]
    public void NoOpFetchClient_TryFetch_ReturnsFalse()
    {
        var client = new NoOpGridResourceFetchClient();

        bool result = client.TryFetch("some-grid.gsb", @"C:\target\some-grid.gsb");

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that <see cref="NoOpGridResourceFetchClient.TryFetchAsync"/> always returns <see langword="false"/>.
    /// </summary>
    [Fact]
    public async Task NoOpFetchClient_TryFetchAsync_ReturnsFalse()
    {
        var client = new NoOpGridResourceFetchClient();

        bool result = await client.TryFetchAsync("some-grid.gsb", @"C:\target\some-grid.gsb");

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that <see cref="NoOpGridResourceFetchClient.TryFetchAsync"/> returns false regardless of grid name.
    /// </summary>
    [Theory]
    [InlineData("grid1.gsb")]
    [InlineData("grid2.tif")]
    [InlineData("")]
    public async Task NoOpFetchClient_TryFetchAsync_AlwaysReturnsFalse(string gridName)
    {
        var client = new NoOpGridResourceFetchClient();

        bool result = await client.TryFetchAsync(gridName, "target-path");

        Assert.False(result);
    }

    /// <summary>
    /// Verifies that <see cref="NoOpGridResourceFetchClient.TryFetchAsync"/> completes synchronously.
    /// </summary>
    [Fact]
    public async Task NoOpFetchClient_TryFetchAsync_WithCancellationToken_CompletesSynchronously()
    {
        var client = new NoOpGridResourceFetchClient();
        using var cts = new CancellationTokenSource();

        Task<bool> task = client.TryFetchAsync("grid.gsb", "path", cts.Token);

        Assert.True(task.IsCompleted);
        Assert.False(await task);
    }

    // ---- GridResourceResolver async tests ----

    /// <summary>
    /// Verifies that <see cref="GridResourceResolver.TryResolveAsync"/> returns the path when a local file exists.
    /// </summary>
    [Fact]
    public async Task TryResolveAsync_WithLocalFile_ReturnsPath()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            string localGridPath = Path.Combine(localDirectory, "sample.gsb");
            File.WriteAllText(localGridPath, "local-grid");

            var options = new GridResourceResolverOptions(
                [localDirectory], null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, new NoOpGridResourceFetchClient());

            string? resolvedPath = await resolver.TryResolveAsync("sample.gsb");

            Assert.NotNull(resolvedPath);
            Assert.Equal(localGridPath, resolvedPath);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    /// <summary>
    /// Verifies that <see cref="GridResourceResolver.TryResolveAsync"/> returns null when no file exists.
    /// </summary>
    [Fact]
    public async Task TryResolveAsync_WithNoFile_ReturnsNull()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            var options = new GridResourceResolverOptions(
                [localDirectory], null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, new NoOpGridResourceFetchClient());

            string? resolvedPath = await resolver.TryResolveAsync("missing.gsb");

            Assert.Null(resolvedPath);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    /// <summary>
    /// Verifies that <see cref="GridResourceResolver.TryResolveAsync"/> with cancellation token completes normally.
    /// </summary>
    [Fact]
    public async Task TryResolveAsync_WithCancellationToken_Completes()
    {
        string localDirectory = CreateTemporaryDirectory();
        try
        {
            string localGridPath = Path.Combine(localDirectory, "test.gsb");
            await File.WriteAllTextAsync(localGridPath, "data");

            var options = new GridResourceResolverOptions(
                [localDirectory], null, GridResourceResolutionMode.LocalOnly);
            var resolver = new GridResourceResolver(options, new NoOpGridResourceFetchClient());

            using var cts = new CancellationTokenSource();
            string? resolvedPath = await resolver.TryResolveAsync("test.gsb", cts.Token);

            Assert.NotNull(resolvedPath);
        }
        finally
        {
            Directory.Delete(localDirectory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "projnet-grid-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
