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
public class Phase6GeoTiffGridRuntimeTests
{
    private static readonly double[] GeoTiffGridInput = { 4.5d, 52.5d, 0d };
    private static readonly double[] GeoTiffNodataInput = { 4.05d, 52.1d, 0d };

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridFileName">GeoTIFF horizontal grid fixture file name.</param>
    [Theory]
    [InlineData("test_hgrid.tif")]
    [InlineData("test_hgrid_positive_west.tif")]
    public void HgridshiftWithGeoTiffGridAppliesExpectedShift(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = "+proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);

        double[] output = transform.Transform(GeoTiffGridInput);
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="gridFileName">GeoTIFF vertical grid fixture file name.</param>
    [Theory]
    [InlineData("test_vgrid_pixelispoint.tif")]
    [InlineData("test_vgrid_uint16_with_scale_offset.tif")]
    public void VgridshiftWithGeoTiffGridAppliesExpectedDefaultShift(string gridFileName)
    {
        string gridPath = FindGridPath(gridFileName);
        string operation = "+proj=vgridshift +grids=" + gridPath + " +multiplier=1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);

        double[] output = transform.Transform(GeoTiffGridInput);
        Assert.Equal(4.5d, output[0], 9);
        Assert.Equal(52.5d, output[1], 9);
        Assert.Equal(11.5d, output[2], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void VgridshiftWithGeoTiffNodataPerformsWeightedInterpolation()
    {
        string gridPath = FindGridPath("test_vgrid_nodata.tif");
        string operation = "+proj=vgridshift +grids=" + gridPath + " +multiplier=1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);

        double[] output = transform.Transform(GeoTiffNodataInput);
        Assert.Equal(10d, output[2], 7);
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
