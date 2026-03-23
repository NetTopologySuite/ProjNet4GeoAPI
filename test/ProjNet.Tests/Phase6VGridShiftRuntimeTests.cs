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
public class Phase6VGridShiftRuntimeTests
{
    private static readonly double[] VerticalGridInput = { 12d, 56d, 0d };

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="multiplierToken">Multiplier token appended to operation.</param>
    /// <param name="expectedZ">Expected transformed Z value.</param>
    [Theory]
    [InlineData("", -36.9959410718d)]
    [InlineData(" +multiplier=1", 36.9959410718d)]
    public void VgridshiftWithGtxGridAppliesExpectedVerticalShift(string multiplierToken, double expectedZ)
    {
        string gridPath = FindGridPath("egm96_15_downsampled.gtx");
        string operation = "+proj=vgridshift +grids=" + gridPath + multiplierToken;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] output = transform.Transform(VerticalGridInput);
        Assert.Equal(12d, output[0], 12);
        Assert.Equal(56d, output[1], 12);
        Assert.Equal(expectedZ, output[2], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void VgridshiftWithInverseFlagRoundtripsSinglePoint()
    {
        string gridPath = FindGridPath("egm96_15_downsampled.gtx");
        string forwardOperation = "+proj=vgridshift +grids=" + gridPath;
        string inverseOperation = "+inv +proj=vgridshift +grids=" + gridPath;

        bool forwardOk = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(forwardOperation, out MathTransform forward, out string forwardSkipReason);
        bool inverseOk = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(inverseOperation, out MathTransform inverse, out string inverseSkipReason);

        Assert.True(forwardOk, forwardSkipReason);
        Assert.True(inverseOk, inverseSkipReason);

        double[] shifted = forward.Transform(VerticalGridInput);
        double[] unshifted = inverse.Transform(shifted);

        Assert.Equal(12d, unshifted[0], 10);
        Assert.Equal(56d, unshifted[1], 10);
        Assert.Equal(0d, unshifted[2], 7);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void VgridshiftOutsideGridExtentThrowsArgumentException()
    {
        string gridPath = FindGridPath("test_nodata.gtx");
        string operation = "+proj=vgridshift +grids=" + gridPath;

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);

        Assert.Throws<ArgumentException>(() => transform.Transform(VerticalGridInput));
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
