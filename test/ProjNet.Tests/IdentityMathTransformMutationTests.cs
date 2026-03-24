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
/// Verifies mutation-sensitive behavior of <see cref="IdentityMathTransform"/>.
/// </summary>
public class IdentityMathTransformMutationTests
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
