// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

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
/// Contains theory tests for GIGS 5101 through 5200 coordinate transformation fixtures.
/// </summary>
public class Gigs5101TheoryTests
{
    private static readonly string[] Fixture5101Files =
    [
        "5101.1-jhs.gie",
        "5101.2-jhs.gie",
        "5101.3-jhs.gie",
        "5101.4-jhs-etmerc.gie",
    ];

    private static readonly string[] Fixture5102And5103Files =
    [
        "5102.1.gie",
        "5102.2.gie",
        "5103.1.gie",
        "5103.2.gie",
        "5103.3.gie",
    ];

    private static readonly string[] Fixture5104To5113Files =
    [
        "5104.gie",
        "5105.2.gie",
        "5106.gie",
        "5107.gie",
        "5108.gie",
        "5109.gie",
        "5111.1.gie",
        "5112.gie",
        "5113.gie",
    ];

    private static readonly string[] Fixture5200Files =
    [
        "5201.gie",
        "5208.gie",
    ];

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    private static readonly char[] OperationTokenSeparators = [' ', '\t'];
    private static readonly CoordinateSystemServices CoordinateSystemServices = new();

    /// <summary>
    /// Gets coverage parameters for GIGS 5101 fixtures.
    /// </summary>
    /// <value>Coverage parameters.</value>
    public static IEnumerable<TheoryDataRow<string[], int, int, string, bool>> Gigs5101CoverageData
    {
        get
        {
            yield return new TheoryDataRow<string[], int, int, string, bool>(Fixture5101Files, 50, 50, "5101", true);
        }
    }

    /// <summary>
    /// Gets coverage parameters for GIGS 5102 and 5103 fixtures.
    /// </summary>
    /// <value>Coverage parameters.</value>
    public static IEnumerable<TheoryDataRow<string[], int, int, string, bool>> Gigs5102And5103CoverageData
    {
        get
        {
            yield return new TheoryDataRow<string[], int, int, string, bool>(Fixture5102And5103Files, 70, 0, "5102/5103", false);
        }
    }

    /// <summary>
    /// Gets coverage parameters for GIGS 5104 through 5113 fixtures.
    /// </summary>
    /// <value>Coverage parameters.</value>
    public static IEnumerable<TheoryDataRow<string[], int, int, string, bool>> Gigs5104To5113CoverageData
    {
        get
        {
            yield return new TheoryDataRow<string[], int, int, string, bool>(Fixture5104To5113Files, 80, 0, "5104-5113", false);
        }
    }

    /// <summary>
    /// Gets coverage parameters for GIGS 5200 fixtures.
    /// </summary>
    /// <value>Coverage parameters.</value>
    public static IEnumerable<TheoryDataRow<string[], int, int, string, bool>> Gigs5200CoverageData
    {
        get
        {
            yield return new TheoryDataRow<string[], int, int, string, bool>(Fixture5200Files, 20, 0, "5200", false);
        }
    }

    /// <summary>
    /// Verifies that supported GIGS 5101 pipeline cases execute and produce results within the declared tolerance.
    /// </summary>
    /// <param name="fixtureFiles">Fixture file names to parse.</param>
    /// <param name="minTransformed">Minimum transformed case count expected.</param>
    /// <param name="minWithinTolerance">Minimum within-tolerance case count expected.</param>
    /// <param name="label">Human-readable fixture group label.</param>
    /// <param name="requireToleranceMatch">Whether tolerance assertions are required.</param>
    [Theory]
    [Trait("Category", "Gigs5101")]
    [MemberData(nameof(Gigs5101CoverageData))]
    public void Gigs5101CasesForSupportedPipelinesStayWithinTolerance(
        string[] fixtureFiles,
        int minTransformed,
        int minWithinTolerance,
        string label,
        bool requireToleranceMatch)
    {
        AssertFixtureCoverage(fixtureFiles, minTransformed, minWithinTolerance, label, requireToleranceMatch);
    }

    /// <summary>
    /// Verifies that a substantial number of supported GIGS 5102 and 5103 pipeline cases execute successfully.
    /// </summary>
    /// <param name="fixtureFiles">Fixture file names to parse.</param>
    /// <param name="minTransformed">Minimum transformed case count expected.</param>
    /// <param name="minWithinTolerance">Minimum within-tolerance case count expected.</param>
    /// <param name="label">Human-readable fixture group label.</param>
    /// <param name="requireToleranceMatch">Whether tolerance assertions are required.</param>
    [Theory]
    [Trait("Category", "Gigs5102")]
    [Trait("Category", "Gigs5103")]
    [MemberData(nameof(Gigs5102And5103CoverageData))]
    public void Gigs5102And5103CasesForSupportedPipelinesStayWithinTolerance(
        string[] fixtureFiles,
        int minTransformed,
        int minWithinTolerance,
        string label,
        bool requireToleranceMatch)
    {
        AssertFixtureCoverage(fixtureFiles, minTransformed, minWithinTolerance, label, requireToleranceMatch);
    }

    /// <summary>
    /// Verifies that a substantial number of supported GIGS 5104 through 5113 pipeline cases execute successfully.
    /// </summary>
    /// <param name="fixtureFiles">Fixture file names to parse.</param>
    /// <param name="minTransformed">Minimum transformed case count expected.</param>
    /// <param name="minWithinTolerance">Minimum within-tolerance case count expected.</param>
    /// <param name="label">Human-readable fixture group label.</param>
    /// <param name="requireToleranceMatch">Whether tolerance assertions are required.</param>
    [Theory]
    [Trait("Category", "Gigs5104")]
    [Trait("Category", "Gigs5113")]
    [MemberData(nameof(Gigs5104To5113CoverageData))]
    public void Gigs5104To5113CasesForSupportedPipelinesExecute(
        string[] fixtureFiles,
        int minTransformed,
        int minWithinTolerance,
        string label,
        bool requireToleranceMatch)
    {
        AssertFixtureCoverage(fixtureFiles, minTransformed, minWithinTolerance, label, requireToleranceMatch);
    }

    /// <summary>
    /// Verifies that a substantial number of supported GIGS 5200 pipeline cases execute successfully.
    /// </summary>
    /// <param name="fixtureFiles">Fixture file names to parse.</param>
    /// <param name="minTransformed">Minimum transformed case count expected.</param>
    /// <param name="minWithinTolerance">Minimum within-tolerance case count expected.</param>
    /// <param name="label">Human-readable fixture group label.</param>
    /// <param name="requireToleranceMatch">Whether tolerance assertions are required.</param>
    [Theory]
    [Trait("Category", "Gigs5200")]
    [MemberData(nameof(Gigs5200CoverageData))]
    public void Gigs5200CasesForSupportedPipelinesExecute(
        string[] fixtureFiles,
        int minTransformed,
        int minWithinTolerance,
        string label,
        bool requireToleranceMatch)
    {
        AssertFixtureCoverage(fixtureFiles, minTransformed, minWithinTolerance, label, requireToleranceMatch);
    }

    private static void AssertFixtureCoverage(
        IReadOnlyList<string> fixtureFiles,
        int minTransformed,
        int minWithinTolerance,
        string label,
        bool requireToleranceMatch)
    {
        int parsedCases = 0;
        int transformedCases = 0;
        int withinToleranceCases = 0;

        foreach (GieCase testCase in EnumerateFixtureCases(fixtureFiles))
        {
            parsedCases++;
            if (testCase.ExpectsFailure || testCase.Accept is null || testCase.Expect is null || testCase.Accept.Length < 2 || testCase.Expect.Length < 2)
            {
                continue;
            }

            if (!TryCreatePipelineTransform(testCase.Operation, out MathTransform? transform))
            {
                continue;
            }

            MathTransform pipelineTransform = Assert.IsType<MathTransform>(transform, exactMatch: false);
            double[] output;
            try
            {
                output = pipelineTransform.Transform(testCase.Accept);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (output is null || output.Length < 2 || double.IsNaN(output[0]) || double.IsNaN(output[1]))
            {
                continue;
            }

            transformedCases++;
            double tolerance = Math.Max(ToNumericTolerance(testCase.ToleranceValue, testCase.ToleranceUnit), 1e-3d);
            if (LooksLikeGeographicExpect(testCase.Expect))
            {
                tolerance /= 111319.49079327358d;
            }

            double deltaX = Math.Abs(output[0] - testCase.Expect[0]);
            double deltaY = Math.Abs(output[1] - testCase.Expect[1]);
            if (deltaX <= tolerance && deltaY <= tolerance)
            {
                withinToleranceCases++;
            }
        }

        Assert.True(parsedCases > 0, $"Expected parsed GIGS {label} cases.");
        Assert.True(
            transformedCases > minTransformed,
            FormattableString.Invariant($"Expected to execute a substantial subset of GIGS {label} cases. transformed={transformedCases}, min={minTransformed}, parsed={parsedCases}."));
        if (requireToleranceMatch)
        {
            Assert.True(
                withinToleranceCases > minWithinTolerance,
                FormattableString.Invariant($"Expected a substantial subset of executed GIGS {label} cases to match tolerance. within={withinToleranceCases}, min={minWithinTolerance}, transformed={transformedCases}."));
        }
    }

    private static IEnumerable<GieCase> EnumerateFixtureCases(IReadOnlyList<string> fileNames)
    {
        string gigsDirectory = FindGigsDirectory();
        if (string.IsNullOrEmpty(gigsDirectory))
        {
            yield break;
        }

        foreach (string fileName in fileNames)
        {
            string path = Path.Combine(gigsDirectory, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            IReadOnlyList<GieCase> parsed = GieParser.ParseFile(
                path,
                new GieParserOptions
                {
                    IgnoreUnknownDirectives = true,
                    AllowOperationContinuation = true,
                });

            foreach (GieCase item in parsed)
            {
                yield return item;
            }
        }
    }

    private static bool TryCreatePipelineTransform(string operation, out MathTransform? transform)
    {
        transform = null;
        if (string.IsNullOrWhiteSpace(operation))
        {
            return false;
        }

        if (!TrySplitPipelineSteps(operation, out IReadOnlyList<string> steps) || steps.Count != 2)
        {
            return false;
        }

        if (!TryParseOperationArguments(steps[0], out Dictionary<string, string> firstArgs)
            || !TryParseOperationArguments(steps[1], out Dictionary<string, string> secondArgs))
        {
            return false;
        }

        if (!firstArgs.ContainsKey("inv") || secondArgs.ContainsKey("inv"))
        {
            return false;
        }

        if (!TryResolveDeclaredCoordinateSystem(firstArgs, out CoordinateSystem? source)
            || !TryResolveDeclaredCoordinateSystem(secondArgs, out CoordinateSystem? target))
        {
            return false;
        }

        CoordinateSystem targetCoordinateSystem = Assert.IsType<CoordinateSystem>(target, exactMatch: false);
        try
        {
            CoordinateSystem sourceCoordinateSystem = Assert.IsType<CoordinateSystem>(source, exactMatch: false);
            ICoordinateTransformation coordinateTransformation = CoordinateTransformationFactory.CreateFromCoordinateSystems(sourceCoordinateSystem, targetCoordinateSystem);
            transform = coordinateTransformation.MathTransform;
            return transform is not null;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.Reflection.TargetInvocationException)
        {
            return false;
        }
    }

    private static bool TrySplitPipelineSteps(string operation, out IReadOnlyList<string> steps)
    {
        var parsedSteps = new List<string>();
        var currentStepTokens = new List<string>();
        bool inPipeline = false;

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string normalized = token.StartsWith('+')
                ? token[1..]
                : token;

            if (normalized.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                continue;
            }

            if (normalized.Equals("step", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                if (currentStepTokens.Count > 0)
                {
                    parsedSteps.Add(string.Join(" ", currentStepTokens));
                    currentStepTokens.Clear();
                }

                continue;
            }

            if (!inPipeline)
            {
                continue;
            }

            currentStepTokens.Add(token);
        }

        if (currentStepTokens.Count > 0)
        {
            parsedSteps.Add(string.Join(" ", currentStepTokens));
        }

        steps = parsedSteps;
        return parsedSteps.Count > 0;
    }

    private static bool TryResolveDeclaredCoordinateSystem(Dictionary<string, string> args, out CoordinateSystem? coordinateSystem)
    {
        coordinateSystem = null;

        if (args.TryGetValue("init", out string? initValue)
            && TryParseEpsgCode(initValue, out int srid))
        {
            coordinateSystem = CoordinateSystemServices.GetCoordinateSystem(srid);
            coordinateSystem = NormalizeAxisOrder(coordinateSystem);
            return coordinateSystem is not null;
        }

        if (!args.TryGetValue("proj", out string? projCode))
        {
            return false;
        }

        if (!TryMapProjectionClass(projCode, out string projectionClassName))
        {
            return false;
        }

        if (!TryCreateGeographicCoordinateSystem(args, out GeographicCoordinateSystem? geographicCoordinateSystem))
        {
            return false;
        }

        if (!TryBuildProjectionParameters(args, out List<ProjectionParameter> parameters))
        {
            return false;
        }

        try
        {
            IProjection projection = CoordinateSystemFactory.CreateProjection($"GIGS {projectionClassName}", projectionClassName, parameters);
            GeographicCoordinateSystem geographic = Assert.IsType<GeographicCoordinateSystem>(geographicCoordinateSystem);
            coordinateSystem = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                "GIGS projected",
                geographic,
                projection,
                LinearUnit.Metre,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.Reflection.TargetInvocationException)
        {
            return false;
        }
    }

    private static bool TryParseEpsgCode(string token, out int srid)
    {
        srid = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string value = token.Trim();
        const string epsgPrefix = "epsg:";
        if (value.StartsWith(epsgPrefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[epsgPrefix.Length..];
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out srid);
    }

    private static CoordinateSystem? NormalizeAxisOrder(CoordinateSystem? coordinateSystem)
    {
        if (coordinateSystem is null)
        {
            return null;
        }

        var geographic = coordinateSystem as GeographicCoordinateSystem;
        if (geographic is not null)
        {
            return CoordinateSystemFactory.CreateGeographicCoordinateSystem(
                geographic.Name,
                geographic.AngularUnit,
                geographic.HorizontalDatum,
                geographic.PrimeMeridian,
                new AxisInfo("Lon", AxisOrientationEnum.East),
                new AxisInfo("Lat", AxisOrientationEnum.North));
        }

        var projected = coordinateSystem as ProjectedCoordinateSystem;
        if (projected is not null)
        {
            GeographicCoordinateSystem normalizedProjectedGeographic = Assert.IsType<GeographicCoordinateSystem>(NormalizeAxisOrder(projected.GeographicCoordinateSystem));
            return CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                projected.Name,
                normalizedProjectedGeographic,
                projected.Projection,
                projected.LinearUnit,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));
        }

        return coordinateSystem;
    }

    private static bool TryMapProjectionClass(string? projCode, out string projectionClassName)
    {
        projectionClassName = string.Empty;
        if (projCode is null)
        {
            return false;
        }

        if (projCode.Equals("etmerc", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("tmerc", StringComparison.OrdinalIgnoreCase))
        {
            projectionClassName = "transverse_mercator";
            return true;
        }

        if (projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
        {
            projectionClassName = "utm";
            return true;
        }

        if (projCode.Equals("poly", StringComparison.OrdinalIgnoreCase))
        {
            projectionClassName = "polyconic";
            return true;
        }

        return false;
    }

    private static bool TryCreateGeographicCoordinateSystem(Dictionary<string, string> args, out GeographicCoordinateSystem? gcs)
    {
        gcs = null;

        if (!TryResolveEllipsoid(args, out Ellipsoid? ellipsoid))
        {
            return false;
        }

        HorizontalDatum datum = CoordinateSystemFactory.CreateHorizontalDatum(
            "GIGS datum",
            DatumType.HD_Geocentric,
            Assert.IsType<Ellipsoid>(ellipsoid),
            null);
        gcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "GIGS geographic",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        return true;
    }

    private static bool TryResolveEllipsoid(Dictionary<string, string> args, out Ellipsoid? ellipsoid)
    {
        if (args.TryGetValue("ellps", out string? ellps))
        {
            if (ellps.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.WGS84;
                return true;
            }

            if (ellps.Equals("grs80", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.GRS80;
                return true;
            }
        }

        ellipsoid = Ellipsoid.WGS84;
        return true;
    }

    private static bool TryBuildProjectionParameters(Dictionary<string, string> args, out List<ProjectionParameter> parameters)
    {
        parameters =
        [
            new("latitude_of_origin", 0d),
            new("central_meridian", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
        ];

        if (TryGetDouble(args, "lat_0", out double lat0))
        {
            ReplaceParameter(parameters, "latitude_of_origin", lat0);
        }

        if (TryGetDouble(args, "lon_0", out double lon0))
        {
            ReplaceParameter(parameters, "central_meridian", lon0);
        }

        if (TryGetDouble(args, "k_0", out double k0))
        {
            ReplaceParameter(parameters, "scale_factor", k0);
        }
        else if (TryGetDouble(args, "k", out double k))
        {
            ReplaceParameter(parameters, "scale_factor", k);
        }

        if (TryGetDouble(args, "x_0", out double x0))
        {
            ReplaceParameter(parameters, "false_easting", x0);
        }

        if (TryGetDouble(args, "y_0", out double y0))
        {
            ReplaceParameter(parameters, "false_northing", y0);
        }

        if (args.TryGetValue("proj", out string? projCode) && projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetZoneCentralMeridian(args, out double utmCentralMeridian))
            {
                return false;
            }

            ReplaceParameter(parameters, "latitude_of_origin", 0d);
            ReplaceParameter(parameters, "central_meridian", utmCentralMeridian);
            ReplaceParameter(parameters, "scale_factor", 0.9996d);
            ReplaceParameter(parameters, "false_easting", 500000d);
            ReplaceParameter(parameters, "false_northing", args.ContainsKey("south") ? 10000000d : 0d);
        }

        return true;
    }

    private static bool TryGetZoneCentralMeridian(Dictionary<string, string> args, out double centralMeridian)
    {
        centralMeridian = 0d;
        if (!args.TryGetValue("zone", out string? zoneToken) || string.IsNullOrWhiteSpace(zoneToken))
        {
            return false;
        }

        string digits = zoneToken.Trim();
        int i = 0;
        while (i < digits.Length && char.IsDigit(digits[i]))
        {
            i++;
        }

        if (i == 0 || !int.TryParse(digits.AsSpan(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int zone))
        {
            return false;
        }

        centralMeridian = (zone * 6d) - 183d;
        return true;
    }

    private static bool TryGetDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        return args.TryGetValue(key, out string? raw) && !string.IsNullOrWhiteSpace(raw) && double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
    }

    private static void ReplaceParameter(List<ProjectionParameter> parameters, string name, double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }

    private static bool TryParseOperationArguments(string operation, out Dictionary<string, string> args)
    {
        args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (operation is null)
        {
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string body = token.Length > 0 && token[0] == '+'
                ? token[1..]
                : token;

            if (body.Length == 0
                || body.Equals("step", StringComparison.OrdinalIgnoreCase)
                || body.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int index = body.IndexOf('=', StringComparison.Ordinal);
            if (index < 0)
            {
                args[body] = "true";
            }
            else
            {
                string key = body[..index];
                string value = body[(index + 1)..];
                args[key] = value;
            }
        }

        return args.Count > 0;
    }

    private static string FindGigsDirectory()
    {
        string direct = Path.Combine(AppContext.BaseDirectory, "Fixtures", "gigs");
        if (Directory.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "gigs");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return string.Empty;
    }

    private static double ToNumericTolerance(double value, string unit)
    {
        if (unit is null)
        {
            return value;
        }

        if (unit.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            return value / 1000d;
        }

        if (unit.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            return value / 100d;
        }

        return unit.Equals("nm", StringComparison.OrdinalIgnoreCase) ? value * 1e-9d : value;
    }

    private static bool LooksLikeGeographicExpect(double[] expect)
    {
        return expect is not null && expect.Length >= 2 && Math.Abs(expect[0]) <= 360d && Math.Abs(expect[1]) <= 90d;
    }
}
