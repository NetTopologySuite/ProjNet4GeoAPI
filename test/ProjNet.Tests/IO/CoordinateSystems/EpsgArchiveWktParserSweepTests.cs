// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
using ProjNet.Data.Generated;
using ProjNet.IO.Wkt;
using Xunit;
using Xunit.Sdk;

/// <summary>
/// Verifies that every managed EPSG coordinate system still parses from the current upstream WKT archive.
/// </summary>
public class EpsgArchiveWktParserSweepTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly Regex EpsgIdRegex = new("""ID\["EPSG",(?<id>\d+)\]""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> CoordinateSystemRoots =
    [
        "COMPOUNDCRS",
        "ENGINEERINGCRS",
        "ENGCRS",
        "GEODCRS",
        "GEODETICCRS",
        "GEOGCRS",
        "PROJCRS",
        "VERTCRS",
    ];

    private static readonly Lazy<HashSet<int>> SupportedSrids = new(
        () => new ManagedCoordinateSystemDefinitionProvider()
            .GetCoordinateSystems()
            .Select(entry => entry.Srid)
            .ToHashSet(),
        true);

    /// <summary>
    /// Verifies the current EPSG WKT ZIP parses cleanly for every coordinate system exposed by the managed catalog.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesAllSupportedCoordinateSystemEntriesFromCurrentEpsgArchive()
    {
        string archivePath = GetArchivePath();
        Assert.True(File.Exists(archivePath), $"Could not locate EPSG archive '{archivePath}'.");

        var matchedSrids = new HashSet<int>();
        var failures = new List<string>();

        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        foreach (ZipArchiveEntry entry in archive.Entries.OrderBy(item => item.FullName, StringComparer.Ordinal))
        {
            if (!entry.FullName.EndsWith(".wkt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string wkt = ReadEntryText(entry);
            string rootKeyword = GetRootKeyword(wkt);
            if (!CoordinateSystemRoots.Contains(rootKeyword))
            {
                continue;
            }

            int? srid = TryGetFinalEpsgId(wkt);
            if (srid is null || !SupportedSrids.Value.Contains(srid.Value))
            {
                continue;
            }

            if (!matchedSrids.Add(srid.Value))
            {
                failures.Add($"{entry.FullName} duplicates supported EPSG:{srid.Value.ToString(CultureInfo.InvariantCulture)}.");
                continue;
            }

            string? failure = TryParseArchiveEntry(wkt, srid.Value);
            if (failure is not null)
            {
                failures.Add(
                    $"{entry.FullName} (EPSG:{srid.Value.ToString(CultureInfo.InvariantCulture)}, {rootKeyword}) failed: {failure}");
            }
        }

        Assert.Equal(SupportedSrids.Value.Count, matchedSrids.Count);
        Assert.True(failures.Count == 0, BuildFailureMessage(archivePath, matchedSrids.Count, failures));
    }

    private static string BuildFailureMessage(string archivePath, int parsedCount, List<string> failures)
    {
        string header = string.Format(
            CultureInfo.InvariantCulture,
            "Failed to parse {0} supported coordinate system entr{1} from {2} after matching {3} managed SRIDs.",
            failures.Count,
            failures.Count == 1 ? "y" : "ies",
            Path.GetFileName(archivePath),
            parsedCount);

        if (failures.Count == 0)
        {
            return header;
        }

        const int maxFailuresToPrint = 20;
        IEnumerable<string> lines = failures.Take(maxFailuresToPrint);
        string remainder = failures.Count > maxFailuresToPrint
            ? $"{Environment.NewLine}... {failures.Count - maxFailuresToPrint} more failure(s) omitted."
            : string.Empty;

        return $"{header}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}{remainder}";
    }

    private static string GetArchivePath()
    {
        string projectRoot = GetProjectRoot();
        return Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "spec", "epsg", EpsgGeneratedCatalog.SourceArchive));
    }

    private static string GetProjectRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ProjNet4GeoAPI.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate project root from test output directory.");
    }

    private static string ReadEntryText(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string GetRootKeyword(string wkt)
    {
        ReadOnlySpan<char> trimmed = wkt.AsSpan().TrimStart();
        int bracketIndex = trimmed.IndexOf('[');
        return bracketIndex > 0
            ? trimmed[..bracketIndex].ToString()
            : string.Empty;
    }

    private static int? TryGetFinalEpsgId(string wkt)
    {
        MatchCollection matches = EpsgIdRegex.Matches(wkt);
        if (matches.Count == 0)
        {
            return null;
        }

        Group id = matches[matches.Count - 1].Groups["id"];
        return int.TryParse(id.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int srid)
            ? srid
            : null;
    }

    private static string? TryParseArchiveEntry(string wkt, int srid)
    {
        try
        {
            CoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem(CoordinateSystemFactory, wkt);
            Assert.Equal("EPSG", parsed.Authority);
            Assert.Equal(srid, parsed.AuthorityCode);
            return null;
        }
        catch (XunitException ex)
        {
            return ex.Message;
        }
        catch (WktParseException ex)
        {
            return ex.Message;
        }
        catch (NotSupportedException ex)
        {
            return ex.Message;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (FormatException ex)
        {
            return ex.Message;
        }
        catch (OverflowException ex)
        {
            return ex.Message;
        }
    }
}
