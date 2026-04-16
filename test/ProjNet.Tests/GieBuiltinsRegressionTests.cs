// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for GIE builtins harness behavior.
/// </summary>
public class GieBuiltinsRegressionTests
{
    private const double MercatorLatitudeSpherificationTolerance = 1e-8d;
    private static readonly HashSet<int> FormerHarnessOnlyFixtureLines =
    [
        60, 128, 130, 147, 163, 205, 209, 253, 264, 268, 320, 404, 411, 414, 416, 428, 447, 506,
        659, 666, 1301, 1945, 1961, 2302, 2364, 3321, 3333, 3381, 4032, 4042, 4278, 4280, 5020,
        5030, 5087, 5118, 5227, 5250, 5290, 5806, 5813, 6900, 6902, 6904, 6906, 7110, 7162, 7570,
    ];

    /// <summary>
    /// Gets the former harness-only builtins rows that are now asserted directly in this regression suite.
    /// </summary>
    /// <returns>The replacement direct-assertion test data rows.</returns>
    public static IEnumerable<object?[]> GetFormerHarnessOnlyCases()
    {
        foreach (GieCase testCase in GetFormerHarnessOnlyCasesCore())
        {
            yield return [testCase];
        }
    }

    /// <summary>
    /// Verifies that inverse Equal Earth cases execute via inverse transform semantics instead of being skipped.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithInverseEqearthCaseReturnsExpectedGeographicCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 656,
            Operation = "+proj=eqearth +R=6378137",
            ToleranceValue = 1d,
            ToleranceUnit = "cm",
            Direction = GieDirection.Inverse,
            Accept = [17263256.84d, 0d],
            Expect = [180d, 0d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        MathTransform mathTransform = Assert.IsAssignableFrom<MathTransform>(transform);
        double[] output = mathTransform.Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 7);
        Assert.Equal(testCase.Expect[1], output[1], 8);
    }

    /// <summary>
    /// Verifies that inverse projection cases executed through the conversion harness apply inverse direction semantics.
    /// </summary>
    /// <param name="operation">Projection operation under test.</param>
    /// <param name="x">Projected X coordinate.</param>
    /// <param name="y">Projected Y coordinate.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    /// <param name="expectedLatitude">Expected latitude in degrees.</param>
    /// <param name="tolerance">Allowed inverse tolerance.</param>
    [Theory]
    [InlineData("+proj=aea +ellps=GRS80 +lat_1=0 +lat_2=2", 16468399.3582d, 5275043.9815d, 150d, 50d, 5e-8d)]
    [InlineData("+proj=cea +ellps=GRS80", 16697923.6190d, 4865983.5552d, 150d, 50d, 1e-8d)]
    public void TryCreateConversionTransformForDirectionWithInverseProjectionCaseReturnsExpectedCoordinate(
        string operation,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([x, y]);
        Assert.InRange(Math.Abs(output[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(output[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that spherical builtins operations using only <c>+a</c> keep that radius instead of falling back to WGS 84.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithGnSinuSemiMajorOnlyCaseReturnsExpectedProjectedCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 2223,
            Operation = "+proj=gn_sinu +a=6400000 +m=1 +n=2",
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [2d, 1d],
            Expect = [223385.132504696d, 111698.236447187d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        MathTransform mathTransform = Assert.IsAssignableFrom<MathTransform>(transform);
        double[] output = mathTransform.Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 9);
        Assert.Equal(testCase.Expect[1], output[1], 9);
    }

    /// <summary>
    /// Verifies that <c>to_meter</c> ratio expressions are honored by the builtins harness.
    /// </summary>
    [Fact]
    public void TryGetDoubleParsesToMeterRatioExpression()
    {
        MethodInfo parseOperationArgumentsMethod = typeof(GieBuiltinsTheoryTests).GetMethod("TryParseOperationArguments", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryParseOperationArguments.");
        object?[] parseArgs = ["proj=utm ellps=GRS80 zone=32 to_meter=2.0/0.2", null];
        bool parsed = Assert.IsType<bool>(parseOperationArgumentsMethod.Invoke(null, parseArgs));
        Assert.True(parsed);

        Dictionary<string, string> operationArgs = Assert.IsType<Dictionary<string, string>>(parseArgs[1]);
        MethodInfo tryGetDoubleMethod = typeof(GieBuiltinsTheoryTests).GetMethod("TryGetDouble", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryGetDouble.");
        object?[] valueArgs = [operationArgs, "to_meter", null];
        bool parsedToMeter = Assert.IsType<bool>(tryGetDoubleMethod.Invoke(null, valueArgs));

        Assert.True(parsedToMeter);
        double toMeterValue = Assert.IsType<double>(valueArgs[2]);
        Assert.Equal(10d, toMeterValue, 12);
    }

    /// <summary>
    /// Verifies that synthetic UTM false offsets are expressed in the configured output units in the GIE builtins harness.
    /// </summary>
    /// <param name="operation">UTM operation under test.</param>
    [Theory]
    [InlineData("proj=utm ellps=GRS80 zone=32 to_meter=10")]
    [InlineData("proj=utm ellps=GRS80 zone=32 to_meter=2.0/0.2")]
    public void TryCreateTransformWithUtmCustomOutputUnitsReturnsExpectedProjectedCoordinate(string operation)
    {
        var testCase = new GieCase
        {
            LineNumber = 518,
            Operation = operation,
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [12d, 55d],
            Expect = [69187.5632d, 609890.7825d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        MathTransform mathTransform = Assert.IsAssignableFrom<MathTransform>(transform);
        double[] output = mathTransform.Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 4);
        Assert.Equal(testCase.Expect[1], output[1], 4);
    }

    /// <summary>
    /// Verifies that the builtins harness applies explicit ellipsoid shape overrides after resolving a named ellipsoid.
    /// </summary>
    /// <param name="operation">UTM operation under test.</param>
    /// <param name="equivalentOperation">Equivalent UTM operation with the ellipsoid shape specified directly.</param>
    /// <param name="baselineOperation">UTM operation that keeps the original named ellipsoid without an explicit shape override.</param>
    [Theory]
    [InlineData("proj=utm ellps=GRS80 zone=32 b=6000000", "proj=utm a=6378137 zone=32 b=6000000", "proj=utm ellps=GRS80 zone=32")]
    [InlineData("proj=utm ellps=GRS80 zone=32 rf=300", "proj=utm a=6378137 zone=32 rf=300", "proj=utm ellps=GRS80 zone=32")]
    [InlineData("proj=utm ellps=GRS80 zone=32 f=0.00333333333333", "proj=utm a=6378137 zone=32 f=0.00333333333333", "proj=utm ellps=GRS80 zone=32")]
    public void TryCreateTransformWithUtmEllipsoidOverridesReturnsExpectedProjectedCoordinate(
        string operation,
        string equivalentOperation,
        string baselineOperation)
    {
        double[] output = RequireBuiltinsProjectedOutput(operation);
        double[] equivalentOutput = RequireBuiltinsProjectedOutput(equivalentOperation);
        double[] baselineOutput = RequireBuiltinsProjectedOutput(baselineOperation);

        Assert.Equal(equivalentOutput[0], output[0], 12);
        Assert.Equal(equivalentOutput[1], output[1], 12);
        Assert.NotEqual(baselineOutput[0], output[0], 9);
        Assert.NotEqual(baselineOutput[1], output[1], 9);
    }

    /// <summary>
    /// Verifies that the runtime UTM conversion path uses the exact transverse Mercator kernel for explicit shape overrides, matching the GIE reference values.
    /// </summary>
    [Theory]
    [InlineData("proj=utm ellps=GRS80 zone=32 b=6000000", 699293.0880d, 5674591.5295d)]
    [InlineData("proj=utm a=6400000 zone=32 b=6000000", 700416.5900d, 5669475.8884d)]
    public void TryCreateConversionTransformWithUtmShapeOverridesMatchesGieReference(
        string operation,
        double expectedX,
        double expectedY)
    {
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, 12d, 55d);

        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, 0.0005d);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, 0.0005d);
    }

    /// <summary>
    /// Verifies that legacy non-pipeline <c>geoidgrids</c> operations are normalized into executable runtime pipelines.
    /// </summary>
    /// <param name="operation">Legacy geoidgrids operation under test.</param>
    [Theory]
    [InlineData("proj=latlong geoidgrids=egm96_15.gtx ellps=GRS80")]
    [InlineData("proj=merc geoidgrids=egm96_15.gtx axis=sue ellps=GRS80")]
    [InlineData("+proj=latlong +ellps=WGS84 +geoidgrids=tests/test_nodata.gtx")]
    public void TryIsRuntimeOperationSupportedRecognizesLegacyGeoidGridOperations(string operation)
    {
        Assert.True(TryIsRuntimeOperationSupported(operation));
    }

    /// <summary>
    /// Verifies that legacy geographic <c>geoidgrids</c> operations apply the expected forward vertical shift.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLegacyLatlongGeoidGridsReturnsExpectedVerticalShift()
    {
        double[] output = RequireBuiltinsRuntimeProjectedOutput("proj=latlong geoidgrids=egm96_15.gtx ellps=GRS80", 12.5d, 55.5d, 0d);

        Assert.InRange(output[0], 12.5d - 1e-12d, 12.5d + 1e-12d);
        Assert.InRange(output[1], 55.5d - 1e-12d, 55.5d + 1e-12d);
        Assert.InRange(output[2], -36.3941d - 1e-4d, -36.3941d + 1e-4d);
    }

    /// <summary>
    /// Verifies that legacy geographic <c>geoidgrids</c> operations also preserve the inverse vertical path.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformForDirectionWithLegacyLatlongGeoidGridsInverseReturnsExpectedVerticalShift()
    {
        bool created = TryCreateConversionTransformForDirection(
            "proj=latlong geoidgrids=egm96_15.gtx ellps=GRS80",
            GieDirection.Inverse,
            out Func<double[], double[]>? transform,
            out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([12.5d, 55.5d, -36.3941d]);
        Assert.InRange(output[0], 12.5d - 1e-12d, 12.5d + 1e-12d);
        Assert.InRange(output[1], 55.5d - 1e-12d, 55.5d + 1e-12d);
        Assert.InRange(output[2], -1e-4d, 1e-4d);
    }

    /// <summary>
    /// Verifies that projected legacy <c>geoidgrids</c> operations keep the vertical component alongside the projected ordinates.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLegacyMercGeoidGridsReturnsExpectedProjectedAndVerticalOutput()
    {
        double[] output = RequireBuiltinsRuntimeProjectedOutput("proj=merc geoidgrids=egm96_15.gtx ellps=GRS80", 12.5d, 55.5d, 0d);

        Assert.InRange(output[0], 1391493.63492d - 1e-4d, 1391493.63492d + 1e-4d);
        Assert.InRange(output[1], 7424275.19462d - 1e-4d, 7424275.19462d + 1e-4d);
        Assert.InRange(output[2], -36.3941d - 1e-4d, -36.3941d + 1e-4d);
    }

    /// <summary>
    /// Verifies that legacy <c>geoidgrids</c> operations still respect nodata cells when rewritten for the runtime path.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLegacyLatlongGeoidGridsNodataReturnsExpectedVerticalShift()
    {
        double[] output = RequireBuiltinsRuntimeProjectedOutput("+proj=latlong +ellps=WGS84 +geoidgrids=tests/test_nodata.gtx", 4.05d, 52.1d, 0d);

        Assert.InRange(output[0], 4.05d - 1e-12d, 4.05d + 1e-12d);
        Assert.InRange(output[1], 52.1d - 1e-12d, 52.1d + 1e-12d);
        Assert.InRange(output[2], -10d - 1e-6d, -10d + 1e-6d);
    }

    /// <summary>
    /// Verifies that the projected Krovak gridshift runtime path binds the Krovak-specific parameters that PROJ defaults for legacy pipelines.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithProjectedKrovakGridShiftPipelineReturnsExpectedCoordinate()
    {
        const string operation = "+proj=pipeline +step +proj=krovak +lat_0=49.5 +lon_0=24.8333333333333 +alpha=30.2881397527778 +k=0.9999 +x_0=0 +y_0=0 +ellps=bessel +step +proj=gridshift +grids=tests/test_gridshift_projected.tif +step +inv +proj=mod_krovak +lat_0=49.5 +lon_0=24.8333333333333 +alpha=30.2881397222222 +k=0.9999 +x_0=5000000 +y_0=5000000 +ellps=bessel";

        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, 16.610452439d, 49.202425040d, 0d);

        Assert.InRange(output[0], 16.610455233081716d - 1e-8d, 16.610455233081716d + 1e-8d);
        Assert.InRange(output[1], 49.202425036121703d - 5e-8d, 49.202425036121703d + 5e-8d);
        Assert.InRange(output.Length, 2, 3);
        if (output.Length == 3)
        {
            Assert.InRange(output[2], -1e-9d, 1e-9d);
        }
    }

    /// <summary>
    /// Verifies that the runtime <c>+proj=utm +approx</c> path still uses the approximate Snyder-based kernel instead of the exact ETMERC path.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithUtmApproxMatchesEquivalentTmercStep()
    {
        double[] utmApproxOutput = RequireBuiltinsRuntimeProjectedOutput("proj=utm zone=32 ellps=GRS80 approx", 12d, 55d);
        double[] tmercOutput = RequireBuiltinsRuntimeProjectedOutput("proj=tmerc ellps=GRS80 lat_0=0 lon_0=9 k_0=0.9996 x_0=500000 y_0=0 approx", 12d, 55d);

        Assert.Equal(tmercOutput[0], utmApproxOutput[0], 12);
        Assert.Equal(tmercOutput[1], utmApproxOutput[1], 12);
    }

    /// <summary>
    /// Verifies that Mercator builtins cases distinguish PROJ's case-sensitive spherification flags.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithMercatorRadiusSpherificationFlagsReturnsDistinctProjectedCoordinates()
    {
        double[] areaEquivalentOutput = RequireBuiltinsProjectedOutput("proj=merc ellps=GRS80 R_A");
        double[] arithmeticMeanOutput = RequireBuiltinsProjectedOutput("proj=merc ellps=GRS80 R_a");
        double[] geometricMeanOutput = RequireBuiltinsProjectedOutput("proj=merc ellps=GRS80 R_g");
        double[] harmonicMeanOutput = RequireBuiltinsProjectedOutput("proj=merc ellps=GRS80 R_h");

        Assert.Equal(1334340.6237297705d, areaEquivalentOutput[0], 9);
        Assert.InRange(Math.Abs(areaEquivalentOutput[1] - 7353636.6296552019d), 0d, 1e-8d);
        Assert.Equal(1333594.4904527504d, arithmeticMeanOutput[0], 9);
        Assert.InRange(Math.Abs(arithmeticMeanOutput[1] - 7349524.6413825499d), 0d, 1e-8d);
        Assert.Equal(1333592.6102291327d, geometricMeanOutput[0], 9);
        Assert.InRange(Math.Abs(geometricMeanOutput[1] - 7349514.2793497816d), 0d, 1e-8d);
        Assert.Equal(1333590.7300081658d, harmonicMeanOutput[0], 9);
        Assert.InRange(Math.Abs(harmonicMeanOutput[1] - 7349503.9173316229d), 0d, 1e-8d);

        Assert.NotEqual(areaEquivalentOutput[0], arithmeticMeanOutput[0], 9);
        Assert.NotEqual(geometricMeanOutput[0], harmonicMeanOutput[0], 9);
    }

    /// <summary>
    /// Verifies that Mercator builtins cases honor latitude-based PROJ spherification flags.
    /// </summary>
    [Theory]
    [InlineData("proj=merc ellps=GRS80 R_lat_a=60", 1338073.7436268919d, 7374210.0924803326d)]
    [InlineData("proj=merc ellps=GRS80 R_lat_g=60", 1338073.2696101593d, 7374207.4801437631d)]
    [InlineData("+proj=merc +R_C +ellps=WGS84 +lat_0=45", 1331355.0914081715d, 7337183.169834906d)]
    public void TryCreateTransformWithMercatorLatitudeSpherificationReturnsExpectedProjectedCoordinate(
        string operation,
        double expectedX,
        double expectedY)
    {
        double[] output = RequireBuiltinsProjectedOutput(operation);

        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, MercatorLatitudeSpherificationTolerance);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, MercatorLatitudeSpherificationTolerance);
    }

    /// <summary>
    /// Verifies that invalid Mercator spherification and eccentricity overrides are rejected.
    /// </summary>
    [Theory]
    [InlineData("+proj=merc +R_a +a=2 +f=2", "+f")]
    [InlineData("proj=merc a=1E77 R_lat_a=90 b=1", "R_lat_a")]
    [InlineData("proj=utm zone=32 ellps=GRS80 e=-0.5", "+e")]
    [InlineData("proj=utm zone=32 ellps=GRS80 e=1", "+e")]
    public void TryCreateConversionTransformWithInvalidEllipsoidOverridesReturnsValidationReason(string operation, string expectedToken)
    {
        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.False(created);
        Assert.Null(transform);
        Assert.Contains(expectedToken, skipReason ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that flattening can be set to zero explicitly for Mercator builtins cases.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithMercatorZeroFlatteningReturnsExpectedProjectedCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 181,
            Operation = "proj=merc +a=1.0 +f=0.0",
            ToleranceValue = 10d,
            ToleranceUnit = "nm",
            Direction = GieDirection.Forward,
            Accept = [12d, 56d],
            Expect = [0.20944d, 1.18505d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        double[] output = Assert.IsAssignableFrom<MathTransform>(transform).Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 5);
        Assert.Equal(testCase.Expect[1], output[1], 5);
    }

    /// <summary>
    /// Verifies that DMS projection parameters are parsed for builtins cases.
    /// </summary>
    [Fact]
    public void TryGetDoubleParsesDmsProjectionParameters()
    {
        MethodInfo parseOperationArgumentsMethod = typeof(GieBuiltinsTheoryTests).GetMethod("TryParseOperationArguments", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryParseOperationArguments.");
        object?[] parseArgs = ["+proj=lcc +ellps=clrk66 +lat_1=44d11'N +lat_2=45d42'N +x_0=609601.2192 +lon_0=84d20'W +lat_0=43d19'N +k_0=1.0000382 +units=us-ft", null];
        bool parsed = Assert.IsType<bool>(parseOperationArgumentsMethod.Invoke(null, parseArgs));
        Assert.True(parsed);

        Dictionary<string, string> operationArgs = Assert.IsType<Dictionary<string, string>>(parseArgs[1]);
        MethodInfo tryGetDoubleMethod = typeof(GieBuiltinsTheoryTests).GetMethod("TryGetDouble", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryGetDouble.");

        object?[] lat1Args = [operationArgs, "lat_1", null];
        bool parsedLat1 = Assert.IsType<bool>(tryGetDoubleMethod.Invoke(null, lat1Args));
        Assert.True(parsedLat1);
        double lat1 = Assert.IsType<double>(lat1Args[2]);
        Assert.Equal(44.18333333333333d, lat1, 12);

        object?[] lon0Args = [operationArgs, "lon_0", null];
        bool parsedLon0 = Assert.IsType<bool>(tryGetDoubleMethod.Invoke(null, lon0Args));
        Assert.True(parsedLon0);
        double lon0 = Assert.IsType<double>(lon0Args[2]);
        Assert.Equal(-84.33333333333333d, lon0, 12);
    }

    /// <summary>
    /// Verifies that the builtins operation tokenizer preserves GIE-style assignments where a negative value is attached to the equals sign token.
    /// </summary>
    [Fact]
    public void TryParseOperationArgumentsPreservesNegativeAssignmentValuesAfterDetachedEquals()
    {
        MethodInfo parseOperationArgumentsMethod = typeof(GieBuiltinsTheoryTests).GetMethod("TryParseOperationArguments", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryParseOperationArguments.");
        object?[] parseArgs = ["proj=helmert convention=position_vector x = 0.01270 y = 0.00650 z =-0.0209 dz =-0.0006", null];
        bool parsed = Assert.IsType<bool>(parseOperationArgumentsMethod.Invoke(null, parseArgs));
        Assert.True(parsed);

        Dictionary<string, string> operationArgs = Assert.IsType<Dictionary<string, string>>(parseArgs[1]);
        Assert.Equal("-0.0209", operationArgs["z"]);
        Assert.Equal("-0.0006", operationArgs["dz"]);
    }

    /// <summary>
    /// Verifies that builtins LCC cases treat explicit false offsets as meters even when the projection outputs US survey feet.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithLccUsFootOffsetsInMetersReturnsExpectedProjectedCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 320,
            Operation = "+proj=lcc +ellps=clrk66 +lat_1=44d11'N +lat_2=45d42'N +x_0=609601.2192 +lon_0=84d20'W +lat_0=43d19'N +k_0=1.0000382 +units=us-ft",
            ToleranceValue = 5d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [-83.16666666666667d, 43.75d],
            Expect = [2308335.75d, 160210.48d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        double[] output = Assert.IsAssignableFrom<MathTransform>(transform).Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 2);
        Assert.Equal(testCase.Expect[1], output[1], 2);
    }

    /// <summary>
    /// Verifies that builtins Cassini cases treat explicit false offsets as meters when <c>+to_meter</c> defines a non-metric output unit.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithCassToMeterOffsetsInMetersReturnsExpectedProjectedCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 912,
            Operation = "+proj=cass +lat_0=10.4416666666667 +lon_0=-61.3333333333333 +x_0=86501.46392052 +y_0=65379.0134283 +a=6378293.64520876 +b=6356617.98767984 +to_meter=0.201166195164",
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [-62d, 10d],
            Expect = [66644.94040882d, 82536.21873655d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        double[] output = Assert.IsAssignableFrom<MathTransform>(transform).Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 6);
        Assert.Equal(testCase.Expect[1], output[1], 6);
    }

    /// <summary>
    /// Verifies that the GIE conversion path normalizes explicit Cassini false offsets from meters to the declared output unit.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithCassToMeterOffsetsInMetersReturnsExpectedProjectedCoordinate()
    {
        const string operation = "+proj=cass +lat_0=10.4416666666667 +lon_0=-61.3333333333333 +x_0=86501.46392052 +y_0=65379.0134283 +a=6378293.64520876 +b=6356617.98767984 +to_meter=0.201166195164";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([-62d, 10d]);
        Assert.Equal(66644.94040882d, output[0], 6);
        Assert.Equal(82536.21873655d, output[1], 6);
    }

    /// <summary>
    /// Verifies that Hyperbolic Cassini direct-transform cases bind the runtime flag and return the expected projected coordinate.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithHyperbolicCassCaseReturnsExpectedProjectedCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 924,
            Operation = "+proj=cass +hyperbolic +a=6378306.376305601 +rf=293.466307 +lat_0=-16.25 +lon_0=179.33333333333333 +to_meter=20.1168 +x_0=251727.9155424 +y_0=334519.953768",
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [179.99433652777776d, -16.841456527777776d],
            Expect = [16015.28901692d, 13369.66005367d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        double[] output = Assert.IsAssignableFrom<MathTransform>(transform).Transform(testCase.Accept);
        Assert.Equal(testCase.Expect[0], output[0], 6);
        Assert.Equal(testCase.Expect[1], output[1], 6);
    }

    /// <summary>
    /// Verifies that Hyperbolic Cassini conversion cases execute through the pipeline runtime with the expected projected coordinate.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithHyperbolicCassCaseReturnsExpectedProjectedCoordinate()
    {
        const string operation = "+proj=cass +hyperbolic +a=6378306.376305601 +rf=293.466307 +lat_0=-16.25 +lon_0=179.33333333333333 +to_meter=20.1168 +x_0=251727.9155424 +y_0=334519.953768";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([179.99433652777776d, -16.841456527777776d]);
        Assert.Equal(16015.28901692d, output[0], 6);
        Assert.Equal(13369.66005367d, output[1], 6);
    }

    /// <summary>
    /// Verifies that Hyperbolic Cassini inverse-transform cases recover the expected geographic coordinate.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithInverseHyperbolicCassCaseReturnsExpectedGeographicCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 924,
            Operation = "+proj=cass +hyperbolic +a=6378306.376305601 +rf=293.466307 +lat_0=-16.25 +lon_0=179.33333333333333 +to_meter=20.1168 +x_0=251727.9155424 +y_0=334519.953768",
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Inverse,
            Accept = [16015.28901692d, 13369.66005367d],
            Expect = [179.99433652777776d, -16.841456527777776d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");

        double[] output = Assert.IsAssignableFrom<MathTransform>(transform).Transform(testCase.Accept);
        Assert.InRange(Math.Abs(output[0] - testCase.Expect[0]), 0d, 1e-9d);
        Assert.InRange(Math.Abs(output[1] - testCase.Expect[1]), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that Hyperbolic Cassini conversion cases roundtrip through the inverse pipeline runtime.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithHyperbolicCassCaseRoundtripsProjectedCoordinate()
    {
        const string operation = "+proj=cass +hyperbolic +a=6378306.376305601 +rf=293.466307 +lat_0=-16.25 +lon_0=179.33333333333333 +to_meter=20.1168 +x_0=251727.9155424 +y_0=334519.953768";
        double[] geographic = [179.99433652777776d, -16.841456527777776d];

        bool createdForward = TryCreateConversionTransform(operation, out Func<double[], double[]>? forwardTransform, out string? forwardSkipReason);
        Assert.True(createdForward, forwardSkipReason ?? "TryCreateConversionTransform returned false.");

        bool createdInverse = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? inverseTransform, out string? inverseSkipReason);
        Assert.True(createdInverse, inverseSkipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] projected = Assert.IsType<Func<double[], double[]>>(forwardTransform)(geographic);
        double[] roundtripped = Assert.IsType<Func<double[], double[]>>(inverseTransform)(projected);

        Assert.InRange(Math.Abs(roundtripped[0] - geographic[0]), 0d, 1e-9d);
        Assert.InRange(Math.Abs(roundtripped[1] - geographic[1]), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that geographic datum-shift operations are rejected for projected coordinate tuples.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithProjectedCoordinatesForLatlongDatumShiftReturnsFalse()
    {
        var testCase = new GieCase
        {
            LineNumber = 188,
            Operation = "proj=latlong towgs84=598.1,73.7,418.2,0.202,0.045,-2.455,6.7 ellps=bessel",
            ToleranceValue = 3d,
            ToleranceUnit = "m",
            Direction = GieDirection.Inverse,
            Accept = [2598417.333192d, 5930677.980308d],
            Expect = [399340.601863d, 5928794.177992d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);

        Assert.False(created);
        Assert.Null(transform);
        Assert.Contains("geographic", skipReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that geographic datum-shift operations still work for valid geographic coordinates.
    /// </summary>
    [Fact]
    public void TryCreateTransformWithGeographicCoordinatesForLatlongDatumShiftReturnsExpectedCoordinate()
    {
        var testCase = new GieCase
        {
            LineNumber = 98,
            Operation = "proj=latlong towgs84=598.1,73.7,418.2,0.202,0.045,-2.455,6.7 ellps=bessel",
            ToleranceValue = 3d,
            ToleranceUnit = "m",
            Direction = GieDirection.Inverse,
            Accept = [7.483333333333d, 53.5d],
            Expect = [7.482506019176d, 53.498461143331d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");
        MathTransform mathTransform = Assert.IsAssignableFrom<MathTransform>(transform);
        double[] output = mathTransform.Transform(testCase.Accept);
        Assert.InRange(output[0], testCase.Expect[0] - 2e-5d, testCase.Expect[0] + 2e-5d);
        Assert.InRange(output[1], testCase.Expect[1] - 2e-5d, testCase.Expect[1] + 2e-5d);
    }

    /// <summary>
    /// Verifies that the builtins harness recognizes legacy <c>+init=</c> pipeline steps after runtime normalization.
    /// </summary>
    /// <param name="operation">Pipeline operation containing legacy init references.</param>
    [Theory]
    [InlineData("+proj=pipeline +step +init=epsg:26915 +inv +step +init=epsg:3857")]
    [InlineData("+proj=pipeline +step +init=epsg:25832 +inv +step +init=epsg:25833 +step +init=epsg:25833 +inv +step +init=epsg:25832")]
    [InlineData("+proj=pipeline +step +proj=latlong +datum=NAD27 +inv +step +units=us-ft +init=nad27:3901")]
    public void TryIsRuntimeOperationSupportedRecognizesLegacyInitPipelines(string operation)
    {
        Assert.True(TryIsRuntimeOperationSupported(operation));
    }

    /// <summary>
    /// Verifies that legacy EPSG init pipeline steps can be executed through the conversion harness.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLegacyEpsgInitPipelineReturnsExpectedCoordinate()
    {
        const string operation = "+proj=pipeline +step +init=epsg:26915 +inv +step +init=epsg:3857";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([487147.594520173d, 4934316.46263998d]);
        Assert.InRange(output[0], -10370728.80d - 0.2d, -10370728.80d + 0.2d);
        Assert.InRange(output[1], 5552839.74d - 0.2d, 5552839.74d + 0.2d);
    }

    /// <summary>
    /// Verifies that legacy NAD27 init pipeline steps can be executed through the conversion harness and still land in the expected State Plane output range.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLegacyNad27InitPipelineReturnsProjectedCoordinateInExpectedRange()
    {
        const string operation = "+proj=pipeline +step +proj=latlong +datum=NAD27 +inv +step +units=us-ft +init=nad27:3901";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([-80.54166666666667d, 34.54166666666667d, 0d]);
        Assert.True(output.Length >= 2);
        Assert.True(double.IsFinite(output[0]));
        Assert.True(double.IsFinite(output[1]));
        Assert.InRange(output[0], 2137500d, 2138500d);
        Assert.InRange(output[1], 561000d, 561500d);
    }

    /// <summary>
    /// Verifies that runtime support now includes the former Bessel-based pipeline cases and the xyzgridshift test-grid case.
    /// </summary>
    /// <param name="operation">Pipeline operation to probe.</param>
    [Theory]
    [InlineData("+proj=pipeline +step +proj=cart +ellps=WGS84 +step +proj=helmert +x=674.374 +y=15.056 +z=405.346 +inv +step +proj=cart +ellps=bessel +inv +step +proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +k_0=1 +x_0=2600000 +y_0=1200000 +ellps=bessel +units=m")]
    [InlineData("+proj=pipeline +step +proj=krovak +lat_0=49.5 +lon_0=24.8333333333333 +alpha=30.2881397527778 +k=0.9999 +x_0=0 +y_0=0 +ellps=bessel +step +proj=gridshift +grids=tests/test_gridshift_projected.tif +step +inv +proj=mod_krovak +lat_0=49.5 +lon_0=24.8333333333333 +alpha=30.2881397222222 +k=0.9999 +x_0=5000000 +y_0=5000000 +ellps=bessel")]
    [InlineData("+proj=pipeline +step +inv +proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=0 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +ellps=clrk80ign +pm=paris +step +proj=push +v_3 +step +proj=cart +ellps=clrk80ign +step +proj=xyzgridshift +grids=tests/subset_of_gr3df97a.tif +grid_ref=output_crs +ellps=GRS80 +step +proj=cart +ellps=GRS80 +inv +step +proj=pop +v_3 +step +proj=lcc +lat_0=46.5 +lon_0=3 +lat_1=49 +lat_2=44 +x_0=700000 +y_0=6600000 +ellps=GRS80")]
    public void TryIsRuntimeOperationSupportedRecognizesRemainingRuntimeFeaturePipelines(string operation)
    {
        Assert.True(TryIsRuntimeOperationSupported(operation));
    }

    /// <summary>
    /// Verifies that the GIE conversion path resolves known <c>tests/...</c> grid tokens to local fixtures.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithKnownTestGridTokenReturnsExpectedCoordinate()
    {
        const string operation = "+proj=pipeline +step +inv +proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=0 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +ellps=clrk80ign +pm=paris +step +proj=push +v_3 +step +proj=cart +ellps=clrk80ign +step +proj=xyzgridshift +grids=tests/subset_of_gr3df97a.tif +grid_ref=output_crs +ellps=GRS80 +step +proj=cart +ellps=GRS80 +inv +step +proj=pop +v_3 +step +proj=lcc +lat_0=46.5 +lon_0=3 +lat_1=49 +lat_2=44 +x_0=700000 +y_0=6600000 +ellps=GRS80";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([814149.529d, 1887019.768d, 0d]);
        Assert.InRange(output[0], 860690.804d - 1e-3d, 860690.804d + 1e-3d);
        Assert.InRange(output[1], 6319036.849d - 1e-3d, 6319036.849d + 1e-3d);
        if (output.Length > 2)
        {
            Assert.InRange(output[2], -1e-6d, 1e-6d);
        }
    }

    /// <summary>
    /// Verifies that the builtins harness recognizes the conversion operations needed by the former no-applicable fixtures.
    /// </summary>
    /// <param name="operation">Operation to probe.</param>
    [Theory]
    [InlineData("+proj=defmodel +model=tests/simple_model_degree_horizontal.json")]
    [InlineData("+proj=deformation +xy_grids=alaska +z_grids=egm96_15.gtx +t_epoch=2016.0 +ellps=GRS80")]
    [InlineData("proj=helmert convention=coordinate_frame x=0.06155 y=-0.01087 z=-0.04019 rx=-0.0394924 ry=-0.0327221 rz=-0.0328979 s=-0.009994")]
    [InlineData("proj = pipeline ellps=GRS80; step proj = cart; step proj = helmert convention=coordinate_frame x = 0.06155 y = -0.01087 z = -0.04019 rx = -0.0394924 ry = -0.0327221 rz = -0.0328979 s = -0.009994; step proj = cart inv;")]
    public void TryIsRuntimeOperationSupportedRecognizesNoApplicableQuickWins(string operation)
    {
        Assert.True(TryIsRuntimeOperationSupported(operation));
    }

    /// <summary>
    /// Verifies that standalone Helmert conversion cases execute through the builtins conversion path.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithStandaloneHelmertReturnsExpectedCoordinate()
    {
        const string operation = "proj=helmert convention=coordinate_frame x=0.06155 y=-0.01087 z=-0.04019 rx=-0.0394924 ry=-0.0327221 rz=-0.0328979 s=-0.009994";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([-4052051.7643d, 4212836.2017d, -2545106.0245d]);
        Assert.InRange(output[0], -4052052.7379d - 1e-4d, -4052052.7379d + 1e-4d);
        Assert.InRange(output[1], 4212835.9897d - 1e-4d, 4212835.9897d + 1e-4d);
        Assert.InRange(output[2], -2545104.5898d - 1e-4d, -2545104.5898d + 1e-4d);
    }

    /// <summary>
    /// Verifies that standalone kinematic Helmert conversion cases execute through the builtins conversion path even when GIE uses detached equals tokens.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithStandaloneKinematicHelmertReturnsExpectedCoordinate()
    {
        const string operation = "proj=helmert convention=position_vector x = 0.01270  dx =-0.0029  rx =-0.00039  drx =-0.00011 y = 0.00650  dy =-0.0002  ry = 0.00080  dry =-0.00019 z =-0.0209   dz =-0.0006  rz =-0.00114  drz = 0.00007 s = 0.00195  ds = 0.00001 t_epoch=1988.0";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([3370658.378d, 711877.314d, 5349787.086d, 2018d]);
        Assert.InRange(output[0], 3370658.18087d - 1e-4d, 3370658.18087d + 1e-4d);
        Assert.InRange(output[1], 711877.42750d - 1e-4d, 711877.42750d + 1e-4d);
        Assert.InRange(output[2], 5349787.12648d - 1e-4d, 5349787.12648d + 1e-4d);
        Assert.InRange(output[3], 2018d - 1e-12d, 2018d + 1e-12d);
    }

    /// <summary>
    /// Verifies that standalone geographic identity conversion cases honor the legacy <c>+geoc</c> flag using PROJ's longlat semantics.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLonglatGeocAndInverseReturnsGeocentricLatitude()
    {
        const string operation = "proj=pipeline step proj=longlat ellps=GRS80 geoc inv";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([12d, 55d, 0d, 0d]);
        Assert.Equal(12d, output[0], 12);
        Assert.Equal(54.818973308324573d, output[1], 12);
        Assert.Equal(0d, output[2], 12);
        Assert.Equal(0d, output[3], 12);
    }

    /// <summary>
    /// Verifies that standalone <c>set</c> conversion cases override the 4th ordinate.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithSetV4ReturnsExpectedFourthOrdinate()
    {
        const string operation = "+proj=set +v_1=10 +v_2=20 +v_3=30 +v_4=40";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([1d, 2d, 3d, 4d]);
        Assert.Equal([10d, 20d, 30d, 40d], output);
    }

    /// <summary>
    /// Verifies that standalone geographic identity conversion cases honor <c>+vto_meter</c>.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLonglatVtoMeterScalesThirdOrdinate()
    {
        const string operation = "+proj=longlat +a=1 +b=1 +vto_meter=1000";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([0d, 0d, 1000d]);
        Assert.Equal([0d, 0d, 1d], output);
    }

    /// <summary>
    /// Verifies that projected conversion cases honor <c>+vunits</c> for the third ordinate.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithMercatorVunitsScalesThirdOrdinate()
    {
        const string operation = "+proj=merc +a=1 +b=1 +vunits=km";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([0d, 0d, 1000d]);
        Assert.Equal(0d, output[0], 12);
        Assert.InRange(output[1], -1e-12d, 1e-12d);
        Assert.Equal(1d, output[2], 12);
    }

    /// <summary>
    /// Verifies that standalone geographic identity conversion cases honor <c>+lon_wrap</c>.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLonglatLonWrapNormalizesLongitude()
    {
        const string operation = "+proj=longlat +ellps=WGS84 +lon_wrap=180";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([-1d, 10d, 0d]);
        Assert.Equal([359d, 10d, 0d], output);
    }

    /// <summary>
    /// Verifies that loose GIE-style assignment syntax with semicolon separators is normalized for runtime pipeline execution.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLooseAssignmentPipelineReturnsExpectedCoordinate()
    {
        const string operation = "proj = pipeline ellps=GRS80; step proj = cart; step proj = helmert convention=coordinate_frame x = 0.06155 y = -0.01087 z = -0.04019 rx = -0.0394924 ry = -0.0327221 rz = -0.0328979 s = -0.009994; step proj = cart inv;";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([133.88551329d, -23.67012389d, 603.3466d, 0d]);
        Assert.InRange(output[0], 133.8855216d - 1e-6d, 133.8855216d + 1e-6d);
        Assert.InRange(output[1], -23.67011014d - 1e-6d, -23.67011014d + 1e-6d);
        Assert.InRange(output[2], 603.2489d - 1e-3d, 603.2489d + 1e-3d);
        Assert.InRange(output[3], -1e-9d, 1e-9d);
    }

    /// <summary>
    /// Verifies that mapped NKG URNs execute through the builtins conversion path, including the Norway-specific xyzgridshift case.
    /// </summary>
    [Theory]
    [InlineData(
        "urn:ogc:def:coordinateOperation:NKG::ITRF2000_TO_DK",
        3541657.3778d,
        948984.2343d,
        5201383.5231d,
        2020.5d,
        3541657.9362d,
        948983.7825d,
        5201383.2292d,
        2020.5d,
        1e-3d)]
    [InlineData(
        "urn:ogc:def:coordinateOperation:NKG::ITRF2014_TO_NO",
        3275753.4135d,
        321111.2481d,
        5445042.2134d,
        2020.0d,
        3275753.9094d,
        321110.8626d,
        5445041.8818d,
        2020.0d,
        1e-4d)]
    public void TryCreateConversionTransformWithCoordinateOperationUrnReturnsExpectedCoordinate(
        string operation,
        double x,
        double y,
        double z,
        double epoch,
        double expectedX,
        double expectedY,
        double expectedZ,
        double expectedEpoch,
        double tolerance)
    {
        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([x, y, z, epoch]);
        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, tolerance);
        Assert.InRange(Math.Abs(output[2] - expectedZ), 0d, tolerance);
        Assert.InRange(Math.Abs(output[3] - expectedEpoch), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that the NKG fixture now emits its mapped rows instead of falling back to a single placeholder case.
    /// </summary>
    [Fact]
    public void GetCasesFromFixtureWithNkgFixtureReturnsMappedTheoryRows()
    {
        List<object> rows = GetCasesFromFixtureRows("nkg.gie");

        Assert.True(rows.Count > 20, $"Expected mapped NKG rows, but found only {rows.Count}.");
    }

    /// <summary>
    /// Verifies that the former no-applicable fixtures now emit concrete theory rows instead of null placeholders.
    /// </summary>
    /// <param name="fixtureName">Fixture name to inspect.</param>
    [Theory]
    [InlineData("defmodel.gie")]
    [InlineData("deformation.gie")]
    [InlineData("ellipsoid.gie")]
    [InlineData("GDA.gie")]
    [InlineData("nkg.gie")]
    public void GetCasesFromFixtureForFormerNoApplicableFixturesReturnsConcreteRow(string fixtureName)
    {
        List<object> rows = GetCasesFromFixtureRows(fixtureName);

        Assert.NotEmpty(rows);

        PropertyInfo dataProperty = rows[0].GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Could not locate TheoryDataRow.Data.");
        var firstCase = dataProperty.GetValue(rows[0]) as GieCase;

        Assert.NotNull(firstCase);
        Assert.NotNull(firstCase!.Operation);
        Assert.NotEmpty(firstCase.Operation);
    }

    /// <summary>
    /// Verifies that the dedicated failure provider emits expected-failure rows from the builtins fixtures.
    /// </summary>
    [Fact]
    public void GetBuiltinsFailureCasesReturnsExpectedFailureRows()
    {
        List<object> rows = GetFailureCaseRows();

        Assert.NotEmpty(rows);

        PropertyInfo dataProperty = rows[0].GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Could not locate TheoryDataRow.Data.");
        var firstCase = dataProperty.GetValue(rows[0]) as GieCase;

        Assert.NotNull(firstCase);
        Assert.True(firstCase!.ExpectsFailure);
        Assert.NotEmpty(firstCase.Operation);
    }

    /// <summary>
    /// Verifies that the runtime <c>s2</c> path binds string <c>+UVtoST=</c> modes instead of silently falling back to the quadratic default.
    /// </summary>
    /// <param name="operation">Projection operation string.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected projected x coordinate.</param>
    /// <param name="expectedY">Expected projected y coordinate.</param>
    [Theory]
    [InlineData("+proj=s2 +ellps=WGS84 +lat_0=0 +lon_0=0 +UVtoST=linear", 20d, 20.124006563576454d, 0.6819851171331012d, 0.6936645165744716d)]
    [InlineData("+proj=s2 +ellps=WGS84 +lat_0=90 +UVtoST=tangent", 20d, 70.12337013762532d, 0.29020309743436806d, 0.4211558922141421d)]
    [InlineData("+proj=s2 +ellps=WGS84 +lat_0=0 +lon_0=180 +UVtoST=none", 160d, 20.124006563576454d, -0.3873290331489431d, -0.3639702342662023d)]
    public void TryCreateConversionTransformWithS2StringUvToStModesReturnsExpectedCoordinate(
        string operation,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, longitude, latitude);

        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, 1e-12d);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, 1e-12d);
    }

    /// <summary>
    /// Verifies that the runtime <c>s2</c> inverse path also honors string <c>+UVtoST=</c> modes.
    /// </summary>
    /// <param name="operation">Projection operation string.</param>
    /// <param name="x">Input projected x coordinate.</param>
    /// <param name="y">Input projected y coordinate.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    /// <param name="expectedLatitude">Expected latitude in degrees.</param>
    [Theory]
    [InlineData("+proj=s2 +ellps=WGS84 +lat_0=0 +lon_0=0 +UVtoST=linear", 0.6819851171331012d, 0.6936645165744716d, 20d, 20.124006563576454d)]
    [InlineData("+proj=s2 +ellps=WGS84 +lat_0=90 +UVtoST=tangent", 0.29020309743436806d, 0.4211558922141421d, 20d, 70.12337013762532d)]
    [InlineData("+proj=s2 +ellps=WGS84 +lat_0=0 +lon_0=180 +UVtoST=none", -0.3873290331489431d, -0.3639702342662023d, 160d, 20.124006563576454d)]
    public void TryCreateConversionTransformForDirectionWithS2StringUvToStModesReturnsExpectedCoordinate(
        string operation,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([x, y]);
        Assert.InRange(Math.Abs(output[0] - expectedLongitude), 0d, 1e-9d);
        Assert.InRange(Math.Abs(output[1] - expectedLatitude), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that the runtime <c>healpix</c> path applies the PROJ <c>rot_xy</c> parameter.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithRotatedHealpixReturnsExpectedCoordinate()
    {
        const string operation = "+proj=healpix +R=6400000 +lat_1=0.5 +lat_2=2 +rot_xy=42";
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, 2d, 1d);

        Assert.InRange(Math.Abs(output[0] - 254069.735470912856d), 0d, 1e-6d);
        Assert.InRange(Math.Abs(output[1] - -51696.237925639456d), 0d, 1e-6d);
    }

    /// <summary>
    /// Verifies that the runtime <c>healpix</c> inverse path undoes the PROJ <c>rot_xy</c> rotation.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformForDirectionWithRotatedHealpixReturnsExpectedCoordinate()
    {
        const string operation = "+proj=healpix +R=6400000 +lat_1=0.5 +lat_2=2 +rot_xy=42";
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([254069.735470912856d, -51696.237925639456d]);
        Assert.InRange(Math.Abs(output[0] - 2d), 0d, 1e-9d);
        Assert.InRange(Math.Abs(output[1] - 1d), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that the runtime <c>rhealpix</c> path combines polar caps using the configured north and south square indices.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithRhealpixPolarSquaresReturnsExpectedCoordinate()
    {
        const string operation = "+proj=rhealpix +south_square=2 +north_square=3 +ellps=WGS84";
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, 45d, 50d);

        Assert.InRange(Math.Abs(output[0] - 10806592d), 0d, 0.75d);
        Assert.InRange(Math.Abs(output[1] - 10007554d), 0d, 0.75d);
    }

    /// <summary>
    /// Verifies that the runtime <c>rhealpix</c> inverse path disassembles polar squares back into the HEALPix cap layout.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformForDirectionWithRhealpixPolarSquaresReturnsExpectedCoordinate()
    {
        const string operation = "+proj=rhealpix +south_square=2 +north_square=3 +ellps=WGS84";
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([10806592d, 10007554d]);
        Assert.InRange(Math.Abs(output[0] - 45d), 0d, 1e-5d);
        Assert.InRange(Math.Abs(output[1] - 50d), 0d, 1e-5d);
    }

    /// <summary>
    /// Verifies that the runtime <c>isea</c> path binds string <c>+orient=pole</c> instead of silently falling back to the default orientation.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected projected x coordinate.</param>
    /// <param name="expectedY">Expected projected y coordinate.</param>
    [Theory]
    [InlineData(0d, 0d, 0d, -195097.13364071414d)]
    [InlineData(90d, 0d, 9593072.435467451811d, 0d)]
    [InlineData(0d, 45d, 0d, 4726854.770339427515864d)]
    public void TryCreateConversionTransformWithIseaPoleOrientationReturnsExpectedCoordinate(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        const string operation = "+proj=isea +R=6371007.18091875 +orient=pole";
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, longitude, latitude);

        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, 2e-4d);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, 2e-4d);
    }

    /// <summary>
    /// Verifies that the runtime <c>isea</c> inverse path also honors string <c>+orient=pole</c>.
    /// </summary>
    /// <param name="x">Input projected x coordinate.</param>
    /// <param name="y">Input projected y coordinate.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    /// <param name="expectedLatitude">Expected latitude in degrees.</param>
    [Theory]
    [InlineData(0d, -195097.13364071414d, 0d, 0d)]
    [InlineData(9593072.435467451811d, 0d, 90d, 0d)]
    [InlineData(0d, 4726854.770339427515864d, 0d, 45d)]
    public void TryCreateConversionTransformForDirectionWithIseaPoleOrientationReturnsExpectedCoordinate(
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        const string operation = "+proj=isea +R=6371007.18091875 +orient=pole";
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([x, y]);
        Assert.InRange(Math.Abs(output[0] - expectedLongitude), 0d, 1e-9d);
        Assert.InRange(Math.Abs(output[1] - expectedLatitude), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that the runtime <c>lagrng</c> path binds the PROJ <c>lat_1</c> and <c>W</c> parameters instead of silently using constructor defaults.
    /// </summary>
    /// <param name="operation">Projection operation string.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected projected x coordinate.</param>
    /// <param name="expectedY">Expected projected y coordinate.</param>
    /// <param name="tolerance">Allowed projected-coordinate tolerance in meters.</param>
    [Theory]
    [InlineData("+proj=lagrng +a=6400000 +W=2 +lat_1=0.5", 2d, 1d, 111703.375917226d, 27929.831908033d, 1e-4d)]
    [InlineData("+proj=lagrng +R=1 +lat_1=56", 12d, 56d, 0.10d, 0d, 0.01d)]
    public void TryCreateConversionTransformWithLagrangeProjParametersReturnsExpectedCoordinate(
        string operation,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, longitude, latitude);

        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that the runtime <c>lagrng</c> inverse path reconstructs coordinates for the repaired <c>lat_1</c> and <c>W</c> cases.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformForDirectionWithLagrangeProjParametersReturnsExpectedCoordinate()
    {
        const string operation = "+proj=lagrng +a=6400000 +W=2 +lat_1=0.5";
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([111703.375917226d, 27929.831908033d]);
        Assert.InRange(Math.Abs(output[0] - 2d), 0d, 1e-9d);
        Assert.InRange(Math.Abs(output[1] - 1d), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that the repaired runtime <c>lagrng</c> path remains stable across the 100 roundtrips exercised by the GIE row.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLagrangeProjParametersRoundtripsProjectedCoordinate()
    {
        AssertRuntimeProjectedRoundtrip(
            "+proj=lagrng +a=6400000 +W=2 +lat_1=0.5",
            [2d, 1d],
            111703.375917226d,
            27929.831908033d,
            roundtripCount: 100,
            tolerance: 1e-4d);
    }

    /// <summary>
    /// Verifies that the runtime <c>vandg</c> path applies <c>+over</c> instead of wrapping longitudes back into the default range.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected projected x coordinate.</param>
    /// <param name="expectedY">Expected projected y coordinate.</param>
    [Theory]
    [InlineData(180.1d, 50d, 18569963.6471d, 7734997.6218d)]
    [InlineData(-180.1d, -50d, -18569963.6471d, -7734997.6218d)]
    public void TryCreateConversionTransformWithVanDerGrintenOverReturnsExpectedCoordinate(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        const string operation = "+proj=vandg +a=6400000 +over";
        double[] output = RequireBuiltinsRuntimeProjectedOutput(operation, longitude, latitude);

        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, 5e-4d);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, 5e-4d);
    }

    /// <summary>
    /// Verifies that the runtime <c>vandg</c> inverse path preserves <c>+over</c> longitudes beyond 180 degrees.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformForDirectionWithVanDerGrintenOverReturnsExpectedCoordinate()
    {
        const string operation = "+proj=vandg +a=6400000 +over";
        bool created = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransformForDirection returned false.");

        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([18569963.6471d, 7734997.6218d]);
        Assert.InRange(Math.Abs(output[0] - 180.1d), 0d, 1e-9d);
        Assert.InRange(Math.Abs(output[1] - 50d), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies that the repaired runtime <c>vandg</c> path remains stable across the 10 roundtrips exercised by the GIE row.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithVanDerGrintenOverRoundtripsProjectedCoordinate()
    {
        AssertRuntimeProjectedRoundtrip(
            "+proj=vandg +a=6400000 +over",
            [180.1d, 50d],
            18569963.6471d,
            7734997.6218d,
            roundtripCount: 10,
            tolerance: 5e-4d);
    }

    /// <summary>
    /// Verifies that all former harness-only skip-status rows now execute as direct result assertions without reflection into <c>AssertCaseWithinTolerance</c>.
    /// </summary>
    /// <param name="testCase">Former harness-only GIE case.</param>
    [Theory]
    [MemberData(nameof(GetFormerHarnessOnlyCases))]
    public void FormerHarnessOnlyCasesStayWithinToleranceWithDirectAssertions(GieCase testCase)
    {
        ArgumentNullException.ThrowIfNull(testCase);
        AssertDirectGieCaseWithinTolerance(testCase);
    }

    private static List<GieCase> GetFormerHarnessOnlyCasesCore()
    {
        List<object> rows = GetCasesFromFixtureRows("builtins.gie");
        var cases = new List<GieCase>();
        for (int i = 0; i < rows.Count; i++)
        {
            PropertyInfo dataProperty = rows[i].GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException("Could not locate TheoryDataRow.Data.");
            if (dataProperty.GetValue(rows[i]) is GieCase testCase && FormerHarnessOnlyFixtureLines.Contains(testCase.LineNumber))
            {
                cases.Add(testCase);
            }
        }

        cases.Sort((left, right) => left.LineNumber.CompareTo(right.LineNumber));
        return cases;
    }

    private static void AssertDirectGieCaseWithinTolerance(GieCase testCase)
    {
        double[] output = ExecuteGieCase(testCase);
        double tolerance = Math.Max(ToNumericTolerance(testCase.ToleranceValue, testCase.ToleranceUnit), 1e-3d);
        int dimensionsToCompare = Math.Min(output.Length, testCase.Expect.Length);

        Assert.True(dimensionsToCompare >= 2, $"GIE case {testCase.LineNumber} does not contain enough coordinates for comparison.");
        for (int i = 0; i < dimensionsToCompare; i++)
        {
            double delta = GetComparisonDelta(output, testCase.Expect, i);
            Assert.True(
                delta <= tolerance,
                $"GIE case {testCase.LineNumber} exceeded tolerance on axis {i}: delta={delta:R}, tolerance={tolerance:R}.");
        }
    }

    private static double[] ExecuteGieCase(GieCase testCase)
    {
        if (TryCreateConversionTransformForDirection(testCase.Operation, testCase.Direction, out Func<double[], double[]>? conversionTransform, out string? conversionSkipReason))
        {
            return Assert.IsType<Func<double[], double[]>>(conversionTransform)(testCase.Accept);
        }

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? conversionSkipReason ?? $"Could not create transform for GIE case {testCase.LineNumber}.");
        return Assert.IsAssignableFrom<MathTransform>(transform).Transform(testCase.Accept);
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

    private static bool IsLikelyGeographicCoordinatePair(double[] coordinates)
    {
        if (coordinates is null || coordinates.Length < 2)
        {
            return false;
        }

        double first = Math.Abs(coordinates[0]);
        double second = Math.Abs(coordinates[1]);

        bool lonLatRange = first <= 180d && second <= 90d;
        bool latLonRange = first <= 90d && second <= 180d;
        return lonLatRange || latLonRange;
    }

    private static double GetComparisonDelta(double[] actual, double[] expected, int axis)
    {
        double delta = Math.Abs(actual[axis] - expected[axis]);
        if (axis != 0 || !IsLikelyGeographicCoordinatePair(actual) || !IsLikelyGeographicCoordinatePair(expected))
        {
            return delta;
        }

        double normalizedActual = TransformationMath.NormalizeLongitudeDegrees(actual[axis]);
        double normalizedExpected = TransformationMath.NormalizeLongitudeDegrees(expected[axis]);
        double normalizedDelta = Math.Abs(normalizedActual - normalizedExpected);
        return Math.Min(normalizedDelta, 360d - normalizedDelta);
    }

    private static bool TryIsRuntimeOperationSupported(string operation)
    {
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("TryIsRuntimeOperationSupported", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryIsRuntimeOperationSupported.");
        return Assert.IsType<bool>(method.Invoke(null, [operation]));
    }

    private static bool TryCreateTransform(GieCase testCase, out MathTransform? transform, out string? skipReason)
    {
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("TryCreateTransform", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryCreateTransform.");
        object?[] args = [testCase, null, null];
        bool created = Assert.IsType<bool>(method.Invoke(null, args));
        transform = args[1] as MathTransform;
        skipReason = args[2] as string;
        return created;
    }

    private static bool TryCreateConversionTransform(string operation, out Func<double[], double[]>? transform, out string? skipReason)
    {
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("TryCreateConversionTransform", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryCreateConversionTransform.");
        object?[] args = [operation, null, null];
        bool created = Assert.IsType<bool>(method.Invoke(null, args));
        transform = args[1] as Func<double[], double[]>;
        skipReason = args[2] as string;
        return created;
    }

    private static bool TryCreateConversionTransformForDirection(
        string operation,
        GieDirection direction,
        out Func<double[], double[]>? transform,
        out string? skipReason)
    {
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("TryCreateConversionTransformForDirection", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.TryCreateConversionTransformForDirection.");
        object?[] args = [operation, direction, null, null];
        bool created = Assert.IsType<bool>(method.Invoke(null, args));
        transform = args[2] as Func<double[], double[]>;
        skipReason = args[3] as string;
        return created;
    }

    private static List<object> GetCasesFromFixtureRows(string fixtureName)
    {
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("GetCasesFromFixture", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.GetCasesFromFixture.");

        var rows = new List<object>();
        foreach (object row in Assert.IsAssignableFrom<System.Collections.IEnumerable>(method.Invoke(null, [fixtureName])))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static List<object> GetFailureCaseRows()
    {
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("GetBuiltinsFailureCases", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.GetBuiltinsFailureCases.");

        var rows = new List<object>();
        foreach (object row in Assert.IsAssignableFrom<System.Collections.IEnumerable>(method.Invoke(null, [])))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static double[] RequireBuiltinsRuntimeProjectedOutput(string operation, params double[] input)
    {
        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        return Assert.IsType<Func<double[], double[]>>(transform)(input);
    }

    private static void AssertRuntimeProjectedRoundtrip(
        string operation,
        double[] geographic,
        double expectedX,
        double expectedY,
        int roundtripCount,
        double tolerance)
    {
        bool createdForward = TryCreateConversionTransform(operation, out Func<double[], double[]>? forwardTransform, out string? forwardSkipReason);
        Assert.True(createdForward, forwardSkipReason ?? "TryCreateConversionTransform returned false.");

        bool createdInverse = TryCreateConversionTransformForDirection(operation, GieDirection.Inverse, out Func<double[], double[]>? inverseTransform, out string? inverseSkipReason);
        Assert.True(createdInverse, inverseSkipReason ?? "TryCreateConversionTransformForDirection returned false.");

        Func<double[], double[]> forward = Assert.IsType<Func<double[], double[]>>(forwardTransform);
        Func<double[], double[]> inverse = Assert.IsType<Func<double[], double[]>>(inverseTransform);
        double[] geographicCurrent = [geographic[0], geographic[1]];
        double[] projected = forward(geographicCurrent);
        for (int i = 0; i < roundtripCount; i++)
        {
            geographicCurrent = inverse(projected);
            projected = forward(geographicCurrent);
        }

        Assert.InRange(Math.Abs(projected[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projected[1] - expectedY), 0d, tolerance);
    }

    private static double[] RequireBuiltinsProjectedOutput(string operation)
    {
        var testCase = new GieCase
        {
            LineNumber = 157,
            Operation = operation,
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [12d, 55d],
            Expect = [0d, 0d],
        };

        bool created = TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason);
        Assert.True(created, skipReason ?? "TryCreateTransform returned false.");
        MathTransform mathTransform = Assert.IsAssignableFrom<MathTransform>(transform);
        return mathTransform.Transform(testCase.Accept);
    }
}
