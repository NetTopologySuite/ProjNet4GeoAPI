// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;
using Xunit.Sdk;

/// <summary>
/// Verifies that every managed EPSG coordinate system still parses from the current upstream WKT archive.
/// </summary>
public class EpsgArchiveWktParserSweepTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();

    /// <summary>
    /// Verifies the current EPSG WKT ZIP parses cleanly for every coordinate system exposed by the managed catalog.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesAllSupportedCoordinateSystemEntriesFromCurrentEpsgArchive()
    {
        IReadOnlyList<EpsgArchiveWktFixture> fixtures = EpsgArchiveWktFixtureSource.GetAllSupportedFixtures();
        var failures = new List<string>();

        foreach (EpsgArchiveWktFixture fixture in fixtures)
        {
            string? failure = TryParseArchiveEntry(fixture.Wkt, fixture.Srid);
            if (failure is not null)
            {
                failures.Add(
                    $"{fixture.ArchiveEntryPath} (EPSG:{fixture.Srid.ToString(CultureInfo.InvariantCulture)}, {fixture.RootKeyword}) failed: {failure}");
            }
        }

        Assert.True(
            failures.Count == 0,
            BuildFailureMessage(EpsgArchiveWktFixtureSource.ArchivePath, fixtures.Count, failures));
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
