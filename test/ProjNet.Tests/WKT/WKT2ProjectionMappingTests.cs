using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2ProjectionMappingTests
{
    [Test]
    public void MapProjectionMethodName_TransverseMercator()
    {
        string result = Wkt2Conversions.MapProjectionMethodName("Transverse Mercator");
        Assert.That(result, Is.EqualTo("Transverse_Mercator"));
    }

    [Test]
    public void MapProjectionMethodName_LambertConicConformal2SP()
    {
        string result = Wkt2Conversions.MapProjectionMethodName("Lambert Conic Conformal (2SP)");
        Assert.That(result, Is.EqualTo("lambert_conformal_conic_2sp"));
    }

    [Test]
    public void MapProjectionMethodName_AlbersEqualArea()
    {
        string result = Wkt2Conversions.MapProjectionMethodName("Albers Equal Area");
        Assert.That(result, Is.EqualTo("Albers_Conic_Equal_Area"));
    }

    [Test]
    public void MapProjectionMethodName_Mercator_Variant_A()
    {
        string result = Wkt2Conversions.MapProjectionMethodName("Mercator (variant A)");
        Assert.That(result, Is.EqualTo("Mercator_1SP"));
    }

    [Test]
    public void MapProjectionMethodName_UnknownMethod_ReturnsInput()
    {
        string result = Wkt2Conversions.MapProjectionMethodName("Some Unknown Projection");
        Assert.That(result, Is.EqualTo("Some Unknown Projection"));
    }

    [Test]
    public void MapProjNetToWkt2MethodName_TransverseMercator()
    {
        string result = Wkt2Conversions.MapProjNetToWkt2MethodName("Transverse_Mercator");
        Assert.That(result, Is.EqualTo("Transverse Mercator"));
    }

    [Test]
    public void MapProjNetToWkt2MethodName_LambertConicConformal2SP()
    {
        string result = Wkt2Conversions.MapProjNetToWkt2MethodName("lambert_conformal_conic_2sp");
        Assert.That(result, Is.EqualTo("Lambert Conic Conformal (2SP)"));
    }

    [Test]
    public void MapProjNetToWkt2MethodName_UnknownMethod_ReturnsInput()
    {
        string result = Wkt2Conversions.MapProjNetToWkt2MethodName("Unknown_Projection");
        Assert.That(result, Is.EqualTo("Unknown_Projection"));
    }

    [Test]
    public void MapProjectionMethodName_CaseInsensitivity()
    {
        // The dictionaries use StringComparer.OrdinalIgnoreCase, so different casing should match.
        string result = Wkt2Conversions.MapProjectionMethodName("transverse mercator");
        Assert.That(result, Is.EqualTo("Transverse_Mercator"));

        string result2 = Wkt2Conversions.MapProjectionMethodName("TRANSVERSE MERCATOR");
        Assert.That(result2, Is.EqualTo("Transverse_Mercator"));
    }

    [Test]
    public void RoundTrip_MethodNameMapping()
    {
        // For each known WKT2 method name that maps to a unique ProjNet name which maps back,
        // verify the round-trip returns a valid WKT2 name.
        var wkt2ToProjNet = new (string Wkt2, string ProjNet)[]
        {
            ("Transverse Mercator", "Transverse_Mercator"),
            ("Mercator (variant A)", "Mercator_1SP"),
            ("Mercator (variant B)", "Mercator_2SP"),
            ("Lambert Conic Conformal (2SP)", "lambert_conformal_conic_2sp"),
            ("Lambert Conic Conformal (1SP)", "Lambert_Conformal_Conic"),
            ("Lambert Azimuthal Equal Area", "Lambert_Azimuthal_Equal_Area"),
            ("Albers Equal Area", "Albers_Conic_Equal_Area"),
            ("Oblique Stereographic", "Oblique_Stereographic"),
            ("Cassini-Soldner", "Cassini_Soldner"),
            ("Krovak", "Krovak"),
            ("Orthographic", "Orthographic"),
        };

        foreach (var (wkt2Name, projNetName) in wkt2ToProjNet)
        {
            // WKT2 → ProjNet
            string toProjNet = Wkt2Conversions.MapProjectionMethodName(wkt2Name);
            Assert.That(toProjNet, Is.EqualTo(projNetName), $"WKT2→ProjNet failed for '{wkt2Name}'");

            // ProjNet → WKT2 (should produce a valid WKT2 name)
            string backToWkt2 = Wkt2Conversions.MapProjNetToWkt2MethodName(toProjNet);
            Assert.That(backToWkt2, Is.Not.Null.And.Not.Empty, $"ProjNet→WKT2 failed for '{toProjNet}'");

            // The round-trip WKT2 name should map back to the same ProjNet name
            string roundTripped = Wkt2Conversions.MapProjectionMethodName(backToWkt2);
            Assert.That(roundTripped, Is.EqualTo(projNetName), $"Round-trip failed for '{wkt2Name}' → '{projNetName}' → '{backToWkt2}' → '{roundTripped}'");
        }
    }
}
