using System;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNET.Tests
{
    /// <summary>
    /// Inverses of Lambert Azimuthal Equal Area and Orthographic close to the poles, where asin()
    /// of a value close to 1 lost half the digits (centimetres on the Earth). The projected
    /// coordinates are PROJ's forward (cct -d 10) of the given longitude and latitude.
    /// </summary>
    [TestFixture]
    public class AzimuthalPrecisionTests
    {
        private static readonly CoordinateSystemFactory Factory = new CoordinateSystemFactory();
        private static readonly CoordinateTransformationFactory TransformFactory = new CoordinateTransformationFactory();

        private const string Wgs84 = "SPHEROID[\"WGS_1984\",6378137.0,298.257223563]";
        private const string Sphere = "SPHEROID[\"Sphere\",6371000.0,0.0]";

        private static void CheckInverse(string projection, string spheroid, double lat0, double lon0,
            double x, double y, double lon, double lat)
        {
            string geogcs = "GEOGCS[\"GCS\",DATUM[\"D\"," + spheroid + "],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]]";
            var geographic = Factory.CreateFromWkt(geogcs);
            var projected = Factory.CreateFromWkt(
                "PROJCS[\"P\"," + geogcs + ",PROJECTION[\"" + projection + "\"]," +
                "PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0]," +
                $"PARAMETER[\"Central_Meridian\",{lon0}],PARAMETER[\"Latitude_Of_Origin\",{lat0}]," +
                "UNIT[\"Meter\",1.0]]");
            var inverse = TransformFactory.CreateFromCoordinateSystems(projected, geographic).MathTransform;
            double[] geo = inverse.Transform(new[] { x, y });
            string at = $"{projection} lat_0={lat0} at {lon}, {lat}";
            // 1e-9 degrees of latitude is about 0.1 mm
            Assert.That(geo[1], Is.EqualTo(lat).Within(1e-9), at + " latitude");
            // close to the pole the longitude is ill-conditioned: compare its effect on the ground
            Assert.That((geo[0] - lon) * Math.Cos(lat * Math.PI / 180), Is.EqualTo(0).Within(1e-9), at + " longitude");
        }

        [Test]
        public void LambertAzimuthalEqualAreaEllipsoidal()
        {
            CheckInverse("Lambert_Azimuthal_Equal_Area", Wgs84, 52, 10, 5.9112198326, 4159707.1167619219, 40, 89.9999);
            CheckInverse("Lambert_Azimuthal_Equal_Area", Wgs84, 52, 10, -1110.9577298914, 4160077.1237620777, -100, 89.99);
            CheckInverse("Lambert_Azimuthal_Equal_Area", Wgs84, 90, 0, 5.5846989779, -9.6729823747, 30, 89.9999);
            CheckInverse("Lambert_Azimuthal_Equal_Area", Wgs84, 52, 10, 0.5911221239, 4159715.3415663159, 40, 89.99999);
            CheckInverse("Lambert_Azimuthal_Equal_Area", Wgs84, 52, 10, 0.0059112217, 4159716.2462945590, 40, 89.9999999);
            CheckInverse("Lambert_Azimuthal_Equal_Area", Wgs84, 90, 0, 0.0055846991, -0.0096729826, 30, 89.9999999);
        }

        [Test]
        public void LambertAzimuthalEqualAreaSpherical()
        {
            CheckInverse("Lambert_Azimuthal_Equal_Area", Sphere, 52, 10, 5.8801011732, 4148380.3189707603, 40, 89.9999);
        }

        [Test]
        public void OrthographicSpherical()
        {
            CheckInverse("Orthographic", Sphere, 55, 5, 6.3778789768, 3654248.0146953794, 40, 89.9999);
            CheckInverse("Orthographic", Sphere, 90, 0, 5.5597463325, -9.6297631250, 30, 89.9999);
            CheckInverse("Orthographic", Sphere, 55, 5, 0.0063778791, 3654255.4685312235, 40, 89.9999999);
            CheckInverse("Orthographic", Sphere, 90, 0, 0.0055597464, -0.0096297633, 30, 89.9999999);
        }
    }
}
