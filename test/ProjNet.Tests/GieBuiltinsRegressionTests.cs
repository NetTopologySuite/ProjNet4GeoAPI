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
}
