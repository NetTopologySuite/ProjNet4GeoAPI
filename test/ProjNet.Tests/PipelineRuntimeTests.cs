// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for the PROJ pipeline math transform runtime, covering step execution, parameter parsing, and validation.
/// </summary>
public class PipelineRuntimeTests
{
    private const double GeocentricLatitudeAt45OnGrs80 = 44.807576783073245d;
    private static readonly double[] GeocentRoundtripInput = [2d, 1d, 250d];
    private static readonly double[] GeocForwardInput = [12d, 45d];
    private static readonly double[] GeocInverseInput = [12d, GeocentricLatitudeAt45OnGrs80];
    private static readonly double[] CartAliasInput = [90d, 0d, 0d];
    private static readonly double[] GeogOffset2DInput = [10d, 20d];
    private static readonly double[] GeogOffset3DInput = [10d, 20d, 30d];
    private static readonly double[] GeogOffsetInverseInput = [11d, 19d, 33d];
    private static readonly double[] MolobadekasInput = [2550408.96d, -5749912.26d, 1054891.11d];
    private static readonly double[] AffineInput4D = [2d, 49d, 10d, 100d];
    private static readonly double[] PushPopInput4D = [12d, 56d, 0d, 2020d];
    private static readonly double[] PipelineNoopInput = [1.5d, 2.25d, 9d];
    private static readonly double[] PipelineSwapInput = [100d, 200d];

    /// <summary>
    /// Verifies that a pipeline combining unit conversion and axis swap correctly converts and reorders the output ordinates.
    /// </summary>
    [Fact]
    public void PipelineWithUnitConvertAndAxisSwapConvertsAndSwaps()
    {
        const string operation = "+proj=pipeline +step +proj=unitconvert +xy_in=m +xy_out=ft +step +proj=axisswap +order=2,1";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PipelineSwapInput);

        Assert.Equal(656.1679790026246d, transformed[0], 9);
        Assert.Equal(328.0839895013123d, transformed[1], 9);
    }

    /// <summary>
    /// Verifies that a pipeline with a noop, a set step, and unit conversion applies the set value override to the third ordinate.
    /// </summary>
    [Fact]
    public void PipelineWithNoopSetAndUnitConvertAppliesSetOverride()
    {
        const string operation = "+proj=pipeline +step +proj=noop +step +proj=set +v_3=17 +step +proj=unitconvert +xy_in=km +xy_out=m";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PipelineNoopInput);

        Assert.Equal(1500d, transformed[0], 10);
        Assert.Equal(2250d, transformed[1], 10);
        Assert.Equal(17d, transformed[2], 10);
    }

    /// <summary>
    /// Verifies that a 4D axis swap step with negated indices correctly reorders and flips all four ordinates.
    /// </summary>
    [Fact]
    public void PipelineWith4DAxisSwapReordersAndFlipsAllOrdinates()
    {
        const string operation = "+proj=axisswap +order=4,3,-2,1";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(AffineInput4D);

        Assert.Equal(100d, transformed[0], 12);
        Assert.Equal(10d, transformed[1], 12);
        Assert.Equal(-49d, transformed[2], 12);
        Assert.Equal(2d, transformed[3], 12);
    }

    /// <summary>
    /// Verifies that push and pop steps correctly save and restore the first ordinate across intermediate transform steps.
    /// </summary>
    [Fact]
    public void PipelineWithPushAndPopRestoresSavedHorizontalComponent()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_1 +step +proj=utm +zone=32 +step +proj=utm +zone=33 +inv +step +proj=pop +v_1";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 9);
        Assert.Equal(PushPopInput4D[1], transformed[1], 9);
        Assert.Equal(PushPopInput4D[2], transformed[2], 9);
        Assert.Equal(PushPopInput4D[3], transformed[3], 9);
    }

    /// <summary>
    /// Verifies that global ellipsoid parameters are shared across all pipeline steps.
    /// </summary>
    [Fact]
    public void PipelineUsesGlobalEllipsoidParametersAcrossSteps()
    {
        const string operation = "+proj=pipeline +ellps=GRS80 +step +proj=geocent +step +proj=geocent +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeocentRoundtripInput);

        Assert.Equal(GeocentRoundtripInput[0], transformed[0], 9);
        Assert.Equal(GeocentRoundtripInput[1], transformed[1], 9);
        Assert.Equal(GeocentRoundtripInput[2], transformed[2], 6);
    }

    /// <summary>
    /// Verifies that a UTM step in a pipeline inherits the global ellipsoid parameter.
    /// </summary>
    [Fact]
    public void PipelineWithUtmStepUsesGlobalEllipsoid()
    {
        const string operation = "+proj=pipeline +ellps=GRS80 +step +proj=utm +zone=32 +step +proj=utm +zone=32 +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeocForwardInput);

        Assert.Equal(GeocForwardInput[0], transformed[0], 7);
        Assert.Equal(GeocForwardInput[1], transformed[1], 7);
    }

    /// <summary>
    /// Verifies that a global pipeline <c>+inv</c> flag reverses the step order and toggles each step inversion.
    /// </summary>
    [Fact]
    public void PipelineWithGlobalInvInvertsWholePipeline()
    {
        const string operation = "+proj=pipeline +inv +step +proj=affine +xoff=1 +step +proj=affine +xoff=2";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform([10d, 20d]);

        Assert.Equal(7d, transformed[0], 12);
        Assert.Equal(20d, transformed[1], 12);
    }

    /// <summary>
    /// Verifies that <c>urm5</c> pipeline steps accept the required <c>n</c> parameter and optional shape parameters.
    /// </summary>
    [Fact]
    public void PipelineWithUrm5StepAcceptsProjectionSpecificParameters()
    {
        const string operation = "+proj=urm5 +ellps=WGS84 +n=0.5 +q=0.2 +alpha=10";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform([12d, 56d]);

        Assert.False(double.IsNaN(transformed[0]) || double.IsInfinity(transformed[0]));
        Assert.False(double.IsNaN(transformed[1]) || double.IsInfinity(transformed[1]));
    }

    /// <summary>
    /// Verifies that <c>+pm</c> shifts the longitude reference before the projection step is applied.
    /// </summary>
    [Fact]
    public void PipelineProjectionStepWithPrimeMeridianUsesLocalLongitudeReference()
    {
        const string operation = "+proj=latlong +pm=paris";
        const double projParisLongitude = 2d + (20d / 60d) + (14.025d / 3600d);

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] input = [projParisLongitude, 48d];
        double[] forward = transform.Transform(input);
        double[] inverse = transform.Inverse().Transform(forward);

        Assert.Equal(0d, forward[0], 12);
        Assert.Equal(48d, forward[1], 12);
        Assert.Equal(projParisLongitude, inverse[0], 12);
        Assert.Equal(48d, inverse[1], 10);
    }

    /// <summary>
    /// Verifies that a transverse Mercator step round-trips correctly using the specified projection parameters.
    /// </summary>
    [Fact]
    public void PipelineWithTmercStepRoundTripsUsingProjectionParameters()
    {
        const string operation = "+proj=pipeline +ellps=GRS80 +step +proj=tmerc +lon_0=9 +k_0=0.9996 +x_0=500000 +y_0=0 +step +proj=tmerc +lon_0=9 +k_0=0.9996 +x_0=500000 +y_0=0 +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeocForwardInput);

        Assert.Equal(GeocForwardInput[0], transformed[0], 7);
        Assert.Equal(GeocForwardInput[1], transformed[1], 7);
    }

    /// <summary>
    /// Verifies that an LCC step respects the configured scale factor in both forward and inverse directions.
    /// </summary>
    [Fact]
    public void PipelineWithLccStepRoundTripsUsingScaleFactor()
    {
        const string operation = "+proj=pipeline +step +proj=lcc +lon_0=0 +lat_0=46.8 +lat_1=46.8 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +ellps=clrk80ign +pm=paris +step +proj=lcc +lon_0=0 +lat_0=46.8 +lat_1=46.8 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +ellps=clrk80ign +pm=paris +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform([2.5969213d, 48d]);

        Assert.Equal(2.5969213d, transformed[0], 7);
        Assert.Equal(48d, transformed[1], 7);
    }

    /// <summary>
    /// Verifies that a pushed value is not restored when no corresponding pop step is present.
    /// </summary>
    [Fact]
    public void PipelineWithPushWithoutPopKeepsChangedValue()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_1 +step +proj=set +v_1=18";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(18d, transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Verifies that a pop step with no prior push for the same component leaves the current ordinate value unchanged.
    /// </summary>
    [Fact]
    public void PipelineWithPopFromEmptyStackKeepsCurrentValue()
    {
        const string operation = "+proj=pipeline +step +proj=set +v_1=18 +step +proj=pop +v_1";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(18d, transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Verifies that push and pop correctly save and restore the time component across an affine transform step.
    /// </summary>
    [Fact]
    public void PipelineWithPushAndPopOnTimeComponentRestoresEpoch()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_4 +step +proj=affine +toff=4 +tscale=34 +step +proj=pop +v_4";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Verifies that multiple push and pop steps for the same component follow last-in, first-out order.
    /// </summary>
    [Fact]
    public void PipelineWithMultiplePushesAndPopsUsesLifoPerComponent()
    {
        const string operation = "+proj=pipeline +step +proj=push +v_1 +step +proj=set +v_1=20 +step +proj=push +v_1 +step +proj=set +v_1=30 +step +proj=pop +v_1 +step +proj=pop +v_1";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Verifies that the omit_inv flag causes a pipeline step to be applied in the forward direction but skipped in the inverse direction.
    /// </summary>
    [Fact]
    public void PipelineWithOmitInvSkipsStepOnlyInInverseDirection()
    {
        const string operation = "+proj=pipeline +step +proj=affine +xoff=1 +yoff=1 +omit_inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] forward = transform.Transform(AffineInput4D);
        double[] inverse = transform.Inverse().Transform(AffineInput4D);

        Assert.Equal(3d, forward[0], 12);
        Assert.Equal(50d, forward[1], 12);
        Assert.Equal(2d, inverse[0], 12);
        Assert.Equal(49d, inverse[1], 12);
    }

    /// <summary>
    /// Verifies that the omit_fwd flag causes a pipeline step to be skipped in the forward direction but applied in the inverse direction.
    /// </summary>
    [Fact]
    public void PipelineWithOmitFwdSkipsStepOnlyInForwardDirection()
    {
        const string operation = "+proj=pipeline +step +proj=affine +xoff=1 +yoff=1 +omit_fwd";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] forward = transform.Transform(AffineInput4D);
        double[] inverse = transform.Inverse().Transform(AffineInput4D);

        Assert.Equal(2d, forward[0], 12);
        Assert.Equal(49d, forward[1], 12);
        Assert.Equal(1d, inverse[0], 12);
        Assert.Equal(48d, inverse[1], 12);
    }

    /// <summary>
    /// Verifies that a push step without an ordinate flag produces a validation failure.
    /// </summary>
    [Fact]
    public void PipelinePushWithoutOrdinateFlagReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=push +step +proj=pop +v_1";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("v_1", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that a transverse Mercator step with a non-numeric parameter value produces a validation failure.
    /// </summary>
    [Fact]
    public void PipelineWithInvalidTmercParameterReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=tmerc +lon_0=abc";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("lon_0", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that an unsupported projection name produces a validation failure referencing the current builtins wave.
    /// </summary>
    [Fact]
    public void PipelineWithUnsupportedProjectionKeepsBuiltinsWaveError()
    {
        const string operation = "+proj=pipeline +step +proj=unknown_projection";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("current builtins wave", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that an invalid UTM zone number produces a validation failure.
    /// </summary>
    [Fact]
    public void PipelineWithInvalidUtmZoneReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=utm +zone=99";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("zone", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that +lat_ts is mapped to a single projection parameter entry.
    /// </summary>
    [Fact]
    public void PipelineProjectionStepWithLatTsDoesNotCreateDuplicateAliases()
    {
        const string operation = "+proj=wink1 +lat_ts=30";

        MathTransform transform = RequirePipelineMathTransform(operation);
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(transform);
        int latTsParameterCount = 0;
        for (int i = 0; i < projection.NumParameters; i++)
        {
            string name = projection.GetParameter(i).Name;
            if (name.Equals("lat_ts", StringComparison.OrdinalIgnoreCase)
                || name.Equals("latitude_true_scale", StringComparison.OrdinalIgnoreCase))
            {
                latTsParameterCount++;
            }
        }

        Assert.Equal(1, latTsParameterCount);
    }

    /// <summary>
    /// Verifies that a standalone push or pop step without a matching counterpart leaves all ordinates unchanged.
    /// </summary>
    [Theory]
    [InlineData("+proj=push +v_3")]
    [InlineData("+proj=pop +v_3")]
    public void StandalonePushOrPopBehavesAsNoop(string operation)
    {
        MathTransform transform = RequirePipelineMathTransform($"+proj=pipeline +step {operation}");
        double[] transformed = transform.Transform(PushPopInput4D);

        Assert.Equal(PushPopInput4D[0], transformed[0], 12);
        Assert.Equal(PushPopInput4D[1], transformed[1], 12);
        Assert.Equal(PushPopInput4D[2], transformed[2], 12);
        Assert.Equal(PushPopInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Verifies that an axis swap step with duplicate axis indices in the order parameter produces a validation failure.
    /// </summary>
    [Fact]
    public void PipelineInvalidAxisSwapOrderReturnsValidationFailure()
    {
        const string operation = "+proj=pipeline +step +proj=axisswap +order=1,1";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("+order", skipReason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that an axis swap using the +axis parameter correctly interprets compass and vertical orientation codes.
    /// </summary>
    [Fact]
    public void AxisSwapWithAxisParameterParsesOrientationCodes()
    {
        const string operation = "+proj=axisswap +axis=wsu";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform([2d, 3d, 4d]);

        Assert.Equal(-2d, transformed[0], 12);
        Assert.Equal(-3d, transformed[1], 12);
        Assert.Equal(4d, transformed[2], 12);
    }

    /// <summary>
    /// Verifies that an axis swap step without either +order or +axis produces a validation failure.
    /// </summary>
    [Fact]
    public void AxisSwapWithoutOrderAndAxisReturnsValidationFailure()
    {
        const string operation = "+proj=axisswap";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("exactly one of +order or +axis", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that specifying both +order and +axis on an axis swap step produces a validation failure.
    /// </summary>
    [Fact]
    public void AxisSwapWithOrderAndAxisReturnsValidationFailure()
    {
        const string operation = "+proj=axisswap +order=1,2 +axis=en";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("exactly one of +order or +axis", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that an axis swap step with an invalid character in the +axis value produces a validation failure.
    /// </summary>
    [Fact]
    public void AxisSwapWithInvalidAxisTokenReturnsValidationFailure()
    {
        const string operation = "+proj=axisswap +axis=ee";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("+axis", skipReason, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that an axis swap step referencing an ordinate index beyond the input dimension produces a validation failure.
    /// </summary>
    [Fact]
    public void AxisSwapWithOutOfRangeOrderReferenceReturnsValidationFailure()
    {
        const string operation = "+proj=axisswap +order=3,1";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("out-of-range axis", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies 4D epoch propagation through composite pipelines with kinematic Helmert.
    /// </summary>
    [Fact]
    public void PipelineWithKinematicHelmertPreservesEpochAndAppliesDynamicParameters()
    {
        const string operation = "+proj=pipeline +step +proj=noop +step +proj=helmert +convention=position_vector +x=0.0127 +dx=-0.0029 +rx=-0.00039 +drx=-0.00011 +y=0.0065 +dy=-0.0002 +ry=0.00080 +dry=-0.00019 +z=-0.0209 +dz=-0.0006 +rz=-0.00114 +drz=0.00007 +s=0.00195 +ds=0.00001 +t_epoch=1988.0";
        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform([3370658.37800d, 711877.31400d, 5349787.08600d, 2018.0d]);

        Assert.Equal(4, transformed.Length);
        Assert.InRange(Math.Abs(transformed[0] - 3370658.18087d), 0d, 1e-4d);
        Assert.InRange(Math.Abs(transformed[1] - 711877.42750d), 0d, 1e-4d);
        Assert.InRange(Math.Abs(transformed[2] - 5349787.12648d), 0d, 1e-4d);
        Assert.InRange(Math.Abs(transformed[3] - 2018.0d), 0d, 1e-12d);
    }

    /// <summary>
    /// Verifies that a geocentric conversion followed by its inverse round-trips geodetic coordinates to the expected precision.
    /// </summary>
    [Fact]
    public void PipelineWithGeocentAndInverseRoundTripsGeodeticCoordinates()
    {
        const string operation = "+proj=pipeline +step +proj=geocent +ellps=GRS80 +step +proj=geocent +ellps=GRS80 +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeocentRoundtripInput);

        Assert.Equal(2d, transformed[0], 9);
        Assert.Equal(1d, transformed[1], 9);
        Assert.Equal(250d, transformed[2], 6);
    }

    /// <summary>
    /// Verifies that the geoc alias step converts geodetic latitude to geocentric latitude.
    /// </summary>
    [Fact]
    public void PipelineWithGeocAliasConvertsGeodeticToGeocentricLatitude()
    {
        const string operation = "+proj=geoc +ellps=GRS80";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeocForwardInput);

        Assert.Equal(12d, transformed[0], 12);
        Assert.Equal(GeocentricLatitudeAt45OnGrs80, transformed[1], 12);
    }

    /// <summary>
    /// Verifies that the inverse geoc alias step converts geocentric latitude back to geodetic latitude.
    /// </summary>
    [Fact]
    public void PipelineWithGeocAliasInverseConvertsGeocentricToGeodeticLatitude()
    {
        const string operation = "+proj=geoc +ellps=GRS80 +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeocInverseInput);

        Assert.Equal(12d, transformed[0], 12);
        Assert.Equal(45d, transformed[1], 12);
    }

    /// <summary>
    /// Verifies that the cart alias step respects the +to_meter scaling parameter when converting to Cartesian coordinates.
    /// </summary>
    [Fact]
    public void PipelineWithCartAliasAndToMeterScalesCartesianOutputUnits()
    {
        const string operation = "+proj=cart +a=1000 +b=1000 +to_meter=1000";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(CartAliasInput);

        Assert.Equal(0d, transformed[0], 12);
        Assert.Equal(1d, transformed[1], 12);
        Assert.Equal(0d, transformed[2], 12);
    }

    /// <summary>
    /// Verifies that Clarke 1880 PROJ ellipsoid tokens resolve to metric axes in cartesian runtime steps.
    /// </summary>
    /// <param name="ellipsoidToken">The PROJ ellipsoid token.</param>
    /// <param name="expectedSemiMajorAxis">The expected semi-major axis in metres.</param>
    [Theory]
    [InlineData("clrk80", 6378249.145d)]
    [InlineData("clrk80ign", 6378249.2d)]
    public void PipelineWithCartStepUsesMetricClarke1880TokenAxes(string ellipsoidToken, double expectedSemiMajorAxis)
    {
        MathTransform transform = RequirePipelineMathTransform($"+proj=cart +ellps={ellipsoidToken}");
        double[] transformed = transform.Transform([0d, 0d, 0d]);

        Assert.Equal(expectedSemiMajorAxis, transformed[0], 9);
        Assert.Equal(0d, transformed[1], 12);
        Assert.Equal(0d, transformed[2], 12);
    }

    /// <summary>
    /// Verifies that the geogoffset step adds the configured arc-second longitude, latitude, and height offsets to both 2D and 3D input.
    /// </summary>
    [Fact]
    public void PipelineWithGeogOffsetAddsConfiguredArcSecondAndHeightOffsets()
    {
        const string operation = "+proj=geogoffset +dlon=3600 +dlat=-3600 +dh=3";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed2D = transform.Transform(GeogOffset2DInput);
        double[] transformed3D = transform.Transform(GeogOffset3DInput);

        Assert.Equal(11d, transformed2D[0], 12);
        Assert.Equal(19d, transformed2D[1], 12);
        Assert.Equal(11d, transformed3D[0], 12);
        Assert.Equal(19d, transformed3D[1], 12);
        Assert.Equal(33d, transformed3D[2], 12);
    }

    /// <summary>
    /// Verifies that the inverse geogoffset step subtracts the configured offsets.
    /// </summary>
    [Fact]
    public void PipelineWithGeogOffsetInverseSubtractsConfiguredOffsets()
    {
        const string operation = "+proj=geogoffset +dlon=3600 +dlat=-3600 +dh=3 +inv";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeogOffsetInverseInput);

        Assert.Equal(10d, transformed[0], 12);
        Assert.Equal(20d, transformed[1], 12);
        Assert.Equal(30d, transformed[2], 12);
    }

    /// <summary>
    /// Verifies that a geogoffset step without any configured offsets acts as an identity transform.
    /// </summary>
    [Fact]
    public void PipelineWithGeogOffsetWithoutOffsetsActsAsIdentity()
    {
        const string operation = "+proj=geogoffset";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(GeogOffset3DInput);

        Assert.Equal(GeogOffset3DInput[0], transformed[0], 12);
        Assert.Equal(GeogOffset3DInput[1], transformed[1], 12);
        Assert.Equal(GeogOffset3DInput[2], transformed[2], 12);
    }

    /// <summary>
    /// Verifies that the molobadekas step correctly applies coordinate frame rotation parameters to the input geocentric coordinates.
    /// </summary>
    [Fact]
    public void PipelineWithMolobadekasAppliesCoordinateFrameParameters()
    {
        const string operation = "+proj=molobadekas +convention=coordinate_frame +x=-270.933 +y=115.599 +z=-360.226 +rx=-5.266 +ry=-1.238 +rz=2.381 +s=-5.109 +px=2464351.59 +py=-5783466.61 +pz=974809.81";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(MolobadekasInput);

        const double toleranceMeters = 0.01d;
        Assert.InRange(Math.Abs(transformed[0] - 2550138.45d), 0d, toleranceMeters);
        Assert.InRange(Math.Abs(transformed[1] - -5749799.87d), 0d, toleranceMeters);
        Assert.InRange(Math.Abs(transformed[2] - 1054530.82d), 0d, toleranceMeters);
    }

    /// <summary>
    /// Verifies that a molobadekas step without a +convention parameter produces a validation failure.
    /// </summary>
    [Fact]
    public void PipelineWithMolobadekasMissingConventionReturnsValidationFailure()
    {
        const string operation = "+proj=molobadekas";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("convention", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that an affine step with default parameters leaves all ordinates unchanged.
    /// </summary>
    [Fact]
    public void PipelineWithAffineIdentityLeavesCoordinatesUnchanged()
    {
        const string operation = "+proj=affine";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(AffineInput4D);

        Assert.Equal(AffineInput4D[0], transformed[0], 12);
        Assert.Equal(AffineInput4D[1], transformed[1], 12);
        Assert.Equal(AffineInput4D[2], transformed[2], 12);
        Assert.Equal(AffineInput4D[3], transformed[3], 12);
    }

    /// <summary>
    /// Verifies that an affine step applies the configured offset and matrix coefficients to all four input ordinates.
    /// </summary>
    [Fact]
    public void PipelineWithAffineAppliesConfiguredSpatialAndTemporalTerms()
    {
        const string operation = "+proj=affine +xoff=1 +yoff=2 +zoff=3 +toff=4 +s11=11 +s12=12 +s13=13 +s21=21 +s22=22 +s23=23 +s31=-31 +s32=32 +s33=33 +tscale=34";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(AffineInput4D);

        Assert.Equal(741d, transformed[0], 12);
        Assert.Equal(1352d, transformed[1], 12);
        Assert.Equal(1839d, transformed[2], 12);
        Assert.Equal(3404d, transformed[3], 12);
    }

    /// <summary>
    /// Verifies that an affine transform and its inverse correctly round-trip all four input ordinates.
    /// </summary>
    [Fact]
    public void PipelineWithAffineInverseRoundTrips()
    {
        const string operation = "+proj=affine +xoff=1 +yoff=2 +zoff=3 +toff=4 +s11=11 +s12=12 +s13=13 +s21=21 +s22=22 +s23=23 +s31=-31 +s32=32 +s33=33 +tscale=34";

        MathTransform transform = RequirePipelineMathTransform(operation);
        double[] transformed = transform.Transform(AffineInput4D);
        double[] roundTripped = transform.Inverse().Transform(transformed);

        Assert.Equal(AffineInput4D[0], roundTripped[0], 9);
        Assert.Equal(AffineInput4D[1], roundTripped[1], 9);
        Assert.Equal(AffineInput4D[2], roundTripped[2], 9);
        Assert.Equal(AffineInput4D[3], roundTripped[3], 9);
    }

    /// <summary>
    /// Verifies that an affine step with a non-invertible matrix produces a validation failure when inverted.
    /// </summary>
    [Fact]
    public void PipelineWithAffineNonInvertibleMatrixRejectsInverse()
    {
        const string operation = "+proj=affine +s11=0 +s22=0 +s23=0 +inv";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("invertible", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that an affine step with a zero time scale produces a validation failure when inverted.
    /// </summary>
    [Fact]
    public void PipelineWithAffineZeroTimeScaleRejectsInverse()
    {
        const string operation = "+proj=affine +tscale=0 +inv";

        string skipReason = RequirePipelineValidationFailure(operation);
        Assert.Contains("tscale", skipReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that geographic identity steps honor vertical unit scaling.
    /// </summary>
    [Fact]
    public void PipelineWithLonglatVerticalUnitsScalesZ()
    {
        MathTransform transform = RequirePipelineMathTransform("+proj=longlat +a=1 +b=1 +vto_meter=1000");
        double[] output = transform.Transform([0d, 0d, 1000d]);

        Assert.Equal(0d, output[0], 12);
        Assert.Equal(0d, output[1], 12);
        Assert.Equal(1d, output[2], 12);
    }

    /// <summary>
    /// Verifies that projected steps honor vertical unit scaling independently from XY projection units.
    /// </summary>
    [Fact]
    public void PipelineWithMercatorVerticalUnitsScalesZ()
    {
        MathTransform transform = RequirePipelineMathTransform("+proj=merc +a=1 +b=1 +vunits=km");
        double[] output = transform.Transform([0d, 0d, 1000d]);

        Assert.Equal(0d, output[0], 12);
        Assert.Equal(0d, output[1], 12);
        Assert.Equal(1d, output[2], 12);
    }

    /// <summary>
    /// Verifies that geographic identity steps honor longitude wrapping.
    /// </summary>
    [Fact]
    public void PipelineWithLonglatLongitudeWrapNormalizesLongitude()
    {
        MathTransform transform = RequirePipelineMathTransform("+proj=longlat +ellps=WGS84 +lon_wrap=180");
        double[] output = transform.Transform([-1d, 10d, 0d]);

        Assert.Equal(359d, output[0], 12);
        Assert.Equal(10d, output[1], 12);
        Assert.Equal(0d, output[2], 12);
    }

    private static MathTransform RequirePipelineMathTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }

    private static string RequirePipelineValidationFailure(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string? skipReason);
        Assert.False(ok);
        return Assert.IsType<string>(skipReason);
    }
}
