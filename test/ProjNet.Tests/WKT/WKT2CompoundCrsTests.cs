using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2CompoundCrsTests
    {
        private const string Wkt2CompoundCrs_Wgs84_Height = "COMPOUNDCRS[\"WGS 84 + height\"," +
                                                           "GEOGCRS[\"WGS 84\"," +
                                                           "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
                                                           "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
                                                           "CS[ellipsoidal,2]," +
                                                           "AXIS[\"longitude\",east,ORDER[1]]," +
                                                           "AXIS[\"latitude\",north,ORDER[2]]," +
                                                           "ANGLEUNIT[\"degree\",0.0174532925199433]]," +
                                                           "VERTCRS[\"EGM96 height\"," +
                                                           "VDATUM[\"EGM96 geoid\"]," +
                                                           "CS[vertical,1]," +
                                                           "AXIS[\"gravity-related height (H)\",up,ORDER[1]]," +
                                                           "LENGTHUNIT[\"metre\",1]]," +
                                                           "ID[\"EPSG\",4979]," +
                                                           "USAGE[SCOPE[\"unknown\"],AREA[\"World\"],BBOX[-90,-180,90,180]]]";

        [Test]
        public void ParseWkt2CompoundCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2CompoundCrs_Wgs84_Height);
            Assert.That(model, Is.InstanceOf<Wkt2CompoundCrs>());

            var compound = (Wkt2CompoundCrs)model;
            Assert.That(compound.Components, Has.Count.EqualTo(2));
            Assert.That(compound.Components[0], Is.InstanceOf<Wkt2GeogCrs>());
            Assert.That(compound.Components[1], Is.InstanceOf<Wkt2VertCrs>());

            string wkt2 = compound.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("COMPOUNDCRS[\"WGS 84 + height\""));
            Assert.That(wkt2, Does.Contain("GEOGCRS[\"WGS 84\""));
            Assert.That(wkt2, Does.Contain("VERTCRS[\"EGM96 height\""));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"4979\"]"));
        }

        [Test]
        public void CompoundCrsParserSkipsUnknownMetadata()
        {
            var compound = (Wkt2CompoundCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2CompoundCrs_Wgs84_Height);
            Assert.That(compound.Components.Count, Is.EqualTo(2));
        }
    }
}
