// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies mutation-sensitive behavior of <see cref="IdentityMathTransform"/>.
/// </summary>
public class IdentityMathTransformTests
{
    /// <summary>
    /// Verifies that dimensions below 2 are promoted to 2.
    /// </summary>
    [Fact]
    public void CtorWithDimensionLowerThanTwoPromotesToTwo()
    {
        var transform = new IdentityMathTransform(1);

        Assert.Equal(2, transform.DimSource);
        Assert.Equal(2, transform.DimTarget);
    }

    /// <summary>
    /// Verifies that dimensions above 2 are preserved.
    /// </summary>
    [Fact]
    public void CtorWithDimensionGreaterThanTwoPreservesRequestedDimension()
    {
        var transform = new IdentityMathTransform(3);
        double[] output = transform.Transform([12d, 34d, 56d]);

        Assert.Equal(3, transform.DimSource);
        Assert.Equal(3, transform.DimTarget);
        Assert.Equal(3, output.Length);
        Assert.Equal(56d, output[2], 12);
    }

    /// <summary>
    /// Verifies that the generated WKT carries the configured dimension.
    /// </summary>
    [Fact]
    public void WktContainsConfiguredDimension()
    {
        var transform = new IdentityMathTransform(4);

        Assert.Equal("PARAM_MT[\"Identity\",PARAMETER[\"dimension\",4]]", transform.WKT);
    }

    /// <summary>
    /// Verifies that the transform reports identity semantics.
    /// </summary>
    [Fact]
    public void IdentityReturnsTrue()
    {
        var transform = new IdentityMathTransform(4);

        Assert.True(transform.Identity());
    }
}

