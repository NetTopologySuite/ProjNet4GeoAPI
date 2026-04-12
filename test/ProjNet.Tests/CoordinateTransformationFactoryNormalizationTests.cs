// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Reflection;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests the private operation-method normalization helper used by <see cref="CoordinateTransformationFactory"/>.
/// </summary>
public class CoordinateTransformationFactoryNormalizationTests
{
    private static readonly MethodInfo NormalizeOperationMethodNameMethod = typeof(CoordinateTransformationFactory)
        .GetMethod("NormalizeOperationMethodName", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("The CoordinateTransformationFactory.NormalizeOperationMethodName helper could not be located.");

    /// <summary>
    /// Verifies that operation method names are normalized by stripping non-alphanumeric characters and lower-casing the remaining content.
    /// </summary>
    /// <param name="value">The input method name.</param>
    /// <param name="expected">The expected normalized representation.</param>
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("Coordinate Frame rotation", "coordinateframerotation")]
    [InlineData("Geographic 2D offsets", "geographic2doffsets")]
    [InlineData("Time-dependent Position Vector (geocentric)", "timedependentpositionvectorgeocentric")]
    [InlineData("Molodensky-Badekas", "molodenskybadekas")]
    public void NormalizeOperationMethodName_StripsPunctuationAndLowerCases(string? value, string expected)
    {
        string normalized = Assert.IsType<string>(NormalizeOperationMethodNameMethod.Invoke(null, [value]), exactMatch: false);

        Assert.Equal(expected, normalized);
    }
}
