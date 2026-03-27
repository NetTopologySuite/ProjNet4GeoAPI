using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2Nad83NewJerseyFtUsTests
    {
        private const string Wkt2Nad83_NewJersey_FtUs = "PROJCRS[\"NAD83 / New Jersey (ftUS)\"," +
                                                        "BASEGEODCRS[\"NAD83\"," +
                                                        "DATUM[\"North American Datum 1983\"," +
                                                        "ELLIPSOID[\"GRS 1980\",6378137,298.257222101,LENGTHUNIT[\"metre\",1]]]," +
                                                        "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
                                                        "CONVERSION[\"SPCS83 New Jersey zone (US Survey feet)\"," +
                                                        "METHOD[\"Transverse Mercator\",ID[\"EPSG\",9807]]," +
                                                        "PARAMETER[\"Latitude of natural origin\",38.8333333333333,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8801]]," +
                                                        "PARAMETER[\"Longitude of natural origin\",-74.5,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8802]]," +
                                                        "PARAMETER[\"Scale factor at natural origin\",0.9999,SCALEUNIT[\"unity\",1],ID[\"EPSG\",8805]]," +
                                                        "PARAMETER[\"False easting\",492125,LENGTHUNIT[\"US survey foot\",0.304800609601219],ID[\"EPSG\",8806]]," +
                                                        "PARAMETER[\"False northing\",0,LENGTHUNIT[\"US survey foot\",0.304800609601219],ID[\"EPSG\",8807]]]," +
                                                        "CS[Cartesian,2]," +
                                                        "AXIS[\"easting (X)\",east,ORDER[1],LENGTHUNIT[\"US survey foot\",0.304800609601219]]," +
                                                        "AXIS[\"northing (Y)\",north,ORDER[2],LENGTHUNIT[\"US survey foot\",0.304800609601219]]," +
                                                        "AREA[\"USA - New Jersey\"]," +
                                                        "BBOX[38.87,-75.6,41.36,-73.88]," +
                                                        "ID[\"EPSG\",3424]]";

        [Test]
        public void ParseWkt2ProjCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2Nad83_NewJersey_FtUs);
            Assert.That(model, Is.InstanceOf<Wkt2ProjCrs>());

            string wkt2 = model.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("PROJCRS[\"NAD83 / New Jersey (ftUS)\""));
            Assert.That(wkt2, Does.Contain("CONVERSION[\"SPCS83 New Jersey zone (US Survey feet)\""));
            Assert.That(wkt2, Does.Contain("METHOD[\"Transverse Mercator\"]"));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"3424\"]"));
        }

        [Test]
        public void ParseWkt2ProjCrsToProjNetProducesProjectedCs()
        {
            var cs = new CoordinateSystemFactory().CreateFromWkt(Wkt2Nad83_NewJersey_FtUs);
            Assert.That(cs, Is.InstanceOf<ProjectedCoordinateSystem>());

            var pcs = (ProjectedCoordinateSystem)cs;
            Assert.That(pcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(pcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(pcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }

        [Test]
        public void ParseWkt2ProjCrsModelToProjNetUsesWkt2Conversions()
        {
            var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2Nad83_NewJersey_FtUs);
            var pcs = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(model);

            Assert.That(pcs, Is.Not.Null);
            Assert.That(pcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(pcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(pcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }
    }
}
