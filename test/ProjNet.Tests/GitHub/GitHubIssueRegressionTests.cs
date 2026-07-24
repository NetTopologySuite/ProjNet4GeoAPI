// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.GitHub;

using System;
using System.Collections.Generic;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for issues reported on the GitHub issue tracker.
/// </summary>
public class GitHubIssueRegressionTests
{
    private static readonly CoordinateSystemServices Css = new(CoordinateSystemServicesTests.LoadCsv());

    /// <summary>
    /// Verifies that GitHub issue #10 is fixed: <see cref="ConcatenatedTransform.Inverse"/> creates a new
    /// child transformation list and does not mutate the forward transform's state.
    /// </summary>
    [GitHubIssue(10)]
    [Fact(DisplayName = "Issue #10, ConcatenatedTransform.Inverse() method destroys the state of child transformations")]
    public void TestConcatenatedTransformInvert()
    {
        CoordinateSystem epsg31466 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(31466), exactMatch: false);
        CoordinateSystem epsg25832 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(25832), exactMatch: false);

        ConcatenatedTransform ctFwd = Assert.IsType<ConcatenatedTransform>(Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(epsg31466, epsg25832), exactMatch: false).MathTransform);
        var ctRev = (ConcatenatedTransform)ctFwd.Inverse();

        IList<ICoordinateTransformationCore> ctlFwd = ctFwd.CoordinateTransformationList;
        IList<ICoordinateTransformationCore> ctlRev = ctRev.CoordinateTransformationList;

        Assert.False(ReferenceEquals(ctlFwd, ctlRev));
        Assert.Equal(ctlRev.Count, ctlFwd.Count);
        for (int i = 0, j = ctlFwd.Count - 1; i < ctlFwd.Count; i++, j--)
        {
            Assert.False(ReferenceEquals(ctlFwd[i], ctlRev[j]));
        }
    }

    /// <summary>
    /// Verifies that GitHub issue #10 is fixed: repeated calls to <see cref="ConcatenatedTransform.Inverse"/>
    /// return the same cached instance and produce consistent round-trip results.
    /// </summary>
    [GitHubIssue(10)]
    [Fact(DisplayName = "Issue #10, Repeated Inverse() calls keep ConcatenatedTransform stable")]
    public void TestConcatenatedTransformInverseIsStableAcrossRepeatedCalls()
    {
        CoordinateSystem epsg31466 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(31466), exactMatch: false);
        CoordinateSystem epsg25832 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(25832), exactMatch: false);

        ConcatenatedTransform ctFwd = Assert.IsType<ConcatenatedTransform>(Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(epsg31466, epsg25832), exactMatch: false).MathTransform);

        (double X, double Y) source = (3500000d, 5640000d);
        (double X, double Y) projected = ctFwd.Transform(source.X, source.Y);

        ConcatenatedTransform inverse1 = Assert.IsType<ConcatenatedTransform>(ctFwd.Inverse());
        (double X, double Y) roundtrip1 = inverse1.Transform(projected.X, projected.Y);

        ConcatenatedTransform inverse2 = Assert.IsType<ConcatenatedTransform>(ctFwd.Inverse());
        (double X, double Y) roundtrip2 = inverse2.Transform(projected.X, projected.Y);
        (double X, double Y) projectedAgain = ctFwd.Transform(source.X, source.Y);

        const double roundtripTolerance = 2d;
        const double stabilityTolerance = TestTolerances.StableResult;

        Assert.Same(inverse1, inverse2);

        Assert.InRange(Math.Abs(roundtrip1.X - source.X), 0d, roundtripTolerance);
        Assert.InRange(Math.Abs(roundtrip1.Y - source.Y), 0d, roundtripTolerance);

        Assert.InRange(Math.Abs(roundtrip2.X - source.X), 0d, roundtripTolerance);
        Assert.InRange(Math.Abs(roundtrip2.Y - source.Y), 0d, roundtripTolerance);

        Assert.InRange(Math.Abs(roundtrip1.X - roundtrip2.X), 0d, stabilityTolerance);
        Assert.InRange(Math.Abs(roundtrip1.Y - roundtrip2.Y), 0d, stabilityTolerance);
        Assert.InRange(Math.Abs(projected.X - projectedAgain.X), 0d, stabilityTolerance);
        Assert.InRange(Math.Abs(projected.Y - projectedAgain.Y), 0d, stabilityTolerance);
    }

    /// <summary>
    /// Verifies that inverting a concatenated transform invalidates any previously cached inverse instance.
    /// </summary>
    [Fact(DisplayName = "ConcatenatedTransform.Invert clears stale inverse cache")]
    public void TestConcatenatedTransformInvertInvalidatesCachedInverse()
    {
        CoordinateSystem epsg31466 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(31466), exactMatch: false);
        CoordinateSystem epsg25832 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(25832), exactMatch: false);

        ConcatenatedTransform forward = Assert.IsType<ConcatenatedTransform>(Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(epsg31466, epsg25832), exactMatch: false).MathTransform);
        MathTransform cachedInverse = forward.Inverse();
        forward.Invert();
        MathTransform inverseAfterInvert = forward.Inverse();
        Assert.NotSame(cachedInverse, inverseAfterInvert);
        Assert.Same(inverseAfterInvert, forward.Inverse());
    }

    /// <summary>
    /// Verifies that concatenated transforms can invert child math transforms that only support <see cref="MathTransform.Inverse"/>.
    /// </summary>
    [Fact(DisplayName = "ConcatenatedTransform uses child Inverse() for immutable math transforms")]
    public void ConcatenatedTransformSupportsImmutableChildMathTransforms()
    {
        var child = new CoordinateTransformation(
            GeographicCoordinateSystem.WGS84,
            GeographicCoordinateSystem.WGS84,
            TransformType.Conversion,
            new ImmutableOffsetMathTransform(5d),
            "immutable",
            string.Empty,
            -1,
            string.Empty,
            string.Empty);
        var concatenated = new ConcatenatedTransform([child]);

        MathTransform inverse = concatenated.Inverse();
        double[] projected = concatenated.Transform([10d, 20d, 0d]);
        double[] roundtrip = inverse.Transform(projected);

        Assert.Equal(10d, roundtrip[0], 12);
        Assert.Equal(20d, roundtrip[1], 12);

        concatenated.Invert();
        (double x, double y, double z) = concatenated.Transform(15d, 25d, 0d);
        Assert.Equal(10d, x, 12);
        Assert.Equal(20d, y, 12);
        Assert.Equal(0d, z, 12);
    }

    /// <summary>
    /// Verifies that empty concatenated transforms fail with a clear exception instead of index errors.
    /// </summary>
    [Fact(DisplayName = "ConcatenatedTransform empty chain throws clear exception on metadata access")]
    public void ConcatenatedTransformEmptyChainThrowsClearException()
    {
        var transform = new ConcatenatedTransform();

        const string expectedMessage = "Concatenated transform does not contain any child transformations.";

        InvalidOperationException dimSourceException = Assert.Throws<InvalidOperationException>(() => _ = transform.DimSource);
        InvalidOperationException dimTargetException = Assert.Throws<InvalidOperationException>(() => _ = transform.DimTarget);
        InvalidOperationException sourceCsException = Assert.Throws<InvalidOperationException>(() => _ = transform.SourceCS);
        InvalidOperationException targetCsException = Assert.Throws<InvalidOperationException>(() => _ = transform.TargetCS);

        Assert.Equal(expectedMessage, dimSourceException.Message);
        Assert.Equal(expectedMessage, dimTargetException.Message);
        Assert.Equal(expectedMessage, sourceCsException.Message);
        Assert.Equal(expectedMessage, targetCsException.Message);
    }

    /// <summary>
    /// Verifies that <see cref="DatumTransform"/> applies the scale factor only once to rotation terms.
    /// </summary>
    [Fact(DisplayName = "DatumTransform applies single scale factor on rotation terms")]
    public void DatumTransformRotationTermsUseSingleScaleFactor()
    {
        const double secondsToRadians = 4.84813681109535993589914102357e-6;
        const double dx = -81.0703;
        const double dy = -89.3603;
        const double dz = -115.7526;
        const double ex = -0.48488;
        const double ey = -0.02436;
        const double ez = -0.41321;
        const double ppm = -540.645;
        const double x = 3657660.66;
        const double y = 255768.55;
        const double z = 5201382.11;

        double scale = 1d + (ppm * 0.000001d);
        double rx = ex * secondsToRadians;
        double ry = ey * secondsToRadians;
        double rz = ez * secondsToRadians;

        double expectedX = (scale * x) - (scale * rz * y) + (scale * ry * z) + dx;
        double expectedY = (scale * rz * x) + (scale * y) - (scale * rx * z) + dy;
        double expectedZ = (-scale * ry * x) + (scale * rx * y) + (scale * z) + dz;

        var transform = new DatumTransform(new Wgs84ConversionInfo(dx, dy, dz, ex, ey, ez, ppm));
        double[] actual = transform.Transform([x, y, z]);

        Assert.Equal(expectedX, actual[0], 9);
        Assert.Equal(expectedY, actual[1], 9);
        Assert.Equal(expectedZ, actual[2], 9);
    }

    /// <summary>
    /// Verifies that GitHub issue #20 is fixed: calling <see cref="MathTransform.Inverse"/> does not corrupt
    /// subsequent results of the forward transform.
    /// </summary>
    [GitHubIssue(20)]
    [Fact(DisplayName = "Issue #20, Math transform bug")]
    public void TestMathTransformBug()
    {
        _ = new CoordinateTransformationFactory();
        var coordinateSystemFactory = new CoordinateSystemFactory();
        var itmParameters = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 31.734393611111109123611111111111),
            new("central_meridian", 35.204516944444442572222222222222),
            new("false_northing", 626907.390),
            new("false_easting", 219529.584),
            new("scale_factor", 1.0000067),
        };

        HorizontalDatum itmDatum = coordinateSystemFactory.CreateHorizontalDatum(
            "Isreal 1993",
            DatumType.HD_Geocentric,
            Ellipsoid.GRS80,
            new Wgs84ConversionInfo(-24.0024, -17.1032, -17.8444, -0.33077, -1.85269, 1.66969, 5.4248));

        GeographicCoordinateSystem itmGeo = coordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ITM",
            AngularUnit.Degrees,
            itmDatum,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        IProjection itmProjection = coordinateSystemFactory.CreateProjection("Transverse_Mercator", "Transverse_Mercator", itmParameters);
        ProjectedCoordinateSystem itm = coordinateSystemFactory.CreateProjectedCoordinateSystem(
            "ITM",
            itmGeo,
            itmProjection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        GeographicCoordinateSystem wgs84 = ProjectedCoordinateSystem.WGS84_UTM(36, true).GeographicCoordinateSystem;

        MathTransform ctFwd = Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(itm, wgs84), exactMatch: false).MathTransform;
        (double X, double Y) pt1a = (X: 200000.0, Y: 600000.0);
        (double X, double Y) pt2a = ctFwd.Transform(pt1a.X, pt1a.Y);
        (double X, double Y) pt1b = ctFwd.Inverse().Transform(pt2a.X, pt2a.Y);
        (double X, double Y) pt2b = ctFwd.Transform(pt1a.X, pt1a.Y);

        Assert.InRange(pt1b.X, pt1a.X - 0.01, pt1a.X + 0.01);
        Assert.InRange(pt1b.Y, pt1a.Y - 0.01, pt1a.Y + 0.01);
        Assert.Equal(pt2b, pt2a);
    }

    /// <summary>
    /// Verifies that transformations between EPSG 25832 (UTM zone 32N) and EPSG 3857 (Web Mercator)
    /// produce accurate results regardless of how the Web Mercator system is obtained.
    /// </summary>
    [Fact]
    public void TestIssuesWith3857To25832()
    {
        ProjectedCoordinateSystem epsg_3857 = ProjectedCoordinateSystem.WebMercator;
        Console.WriteLine(epsg_3857.Projection.ClassName);
        Console.WriteLine(epsg_3857.WKT);

        CoordinateSystem epsg25832 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem(25832), exactMatch: false);

        MathTransform mt1 = Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(epsg25832, epsg_3857), exactMatch: false).MathTransform;
        (int X, int Y) pt25832 = (X: 702575, Y: 6153153);
        (double X, double Y) pt_3857ex = (X: 1358761.89, Y: 7456070.47);

        (double X, double Y) pt_3857 = mt1.Transform(pt25832.X, pt25832.Y);
        Assert.InRange(pt_3857.X, pt_3857ex.X - 0.015, pt_3857ex.X + 0.015);
        Assert.InRange(pt_3857.Y, pt_3857ex.Y - 0.015, pt_3857ex.Y + 0.015);

        epsg_3857 = Assert.IsType<ProjectedCoordinateSystem>(Css.GetCoordinateSystem(3857));
        Console.WriteLine(epsg_3857.Projection.ClassName);
        Console.WriteLine(epsg_3857.WKT);

        MathTransform mt2 = Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(epsg25832, epsg_3857), exactMatch: false).MathTransform;
        pt_3857 = mt2.Transform(pt25832.X, pt25832.Y);
        Assert.InRange(pt_3857.X, pt_3857ex.X - 0.015, pt_3857ex.X + 0.015);
        Assert.InRange(pt_3857.Y, pt_3857ex.Y - 0.015, pt_3857ex.Y + 0.015);
    }

    /// <summary>
    /// Verifies that transforming coordinates from EPSG 26910 (NAD83 / UTM zone 10N) to
    /// EPSG 4326 (WGS 84 geographic) produces accurate results.
    /// </summary>
    [Fact(DisplayName = "Convert latitude/longitude to Canada grid NAD83 (epsg:26910)")]
    public void TestConvertWgs84ToEPSG26910()
    {
        CoordinateSystem epsg26910 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 26910), exactMatch: false);
        CoordinateSystem epsg_4326 = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 4326), exactMatch: false);

        double[] ptI = [3523562.711189, 6246615.391161];

        ICoordinateTransformation ct = Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(epsg26910, epsg_4326), exactMatch: false);
        (double x, double y) = ct.MathTransform.Transform(ptI[0], ptI[1]);
        Assert.InRange(x, -82.0479097d - 0.01d, -82.0479097d + 0.01d);
        Assert.InRange(y, 48.4185597d - 0.01d, 48.4185597d + 0.01d);
    }

    /// <summary>
    /// Verifies that transforming EPSG 27700 coordinates to WGS 84 stays within the expected tolerance.
    /// </summary>
    [GitHubIssue(67)]
    [Fact(DisplayName = "Issue #67, OSGB36 to WGS84 stays within expected accuracy tolerance")]
    public void Osgb36ToWgs84TransformationMatchesExpectedCoordinate()
    {
        CoordinateSystem source = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 27700), exactMatch: false);
        CoordinateSystem target = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 4326), exactMatch: false);

        ICoordinateTransformation transformation = Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(source, target), exactMatch: false);
        (double longitude, double latitude) = transformation.MathTransform.Transform(362895d, 155602d);

        Assert.InRange(longitude, -2.5335813d - 0.0005d, -2.5335813d + 0.0005d);
        Assert.InRange(latitude, 51.2983258d - 0.0005d, 51.2983258d + 0.0005d);
    }

    /// <summary>
    /// Verifies that the NAD83 WKT from GitHub issue #51 transforms to WGS 84 with the expected offset.
    /// </summary>
    [GitHubIssue(51)]
    [Fact(DisplayName = "Issue #51, NAD83 WKT transforms to WGS84 within expected tolerance")]
    public void Nad83WktToWgs84MatchesExpectedCoordinate()
    {
        const string nad83Wkt =
            """
            GEOGCS["GCS_North_American_1983",DATUM["D_North_American_1983",SPHEROID["GRS_1980",6378137,298.257222101]],PRIMEM["Greenwich",0],UNIT["Degree",0.017453292519943295]]
            """;

        var transformationFactory = new CoordinateTransformationFactory();
        CoordinateSystem source = CoordinateSystemTestHelpers.RequireCoordinateSystem(nad83Wkt);

        ICoordinateTransformation transformation = transformationFactory.CreateFromCoordinateSystems(source, GeographicCoordinateSystem.WGS84);
        (double longitude, double latitude) = transformation.MathTransform.Transform(-120.5757999d, 47.4073238d);

        Assert.InRange(longitude, -120.575814456652d - 0.001d, -120.575814456652d + 0.001d);
        Assert.InRange(latitude, 47.4073295963295d - 0.001d, 47.4073295963295d + 0.001d);
    }

    /// <summary>
    /// Verifies that EPSG 28992 coordinates can be transformed to WGS 84 and remain within the Netherlands.
    /// </summary>
    [GitHubIssue(126)]
    [Fact(DisplayName = "Issue #126, Amersfoort / RD New transforms to a plausible WGS84 coordinate")]
    public void AmersfoortToWgs84TransformationProducesCoordinateInTheNetherlands()
    {
        CoordinateSystem source = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 28992), exactMatch: false);
        CoordinateSystem target = Assert.IsType<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 4326), exactMatch: false);

        ICoordinateTransformation transformation = Assert.IsType<ICoordinateTransformation>(Css.CreateTransformation(source, target), exactMatch: false);
        (double longitude, double latitude) = transformation.MathTransform.Transform(155000d, 463000d);

        Assert.False(double.IsNaN(longitude));
        Assert.False(double.IsNaN(latitude));
        Assert.InRange(longitude, 3d, 8d);
        Assert.InRange(latitude, 50d, 54d);
    }

    /// <summary>
    /// Verifies that GitHub issue #64 is fixed: <see cref="GeographicCoordinateSystem"/> and
    /// <see cref="ProjectedCoordinateSystem"/> correctly store abbreviation and remarks
    /// passed to their constructors.
    /// </summary>
    [GitHubIssue(64)]
    [Fact(DisplayName = "Issue #64, Wrong parameter order when calling base constructor (in systems extending HorizontalCoordinateSystem)")]
    public void TestHorizontalCoordinateSystemImplementationsAbbreviationAndRemarks()
    {
        string abbreviation = "TestAbbreviation";
        string remarks = "This is a test remark.";

        // construct a GeographicCoordinateSystem to test
        var gcsAxes = new List<AxisInfo>(2)
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        var geographicCoordinateSystem =
            new GeographicCoordinateSystem(
                AngularUnit.Degrees,
                HorizontalDatum.WGS84,
                PrimeMeridian.Greenwich,
                gcsAxes,
                "WGS 84",
                "EPSG",
                4326,
                string.Empty,
                abbreviation,
                remarks);

        Assert.Equal(abbreviation, geographicCoordinateSystem.Abbreviation);
        Assert.Equal(remarks, geographicCoordinateSystem.Remarks);

        // construct a ProjectedCoordinateSystem to test
        var pInfo = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0.0),
            new("central_meridian", 0.0),
            new("false_easting", 0.0),
            new("false_northing", 0.0),
        };

        var proj = new Projection(
            "Popular Visualisation Pseudo-Mercator",
            pInfo,
            "Popular Visualisation Pseudo-Mercator",
            "EPSG",
            3856,
            "Pseudo-Mercator",
            string.Empty,
            string.Empty);

        var pcsAxes = new List<AxisInfo>
            {
                new("East", AxisOrientationEnum.East),
                new("North", AxisOrientationEnum.North),
            };

        var projectedCoordinateSystem =
            new ProjectedCoordinateSystem(
                HorizontalDatum.WGS84,
                GeographicCoordinateSystem.WGS84,
                LinearUnit.Metre,
                proj,
                pcsAxes,
                "WGS 84 / Pseudo-Mercator",
                "EPSG",
                3857,
                "WGS 84 / Popular Visualisation Pseudo-Mercator",
                remarks,
                abbreviation);

        Assert.Equal(abbreviation, projectedCoordinateSystem.Abbreviation);
        Assert.Equal(remarks, projectedCoordinateSystem.Remarks);
    }

    private sealed class ImmutableOffsetMathTransform : MathTransform
    {
        private readonly double offset;

        public ImmutableOffsetMathTransform(double offset)
        {
            this.offset = offset;
        }

        public override int DimSource => 2;

        public override int DimTarget => 2;

        public override string WKT => throw new NotImplementedException();

        public override string XML => throw new NotImplementedException();

        public override bool Identity() => this.offset == 0d;

        public override MathTransform Inverse() => new ImmutableOffsetMathTransform(-this.offset);

        public override void Invert() => throw new NotSupportedException();

        public override void Transform(ref double x, ref double y, ref double z)
        {
            x += this.offset;
            y += this.offset;
        }
    }
}
