using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNET.Tests.Geometries.Implementation;

namespace ProjNET.Tests.GitHub
{
    [Category("GitHub Issue")]
    public class Issues
    {
        //
        private static CoordinateSystemServices _css = new CoordinateSystemServices(CoordinateSystemServicesTest.LoadCsv());

        [Test(Description = "Issue #10, ConcatenatedTransform.Inverse() method destroys the state of child transformations")]
        public void TestConcatenatedTransformInvert()
        {
            
            var epsg31466 = _css.GetCoordinateSystem(31466);
            var epsg25832 = _css.GetCoordinateSystem(25832);

            var ctFwd = (ConcatenatedTransform)_css.CreateTransformation(epsg31466, epsg25832).MathTransform;
            var ctRev = (ConcatenatedTransform)ctFwd.Inverse();

            var ctlFwd = ctFwd.CoordinateTransformationList;
            var ctlRev = ctRev.CoordinateTransformationList;

            Assert.That(ReferenceEquals(ctlFwd, ctlRev), Is.False);
            Assert.That(ctlFwd.Count, Is.EqualTo(ctlRev.Count));
            for (int i = 0, j = ctlFwd.Count - 1; i < ctlFwd.Count; i++, j--)
                Assert.That(ReferenceEquals(ctlFwd[i], ctlRev[j]), Is.False);
        }

        [Test(Description = "Issue #20, Math transform bug")]
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
                new ProjectionParameter("scale_factor", 1.0000067)
            };

            var itmDatum = coordinateSystemFactory.CreateHorizontalDatum("Isreal 1993", DatumType.HD_Geocentric,
                Ellipsoid.GRS80, new Wgs84ConversionInfo(-24.0024, -17.1032, -17.8444, -0.33077, -1.85269, 1.66969, 5.4248));

            var itmGeo = coordinateSystemFactory.CreateGeographicCoordinateSystem("ITM", AngularUnit.Degrees, itmDatum,
                PrimeMeridian.Greenwich, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

            var itmProjection = coordinateSystemFactory.CreateProjection("Transverse_Mercator", "Transverse_Mercator", itmParameters);
            var itm = coordinateSystemFactory.CreateProjectedCoordinateSystem("ITM", itmGeo, itmProjection, LinearUnit.Metre,
                new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

            var wgs84 = ProjectedCoordinateSystem.WGS84_UTM(36, true).GeographicCoordinateSystem;

            var ctFwd = _css.CreateTransformation(itm, wgs84).MathTransform;
            var pt1a = new Coordinate(200000, 600000);
            var pt2a = ctFwd.Transform(pt1a);
            var pt1b = ctFwd.Inverse().Transform(pt2a);
            var pt2b = ctFwd.Transform(pt1a);

            Assert.That(pt1a.Distance(pt1b), Is.LessThan(0.01));
            Assert.That(pt2a, Is.EqualTo(pt2b));

        }

        [Test]
        public void TestIssuesWith3857To25832()
        {
            var epsg_3857 = ProjectedCoordinateSystem.WebMercator;
            Console.WriteLine(epsg_3857.Projection.ClassName);
            Console.WriteLine(epsg_3857.WKT);

            var epsg25832 = _css.GetCoordinateSystem(25832);

            var mt1 = _css.CreateTransformation(epsg25832, epsg_3857).MathTransform;
            var pt25832 = new Coordinate(702575, 6153153);
            var pt_3857ex = new Coordinate(1358761.89, 7456070.47);

            var pt_3857 = mt1.Transform(pt25832);
            Assert.That(pt_3857.Distance(pt_3857ex), Is.LessThan(0.015));


            epsg_3857 = (ProjectedCoordinateSystem)_css.GetCoordinateSystem(3857);
            Console.WriteLine(epsg_3857.Projection.ClassName);
            Console.WriteLine(epsg_3857.WKT);

            var mt2 = _css.CreateTransformation(epsg25832, epsg_3857).MathTransform;
            pt_3857 = mt2.Transform(pt25832);
            Assert.That(pt_3857.Distance(pt_3857ex), Is.LessThan(0.015));
        }

        [Test, Description("Convert latitude/longitude to Canada grid NAD83 (epsg:26910)")]
        public void TestConvertWgs84ToEPSG26910()
        {
            var epsg26910 = _css.GetCoordinateSystem("EPSG", 26910);
            var epsg_4326 = _css.GetCoordinateSystem("EPSG", 4326);

            double[] ptI = { 3523562.711189, 6246615.391161 };


            var ct = _css.CreateTransformation(epsg26910, epsg_4326);
            var pt1a = ct.MathTransform.Transform(ptI[0], ptI[1]);
            Assert.That(pt1a.x, Is.EqualTo(-82.0479097).Within(0.01), "Longitude");
            Assert.That(pt1a.y, Is.EqualTo(48.4185597).Within(0.01), "Latitude");
            /*
            var pt1b = ct.MathTransform.Inverse().Transform(pt1a);
            Assert.That(pt1b[0], Is.EqualTo(3523562.711189).Within(0.01), "Easting");
            Assert.That(pt1b[1], Is.EqualTo(6246615.391161).Within(0.01), "Northing");
             */
        }

        [Test, Ignore("Requires DotSpatial.Projections, Result same as in TestConvertWgs84ToEPSG26910")]
        public void TestConvertWgs84ToEPSG26910_DS()
        {
            /*
            var epsg26910 = DotSpatial.Projections.ProjectionInfo.FromEpsgCode(26910);
            var epsg_4326 = DotSpatial.Projections.ProjectionInfo.FromEpsgCode(4326);

            var ptI = new double[] { 3523562.711189, 6246615.391161 };

            DotSpatial.Projections.Reproject.ReprojectPoints(ptI, null, epsg26910, epsg_4326, 0, 1);
            Assert.That(ptI[0], Is.EqualTo(-82.0479097).Within(0.01), "Longitude");
            Assert.That(ptI[1], Is.EqualTo(48.4185597).Within(0.01), "Latitude");

            
            DotSpatial.Projections.Reproject.ReprojectPoints(ptI, null, epsg_4326, epsg26910, 0, 1);
            Assert.That(ptI[0], Is.EqualTo(3523562.711189).Within(0.01), "Easting");
            Assert.That(ptI[1], Is.EqualTo(6246615.391161).Within(0.01), "Northing");
             */
        }

        [Test, Description("MathTransform.Transform modifies the original ICoordinateSequence")]
        public void TestMathTransformOnSequence()
        {
            var epsg4326 = _css.GetCoordinateSystem(4326);
            var epsg3857 = _css.GetCoordinateSystem(3857);

            var ct = _css.CreateTransformation(epsg4326, epsg3857);
            var seqIn = new CoordinateArraySequence(new [] {new Coordinate(7.47, 53.48),});
            var seqOut = ct.MathTransform.Transform(seqIn);

            Assert.That(seqOut, Is.Not.Null, "Output sequence null");
            Assert.That(ReferenceEquals(seqIn, seqOut), Is.False, "In- and output sequence same object.");
            Assert.That(seqOut.Count, Is.EqualTo(seqIn.Count), "In- and output sequence have same size");

            for (int i = 0; i < seqIn.Count; i++)
                Assert.That(ReferenceEquals(seqIn.GetCoordinate(i), seqOut.GetCoordinate(i)), Is.False, 
                    "In- and output sequence contain same coordinate objects");
        }
    }
}
