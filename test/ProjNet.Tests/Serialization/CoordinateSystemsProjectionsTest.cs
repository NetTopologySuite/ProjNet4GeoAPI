using NUnit.Framework;
using ProjNet.CoordinateSystems;

namespace ProjNET.Tests.Serialization
{
    public class CoordinateSystemsProjectionsTest : BaseSerializationTest
    {
        [Test]
        public void TestProjectionParameterSet() 
        {
            var ps = new ProjNet.CoordinateSystems.Projections.ProjectionParameterSet(
                new[]
                    {
                        new ProjectionParameter("latitude_of_origin", 0),
                        new ProjectionParameter("false_easting", 500)
                    }
                );

            var psD = SanD(ps, GetFormatter());

            Assert.AreEqual(ps, psD);
        }

        [Test]
        public void CreateTransformationFromCoordinateSystemDeserializedFromWKT()
        {
            var utm17n_original = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WGS84_UTM(17, true);
            string utm17n_wkt = utm17n_original.WKT;

            var utm17n_fromWKT = (ProjNet.CoordinateSystems.ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(utm17n_wkt);
            var wgs84 = ProjNet.CoordinateSystems.GeographicCoordinateSystem.WGS84;

            var coordinateSystemServices = new ProjNet.CoordinateSystemServices();
            Assert.DoesNotThrow(() => coordinateSystemServices.CreateTransformation(utm17n_fromWKT, wgs84));
        }
    }
}
