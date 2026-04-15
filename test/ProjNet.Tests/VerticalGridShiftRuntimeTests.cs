// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for vertical grid shift operations at runtime using GTX grid files.
/// </summary>
public class VerticalGridShiftRuntimeTests
{
    private static readonly double[] VerticalGridInput = [12d, 56d, 0d];

    /// <summary>
    /// Verifies that a <c>vgridshift</c> operation backed by a GTX grid file applies the expected vertical shift, respecting the <c>+multiplier</c> option.
    /// </summary>
    /// <param name="multiplierToken">Multiplier token appended to operation.</param>
    /// <param name="expectedZ">Expected transformed Z value.</param>
    [Theory]
    [InlineData("", -36.9959410718d)]
    [InlineData(" +multiplier=1", 36.9959410718d)]
    public void VgridshiftWithGtxGridAppliesExpectedVerticalShift(string multiplierToken, double expectedZ)
    {
        string gridPath = FindGridPath("egm96_15.gtx");
        string operation = $"+proj=vgridshift +grids={gridPath}{multiplierToken}";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);

        Assert.True(ok, skipReason);
        double[] output = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(VerticalGridInput);
        Assert.Equal(12d, output[0], 12);
        Assert.Equal(56d, output[1], 12);
        Assert.Equal(expectedZ, output[2], 9);
    }

    /// <summary>
    /// Verifies that applying the forward then inverse <c>vgridshift</c> operations round-trips a single point back to its original coordinates.
    /// </summary>
    [Fact]
    public void VgridshiftWithInverseFlagRoundtripsSinglePoint()
    {
        string gridPath = FindGridPath("egm96_15.gtx");
        string forwardOperation = $"+proj=vgridshift +grids={gridPath}";
        string inverseOperation = $"+inv +proj=vgridshift +grids={gridPath}";

        bool forwardOk = ProjPipelineMathTransformFactory.TryCreateMathTransform(forwardOperation, out MathTransform? forward, out string? forwardSkipReason);
        bool inverseOk = ProjPipelineMathTransformFactory.TryCreateMathTransform(inverseOperation, out MathTransform? inverse, out string? inverseSkipReason);

        Assert.True(forwardOk, forwardSkipReason);
        Assert.True(inverseOk, inverseSkipReason);

        double[] shifted = Assert.IsType<MathTransform>(forward, exactMatch: false).Transform(VerticalGridInput);
        double[] unshifted = Assert.IsType<MathTransform>(inverse, exactMatch: false).Transform(shifted);

        Assert.Equal(12d, unshifted[0], 10);
        Assert.Equal(56d, unshifted[1], 10);
        Assert.Equal(0d, unshifted[2], 7);
    }

    /// <summary>
    /// Verifies that transforming a coordinate that falls outside the grid extent throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void VgridshiftOutsideGridExtentThrowsArgumentException()
    {
        string gridPath = FindGridPath("test_nodata.gtx");
        string operation = $"+proj=vgridshift +grids={gridPath}";

        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);

        Assert.Throws<ArgumentException>(() => Assert.IsType<MathTransform>(transform, exactMatch: false).Transform(VerticalGridInput));
    }

    private static string FindGridPath(string fileName)
    {
        string direct = Path.Combine(AppContext.BaseDirectory, "Fixtures", "grids", fileName);
        if (File.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "grids", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate local test grid fixture under test\\ProjNet.Tests\\Fixtures\\grids.", fileName);
    }
}
