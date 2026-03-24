// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.IO;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class Phase6HGridShiftRuntimeTests
{
    private static readonly double[] HorizontalGridInput = { 4.5d, 52.5d, 0d };
    private static readonly double[] HorizontalGridInverseInput = { 5.875d, 55.375d, 0d };

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridFileName">NTv2 grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid_little_endian.gsb")]
    [InlineData("test_hgrid_big_endian.gsb")]
    public void HgridshiftWithNtv2GridAppliesExpectedShift(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = "+proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(HorizontalGridInput);
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
        Assert.Equal(0d, output[2], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridFileName">NTv2 grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid_little_endian.gsb")]
    [InlineData("test_hgrid_big_endian.gsb")]
    public void HgridshiftWithInverseFlagForSyntheticFixtureSignalsOutsideGrid(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = "+inv +proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        Assert.Throws<ArgumentException>(() => transform.Transform(HorizontalGridInverseInput));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridFileName">NTv2 grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid_little_endian.gsb")]
    [InlineData("test_hgrid_big_endian.gsb")]
    public void GridshiftWithNtv2GridUsesHorizontalShiftImplementation(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = "+proj=gridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(HorizontalGridInput);
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
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
