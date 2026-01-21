using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2RoundTripTests
    {
        private const string Wkt2Wgs84_3D_Epsg4329 = "GEOGCRS[\"WGS 84 (3D)\"," +
                                                     "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
                                                     "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
                                                     "CS[ellipsoidal,3]," +
                                                     "AXIS[\"geodetic latitude (Lat)\",north,ORDER[1]]," +
                                                     "AXIS[\"geodetic longitude (Long)\",east,ORDER[2]]," +
                                                     "AXIS[\"ellipsoidal height (h)\",up,ORDER[3],LENGTHUNIT[\"metre\",1]]," +
                                                     "ANGLEUNIT[\"degree\",0.0174532925199433]," +
                                                     "ID[\"EPSG\",4329]]";

        [Test]
        public void ParseWkt2ToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2Wgs84_3D_Epsg4329);
            Assert.That(model, Is.InstanceOf<Wkt2GeogCrs>());

            string wkt2 = model.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("GEOGCRS[\"WGS 84 (3D)\""));
            Assert.That(wkt2, Does.Contain("CS[ellipsoidal,3]"));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"4329\"]"));
        }

        [Test]
        public void ParseWkt2ToProjNetNormalizesToLonLat2D()
        {
            var cs = new CoordinateSystemFactory().CreateFromWkt(Wkt2Wgs84_3D_Epsg4329);
            Assert.That(cs, Is.InstanceOf<GeographicCoordinateSystem>());

            var gcs = (GeographicCoordinateSystem)cs;
            Assert.That(gcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(gcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(gcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }

        [Test]
        public void ParseWkt2ModelToProjNetUsesWkt2ConversionsNormalization()
        {
            var model = (Wkt2GeogCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2Wgs84_3D_Epsg4329);
            var gcs = Wkt2Conversions.ToProjNetGeographicCoordinateSystem(model);

            Assert.That(gcs, Is.Not.Null);
            Assert.That(gcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(gcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(gcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }

        [Test]
        public void ProjNetToWkt2ModelWritesWkt2()
        {
            var gcs = GeographicCoordinateSystem.WGS84;
            var model = Wkt2Conversions.FromProjNetGeographicCoordinateSystem(gcs);

            string wkt2 = model.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("GEOGCRS[\"WGS 84\""));
            Assert.That(wkt2, Does.Contain("CS[ellipsoidal,2]"));
        }

        [Test]
        public void ProjNetToWkt2ToProjNetRoundTripPreservesCoreParams()
        {
            var original = GeographicCoordinateSystem.WGS84;

            var wkt2Model = Wkt2Conversions.FromProjNetGeographicCoordinateSystem(original);
            var roundTripped = Wkt2Conversions.ToProjNetGeographicCoordinateSystem(wkt2Model);

            Assert.That(roundTripped.EqualParams(original), Is.True);
        }
    }
}
