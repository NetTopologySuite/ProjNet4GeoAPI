// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies the shared finite-double parsing helpers used by runtime transformation argument parsing.
/// </summary>
public class SpanParseUtilityTests
{
    /// <summary>
    /// Verifies the shared finite-double parser accepts representative invariant-culture values.
    /// </summary>
    /// <param name="token">Token to parse.</param>
    /// <param name="expected">Expected parsed value.</param>
    [Theory]
    [InlineData("1.5", 1.5d)]
    [InlineData(" 1234.5 ", 1234.5d)]
    [InlineData("1,234.5", 1234.5d)]
    public void TryParseFiniteDouble_ValidFiniteValues_ReturnsTrue(string token, double expected)
    {
        bool parsed = SpanParseUtility.TryParseFiniteDouble(token, out double value);

        Assert.True(parsed);
        Assert.Equal(expected, value, 12);
    }

    /// <summary>
    /// Verifies the shared finite-double parser rejects invalid and non-finite values.
    /// </summary>
    /// <param name="token">Token to parse.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("abc")]
    public void TryParseFiniteDouble_InvalidOrNonFiniteValues_ReturnsFalse(string token)
    {
        bool parsed = SpanParseUtility.TryParseFiniteDouble(token, out double value);

        Assert.False(parsed);
        Assert.True(double.IsNaN(value) || double.IsInfinity(value) || value == 0d);
    }

    /// <summary>
    /// Verifies CSV parsing still rejects non-finite segments once the shared finite-double logic is centralized.
    /// </summary>
    [Fact]
    public void TryParseCsvValues_NonFiniteSegment_ReturnsInvalidValue()
    {
        Span<double> destination = stackalloc double[2];

        CsvParseStatus status = SpanParseUtility.TryParseCsvValues("1,NaN", destination, out int parsedCount);

        Assert.Equal(CsvParseStatus.InvalidValue, status);
        Assert.Equal(1, parsedCount);
        Assert.Equal(1d, destination[0], 12);
    }

    /// <summary>
    /// Verifies the presence-check overload only succeeds when the optional key exists and parses as a finite value.
    /// </summary>
    [Fact]
    public void TryGetOptionalDouble_PresentFiniteValue_ReturnsTrueAndParsedValue()
    {
        var args = new Dictionary<string, string>
        {
            ["dx"] = "1.25",
        };

        bool parsed = SpanParseUtility.TryGetOptionalDouble(args, "dx", out double value);

        Assert.True(parsed);
        Assert.Equal(1.25d, value, 12);
    }

    /// <summary>
    /// Verifies the presence-check overload treats a missing key as absent instead of injecting a default value.
    /// </summary>
    [Fact]
    public void TryGetOptionalDouble_MissingKey_ReturnsFalse()
    {
        bool parsed = SpanParseUtility.TryGetOptionalDouble(new Dictionary<string, string>(), "dx", out double value);

        Assert.False(parsed);
        Assert.Equal(0d, value);
    }

    /// <summary>
    /// Verifies the default-value overload returns the supplied fallback when the key is absent.
    /// </summary>
    [Fact]
    public void TryGetOptionalDouble_WithDefault_MissingKey_ReturnsDefaultValue()
    {
        bool parsed = SpanParseUtility.TryGetOptionalDouble(
            new Dictionary<string, string>(),
            "scale",
            2.5d,
            out double value,
            out string? skipReason);

        Assert.True(parsed);
        Assert.Null(skipReason);
        Assert.Equal(2.5d, value, 12);
    }

    /// <summary>
    /// Verifies the diagnostic overload reports invalid optional values without overwriting the zero default.
    /// </summary>
    [Fact]
    public void TryGetOptionalDouble_WithImplicitZeroDefault_InvalidToken_ReturnsReason()
    {
        var args = new Dictionary<string, string>
        {
            ["dx"] = "oops",
        };

        bool parsed = SpanParseUtility.TryGetOptionalDouble(args, "dx", out double value, out string? skipReason);

        Assert.False(parsed);
        Assert.Equal(0d, value);
        Assert.Equal("Invalid value for +dx.", skipReason);
    }

    /// <summary>
    /// Verifies the default-value overload reports invalid tokens and leaves the parser's failed-value output in place.
    /// </summary>
    [Fact]
    public void TryGetOptionalDouble_WithDefault_InvalidToken_ReturnsReasonAndFailedValue()
    {
        var args = new Dictionary<string, string>
        {
            ["scale"] = "not-a-number",
        };

        bool parsed = SpanParseUtility.TryGetOptionalDouble(args, "scale", 3d, out double value, out string? skipReason);

        Assert.False(parsed);
        Assert.Equal(0d, value);
        Assert.Equal("Invalid value for +scale.", skipReason);
    }
}
