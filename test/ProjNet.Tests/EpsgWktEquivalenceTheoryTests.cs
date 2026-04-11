// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

/// <summary>
/// Verifies semantic equivalence between generated EPSG WKT and committed fixtures.
/// </summary>
public class EpsgWktEquivalenceTheoryTests
{
    private const string FixtureRelativePath = "Generated/epsg-wkt-equivalence-fixture.json";
    private static readonly Regex SrsIdRegex = new("(?:AUTHORITY|ID)\\[\"EPSG\",\\s*\"?(?<id>\\d+)\"?\\]", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex EllipsoidRegex = new("ELLIPSOID\\[\"[^\"]+\",\\s*(?<semiMajor>[-+0-9.Ee]+),\\s*(?<inverseFlattening>[-+0-9.Ee]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex MethodRegex = new("(?:PROJECTION|METHOD)\\[\"(?<name>[^\"]+)\"", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ParameterRegex = new("PARAMETER\\[\"(?<name>[^\"]+)\",\\s*(?<value>[-+0-9.Ee]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Lazy<IReadOnlyList<EpsgFixtureRow>> FixtureRows = new(LoadFixtureRows);

    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Srid)
            .ToDictionary(group => group.Key, group => group.Last().Wkt));

    /// <summary>
    /// Enumerates fixture rows used by the WKT equivalence theory.
    /// </summary>
    /// <returns>SRID/WKT row pairs.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> EpsgFixtureRows()
    {
        foreach (EpsgFixtureRow row in FixtureRows.Value)
        {
            yield return new TheoryDataRow<int, string>(row.Srid, row.Wkt);
        }
    }

    /// <summary>
    /// Verifies that the committed EPSG WKT fixture covers at least 50 representative SRIDs with 10 examples per supported CRS kind.
    /// </summary>
    [Fact]
    public void EpsgFixtureShouldCoverFiftyRepresentativeCoordinateSystemsAcrossAllKinds()
    {
        IReadOnlyList<EpsgFixtureRow> rows = FixtureRows.Value;
        Assert.True(rows.Count >= 50, $"Expected at least 50 EPSG WKT fixture rows, but found {rows.Count}.");
        Assert.Equal(rows.Count, rows.Select(row => row.Srid).Distinct().Count());

        var counts = rows
            .GroupBy(row => GetCoordinateSystemKind(row.Srid))
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(10, GetKindCount(counts, EpsgCoordinateSystemKind.Geographic2D));
        Assert.Equal(10, GetKindCount(counts, EpsgCoordinateSystemKind.Projected));
        Assert.Equal(10, GetKindCount(counts, EpsgCoordinateSystemKind.Geocentric));
        Assert.Equal(10, GetKindCount(counts, EpsgCoordinateSystemKind.Vertical));
        Assert.Equal(10, GetKindCount(counts, EpsgCoordinateSystemKind.Compound));
    }

    /// <summary>
    /// Verifies that generated catalog WKT is equivalent to the committed fixture for a given SRID.
    /// </summary>
    /// <param name="srid">EPSG SRID.</param>
    /// <param name="expectedWkt">Expected WKT from fixture.</param>
    [Theory]
    [MemberData(nameof(EpsgFixtureRows))]
    public void GeneratedCatalogWktShouldBeEquivalentToCommittedEpsgFixture(int srid, string expectedWkt)
    {
        Assert.True(CatalogDefinitions.Value.TryGetValue(srid, out string? generatedWkt), $"SRID {srid} not found in managed EPSG catalog.");
        Assert.True(AreEquivalent(expectedWkt, generatedWkt, srid), $"WKT mismatch for SRID {srid}.");
    }

    private static bool AreEquivalent(string expectedWkt, string actualWkt, int srid)
    {
        if (string.Equals(Normalize(expectedWkt), Normalize(actualWkt), StringComparison.Ordinal))
        {
            return true;
        }

        long expectedSrid = TryExtractSrid(expectedWkt);
        long actualSrid = TryExtractSrid(actualWkt);
        if (expectedSrid != srid || actualSrid != srid)
        {
            return false;
        }

        if (!HasCompatibleRootType(expectedWkt, actualWkt))
        {
            return false;
        }

        if (!EllipsoidMatches(expectedWkt, actualWkt))
        {
            return false;
        }

        return NormalizeProjectionMethodName(ExtractMethodName(expectedWkt)) == NormalizeProjectionMethodName(ExtractMethodName(actualWkt)) && ProjectionParametersMatch(expectedWkt, actualWkt);
    }

    private static string Normalize(string wkt) => string.Concat(wkt.Where(c => !char.IsWhiteSpace(c)));

    private static long TryExtractSrid(string wkt)
    {
        MatchCollection matches = SrsIdRegex.Matches(wkt);
        if (matches.Count == 0)
        {
            return -1;
        }

        string id = matches[^1].Groups["id"].Value;
        return long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) ? parsed : -1;
    }

    private static bool HasCompatibleRootType(string expectedWkt, string actualWkt)
    {
        static string Root(string wkt)
        {
            if (wkt.StartsWith("PROJCRS[", StringComparison.OrdinalIgnoreCase) || wkt.StartsWith("PROJCS[", StringComparison.OrdinalIgnoreCase))
            {
                return "projected";
            }

            if (wkt.StartsWith("GEOGCRS[", StringComparison.OrdinalIgnoreCase) || wkt.StartsWith("GEOGCS[", StringComparison.OrdinalIgnoreCase))
            {
                return "geographic";
            }

            if (wkt.StartsWith("GEODCRS[", StringComparison.OrdinalIgnoreCase) || wkt.StartsWith("GEODETICCRS[", StringComparison.OrdinalIgnoreCase))
            {
                return wkt.Contains("CS[Cartesian", StringComparison.OrdinalIgnoreCase) ? "geocentric" : "geographic";
            }

            if (wkt.StartsWith("GEOCCRS[", StringComparison.OrdinalIgnoreCase) || wkt.StartsWith("GEOCCS[", StringComparison.OrdinalIgnoreCase))
            {
                return "geocentric";
            }

            if (wkt.StartsWith("VERTCRS[", StringComparison.OrdinalIgnoreCase) || wkt.StartsWith("VERT_CS[", StringComparison.OrdinalIgnoreCase))
            {
                return "vertical";
            }

            return wkt.StartsWith("COMPOUNDCRS[", StringComparison.OrdinalIgnoreCase) || wkt.StartsWith("COMPD_CS[", StringComparison.OrdinalIgnoreCase)
                ? "compound"
                : "unknown";
        }

        return string.Equals(Root(expectedWkt), Root(actualWkt), StringComparison.Ordinal);
    }

    private static bool EllipsoidMatches(string expectedWkt, string actualWkt)
    {
        Match expected = EllipsoidRegex.Match(expectedWkt);
        Match actual = EllipsoidRegex.Match(actualWkt);
        if (!expected.Success || !actual.Success)
        {
            return true;
        }

        double expectedSemiMajor = ParseInvariantDouble(expected.Groups["semiMajor"].Value);
        double actualSemiMajor = ParseInvariantDouble(actual.Groups["semiMajor"].Value);
        double expectedInvFlattening = ParseInvariantDouble(expected.Groups["inverseFlattening"].Value);
        double actualInvFlattening = ParseInvariantDouble(actual.Groups["inverseFlattening"].Value);
        return NearlyEqual(expectedSemiMajor, actualSemiMajor) && NearlyEqual(expectedInvFlattening, actualInvFlattening);
    }

    private static string ExtractMethodName(string wkt)
    {
        Match match = MethodRegex.Match(wkt);
        return match.Success ? match.Groups["name"].Value : string.Empty;
    }

    private static bool ProjectionParametersMatch(string expectedWkt, string actualWkt)
    {
        Dictionary<string, double> expected = ParseParameters(expectedWkt);
        if (expected.Count == 0)
        {
            return true;
        }

        Dictionary<string, double> actual = ParseParameters(actualWkt);
        foreach (KeyValuePair<string, double> pair in expected)
        {
            if (!actual.TryGetValue(pair.Key, out double actualValue))
            {
                return false;
            }

            if (!NearlyEqual(pair.Value, actualValue))
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, double> ParseParameters(string wkt)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in ParameterRegex.Matches(wkt))
        {
            string name = NormalizeProjectionParameterName(match.Groups["name"].Value);
            result[name] = ParseInvariantDouble(match.Groups["value"].Value);
        }

        return result;
    }

    private static string NormalizeProjectionMethodName(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            return string.Empty;
        }

        string normalized = methodName
            .ToLowerInvariant()
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace(".", "_", StringComparison.Ordinal)
            .Replace("__", "_", StringComparison.Ordinal);

        return normalized switch
        {
            "polar_stereographic_variant_a" => "polar_stereographic",
            "polar_stereographic_variant_b" => "polar_stereographic",
            _ => normalized,
        };
    }

    private static string NormalizeProjectionParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        string normalized = parameterName
            .ToLowerInvariant()
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace("-", "_", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal)
            .Replace(".", "_", StringComparison.Ordinal)
            .Replace("__", "_", StringComparison.Ordinal);

        return normalized switch
        {
            "longitude_of_natural_origin" => "central_meridian",
            "longitude_of_false_origin" => "central_meridian",
            "longitude_of_projection_centre" => "central_meridian",
            "longitude_of_origin" => "central_meridian",
            "latitude_of_natural_origin" => "latitude_of_origin",
            "latitude_of_false_origin" => "latitude_of_origin",
            "latitude_of_projection_centre" => "latitude_of_origin",
            "scale_factor_at_natural_origin" => "scale_factor",
            "scale_factor_at_projection_centre" => "scale_factor",
            "scale_factor_on_initial_line" => "scale_factor",
            "easting_at_false_origin" => "false_easting",
            "easting_at_projection_centre" => "false_easting",
            "northing_at_false_origin" => "false_northing",
            "northing_at_projection_centre" => "false_northing",
            "latitude_of_1st_standard_parallel" => "standard_parallel_1",
            "latitude_of_2nd_standard_parallel" => "standard_parallel_2",
            _ => normalized,
        };
    }

    private static double ParseInvariantDouble(string value)
    {
        return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) <= 1e-9;
    }

    private static IReadOnlyList<EpsgFixtureRow> LoadFixtureRows()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, FixtureRelativePath.Replace('/', Path.DirectorySeparatorChar));
        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));

        return [.. document.RootElement
            .EnumerateArray()
            .Select(item => new EpsgFixtureRow(
                item.GetProperty("srid").GetInt32(),
                item.GetProperty("wkt").GetString() ?? string.Empty))];
    }

    private static EpsgCoordinateSystemKind GetCoordinateSystemKind(int srid)
    {
        Assert.True(EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out EpsgCoordinateReferenceRecord reference, out _), $"SRID {srid} not found in managed EPSG catalog.");
        return reference.Kind;
    }

    private static int GetKindCount(Dictionary<EpsgCoordinateSystemKind, int> counts, EpsgCoordinateSystemKind kind)
    {
        return counts.TryGetValue(kind, out int count) ? count : 0;
    }

    private readonly record struct EpsgFixtureRow(int Srid, string Wkt);
}
