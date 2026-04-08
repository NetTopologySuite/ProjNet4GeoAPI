// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Downloads grid resources over HTTP into the local resolver cache.
/// </summary>
public sealed class HttpGridResourceFetchClient : IGridResourceFetchClient
{
    private const int CopyBufferSize = 81920;
    private static readonly HttpClient SharedHttpClient = new();

    private readonly Uri baseUri;
    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpGridResourceFetchClient"/> class.
    /// </summary>
    /// <param name="baseUrl">The absolute base URL used to resolve grid file names.</param>
    /// <param name="httpClient">Optional HTTP client to use for requests; when <see langword="null"/>, a shared client is used.</param>
    public HttpGridResourceFetchClient(string baseUrl, HttpClient? httpClient = null)
        : this(CreateBaseUri(baseUrl), httpClient)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpGridResourceFetchClient"/> class.
    /// </summary>
    /// <param name="baseUri">The absolute base URI used to resolve grid file names.</param>
    /// <param name="httpClient">Optional HTTP client to use for requests; when <see langword="null"/>, a shared client is used.</param>
    public HttpGridResourceFetchClient(Uri baseUri, HttpClient? httpClient = null)
    {
        baseUri = ArgumentGuard.ThrowIfNull(baseUri, nameof(baseUri));
        if (!baseUri.IsAbsoluteUri)
        {
            ArgumentGuard.ThrowArgument("The grid fetch base URI must be absolute.", nameof(baseUri));
        }

        string absoluteUri = baseUri.AbsoluteUri;
        this.baseUri = absoluteUri.Length > 0 && absoluteUri[absoluteUri.Length - 1] == '/'
            ? baseUri
            : new Uri($"{absoluteUri}/", UriKind.Absolute);
        this.httpClient = httpClient ?? SharedHttpClient;
    }

    /// <inheritdoc />
    public bool TryFetch(string gridName, string targetFilePath)
    {
        #pragma warning disable CA1849 // IGridResourceFetchClient exposes a synchronous API, but HttpClient only offers async I/O.
        return this.TryFetchCoreAsync(gridName, targetFilePath, CancellationToken.None).GetAwaiter().GetResult();
        #pragma warning restore CA1849
    }

    /// <inheritdoc />
    public Task<bool> TryFetchAsync(string gridName, string targetFilePath, CancellationToken cancellationToken = default)
    {
        return this.TryFetchCoreAsync(gridName, targetFilePath, cancellationToken);
    }

    private static Uri CreateBaseUri(string baseUrl)
    {
        baseUrl = ArgumentGuard.ThrowIfNull(baseUrl, nameof(baseUrl));
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            ArgumentGuard.ThrowArgument("The grid fetch base URL must not be empty.", nameof(baseUrl));
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri))
        {
            ArgumentGuard.ThrowArgument("The grid fetch base URL must be an absolute URI.", nameof(baseUrl));
        }

        return baseUri;
    }

    private static void ReplaceFile(string sourcePath, string targetPath)
    {
        GridResourceCacheManifest.DeleteArtifacts(targetPath);
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(sourcePath, targetPath);
    }

    private static string ValidateTargetFilePath(string targetFilePath)
    {
        targetFilePath = ArgumentGuard.ThrowIfNull(targetFilePath, nameof(targetFilePath));
        if (string.IsNullOrWhiteSpace(targetFilePath))
        {
            ArgumentGuard.ThrowArgument("The target file path must not be empty.", nameof(targetFilePath));
        }

        string? directoryPath = Path.GetDirectoryName(targetFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            ArgumentGuard.ThrowArgument("The target file path must include a directory.", nameof(targetFilePath));
        }

        return directoryPath;
    }

    private static string ValidateGridFileName(string gridName)
    {
        gridName = ArgumentGuard.ThrowIfNull(gridName, nameof(gridName));
        if (string.IsNullOrWhiteSpace(gridName))
        {
            ArgumentGuard.ThrowArgument("The grid name must not be empty.", nameof(gridName));
        }

        string fileName = Path.GetFileName(gridName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            ArgumentGuard.ThrowArgument("The grid name must resolve to a file name.", nameof(gridName));
        }

        return fileName;
    }

    private async Task<bool> TryFetchCoreAsync(string gridName, string targetFilePath, CancellationToken cancellationToken)
    {
        string fileName = ValidateGridFileName(gridName);
        string targetDirectory = ValidateTargetFilePath(targetFilePath);
        Directory.CreateDirectory(targetDirectory);

        Uri requestUri = new(this.baseUri, Uri.EscapeDataString(fileName));
        string tempFilePath = $"{targetFilePath}.{Guid.NewGuid():N}.download";

        try
        {
            using HttpResponseMessage response = await this.httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            long actualLength;
            using Stream contentStream =
#if NET8_0_OR_GREATER
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
                await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await contentStream.CopyToAsync(fileStream, CopyBufferSize, cancellationToken).ConfigureAwait(false);
                await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                actualLength = fileStream.Length;
            }

            long? expectedLength = response.Content.Headers.ContentLength;
            if (expectedLength.HasValue && actualLength != expectedLength.Value)
            {
                return false;
            }

            ReplaceFile(tempFilePath, targetFilePath);
            GridResourceCacheManifest.Write(targetFilePath, requestUri.AbsoluteUri);
            return true;
        }
        catch (HttpRequestException)
        {
            GridResourceCacheManifest.DeleteArtifacts(targetFilePath);
            return false;
        }
        catch (IOException)
        {
            GridResourceCacheManifest.DeleteArtifacts(targetFilePath);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            GridResourceCacheManifest.DeleteArtifacts(targetFilePath);
            return false;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
}
