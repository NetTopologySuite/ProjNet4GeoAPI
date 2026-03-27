// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>helmert</c>.
/// </summary>
public class HelmertRuntimeTests
{
    /// <summary>
    /// Verifies coordinate-frame Helmert vector from PROJ <c>more_builtins.gie</c>.
    /// </summary>
    [Fact]
    public void HelmertCoordinateFrameMatchesMoreBuiltinsVector()
    {
        const string operation = "+proj=helmert +convention=coordinate_frame +x=0.67678 +y=0.65495 +z=-0.52827 +rx=-0.022742 +ry=0.012667 +rz=0.022704 +s=-0.01070";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(3565285.0d, 855949.0d, 5201383.0d));

        Assert.InRange(Math.Abs(output[0] - 3565285.41342351d), 0d, 1e-6);
        Assert.InRange(Math.Abs(output[1] - 855948.67986759d), 0d, 1e-6);
        Assert.InRange(Math.Abs(output[2] - 5201382.72939791d), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies exact-mode Helmert vector from PROJ <c>more_builtins.gie</c>.
    /// </summary>
    [Fact]
    public void HelmertExactCoordinateFrameMatchesMoreBuiltinsVector()
    {
        const string operation = "+proj=helmert +exact +convention=coordinate_frame +x=-81.0703 +y=-89.3603 +z=-115.7526 +rx=-0.48488 +ry=-0.02436 +rz=-0.41321 +s=-0.540645";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(3494994.3012d, 1056601.9725d, 5212382.1666d));

        Assert.InRange(Math.Abs(output[0] - 3494909.84026368d), 0d, 1e-6);
        Assert.InRange(Math.Abs(output[1] - 1056506.78938633d), 0d, 1e-6);
        Assert.InRange(Math.Abs(output[2] - 5212265.66699761d), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies 4-parameter Helmert vector from PROJ <c>more_builtins.gie</c>.
    /// </summary>
    [Fact]
    public void HelmertFourParameterMatchesMoreBuiltinsVector()
    {
        const string operation = "+proj=helmert +x=-9597.3572 +y=0.6112 +s=0.304794780637 +theta=-1.244048";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(2546506.957d, 542256.609d, 0d));

        Assert.InRange(Math.Abs(output[0] - 766563.675d), 0d, 1e-3);
        Assert.InRange(Math.Abs(output[1] - 165282.277d), 0d, 1e-3);
        Assert.InRange(Math.Abs(output[2] - 0d), 0d, 1e-9);
    }

    /// <summary>
    /// Verifies inverse flag for static 7-parameter Helmert operations.
    /// </summary>
    [Fact]
    public void HelmertInverseRecoversInput()
    {
        const string forwardOperation = "+proj=helmert +convention=coordinate_frame +x=0.67678 +y=0.65495 +z=-0.52827 +rx=-0.022742 +ry=0.012667 +rz=0.022704 +s=-0.01070";
        const string inverseOperation = "+proj=helmert +convention=coordinate_frame +x=0.67678 +y=0.65495 +z=-0.52827 +rx=-0.022742 +ry=0.012667 +rz=0.022704 +s=-0.01070 +inv";
        MathTransform forward = CreateTransform(forwardOperation);
        MathTransform inverse = CreateTransform(inverseOperation);

        double[] source = CreatePoint(3565285.0d, 855949.0d, 5201383.0d);
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.InRange(Math.Abs(recovered[0] - source[0]), 0d, 1e-6);
        Assert.InRange(Math.Abs(recovered[1] - source[1]), 0d, 1e-6);
        Assert.InRange(Math.Abs(recovered[2] - source[2]), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies error-path behavior for convention and obsolete transpose handling.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=helmert +rx=1", "missing 'convention'")]
    [InlineData("+proj=helmert +rx=1 +convention=foo", "invalid value for 'convention'")]
    [InlineData("+proj=helmert +rx=1 +convention=1", "invalid value for 'convention'")]
    [InlineData("+proj=helmert +towgs84=1,2,3,4,5,6,7 +convention=coordinate_frame", "towgs84 should only be used")]
    [InlineData("+proj=helmert +transpose", "'transpose' argument is no longer valid")]
    public void HelmertCreationFailsForInvalidConventionOrLegacyTranspose(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string? skipReason);

        Assert.False(ok);
        Assert.Contains(expectedToken, Assert.IsAssignableFrom<string>(skipReason), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies kinematic position-vector Helmert vectors from PROJ <c>more_builtins.gie</c>.
    /// </summary>
    /// <param name="observationEpoch">Observation epoch carried in the 4th ordinate.</param>
    /// <param name="expectedX">Expected X output.</param>
    /// <param name="expectedY">Expected Y output.</param>
    /// <param name="expectedZ">Expected Z output.</param>
    [Theory]
    [InlineData(2017.0d, 3370658.18890d, 711877.42370d, 5349787.12430d)]
    [InlineData(2018.0d, 3370658.18087d, 711877.42750d, 5349787.12648d)]
    public void HelmertKinematicPositionVectorMatchesMoreBuiltinsVectors(
        double observationEpoch,
        double expectedX,
        double expectedY,
        double expectedZ)
    {
        const string operation = "+proj=helmert +convention=position_vector +x=0.0127 +dx=-0.0029 +rx=-0.00039 +drx=-0.00011 +y=0.0065 +dy=-0.0002 +ry=0.00080 +dry=-0.00019 +z=-0.0209 +dz=-0.0006 +rz=-0.00114 +drz=0.00007 +s=0.00195 +ds=0.00001 +t_epoch=1988.0";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(3370658.37800d, 711877.31400d, 5349787.08600d, observationEpoch));

        Assert.Equal(4, output.Length);
        Assert.InRange(Math.Abs(output[0] - expectedX), 0d, 1e-4d);
        Assert.InRange(Math.Abs(output[1] - expectedY), 0d, 1e-4d);
        Assert.InRange(Math.Abs(output[2] - expectedZ), 0d, 1e-4d);
        Assert.InRange(Math.Abs(output[3] - observationEpoch), 0d, 1e-12d);
    }

    /// <summary>
    /// Verifies kinematic coordinate-frame Helmert vector from PROJ <c>GDA.gie</c>.
    /// </summary>
    [Fact]
    public void HelmertKinematicCoordinateFrameMatchesGdaVector()
    {
        const string operation = "+proj=helmert +exact +convention=coordinate_frame +x=0 +rx=0 +dx=0 +drx=0.00150379 +y=0 +ry=0 +dy=0 +dry=0.00118346 +z=0 +rz=0 +dz=0 +drz=0.00120716 +s=0 +ds=0 +t_epoch=2020.0";
        MathTransform transform = CreateTransform(operation);
        double[] output = transform.Transform(CreatePoint(-4052052.6588d, 4212835.9938d, -2545104.6946d, 2018.0d));

        Assert.Equal(4, output.Length);
        Assert.InRange(Math.Abs(output[0] - (-4052052.7373d)), 0d, 4e-5d);
        Assert.InRange(Math.Abs(output[1] - 4212835.9835d), 0d, 4e-5d);
        Assert.InRange(Math.Abs(output[2] - (-2545104.5867d)), 0d, 4e-5d);
        Assert.InRange(Math.Abs(output[3] - 2018.0d), 0d, 1e-12d);
    }

    /// <summary>
    /// Verifies kinematic inverse with an explicit observation epoch.
    /// </summary>
    [Fact]
    public void HelmertKinematicInverseRecoversInput()
    {
        const string forwardOperation = "+proj=helmert +convention=position_vector +x=0.0127 +dx=-0.0029 +rx=-0.00039 +drx=-0.00011 +y=0.0065 +dy=-0.0002 +ry=0.00080 +dry=-0.00019 +z=-0.0209 +dz=-0.0006 +rz=-0.00114 +drz=0.00007 +s=0.00195 +ds=0.00001 +t_epoch=1988.0";
        const string inverseOperation = "+proj=helmert +convention=position_vector +x=0.0127 +dx=-0.0029 +rx=-0.00039 +drx=-0.00011 +y=0.0065 +dy=-0.0002 +ry=0.00080 +dry=-0.00019 +z=-0.0209 +dz=-0.0006 +rz=-0.00114 +drz=0.00007 +s=0.00195 +ds=0.00001 +t_epoch=1988.0 +inv";
        MathTransform forward = CreateTransform(forwardOperation);
        MathTransform inverse = CreateTransform(inverseOperation);

        double[] source = CreatePoint(3370658.37800d, 711877.31400d, 5349787.08600d, 2018.0d);
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(4, recovered.Length);
        Assert.InRange(Math.Abs(recovered[0] - source[0]), 0d, 1e-6d);
        Assert.InRange(Math.Abs(recovered[1] - source[1]), 0d, 1e-6d);
        Assert.InRange(Math.Abs(recovered[2] - source[2]), 0d, 1e-6d);
        Assert.InRange(Math.Abs(recovered[3] - source[3]), 0d, 1e-12d);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsAssignableFrom<MathTransform>(transform);
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];

    private static double[] CreatePoint(double x, double y, double z, double t) => [x, y, z, t];
}
