using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2Rgf93Lambert93Tests
    {
        private const string Wkt2Rgf93_Lambert93 = "PROJCRS[\"RGF93 v1 / Lambert-93\"," +
                                                   "BASEGEOGCRS[\"RGF93 v1\"," +
                                                   "DATUM[\"Reseau Geodesique Francais 1993 v1\"," +
                                                   "ELLIPSOID[\"GRS 1980\",6378137,298.257222101,LENGTHUNIT[\"metre\",1]]]," +
                                                   "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
                                                   "ID[\"EPSG\",4171]]," +
                                                   "CONVERSION[\"Lambert-93\"," +
                                                   "METHOD[\"Lambert Conic Conformal (2SP)\",ID[\"EPSG\",9802]]," +
                                                   "PARAMETER[\"Latitude of false origin\",46.5,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8821]]," +
                                                   "PARAMETER[\"Longitude of false origin\",3,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8822]]," +
                                                   "PARAMETER[\"Latitude of 1st standard parallel\",49,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8823]]," +
                                                   "PARAMETER[\"Latitude of 2nd standard parallel\",44,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8824]]," +
                                                   "PARAMETER[\"Easting at false origin\",700000,LENGTHUNIT[\"metre\",1],ID[\"EPSG\",8826]]," +
                                                   "PARAMETER[\"Northing at false origin\",6600000,LENGTHUNIT[\"metre\",1],ID[\"EPSG\",8827]]]," +
                                                   "CS[Cartesian,2]," +
                                                   "AXIS[\"easting (X)\",east,ORDER[1],LENGTHUNIT[\"metre\",1]]," +
                                                   "AXIS[\"northing (Y)\",north,ORDER[2],LENGTHUNIT[\"metre\",1]]," +
                                                   "USAGE[SCOPE[\"Engineering survey, topographic mapping.\"]," +
                                                   "AREA[\"France - onshore and offshore, mainland and Corsica (France métropolitaine including Corsica).\"]," +
                                                   "BBOX[41.15,-9.86,51.56,10.38]]," +
                                                   "ID[\"EPSG\",2154]]";

        [Test]
        public void ParseWkt2ProjCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2Rgf93_Lambert93);
            Assert.That(model, Is.InstanceOf<Wkt2ProjCrs>());

            string wkt2 = model.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("PROJCRS[\"RGF93 v1 / Lambert-93\""));
            Assert.That(wkt2, Does.Contain("CONVERSION[\"Lambert-93\""));
            Assert.That(wkt2, Does.Contain("METHOD[\"Lambert Conic Conformal (2SP)\"]"));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"2154\"]"));
        }

        [Test]
        public void ParseWkt2ProjCrsToProjNetProducesProjectedCs()
        {
            var cs = new CoordinateSystemFactory().CreateFromWkt(Wkt2Rgf93_Lambert93);
            Assert.That(cs, Is.InstanceOf<ProjectedCoordinateSystem>());

            var pcs = (ProjectedCoordinateSystem)cs;
            Assert.That(pcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(pcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(pcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }

        [Test]
        public void ParseWkt2ProjCrsModelToProjNetUsesWkt2Conversions()
        {
            var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2Rgf93_Lambert93);
            var pcs = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(model);

            Assert.That(pcs, Is.Not.Null);
            Assert.That(pcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(pcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(pcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }
    }
}
