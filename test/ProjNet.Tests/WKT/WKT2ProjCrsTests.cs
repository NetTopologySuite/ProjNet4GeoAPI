using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKT2ProjCrsTests
    {
        private const string Wkt2ProjCrs_Utm32N = "PROJCRS[\"WGS 84 / UTM zone 32N\"," +
                                                 "BASEGEOGCRS[\"WGS 84\"," +
                                                 "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
                                                 "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
                                                 "CONVERSION[\"UTM zone 32N\"," +
                                                 "METHOD[\"Transverse Mercator\"]," +
                                                 "PARAMETER[\"Latitude of natural origin\",0]," +
                                                 "PARAMETER[\"Longitude of natural origin\",9]," +
                                                 "PARAMETER[\"Scale factor at natural origin\",0.9996]," +
                                                 "PARAMETER[\"False easting\",500000]," +
                                                 "PARAMETER[\"False northing\",0]]," +
                                                 "CS[cartesian,2]," +
                                                 "AXIS[\"(E)\",east,ORDER[1]]," +
                                                 "AXIS[\"(N)\",north,ORDER[2]]," +
                                                 "LENGTHUNIT[\"metre\",1]," +
                                                 "ID[\"EPSG\",32632]]";

        [Test]
        public void ParseWkt2ProjCrsToModelRoundTripsToWkt2()
        {
            var model = CoordinateSystemWkt2Reader.ParseCrs(Wkt2ProjCrs_Utm32N);
            Assert.That(model, Is.InstanceOf<Wkt2ProjCrs>());

            string wkt2 = model.ToWkt2String();
            Assert.That(wkt2, Does.StartWith("PROJCRS[\"WGS 84 / UTM zone 32N\""));
            Assert.That(wkt2, Does.Contain("CONVERSION[\"UTM zone 32N\""));
            Assert.That(wkt2, Does.Contain("METHOD[\"Transverse Mercator\"]"));
            Assert.That(wkt2, Does.Contain("CS[cartesian,2]"));
            Assert.That(wkt2, Does.Contain("ID[\"EPSG\",\"32632\"]"));
        }

        [Test]
        public void ParseWkt2ProjCrsToProjNetProducesProjectedCs()
        {
            var cs = new CoordinateSystemFactory().CreateFromWkt(Wkt2ProjCrs_Utm32N);
            Assert.That(cs, Is.InstanceOf<ProjectedCoordinateSystem>());

            var pcs = (ProjectedCoordinateSystem)cs;
            Assert.That(pcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(pcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(pcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));

            // Verify projection parameters
            Assert.That(pcs.Projection.ClassName, Does.Contain("Transverse_Mercator").IgnoreCase);
            Assert.That(pcs.LinearUnit.MetersPerUnit, Is.EqualTo(1.0));
            // Verify base geographic CS
            Assert.That(pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.SemiMajorAxis, Is.EqualTo(6378137));
            Assert.That(pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.InverseFlattening, Is.EqualTo(298.257223563));
        }

        [Test]
        public void ParseWkt2ProjCrsModelToProjNetUsesWkt2Conversions()
        {
            var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2ProjCrs_Utm32N);
            var pcs = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(model);

            Assert.That(pcs, Is.Not.Null);
            Assert.That(pcs.AxisInfo, Has.Count.EqualTo(2));
            Assert.That(pcs.AxisInfo[0].Orientation, Is.EqualTo(AxisOrientationEnum.East));
            Assert.That(pcs.AxisInfo[1].Orientation, Is.EqualTo(AxisOrientationEnum.North));
        }

        [Test]
        public void ProjNetProjectedToWkt2ToProjNetRoundTripPreservesCoreParams()
        {
            var original = ProjectedCoordinateSystem.WGS84_UTM(32, true);

            var wkt2Model = Wkt2Conversions.FromProjNetProjectedCoordinateSystem(original);
            var roundTripped = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(wkt2Model);

            Assert.That(roundTripped.GeographicCoordinateSystem.EqualParams(original.GeographicCoordinateSystem), Is.True);
            Assert.That(roundTripped.LinearUnit.EqualParams(original.LinearUnit), Is.True);
            Assert.That(roundTripped.Projection.ClassName, Is.EqualTo(original.Projection.ClassName));
            Assert.That(roundTripped.Projection.NumParameters, Is.EqualTo(original.Projection.NumParameters));

            // Verify projection parameters individually
            for (int i = 0; i < original.Projection.NumParameters && i < roundTripped.Projection.NumParameters; i++)
            {
                var origParam = original.Projection.GetParameter(i);
                var rtParam = roundTripped.Projection.GetParameter(origParam.Name);
                if (rtParam != null)
                    Assert.That(rtParam.Value, Is.EqualTo(origParam.Value).Within(1e-10), $"Parameter '{origParam.Name}' differs");
            }
        }
    }
}
