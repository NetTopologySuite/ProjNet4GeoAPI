using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjNet.CoordinateSystems;

namespace ProjNET.Tests
{
    [TestFixture]
    public class ProjNetIssues : CoordinateTransformTestsBase
    {
        public ProjNetIssues()
        {
            Verbose = true;
        }

        [Test, Description("WGS_84UTM to WGS_84 is inaccurate")]
        public void TestIssue23773()
        {
            var csUtm18N = ProjectedCoordinateSystem.WGS84_UTM(18, true);
            var csUtm18NWkt = CoordinateSystemFactory.CreateFromWkt(
                "PROJCS[\"WGS 84 / UTM zone 18N\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-75],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],AUTHORITY[\"EPSG\",\"32618\"],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH]]");
            var csWgs84 = GeographicCoordinateSystem.WGS84;

            var ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(csUtm18N, csWgs84);
            var ct2 = CoordinateTransformationFactory.CreateFromCoordinateSystems(csUtm18NWkt, csWgs84);

            double[] putm = new[] {307821.867d, 4219306.387d};
            double[] pgeo = ct.MathTransform.Transform(putm);
            double[] pgeoWkt = ct2.MathTransform.Transform(putm);
            double[] pExpected = new[] {-77.191769, 38.101147d};

            Assert.IsTrue(ToleranceLessThan(pgeoWkt, pExpected, 0.00001d),
                TransformationError("UTM18N -> WGS84", pExpected, pgeo));
            Assert.IsTrue(ToleranceLessThan(pgeo, pExpected, 0.00001d),
                TransformationError("UTM18N -> WGS84", pExpected, pgeo));
        }

        [Test, Description("Proj.net reprojection problem, Discussion http://projnet.codeplex.com/discussions/351733")]
        public void TestDiscussion351733()
        {
            var csSource = CoordinateSystemFactory.CreateFromWkt(
                "PROJCS[\"Pulkovo 1942 / Gauss-Kruger zone 14\",GEOGCS[\"Pulkovo 1942\",DATUM[\"Pulkovo_1942\",SPHEROID[\"Krassowsky 1940\",6378245,298.3,AUTHORITY[\"EPSG\",\"7024\"]],TOWGS84[23.92,-141.27,-80.9,-0,0.35,0.82,-0.12],AUTHORITY[\"EPSG\",\"6284\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4284\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",81],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",14500000],PARAMETER[\"false_northing\",0],AUTHORITY[\"EPSG\",\"28414\"],AXIS[\"X\",NORTH],AXIS[\"Y\",EAST]]\"");
            var csTarget = CoordinateSystemFactory.CreateFromWkt(
                "GEOGCS[\"Pulkovo 1942\",DATUM[\"Pulkovo_1942\",SPHEROID[\"Krassowsky 1940\",6378245,298.3,AUTHORITY[\"EPSG\",\"7024\"]],TOWGS84[23.92,-141.27,-80.9,-0,0.35,0.82,-0.12],AUTHORITY[\"EPSG\",\"6284\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4284\"]]\"");

            var ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(csSource, csTarget);

            double[] pp = new[] {14181052.913, 6435927.692};
            double[] pg = ct.MathTransform.Transform(pp);
            double[] pExpected = new[] { 75.613911283608331, 57.926509119323505 };
            double[] pp2 = ct.MathTransform.Inverse().Transform(pg);

            Verbose = true;
            Assert.IsTrue(ToleranceLessThan(pg, pExpected, 1e-6),
                TransformationError("EPSG 28414 -> EPSG 4284", pExpected, pg));
            Assert.IsTrue(ToleranceLessThan(pp, pp2, 1e-3),
                TransformationError("EPSG 28414 -> Pulkovo 1942", pp, pp2, true));
        }

        [Test, Description("Problem converting coordinates, Discussion http://projnet.codeplex.com/discussions/352813")]
        public void TestDiscussion352813()
        {
            var csSource = GeographicCoordinateSystem.WGS84;
            var csTarget = ProjectedCoordinateSystem.WebMercator;
                //           CoordinateSystemFactory.CreateFromWkt(
                //"PROJCS[\"Popular Visualisation CRS / Mercator\"," +
                //         "GEOGCS[\"Popular Visualisation CRS\"," +
                //                  "DATUM[\"Popular Visualisation Datum\"," +
                //                          "SPHEROID[\"Popular Visualisation Sphere\", 6378137, 298.257223563, " +
                //                          "AUTHORITY[\"EPSG\", \"7030\"]]," +
                // /*"TOWGS84[0, 0, 0, 0, 0, 0, 0], */"AUTHORITY[\"EPSG\", \"6055\"]], " +
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
                //"AXIS[\"North\", NORTH]," +
                //"AUTHORITY[\"EPSG\", \"3857\"]]");

            //"PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],UNIT["metre",1,AUTHORITY["EPSG","9001"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],EXTENSION["PROJ4","+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs"],AUTHORITY["EPSG","3857"],AXIS["X",EAST],AXIS["Y",NORTH]]"
            var ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(csSource, csTarget);
            //var ct2 = CoordinateTransformationFactory.CreateFromCoordinateSystems(csSource, csTarget2);

            Verbose = true;

            double[] pg1 = new[] { 23.57892d, 37.94712d };
            //src DotSpatial.Projections
            double[] pExpected = new[] { 2624793.3678553337, 4571958.333297424 };

            double[] pp = ct.MathTransform.Transform(pg1);
            Console.WriteLine(TransformationError("EPSG 4326 -> EPSG 3857", pExpected, pp));

            Assert.IsTrue(ToleranceLessThan(pp, pExpected, 1e-9),
                TransformationError("EPSG 4326 -> EPSG 3857", pExpected, pp));

            double[] pg2 = ct.MathTransform.Inverse().Transform(pp);
            Assert.IsTrue(ToleranceLessThan(pg1, pg2, 1e-13),
                TransformationError("EPSG 4326 -> EPSG 3857", pg1, pg2, true));
        }

        [Test, Description("Concerned about the accuracy, Discussion http://projnet.codeplex.com/discussions/361248")]
        public void TestDiscussion361248_1()
        {
            var csSource = CoordinateSystemFactory.CreateFromWkt(
@"GEOGCS[""WGS 84"",
    DATUM[""WGS_1984"",
        SPHEROID[""WGS 84"",6378137,298.257223563,
            AUTHORITY[""EPSG"",""7030""]],
        AUTHORITY[""EPSG"",""6326""]],
    PRIMEM[""Greenwich"",0,
        AUTHORITY[""EPSG"",""8901""]],
    UNIT[""degree"",0.01745329251994328,
        AUTHORITY[""EPSG"",""9122""]],
    AUTHORITY[""EPSG"",""4326""]]");

            var csTarget = CoordinateSystemFactory.CreateFromWkt(
                "PROJCS[\"GDA94 / MGA zone 50\",GEOGCS[\"GDA94\",DATUM[\"Geocentric_Datum_of_Australia_1994\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6283\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4283\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",117],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",10000000],AUTHORITY[\"EPSG\",\"28350\"],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH]]");

            //Chose PostGis values
            Test("WGS 84 -> GDA94 / MGA zone 50", csSource, csTarget, new[] { 136d, -30d }, new[] { 2349315.05731837, 6524249.91789138}, 0.05, 1.0e-4);
        }

        [Test, Description("Concerned about the accuracy, Discussion http://projnet.codeplex.com/discussions/361248")]
        public void TestDiscussion361248_2()
        {
            var csSource = ProjectedCoordinateSystem.WGS84_UTM(18, true);

            var csTarget = CoordinateSystemFactory.CreateFromWkt(
@"GEOGCS[""WGS 84"",
    DATUM[""WGS_1984"",
        SPHEROID[""WGS 84"",6378137,298.257223563,
            AUTHORITY[""EPSG"",""7030""]],
        AUTHORITY[""EPSG"",""6326""]],
    PRIMEM[""Greenwich"",0,
        AUTHORITY[""EPSG"",""8901""]],
    UNIT[""degree"",0.01745329251994328,
        AUTHORITY[""EPSG"",""9122""]],
    AUTHORITY[""EPSG"",""4326""]]");

            Test("WGS84_UTM(18,N) -> WGS84", csSource, csTarget, new[] { 307821.867, 4219306.387 }, new[] { -77.191769, 38.101147 }, 1e-6);
        }

        /// <summary>
        /// Wrong <c>null</c> check in ObliqueMercatorProjection.Inverse() method
        /// </summary>
        /// <seealso href="https://code.google.com/p/nettopologysuite/issues/detail?id=191"/>
        [Test, Description("ObliqueMercatorProjection.Inverse() wrong null check")]
        public void TestNtsIssue191()
        {
            var parameters = new List<ProjectionParameter>();
            parameters.Add(new ProjectionParameter("latitude_of_center", 45.30916666666666));
            parameters.Add(new ProjectionParameter("longitude_of_center", -86));
            parameters.Add(new ProjectionParameter("azimuth", 337.25556));
            parameters.Add(new ProjectionParameter("rectified_grid_angle", 337.25556));
            parameters.Add(new ProjectionParameter("scale_factor", 0.9996));
            parameters.Add(new ProjectionParameter("false_easting", 2546731.496));
            parameters.Add(new ProjectionParameter("false_northing", -4354009.816));

            var factory = new CoordinateSystemFactory();
            var projection = factory.CreateProjection("Test Oblique", "oblique_mercator", parameters);
            Assert.That(projection, Is.Not.Null);

            var wgs84 = GeographicCoordinateSystem.WGS84;
            var dummy = factory.CreateProjectedCoordinateSystem("dummy pcs", 
                wgs84, projection, LinearUnit.Metre, 
                new AxisInfo("X", AxisOrientationEnum.East), 
                new AxisInfo("Y", AxisOrientationEnum.North));
            Assert.That(dummy, Is.Not.Null);

            var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84, dummy);
            Assert.That(transform, Is.Not.Null);

            var mathTransform = transform.MathTransform;
            var inverse = mathTransform.Inverse();
            Assert.That(inverse, Is.Not.Null);
        }

        /// <summary>
        /// Wrong AngularUnits.EqualParams implementation
        /// </summary>
        [Test]
        public void TestAngularUnitsEqualParamsIssue()
        {
            //string sourceWkt = " UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]";

            string wkt = "PROJCS[\"DHDN / Gauss-Kruger zone 3\",GEOGCS[\"DHDN\",DATUM[\"Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],AUTHORITY[\"EPSG\",\"6314\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4314\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",3500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"31467\"]]";

            var pcs1 = CoordinateSystemFactory.CreateFromWkt (wkt) as ProjectedCoordinateSystem;

            Assert.NotNull (pcs1);
            Assert.NotNull (pcs1.GeographicCoordinateSystem);
            Assert.NotNull (pcs1.GeographicCoordinateSystem.AngularUnit);

            string savedWkt = pcs1.WKT;
            var pcs2 = CoordinateSystemFactory.CreateFromWkt (savedWkt) as ProjectedCoordinateSystem;

            //test AngularUnit parsing via ProjectedCoordinateSystem
            Assert.NotNull (pcs2);
            Assert.NotNull (pcs2.GeographicCoordinateSystem);
            Assert.NotNull (pcs2.GeographicCoordinateSystem.AngularUnit);

            //check equality of angular units via RadiansPerUnit
            Assert.AreEqual (pcs1.GeographicCoordinateSystem.AngularUnit.RadiansPerUnit, pcs2.GeographicCoordinateSystem.AngularUnit.RadiansPerUnit, 0.0000000000001);
            //check equality of angular units
            Assert.AreEqual (true, pcs1.GeographicCoordinateSystem.AngularUnit.EqualParams (pcs2.GeographicCoordinateSystem.AngularUnit));
        }

        [Test, Description("transformation somehow is wrong"), Category("Question")]
        public void TestGitHubIssue53()
        {
            // arrange
            var csWgs84 = GeographicCoordinateSystem.WGS84;
            var csUtm35N = ProjectedCoordinateSystem.WGS84_UTM(35, true);
            var csTrans = CoordinateTransformationFactory.CreateFromCoordinateSystems(csWgs84, csUtm35N);
            var csTransBack = CoordinateTransformationFactory.CreateFromCoordinateSystems(csUtm35N, csWgs84);

            // act
            double[] point = { 42.5, 24.5 };
            double[] r = csTrans.MathTransform.Transform(point);
            double[] rBack = csTransBack.MathTransform.Transform(r);

            // assert
            Assert.AreEqual(point[0], rBack[0], 1e-5);
            Assert.AreEqual(point[1], rBack[1], 1e-5);
        }
    }
}
