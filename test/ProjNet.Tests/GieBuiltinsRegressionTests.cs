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
    /// Verifies that inverse Albers builtins rows no longer skip because the conversion path now respects the GIE direction.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithInverseAlbersBuiltinsCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 60,
            Operation = "+proj=aea +ellps=GRS80 +lat_1=0 +lat_2=2",
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Inverse,
            Accept = [16468399.3582d, 5275043.9815d],
            Expect = [150d, 50d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that inverse geographic outputs comparing <c>-180</c> and <c>180</c> no longer skip as a false 360-degree delta.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithInverseEqearthAntimeridianCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 659,
            Operation = "+proj=eqearth +R=6378137",
            ToleranceValue = 1d,
            ToleranceUnit = "cm",
            Direction = GieDirection.Inverse,
            Accept = [14795421.79d, 5486671.72d],
            Expect = [180d, 45d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the builtins harness no longer skips the near-center spherical Azimuthal Equidistant case.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithForwardAeqdNearCenterCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 163,
            Operation = "+proj=aeqd +a=6371008.771415 +b=6371008.771415 +lat_0=30.2345 +lon_0=-120.2345",
            ToleranceValue = 1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [-120.234501d, 30.234501d],
            Expect = [-0.096d, 0.111d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the builtins harness no longer skips the Guam-specific Azimuthal Equidistant forward case.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithForwardAeqdGuamCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 205,
            Operation = "+proj=aeqd +guam +ellps=clrk66 +x_0=50000.00 +y_0=50000.00 +lon_0=144.74875069444445 +lat_0=13.47246633333333",
            ToleranceValue = 1d,
            ToleranceUnit = "cm",
            Direction = GieDirection.Forward,
            Accept = [144.63533129166666d, 13.33903846111111d],
            Expect = [37712.48d, 35242.00d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the builtins harness no longer skips the Guam-specific Azimuthal Equidistant inverse case.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithInverseAeqdGuamCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 209,
            Operation = "+proj=aeqd +guam +ellps=clrk66 +x_0=50000.00 +y_0=50000.00 +lon_0=144.74875069444445 +lat_0=13.47246633333333",
            ToleranceValue = 1d,
            ToleranceUnit = "cm",
            Direction = GieDirection.Inverse,
            Accept = [37712.48d, 35242.00d],
            Expect = [144.63533129166666d, 13.33903846111111d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the builtins harness no longer skips horizontal ellipsoidal Airocean forward cases.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithForwardAiroceanHorizontalCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 1301,
            Operation = "+proj=airocean +orient=horizontal +ellps=GRS80",
            ToleranceValue = 0.1d,
            ToleranceUnit = "mm",
            Direction = GieDirection.Forward,
            Accept = [23d, 28d],
            Expect = [13391387.087562159d, 13572113.73386754d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
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
    /// Verifies that the original LCC GIE row no longer skips through the conversion path when its explicit false offsets are normalized from meters to US survey feet.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithLccUsFootOffsetsInMetersDoesNotSkip()
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

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
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
    /// Verifies that the original Cassini GIE row no longer skips through the conversion path when its explicit false offsets are normalized from meters to the declared output unit.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithCassOffsetsInMetersDoesNotSkip()
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

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the wide-offset ETMERC builtins hotspot no longer skips.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithEtmercWideOffsetCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 1945,
            Operation = "+proj=etmerc   +ellps=GRS80",
            ToleranceValue = 50d,
            ToleranceUnit = "nm",
            Direction = GieDirection.Forward,
            Accept = [44.69d, 35.37d],
            Expect = [4168136.489446198d, 4985511.302287407d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that the inverse wide-offset ETMERC builtins hotspot no longer skips.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithInverseEtmercWideOffsetCaseDoesNotSkip()
    {
        var testCase = new GieCase
        {
            LineNumber = 1961,
            Operation = "+proj=etmerc   +ellps=GRS80",
            ToleranceValue = 50d,
            ToleranceUnit = "nm",
            Direction = GieDirection.Inverse,
            Accept = [4168136.489446198d, 4985511.302287407d],
            Expect = [44.69d, 35.37d],
        };

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
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
    /// Verifies that the Hyperbolic Cassini builtins hotspot no longer skips.
    /// </summary>
    [Fact]
    public void AssertCaseWithinToleranceWithHyperbolicCassCaseDoesNotSkip()
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

        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("AssertCaseWithinTolerance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.AssertCaseWithinTolerance.");

        Exception? exception = Record.Exception(() => method.Invoke(null, [testCase]));
        Assert.Null(exception);
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
    /// Verifies that legacy NAD27 init pipeline steps can be executed through the conversion harness.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithLegacyNad27InitPipelineReturnsExpectedCoordinate()
    {
        const string operation = "+proj=pipeline +step +proj=latlong +datum=NAD27 +inv +step +units=us-ft +init=nad27:3901";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(created, skipReason ?? "TryCreateConversionTransform returned false.");
        double[] output = Assert.IsType<Func<double[], double[]>>(transform)([-80.54166666666667d, 34.54166666666667d, 0d]);
        Assert.InRange(output[0], 2138028.224d - 1e-3d, 2138028.224d + 1e-3d);
        Assert.InRange(output[1], 561330.721d - 1e-3d, 561330.721d + 1e-3d);
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
    /// Verifies that URN-based coordinate operations surface a specific unsupported reason instead of collapsing into a null GIE row.
    /// </summary>
    [Fact]
    public void TryCreateConversionTransformWithCoordinateOperationUrnReturnsSpecificSkipReason()
    {
        const string operation = "urn:ogc:def:coordinateOperation:NKG::ITRF2000_TO_DK";

        bool created = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.False(created);
        Assert.Null(transform);
        Assert.Contains("URN-based", skipReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
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
        MethodInfo method = typeof(GieBuiltinsTheoryTests).GetMethod("GetCasesFromFixture", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate GieBuiltinsTheoryTests.GetCasesFromFixture.");

        var rows = new List<object>();
        foreach (object row in Assert.IsAssignableFrom<System.Collections.IEnumerable>(method.Invoke(null, [fixtureName, 300])))
        {
            rows.Add(row);
        }

        Assert.NotEmpty(rows);

        PropertyInfo dataProperty = rows[0].GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Could not locate TheoryDataRow.Data.");
        var firstCase = dataProperty.GetValue(rows[0]) as GieCase;

        Assert.NotNull(firstCase);
        Assert.NotNull(firstCase!.Operation);
        Assert.NotEmpty(firstCase.Operation);
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
