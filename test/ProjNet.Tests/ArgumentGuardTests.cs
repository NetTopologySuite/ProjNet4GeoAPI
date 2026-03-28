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
        Assert.Contains(Assert.IsAssignableFrom<string>(typeof(string).FullName), exception.Message, StringComparison.Ordinal);
    }
}
