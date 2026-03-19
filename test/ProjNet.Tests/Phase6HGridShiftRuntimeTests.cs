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

public class Phase6HGridShiftRuntimeTests
{
    [Fact]
    public void Hgridshift_WithLittleEndianNtv2Grid_AppliesExpectedShift()
    {
        string gridPath = FindGridPath("test_hgrid_little_endian.gsb");
        string operation = "+proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(new[] { 4.5d, 52.5d, 0d });
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
        Assert.Equal(0d, output[2], 9);
    }

    [Fact]
    public void Hgridshift_WithBigEndianNtv2Grid_AppliesExpectedShift()
    {
        string gridPath = FindGridPath("test_hgrid_big_endian.gsb");
        string operation = "+proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(new[] { 4.5d, 52.5d, 0d });
        Assert.Equal(5.875d, output[0], 9);
        Assert.Equal(55.375d, output[1], 9);
        Assert.Equal(0d, output[2], 9);
    }

    [Fact]
    public void Hgridshift_WithInverseFlag_ForSyntheticFixtureSignalsOutsideGrid()
    {
        string gridPath = FindGridPath("test_hgrid_little_endian.gsb");
        string operation = "+inv +proj=hgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        Assert.Throws<ArgumentException>(() => transform.Transform(new[] { 5.875d, 55.375d, 0d }));
    }

    [Fact]
    public void Gridshift_WithNtv2Grid_UsesHorizontalShiftImplementation()
    {
        string gridPath = FindGridPath("test_hgrid_little_endian.gsb");
        string operation = "+proj=gridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(new[] { 4.5d, 52.5d, 0d });
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
