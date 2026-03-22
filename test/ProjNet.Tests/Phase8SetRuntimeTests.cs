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
/// Validates M8 runtime parity for <c>set</c>.
/// </summary>
public class Phase8SetRuntimeTests
{
    /// <summary>
    /// Verifies that empty set operation behaves as identity.
    /// </summary>
    [Fact]
    public void SetWithoutValuesIsIdentity()
    {
        MathTransform transform = CreateTransform("+proj=set");
        double[] output = transform.Transform(CreatePoint(1d, 2d, 3d));

        Assert.Equal(1d, output[0], 12);
        Assert.Equal(2d, output[1], 12);
        Assert.Equal(3d, output[2], 12);
    }

    /// <summary>
    /// Verifies coordinate component overrides for v_1, v_2 and v_3.
    /// </summary>
    [Fact]
    public void SetOverridesSpecifiedComponents()
    {
        MathTransform transform = CreateTransform("+proj=set +v_1=10 +v_2=20 +v_3=30 +v_4=40");
        double[] output = transform.Transform(CreatePoint(1d, 2d, 3d));

        Assert.Equal(10d, output[0], 12);
        Assert.Equal(20d, output[1], 12);
        Assert.Equal(30d, output[2], 12);
    }

    /// <summary>
    /// Verifies that set behaves the same in inverse direction.
    /// </summary>
    [Fact]
    public void SetInverseUsesSameOverrideRules()
    {
        MathTransform transform = CreateTransform("+proj=set +v_1=10 +v_2=20 +v_3=30 +v_4=40 +inv");
        double[] output = transform.Transform(CreatePoint(1d, 2d, 3d));

        Assert.Equal(10d, output[0], 12);
        Assert.Equal(20d, output[1], 12);
        Assert.Equal(30d, output[2], 12);
    }

    /// <summary>
    /// Verifies partial override semantics.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedX">Expected X output.</param>
    /// <param name="expectedY">Expected Y output.</param>
    /// <param name="expectedZ">Expected Z output.</param>
    [Theory]
    [InlineData("+proj=set +v_1=11", 11d, 2d, 3d)]
    [InlineData("+proj=set +v_2=22", 1d, 22d, 3d)]
    [InlineData("+proj=set +v_3=33", 1d, 2d, 33d)]
    [InlineData("+proj=set +v_1=11 +v_3=33", 11d, 2d, 33d)]
    public void SetCanOverrideSubsetOfComponents(string operation, double expectedX, double expectedY, double expectedZ)
    {
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(1d, 2d, 3d));

        Assert.Equal(expectedX, output[0], 12);
        Assert.Equal(expectedY, output[1], 12);
        Assert.Equal(expectedZ, output[2], 12);
    }

    /// <summary>
    /// Verifies validation errors for invalid set parameter values.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=set +v_1=abc", "v_1")]
    [InlineData("+proj=set +v_2=nan", "v_2")]
    [InlineData("+proj=set +v_3=inf", "v_3")]
    [InlineData("+proj=set +v_4=abc", "v_4")]
    public void SetCreationFailsForInvalidValues(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains(expectedToken, skipReason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that existing pipeline behavior now honors set overrides.
    /// </summary>
    [Fact]
    public void PipelineWithNoopSetAndUnitConvertOverridesZOnly()
    {
        const string operation = "+proj=pipeline +step +proj=noop +step +proj=set +v_3=17 +step +proj=unitconvert +xy_in=km +xy_out=m";

        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(1.5d, 2.25d, 9d));

        Assert.Equal(1500d, output[0], 10);
        Assert.Equal(2250d, output[1], 10);
        Assert.Equal(17d, output[2], 10);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);
        return transform;
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
