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
}
