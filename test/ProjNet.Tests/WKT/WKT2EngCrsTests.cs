using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2EngCrsTests
    {
        private const string Wkt2EngCrs_LocalGrid = "ENGCRS[\"Local engineering grid\"," +
                                                   "EDATUM[\"Local datum\",ID[\"EPSG\",1234]]," +
                                                   "CS[cartesian,2]," +
                                                   "AXIS[\"x\",east,ORDER[1]]," +
                                                   "AXIS[\"y\",north,ORDER[2]]," +
                                                   "LENGTHUNIT[\"metre\",1]," +
                                                   "ID[\"LOCAL\",1]," +
                                                   "USAGE[SCOPE[\"unknown\"],AREA[\"Somewhere\"],BBOX[0,0,1,1]]]";

        [Test]
        public void ParseWkt2EngCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2EngCrs_LocalGrid);
            Assert.That(model, Is.InstanceOf<Wkt2EngCrs>());

            var eng = (Wkt2EngCrs)model;
            Assert.That(eng.Datum.Name, Is.EqualTo("Local datum"));
            Assert.That(eng.CoordinateSystem.Dimension, Is.EqualTo(2));

            string wkt2 = eng.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("ENGCRS[\"Local engineering grid\""));
            Assert.That(wkt2, Does.Contain("EDATUM[\"Local datum\""));
            Assert.That(wkt2, Does.Contain("CS[cartesian,2]"));
            Assert.That(wkt2, Does.Contain("ID[\"LOCAL\",\"1\"]"));
        }

        [Test]
        public void EngCrsParserSkipsUnknownMetadata()
        {
            var eng = (Wkt2EngCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2EngCrs_LocalGrid);
            Assert.That(eng.CoordinateSystem.Axes, Has.Count.EqualTo(2));
        }
    }
}
