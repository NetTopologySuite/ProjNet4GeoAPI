using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2VertCrsTests
    {
        private const string Wkt2VertCrs_Egm96 = "VERTCRS[\"EGM96 height\"," +
                                                "VDATUM[\"EGM96 geoid\",ID[\"EPSG\",5171]]," +
                                                "CS[vertical,1]," +
                                                "AXIS[\"gravity-related height (H)\",up,ORDER[1]]," +
                                                "LENGTHUNIT[\"metre\",1]," +
                                                "ID[\"EPSG\",5773]," +
                                                "USAGE[SCOPE[\"unknown\"],AREA[\"World\"],BBOX[-90,-180,90,180]]]";

        [Test]
        public void ParseWkt2VertCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2VertCrs_Egm96);
            Assert.That(model, Is.InstanceOf<Wkt2VertCrs>());

            string wkt2 = model.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("VERTCRS[\"EGM96 height\""));
            Assert.That(wkt2, Does.Contain("CS[vertical,1]"));
            Assert.That(wkt2, Does.Contain("AXIS[\"gravity-related height (H)\",up,ORDER[1]]"));
            Assert.That(wkt2, Does.Contain("LENGTHUNIT[\"metre\",1"));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"5773\"]"));
        }

        [Test]
        public void VertCrsParserSkipsUnknownMetadata()
        {
            var model = (Wkt2VertCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2VertCrs_Egm96);
            Assert.That(model.Datum.Name, Is.EqualTo("EGM96 geoid"));
            Assert.That(model.CoordinateSystem.Dimension, Is.EqualTo(1));
        }
    }
}
