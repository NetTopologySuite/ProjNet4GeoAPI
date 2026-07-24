// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

/// <summary>
/// Lazily indexes supported coordinate-system fixtures from the checked-in EPSG WKT archive.
/// </summary>
internal static class EpsgArchiveWktFixtureSource
{
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

    private static readonly Lazy<FixtureState> Fixtures = new(CreateFixtureState, true);
    private static readonly Lazy<HashSet<int>> SupportedSrids = new(
        () => new ManagedCoordinateSystemDefinitionProvider()
            .GetCoordinateSystems()
            .Select(entry => entry.Srid)
            .ToHashSet(),
        true);

    /// <summary>
    /// Gets the resolved path to the checked-in EPSG WKT archive.
    /// </summary>
    internal static string ArchivePath => Fixtures.Value.ArchivePath;

    /// <summary>
    /// Gets one supported EPSG archive fixture by SRID.
    /// </summary>
    /// <param name="srid">The EPSG SRID to resolve.</param>
    /// <returns>The cached fixture for <paramref name="srid"/>.</returns>
    internal static EpsgArchiveWktFixture GetFixture(int srid)
    {
        FixtureIndex fixtureIndex = RequireFixtureIndex();
        if (!fixtureIndex.FixturesBySrid.TryGetValue(srid, out EpsgArchiveWktFixture? fixture))
        {
            throw new InvalidOperationException(
                FormattableString.Invariant(
                    $"Could not locate a supported EPSG:{srid} fixture in {Path.GetFileName(ArchivePath)}."));
        }

        return fixture;
    }

    /// <summary>
    /// Gets all supported EPSG archive fixtures in deterministic SRID order.
    /// </summary>
    /// <returns>The cached supported fixtures.</returns>
    internal static IReadOnlyList<EpsgArchiveWktFixture> GetAllSupportedFixtures()
        => RequireFixtureIndex().OrderedFixtures;

    /// <summary>
    /// Gets xUnit theory rows for the requested EPSG SRIDs, or for all supported fixtures when none are specified.
    /// </summary>
    /// <param name="srids">Optional EPSG SRIDs to project into theory rows.</param>
    /// <returns>The requested SRID/WKT theory rows.</returns>
    internal static IEnumerable<TheoryDataRow<int, string>> GetTheoryDataRows(params int[] srids)
    {
        ArgumentNullException.ThrowIfNull(srids);

        FixtureState fixtureState = Fixtures.Value;
        if (fixtureState.Index is null)
        {
            yield return CreateSkippedTheoryDataRow(fixtureState.SkipReason);
            yield break;
        }

        FixtureIndex fixtureIndex = Assert.IsType<FixtureIndex>(fixtureState.Index);

        if (srids.Length == 0)
        {
            foreach (EpsgArchiveWktFixture fixture in fixtureIndex.OrderedFixtures)
            {
                yield return CreateTheoryDataRow(fixture);
            }

            yield break;
        }

        foreach (int srid in srids)
        {
            if (!fixtureIndex.FixturesBySrid.TryGetValue(srid, out EpsgArchiveWktFixture? fixture))
            {
                throw new InvalidOperationException(
                    FormattableString.Invariant(
                        $"Could not locate a supported EPSG:{srid} fixture in {Path.GetFileName(ArchivePath)}."));
            }

            yield return CreateTheoryDataRow(fixture);
        }
    }

    private static TheoryDataRow<int, string> CreateTheoryDataRow(EpsgArchiveWktFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return new TheoryDataRow<int, string>(fixture.Srid, fixture.Wkt);
    }

    private static FixtureState CreateFixtureState()
    {
        string archivePath = GetArchivePath();
        if (!File.Exists(archivePath))
        {
            return new FixtureState(
                archivePath,
                null,
                FormattableString.Invariant($"EPSG archive fixture '{archivePath}' is unavailable in this checkout."));
        }

        var fixturesBySrid = new Dictionary<int, EpsgArchiveWktFixture>();
        var duplicates = new List<string>();

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

            var fixture = new EpsgArchiveWktFixture(srid.Value, entry.FullName, rootKeyword, wkt);
            if (!fixturesBySrid.TryAdd(fixture.Srid, fixture))
            {
                duplicates.Add(
                    FormattableString.Invariant(
                        $"{fixture.ArchiveEntryPath} duplicates supported EPSG:{fixture.Srid}."));
            }
        }

        EnsureNoDuplicateFixtures(archivePath, duplicates);
        EnsureAllSupportedSridsWereMatched(archivePath, fixturesBySrid);

        EpsgArchiveWktFixture[] orderedFixtures = fixturesBySrid.Values
            .OrderBy(fixture => fixture.Srid)
            .ToArray();

        return new FixtureState(
            archivePath,
            new FixtureIndex(
                archivePath,
                Array.AsReadOnly(orderedFixtures),
                fixturesBySrid),
            null);
    }

    private static TheoryDataRow<int, string> CreateSkippedTheoryDataRow(string? skipReason)
    {
        return new TheoryDataRow<int, string>(0, string.Empty)
        {
            Skip = skipReason ?? "EPSG archive fixtures are unavailable in this checkout.",
        };
    }

    private static FixtureIndex RequireFixtureIndex()
    {
        FixtureState fixtureState = Fixtures.Value;
        if (fixtureState.Index is null)
        {
            Assert.Skip(fixtureState.SkipReason ?? "EPSG archive fixtures are unavailable in this checkout.");
        }

        return Assert.IsType<FixtureIndex>(fixtureState.Index);
    }

    private static void EnsureNoDuplicateFixtures(string archivePath, List<string> duplicates)
    {
        if (duplicates.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Found duplicate supported EPSG WKT entries in {Path.GetFileName(archivePath)}:{Environment.NewLine}{string.Join(Environment.NewLine, duplicates)}");
    }

    private static void EnsureAllSupportedSridsWereMatched(string archivePath, Dictionary<int, EpsgArchiveWktFixture> fixturesBySrid)
    {
        if (fixturesBySrid.Count == SupportedSrids.Value.Count)
        {
            return;
        }

        int[] missingSrids = SupportedSrids.Value
            .Except(fixturesBySrid.Keys)
            .OrderBy(srid => srid)
            .ToArray();

        const int maxMissingToPrint = 20;
        string printedSrids = string.Join(", ", missingSrids
            .Take(maxMissingToPrint)
            .Select(srid => srid.ToString(CultureInfo.InvariantCulture)));
        string remainder = missingSrids.Length > maxMissingToPrint
            ? FormattableString.Invariant($", ... {missingSrids.Length - maxMissingToPrint} more")
            : string.Empty;

        throw new InvalidOperationException(
            FormattableString.Invariant(
                $"Matched {fixturesBySrid.Count} supported EPSG WKT fixtures from {Path.GetFileName(archivePath)}, but {missingSrids.Length} managed SRIDs were missing: {printedSrids}{remainder}."));
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

    private sealed class FixtureIndex
    {
        internal FixtureIndex(
            string archivePath,
            ReadOnlyCollection<EpsgArchiveWktFixture> orderedFixtures,
            Dictionary<int, EpsgArchiveWktFixture> fixturesBySrid)
        {
            this.ArchivePath = archivePath;
            this.OrderedFixtures = orderedFixtures;
            this.FixturesBySrid = fixturesBySrid;
        }

        internal string ArchivePath { get; }

        internal ReadOnlyCollection<EpsgArchiveWktFixture> OrderedFixtures { get; }

        internal Dictionary<int, EpsgArchiveWktFixture> FixturesBySrid { get; }
    }

    private sealed class FixtureState
    {
        internal FixtureState(string archivePath, FixtureIndex? index, string? skipReason)
        {
            this.ArchivePath = archivePath;
            this.Index = index;
            this.SkipReason = skipReason;
        }

        internal string ArchivePath { get; }

        internal FixtureIndex? Index { get; }

        internal string? SkipReason { get; }
    }
}
