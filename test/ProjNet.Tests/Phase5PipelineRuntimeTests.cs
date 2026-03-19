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

using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class Phase5PipelineRuntimeTests
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithUnitConvertAndAxisSwapConvertsAndSwaps()
    {
        const string operation = "+proj=pipeline +step +proj=unitconvert +xy_in=m +xy_out=ft +step +proj=axisswap +order=2,1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(new[] { 100d, 200d });

        Assert.Equal(656.1679790026246d, transformed[0], 9);
        Assert.Equal(328.0839895013123d, transformed[1], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithNoopSetAndUnitConvertAppliesRelevantStep()
    {
        const string operation = "+proj=pipeline +step +proj=noop +step +proj=set +v_3=17 +step +proj=unitconvert +xy_in=km +xy_out=m";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(new[] { 1.5d, 2.25d, 9d });

        Assert.Equal(1500d, transformed[0], 10);
        Assert.Equal(2250d, transformed[1], 10);
        Assert.Equal(9d, transformed[2], 10);
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
        Assert.Contains("+order", skipReason);
    }
}
