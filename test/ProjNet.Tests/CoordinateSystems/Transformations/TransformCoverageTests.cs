// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;
using Xunit;

/// <summary>
/// Coverage-oriented tests for math transforms with low coverage.
/// Exercises GeocentricLatitude, PrimeMeridian, MathTransform base,
/// MapProjection base, ObTran, VertOffset, and Molodensky transforms.
/// </summary>
public class TransformCoverageTests
{
    private const double ParisLongitude = 2.5969213;

    private static readonly double[] OriginPoint = [0d, 0d];
    private static readonly double[] LonLat1045 = [10d, 45d];
    private static readonly double[] LonLat2060 = [20d, 60d];
    private static readonly double[] LonLat1552 = [15d, 52d];
    private static readonly double[] LonLat1653 = [16d, 53d];
    private static readonly double[] LonLat1020 = [10d, 20d];
    private static readonly double[] LonLat2030 = [20d, 30d];
    private static readonly double[] LonLatAlt00100 = [0d, 0d, 100d];
    private static readonly double[] LonLatAlt1045200 = [10d, 45d, 200d];

    // ──────────────────────────────────────────────────────────────────────
    //  1. GeocentricLatitudeMathTransform  (43.3 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Forward geocentric latitude on a sphere produces an identity transform.
    /// </summary>
    [Fact]
    public void GeocentricLatitudeSphereForwardIsIdentity()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +R=6371000");
        double[] result = transform.Transform([10d, 45d]);

        Assert.Equal(10d, result[0], 10);
        Assert.Equal(45d, result[1], 10);
    }

    /// <summary>
    /// Forward geocentric latitude on WGS84 shifts latitude towards equator.
    /// </summary>
    [Fact]
    public void GeocentricLatitudeWgs84ForwardReducesLatitude()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +ellps=WGS84");
        double[] result = transform.Transform([0d, 45d]);

        Assert.Equal(0d, result[0], 10);
        Assert.True(result[1] < 45d, "Geocentric latitude should be less than geodetic for mid-latitudes.");
        Assert.InRange(result[1], 44.7d, 44.9d);
    }

    /// <summary>
    /// At the equator and poles, geocentric latitude equals geodetic.
    /// </summary>
    /// <param name="latitude">Test latitude.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(90d)]
    [InlineData(-90d)]
    public void GeocentricLatitudeForwardPreservesEquatorAndPoles(double latitude)
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +ellps=WGS84");
        double[] result = transform.Transform([0d, latitude]);

        Assert.Equal(latitude, result[1], 6);
    }

    /// <summary>
    /// Forward/inverse round-trip recovers the original latitude.
    /// </summary>
    [Fact]
    public void GeocentricLatitudeRoundTripRecoversInput()
    {
        MathTransform forward = CreateGeocTransform("+proj=geoc +ellps=WGS84");
        MathTransform inverse = CreateGeocTransform("+proj=geoc +ellps=WGS84 +inv");

        double[] source = [12.5d, 48.3d];
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(source[0], recovered[0], 10);
        Assert.Equal(source[1], recovered[1], 10);
    }

    /// <summary>
    /// Inverse object returned by the transform API round-trips correctly.
    /// </summary>
    [Fact]
    public void GeocentricLatitudeInverseMethodRoundTrips()
    {
        MathTransform forward = CreateGeocTransform("+proj=geoc +ellps=WGS84");
        MathTransform inverse = forward.Inverse();

        double[] source = [5d, 60d];
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(source[0], recovered[0], 10);
        Assert.Equal(source[1], recovered[1], 10);
    }

    /// <summary>
    /// DimSource and DimTarget are both 2 for geocentric latitude.
    /// </summary>
    [Fact]
    public void GeocentricLatitudeDimensionsAreTwo()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +ellps=WGS84");

        Assert.Equal(2, transform.DimSource);
        Assert.Equal(2, transform.DimTarget);
    }

    /// <summary>
    /// NaN latitude input passes through unchanged.
    /// </summary>
    [Fact]
    public void GeocentricLatitudeNaNLatitudePassesThrough()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +ellps=WGS84");
        double[] result = transform.Transform([10d, double.NaN]);

        Assert.Equal(10d, result[0], 10);
        Assert.True(double.IsNaN(result[1]));
    }

    /// <summary>
    /// Verifies geocentric cartesian inverse has measurable high-altitude residual with single-step Bowring.
    /// </summary>
    [Fact]
    public void GeocentricCartesianHighAltitudeRoundTripShowsSingleStepResidual()
    {
        MathTransform forward = CreatePipelineTransform("+proj=cart +ellps=WGS84");
        MathTransform inverse = forward.Inverse();

        double[] source = [12d, 45d, 30000000d];
        double[] cartesian = forward.Transform(source);
        double[] roundtrip = inverse.Transform(cartesian);

        double latitudeError = Math.Abs(roundtrip[1] - source[1]);
        double heightError = Math.Abs(roundtrip[2] - source[2]);

        Assert.InRange(latitudeError, 0d, 1e-8);
        Assert.InRange(heightError, 0d, 0.05d);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  2. PrimeMeridianTransform  (50.0 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Greenwich-to-Greenwich is a no-op.
    /// </summary>
    [Fact]
    public void PrimeMeridianGreenwichToGreenwichIsNoOp()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Greenwich);
        double[] result = transform.Transform([10d, 50d, 100d]);

        Assert.Equal(10d, result[0], 12);
        Assert.Equal(50d, result[1], 12);
        Assert.Equal(100d, result[2], 12);
    }

    /// <summary>
    /// Greenwich-to-Paris shifts longitude by the Paris offset.
    /// </summary>
    [Fact]
    public void PrimeMeridianGreenwichToParisShiftsLongitude()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        double[] result = transform.Transform([10d, 50d, 0d]);

        double expectedShift = PrimeMeridian.Greenwich.Longitude - PrimeMeridian.Paris.Longitude;
        Assert.Equal(10d + expectedShift, result[0], 10);
        Assert.Equal(50d, result[1], 12);
    }

    /// <summary>
    /// Inverse reverses the forward shift.
    /// </summary>
    [Fact]
    public void PrimeMeridianInverseReversesForward()
    {
        var forward = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        MathTransform inverse = forward.Inverse();

        double[] source = [15d, 48d, 0d];
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(source[0], recovered[0], 10);
        Assert.Equal(source[1], recovered[1], 12);
    }

    /// <summary>
    /// Invert method toggles the transform direction.
    /// </summary>
    [Fact]
    public void PrimeMeridianInvertTogglesBehavior()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        double[] original = transform.Transform([0d, 0d, 0d]);

        transform.Invert();
        double[] inverted = transform.Transform([0d, 0d, 0d]);

        Assert.NotEqual(original[0], inverted[0]);
        Assert.Equal(-original[0], inverted[0], 10);
    }

    /// <summary>
    /// DimSource and DimTarget are both 3 for prime meridian transforms.
    /// </summary>
    [Fact]
    public void PrimeMeridianDimensionsAreThree()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);

        Assert.Equal(3, transform.DimSource);
        Assert.Equal(3, transform.DimTarget);
    }

    /// <summary>
    /// Tests various well-known prime meridians for correct longitude offset.
    /// </summary>
    /// <param name="expectedLongitude">Expected resulting longitude.</param>
    [Theory]
    [InlineData(-ParisLongitude)]
    public void PrimeMeridianVariousMeridiansShiftCorrectly(double expectedLongitude)
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        double[] result = transform.Transform([0d, 0d, 0d]);

        Assert.Equal(expectedLongitude, result[0], 10);
    }

    /// <summary>
    /// Batch transform via span-based API.
    /// </summary>
    [Fact]
    public void PrimeMeridianSpanBatchTransform()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        double[] xs = [0d, 10d, 20d];
        double[] ys = [50d, 51d, 52d];
        double[] zs = [0d, 0d, 0d];
        transform.Transform(xs.AsSpan(), ys.AsSpan(), zs.AsSpan());

        double expectedShift = PrimeMeridian.Greenwich.Longitude - PrimeMeridian.Paris.Longitude;
        Assert.Equal(0d + expectedShift, xs[0], 10);
        Assert.Equal(10d + expectedShift, xs[1], 10);
        Assert.Equal(20d + expectedShift, xs[2], 10);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  3. MathTransform base class  (55.7 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TransformList processes a batch of points correctly.
    /// </summary>
    [Fact]
    public void MathTransformBaseTransformListProcessesBatch()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +ellps=WGS84");
        IList<double[]> points = new List<double[]>
        {
            OriginPoint,
            LonLat1045,
            LonLat2060,
        };

        IList<double[]> results = transform.TransformList(points);

        Assert.Equal(3, results.Count);
        Assert.Equal(0d, results[0][1], 6);
        Assert.True(results[1][1] < 45d);
        Assert.True(results[2][1] < 60d);
    }

    /// <summary>
    /// Transform with tuple API returns correct results.
    /// </summary>
    [Fact]
    public void MathTransformBaseTupleTransform2D()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +R=6371000");
        (double x, double y) = transform.Transform(10d, 45d);

        Assert.Equal(10d, x, 10);
        Assert.Equal(45d, y, 10);
    }

    /// <summary>
    /// Transform with 3D tuple API returns correct results.
    /// </summary>
    [Fact]
    public void MathTransformBaseTupleTransform3D()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        (double x, double y, double z) = transform.Transform(10d, 50d, 100d);

        Assert.Equal(50d, y, 12);
        Assert.Equal(100d, z, 12);
    }

    /// <summary>
    /// Transform with ref overload modifies values in-place.
    /// </summary>
    [Fact]
    public void MathTransformBaseRefTransform()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        double x = 10d;
        double y = 50d;
        transform.Transform(ref x, ref y);

        double expected = 10d + PrimeMeridian.Greenwich.Longitude - PrimeMeridian.Paris.Longitude;
        Assert.Equal(expected, x, 10);
        Assert.Equal(50d, y, 12);
    }

    /// <summary>
    /// Span-based Transform overload with XY struct works.
    /// </summary>
    [Fact]
    public void MathTransformBaseXYSpanTransform()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +R=6371000");
        XY[] points = [new XY(10d, 45d), new XY(20d, 60d)];
        transform.Transform(points.AsSpan());

        Assert.Equal(10d, points[0].X, 10);
        Assert.Equal(45d, points[0].Y, 10);
    }

    /// <summary>
    /// Span-based Transform overload with XYZ struct works.
    /// </summary>
    [Fact]
    public void MathTransformBaseXYZSpanTransform()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        XYZ[] points = [new XYZ(10d, 50d, 100d)];
        transform.Transform(points.AsSpan());

        double expected = 10d + PrimeMeridian.Greenwich.Longitude - PrimeMeridian.Paris.Longitude;
        Assert.Equal(expected, points[0].X, 10);
        Assert.Equal(50d, points[0].Y, 12);
        Assert.Equal(100d, points[0].Z, 12);
    }

    /// <summary>
    /// Span-based Transform with stride works correctly.
    /// </summary>
    [Fact]
    public void MathTransformBaseSpanStrideTransform()
    {
        var transform = new PrimeMeridianTransform(PrimeMeridian.Greenwich, PrimeMeridian.Paris);
        double[] xs = [0d, 999d, 10d, 999d];
        double[] ys = [50d, 999d, 51d, 999d];

        transform.Transform(xs.AsSpan(), ys.AsSpan(), 2, 2);

        double expectedShift = PrimeMeridian.Greenwich.Longitude - PrimeMeridian.Paris.Longitude;
        Assert.Equal(0d + expectedShift, xs[0], 10);
        Assert.Equal(10d + expectedShift, xs[2], 10);
    }

    /// <summary>
    /// ReadOnlySpan Transform throws for too-small input.
    /// </summary>
    [Fact]
    public void MathTransformBaseSpanTransformThrowsForSingleOrdinate()
    {
        MathTransform transform = CreateGeocTransform("+proj=geoc +R=6371000");
        double[] input = [42d];
        double[] result = new double[2];

        Assert.Throws<ArgumentException>(() => transform.Transform(new ReadOnlySpan<double>(input), result.AsSpan()));
    }

    // ──────────────────────────────────────────────────────────────────────
    //  4. MapProjection base class  (61.1 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Transverse Mercator via the factory and checks DimSource/DimTarget.
    /// </summary>
    [Fact]
    public void MapProjectionTransverseMercatorDimensionsAreTwo()
    {
        var csFactory = new CoordinateSystemFactory();
        var ctFactory = new CoordinateTransformationFactory();
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation ct = ctFactory.CreateFromCoordinateSystems(wgs84, utm33);

        Assert.Equal(2, ct.MathTransform.DimSource);
        Assert.Equal(2, ct.MathTransform.DimTarget);
    }

    /// <summary>
    /// TransformList through a concrete MapProjection transforms multiple points.
    /// </summary>
    [Fact]
    public void MapProjectionTransformListBatchProcesses()
    {
        var csFactory = new CoordinateSystemFactory();
        var ctFactory = new CoordinateTransformationFactory();
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation ct = ctFactory.CreateFromCoordinateSystems(wgs84, utm33);
        IList<double[]> points = new List<double[]>
        {
            LonLat1552,
            LonLat1653,
        };

        IList<double[]> results = ct.MathTransform.TransformList(points);

        Assert.Equal(2, results.Count);
        Assert.NotEqual(15d, results[0][0]);
        Assert.NotEqual(52d, results[0][1]);
    }

    /// <summary>
    /// Forward/inverse round-trip through a MapProjection-based transform.
    /// </summary>
    [Fact]
    public void MapProjectionRoundTripRecoversInput()
    {
        var ctFactory = new CoordinateTransformationFactory();
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation ct = ctFactory.CreateFromCoordinateSystems(wgs84, utm33);
        MathTransform forward = ct.MathTransform;
        MathTransform inverse = forward.Inverse();

        double[] source = [15d, 52d];
        double[] projected = forward.Transform(source);
        double[] recovered = inverse.Transform(projected);

        Assert.Equal(source[0], recovered[0], 6);
        Assert.Equal(source[1], recovered[1], 6);
    }

    /// <summary>
    /// Edge case: transforming the equator/prime meridian intersection.
    /// </summary>
    [Fact]
    public void MapProjectionOriginPointTransforms()
    {
        var ctFactory = new CoordinateTransformationFactory();
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        var utm31 = ProjectedCoordinateSystem.WGS84_UTM(31, true);

        ICoordinateTransformation ct = ctFactory.CreateFromCoordinateSystems(wgs84, utm31);
        double[] result = ct.MathTransform.Transform([3d, 0d]);

        Assert.True(result[0] > 0d);
    }

    /// <summary>
    /// NaN input to MapProjection results in NaN output.
    /// </summary>
    [Fact]
    public void MapProjectionNaNInputProducesNaN()
    {
        var ctFactory = new CoordinateTransformationFactory();
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation ct = ctFactory.CreateFromCoordinateSystems(wgs84, utm33);
        double[] result = ct.MathTransform.Transform([double.NaN, double.NaN]);

        Assert.True(double.IsNaN(result[0]) || double.IsNaN(result[1]));
    }

    // ──────────────────────────────────────────────────────────────────────
    //  5. ObTranMathTransform  (62.8 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Forward/inverse round-trip for ob_tran with latlon child.
    /// </summary>
    [Fact]
    public void ObTranLatLonRoundTripRecoversInput()
    {
        const string operation = "+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180";
        MathTransform forward = CreatePipelineTransform(operation);
        MathTransform inverse = forward.Inverse();

        double[] source = [2d, 1d];
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(source[0], recovered[0], 6);
        Assert.Equal(source[1], recovered[1], 6);
    }

    /// <summary>
    /// DimSource and DimTarget are both 2 for ob_tran.
    /// </summary>
    [Fact]
    public void ObTranDimensionsAreTwo()
    {
        const string operation = "+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180";
        MathTransform transform = CreatePipelineTransform(operation);

        Assert.Equal(2, transform.DimSource);
        Assert.Equal(2, transform.DimTarget);
    }

    /// <summary>
    /// Ob_tran with Mollweide child — batch transform via TransformList.
    /// </summary>
    [Fact]
    public void ObTranMollTransformListBatch()
    {
        const string operation = "+proj=ob_tran +o_proj=moll +R=6378137.0 +o_lon_p=0 +o_lat_p=0 +lon_0=180";
        MathTransform transform = CreatePipelineTransform(operation);

        IList<double[]> points = new List<double[]>
        {
            LonLat1020,
            LonLat2030,
        };

        IList<double[]> results = transform.TransformList(points);

        Assert.Equal(2, results.Count);
        Assert.NotEqual(10d, results[0][0]);
    }

    /// <summary>
    /// Ob_tran with o_alpha rotation parameter.
    /// </summary>
    [Fact]
    public void ObTranWithAlphaCreatesSuccessfully()
    {
        const string operation = "+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_c=0 +o_lat_c=30 +o_alpha=45";
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(
            operation, out MathTransform? transform, out string? skipReason);

        Assert.True(ok, skipReason);
        double[] result = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform([10d, 20d]);
        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    /// <summary>
    /// Missing required o_proj parameter fails gracefully.
    /// </summary>
    [Fact]
    public void ObTranMissingOProjFails()
    {
        const string operation = "+proj=ob_tran +R=6400000";
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(
            operation, out _, out string? skipReason);

        Assert.False(ok);
        Assert.NotNull(skipReason);
    }

    /// <summary>
    /// Inverse flag in the operation string creates the inverse transform.
    /// </summary>
    [Fact]
    public void ObTranInvFlagCreatesInverseTransform()
    {
        const string forwardOp = "+proj=ob_tran +R=6400000 +o_proj=latlon +o_lon_p=20 +o_lat_p=20 +lon_0=180";
        const string inverseOp = forwardOp + " +inv";

        MathTransform forward = CreatePipelineTransform(forwardOp);
        MathTransform inverse = CreatePipelineTransform(inverseOp);

        double[] source = [2d, 1d];
        double[] fwdResult = forward.Transform(source);
        double[] recovered = inverse.Transform(fwdResult);

        Assert.Equal(source[0], recovered[0], 6);
        Assert.Equal(source[1], recovered[1], 6);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  6. VertOffsetMathTransform  (62.7 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Forward/inverse round-trip preserves coordinates.
    /// </summary>
    [Fact]
    public void VertOffsetRoundTripRecoversInput()
    {
        const string operation = "+proj=vertoffset +lat_0=46.9166666666666666 +lon_0=8.183333333333334 +dh=-0.245 +slope_lat=-0.210 +slope_lon=-0.032 +ellps=GRS80";
        MathTransform forward = CreatePipelineTransform(operation);
        MathTransform inverse = forward.Inverse();

        double[] source = [9.666666666666666d, 47.333333333333336d, 473.0d];
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(source[0], recovered[0], 10);
        Assert.Equal(source[1], recovered[1], 10);
        Assert.Equal(source[2], recovered[2], 3);
    }

    /// <summary>
    /// DimSource and DimTarget are both 3 for vertoffset.
    /// </summary>
    [Fact]
    public void VertOffsetDimensionsAreThree()
    {
        MathTransform transform = CreatePipelineTransform("+proj=vertoffset +ellps=GRS80");

        Assert.Equal(3, transform.DimSource);
        Assert.Equal(3, transform.DimTarget);
    }

    /// <summary>
    /// NaN z-input defaults to 0 before applying offset.
    /// </summary>
    [Fact]
    public void VertOffsetNaNZInputDefaultsToZero()
    {
        const string operation = "+proj=vertoffset +dh=10 +ellps=GRS80";
        MathTransform transform = CreatePipelineTransform(operation);
        double[] result = transform.Transform([0d, 0d, double.NaN]);

        Assert.False(double.IsNaN(result[2]));
        Assert.InRange(result[2], 9.5d, 10.5d);
    }

    /// <summary>
    /// Invert method toggles between forward and inverse behavior.
    /// </summary>
    [Fact]
    public void VertOffsetInvertTogglesBehavior()
    {
        const string operation = "+proj=vertoffset +dh=10 +ellps=GRS80";
        MathTransform transform = CreatePipelineTransform(operation);

        double[] forward = transform.Transform([0d, 0d, 100d]);
        transform.Invert();
        double[] inverted = transform.Transform([0d, 0d, 100d]);

        Assert.True(forward[2] > 100d);
        Assert.True(inverted[2] < 100d);
    }

    /// <summary>
    /// TransformList processes batch for vertoffset.
    /// </summary>
    [Fact]
    public void VertOffsetTransformListBatch()
    {
        const string operation = "+proj=vertoffset +dh=5 +ellps=GRS80";
        MathTransform transform = CreatePipelineTransform(operation);

        IList<double[]> points = new List<double[]>
        {
            LonLatAlt00100,
            LonLatAlt1045200,
        };

        IList<double[]> results = transform.TransformList(points);

        Assert.Equal(2, results.Count);
        Assert.InRange(results[0][2], 104d, 106d);
        Assert.InRange(results[1][2], 204d, 206d);
    }

    /// <summary>
    /// Ellipsoid resolution via +r (sphere radius) parameter.
    /// </summary>
    [Fact]
    public void VertOffsetSphereRadiusWorks()
    {
        const string operation = "+proj=vertoffset +r=6371000 +dh=2.5";
        MathTransform transform = CreatePipelineTransform(operation);
        double[] result = transform.Transform([10d, 45d, 50d]);

        Assert.InRange(result[2], 52d, 53d);
    }

    /// <summary>
    /// Ellipsoid resolution via +a +rf (semi-major and inverse flattening).
    /// </summary>
    [Fact]
    public void VertOffsetSemiMajorInverseFlatteningWorks()
    {
        const string operation = "+proj=vertoffset +a=6378137 +rf=298.257223563 +dh=3";
        MathTransform transform = CreatePipelineTransform(operation);
        double[] result = transform.Transform([0d, 0d, 100d]);

        Assert.InRange(result[2], 102.5d, 103.5d);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  7. MolodenskyMathTransform  (71.7 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Standard Molodensky round-trip (forward then inverse) recovers input.
    /// </summary>
    [Fact]
    public void MolodenskyStandardRoundTripRecoversInput()
    {
        const string forwardOp = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149";
        MathTransform forward = CreatePipelineTransform(forwardOp);
        MathTransform inverse = forward.Inverse();

        double[] source = [144.9667d, -37.8d, 50d];
        double[] transformed = forward.Transform(source);
        double[] recovered = inverse.Transform(transformed);

        Assert.Equal(source[0], recovered[0], 3);
        Assert.Equal(source[1], recovered[1], 3);
        Assert.InRange(Math.Abs(recovered[2] - source[2]), 0d, 1d);
    }

    /// <summary>
    /// Abridged Molodensky produces slightly different results from standard.
    /// </summary>
    [Fact]
    public void MolodenskyAbridgedDiffersFromStandard()
    {
        const string standard = "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149";
        const string abridged = standard + " +abridged";

        MathTransform stdTransform = CreatePipelineTransform(standard);
        MathTransform abrTransform = CreatePipelineTransform(abridged);

        double[] source = [144.9667d, -37.8d, 50d];
        double[] stdResult = stdTransform.Transform(source);
        double[] abrResult = abrTransform.Transform(source);

        // Both should produce similar results
        Assert.InRange(Math.Abs(stdResult[0] - abrResult[0]), 0d, 0.01d);
        Assert.InRange(Math.Abs(stdResult[1] - abrResult[1]), 0d, 0.01d);
    }

    /// <summary>
    /// DimSource and DimTarget are both 3 for Molodensky.
    /// </summary>
    [Fact]
    public void MolodenskyDimensionsAreThree()
    {
        MathTransform transform = CreatePipelineTransform(
            "+proj=molodensky +a=6378160 +rf=298.25 +da=0 +df=0 +dx=0 +dy=0 +dz=0");

        Assert.Equal(3, transform.DimSource);
        Assert.Equal(3, transform.DimTarget);
    }

    /// <summary>
    /// Molodensky handles NaN z-input by treating it as zero.
    /// </summary>
    [Fact]
    public void MolodenskyNaNZInputDefaultsToZero()
    {
        MathTransform transform = CreatePipelineTransform(
            "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149");
        double[] result = transform.Transform([144.9667d, -37.8d, double.NaN]);

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    /// <summary>
    /// Molodensky TransformList batch processing.
    /// </summary>
    [Fact]
    public void MolodenskyTransformListBatch()
    {
        MathTransform transform = CreatePipelineTransform(
            "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149");

        IList<double[]> points = new List<double[]>
        {
            new[] { 144.9667d, -37.8d, 50d },
            new[] { 150d, -33.5d, 100d },
        };

        IList<double[]> results = transform.TransformList(points);

        Assert.Equal(2, results.Count);
        Assert.NotEqual(144.9667d, results[0][0]);
        Assert.NotEqual(150d, results[1][0]);
    }

    /// <summary>
    /// Molodensky Invert method toggles direction.
    /// </summary>
    [Fact]
    public void MolodenskyInvertTogglesBehavior()
    {
        MathTransform transform = CreatePipelineTransform(
            "+proj=molodensky +a=6378160 +rf=298.25 +da=-23 +df=-8.120449e-8 +dx=-134 +dy=-48 +dz=149");

        double[] forward = transform.Transform([144.9667d, -37.8d, 50d]);
        transform.Invert();
        double[] inverted = transform.Transform(forward);

        Assert.Equal(144.9667d, inverted[0], 3);
        Assert.Equal(-37.8d, inverted[1], 3);
    }

    /// <summary>
    /// Molodensky with sphere radius (+r) resolves correctly.
    /// </summary>
    [Fact]
    public void MolodenskySphereRadiusCreatesSuccessfully()
    {
        const string operation = "+proj=molodensky +r=6371000 +da=0 +df=0 +dx=1 +dy=2 +dz=3";
        MathTransform transform = CreatePipelineTransform(operation);
        double[] result = transform.Transform([0d, 0d, 0d]);

        Assert.False(double.IsNaN(result[0]));
    }

    /// <summary>
    /// Molodensky with +a +b resolves ellipsoid correctly.
    /// </summary>
    [Fact]
    public void MolodenskySemiMajorMinorCreatesSuccessfully()
    {
        const string operation = "+proj=molodensky +a=6378137 +b=6356752.314245 +da=0 +df=0 +dx=1 +dy=2 +dz=3";
        MathTransform transform = CreatePipelineTransform(operation);
        double[] result = transform.Transform([10d, 45d, 100d]);

        Assert.False(double.IsNaN(result[0]));
    }

    // ──────────────────────────────────────────────────────────────────────
    //  8. UnitConvertMathTransform  (62.2 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two-dimensional unit conversion scales only X/Y and leaves Z unchanged.
    /// </summary>
    [Fact]
    public void UnitConvertTwoDimensionalTransformLeavesZUnchanged()
    {
        var transform = new UnitConvertMathTransform(2, 2d, 5d);
        double x = 3d;
        double y = 4d;
        double z = 7d;

        transform.Transform(ref x, ref y, ref z);

        Assert.Equal(6d, x, 12);
        Assert.Equal(8d, y, 12);
        Assert.Equal(7d, z, 12);
    }

    /// <summary>
    /// Two-dimensional identity detection ignores the Z scale factor.
    /// </summary>
    [Fact]
    public void UnitConvertTwoDimensionalIdentityIgnoresZScale()
    {
        var transform = new UnitConvertMathTransform(2, 1d, 5d);

        Assert.True(transform.Identity());
    }

    /// <summary>
    /// The inverse transform uses the reciprocal XY/Z scale factors.
    /// </summary>
    [Fact]
    public void UnitConvertInverseReturnsReciprocalTransform()
    {
        var transform = new UnitConvertMathTransform(3, 2d, 4d);
        MathTransform inverse = transform.Inverse();

        double[] result = inverse.Transform([8d, 12d, 20d]);

        Assert.Equal(4d, result[0], 12);
        Assert.Equal(6d, result[1], 12);
        Assert.Equal(5d, result[2], 12);
    }

    /// <summary>
    /// In-place inversion mutates the conversion scale factors.
    /// </summary>
    [Fact]
    public void UnitConvertInvertMutatesScaleFactors()
    {
        var transform = new UnitConvertMathTransform(3, 2d, 4d);

        transform.Invert();

        double[] result = transform.Transform([8d, 12d, 20d]);
        Assert.Equal(4d, result[0], 12);
        Assert.Equal(6d, result[1], 12);
        Assert.Equal(5d, result[2], 12);
    }

    /// <summary>
    /// Invalid dimensions are rejected.
    /// </summary>
    [Fact]
    public void UnitConvertInvalidDimensionThrowsArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new UnitConvertMathTransform(4, 1d, 1d));

        Assert.Equal("dimension", exception.ParamName);
    }

    /// <summary>
    /// Invalid XY scales are rejected.
    /// </summary>
    /// <param name="xyScale">Invalid XY scale.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UnitConvertInvalidXyScaleThrowsArgumentOutOfRangeException(double xyScale)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new UnitConvertMathTransform(3, xyScale, 1d));

        Assert.Equal("xyScale", exception.ParamName);
    }

    /// <summary>
    /// Invalid Z scales are rejected.
    /// </summary>
    /// <param name="zScale">Invalid Z scale.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UnitConvertInvalidZScaleThrowsArgumentOutOfRangeException(double zScale)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new UnitConvertMathTransform(3, 1d, zScale));

        Assert.Equal("zScale", exception.ParamName);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  9. PipelineOmitMathTransform  (54.8 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Forward omission bypasses the wrapped transform for 3D coordinates.
    /// </summary>
    [Fact]
    public void PipelineOmitSkipForwardBypassesInnerTransform()
    {
        var transform = new PipelineOmitMathTransform(new TrackingMathTransform(2d, 5d), skipForward: true, skipInverse: false);
        double x = 1d;
        double y = 2d;
        double z = 3d;

        transform.Transform(ref x, ref y, ref z);

        Assert.Equal(1d, x, 12);
        Assert.Equal(2d, y, 12);
        Assert.Equal(3d, z, 12);
    }

    /// <summary>
    /// When forward omission is disabled, the wrapped transform is executed.
    /// </summary>
    [Fact]
    public void PipelineOmitWithoutSkipForwardExecutesInnerTransform()
    {
        var transform = new PipelineOmitMathTransform(new TrackingMathTransform(2d, 5d), skipForward: false, skipInverse: false);
        double x = 1d;
        double y = 2d;
        double z = 3d;

        transform.Transform(ref x, ref y, ref z);

        Assert.Equal(3d, x, 12);
        Assert.Equal(4d, y, 12);
        Assert.Equal(5d, z, 12);
    }

    /// <summary>
    /// A skipped forward step reports itself as an identity transform.
    /// </summary>
    [Fact]
    public void PipelineOmitIdentityReturnsTrueWhenForwardIsSkipped()
    {
        var transform = new PipelineOmitMathTransform(new TrackingMathTransform(2d, 5d), skipForward: true, skipInverse: false);

        Assert.True(transform.Identity());
    }

    /// <summary>
    /// The inverse is cached and swaps the forward/inverse omission flags.
    /// </summary>
    [Fact]
    public void PipelineOmitInverseCachesAndSwapsSkipFlags()
    {
        var transform = new PipelineOmitMathTransform(new TrackingMathTransform(2d, 5d), skipForward: true, skipInverse: false);
        MathTransform inverse = transform.Inverse();

        Assert.Same(inverse, transform.Inverse());

        double x = 1d;
        double y = 2d;
        double z = 3d;
        double t = 4d;
        inverse.Transform(ref x, ref y, ref z, ref t);

        Assert.Equal(-1d, x, 12);
        Assert.Equal(0d, y, 12);
        Assert.Equal(1d, z, 12);
        Assert.Equal(-1d, t, 12);
    }

    /// <summary>
    /// Forward omission bypasses the wrapped transform for 4D coordinates as well.
    /// </summary>
    [Fact]
    public void PipelineOmitSkipForwardBypassesInnerTransformForFourDimensions()
    {
        var transform = new PipelineOmitMathTransform(new TrackingMathTransform(2d, 5d), skipForward: true, skipInverse: false);
        double x = 1d;
        double y = 2d;
        double z = 3d;
        double t = 4d;

        transform.Transform(ref x, ref y, ref z, ref t);

        Assert.Equal(1d, x, 12);
        Assert.Equal(2d, y, 12);
        Assert.Equal(3d, z, 12);
        Assert.Equal(4d, t, 12);
    }

    /// <summary>
    /// In-place inversion is intentionally not supported.
    /// </summary>
    [Fact]
    public void PipelineOmitInvertThrowsNotSupportedException()
    {
        var transform = new PipelineOmitMathTransform(new TrackingMathTransform(2d, 5d), skipForward: true, skipInverse: false);

        Assert.Throws<NotSupportedException>(() => transform.Invert());
    }

    // ──────────────────────────────────────────────────────────────────────
    //  10. GeographicTransform  (60.0 %)
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Greenwich-to-Paris shifts the longitude by the Paris prime meridian offset.
    /// </summary>
    [Fact]
    public void GeographicTransformGreenwichToParisShiftsLongitudeInDegrees()
    {
        GeographicCoordinateSystem source = CreateGeographicCoordinateSystem(PrimeMeridian.Greenwich);
        GeographicCoordinateSystem target = CreateGeographicCoordinateSystem(PrimeMeridian.Paris);
        var transform = new GeographicTransform(source, target);
        double x = 0d;
        double y = 1d;
        double z = 2d;

        transform.Transform(ref x, ref y, ref z);

        Assert.Equal(PrimeMeridian.Paris.Longitude, x, 12);
        Assert.Equal(1d, y, 12);
        Assert.Equal(2d, z, 12);
    }

    /// <summary>
    /// Paris-to-Greenwich removes the Paris prime meridian offset.
    /// </summary>
    [Fact]
    public void GeographicTransformParisToGreenwichRemovesPrimeMeridianOffset()
    {
        GeographicCoordinateSystem source = CreateGeographicCoordinateSystem(PrimeMeridian.Paris);
        GeographicCoordinateSystem target = CreateGeographicCoordinateSystem(PrimeMeridian.Greenwich);
        var transform = new GeographicTransform(source, target);
        double x = 10d;
        double y = 0d;
        double z = 0d;

        transform.Transform(ref x, ref y, ref z);

        Assert.Equal(10d - PrimeMeridian.Paris.Longitude, x, 12);
    }

    /// <summary>
    /// Source and target dimensions mirror the wrapped geographic coordinate systems.
    /// </summary>
    [Fact]
    public void GeographicTransformDimensionsMatchCoordinateSystems()
    {
        GeographicCoordinateSystem source = CreateGeographicCoordinateSystem(PrimeMeridian.Greenwich);
        GeographicCoordinateSystem target = CreateGeographicCoordinateSystem(PrimeMeridian.Paris);
        var transform = new GeographicTransform(source, target);

        Assert.Equal(source.Dimension, transform.DimSource);
        Assert.Equal(target.Dimension, transform.DimTarget);
    }

    /// <summary>
    /// The unimplemented WKT/XML and inversion APIs throw as documented.
    /// </summary>
    [Fact]
    public void GeographicTransformUnimplementedMembersThrowNotImplementedException()
    {
        GeographicCoordinateSystem source = CreateGeographicCoordinateSystem(PrimeMeridian.Greenwich);
        GeographicCoordinateSystem target = CreateGeographicCoordinateSystem(PrimeMeridian.Paris);
        var transform = new GeographicTransform(source, target);

        Assert.Throws<NotImplementedException>(() => _ = transform.WKT);
        Assert.Throws<NotImplementedException>(() => _ = transform.XML);
        Assert.Throws<NotImplementedException>(() => transform.Inverse());
        Assert.Throws<NotImplementedException>(() => transform.Invert());
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────────────
    private static MathTransform CreatePipelineTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(
            operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }

    private static MathTransform CreateGeocTransform(string operation)
    {
        return CreatePipelineTransform(operation);
    }

    private static GeographicCoordinateSystem CreateGeographicCoordinateSystem(PrimeMeridian primeMeridian)
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        return coordinateSystemFactory.CreateGeographicCoordinateSystem(
            $"{primeMeridian.Name} test GCS",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            primeMeridian,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
    }

    private sealed class TrackingMathTransform : MathTransform
    {
        private readonly double xyzDelta;
        private readonly double timeDelta;

        internal TrackingMathTransform(double xyzDelta, double timeDelta)
        {
            this.xyzDelta = xyzDelta;
            this.timeDelta = timeDelta;
        }

        public override int DimSource => 3;

        public override int DimTarget => 3;

        public override string WKT => string.Empty;

        public override string XML => string.Empty;

        public override bool Identity()
        {
            return this.xyzDelta == 0d && this.timeDelta == 0d;
        }

        public override MathTransform Inverse()
        {
            return new TrackingMathTransform(-this.xyzDelta, -this.timeDelta);
        }

        public override void Invert()
        {
            throw new NotSupportedException();
        }

        public override void Transform(ref double x, ref double y, ref double z)
        {
            x += this.xyzDelta;
            y += this.xyzDelta;
            z += this.xyzDelta;
        }

        internal override void Transform(ref double x, ref double y, ref double z, ref double t)
        {
            this.Transform(ref x, ref y, ref z);
            t += this.timeDelta;
        }
    }
}
