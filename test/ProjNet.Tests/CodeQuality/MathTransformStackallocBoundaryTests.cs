// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies dimensional boundary behavior around stackalloc-backed transform paths.
/// </summary>
public class MathTransformStackallocBoundaryTests
{
    /// <summary>
    /// Verifies 2D input uses the 3D transform path and returns 2 ordinates.
    /// </summary>
    [Fact]
    public void TransformArray2DUsesThreeDimensionalPath()
    {
        var transform = new TrackingBoundaryMathTransform();

        double[] result = transform.Transform([1d, 2d]);

        Assert.Equal([11d, 22d], result);
        Assert.Equal(1, transform.Transform3DCalls);
        Assert.Equal(0, transform.Transform4DCalls);
    }

    /// <summary>
    /// Verifies 3D input remains on the 3D transform path for a 2D target transform.
    /// </summary>
    [Fact]
    public void TransformArray3DUsesThreeDimensionalPath()
    {
        var transform = new TrackingBoundaryMathTransform();

        double[] result = transform.Transform([1d, 2d, 3d]);

        Assert.Equal(2, result.Length);
        Assert.Equal(11d, result[0], 12);
        Assert.Equal(22d, result[1], 12);
        Assert.Equal(1, transform.Transform3DCalls);
        Assert.Equal(0, transform.Transform4DCalls);
    }

    /// <summary>
    /// Verifies 4D input uses the 4D transform path at the stackalloc boundary.
    /// </summary>
    [Fact]
    public void TransformArray4DUsesFourDimensionalPathAtBoundary()
    {
        var transform = new TrackingBoundaryMathTransform();

        double[] result = transform.Transform([1d, 2d, 3d, 4d]);

        Assert.Equal([2d, 4d, 6d, 8d], result);
        Assert.Equal(0, transform.Transform3DCalls);
        Assert.Equal(1, transform.Transform4DCalls);
    }

    /// <summary>
    /// Verifies input above 4D uses the 4D transform path and preserves trailing ordinates.
    /// </summary>
    [Fact]
    public void TransformArrayAbove4DUsesFourDimensionalPathAndPreservesTail()
    {
        var transform = new TrackingBoundaryMathTransform();

        double[] result = transform.Transform([1d, 2d, 3d, 4d, 99d]);

        Assert.Equal(5, result.Length);
        Assert.Equal(2d, result[0], 12);
        Assert.Equal(4d, result[1], 12);
        Assert.Equal(6d, result[2], 12);
        Assert.Equal(8d, result[3], 12);
        Assert.Equal(99d, result[4], 12);
        Assert.Equal(0, transform.Transform3DCalls);
        Assert.Equal(1, transform.Transform4DCalls);
    }

    private sealed class TrackingBoundaryMathTransform : MathTransform
    {
        public int Transform3DCalls { get; private set; }

        public int Transform4DCalls { get; private set; }

        public override int DimSource => 2;

        public override int DimTarget => 2;

        public override string WKT => "PARAM_MT[\"TrackingBoundary\"]";

        public override string XML => "<TrackingBoundary />";

        public override MathTransform Inverse() => this;

        public override void Invert()
        {
        }

        public override bool Identity() => false;

        public override void Transform(ref double x, ref double y, ref double z)
        {
            this.Transform3DCalls++;
            x += 10d;
            y += 20d;
            z += 30d;
        }

        internal override void Transform(ref double x, ref double y, ref double z, ref double t)
        {
            this.Transform4DCalls++;
            x += 1d;
            y += 2d;
            z += 3d;
            t += 4d;
        }
    }
}
