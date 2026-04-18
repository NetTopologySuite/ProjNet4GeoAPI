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
/// Verifies that every supported EPSG archive fixture materializes into a basic, structurally valid coordinate system.
/// </summary>
public class EpsgBulkFunctionalSweepTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();

    /// <summary>
    /// Verifies the current EPSG WKT ZIP materializes every supported coordinate system with core runtime invariants intact.
    /// </summary>
    [Fact]
    public void CreateFromWkt_MaterializesAllSupportedCoordinateSystemEntriesWithBasicInvariants()
    {
        IReadOnlyList<EpsgArchiveWktFixture> fixtures = EpsgArchiveWktFixtureSource.GetAllSupportedFixtures();
        var failures = new List<string>();

        foreach (EpsgArchiveWktFixture fixture in fixtures)
        {
            string? failure = TryValidateArchiveEntry(fixture);
            if (failure is not null)
            {
                failures.Add(failure);
            }
        }

        Assert.True(
            failures.Count == 0,
            BuildFailureMessage(EpsgArchiveWktFixtureSource.ArchivePath, fixtures.Count, failures));
    }

    private static string BuildFailureMessage(string archivePath, int materializedCount, List<string> failures)
    {
        string header =
            $"Failed to materialize or validate {failures.Count.ToString(CultureInfo.InvariantCulture)} supported coordinate system entr{(failures.Count == 1 ? "y" : "ies")} from {Path.GetFileName(archivePath)} after matching {materializedCount.ToString(CultureInfo.InvariantCulture)} managed SRIDs.";

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

    private static string? TryValidateArchiveEntry(EpsgArchiveWktFixture fixture)
    {
        try
        {
            CoordinateSystem coordinateSystem = CoordinateSystemTestHelpers.RequireCoordinateSystem(CoordinateSystemFactory, fixture.Wkt);
            List<string> failures = ValidateCoordinateSystem(fixture, coordinateSystem);
            return failures.Count == 0
                ? null
                : BuildFixtureFailure(fixture, string.Join("; ", failures));
        }
        catch (XunitException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
        catch (WktParseException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
        catch (FormatException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
        catch (OverflowException ex)
        {
            return BuildFixtureFailure(fixture, ex.Message);
        }
    }

    private static List<string> ValidateCoordinateSystem(EpsgArchiveWktFixture fixture, CoordinateSystem coordinateSystem)
    {
        var failures = new List<string>();

        if (!string.Equals("EPSG", coordinateSystem.Authority, StringComparison.Ordinal))
        {
            failures.Add($"Authority was '{coordinateSystem.Authority}' instead of 'EPSG'.");
        }

        if (coordinateSystem.AuthorityCode <= 0)
        {
            failures.Add($"Authority code was {coordinateSystem.AuthorityCode.ToString(CultureInfo.InvariantCulture)}, expected a positive EPSG SRID.");
        }
        else if (coordinateSystem.AuthorityCode != fixture.Srid)
        {
            failures.Add(
                $"Authority code was {coordinateSystem.AuthorityCode.ToString(CultureInfo.InvariantCulture)}, expected EPSG:{fixture.Srid.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystem.Name))
        {
            failures.Add("Name was empty.");
        }

        if (coordinateSystem is VerticalCoordinateSystem verticalCoordinateSystem)
        {
            if (verticalCoordinateSystem.Dimension != 1)
            {
                failures.Add($"Vertical coordinate system dimension was {verticalCoordinateSystem.Dimension.ToString(CultureInfo.InvariantCulture)}, expected 1.");
            }
        }
        else if (coordinateSystem.Dimension < 2)
        {
            failures.Add($"Dimension was {coordinateSystem.Dimension.ToString(CultureInfo.InvariantCulture)}, expected at least 2.");
        }

        switch (coordinateSystem)
        {
            case ProjectedCoordinateSystem projectedCoordinateSystem:
                IProjection? projection = projectedCoordinateSystem.Projection;
                if (projection is null)
                {
                    failures.Add("Projected coordinate system had no projection.");
                }

                break;
            case GeographicCoordinateSystem geographicCoordinateSystem:
                HorizontalDatum? datum = geographicCoordinateSystem.HorizontalDatum;
                if (datum is null)
                {
                    failures.Add("Geographic coordinate system had no datum.");
                    break;
                }

                Ellipsoid? ellipsoid = datum.Ellipsoid;
                if (ellipsoid is null)
                {
                    failures.Add("Geographic coordinate system datum had no ellipsoid.");
                }

                break;
        }

        return failures;
    }

    private static string BuildFixtureFailure(EpsgArchiveWktFixture fixture, string failure)
    {
        return
            $"{fixture.ArchiveEntryPath} (EPSG:{fixture.Srid.ToString(CultureInfo.InvariantCulture)}, {fixture.RootKeyword}) failed: {failure}";
    }
}
