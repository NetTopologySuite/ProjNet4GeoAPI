using System;
using System.Collections.Generic;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

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
            var epsg_3857 = (ICoordinateSystem)ProjectedCoordinateSystem.WebMercator;
            Console.WriteLine(((IProjectedCoordinateSystem)epsg_3857).Projection.ClassName);
            Console.WriteLine(epsg_3857.WKT);

            var epsg25832 = _css.GetCoordinateSystem(25832);

            var mt1 = _css.CreateTransformation(epsg25832, epsg_3857).MathTransform;
            var pt25832 = new Coordinate(702575, 6153153);
            var pt_3857ex = new Coordinate(1358761.89, 7456070.47);

            var pt_3857 = mt1.Transform(pt25832);
            Assert.That(pt_3857.Distance(pt_3857ex), Is.LessThan(0.015));


            epsg_3857 = _css.GetCoordinateSystem(3857);
            Console.WriteLine(((IProjectedCoordinateSystem)epsg_3857).Projection.ClassName);
            Console.WriteLine(epsg_3857.WKT);

            var mt2 = _css.CreateTransformation(epsg25832, epsg_3857).MathTransform;
            pt_3857 = mt2.Transform(pt25832);
            Assert.That(pt_3857.Distance(pt_3857ex), Is.LessThan(0.015));
        }
    }
}