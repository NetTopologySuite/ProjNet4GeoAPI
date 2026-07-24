// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.CoordinateSystems.Transformations;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for <see cref="CompositeMathTransform"/>.
/// </summary>
public class CompositeMathTransformTests
{
    /// <summary>
    /// Verifies that DimSource is taken from the first transform in the chain.
    /// </summary>
    [Fact]
    public void DimSource_ReturnsFirstTransformDimSource()
    {
        var composite = new CompositeMathTransform([new IdentityMathTransform(3)]);

        Assert.Equal(3, composite.DimSource);
    }

    /// <summary>
    /// Verifies that DimTarget is taken from the last transform in the chain.
    /// </summary>
    [Fact]
    public void DimTarget_ReturnsLastTransformDimTarget()
    {
        var composite = new CompositeMathTransform(
        [
            new IdentityMathTransform(2),
            new IdentityMathTransform(4),
        ]);

        Assert.Equal(4, composite.DimTarget);
    }

    /// <summary>
    /// Verifies that Identity returns true when all transforms are identity.
    /// </summary>
    [Fact]
    public void Identity_AllIdentity_ReturnsTrue()
    {
        var composite = new CompositeMathTransform(
        [
            new IdentityMathTransform(2),
            new IdentityMathTransform(2),
        ]);

        Assert.True(composite.Identity());
    }

    /// <summary>
    /// Verifies that Identity returns false when any transform is not identity.
    /// </summary>
    [Fact]
    public void Identity_ContainsNonIdentity_ReturnsFalse()
    {
        var composite = new CompositeMathTransform(
        [
            new IdentityMathTransform(2),
            new OffsetMathTransform(5.0),
        ]);

        Assert.False(composite.Identity());
    }

    /// <summary>
    /// Verifies that Transform chains multiple transforms sequentially.
    /// </summary>
    [Fact]
    public void Transform_ChainsTransformsSequentially()
    {
        var composite = new CompositeMathTransform(
        [
            new OffsetMathTransform(10.0),
            new OffsetMathTransform(5.0),
        ]);

        double x = 1.0, y = 2.0, z = 0.0;
        composite.Transform(ref x, ref y, ref z);

        Assert.Equal(16.0, x, 12);
        Assert.Equal(17.0, y, 12);
    }

    /// <summary>
    /// Verifies that a single identity transform leaves values unchanged.
    /// </summary>
    [Fact]
    public void Transform_SingleIdentity_LeavesValuesUnchanged()
    {
        var composite = new CompositeMathTransform([new IdentityMathTransform(2)]);

        double x = 42.0, y = 99.0, z = 0.0;
        composite.Transform(ref x, ref y, ref z);

        Assert.Equal(42.0, x, 12);
        Assert.Equal(99.0, y, 12);
    }

    /// <summary>
    /// Verifies that Inverse returns a transform that reverses the chain order and inverts each transform.
    /// </summary>
    [Fact]
    public void Inverse_ReversesAndInvertsChain()
    {
        var composite = new CompositeMathTransform(
        [
            new OffsetMathTransform(10.0),
            new OffsetMathTransform(5.0),
        ]);

        MathTransform inverse = composite.Inverse();

        double x = 16.0, y = 17.0, z = 0.0;
        inverse.Transform(ref x, ref y, ref z);

        Assert.Equal(1.0, x, 12);
        Assert.Equal(2.0, y, 12);
    }

    /// <summary>
    /// Verifies that Inverse returns the same instance on repeated calls (caching).
    /// </summary>
    [Fact]
    public void Inverse_ReturnsSameInstanceOnRepeatedCalls()
    {
        var composite = new CompositeMathTransform([new IdentityMathTransform(2)]);

        MathTransform inverse1 = composite.Inverse();
        MathTransform inverse2 = composite.Inverse();

        Assert.Same(inverse1, inverse2);
    }

    /// <summary>
    /// Verifies that Invert modifies the composite in place.
    /// </summary>
    [Fact]
    public void Invert_ModifiesTransformInPlace()
    {
        var composite = new CompositeMathTransform(
        [
            new OffsetMathTransform(10.0),
            new OffsetMathTransform(5.0),
        ]);

        // Forward: x=0 -> x=15
        double x = 0.0, y = 0.0, z = 0.0;
        composite.Transform(ref x, ref y, ref z);
        Assert.Equal(15.0, x, 12);

        // Invert in place: now should subtract
        composite.Invert();
        x = 15.0;
        y = 15.0;
        z = 0.0;
        composite.Transform(ref x, ref y, ref z);

        Assert.Equal(0.0, x, 12);
        Assert.Equal(0.0, y, 12);
    }

    /// <summary>
    /// Verifies that in-place inversion can reverse immutable child transforms by using <see cref="MathTransform.Inverse"/>.
    /// </summary>
    [Fact]
    public void Invert_UsesInverseForImmutableChildren()
    {
        var composite = new CompositeMathTransform(
        [
            new ImmutableOffsetMathTransform(10.0),
            new ImmutableOffsetMathTransform(5.0),
        ]);

        composite.Invert();

        double x = 15.0, y = 15.0, z = 0.0;
        composite.Transform(ref x, ref y, ref z);

        Assert.Equal(0.0, x, 12);
        Assert.Equal(0.0, y, 12);
    }

    /// <summary>
    /// Verifies that WKT throws <see cref="NotSupportedException"/>.
    /// </summary>
    [Fact]
    public void WKT_ThrowsNotSupportedException()
    {
        var composite = new CompositeMathTransform([new IdentityMathTransform(2)]);

        Assert.Throws<NotSupportedException>(() => composite.WKT);
    }

    /// <summary>
    /// Verifies that XML throws <see cref="NotSupportedException"/>.
    /// </summary>
    [Fact]
    public void XML_ThrowsNotSupportedException()
    {
        var composite = new CompositeMathTransform([new IdentityMathTransform(2)]);

        Assert.Throws<NotSupportedException>(() => composite.XML);
    }

    /// <summary>
    /// A simple test double that offsets X and Y by a fixed amount.
    /// </summary>
    private sealed class OffsetMathTransform : MathTransform
    {
        private double offset;

        public OffsetMathTransform(double offset)
        {
            this.offset = offset;
        }

        /// <inheritdoc/>
        public override int DimSource => 2;

        /// <inheritdoc/>
        public override int DimTarget => 2;

        /// <inheritdoc/>
        public override string WKT => throw new NotImplementedException();

        /// <inheritdoc/>
        public override string XML => throw new NotImplementedException();

        /// <inheritdoc/>
        public override bool Identity() => this.offset == 0;

        /// <inheritdoc/>
        public override MathTransform Inverse() => new OffsetMathTransform(-this.offset);

        /// <inheritdoc/>
        public override void Invert() => this.offset = -this.offset;

        /// <inheritdoc/>
        public override void Transform(ref double x, ref double y, ref double z)
        {
            x += this.offset;
            y += this.offset;
        }
    }

    /// <summary>
    /// A simple immutable test double that offsets X and Y by a fixed amount.
    /// </summary>
    private sealed class ImmutableOffsetMathTransform : MathTransform
    {
        private readonly double offset;

        public ImmutableOffsetMathTransform(double offset)
        {
            this.offset = offset;
        }

        /// <inheritdoc/>
        public override int DimSource => 2;

        /// <inheritdoc/>
        public override int DimTarget => 2;

        /// <inheritdoc/>
        public override string WKT => throw new NotImplementedException();

        /// <inheritdoc/>
        public override string XML => throw new NotImplementedException();

        /// <inheritdoc/>
        public override bool Identity() => this.offset == 0d;

        /// <inheritdoc/>
        public override MathTransform Inverse() => new ImmutableOffsetMathTransform(-this.offset);

        /// <inheritdoc/>
        public override void Invert() => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Transform(ref double x, ref double y, ref double z)
        {
            x += this.offset;
            y += this.offset;
        }
    }
}
