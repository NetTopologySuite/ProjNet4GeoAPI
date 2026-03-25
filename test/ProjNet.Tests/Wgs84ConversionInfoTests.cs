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

        ArgumentException exception = null;
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
}
