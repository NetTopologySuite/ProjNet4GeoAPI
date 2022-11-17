using System.Reflection;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Services;

namespace ProjNET.Tests
{
    public class DefaultServiceTest: CoordinateTransformTestsBase
    {
        public DefaultServiceTest()
        {
            Verbose = true;
        }

        [Test, Description("Tests initializing default service from the CoordinateSystemServices")] 
        public void TestDefault()
        {
            var css = new CoordinateSystemServices();
            var src = css.GetCoordinateSystem("EPSG", 3857);
            var tgt = css.GetCoordinateSystem(4326);

            Assert.IsNotNull(src);
            Assert.IsNotNull(tgt);

            ICoordinateTransformation transform = null;
            Assert.DoesNotThrow(() => transform = css.CreateTransformation(src, tgt));
            Assert.IsNotNull(transform);
        }

        private string wkt7151 = "PROJCS[\"NAD_1983_Hotine_Oblique_Mercator_Azimuth_Natural_Origin\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"longitude_of_center\",-86.0],PARAMETER[\"latitude_of_center\",45.30916666666666],PARAMETER[\"azimuth\",337.25555999999995],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",2546731.496],PARAMETER[\"false_northing\",-4354009.816],PARAMETER[\"rectified_grid_angle\",337.25555999999995],UNIT[\"m\",1.0]]";

        [Test, Description("Test adding coordinate system")]
        public void TestAddingCStoService()
        {
            var css = new CoordinateSystemServices();
            css.AddCoordinateSystem(123, css.CreateFromWkt(wkt7151));

            var cs = css.GetCoordinateSystem(123);
            Assert.IsNotNull(cs);
        }

        [Test, Description("Count")]
        public void TestCount()
        {
            var css = new CoordinateSystemServices();

            int count = css.Count;
            Assert.AreEqual(count, 2);

            css.AddCoordinateSystem(123, css.CreateFromWkt(wkt7151));

            count = css.Count;
            Assert.AreEqual(count, 3);
        }
    }
}
