// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

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
    [Fact]
    public void HgridshiftWithLittleEndianNtv2GridAppliesExpectedShift()
    {
        string gridPath = FindGridPath("test_hgrid_little_endian.gsb");
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
    [Fact]
    public void HgridshiftWithBigEndianNtv2GridAppliesExpectedShift()
    {
        string gridPath = FindGridPath("test_hgrid_big_endian.gsb");
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
    [Fact]
    public void HgridshiftWithInverseFlagForSyntheticFixtureSignalsOutsideGrid()
    {
        string gridPath = FindGridPath("test_hgrid_little_endian.gsb");
        string operation = "+inv +proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        Assert.Throws<ArgumentException>(() => transform.Transform(HorizontalGridInverseInput));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GridshiftWithNtv2GridUsesHorizontalShiftImplementation()
    {
        string gridPath = FindGridPath("test_hgrid_little_endian.gsb");
        string operation = "+proj=gridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(HorizontalGridInput);
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
    }

    private static string FindGridPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "spec", "PROJ", "data", "tests", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("Could not locate PROJ test grid fixture.", fileName);
    }
}
