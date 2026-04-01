// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies DHDN Gauß-Krüger to ETRS89 UTM fixture cases against the BETA2007 NTv2 grid.
/// </summary>
public sealed class DhdnGkToUtmTheoryTests
{
    private const double FixtureToleranceMetres = 0.001d;
    private const string FixtureMarker = "Tests for GK system zones to UTM32/33 not implemented yet";
    private static readonly CoordinateSystemServices Css = new(CoordinateSystemServicesTests.LoadCsv());
    private static readonly CoordinateTransformationFactory TransformationFactory = new();
    private static readonly Dictionary<(int SourceSrid, int TargetSrid), ICoordinateTransformation> TransformationCache = [];
    private static readonly object TransformationCacheSync = new();

    /// <summary>
    /// Provides the projected GK-to-UTM fixture rows.
    /// </summary>
    /// <returns>Parsed fixture rows with source/target SRIDs and expected coordinates.</returns>
    public static IEnumerable<TheoryDataRow<int, int, double, double, double, double, string>> GetProjectedCases()
    {
        foreach (ProjectedFixtureCase testCase in ParseProjectedCases())
        {
            yield return new TheoryDataRow<int, int, double, double, double, double, string>(
                testCase.SourceSrid,
                testCase.TargetSrid,
                testCase.InputX,
                testCase.InputY,
                testCase.ExpectedX,
                testCase.ExpectedY,
                $"{testCase.SourceTag}->{testCase.TargetTag}");
        }
    }

    /// <summary>
    /// Verifies that the projected section contributes the expected 94 GK-to-UTM cases.
    /// </summary>
    [Fact]
    public void ProjectedFixtureSectionContainsNinetyFourCases()
    {
        int caseCount = 0;
        foreach (ProjectedFixtureCase _ in ParseProjectedCases())
        {
            caseCount++;
        }

        Assert.Equal(94, caseCount);
    }

    /// <summary>
    /// Verifies that all projected GK-to-UTM fixture cases are reproducible with the resolved BETA2007 grid-backed SRID transformations.
    /// </summary>
    /// <param name="sourceSrid">Source projected SRID.</param>
    /// <param name="targetSrid">Target projected SRID.</param>
    /// <param name="inputX">Source easting.</param>
    /// <param name="inputY">Source northing.</param>
    /// <param name="expectedX">Expected target easting.</param>
    /// <param name="expectedY">Expected target northing.</param>
    /// <param name="caseLabel">Human-readable case label for assertion output.</param>
    [Theory]
    [MemberData(nameof(GetProjectedCases))]
    public void ProjectedFixtureCaseMatchesBETA2007GridBackedTransformation(
        int sourceSrid,
        int targetSrid,
        double inputX,
        double inputY,
        double expectedX,
        double expectedY,
        string caseLabel)
    {
        ICoordinateTransformation transformation = CreateGridBackedTransformation(sourceSrid, targetSrid);
        double[] output = transformation.MathTransform.Transform([inputX, inputY, 0d]);

        double deltaX = Math.Abs(output[0] - expectedX);
        double deltaY = Math.Abs(output[1] - expectedY);

        Assert.True(
            deltaX <= FixtureToleranceMetres,
            FormattableString.Invariant($"{caseLabel}: expected X delta <= {FixtureToleranceMetres:R} m but was {deltaX:R}."));
        Assert.True(
            deltaY <= FixtureToleranceMetres,
            FormattableString.Invariant($"{caseLabel}: expected Y delta <= {FixtureToleranceMetres:R} m but was {deltaY:R}."));
    }

    private static IEnumerable<ProjectedFixtureCase> ParseProjectedCases()
    {
        const int sridDhdnGk2 = 31466;
        const int sridDhdnGk3 = 31467;
        const int sridDhdnGk4 = 31468;
        const int sridDhdnGk5 = 31469;
        const int sridEtrs89Utm32 = 25832;
        const int sridEtrs89Utm33 = 25833;

        Dictionary<string, int> sridsByTag = new(StringComparer.Ordinal)
        {
            ["DE_DHDN_3GK2"] = sridDhdnGk2,
            ["DE_DHDN_3GK3"] = sridDhdnGk3,
            ["DE_DHDN_3GK4"] = sridDhdnGk4,
            ["DE_DHDN_3GK5"] = sridDhdnGk5,
            ["ETRS89_UTM32"] = sridEtrs89Utm32,
            ["ETRS89_UTM33"] = sridEtrs89Utm33,
        };

        string fixturePath = FindDhdnFixturePath();
        string[] lines = File.ReadAllLines(fixturePath);
        bool inProjectedSection = false;
        TaggedCoordinate? pendingAccept = null;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!inProjectedSection)
            {
                if (trimmed.Equals(FixtureMarker, StringComparison.Ordinal))
                {
                    inProjectedSection = true;
                }

                continue;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            if (trimmed.StartsWith("accept", StringComparison.OrdinalIgnoreCase))
            {
                pendingAccept = ParseTaggedCoordinate(line, "accept");
                continue;
            }

            if (!trimmed.StartsWith("expect", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TaggedCoordinate accept = pendingAccept ?? throw new FormatException("Encountered projected expect line without a preceding accept line.");
            TaggedCoordinate expect = ParseTaggedCoordinate(line, "expect");

            if (!sridsByTag.TryGetValue(accept.Tag, out int sourceSrid))
            {
                throw new FormatException(FormattableString.Invariant($"Unknown projected source tag '{accept.Tag}'."));
            }

            if (!sridsByTag.TryGetValue(expect.Tag, out int targetSrid))
            {
                throw new FormatException(FormattableString.Invariant($"Unknown projected target tag '{expect.Tag}'."));
            }

            yield return new ProjectedFixtureCase(
                sourceSrid,
                targetSrid,
                accept.X,
                accept.Y,
                expect.X,
                expect.Y,
                accept.Tag,
                expect.Tag);

            pendingAccept = null;
        }

        if (pendingAccept is not null)
        {
            throw new FormatException("Projected GK-to-UTM fixture ended with an unmatched accept line.");
        }
    }

    private static ICoordinateTransformation CreateGridBackedTransformation(int sourceSrid, int targetSrid)
    {
        lock (TransformationCacheSync)
        {
            if (TransformationCache.TryGetValue((sourceSrid, targetSrid), out ICoordinateTransformation? cachedTransformation))
            {
                return cachedTransformation;
            }

            ProjectedCoordinateSystem source = Assert.IsType<ProjectedCoordinateSystem>(
                Css.GetCoordinateSystem(sourceSrid),
                exactMatch: false);
            ProjectedCoordinateSystem target = Assert.IsType<ProjectedCoordinateSystem>(
                Css.GetCoordinateSystem(targetSrid),
                exactMatch: false);
            string gridPath = Path.Combine(FindProjGridDirectory(), "BETA2007.gsb");

            ConcatenatedTransform concatenatedTransform = new();
            concatenatedTransform.CoordinateTransformationList.Add(
                TransformationFactory.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
            concatenatedTransform.CoordinateTransformationList.Add(
                new CoordinateTransformation(
                    source.GeographicCoordinateSystem,
                    target.GeographicCoordinateSystem,
                    TransformType.Transformation,
                    new Ntv2HGridShiftMathTransform([gridPath]),
                    "NTv2",
                    "EPSG",
                    15948,
                    string.Empty,
                    $"Grid: {gridPath}"));
            concatenatedTransform.CoordinateTransformationList.Add(
                TransformationFactory.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));

            CoordinateTransformation transformation = new(
                source,
                target,
                TransformType.Transformation,
                concatenatedTransform,
                "DHDN GK to ETRS89 UTM via BETA2007",
                "EPSG",
                15948,
                string.Empty,
                $"Grid: {gridPath}");
            TransformationCache[(sourceSrid, targetSrid)] = transformation;
            return transformation;
        }
    }

    private static TaggedCoordinate ParseTaggedCoordinate(string line, string keyword)
    {
        int commentIndex = line.IndexOf('#', StringComparison.Ordinal);
        if (commentIndex < 0)
        {
            throw new FormatException(FormattableString.Invariant($"Projected fixture line is missing a tag comment: '{line}'."));
        }

        string coordinatePart = line[..commentIndex];
        string[] tokens = coordinatePart.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 3 || !tokens[0].Equals(keyword, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(FormattableString.Invariant($"Could not parse projected fixture line: '{line}'."));
        }

        return new TaggedCoordinate(
            double.Parse(tokens[1], CultureInfo.InvariantCulture),
            double.Parse(tokens[2], CultureInfo.InvariantCulture),
            line[(commentIndex + 1)..].Trim());
    }

    private static string FindDhdnFixturePath()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "gie", "DHDN_ETRS89.gie");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate the DHDN_ETRS89.gie fixture under test\\ProjNet.Tests\\Fixtures\\gie.", "DHDN_ETRS89.gie");
    }

    private static string FindProjGridDirectory()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "spec", "PROJ", "data", "tests");
            if (File.Exists(Path.Combine(candidate, "BETA2007.gsb")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate spec\\PROJ\\data\\tests with BETA2007.gsb.");
    }

    private sealed record TaggedCoordinate(double X, double Y, string Tag);

    private sealed record ProjectedFixtureCase(
        int SourceSrid,
        int TargetSrid,
        double InputX,
        double InputY,
        double ExpectedX,
        double ExpectedY,
        string SourceTag,
        string TargetTag);
}
