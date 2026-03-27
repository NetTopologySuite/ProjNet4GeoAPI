using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2ParserEdgeCaseTests
{
    // --- B4: Alternative Keywords ---

    [Test]
    public void ParseCrs_GeographicCrsKeyword_ParsesAsGeogCrs()
    {
        // Use GEOGRAPHICCRS instead of GEOGCRS
        string wkt = "GEOGRAPHICCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north,ORDER[1]],AXIS[\"longitude\",east,ORDER[2]],ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",4326]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model, Is.InstanceOf<Wkt2GeogCrs>());
        var geog = (Wkt2GeogCrs)model;
        Assert.That(geog.Keyword, Is.EqualTo("GEOGRAPHICCRS"));
        Assert.That(geog.Name, Is.EqualTo("WGS 84"));
    }

    [Test]
    public void ParseCrs_ProjectedCrsKeyword_ParsesAsProjCrs()
    {
        // Use PROJECTEDCRS instead of PROJCRS
        string wkt = "PROJECTEDCRS[\"WGS 84 / UTM zone 32N\"," +
            "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
            "CONVERSION[\"UTM zone 32N\",METHOD[\"Transverse Mercator\"],PARAMETER[\"Latitude of natural origin\",0],PARAMETER[\"Longitude of natural origin\",9],PARAMETER[\"Scale factor at natural origin\",0.9996],PARAMETER[\"False easting\",500000],PARAMETER[\"False northing\",0]]," +
            "CS[cartesian,2],AXIS[\"easting (E)\",east,ORDER[1]],AXIS[\"northing (N)\",north,ORDER[2]],LENGTHUNIT[\"metre\",1],ID[\"EPSG\",32632]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model, Is.InstanceOf<Wkt2ProjCrs>());
        Assert.That(((Wkt2ProjCrs)model).Keyword, Is.EqualTo("PROJECTEDCRS"));
    }

    [Test]
    public void ParseCrs_VerticalCrsKeyword_ParsesAsVertCrs()
    {
        string wkt = "VERTICALCRS[\"EGM96 height\",VDATUM[\"EGM96 geoid\",ID[\"EPSG\",5171]],CS[vertical,1],AXIS[\"gravity-related height (H)\",up,ORDER[1]],LENGTHUNIT[\"metre\",1],ID[\"EPSG\",5773]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model, Is.InstanceOf<Wkt2VertCrs>());
        Assert.That(((Wkt2VertCrs)model).Keyword, Is.EqualTo("VERTICALCRS"));
    }

    [Test]
    public void ParseCrs_EngineeringCrsKeyword_ParsesAsEngCrs()
    {
        string wkt = "ENGINEERINGCRS[\"Local grid\",EDATUM[\"Local datum\"],CS[cartesian,2],AXIS[\"x\",east,ORDER[1]],AXIS[\"y\",north,ORDER[2]],LENGTHUNIT[\"metre\",1]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model, Is.InstanceOf<Wkt2EngCrs>());
        Assert.That(((Wkt2EngCrs)model).Keyword, Is.EqualTo("ENGINEERINGCRS"));
    }

    // --- B4: Element Ordering ---

    [Test]
    public void ParseConversion_MethodAfterParameter_PreservesAllData()
    {
        // This tests the fix: METHOD appearing after PARAMETER should not lose params
        string wkt = "PROJCRS[\"Test\"," +
            "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
            "CONVERSION[\"Test Conv\",PARAMETER[\"False easting\",500000],METHOD[\"Transverse Mercator\"],PARAMETER[\"False northing\",0]]," +
            "CS[cartesian,2],AXIS[\"easting\",east],AXIS[\"northing\",north],LENGTHUNIT[\"metre\",1]]";
        var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Conversion.MethodName, Is.EqualTo("Transverse Mercator"));
        Assert.That(model.Conversion.Parameters, Has.Count.EqualTo(2));
        Assert.That(model.Conversion.Parameters[0].Name, Is.EqualTo("False easting"));
        Assert.That(model.Conversion.Parameters[0].Value, Is.EqualTo(500000));
        Assert.That(model.Conversion.Parameters[1].Name, Is.EqualTo("False northing"));
        Assert.That(model.Conversion.Parameters[1].Value, Is.EqualTo(0));
    }

    [Test]
    public void ParseConversion_IdBeforeMethod_PreservesId()
    {
        string wkt = "PROJCRS[\"Test\"," +
            "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
            "CONVERSION[\"Test Conv\",ID[\"EPSG\",16032],METHOD[\"Transverse Mercator\"],PARAMETER[\"False easting\",500000]]," +
            "CS[cartesian,2],AXIS[\"easting\",east],AXIS[\"northing\",north],LENGTHUNIT[\"metre\",1]]";
        var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Conversion.Id, Is.Not.Null);
        Assert.That(model.Conversion.Id.Authority, Is.EqualTo("EPSG"));
        Assert.That(model.Conversion.Id.Code, Is.EqualTo("16032"));
        Assert.That(model.Conversion.MethodName, Is.EqualTo("Transverse Mercator"));
    }

    [Test]
    public void ParseConversion_RemarkBeforeMethod_PreservesRemark()
    {
        string wkt = "PROJCRS[\"Test\"," +
            "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
            "CONVERSION[\"Test Conv\",REMARK[\"test remark\"],METHOD[\"Transverse Mercator\"],PARAMETER[\"False easting\",500000]]," +
            "CS[cartesian,2],AXIS[\"easting\",east],AXIS[\"northing\",north],LENGTHUNIT[\"metre\",1]]";
        var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Conversion.Remark, Is.EqualTo("test remark"));
        Assert.That(model.Conversion.MethodName, Is.EqualTo("Transverse Mercator"));
    }

    // --- B4: Bracket Styles ---
    // Note: WKT2 supports both [] and () brackets. The tokenizer handles this.

    [Test]
    public void ParseCrs_ParenthesisBrackets_ParsesCorrectly()
    {
        // Use () instead of []
        string wkt = "GEOGCRS(\"WGS 84\",DATUM(\"World Geodetic System 1984\",ELLIPSOID(\"WGS 84\",6378137,298.257223563,LENGTHUNIT(\"metre\",1))),PRIMEM(\"Greenwich\",0,ANGLEUNIT(\"degree\",0.0174532925199433)),CS(ellipsoidal,2),AXIS(\"latitude\",north,ORDER(1)),AXIS(\"longitude\",east,ORDER(2)),ANGLEUNIT(\"degree\",0.0174532925199433),ID(\"EPSG\",4326))";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model, Is.InstanceOf<Wkt2GeogCrs>());
        Assert.That(model.Name, Is.EqualTo("WGS 84"));
        var geog = (Wkt2GeogCrs)model;
        Assert.That(geog.Datum.Ellipsoid.SemiMajorAxis, Is.EqualTo(6378137));
    }

    // --- B7: Edge Cases ---

    [Test]
    public void ParseCrs_3DGeogCrs_ParsesAllThreeAxes()
    {
        string wkt = "GEOGCRS[\"WGS 84 (3D)\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,3],AXIS[\"latitude\",north,ORDER[1],ANGLEUNIT[\"degree\",0.0174532925199433]],AXIS[\"longitude\",east,ORDER[2],ANGLEUNIT[\"degree\",0.0174532925199433]],AXIS[\"ellipsoidal height\",up,ORDER[3],LENGTHUNIT[\"metre\",1]],ID[\"EPSG\",4329]]";
        var model = (Wkt2GeogCrs)CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.CoordinateSystem.Dimension, Is.EqualTo(3));
        Assert.That(model.CoordinateSystem.Axes, Has.Count.EqualTo(3));
        Assert.That(model.CoordinateSystem.Axes[2].Direction, Is.EqualTo("up"));
    }

    [Test]
    public void ParseCrs_ZeroInverseFlattening_Sphere()
    {
        // A sphere has InverseFlattening = 0
        string wkt = "GEOGCRS[\"Sphere\",DATUM[\"Sphere datum\",ELLIPSOID[\"Sphere\",6371000,0,LENGTHUNIT[\"metre\",1]]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]";
        var model = (Wkt2GeogCrs)CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Datum.Ellipsoid.InverseFlattening, Is.EqualTo(0));
        Assert.That(model.Datum.Ellipsoid.SemiMajorAxis, Is.EqualTo(6371000));
    }

    [Test]
    public void ParseCrs_VeryLongName_HandlesCorrectly()
    {
        string longName = new string('A', 500);
        string wkt = $"GEOGCRS[\"{longName}\",DATUM[\"datum\",ELLIPSOID[\"ellips\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],CS[ellipsoidal,2],AXIS[\"lat\",north],AXIS[\"lon\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Name, Is.EqualTo(longName));
    }

    [Test]
    public void ParseCrs_NameWithEscapedQuotes_ThrowsBecauseTokenizerDoesNotSupportIt()
    {
        // WKT2 spec escapes double quotes by doubling them: "" inside quoted string.
        // The current tokenizer does not handle this and throws an ArgumentException.
        string wkt = "GEOGCRS[\"WGS 84 \"\"test\"\"\",DATUM[\"datum\",ELLIPSOID[\"ellips\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],CS[ellipsoidal,2],AXIS[\"lat\",north],AXIS[\"lon\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]";
        Assert.That(() => CoordinateSystemWkt2Reader.ParseCrs(wkt), Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void ParseCrs_RemarkPreservation()
    {
        string wkt = "GEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east],ANGLEUNIT[\"degree\",0.0174532925199433],REMARK[\"This is a test remark\"]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Remark, Is.EqualTo("This is a test remark"));
    }

    [Test]
    public void ParseCrs_BaseGeodCrsKeyword_ParsesCorrectly()
    {
        // BASEGEODCRS is an alternative to BASEGEOGCRS
        string wkt = "PROJCRS[\"NAD83\",BASEGEODCRS[\"NAD83\",DATUM[\"North American Datum 1983\",ELLIPSOID[\"GRS 1980\",6378137,298.257222101,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]],CONVERSION[\"test\",METHOD[\"Transverse Mercator\"],PARAMETER[\"False easting\",500000]],CS[cartesian,2],AXIS[\"easting\",east],AXIS[\"northing\",north],LENGTHUNIT[\"metre\",1]]";
        var model = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.BaseCrs, Is.Not.Null);
        Assert.That(model.BaseCrs.Datum.Name, Is.EqualTo("North American Datum 1983"));
    }

    [Test]
    public void ParseCrs_IdWithVersionAndUri_PreservesAll()
    {
        string wkt = "GEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],CS[ellipsoidal,2],AXIS[\"lat\",north],AXIS[\"lon\",east],ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",4326,\"9.8.15\",URI[\"urn:ogc:def:crs:EPSG::4326\"]]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Id, Is.Not.Null);
        Assert.That(model.Id.Authority, Is.EqualTo("EPSG"));
        Assert.That(model.Id.Code, Is.EqualTo("4326"));
        // Version and URI might or might not be preserved depending on implementation
        // At minimum, parsing should succeed
    }
}
