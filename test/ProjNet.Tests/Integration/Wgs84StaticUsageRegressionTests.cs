// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for the main runtime consumers of the public WGS84 static accessors.
/// </summary>
public class Wgs84StaticUsageRegressionTests
{
    /// <summary>
    /// Verifies that the public WGS84 geographic and geocentric statics still round-trip through the standard datum transform path.
    /// </summary>
    [Fact]
    public void CoordinateTransformationFactory_WithWgs84Statics_RoundTripsThroughGeocentricTransform()
    {
        var factory = new CoordinateTransformationFactory();
        ICoordinateTransformation transformation = factory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84,
            GeocentricCoordinateSystem.WGS84);

        double[] source = [12d, 55d, 120d];
        double[] geocentric = transformation.MathTransform.Transform(source);
        double[] roundTrip = transformation.MathTransform.Inverse().Transform(geocentric);

        Assert.Equal(source[0], roundTrip[0], 9);
        Assert.Equal(source[1], roundTrip[1], 9);
        Assert.Equal(source[2], roundTrip[2], 6);
    }

    /// <summary>
    /// Verifies that bound coordinate systems still normalize against the canonical lon/lat WGS84 runtime target.
    /// </summary>
    [Fact]
    public void CoordinateTransformationFactory_WithBoundSourceAndWgs84StaticTarget_MatchesLegacyRuntime()
    {
        Wgs84ConversionInfo parameters = new(1, 2, 3, 4, 5, 6, 7);
        GeographicCoordinateSystem sourceCoordinateSystem = CreateGeographicCoordinateSystem("Bound source", CreateHorizontalDatum(null));
        GeographicCoordinateSystem legacySourceCoordinateSystem = CreateGeographicCoordinateSystem("Legacy source", CreateHorizontalDatum(parameters));
        GeographicCoordinateSystem targetCoordinateSystem = GeographicCoordinateSystem.WGS84;
        BoundCoordinateSystem boundSource = new(
            sourceCoordinateSystem,
            targetCoordinateSystem,
            new BoundTransformation("Position Vector transformation (geog2D domain)", parameters),
            "Bound source",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        var factory = new CoordinateTransformationFactory();
        ICoordinateTransformation boundTransformation = factory.CreateFromCoordinateSystems(boundSource, targetCoordinateSystem);
        ICoordinateTransformation legacyTransformation = factory.CreateFromCoordinateSystems(legacySourceCoordinateSystem, targetCoordinateSystem);

        double[] boundOutput = boundTransformation.MathTransform.Transform([10d, 50d]);
        double[] legacyOutput = legacyTransformation.MathTransform.Transform([10d, 50d]);

        Assert.Equal(AxisOrientationEnum.East, targetCoordinateSystem.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.North, targetCoordinateSystem.GetAxis(1).Orientation);
        Assert.Equal(legacyOutput[0], boundOutput[0], 9);
        Assert.Equal(legacyOutput[1], boundOutput[1], 9);
    }

    /// <summary>
    /// Verifies that WGS84 UTM pipeline steps stay aligned with the public WGS84 static coordinate systems.
    /// </summary>
    [Fact]
    public void ProjPipelineFactory_WithWgs84UtmStep_MatchesStaticCoordinateSystems()
    {
        const string operation = "+proj=pipeline +step +proj=utm +zone=32 +datum=WGS84";
        MathTransform pipelineTransform = RequirePipelineMathTransform(operation);
        var factory = new CoordinateTransformationFactory();
        ICoordinateTransformation staticTransformation = factory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84,
            ProjectedCoordinateSystem.WGS84_UTM(32, true));

        double[] source = [12d, 55d];
        double[] pipelineOutput = pipelineTransform.Transform(source);
        double[] staticOutput = staticTransformation.MathTransform.Transform(source);

        Assert.Equal(staticOutput[0], pipelineOutput[0], 5);
        Assert.Equal(staticOutput[1], pipelineOutput[1], 5);
    }

    /// <summary>
    /// Verifies that datum-aware geographic pipeline steps use the same public WGS84 runtime source as direct factory calls.
    /// </summary>
    [Fact]
    public void ProjPipelineFactory_WithDatumAwareGeographicStep_MatchesStaticCoordinateSystems()
    {
        const string operation = "+proj=pipeline +step +proj=longlat +datum=GGRS87";
        MathTransform pipelineTransform = RequirePipelineMathTransform(operation);
        GeographicCoordinateSystem targetCoordinateSystem = CreateGeographicCoordinateSystem(
            "GGRS87",
            new HorizontalDatum(
                Ellipsoid.GRS80,
                new Wgs84ConversionInfo(-199.87d, 74.79d, 246.02d, 0d, 0d, 0d, 0d),
                DatumType.HD_Geocentric,
                "GGRS87",
                string.Empty,
                -1,
                string.Empty,
                string.Empty,
                string.Empty));
        CoordinateTransformationFactory factory = new();
        ICoordinateTransformation staticTransformation = factory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84,
            targetCoordinateSystem);

        double[] source = [23.72d, 37.98d];
        double[] pipelineOutput = pipelineTransform.Transform(source);
        double[] staticOutput = staticTransformation.MathTransform.Transform(source);

        Assert.Equal(staticOutput[0], pipelineOutput[0], 9);
        Assert.Equal(staticOutput[1], pipelineOutput[1], 9);
    }

    private static GeographicCoordinateSystem CreateGeographicCoordinateSystem(string name, HorizontalDatum horizontalDatum)
    {
        return new GeographicCoordinateSystem(
            AngularUnit.Degrees,
            horizontalDatum,
            PrimeMeridian.Greenwich,
            [new AxisInfo("Lon", AxisOrientationEnum.East), new AxisInfo("Lat", AxisOrientationEnum.North)],
            name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static HorizontalDatum CreateHorizontalDatum(Wgs84ConversionInfo? parameters)
    {
        return new HorizontalDatum(
            Ellipsoid.GRS80,
            parameters,
            DatumType.HD_Geocentric,
            "Custom datum",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static MathTransform RequirePipelineMathTransform(string operation)
    {
        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }
}
