// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Resources;

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// Provides cache-manifest persistence and validation for downloaded grid resources.
/// </summary>
internal static class GridResourceCacheManifest
{
    private const string ManifestFileSuffix = ".projnet-fetch.json";

    /// <summary>
    /// Deletes the cached grid file and its manifest, ignoring files that do not exist.
    /// </summary>
    /// <param name="targetFilePath">The cached grid file path.</param>
    internal static void DeleteArtifacts(string targetFilePath)
    {
        TryDelete(targetFilePath);
        TryDelete(GetManifestPath(targetFilePath));
    }

    /// <summary>
    /// Gets the cache-manifest path associated with a downloaded grid file.
    /// </summary>
    /// <param name="targetFilePath">The cached grid file path.</param>
    /// <returns>The sidecar manifest path.</returns>
    internal static string GetManifestPath(string targetFilePath) => $"{targetFilePath}{ManifestFileSuffix}";

    /// <summary>
    /// Determines whether the cached grid file is valid according to its manifest.
    /// </summary>
    /// <remarks>
    /// A missing manifest sidecar is treated as valid when the cached grid file itself exists.
    /// Manifest validation is only enforced once the sidecar file has been written.
    /// </remarks>
    /// <param name="targetFilePath">The cached grid file path.</param>
    /// <returns><see langword="true"/> when the cache entry is usable; otherwise <see langword="false"/>.</returns>
    internal static bool IsValid(string targetFilePath)
    {
        if (!File.Exists(targetFilePath))
        {
            return false;
        }

        string manifestPath = GetManifestPath(targetFilePath);
        if (!File.Exists(manifestPath))
        {
            return true;
        }

        try
        {
            using FileStream stream = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(stream);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("content_length", out JsonElement contentLengthElement)
                || !contentLengthElement.TryGetInt64(out long expectedLength)
                || expectedLength < 0)
            {
                return false;
            }

            if (!root.TryGetProperty("sha256", out JsonElement sha256Element))
            {
                return false;
            }

            string? expectedHash = sha256Element.GetString();
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                return false;
            }

            long actualLength = new FileInfo(targetFilePath).Length;
            if (actualLength != expectedLength)
            {
                return false;
            }

            string actualHash = ComputeSha256(targetFilePath);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Writes a cache manifest for the downloaded grid file.
    /// </summary>
    /// <param name="targetFilePath">The cached grid file path.</param>
    /// <param name="sourceUri">The absolute source URI used for the download.</param>
    internal static void Write(string targetFilePath, string sourceUri)
    {
        ArgumentGuard.ThrowIfNull(targetFilePath, nameof(targetFilePath));
        ArgumentGuard.ThrowIfNull(sourceUri, nameof(sourceUri));

        if (!File.Exists(targetFilePath))
        {
            ArgumentGuard.ThrowArgument("The cached grid file must exist before writing its manifest.", nameof(targetFilePath));
        }

        string manifestPath = GetManifestPath(targetFilePath);
        long contentLength = new FileInfo(targetFilePath).Length;
        string sha256 = ComputeSha256(targetFilePath);

        using var stream = new FileStream(manifestPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        writer.WriteString("source_uri", sourceUri);
        writer.WriteNumber("content_length", contentLength);
        writer.WriteString("sha256", sha256);
        writer.WriteEndObject();
        writer.Flush();
    }

    private static string ComputeSha256(string targetFilePath)
    {
        using FileStream stream = File.OpenRead(targetFilePath);
        using var hashAlgorithm = SHA256.Create();
        byte[] hash = hashAlgorithm.ComputeHash(stream);

        var builder = new StringBuilder(hash.Length * 2);
        foreach (byte octet in hash)
        {
            builder.Append(octet.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static void TryDelete(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
