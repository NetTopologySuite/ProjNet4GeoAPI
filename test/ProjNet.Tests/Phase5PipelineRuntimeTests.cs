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
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class Phase5PipelineRuntimeTests
{
    private static readonly double[] PipelineNoopInput = { 1.5d, 2.25d, 9d };
    private static readonly double[] PipelineSwapInput = { 100d, 200d };

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithUnitConvertAndAxisSwapConvertsAndSwaps()
    {
        const string operation = "+proj=pipeline +step +proj=unitconvert +xy_in=m +xy_out=ft +step +proj=axisswap +order=2,1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PipelineSwapInput);

        Assert.Equal(656.1679790026246d, transformed[0], 9);
        Assert.Equal(328.0839895013123d, transformed[1], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithNoopSetAndUnitConvertAppliesSetOverride()
    {
        const string operation = "+proj=pipeline +step +proj=noop +step +proj=set +v_3=17 +step +proj=unitconvert +xy_in=km +xy_out=m";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PipelineNoopInput);

        Assert.Equal(1500d, transformed[0], 10);
        Assert.Equal(2250d, transformed[1], 10);
        Assert.Equal(17d, transformed[2], 10);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineInvalidAxisSwapOrderReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=axisswap +order=1,1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("+order", skipReason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies 4D epoch propagation through composite pipelines with kinematic Helmert.
    /// </summary>
    [Fact]
    public void PipelineWithKinematicHelmertPreservesEpochAndAppliesDynamicParameters()
    {
        const string operation = "+proj=pipeline +step +proj=noop +step +proj=helmert +convention=position_vector +x=0.0127 +dx=-0.0029 +rx=-0.00039 +drx=-0.00011 +y=0.0065 +dy=-0.0002 +ry=0.00080 +dry=-0.00019 +z=-0.0209 +dz=-0.0006 +rz=-0.00114 +drz=0.00007 +s=0.00195 +ds=0.00001 +t_epoch=1988.0";
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform([3370658.37800d, 711877.31400d, 5349787.08600d, 2018.0d]);

        Assert.Equal(4, transformed.Length);
        Assert.InRange(Math.Abs(transformed[0] - 3370658.18087d), 0d, 1e-4d);
        Assert.InRange(Math.Abs(transformed[1] - 711877.42750d), 0d, 1e-4d);
        Assert.InRange(Math.Abs(transformed[2] - 5349787.12648d), 0d, 1e-4d);
        Assert.InRange(Math.Abs(transformed[3] - 2018.0d), 0d, 1e-12d);
    }
}
