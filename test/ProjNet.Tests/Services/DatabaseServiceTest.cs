using System.Reflection;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Services;

namespace ProjNET.Tests
{
    public class DatabseServiceTest: CoordinateTransformTestsBase
    {
        public DatabseServiceTest()
        {
            Verbose = true;
        }

        string wkt2236 = "PROJCS[\"NAD83 / Florida East (ftUS)\", GEOGCS [ \"NAD83\", DATUM [\"North American Datum 1983 (EPSG ID 6269)\", SPHEROID [\"GRS 1980 (EPSG ID 7019)\", 6378137, 298.257222101]], PRIMEM [ \"Greenwich\", 0.000000 ], UNIT [\"Decimal Degree\", 0.01745329251994328]], PROJECTION [\"SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)\"], PARAMETER [\"Latitude_Of_Origin\", 24.33333333333333333333333333333333333333], PARAMETER [\"Central_Meridian\", -80.9999999999999999999999999999999999999], PARAMETER [\"Scale_Factor\", 0.999941177], PARAMETER [\"False_Easting\", 656166.6669999999999999999999999999999999], PARAMETER [\"False_Northing\", 0], UNIT [\"U.S. Foot\", 0.3048006096012192024384048768097536195072]]";


        [Test, Description("Tests initializing a database service")] 
        public void TestDefault()
        {
            var css = new DatabaseCoordinateService();
            var src = css.GetCoordinateSystem("EPSG", 3857);
            var tgt = css.GetCoordinateSystem(4326);

            Assert.IsNotNull(src);
            Assert.IsNotNull(tgt);

            CoordinateSystem cs = null;
            Assert.DoesNotThrow(() => cs = css.CsFactory.CreateFromWkt(wkt2236));
            Assert.IsNotNull(cs);
        }

        private string wkt7151 = "PROJCS[\"NAD_1983_Hotine_Oblique_Mercator_Azimuth_Natural_Origin\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"longitude_of_center\",-86.0],PARAMETER[\"latitude_of_center\",45.30916666666666],PARAMETER[\"azimuth\",337.25555999999995],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",2546731.496],PARAMETER[\"false_northing\",-4354009.816],PARAMETER[\"rectified_grid_angle\",337.25555999999995],UNIT[\"m\",1.0]]";


        [Test, Description("Test adding coordinate system")]
        public void TestAddingCStoService()
        {
            var css = new DatabaseCoordinateService();
            css.AddCoordinateSystem(123, css.CsFactory.CreateFromWkt(wkt7151));

            var cs = css.GetCoordinateSystem(123);
            Assert.IsNotNull(cs);

            css.RemoveCoordinateSystem(123);

            cs = css.GetCoordinateSystem(123);
            Assert.IsNull(cs);
        }

        [Test, Description("Test removing coordinate system")]
        public void TestDeleteCS()
        {
            var css = new DatabaseCoordinateService();
            css.RemoveCoordinateSystem(123);

            var cs = css.GetCoordinateSystem(123);
            Assert.IsNull(cs);
        }

        [Test, Description("Count")]
        public void TestCount()
        {
            var css = new DatabaseCoordinateService();

            int count = css.Count;
            Assert.IsTrue(count > 2);

            css.AddCoordinateSystem(123, css.CsFactory.CreateFromWkt(wkt7151));

            int newCount = css.Count;
            Assert.IsTrue(newCount == count + 1);

            //cleanup
            css.RemoveCoordinateSystem(123);
        }
    }
}
