using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

/// <summary>
/// Verifies that coordinate transformations produce correct results after
/// WKT2 model conversion (ProjNet → WKT2 → ProjNet round-trip) and
/// direct WKT2 GEOGCRS parsing.
/// </summary>
[TestFixture]
public class WKT2TransformAccuracyTests
{
    private static readonly CoordinateTransformationFactory CtFactory = new CoordinateTransformationFactory();

    // WGS 84 Geographic CRS (WKT2)
    private const string Wkt2Wgs84 =
        "GEOGCRS[\"WGS 84\"," +
        "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
        "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
        "CS[ellipsoidal,2]," +
        "AXIS[\"latitude\",north,ORDER[1]]," +
        "AXIS[\"longitude\",east,ORDER[2]]," +
        "ANGLEUNIT[\"degree\",0.0174532925199433]," +
        "ID[\"EPSG\",4326]]";

    // WKT1 Lambert-93 for constructing a ProjNet base PCS
    private const string Wkt1Lambert93 =
        "PROJCS[\"RGF93 / Lambert-93\"," +
        "GEOGCS[\"RGF93\"," +
        "DATUM[\"Reseau_Geodesique_Francais_1993\"," +
        "SPHEROID[\"GRS 1980\",6378137,298.257222101]]," +
        "PRIMEM[\"Greenwich\",0]," +
        "UNIT[\"degree\",0.0174532925199433]]," +
        "PROJECTION[\"Lambert_Conformal_Conic_2SP\"]," +
        "PARAMETER[\"standard_parallel_1\",49]," +
        "PARAMETER[\"standard_parallel_2\",44]," +
        "PARAMETER[\"latitude_of_origin\",46.5]," +
        "PARAMETER[\"central_meridian\",3]," +
        "PARAMETER[\"false_easting\",700000]," +
        "PARAMETER[\"false_northing\",6600000]," +
        "UNIT[\"metre\",1]]";

    [Test]
    public void Wkt2ProjCrs_Utm32N_TransformsCoordinatesCorrectly()
    {
        // Round-trip ProjNet UTM32N through WKT2 model and verify transform
        var original = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var wkt2Model = Wkt2Conversions.FromProjNetProjectedCoordinateSystem(original);
        var pcs = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(wkt2Model);

        var transform = CtFactory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84, pcs);

        // Transform Stuttgart (lon=9.18, lat=48.78) - well within UTM zone 32
        double[] pt = { 9.18, 48.78 };
        var result = transform.MathTransform.Transform(pt);

        Assert.That(result[0], Is.EqualTo(513224).Within(500), "Easting for Stuttgart");
        Assert.That(result[1], Is.EqualTo(5403000).Within(500), "Northing for Stuttgart");
    }

    [Test]
    public void Wkt2ProjCrs_Lambert93_TransformsCoordinatesCorrectly()
    {
        // Create Lambert-93 from WKT1, round-trip through WKT2
        var factory = new CoordinateSystemFactory();
        var lambert93 = (ProjectedCoordinateSystem)factory.CreateFromWkt(Wkt1Lambert93);
        var wkt2Model = Wkt2Conversions.FromProjNetProjectedCoordinateSystem(lambert93);
        var pcs = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(wkt2Model);

        var transform = CtFactory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84, pcs);

        // Transform Paris (lon=2.35, lat=48.86)
        double[] pt = { 2.35, 48.86 };
        var result = transform.MathTransform.Transform(pt);

        Assert.That(result[0], Is.EqualTo(652469).Within(500), "Easting for Paris in Lambert-93");
        Assert.That(result[1], Is.EqualTo(6862035).Within(500), "Northing for Paris in Lambert-93");
    }

    [Test]
    public void Wkt2GeogCrs_ToProjNetGcs_TransformToUtmWorks()
    {
        // Parse WKT2 GEOGCRS directly and use as source for transformation
        var gcsModel = (Wkt2GeogCrs)CoordinateSystemWkt2Reader.ParseCrs(Wkt2Wgs84);
        var gcs = Wkt2Conversions.ToProjNetGeographicCoordinateSystem(gcsModel);

        // Use pre-defined UTM32N as target
        var utm32 = ProjectedCoordinateSystem.WGS84_UTM(32, true);

        var transform = CtFactory.CreateFromCoordinateSystems(gcs, utm32);

        // Transform Frankfurt (lon=8.68, lat=50.11) - within UTM zone 32
        double[] pt = { 8.68, 50.11 };
        var result = transform.MathTransform.Transform(pt);

        Assert.That(result[0], Is.GreaterThan(100000).And.LessThan(900000), "Easting in valid UTM range");
        Assert.That(result[1], Is.GreaterThan(5000000).And.LessThan(6500000), "Northing in valid UTM range");
    }

    [Test]
    public void Wkt2ProjCrs_RoundTrip_TransformResultMatchesOriginal()
    {
        // Create original ProjNet UTM32N
        var original = ProjectedCoordinateSystem.WGS84_UTM(32, true);

        // Convert to WKT2 and back
        var wkt2Model = Wkt2Conversions.FromProjNetProjectedCoordinateSystem(original);
        var roundTripped = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(wkt2Model);

        // Transform same point with both
        var transformOriginal = CtFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, original);
        var transformRoundTripped = CtFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, roundTripped);

        double[] pt1 = { 10.0, 50.0 };
        double[] pt2 = { 10.0, 50.0 };

        var result1 = transformOriginal.MathTransform.Transform(pt1);
        var result2 = transformRoundTripped.MathTransform.Transform(pt2);

        // Results should be very close (within 0.01 meters)
        Assert.That(result2[0], Is.EqualTo(result1[0]).Within(0.01), "Easting matches after round-trip");
        Assert.That(result2[1], Is.EqualTo(result1[1]).Within(0.01), "Northing matches after round-trip");
    }

    [Test]
    public void Wkt2ProjCrs_InverseTransform_RecoversOriginalCoordinates()
    {
        // Round-trip ProjNet UTM32N through WKT2 model
        var original = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var wkt2Model = Wkt2Conversions.FromProjNetProjectedCoordinateSystem(original);
        var pcs = Wkt2Conversions.ToProjNetProjectedCoordinateSystem(wkt2Model);

        var transform = CtFactory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84, pcs);

        // Forward transform
        double[] originalPt = { 9.0, 48.0 };
        var projected = transform.MathTransform.Transform(originalPt);

        // Inverse transform
        var recovered = transform.MathTransform.Inverse().Transform(projected);

        Assert.That(recovered[0], Is.EqualTo(9.0).Within(1e-6), "Longitude recovered");
        Assert.That(recovered[1], Is.EqualTo(48.0).Within(1e-6), "Latitude recovered");
    }
}
