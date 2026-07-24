// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests.CoordinateSystems.Transformations;

using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies the runtime affine transform implementation created from PROJ pipeline arguments.
/// </summary>
public class AffineRuntimeMathTransformTests
{
    /// <summary>
    /// Verifies that affine runtime parameters transform spatial and temporal ordinates as expected.
    /// </summary>
    [Fact]
    public void TryCreate_WithAffineParameters_TransformsFourDimensionalPoint()
    {
        AffineRuntimeMathTransform transform = CreateTransform();

        double[] transformed = transform.Transform([1d, 2d, 3d, 4d]);

        Assert.Equal(9d, transformed[0], 12);
        Assert.Equal(2d, transformed[1], 12);
        Assert.Equal(23d, transformed[2], 12);
        Assert.Equal(21d, transformed[3], 12);
    }

    /// <summary>
    /// Verifies that the computed inverse restores the original 4D coordinate and is cached.
    /// </summary>
    [Fact]
    public void Inverse_RestoresOriginalFourDimensionalPoint()
    {
        AffineRuntimeMathTransform transform = CreateTransform();
        MathTransform inverse = transform.Inverse();

        Assert.Same(inverse, transform.Inverse());

        double[] transformed = transform.Transform([1d, 2d, 3d, 4d]);
        double[] restored = inverse.Transform(transformed);

        Assert.Equal(1d, restored[0], 12);
        Assert.Equal(2d, restored[1], 12);
        Assert.Equal(3d, restored[2], 12);
        Assert.Equal(4d, restored[3], 12);
    }

    /// <summary>
    /// Verifies that default affine parameters collapse to an identity transform.
    /// </summary>
    [Fact]
    public void TryCreate_WithDefaultParameters_ReturnsIdentityTransform()
    {
        bool created = AffineRuntimeMathTransform.TryCreate([], out MathTransform? transform, out string? skipReason);

        Assert.True(created);
        Assert.Null(skipReason);
        Assert.IsType<IdentityMathTransform>(transform);
        Assert.True(transform.Identity());
    }

    /// <summary>
    /// Verifies that taking the inverse twice recreates the original mapping.
    /// </summary>
    [Fact]
    public void InverseOfInverse_PreservesForwardMapping()
    {
        AffineRuntimeMathTransform transform = CreateTransform();
        MathTransform doubleInverse = transform.Inverse().Inverse();

        double[] expected = transform.Transform([2d, -1d, 0.5d, 8d]);
        double[] actual = doubleInverse.Transform([2d, -1d, 0.5d, 8d]);

        Assert.Equal(expected[0], actual[0], 12);
        Assert.Equal(expected[1], actual[1], 12);
        Assert.Equal(expected[2], actual[2], 12);
        Assert.Equal(expected[3], actual[3], 12);
    }

    private static AffineRuntimeMathTransform CreateTransform()
    {
        bool created = AffineRuntimeMathTransform.TryCreate(CreateArguments(), out MathTransform? transform, out string? skipReason);

        Assert.True(created);
        Assert.Null(skipReason);
        return Assert.IsType<AffineRuntimeMathTransform>(transform);
    }

    private static Dictionary<string, string> CreateArguments()
    {
        return new Dictionary<string, string>
        {
            ["xoff"] = "5",
            ["yoff"] = "-7",
            ["zoff"] = "11",
            ["toff"] = "13",
            ["s11"] = "2",
            ["s12"] = "1",
            ["s22"] = "3",
            ["s23"] = "1",
            ["s33"] = "4",
            ["tscale"] = "2",
        };
    }
}
