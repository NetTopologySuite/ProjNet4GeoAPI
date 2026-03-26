// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class PipelineRuntimeTests
{
    private const double GeocentricLatitudeAt45OnGrs80 = 44.807576783073245d;
    private static readonly double[] GeocentRoundtripInput = { 2d, 1d, 250d };
    private static readonly double[] GeocForwardInput = { 12d, 45d };
    private static readonly double[] GeocInverseInput = { 12d, GeocentricLatitudeAt45OnGrs80 };
    private static readonly double[] CartAliasInput = { 90d, 0d, 0d };
    private static readonly double[] GeogOffset2DInput = { 10d, 20d };
    private static readonly double[] GeogOffset3DInput = { 10d, 20d, 30d };
    private static readonly double[] GeogOffsetInverseInput = { 11d, 19d, 33d };
    private static readonly double[] MolobadekasInput = { 2550408.96d, -5749912.26d, 1054891.11d };
    private static readonly double[] AffineInput4D = { 2d, 49d, 10d, 100d };
    private static readonly double[] PushPopInput4D = { 12d, 56d, 0d, 2020d };
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
    public void PipelineWith4DAxisSwapReordersAndFlipsAllOrdinates()
    {
        const string operation = "+proj=axisswap +order=4,3,-2,1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(AffineInput4D);

        Assert.Equal(100d, transformed[0], 12);
        Assert.Equal(10d, transformed[1], 12);
        Assert.Equal(-49d, transformed[2], 12);
        Assert.Equal(2d, transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithPushAndPopRestoresSavedHorizontalComponent()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_1 +step +proj=utm +zone=32 +step +proj=utm +zone=33 +inv +step +proj=pop +v_1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 9);
        Assert.Equal(PushPopInput4D[1], transformed[1], 9);
        Assert.Equal(PushPopInput4D[2], transformed[2], 9);
        Assert.Equal(PushPopInput4D[3], transformed[3], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineUsesGlobalEllipsoidParametersAcrossSteps()
    {
        const string operation = "+proj=pipeline +ellps=GRS80 +step +proj=geocent +step +proj=geocent +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeocentRoundtripInput);

        Assert.Equal(GeocentRoundtripInput[0], transformed[0], 9);
        Assert.Equal(GeocentRoundtripInput[1], transformed[1], 9);
        Assert.Equal(GeocentRoundtripInput[2], transformed[2], 6);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithUtmStepUsesGlobalEllipsoid()
    {
        const string operation = "+proj=pipeline +ellps=GRS80 +step +proj=utm +zone=32 +step +proj=utm +zone=32 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeocForwardInput);

        Assert.Equal(GeocForwardInput[0], transformed[0], 7);
        Assert.Equal(GeocForwardInput[1], transformed[1], 7);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithTmercStepRoundTripsUsingProjectionParameters()
    {
        const string operation = "+proj=pipeline +ellps=GRS80 +step +proj=tmerc +lon_0=9 +k_0=0.9996 +x_0=500000 +y_0=0 +step +proj=tmerc +lon_0=9 +k_0=0.9996 +x_0=500000 +y_0=0 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeocForwardInput);

        Assert.Equal(GeocForwardInput[0], transformed[0], 7);
        Assert.Equal(GeocForwardInput[1], transformed[1], 7);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithPushWithoutPopKeepsChangedValue()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_1 +step +proj=set +v_1=18";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(18d, transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithPopFromEmptyStackKeepsCurrentValue()
    {
        const string operation = "+proj=pipeline +step +proj=set +v_1=18 +step +proj=pop +v_1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(18d, transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithPushAndPopOnTimeComponentRestoresEpoch()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_4 +step +proj=affine +toff=4 +tscale=34 +step +proj=pop +v_4";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithMultiplePushesAndPopsUsesLifoPerComponent()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_1 +step +proj=set +v_1=20 +step +proj=push +v_1 +step +proj=set +v_1=30 +step +proj=pop +v_1 +step +proj=pop +v_1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithOmitInvSkipsStepOnlyInInverseDirection()
    {
        const string operation = "+proj=pipeline +step +proj=affine +xoff=1 +yoff=1 +omit_inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] forward = transform.Transform(AffineInput4D);
        double[] inverse = transform.Inverse().Transform(AffineInput4D);

        Assert.Equal(3d, forward[0], 12);
        Assert.Equal(50d, forward[1], 12);
        Assert.Equal(2d, inverse[0], 12);
        Assert.Equal(49d, inverse[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithOmitFwdSkipsStepOnlyInForwardDirection()
    {
        const string operation = "+proj=pipeline +step +proj=affine +xoff=1 +yoff=1 +omit_fwd";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] forward = transform.Transform(AffineInput4D);
        double[] inverse = transform.Inverse().Transform(AffineInput4D);

        Assert.Equal(2d, forward[0], 12);
        Assert.Equal(49d, forward[1], 12);
        Assert.Equal(1d, inverse[0], 12);
        Assert.Equal(48d, inverse[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelinePushWithoutOrdinateFlagReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=push +step +proj=pop +v_1";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("v_1", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithInvalidTmercParameterReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=tmerc +lon_0=abc";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("lon_0", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithUnsupportedProjectionKeepsBuiltinsWaveError()
    {
        const string operation = "+proj=pipeline +step +proj=unknown_projection";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("current builtins wave", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithInvalidUtmZoneReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=utm +zone=99";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("zone", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Theory]
    [InlineData("+proj=push +v_3")]
    [InlineData("+proj=pop +v_3")]
    public void StandalonePushOrPopBehavesAsNoop(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
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

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithGeocentAndInverseRoundTripsGeodeticCoordinates()
    {
        const string operation = "+proj=pipeline +step +proj=geocent +ellps=GRS80 +step +proj=geocent +ellps=GRS80 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeocentRoundtripInput);

        Assert.Equal(2d, transformed[0], 9);
        Assert.Equal(1d, transformed[1], 9);
        Assert.Equal(250d, transformed[2], 6);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithGeocAliasConvertsGeodeticToGeocentricLatitude()
    {
        const string operation = "+proj=geoc +ellps=GRS80";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeocForwardInput);

        Assert.Equal(12d, transformed[0], 12);
        Assert.Equal(GeocentricLatitudeAt45OnGrs80, transformed[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithGeocAliasInverseConvertsGeocentricToGeodeticLatitude()
    {
        const string operation = "+proj=geoc +ellps=GRS80 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeocInverseInput);

        Assert.Equal(12d, transformed[0], 12);
        Assert.Equal(45d, transformed[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithCartAliasAndToMeterScalesCartesianOutputUnits()
    {
        const string operation = "+proj=cart +a=1000 +b=1000 +to_meter=1000";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(CartAliasInput);

        Assert.Equal(0d, transformed[0], 12);
        Assert.Equal(1d, transformed[1], 12);
        Assert.Equal(0d, transformed[2], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithGeogOffsetAddsConfiguredArcSecondAndHeightOffsets()
    {
        const string operation = "+proj=geogoffset +dlon=3600 +dlat=-3600 +dh=3";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed2D = transform.Transform(GeogOffset2DInput);
        double[] transformed3D = transform.Transform(GeogOffset3DInput);

        Assert.Equal(11d, transformed2D[0], 12);
        Assert.Equal(19d, transformed2D[1], 12);
        Assert.Equal(11d, transformed3D[0], 12);
        Assert.Equal(19d, transformed3D[1], 12);
        Assert.Equal(33d, transformed3D[2], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithGeogOffsetInverseSubtractsConfiguredOffsets()
    {
        const string operation = "+proj=geogoffset +dlon=3600 +dlat=-3600 +dh=3 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeogOffsetInverseInput);

        Assert.Equal(10d, transformed[0], 12);
        Assert.Equal(20d, transformed[1], 12);
        Assert.Equal(30d, transformed[2], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithGeogOffsetWithoutOffsetsActsAsIdentity()
    {
        const string operation = "+proj=geogoffset";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(GeogOffset3DInput);

        Assert.Equal(GeogOffset3DInput[0], transformed[0], 12);
        Assert.Equal(GeogOffset3DInput[1], transformed[1], 12);
        Assert.Equal(GeogOffset3DInput[2], transformed[2], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithMolobadekasAppliesCoordinateFrameParameters()
    {
        const string operation = "+proj=molobadekas +convention=coordinate_frame +x=-270.933 +y=115.599 +z=-360.226 +rx=-5.266 +ry=-1.238 +rz=2.381 +s=-5.109 +px=2464351.59 +py=-5783466.61 +pz=974809.81";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(MolobadekasInput);

        const double toleranceMeters = 0.01d;
        Assert.InRange(Math.Abs(transformed[0] - 2550138.45d), 0d, toleranceMeters);
        Assert.InRange(Math.Abs(transformed[1] - -5749799.87d), 0d, toleranceMeters);
        Assert.InRange(Math.Abs(transformed[2] - 1054530.82d), 0d, toleranceMeters);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithMolobadekasMissingConventionReturnsValidationFailure()
    {
        const string operation = "+proj=molobadekas";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("convention", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithAffineIdentityLeavesCoordinatesUnchanged()
    {
        const string operation = "+proj=affine";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(AffineInput4D);

        Assert.Equal(AffineInput4D[0], transformed[0], 12);
        Assert.Equal(AffineInput4D[1], transformed[1], 12);
        Assert.Equal(AffineInput4D[2], transformed[2], 12);
        Assert.Equal(AffineInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithAffineAppliesConfiguredSpatialAndTemporalTerms()
    {
        const string operation = "+proj=affine +xoff=1 +yoff=2 +zoff=3 +toff=4 +s11=11 +s12=12 +s13=13 +s21=21 +s22=22 +s23=23 +s31=-31 +s32=32 +s33=33 +tscale=34";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);

        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(AffineInput4D);

        Assert.Equal(741d, transformed[0], 12);
        Assert.Equal(1352d, transformed[1], 12);
        Assert.Equal(1839d, transformed[2], 12);
        Assert.Equal(3404d, transformed[3], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithAffineInverseRoundTrips()
    {
        const string operation = "+proj=affine +xoff=1 +yoff=2 +zoff=3 +toff=4 +s11=11 +s12=12 +s13=13 +s21=21 +s22=22 +s23=23 +s31=-31 +s32=32 +s33=33 +tscale=34";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform transform, out string skipReason);
        Assert.True(ok, skipReason);
        double[] transformed = transform.Transform(AffineInput4D);
        double[] roundTripped = transform.Inverse().Transform(transformed);

        Assert.Equal(AffineInput4D[0], roundTripped[0], 9);
        Assert.Equal(AffineInput4D[1], roundTripped[1], 9);
        Assert.Equal(AffineInput4D[2], roundTripped[2], 9);
        Assert.Equal(AffineInput4D[3], roundTripped[3], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithAffineNonInvertibleMatrixRejectsInverse()
    {
        const string operation = "+proj=affine +s11=0 +s22=0 +s23=0 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("invertible", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void PipelineWithAffineZeroTimeScaleRejectsInverse()
    {
        const string operation = "+proj=affine +tscale=0 +inv";

        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string skipReason);

        Assert.False(ok);
        Assert.Contains("tscale", skipReason, StringComparison.OrdinalIgnoreCase);
    }
}
