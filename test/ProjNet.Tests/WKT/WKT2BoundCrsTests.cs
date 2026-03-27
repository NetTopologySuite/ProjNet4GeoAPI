using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2BoundCrsTests
    {
        private const string Wkt2BoundCrs_Etrs89_ToWgs84 = "BOUNDCRS[\"ETRS89 (bound)\"," +
                                                           "SOURCECRS[GEOGCRS[\"ETRS89\"," +
                                                           "DATUM[\"European Terrestrial Reference System 1989\",ELLIPSOID[\"GRS 1980\",6378137,298.257222101,LENGTHUNIT[\"metre\",1]]]," +
                                                           "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
                                                           "CS[ellipsoidal,2]," +
                                                           "AXIS[\"longitude\",east,ORDER[1]]," +
                                                           "AXIS[\"latitude\",north,ORDER[2]]," +
                                                           "ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
                                                           "TARGETCRS[GEOGCRS[\"WGS 84\"," +
                                                           "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
                                                           "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
                                                           "CS[ellipsoidal,2]," +
                                                           "AXIS[\"longitude\",east,ORDER[1]]," +
                                                           "AXIS[\"latitude\",north,ORDER[2]]," +
                                                           "ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
                                                           "ABRIDGEDTRANSFORMATION[\"ETRS89 to WGS 84\"," +
                                                           "METHOD[\"Geocentric translations\"]," +
                                                           "PARAMETER[\"X-axis translation\",0]," +
                                                           "PARAMETER[\"Y-axis translation\",0]," +
                                                           "PARAMETER[\"Z-axis translation\",0]]," +
                                                           "ID[\"EPSG\",4937]," +
                                                           "USAGE[SCOPE[\"unknown\"],AREA[\"Europe\"],BBOX[34,-10,72,40]]]";

        [Test]
        public void ParseWkt2BoundCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2BoundCrs_Etrs89_ToWgs84);
            Assert.That(model, Is.InstanceOf<Wkt2BoundCrs>());

            var bound = (Wkt2BoundCrs)model;
            Assert.That(bound.SourceCrs, Is.InstanceOf<Wkt2GeogCrs>());
            Assert.That(bound.TargetCrs, Is.InstanceOf<Wkt2GeogCrs>());
            Assert.That(bound.Transformation.MethodName, Is.EqualTo("Geocentric translations"));
            Assert.That(bound.Transformation.Parameters, Has.Count.EqualTo(3));

            string wkt2 = bound.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("BOUNDCRS[\"ETRS89 (bound)\""));
            Assert.That(wkt2, Does.Contain("SOURCECRS[GEOGCRS[\"ETRS89\""));
            Assert.That(wkt2, Does.Contain("TARGETCRS[GEOGCRS[\"WGS 84\""));
            Assert.That(wkt2, Does.Contain("ABRIDGEDTRANSFORMATION[\"ETRS89 to WGS 84\""));
            Assert.That(wkt2, Does.Contain("METHOD[\"Geocentric translations\"]"));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"4937\"]"));
        }

        [Test]
        public void BoundCrsParserSkipsUnknownMetadata()
        {
            var bound = (Wkt2BoundCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2BoundCrs_Etrs89_ToWgs84);
            Assert.That(bound.Id.Authority, Is.EqualTo("EPSG"));
            Assert.That(bound.Id.Code, Is.EqualTo("4937"));
        }
    }
}
