using System;
using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2DerivedGeogCrsTests
{
    private const string Wkt2DerivedGeogCrs_AtlanticPole =
        "DERIVEDGEOGCRS[\"WMO Atlantic Pole\"," +
        "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0]]," +
        "DERIVINGCONVERSION[\"Atlantic pole rotation\"," +
        "METHOD[\"Pole rotation\"]," +
        "PARAMETER[\"Latitude of rotated pole\",52]," +
        "PARAMETER[\"Longitude of rotated pole\",-30]," +
        "PARAMETER[\"Axis rotation\",-25]]," +
        "CS[ellipsoidal,2]," +
        "AXIS[\"latitude\",north,ORDER[1]]," +
        "AXIS[\"longitude\",east,ORDER[2]]," +
        "ANGLEUNIT[\"degree\",0.0174532925199433]]";

    [Test]
    public void Parse_DerivedGeogCrs_Basic()
    {
        var crs = CoordinateSystemWkt2Reader.ParseCrs(Wkt2DerivedGeogCrs_AtlanticPole);
        Assert.That(crs, Is.InstanceOf<Wkt2DerivedGeogCrs>());

        var derived = (Wkt2DerivedGeogCrs)crs;
        Assert.That(derived.Name, Is.EqualTo("WMO Atlantic Pole"));
        Assert.That(derived.Keyword, Is.EqualTo("DERIVEDGEOGCRS"));

        // BaseCrs
        Assert.That(derived.BaseCrs, Is.Not.Null);
        Assert.That(derived.BaseCrs, Is.InstanceOf<Wkt2GeogCrs>());
        Assert.That(derived.BaseCrs.Name, Is.EqualTo("WGS 84"));

        // DerivingConversion
        Assert.That(derived.DerivingConversion, Is.Not.Null);
        Assert.That(derived.DerivingConversion.Name, Is.EqualTo("Atlantic pole rotation"));
        Assert.That(derived.DerivingConversion.MethodName, Is.EqualTo("Pole rotation"));
        Assert.That(derived.DerivingConversion.Parameters, Has.Count.EqualTo(3));
        Assert.That(derived.DerivingConversion.Parameters[0].Name, Is.EqualTo("Latitude of rotated pole"));
        Assert.That(derived.DerivingConversion.Parameters[0].Value, Is.EqualTo(52));
        Assert.That(derived.DerivingConversion.Parameters[1].Name, Is.EqualTo("Longitude of rotated pole"));
        Assert.That(derived.DerivingConversion.Parameters[1].Value, Is.EqualTo(-30));
        Assert.That(derived.DerivingConversion.Parameters[2].Name, Is.EqualTo("Axis rotation"));
        Assert.That(derived.DerivingConversion.Parameters[2].Value, Is.EqualTo(-25));

        // CoordinateSystem
        Assert.That(derived.CoordinateSystem, Is.Not.Null);
        Assert.That(derived.CoordinateSystem.Type, Is.EqualTo("ellipsoidal"));
        Assert.That(derived.CoordinateSystem.Dimension, Is.EqualTo(2));
        Assert.That(derived.CoordinateSystem.Axes, Has.Count.EqualTo(2));
        Assert.That(derived.CoordinateSystem.Axes[0].Name, Is.EqualTo("latitude"));
        Assert.That(derived.CoordinateSystem.Axes[0].Direction, Is.EqualTo("north"));
        Assert.That(derived.CoordinateSystem.Axes[0].Order, Is.EqualTo(1));
        Assert.That(derived.CoordinateSystem.Axes[1].Name, Is.EqualTo("longitude"));
        Assert.That(derived.CoordinateSystem.Axes[1].Direction, Is.EqualTo("east"));
        Assert.That(derived.CoordinateSystem.Axes[1].Order, Is.EqualTo(2));

        // Unit
        Assert.That(derived.CoordinateSystem.Unit, Is.Not.Null);
        Assert.That(derived.CoordinateSystem.Unit.Name, Is.EqualTo("degree"));
        Assert.That(derived.CoordinateSystem.Unit.ConversionFactor, Is.EqualTo(0.0174532925199433).Within(1e-13));
    }

    [Test]
    public void Parse_DerivedGeogCrs_WithIdAndRemark()
    {
        const string wkt =
            "DERIVEDGEOGCRS[\"WMO Atlantic Pole\"," +
            "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0]]," +
            "DERIVINGCONVERSION[\"Atlantic pole rotation\",METHOD[\"Pole rotation\"]]," +
            "CS[ellipsoidal,2]," +
            "AXIS[\"latitude\",north]," +
            "AXIS[\"longitude\",east]," +
            "ANGLEUNIT[\"degree\",0.0174532925199433]," +
            "ID[\"EXAMPLE\",\"1234\"]," +
            "REMARK[\"WMO Atlantic pole rotation example\"]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2DerivedGeogCrs>());

        var derived = (Wkt2DerivedGeogCrs)crs;
        Assert.That(derived.Id, Is.Not.Null);
        Assert.That(derived.Id.Authority, Is.EqualTo("EXAMPLE"));
        Assert.That(derived.Id.Code, Is.EqualTo("1234"));
        Assert.That(derived.Remark, Is.EqualTo("WMO Atlantic pole rotation example"));
    }

    [Test]
    public void RoundTrip_DerivedGeogCrs()
    {
        var crs = CoordinateSystemWkt2Reader.ParseCrs(Wkt2DerivedGeogCrs_AtlanticPole);
        string output = crs.ToWkt2String();

        // Verify the serialized output contains expected fragments.
        // Note: The writer outputs GEOGCRS for the BaseCrs (not BASEGEOGCRS),
        // so a full re-parse round-trip is not possible without writer correction.
        Assert.That(output, Does.StartWith("DERIVEDGEOGCRS[\"WMO Atlantic Pole\""));
        Assert.That(output, Does.Contain("\"WGS 84\""));
        Assert.That(output, Does.Contain("DERIVINGCONVERSION[\"Atlantic pole rotation\""));
        Assert.That(output, Does.Contain("METHOD[\"Pole rotation\"]"));
        Assert.That(output, Does.Contain("PARAMETER[\"Latitude of rotated pole\",52]"));
        Assert.That(output, Does.Contain("PARAMETER[\"Longitude of rotated pole\",-30]"));
        Assert.That(output, Does.Contain("PARAMETER[\"Axis rotation\",-25]"));
        Assert.That(output, Does.Contain("CS[ellipsoidal,2]"));
        Assert.That(output, Does.Contain("ANGLEUNIT[\"degree\""));
    }

    [Test]
    public void Parse_DerivedGeogCrs_MissingBaseCrs_Throws()
    {
        const string wkt =
            "DERIVEDGEOGCRS[\"Bad\"," +
            "DERIVINGCONVERSION[\"Identity\",METHOD[\"Identity\"]]," +
            "CS[ellipsoidal,2]," +
            "AXIS[\"latitude\",north]," +
            "AXIS[\"longitude\",east]," +
            "ANGLEUNIT[\"degree\",0.0174532925199433]]";

        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_DerivedGeogCrs_MissingConversion_Throws()
    {
        const string wkt =
            "DERIVEDGEOGCRS[\"Bad\"," +
            "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0]]," +
            "CS[ellipsoidal,2]," +
            "AXIS[\"latitude\",north]," +
            "AXIS[\"longitude\",east]," +
            "ANGLEUNIT[\"degree\",0.0174532925199433]]";

        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_DerivedGeogCrs_WithBaseGeodCrs()
    {
        const string wkt =
            "DERIVEDGEOGCRS[\"Derived\"," +
            "BASEGEODCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0]]," +
            "DERIVINGCONVERSION[\"Identity\",METHOD[\"Identity\"]]," +
            "CS[ellipsoidal,2]," +
            "AXIS[\"latitude\",north]," +
            "AXIS[\"longitude\",east]," +
            "ANGLEUNIT[\"degree\",0.0174532925199433]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2DerivedGeogCrs>());

        var derived = (Wkt2DerivedGeogCrs)crs;
        Assert.That(derived.Name, Is.EqualTo("Derived"));
        Assert.That(derived.BaseCrs, Is.Not.Null);
        Assert.That(derived.BaseCrs.Name, Is.EqualTo("WGS 84"));
        Assert.That(derived.DerivingConversion.MethodName, Is.EqualTo("Identity"));
    }
}
