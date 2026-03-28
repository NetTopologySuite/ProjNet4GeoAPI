// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class AxisSwapMathTransformTests
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ConstructorWithInvalidDimensionThrowsArgumentOutOfRange(int dimension)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AxisSwapMathTransform(dimension, 0, 1, 1, 1, 2, 1, 3, 1));

        Assert.Equal("dimension", exception.ParamName);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void ConstructorWithInvalidSourceIndexThrowsArgumentOutOfRange(int sourceIndex)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AxisSwapMathTransform(4, sourceIndex, 1, 1, 1, 2, 1, 3, 1));

        Assert.Equal("xSourceIndex", exception.ParamName);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-2)]
    public void ConstructorWithInvalidSignThrowsArgumentOutOfRange(int sign)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AxisSwapMathTransform(4, 0, sign, 1, 1, 2, 1, 3, 1));

        Assert.Equal("xSign", exception.ParamName);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void IdentityReturnsTrueForCanonical4DMapping()
    {
        var transform = new AxisSwapMathTransform(4, 0, 1, 1, 1, 2, 1, 3, 1);

        Assert.True(transform.Identity());
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void IdentityReturnsFalseWhen4DMappingChangesSign()
    {
        var transform = new AxisSwapMathTransform(4, 0, 1, 1, 1, 2, 1, 3, -1);

        Assert.False(transform.Identity());
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void InverseRoundTrips4DPoint()
    {
        var transform = new AxisSwapMathTransform(4, 3, 1, 2, 1, 1, -1, 0, 1);
        double[] input = [2d, 49d, 10d, 100d];

        double[] transformed = transform.Transform(input);
        double[] roundtrip = transform.Inverse().Transform(transformed);

        Assert.Equal(100d, transformed[0], 12);
        Assert.Equal(10d, transformed[1], 12);
        Assert.Equal(-49d, transformed[2], 12);
        Assert.Equal(2d, transformed[3], 12);

        Assert.Equal(input[0], roundtrip[0], 12);
        Assert.Equal(input[1], roundtrip[1], 12);
        Assert.Equal(input[2], roundtrip[2], 12);
        Assert.Equal(input[3], roundtrip[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void InvertMutatesIntoInverseMapping()
    {
        var transform = new AxisSwapMathTransform(4, 3, 1, 2, 1, 1, -1, 0, 1);
        double[] input = [2d, 49d, 10d, 100d];
        double[] transformed = transform.Transform(input);

        transform.Invert();
        double[] roundtrip = transform.Transform(transformed);

        Assert.Equal(input[0], roundtrip[0], 12);
        Assert.Equal(input[1], roundtrip[1], 12);
        Assert.Equal(input[2], roundtrip[2], 12);
        Assert.Equal(input[3], roundtrip[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TwoDimensionalTransformLeavesZUntouched()
    {
        var transform = new AxisSwapMathTransform(2, 1, 1, 0, -1, 2, 1, 3, 1);
        double x = 3d;
        double y = 4d;
        double z = 5d;

        transform.Transform(ref x, ref y, ref z);

        Assert.Equal(4d, x, 12);
        Assert.Equal(-3d, y, 12);
        Assert.Equal(5d, z, 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ThreeDimensionalTransformLeavesTimeUntouched()
    {
        var transform = new AxisSwapMathTransform(3, 1, 1, 0, 1, 2, -1, 3, 1);
        double x = 3d;
        double y = 4d;
        double z = 5d;
        double t = 6d;

        transform.Transform(ref x, ref y, ref z, ref t);

        Assert.Equal(4d, x, 12);
        Assert.Equal(3d, y, 12);
        Assert.Equal(-5d, z, 12);
        Assert.Equal(6d, t, 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void WktAndXmlPropertiesThrowNotImplementedException()
    {
        var transform = new AxisSwapMathTransform(2, 0, 1, 1, 1, 2, 1, 3, 1);

        Assert.Throws<NotImplementedException>(() => _ = transform.WKT);
        Assert.Throws<NotImplementedException>(() => _ = transform.XML);
    }
}
