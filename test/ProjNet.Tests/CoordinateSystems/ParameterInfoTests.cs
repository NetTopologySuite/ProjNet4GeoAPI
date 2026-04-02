// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="ParameterInfo"/>.
/// </summary>
public class ParameterInfoTests
{
    /// <summary>
    /// Verifies that <see cref="ParameterInfo.NumParameters"/> returns zero when the parameter list is unset.
    /// </summary>
    [Fact]
    public void NumParameters_WithNullParameters_ReturnsZero()
    {
        var parameterInfo = new ParameterInfo();

        Assert.Equal(0, parameterInfo.NumParameters);
    }

    /// <summary>
    /// Verifies that <see cref="ParameterInfo.NumParameters"/> returns the current parameter count.
    /// </summary>
    [Fact]
    public void NumParameters_WithParameters_ReturnsCount()
    {
        var parameterInfo = new ParameterInfo
        {
            Parameters =
            [
                new Parameter("scale_factor", 0.9996),
                new Parameter("central_meridian", 9.0),
            ],
        };

        Assert.Equal(2, parameterInfo.NumParameters);
    }

    /// <summary>
    /// Verifies that <see cref="ParameterInfo.DefaultParameters"/> returns an empty array.
    /// </summary>
    [Fact]
    public void DefaultParameters_ReturnsEmptyArray()
    {
        var parameterInfo = new ParameterInfo();

        Parameter[] parameters = parameterInfo.DefaultParameters();

        Assert.Empty(parameters);
    }

    /// <summary>
    /// Verifies that <see cref="ParameterInfo.GetParameterByName"/> returns <see langword="null"/> when no parameters are available.
    /// </summary>
    [Fact]
    public void GetParameterByName_WithNullParameterList_ReturnsNull()
    {
        var parameterInfo = new ParameterInfo();

        Parameter? parameter = parameterInfo.GetParameterByName("scale_factor");

        Assert.Null(parameter);
    }

    /// <summary>
    /// Verifies that <see cref="ParameterInfo.GetParameterByName"/> returns the matching parameter instance.
    /// </summary>
    [Fact]
    public void GetParameterByName_WithMatchingName_ReturnsParameter()
    {
        var expected = new Parameter("central_meridian", 15.0);
        var parameterInfo = new ParameterInfo
        {
            Parameters =
            [
                new Parameter("scale_factor", 1.0),
                expected,
            ],
        };

        Parameter? parameter = parameterInfo.GetParameterByName("central_meridian");

        Assert.Same(expected, parameter);
    }

    /// <summary>
    /// Verifies that <see cref="ParameterInfo.GetParameterByName"/> skips null entries and returns <see langword="null"/> when the name is missing.
    /// </summary>
    [Fact]
    public void GetParameterByName_WithNullEntryAndMissingName_ReturnsNull()
    {
        var parameterInfo = new ParameterInfo
        {
            Parameters =
            [
                null!,
                new Parameter("scale_factor", 1.0),
            ],
        };

        Parameter? parameter = parameterInfo.GetParameterByName("latitude_of_origin");

        Assert.Null(parameter);
    }
}
