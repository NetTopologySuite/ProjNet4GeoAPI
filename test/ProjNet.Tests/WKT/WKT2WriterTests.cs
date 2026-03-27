using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2WriterTests
{
    // --- Write + Reparse for all CRS types ---

    private const string GeogCrsWkt = "GEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north,ORDER[1]],AXIS[\"longitude\",east,ORDER[2]],ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",4326]]";

    private const string ProjCrsWkt = "PROJCRS[\"WGS 84 / UTM zone 32N\"," +
        "BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
        "CONVERSION[\"UTM zone 32N\",METHOD[\"Transverse Mercator\"],PARAMETER[\"Latitude of natural origin\",0],PARAMETER[\"Longitude of natural origin\",9],PARAMETER[\"Scale factor at natural origin\",0.9996],PARAMETER[\"False easting\",500000],PARAMETER[\"False northing\",0]]," +
        "CS[cartesian,2],AXIS[\"easting (E)\",east,ORDER[1]],AXIS[\"northing (N)\",north,ORDER[2]],LENGTHUNIT[\"metre\",1],ID[\"EPSG\",32632]]";

    private const string VertCrsWkt = "VERTCRS[\"EGM96 height\",VDATUM[\"EGM96 geoid\",ID[\"EPSG\",5171]],CS[vertical,1],AXIS[\"gravity-related height (H)\",up,ORDER[1]],LENGTHUNIT[\"metre\",1],ID[\"EPSG\",5773]]";

    private const string EngCrsWkt = "ENGCRS[\"Local grid\",EDATUM[\"Local datum\"],CS[cartesian,2],AXIS[\"x\",east,ORDER[1]],AXIS[\"y\",north,ORDER[2]],LENGTHUNIT[\"metre\",1]]";

    private const string ParametricCrsWkt = "PARAMETRICCRS[\"Sigma\",PDATUM[\"Sigma datum\"],CS[parametric,1],AXIS[\"sigma\",up,ORDER[1]],PARAMETRICUNIT[\"unity\",1]]";

    private const string CompoundCrsWkt = "COMPOUNDCRS[\"WGS 84 + height\"," +
        "GEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]," +
        "VERTCRS[\"EGM96 height\",VDATUM[\"EGM96 geoid\"],CS[vertical,1],AXIS[\"height\",up],LENGTHUNIT[\"metre\",1]]]";

    private const string BoundCrsWkt = "BOUNDCRS[\"ETRS89 (bound)\"," +
        "SOURCECRS[GEOGCRS[\"ETRS89\",DATUM[\"European Terrestrial Reference System 1989\",ELLIPSOID[\"GRS 1980\",6378137,298.257222101,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
        "TARGETCRS[GEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]]," +
        "ABRIDGEDTRANSFORMATION[\"ETRS89 to WGS 84\",METHOD[\"Geocentric translations\"],PARAMETER[\"X-axis translation\",0],PARAMETER[\"Y-axis translation\",0],PARAMETER[\"Z-axis translation\",0]]]";

    [Test]
    public void WriteGeogCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2GeogCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Name, Is.EqualTo("WGS 84"));
        Assert.That(reparsed.Datum.Ellipsoid.SemiMajorAxis, Is.EqualTo(6378137));
        Assert.That(reparsed.Datum.Ellipsoid.InverseFlattening, Is.EqualTo(298.257223563));
        Assert.That(reparsed.CoordinateSystem.Dimension, Is.EqualTo(2));
    }

    [Test]
    public void WriteProjCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(ProjCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2ProjCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Name, Is.EqualTo("WGS 84 / UTM zone 32N"));
        Assert.That(reparsed.Conversion.MethodName, Is.EqualTo("Transverse Mercator"));
        Assert.That(reparsed.Conversion.Parameters, Has.Count.EqualTo(5));
        Assert.That(reparsed.BaseCrs.Datum.Ellipsoid.SemiMajorAxis, Is.EqualTo(6378137));
    }

    [Test]
    public void WriteVertCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(VertCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2VertCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Name, Is.EqualTo("EGM96 height"));
        Assert.That(reparsed.CoordinateSystem.Dimension, Is.EqualTo(1));
    }

    [Test]
    public void WriteEngCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(EngCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2EngCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Name, Is.EqualTo("Local grid"));
        Assert.That(reparsed.CoordinateSystem.Axes, Has.Count.EqualTo(2));
    }

    [Test]
    public void WriteParametricCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(ParametricCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2ParametricCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Name, Is.EqualTo("Sigma"));
        Assert.That(reparsed.CoordinateSystem.Dimension, Is.EqualTo(1));
    }

    [Test]
    public void WriteCompoundCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(CompoundCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2CompoundCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Name, Is.EqualTo("WGS 84 + height"));
        Assert.That(reparsed.Components, Has.Count.EqualTo(2));
        Assert.That(reparsed.Components[0], Is.InstanceOf<Wkt2GeogCrs>());
        Assert.That(reparsed.Components[1], Is.InstanceOf<Wkt2VertCrs>());
    }

    [Test]
    public void WriteBoundCrs_OutputIsReparseable()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(BoundCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        var reparsed = (Wkt2BoundCrs)CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.SourceCrs, Is.InstanceOf<Wkt2GeogCrs>());
        Assert.That(reparsed.TargetCrs, Is.InstanceOf<Wkt2GeogCrs>());
        Assert.That(reparsed.Transformation.MethodName, Is.EqualTo("Geocentric translations"));
        Assert.That(reparsed.Transformation.Parameters, Has.Count.EqualTo(3));
    }

    // --- Output format tests ---

    [Test]
    public void Write_OutputUsesCorrectKeywordCasing()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        Assert.That(written, Does.StartWith("GEOGCRS["));
        Assert.That(written, Does.Contain("DATUM["));
        Assert.That(written, Does.Contain("ELLIPSOID["));
        Assert.That(written, Does.Contain("CS["));
        Assert.That(written, Does.Contain("AXIS["));
    }

    [Test]
    public void Write_OutputUsesInvariantDecimalPoint()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        // Should use '.' not ',' for decimals (invariant culture)
        Assert.That(written, Does.Contain("298.257223563"));
        Assert.That(written, Does.Not.Contain("298,257223563"));
    }

    [Test]
    public void Write_IdIsPreserved()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWkt);
        string written = CoordinateSystemWkt2Writer.Write(model);
        // Writer formats codes as quoted strings
        Assert.That(written, Does.Contain("ID[\"EPSG\",\"4326\"]"));
    }
}
