// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Resources;
using Xunit;

/// <summary>
/// Tests for <see cref="HttpGridResourceFetchClient"/> and environment-driven network resolution.
/// </summary>
[Collection(GlobalEnvironmentTestIsolation.Name)]
public sealed class HttpGridResourceFetchClientTests
{
    /// <summary>
    /// Verifies the HTTP fetch client downloads the requested grid file and writes a cache manifest.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryFetchAsync_DownloadsGridAndWritesManifest()
    {
        string targetDirectory = CreateTemporaryDirectory();
        try
        {
            byte[] payload = Encoding.UTF8.GetBytes("downloaded-grid");
            using var handler = new RecordingHttpMessageHandler(_ => CreateResponse(HttpStatusCode.OK, payload));
            using var httpClient = new HttpClient(handler);
            var fetchClient = new HttpGridResourceFetchClient("https://example.test/grids/", httpClient);
            string targetPath = Path.Combine(targetDirectory, "sample.gsb");

            bool fetched = await fetchClient.TryFetchAsync(@"nested\sample.gsb", targetPath, TestContext.Current.CancellationToken);

            Assert.True(fetched);
            Assert.Equal(new Uri("https://example.test/grids/sample.gsb"), handler.LastRequestUri);
            Assert.Equal(payload, await File.ReadAllBytesAsync(targetPath, TestContext.Current.CancellationToken));
            Assert.True(File.Exists(GridResourceCacheManifest.GetManifestPath(targetPath)));
            Assert.True(GridResourceCacheManifest.IsValid(targetPath));
        }
        finally
        {
            Directory.Delete(targetDirectory, true);
        }
    }

    /// <summary>
    /// Verifies non-success HTTP responses do not leave partial cache artifacts behind.
    /// </summary>
    [Fact]
    public void TryFetch_WithNonSuccessStatus_ReturnsFalseAndLeavesNoArtifacts()
    {
        string targetDirectory = CreateTemporaryDirectory();
        try
        {
            using var handler = new RecordingHttpMessageHandler(_ => CreateResponse(HttpStatusCode.NotFound));
            using var httpClient = new HttpClient(handler);
            var fetchClient = new HttpGridResourceFetchClient("https://example.test/grids/", httpClient);
            string targetPath = Path.Combine(targetDirectory, "missing.gsb");

            bool fetched = fetchClient.TryFetch("missing.gsb", targetPath);

            Assert.False(fetched);
            Assert.False(File.Exists(targetPath));
            Assert.False(File.Exists(GridResourceCacheManifest.GetManifestPath(targetPath)));
        }
        finally
        {
            Directory.Delete(targetDirectory, true);
        }
    }

    /// <summary>
    /// Verifies <see cref="CoordinateTransformationFactory.ConfigureGridResolution(ProjNet.Resources.IGridResourceFetchClient?, System.Collections.Generic.IEnumerable{string}?, string?, ProjNet.Resources.GridResourceResolutionMode)"/>
    /// recreates the resolver from environment settings, including the <c>network</c> mode alias and HTTP fetch activation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigureGridResolution_WithoutArgumentsUsesEnvironmentNetworkSettings()
    {
        string cacheDirectory = CreateTemporaryDirectory();
        string? originalGridMode = Environment.GetEnvironmentVariable("PROJNET_GRID_MODE");
        string? originalGridBaseUrl = Environment.GetEnvironmentVariable("PROJNET_GRID_BASE_URL");
        string? originalGridCache = Environment.GetEnvironmentVariable("PROJNET_GRID_CACHE");
        string? originalGridPaths = Environment.GetEnvironmentVariable("PROJNET_GRID_PATHS");

        using var server = new TestHttpServer("network-grid.gsb", Encoding.UTF8.GetBytes("server-grid"));

        try
        {
            Environment.SetEnvironmentVariable("PROJNET_GRID_MODE", "network");
            Environment.SetEnvironmentVariable("PROJNET_GRID_BASE_URL", server.BaseUrl);
            Environment.SetEnvironmentVariable("PROJNET_GRID_CACHE", cacheDirectory);
            Environment.SetEnvironmentVariable("PROJNET_GRID_PATHS", string.Empty);

            CoordinateTransformationFactory.ConfigureGridResolution();

            string? resolvedPath = await CoordinateTransformationFactory.TryResolveGridResourcePathAsync(
                "network-grid.gsb",
                TestContext.Current.CancellationToken);

            Assert.NotNull(resolvedPath);
            Assert.Equal("server-grid", await File.ReadAllTextAsync(resolvedPath, TestContext.Current.CancellationToken));
            Assert.Equal(1, server.RequestCount);
            Assert.True(File.Exists(GridResourceCacheManifest.GetManifestPath(resolvedPath)));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PROJNET_GRID_MODE", originalGridMode);
            Environment.SetEnvironmentVariable("PROJNET_GRID_BASE_URL", originalGridBaseUrl);
            Environment.SetEnvironmentVariable("PROJNET_GRID_CACHE", originalGridCache);
            Environment.SetEnvironmentVariable("PROJNET_GRID_PATHS", originalGridPaths);
            CoordinateTransformationFactory.ConfigureGridResolution(
                new NoOpGridResourceFetchClient(),
                Array.Empty<string>(),
                null,
                GridResourceResolutionMode.LocalOnly);
            Directory.Delete(cacheDirectory, true);
        }
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, byte[]? payload = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (payload is not null)
        {
            response.Content = new ByteArrayContent(payload);
            response.Content.Headers.ContentLength = payload.Length;
        }

        return response;
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "projnet-grid-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        internal RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        internal Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequestUri = request.RequestUri;
            HttpResponseMessage response = this.responseFactory(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    private sealed class TestHttpServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly Task listenerLoop;
        private readonly byte[] payload;
        private readonly string requestPath;
        private int requestCount;

        internal TestHttpServer(string fileName, byte[] payload)
        {
            this.payload = payload;
            this.requestPath = $"/{fileName}";
            int port = GetFreePort();
            this.BaseUrl = $"http://127.0.0.1:{port}/";
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(this.BaseUrl);
            this.listener.Start();
            this.listenerLoop = Task.Run(this.RunAsync);
        }

        internal string BaseUrl { get; }

        internal int RequestCount => Volatile.Read(ref this.requestCount);

        public void Dispose()
        {
            this.listener.Stop();
            this.listener.Close();

            try
            {
                this.listenerLoop.GetAwaiter().GetResult();
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task RunAsync()
        {
            while (this.listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await this.listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                Interlocked.Increment(ref this.requestCount);
                context.Response.StatusCode = string.Equals(context.Request.Url?.AbsolutePath, this.requestPath, StringComparison.Ordinal)
                    ? (int)HttpStatusCode.OK
                    : (int)HttpStatusCode.NotFound;

                if (context.Response.StatusCode == (int)HttpStatusCode.OK)
                {
                    context.Response.ContentLength64 = this.payload.Length;
                    await context.Response.OutputStream.WriteAsync(this.payload.AsMemory(), CancellationToken.None).ConfigureAwait(false);
                }

                context.Response.Close();
            }
        }
    }
}
