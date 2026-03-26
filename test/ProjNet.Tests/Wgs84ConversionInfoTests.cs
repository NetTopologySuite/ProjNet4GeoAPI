// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests;

using System;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Verifies span-based affine coefficient access on <see cref="Wgs84ConversionInfo"/>.
/// </summary>
public class Wgs84ConversionInfoTests
{
    /// <summary>
    /// Verifies that span-based affine transform output matches the array-based API.
    /// </summary>
    [Fact]
    public void WriteAffineTransformMatchesArrayBasedResult()
    {
        var info = new Wgs84ConversionInfo(570.8, 85.7, 462.8, 4.998, 1.587, 5.261, 3.56);

        Span<double> destination = stackalloc double[7];
        info.WriteAffineTransform(destination);

        double[] expected = info.GetAffineTransform();
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], destination[i], 12);
        }
    }

    /// <summary>
    /// Verifies that too-small destination spans are rejected.
    /// </summary>
    [Fact]
    public void WriteAffineTransformWithSmallDestinationThrows()
    {
        var info = new Wgs84ConversionInfo();
        Span<double> destination = stackalloc double[6];

        ArgumentException exception = default!;
        try
        {
            info.WriteAffineTransform(destination);
        }
        catch (ArgumentException ex)
        {
            exception = ex;
        }

        Assert.NotNull(exception);
        Assert.Equal("destination", exception.ParamName);
    }

    /// <summary>
    /// Verifies that writing affine coefficients updates only the required 7 destination elements.
    /// </summary>
    [Fact]
    public void WriteAffineTransformWithLargerDestinationPreservesTrailingValues()
    {
        var info = new Wgs84ConversionInfo(1.0, 2.0, 3.0, 0.1, 0.2, 0.3, 0.4);
        Span<double> destination = stackalloc double[9];
        destination[0] = -1.0;
        destination[1] = -1.0;
        destination[2] = -1.0;
        destination[3] = -1.0;
        destination[4] = -1.0;
        destination[5] = -1.0;
        destination[6] = -1.0;
        destination[7] = 1234.5;
        destination[8] = -9876.5;

        info.WriteAffineTransform(destination);
        double[] expected = info.GetAffineTransform();

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], destination[i], 12);
        }

        Assert.Equal(1234.5, destination[7], 12);
        Assert.Equal(-9876.5, destination[8], 12);
    }
}
