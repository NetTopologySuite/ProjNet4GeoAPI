// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using Xunit;

/// <summary>
/// Provides unit tests for <see cref="ArgumentGuard"/>.
/// </summary>
public class ArgumentGuardTests
{
    /// <summary>
    /// Verifies that the generic null guard returns the original reference for non-null values.
    /// </summary>
    [Fact]
    public void ThrowIfNullGenericWithNonNullValueReturnsSameReference()
    {
        string value = "projnet";

        string result = ArgumentGuard.ThrowIfNull(value, nameof(value));

        Assert.Same(value, result);
    }

    /// <summary>
    /// Verifies that the generic null guard throws for null values.
    /// </summary>
    [Fact]
    public void ThrowIfNullGenericWithNullValueThrowsArgumentNullException()
    {
        string? value = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ArgumentGuard.ThrowIfNull(value, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the null-or-empty guard returns the original reference for non-empty values.
    /// </summary>
    [Fact]
    public void ThrowIfNullOrEmptyWithNonEmptyValueReturnsSameReference()
    {
        string value = "valid";

        string result = ArgumentGuard.ThrowIfNullOrEmpty(value, nameof(value));

        Assert.Same(value, result);
    }

    /// <summary>
    /// Verifies that the null-or-empty guard throws for empty values.
    /// </summary>
    [Fact]
    public void ThrowIfNullOrEmptyWithEmptyValueThrowsArgumentException()
    {
        string value = string.Empty;

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ArgumentGuard.ThrowIfNullOrEmpty(value, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the null-or-whitespace guard returns the original reference for non-whitespace values.
    /// </summary>
    [Fact]
    public void ThrowIfNullOrWhiteSpaceWithContentReturnsSameReference()
    {
        string value = "valid";

        string result = ArgumentGuard.ThrowIfNullOrWhiteSpace(value, nameof(value));

        Assert.Same(value, result);
    }

    /// <summary>
    /// Verifies that the null-or-whitespace guard throws for whitespace values.
    /// </summary>
    [Fact]
    public void ThrowIfNullOrWhiteSpaceWithWhitespaceThrowsArgumentException()
    {
        string value = "  ";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ArgumentGuard.ThrowIfNullOrWhiteSpace(value, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the type guard returns the cast value when the type matches.
    /// </summary>
    [Fact]
    public void ThrowIfNotTypeWithMatchingTypeReturnsCastValue()
    {
        object value = "projnet";

        string result = ArgumentGuard.ThrowIfNotType<string>(value, nameof(value));

        Assert.Equal("projnet", result);
    }

    /// <summary>
    /// Verifies that the type guard throws when the value is null.
    /// </summary>
    [Fact]
    public void ThrowIfNotTypeWithNullValueThrowsArgumentNullException()
    {
        object? value = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => ArgumentGuard.ThrowIfNotType<string>(value, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the type guard throws when the runtime type does not match the expected type.
    /// </summary>
    [Fact]
    public void ThrowIfNotTypeWithMismatchingTypeThrowsArgumentException()
    {
        object value = 123;

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ArgumentGuard.ThrowIfNotType<string>(value, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
        Assert.Contains(Assert.IsType<string>(typeof(string).FullName), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the inclusive range guard accepts values on both boundaries and inside the range.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    [Theory]
    [InlineData(1, 1, 10)]
    [InlineData(5, 1, 10)]
    [InlineData(10, 1, 10)]
    public void ThrowIfOutOfRangeWithInclusiveValueSucceeds(int value, int min, int max)
    {
        ArgumentGuard.ThrowIfOutOfRange(value, min, max, nameof(value));
    }

    /// <summary>
    /// Verifies that the inclusive range guard throws when the value is below the lower bound.
    /// </summary>
    [Fact]
    public void ThrowIfOutOfRangeWithValueBelowMinimumThrowsArgumentOutOfRangeException()
    {
        const int value = 0;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ArgumentGuard.ThrowIfOutOfRange(value, 1, 10, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the inclusive range guard throws when the value is above the upper bound.
    /// </summary>
    [Fact]
    public void ThrowIfOutOfRangeWithValueAboveMaximumThrowsArgumentOutOfRangeException()
    {
        const int value = 11;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ArgumentGuard.ThrowIfOutOfRange(value, 1, 10, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the inclusive range guard rejects invalid ranges where the minimum exceeds the maximum.
    /// </summary>
    [Fact]
    public void ThrowIfOutOfRangeWithInvertedBoundsThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ArgumentGuard.ThrowIfOutOfRange(5, 10, 1, "value"));

        Assert.Equal("min", exception.ParamName);
        Assert.Contains("minimum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that the negative-value guard accepts zero and positive values.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(1.5d)]
    public void ThrowIfNegativeWithNonNegativeValueSucceeds(double value)
    {
        ArgumentGuard.ThrowIfNegative(value, nameof(value));
    }

    /// <summary>
    /// Verifies that the negative-value guard throws for negative input.
    /// </summary>
    [Fact]
    public void ThrowIfNegativeWithNegativeValueThrowsArgumentOutOfRangeException()
    {
        const double value = -0.1d;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ArgumentGuard.ThrowIfNegative(value, nameof(value)));

        Assert.Equal(nameof(value), exception.ParamName);
    }

    /// <summary>
    /// Verifies that the finite-number guard accepts finite values.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(-1.5d)]
    [InlineData(42.25d)]
    public void ThrowIfNotFiniteWithFiniteValueSucceeds(double value)
    {
        ArgumentGuard.ThrowIfNotFinite(value, nameof(value));
    }

    /// <summary>
    /// Verifies that the finite-number guard throws for NaN and infinity inputs.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ThrowIfNotFiniteWithNonFiniteValueThrowsArgumentOutOfRangeException(double value)
    {
        const string message = "Custom finite-value message.";

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ArgumentGuard.ThrowIfNotFinite(value, nameof(value), message));

        Assert.Equal(nameof(value), exception.ParamName);
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }
}
