// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests.GitHub;

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
    private static CoordinateSystemServices css = new CoordinateSystemServices(CoordinateSystemServicesTests.LoadCsv());

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Issue #10, ConcatenatedTransform.Inverse() method destroys the state of child transformations")]
    public void TestConcatenatedTransformInvert()
    {
        var epsg31466 = css.GetCoordinateSystem(31466);
        var epsg25832 = css.GetCoordinateSystem(25832);

        var ctFwd = (ConcatenatedTransform)css.CreateTransformation(epsg31466, epsg25832).MathTransform;
        var ctRev = (ConcatenatedTransform)ctFwd.Inverse();

        var ctlFwd = ctFwd.CoordinateTransformationList;
        var ctlRev = ctRev.CoordinateTransformationList;

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

        var itmDatum = coordinateSystemFactory.CreateHorizontalDatum(
            "Isreal 1993",
            DatumType.HD_Geocentric,
            Ellipsoid.GRS80,
            new Wgs84ConversionInfo(-24.0024, -17.1032, -17.8444, -0.33077, -1.85269, 1.66969, 5.4248));

        var itmGeo = coordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ITM",
            AngularUnit.Degrees,
            itmDatum,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        var itmProjection = coordinateSystemFactory.CreateProjection("Transverse_Mercator", "Transverse_Mercator", itmParameters);
        var itm = coordinateSystemFactory.CreateProjectedCoordinateSystem(
            "ITM",
            itmGeo,
            itmProjection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        var wgs84 = ProjectedCoordinateSystem.WGS84_UTM(36, true).GeographicCoordinateSystem;

        var ctFwd = css.CreateTransformation(itm, wgs84).MathTransform;
        var pt1a = (x: 200000.0, y: 600000.0);
        var pt2a = ctFwd.Transform(pt1a.x, pt1a.y);
        var pt1b = ctFwd.Inverse().Transform(pt2a.X, pt2a.Y);
        var pt2b = ctFwd.Transform(pt1a.x, pt1a.y);

        Assert.InRange(pt1b.X, pt1a.x - 0.01, pt1a.x + 0.01);
        Assert.InRange(pt1b.Y, pt1a.y - 0.01, pt1a.y + 0.01);
        Assert.Equal(pt2b, pt2a);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestIssuesWith3857To25832()
    {
        var epsg_3857 = ProjectedCoordinateSystem.WebMercator;
        Console.WriteLine(epsg_3857.Projection.ClassName);
        Console.WriteLine(epsg_3857.WKT);

        var epsg25832 = css.GetCoordinateSystem(25832);

        var mt1 = css.CreateTransformation(epsg25832, epsg_3857).MathTransform;
        var pt25832 = (x: 702575, y: 6153153);
        var pt_3857ex = (x: 1358761.89, y: 7456070.47);

        var pt_3857 = mt1.Transform(pt25832.x, pt25832.y);
        Assert.InRange(pt_3857.X, pt_3857ex.x - 0.015, pt_3857ex.x + 0.015);
        Assert.InRange(pt_3857.Y, pt_3857ex.y - 0.015, pt_3857ex.y + 0.015);

        epsg_3857 = Assert.IsType<ProjectedCoordinateSystem>(css.GetCoordinateSystem(3857));
        Console.WriteLine(epsg_3857.Projection.ClassName);
        Console.WriteLine(epsg_3857.WKT);

        var mt2 = css.CreateTransformation(epsg25832, epsg_3857).MathTransform;
        pt_3857 = mt2.Transform(pt25832.x, pt25832.y);
        Assert.InRange(pt_3857.X, pt_3857ex.x - 0.015, pt_3857ex.x + 0.015);
        Assert.InRange(pt_3857.Y, pt_3857ex.y - 0.015, pt_3857ex.y + 0.015);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Convert latitude/longitude to Canada grid NAD83 (epsg:26910)")]
    public void TestConvertWgs84ToEPSG26910()
    {
        var epsg26910 = css.GetCoordinateSystem("EPSG", 26910);
        var epsg_4326 = css.GetCoordinateSystem("EPSG", 4326);

        double[] ptI = { 3523562.711189, 6246615.391161 };

        var ct = css.CreateTransformation(epsg26910, epsg_4326);
        var pt1a = ct.MathTransform.Transform(ptI[0], ptI[1]);
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
