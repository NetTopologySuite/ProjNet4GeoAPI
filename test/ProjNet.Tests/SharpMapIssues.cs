using System.Reflection;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNET.Tests
{
    public class SharpMapIssues: CoordinateTransformTestsBase
    {
        public SharpMapIssues()
        {
            Verbose = true;
        }

        string wkt2236 = "PROJCS[\"NAD83 / Florida East (ftUS)\", GEOGCS [ \"NAD83\", DATUM [\"North American Datum 1983 (EPSG ID 6269)\", SPHEROID [\"GRS 1980 (EPSG ID 7019)\", 6378137, 298.257222101]], PRIMEM [ \"Greenwich\", 0.000000 ], UNIT [\"Decimal Degree\", 0.01745329251994328]], PROJECTION [\"SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)\"], PARAMETER [\"Latitude_Of_Origin\", 24.33333333333333333333333333333333333333], PARAMETER [\"Central_Meridian\", -80.9999999999999999999999999999999999999], PARAMETER [\"Scale_Factor\", 0.999941177], PARAMETER [\"False_Easting\", 656166.6669999999999999999999999999999999], PARAMETER [\"False_Northing\", 0], UNIT [\"U.S. Foot\", 0.3048006096012192024384048768097536195072]]";
        string wkt8307 = "GEOGCS [ \"WGS 84\", DATUM [\"World Geodetic System 1984 (EPSG ID 6326)\", SPHEROID [\"WGS 84 (EPSG ID 7030)\", 6378137, 298.257223563]], PRIMEM [ \"Greenwich\", 0.000000 ], UNIT [\"Decimal Degree\", 0.01745329251994328]]";

        [Test, Description("NAD83 (State Plane) projection to the WGS84 (Lat/Long), http://sharpmap.codeplex.com/discussions/435794")] 
        public void TestNad83ToWGS84()
        {
            var src = CoordinateSystemFactory.CreateFromWkt(wkt2236);
            var tgt = CoordinateSystemFactory.CreateFromWkt(wkt8307);//CoordinateSystems.GeographicCoordinateSystem.WGS84;;

            ProjNet.CoordinateSystems.Projections.ProjectionsRegistry.Register("SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)", 
                ReflectType("ProjNet.CoordinateSystems.Projections.TransverseMercator"));

            ICoordinateTransformation transform = null;
            Assert.DoesNotThrow(() => transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(src, tgt));
            Assert.IsNotNull(transform);
        }

        private System.Type ReflectType(string typeName)
        {
            var asm = Assembly.GetAssembly(typeof (ProjNet.CoordinateSystems.Projections.MapProjection));
            var res = asm.GetType(typeName);
            return res;
        }

        private string wkt7151 = "PROJCS[\"NAD_1983_Hotine_Oblique_Mercator_Azimuth_Natural_Origin\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"longitude_of_center\",-86.0],PARAMETER[\"latitude_of_center\",45.30916666666666],PARAMETER[\"azimuth\",337.25555999999995],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",2546731.496],PARAMETER[\"false_northing\",-4354009.816],PARAMETER[\"rectified_grid_angle\",337.25555999999995],UNIT[\"m\",1.0]]";
        //projection problem with Michigan GeoRef
        [Test, Description("projection problem with Michigan GeoRef")]
        public void TestMichiganGeoRefToWebMercator()
        {
            var src = CoordinateSystemFactory.CreateFromWkt(wkt7151);
            var tgt = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;

            ICoordinateTransformation transform = null;
            Assert.DoesNotThrow(() => transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(src, tgt));
            Assert.IsNotNull(transform);
            double[] ptSrc = new[] {535247.9375, 324548.09375};
            double[] ptTgt = null;
            Assert.DoesNotThrow(() => ptTgt = transform.MathTransform.Transform(ptSrc));
            Assert.IsNotNull(ptTgt);
        }

        [Test, Description("Parse AUTHORITY with unqouted AuthorityCode")]
        public void TestAuthorityCodeParsing()
        {
            const string wkt1 = "PROJCS[\"NAD_1983_BC_Environment_Albers\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Albers\"],PARAMETER[\"False_Easting\",1000000.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",-126.0],PARAMETER[\"Standard_Parallel_1\",50.0],PARAMETER[\"Standard_Parallel_2\",58.5],PARAMETER[\"Latitude_Of_Origin\",45.0],UNIT[\"Meter\",1.0],AUTHORITY[\"EPSG\",\"3005\"]]";
            CoordinateSystem cs1 = null, cs2 = null;
            Assert.DoesNotThrow( () => cs1 = CoordinateSystemFactory.CreateFromWkt(wkt1));
            Assert.IsNotNull(cs1);
            const string wkt2 = "PROJCS[\"NAD_1983_BC_Environment_Albers\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Albers\"],PARAMETER[\"False_Easting\",1000000.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",-126.0],PARAMETER[\"Standard_Parallel_1\",50.0],PARAMETER[\"Standard_Parallel_2\",58.5],PARAMETER[\"Latitude_Of_Origin\",45.0],UNIT[\"Meter\",1.0],AUTHORITY[\"EPSG\",3005]]";
            Assert.DoesNotThrow(() => cs2 = CoordinateSystemFactory.CreateFromWkt(wkt2));
            Assert.IsNotNull(cs2);
            //Assert.AreEqual(cs1, cs2);
            Assert.IsTrue(cs1.EqualParams(cs2));
        }

        [Test]
        public void Test25832To3857()
        {

            const string wkt1 = //"PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"25832\"]]";
                "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],AUTHORITY[\"EPSG\",\"25832\"],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH]]";

            CoordinateSystem cs1 = null, cs2 = null;
            Assert.DoesNotThrow(() => cs1 = CoordinateSystemFactory.CreateFromWkt(wkt1));
            Assert.IsNotNull(cs1);
            const string wkt2 = "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",                  SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"latitude_of_origin\", 0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs\"],AUTHORITY[\"EPSG\",\"3857\"],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";
            Assert.DoesNotThrow(() => cs2 = CoordinateSystemFactory.CreateFromWkt(wkt2));
            Assert.IsNotNull(cs2);

            ICoordinateTransformation ct = null;
            Assert.DoesNotThrow(() => ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(cs1, cs2));
            Assert.IsNotNull(ct);
            Assert.DoesNotThrow(() => ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(cs2, cs1));
            Assert.IsNotNull(ct);
            Assert.DoesNotThrow(() => ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(cs1, ProjectedCoordinateSystem.WebMercator));
            Assert.IsNotNull(ct);
            Assert.DoesNotThrow(() => ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WebMercator, cs1));
            Assert.IsNotNull(ct);
        }

        [Test]
        public void TestLaea()
        {
            const string Epsg3035 =
                @"PROJCS[""ETRS89 / ETRS-LAEA"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.01745329251994328,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],PROJECTION[""Lambert_Azimuthal_Equal_Area""],PARAMETER[""latitude_of_center"",52],PARAMETER[""longitude_of_center"",10],PARAMETER[""false_easting"",4321000],PARAMETER[""false_northing"",3210000],AUTHORITY[""EPSG"",""3035""],AXIS[""X"",EAST],AXIS[""Y"",NORTH]]";

            var csSrc = GeographicCoordinateSystem.WGS84;
            var csTgt = CoordinateSystemFactory.CreateFromWkt(Epsg3035);

            var ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(csSrc, csTgt);

            (double resX, double resY) = ((MathTransform) ct.MathTransform).Transform(16.4, 48.2);
            Assert.That(resX, Is.EqualTo(4796297.431434812).Within(1e-2));
            Assert.That(resY, Is.EqualTo(2807999.1539475969).Within(1e-2));

            (double origX, double origY) = ((MathTransform) ct.MathTransform.Inverse()).Transform(resX, resY);
            Assert.That(origX, Is.EqualTo(16.4).Within(1e-2));
            Assert.That(origY, Is.EqualTo(48.2).Within(1e-2));

        }

    }
}
