using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2ParametricCrsTests
    {
        private const string Wkt2ParametricCrs_Sigma = "PARAMETRICCRS[\"Sigma (dimensionless)\"," +
                                                       "PDATUM[\"Sigma datum\",ID[\"EPSG\",9999]]," +
                                                       "CS[parametric,1]," +
                                                       "AXIS[\"sigma\",up,ORDER[1]]," +
                                                       "PARAMETRICUNIT[\"unity\",1]," +
                                                       "ID[\"LOCAL\",2]," +
                                                       "USAGE[SCOPE[\"unknown\"],AREA[\"Somewhere\"],BBOX[0,0,1,1]]]";

        [Test]
        public void ParseWkt2ParametricCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2ParametricCrs_Sigma);
            Assert.That(model, Is.InstanceOf<Wkt2ParametricCrs>());

            var p = (Wkt2ParametricCrs)model;
            Assert.That(p.Datum.Name, Is.EqualTo("Sigma datum"));
            Assert.That(p.CoordinateSystem.Dimension, Is.EqualTo(1));

            string wkt2 = p.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("PARAMETRICCRS[\"Sigma (dimensionless)\""));
            Assert.That(wkt2, Does.Contain("PDATUM[\"Sigma datum\""));
            Assert.That(wkt2, Does.Contain("CS[parametric,1]"));
            Assert.That(wkt2, Does.Contain("PARAMETRICUNIT[\"unity\",1"));
            Assert.That(wkt2, Does.Contain("ID[\"LOCAL\",\"2\"]"));
        }

        [Test]
        public void ParametricCrsParserSkipsUnknownMetadata()
        {
            var p = (Wkt2ParametricCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2ParametricCrs_Sigma);
            Assert.That(p.CoordinateSystem.Axes, Has.Count.EqualTo(1));
        }
    }
}
