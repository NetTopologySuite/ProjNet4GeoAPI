// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for issues reported in the ProjNet issue tracker.
/// </summary>
public class ProjNetIssueRegressionTests : CoordinateTransformTestsBase
{
    private const string Discussion361248Wgs84Wkt = @"GEOGCS[""WGS 84"",
    DATUM[""WGS_1984"",
        SPHEROID[""WGS 84"",6378137,298.257223563,
            AUTHORITY[""EPSG"",""7030""]],
        AUTHORITY[""EPSG"",""6326""]],
    PRIMEM[""Greenwich"",0,
        AUTHORITY[""EPSG"",""8901""]],
    UNIT[""degree"",0.01745329251994328,
        AUTHORITY[""EPSG"",""9122""]],
    AUTHORITY[""EPSG"",""4326""]]";

    private static readonly double[] TestDiscussion3612481Expected = [2349315.05731837, 6524249.91789138];
    private static readonly double[] TestDiscussion3612481Input = [136d, -30d];
    private static readonly double[] TestDiscussion3612482Expected = [-77.191769, 38.101147];
    private static readonly double[] TestDiscussion3612482Input = [307821.867, 4219306.387];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjNetIssueRegressionTests"/> class.
    /// </summary>
    public ProjNetIssueRegressionTests()
    {
        this.Verbose = true;
    }

    /// <summary>
    /// Verifies ProjNet issue 23773: WGS84 UTM zone 18N to WGS84 geographic transformation
    /// produces accurate results both from a programmatic and WKT-defined coordinate system.
    /// </summary>
    [Fact(DisplayName = "WGS_84UTM to WGS_84 is inaccurate")]
    public void TestIssue23773()
    {
        var csUtm18N = ProjectedCoordinateSystem.WGS84_UTM(18, true);
        CoordinateSystem csUtm18NWkt = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            "PROJCS[\"WGS 84 / UTM zone 18N\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-75],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"32618\"]]");
        GeographicCoordinateSystem csWgs84 = GeographicCoordinateSystem.WGS84;

        ICoordinateTransformation ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csUtm18N, csWgs84);
        ICoordinateTransformation ct2 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csUtm18NWkt, csWgs84);

        double[] putm = [307821.867d, 4219306.387d];
        double[] pgeo = ct.MathTransform.Transform(putm);
        double[] pgeoWkt = ct2.MathTransform.Transform(putm);
        double[] pExpected = [-77.191769, 38.101147d];

        Assert.True(
            this.ToleranceLessThan(pgeoWkt, pExpected, 0.00001d),
            this.TransformationError("UTM18N -> WGS84", pExpected, pgeo));
        Assert.True(
            this.ToleranceLessThan(pgeo, pExpected, 0.00001d),
            this.TransformationError("UTM18N -> WGS84", pExpected, pgeo));
    }

    /// <summary>
    /// Verifies reprojection from EPSG 28414 (Pulkovo 1942 / Gauss-Kruger zone 14) to
    /// EPSG 4284 (Pulkovo 1942 geographic), per CodePlex discussion 351733.
    /// </summary>
    [Fact(DisplayName = "Proj.net reprojection problem, Discussion http://projnet.codeplex.com/discussions/351733")]
    public void TestDiscussion351733()
    {
        CoordinateSystem csSource = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            "PROJCS[\"Pulkovo 1942 / Gauss-Kruger zone 14\",GEOGCS[\"Pulkovo 1942\",DATUM[\"Pulkovo_1942\",SPHEROID[\"Krassowsky 1940\",6378245,298.3,AUTHORITY[\"EPSG\",\"7024\"]],TOWGS84[23.92,-141.27,-80.9,-0,0.35,0.82,-0.12],AUTHORITY[\"EPSG\",\"6284\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4284\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",81],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",14500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",NORTH],AXIS[\"Y\",EAST],AUTHORITY[\"EPSG\",\"28414\"]]\"");
        CoordinateSystem csTarget = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            "GEOGCS[\"Pulkovo 1942\",DATUM[\"Pulkovo_1942\",SPHEROID[\"Krassowsky 1940\",6378245,298.3,AUTHORITY[\"EPSG\",\"7024\"]],TOWGS84[23.92,-141.27,-80.9,-0,0.35,0.82,-0.12],AUTHORITY[\"EPSG\",\"6284\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4284\"]]\"");

        ICoordinateTransformation ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csSource, csTarget);

        double[] pp = [14181052.913, 6435927.692];
        double[] pg = ct.MathTransform.Transform(pp);
        double[] pExpected = [75.613911283608331, 57.926509119323505];
        double[] pp2 = ct.MathTransform.Inverse().Transform(pg);

        this.Verbose = true;
        Assert.True(
            this.ToleranceLessThan(pg, pExpected, 1e-6),
            this.TransformationError("EPSG 28414 -> EPSG 4284", pExpected, pg));
        Assert.True(
            this.ToleranceLessThan(pp, pp2, 1e-3),
            this.TransformationError("EPSG 28414 -> Pulkovo 1942", pp, pp2, true));
    }

    /// <summary>
    /// Verifies coordinate conversion from WGS84 (EPSG 4326) to Web Mercator (EPSG 3857)
    /// and back, per CodePlex discussion 352813.
    /// </summary>
    [Fact(DisplayName = "Problem converting coordinates, Discussion http://projnet.codeplex.com/discussions/352813")]
    public void TestDiscussion352813()
    {
        GeographicCoordinateSystem csSource = GeographicCoordinateSystem.WGS84;
        ProjectedCoordinateSystem csTarget = ProjectedCoordinateSystem.WebMercator;

        // CoordinateSystemFactory.CreateFromWkt(
        // "PROJCS[\"Popular Visualisation CRS / Mercator\"," +
        //         "GEOGCS[\"Popular Visualisation CRS\"," +
        //                  "DATUM[\"Popular Visualisation Datum\"," +
        //                          "SPHEROID[\"Popular Visualisation Sphere\", 6378137, 298.257223563, " +
        //                          "AUTHORITY[\"EPSG\", \"7030\"]]," +
        // "TOWGS84[0, 0, 0, 0, 0, 0, 0], " + "AUTHORITY[\"EPSG\", \"6055\"]], " +
        //                  "PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]]," +
        //                  "UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9102\"]]," +
        //                  "AXIS[\"E\", EAST]," +
        //                  "AXIS[\"N\", NORTH]," +
        //                  "AUTHORITY[\"EPSG\", \"4055\"]]," +
        //         "PROJECTION[\"Mercator\"]," +
        //         "PARAMETER[\"semi_major\", 6378137]," +
        //         "PARAMETER[\"semi_minor\", 6378137]," +
        //         "PARAMETER[\"scale_factor\", 1]," +
        //         "PARAMETER[\"False_Easting\", 0]," +
        //         "PARAMETER[\"False_Northing\", 0]," +
        //         "PARAMETER[\"Central_Meridian\", 0]," +
        //         "PARAMETER[\"Latitude_of_origin\", 0]," +
        //         "UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]]," +
        //         "AXIS[\"East\", EAST]," +
        // "AXIS[\"North\", NORTH]," +
        // "AUTHORITY[\"EPSG\", \"3857\"]]");

        // "PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],UNIT["metre",1,AUTHORITY["EPSG","9001"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],EXTENSION["PROJ4","+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs"],AUTHORITY["EPSG","3857"],AXIS["X",EAST],AXIS["Y",NORTH]]"
        ICoordinateTransformation ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csSource, csTarget);

        this.Verbose = true;

        double[] pg1 = [23.57892d, 37.94712d];

        // src DotSpatial.Projections
        double[] pExpected = [2624793.3678553337, 4571958.333297424];

        double[] pp = ct.MathTransform.Transform(pg1);
        Console.WriteLine(this.TransformationError("EPSG 4326 -> EPSG 3857", pExpected, pp));

        Assert.True(
            this.ToleranceLessThan(pp, pExpected, 1e-9),
            this.TransformationError("EPSG 4326 -> EPSG 3857", pExpected, pp));

        double[] pg2 = ct.MathTransform.Inverse().Transform(pp);
        Assert.True(
            this.ToleranceLessThan(pg1, pg2, 1e-13),
            this.TransformationError("EPSG 4326 -> EPSG 3857", pg1, pg2, true));
    }

    /// <summary>
    /// Verifies WGS84 to GDA94/MGA zone 50 (EPSG 28350) coordinate accuracy,
    /// per CodePlex discussion 361248.
    /// </summary>
    [Fact(DisplayName = "Concerned about the accuracy, Discussion http://projnet.codeplex.com/discussions/361248")]
    public void TestDiscussion3612481()
    {
        CoordinateSystem csSource = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            Discussion361248Wgs84Wkt);

        CoordinateSystem csTarget = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            "PROJCS[\"GDA94 / MGA zone 50\",GEOGCS[\"GDA94\",DATUM[\"Geocentric_Datum_of_Australia_1994\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6283\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4283\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",117],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",10000000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"28350\"]]");

        // Chose PostGis values
        this.Test("WGS 84 -> GDA94 / MGA zone 50", csSource, csTarget, TestDiscussion3612481Input, TestDiscussion3612481Expected, 0.05, 1.0e-4);
    }

    /// <summary>
    /// Verifies WGS84 UTM zone 18N to WGS84 geographic coordinate accuracy,
    /// per CodePlex discussion 361248.
    /// </summary>
    [Fact(DisplayName = "Concerned about the accuracy, Discussion http://projnet.codeplex.com/discussions/361248")]
    public void TestDiscussion3612482()
    {
        var csSource = ProjectedCoordinateSystem.WGS84_UTM(18, true);

        CoordinateSystem csTarget = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            Discussion361248Wgs84Wkt);

        this.Test("WGS84_UTM(18,N) -> WGS84", csSource, csTarget, TestDiscussion3612482Input, TestDiscussion3612482Expected, 1e-6);
    }

    /// <summary>
    /// Wrong <c>null</c> check in ObliqueMercatorProjection.Inverse() method.
    /// </summary>
    /// <seealso href="https://code.google.com/p/nettopologysuite/issues/detail?id=191"/>
    [Fact(DisplayName = "ObliqueMercatorProjection.Inverse() wrong null check")]
    public void TestNtsIssue191()
    {
        var parameters = new List<ProjectionParameter>
        {
            new("latitude_of_center", 45.30916666666666),
            new("longitude_of_center", -86),
            new("azimuth", 337.25556),
            new("rectified_grid_angle", 337.25556),
            new("scale_factor", 0.9996),
            new("false_easting", 2546731.496),
            new("false_northing", -4354009.816),
        };

        var factory = new CoordinateSystemFactory();
        IProjection projection = factory.CreateProjection("Test Oblique", "oblique_mercator", parameters);
        Assert.NotNull(projection);

        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        ProjectedCoordinateSystem dummy = factory.CreateProjectedCoordinateSystem(
            "dummy pcs",
            wgs84,
            projection,
            LinearUnit.Metre,
            new AxisInfo("X", AxisOrientationEnum.East),
            new AxisInfo("Y", AxisOrientationEnum.North));
        Assert.NotNull(dummy);

        ICoordinateTransformation transform = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84, dummy);
        Assert.NotNull(transform);

        MathTransform mathTransform = transform.MathTransform;
        MathTransform inverse = mathTransform.Inverse();
        Assert.NotNull(inverse);
    }

    /// <summary>
    /// Wrong AngularUnits.EqualParams implementation.
    /// </summary>
    [Fact]
    public void TestAngularUnitsEqualParamsIssue()
    {
        // string sourceWkt = " UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]";
        string wkt = "PROJCS[\"DHDN / Gauss-Kruger zone 3\",GEOGCS[\"DHDN\",DATUM[\"Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],AUTHORITY[\"EPSG\",\"6314\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4314\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",3500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"31467\"]]";

        var pcs1 = this.CoordinateSystemFactory.CreateFromWkt(wkt) as ProjectedCoordinateSystem;

        Assert.NotNull(pcs1);
        Assert.NotNull(pcs1.GeographicCoordinateSystem);
        Assert.NotNull(pcs1.GeographicCoordinateSystem.AngularUnit);

        string savedWkt = pcs1.WKT;
        var pcs2 = this.CoordinateSystemFactory.CreateFromWkt(savedWkt) as ProjectedCoordinateSystem;

        // test AngularUnit parsing via ProjectedCoordinateSystem
        Assert.NotNull(pcs2);
        Assert.NotNull(pcs2.GeographicCoordinateSystem);
        Assert.NotNull(pcs2.GeographicCoordinateSystem.AngularUnit);

        // check equality of angular units via RadiansPerUnit
        Assert.Equal(pcs1.GeographicCoordinateSystem.AngularUnit.RadiansPerUnit, pcs2.GeographicCoordinateSystem.AngularUnit.RadiansPerUnit, 0.0000000000001);

        // check equality of angular units
        Assert.True(pcs1.GeographicCoordinateSystem.AngularUnit.EqualParams(pcs2.GeographicCoordinateSystem.AngularUnit));
    }

    /// <summary>
    /// Verifies that GitHub issue #53 is fixed: a WGS84 to UTM zone 35N forward transformation
    /// followed by the inverse round-trips back to the original coordinates within tolerance.
    /// </summary>
    [GitHubIssue(53)]
    [Fact(DisplayName = "Issue #53, transformation somehow is wrong")]
    public void TestGitHubIssue53()
    {
        GeographicCoordinateSystem csWgs84 = GeographicCoordinateSystem.WGS84;
        var csUtm35N = ProjectedCoordinateSystem.WGS84_UTM(35, true);
        ICoordinateTransformation csTrans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csWgs84, csUtm35N);
        ICoordinateTransformation csTransBack = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csUtm35N, csWgs84);

        double[] point = [42.5, 24.5];
        double[] r = csTrans.MathTransform.Transform(point);
        double[] rBack = csTransBack.MathTransform.Transform(r);

        Assert.Equal(point[0], rBack[0], 1e-5);
        Assert.Equal(point[1], rBack[1], 1e-5);
    }

    /// <summary>
    /// Verifies that GitHub issue #98 is fixed: a compound coordinate system combining a
    /// projected CRS and a vertical CRS can be parsed from WKT and exposes the correct
    /// authority, dimension, and component systems.
    /// </summary>
    [GitHubIssue(98)]
    [Fact(DisplayName = "Issue #98, Coordinate system isn't supported")]
    public void TestGitHubIssue98()
    {
        CoordinateSystem? cs = this.CoordinateSystemFactory.CreateFromWkt(
                @"COMPD_CS[
	                    ""SWEREF99 18 00 + RH2000 height"",
	                    PROJCS[
		                    ""SWEREF99 18 00"",
		                    GEOGCS[
			                    ""SWEREF99"",
			                    DATUM[
				                    ""SWEREF99"",
				                    SPHEROID[
					                    ""GRS 1980"",
					                    6378137,
					                    298.257222101,
					                    AUTHORITY[
						                    ""EPSG"",
						                    ""7019""
					                    ]
				                    ],
				                    TOWGS84[0,0,0,0,0,0,0],
				                    AUTHORITY[""EPSG"",""6619""]
			                    ],
			                    PRIMEM
			                    [
				                    ""Greenwich"",
				                    0,
				                    AUTHORITY[""EPSG"",""8901""]
			                    ],
			                    UNIT[""degree"",0.0174532925199433, AUTHORITY[""EPSG"",""9122""]],
		                        AUTHORITY[""EPSG"",""4619""]
	                        ], 
	                        PROJECTION[""Transverse_Mercator""],
	                        PARAMETER[""latitude_of_origin"",0],
	                        PARAMETER[""central_meridian"",18],
	                        PARAMETER[""scale_factor"",1],
	                        PARAMETER[""false_easting"",150000],
	                        PARAMETER[""false_northing"",0],
	                        UNIT[""metre"",1, AUTHORITY[""EPSG"",""9001""]],
	                        AUTHORITY[""EPSG"",""3011""]
                        ],
	                    VERT_CS[
		                    ""RH2000 height"",
		                    VERT_DATUM[
			                    ""Rikets hojdsystem 2000"",
			                    2005,
			                    AUTHORITY[""EPSG"",""5208""]
		                    ],
		                    UNIT[""metre"",1, AUTHORITY[""EPSG"",""9001""]],
		                    AXIS[""Up"",UP],
		                    AUTHORITY[""EPSG"",""5613""]
	                    ],
	                    AUTHORITY[""EPSG"",""5850""]
                    ]");
        var cmpdCs = cs as CompoundCoordinateSystem;
        Assert.NotNull(cmpdCs);
        Assert.Equal("EPSG", cmpdCs.Authority);
        Assert.Equal(5850, cmpdCs.AuthorityCode);
        Assert.Equal(3, cmpdCs.Dimension);
        Assert.True(cmpdCs.HeadCoordinateSystem is ProjectedCoordinateSystem);
        Assert.True(cmpdCs.TailCoordinateSystem is VerticalCoordinateSystem);
    }

    /// <summary>
    /// Tests if a coordinate system can be created from a Well-Known Text (WKT) representation
    /// and verifies that the authority code is correctly loaded.
    /// </summary>
    /// <remarks>
    /// This test ensures that the WKT parsing functionality of the CoordinateSystemFactory
    /// correctly initializes the coordinate system and its associated metadata, such as the authority code.
    /// </remarks>
    [Fact]
    public void TestAuthorityNotLoadedIssue()
    {
        string wkt = "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs\"],AUTHORITY[\"EPSG\",\"3857\"]]";
        CoordinateSystem? coordinateSystem = this.CoordinateSystemFactory.CreateFromWkt(wkt);
        Assert.NotNull(coordinateSystem);
        Assert.Equal(3857, coordinateSystem.AuthorityCode);
    }
}
