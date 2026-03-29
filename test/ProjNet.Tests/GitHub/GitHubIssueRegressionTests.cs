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
/// Represents the documented type.
/// </summary>
[Xunit.Trait("Category", "GitHub Issue")]
public class GitHubIssueRegressionTests
{
    private static readonly CoordinateSystemServices Css = new CoordinateSystemServices(CoordinateSystemServicesTests.LoadCsv());

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Issue #10, ConcatenatedTransform.Inverse() method destroys the state of child transformations")]
    public void TestConcatenatedTransformInvert()
    {
        CoordinateSystem epsg31466 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem(31466));
        CoordinateSystem epsg25832 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem(25832));

        ConcatenatedTransform ctFwd = Assert.IsType<ConcatenatedTransform>(Assert.IsAssignableFrom<ICoordinateTransformation>(Css.CreateTransformation(epsg31466, epsg25832)).MathTransform);
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
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Issue #10, Repeated Inverse() calls keep ConcatenatedTransform stable")]
    public void TestConcatenatedTransformInverseIsStableAcrossRepeatedCalls()
    {
        CoordinateSystem epsg31466 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem(31466));
        CoordinateSystem epsg25832 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem(25832));

        ConcatenatedTransform ctFwd = Assert.IsType<ConcatenatedTransform>(Assert.IsAssignableFrom<ICoordinateTransformation>(Css.CreateTransformation(epsg31466, epsg25832)).MathTransform);

        (double X, double Y) source = (3500000d, 5640000d);
        (double X, double Y) projected = ctFwd.Transform(source.X, source.Y);

        ConcatenatedTransform inverse1 = Assert.IsType<ConcatenatedTransform>(ctFwd.Inverse());
        (double X, double Y) roundtrip1 = inverse1.Transform(projected.X, projected.Y);

        ConcatenatedTransform inverse2 = Assert.IsType<ConcatenatedTransform>(ctFwd.Inverse());
        (double X, double Y) roundtrip2 = inverse2.Transform(projected.X, projected.Y);
        (double X, double Y) projectedAgain = ctFwd.Transform(source.X, source.Y);

        const double roundtripTolerance = 2d;
        const double stabilityTolerance = 1e-12;

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
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Issue #20, Math transform bug")]
    public void TestMathTransformBug()
    {
        var coordinateTransformFactory = new CoordinateTransformationFactory();
        var coordinateSystemFactory = new CoordinateSystemFactory();
        var itmParameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 31.734393611111109123611111111111),
            new ProjectionParameter("central_meridian", 35.204516944444442572222222222222),
            new ProjectionParameter("false_northing", 626907.390),
            new ProjectionParameter("false_easting", 219529.584),
            new ProjectionParameter("scale_factor", 1.0000067),
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

        MathTransform ctFwd = Assert.IsAssignableFrom<ICoordinateTransformation>(Css.CreateTransformation(itm, wgs84)).MathTransform;
        (double X, double Y) pt1a = (X: 200000.0, Y: 600000.0);
        (double X, double Y) pt2a = ctFwd.Transform(pt1a.X, pt1a.Y);
        (double X, double Y) pt1b = ctFwd.Inverse().Transform(pt2a.X, pt2a.Y);
        (double X, double Y) pt2b = ctFwd.Transform(pt1a.X, pt1a.Y);

        Assert.InRange(pt1b.X, pt1a.X - 0.01, pt1a.X + 0.01);
        Assert.InRange(pt1b.Y, pt1a.Y - 0.01, pt1a.Y + 0.01);
        Assert.Equal(pt2b, pt2a);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestIssuesWith3857To25832()
    {
        ProjectedCoordinateSystem epsg_3857 = ProjectedCoordinateSystem.WebMercator;
        Console.WriteLine(epsg_3857.Projection.ClassName);
        Console.WriteLine(epsg_3857.WKT);

        CoordinateSystem epsg25832 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem(25832));

        MathTransform mt1 = Assert.IsAssignableFrom<ICoordinateTransformation>(Css.CreateTransformation(epsg25832, epsg_3857)).MathTransform;
        (int X, int Y) pt25832 = (X: 702575, Y: 6153153);
        (double X, double Y) pt_3857ex = (X: 1358761.89, Y: 7456070.47);

        (double X, double Y) pt_3857 = mt1.Transform(pt25832.X, pt25832.Y);
        Assert.InRange(pt_3857.X, pt_3857ex.X - 0.015, pt_3857ex.X + 0.015);
        Assert.InRange(pt_3857.Y, pt_3857ex.Y - 0.015, pt_3857ex.Y + 0.015);

        epsg_3857 = Assert.IsAssignableFrom<ProjectedCoordinateSystem>(Css.GetCoordinateSystem(3857));
        Console.WriteLine(epsg_3857.Projection.ClassName);
        Console.WriteLine(epsg_3857.WKT);

        MathTransform mt2 = Assert.IsAssignableFrom<ICoordinateTransformation>(Css.CreateTransformation(epsg25832, epsg_3857)).MathTransform;
        pt_3857 = mt2.Transform(pt25832.X, pt25832.Y);
        Assert.InRange(pt_3857.X, pt_3857ex.X - 0.015, pt_3857ex.X + 0.015);
        Assert.InRange(pt_3857.Y, pt_3857ex.Y - 0.015, pt_3857ex.Y + 0.015);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Convert latitude/longitude to Canada grid NAD83 (epsg:26910)")]
    public void TestConvertWgs84ToEPSG26910()
    {
        CoordinateSystem epsg26910 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 26910));
        CoordinateSystem epsg_4326 = Assert.IsAssignableFrom<CoordinateSystem>(Css.GetCoordinateSystem("EPSG", 4326));

        double[] ptI = { 3523562.711189, 6246615.391161 };

        ICoordinateTransformation ct = Assert.IsAssignableFrom<ICoordinateTransformation>(Css.CreateTransformation(epsg26910, epsg_4326));
        (double X, double Y) pt1a = ct.MathTransform.Transform(ptI[0], ptI[1]);
        Assert.InRange(pt1a.X, -82.0479097 - 0.01, -82.0479097 + 0.01);
        Assert.InRange(pt1a.Y, 48.4185597 - 0.01, 48.4185597 + 0.01);

        // var pt1b = ct.MathTransform.Inverse().Transform(pt1a);
        // Assert.InRange(pt1b[0], 3523562.711189 - 0.01, 3523562.711189 + 0.01);
        // Assert.InRange(pt1b[1], 6246615.391161 - 0.01, 6246615.391161 + 0.01);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(Skip = "Requires DotSpatial.Projections, Result same as in TestConvertWgs84ToEPSG26910")]
    public void TestConvertWgs84ToEPSG26910DS()
    {
        // var epsg26910 = DotSpatial.Projections.ProjectionInfo.FromEpsgCode(26910);
        // var epsg_4326 = DotSpatial.Projections.ProjectionInfo.FromEpsgCode(4326);
        //
        // var ptI = new double[] { 3523562.711189, 6246615.391161 };
        //
        // DotSpatial.Projections.Reproject.ReprojectPoints(ptI, null, epsg26910, epsg_4326, 0, 1);
        // Assert.InRange(ptI[0], -82.0479097 - 0.01, -82.0479097 + 0.01);
        // Assert.InRange(ptI[1], 48.4185597 - 0.01, 48.4185597 + 0.01);
        //
        // DotSpatial.Projections.Reproject.ReprojectPoints(ptI, null, epsg_4326, epsg26910, 0, 1);
        // Assert.InRange(ptI[0], 3523562.711189 - 0.01, 3523562.711189 + 0.01);
        // Assert.InRange(ptI[1], 6246615.391161 - 0.01, 6246615.391161 + 0.01);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Issue #64, Wrong parameter order when calling base constructor (in systems extending HorizontalCoordinateSystem)")]
    public void TestHorizontalCoordinateSystemImplementationsAbbreviationAndRemarks()
    {
        string abbreviation = "TestAbbreviation";
        string remarks = "This is a test remark.";

        // construct a GeographicCoordinateSystem to test
        var gcsAxes = new List<AxisInfo>(2);
        gcsAxes.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
        gcsAxes.Add(new AxisInfo("Lat", AxisOrientationEnum.North));

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
            new ProjectionParameter("latitude_of_origin", 0.0),
            new ProjectionParameter("central_meridian", 0.0),
            new ProjectionParameter("false_easting", 0.0),
            new ProjectionParameter("false_northing", 0.0),
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
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North),
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
}
