// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
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
}
