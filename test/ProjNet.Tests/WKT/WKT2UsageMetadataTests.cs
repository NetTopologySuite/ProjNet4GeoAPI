using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2UsageMetadataTests
{
    private const string GeogCrsWithUsage =
        "GEOGCRS[\"WGS 84\"," +
        "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
        "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
        "CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east]," +
        "ANGLEUNIT[\"degree\",0.0174532925199433]," +
        "USAGE[SCOPE[\"Horizontal component of 3D system.\"],AREA[\"World.\"],BBOX[-90,-180,90,180]]," +
        "ID[\"EPSG\",4326]]";

    private const string VertCrsWithUsage =
        "VERTCRS[\"EGM96 height\"," +
        "VDATUM[\"EGM96 geoid\",ID[\"EPSG\",5171]]," +
        "CS[vertical,1],AXIS[\"gravity-related height (H)\",up,ORDER[1]]," +
        "LENGTHUNIT[\"metre\",1]," +
        "USAGE[SCOPE[\"Geodesy.\"],AREA[\"World.\"],BBOX[-90,-180,90,180]]," +
        "ID[\"EPSG\",5773]]";

    private const string GeogCrsWithRemark =
        "GEOGCRS[\"WGS 84\"," +
        "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
        "PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]]," +
        "CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east]," +
        "ANGLEUNIT[\"degree\",0.0174532925199433]," +
        "REMARK[\"This is the most common CRS for GPS data.\"]]";

    [Test]
    public void ParseCrs_WithUsage_StoresScope()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWithUsage);
        Assert.That(model.Usages, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(model.Usages[0].Scope, Is.EqualTo("Horizontal component of 3D system."));
    }

    [Test]
    public void ParseCrs_WithUsage_StoresArea()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWithUsage);
        Assert.That(model.Usages, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(model.Usages[0].Area, Is.EqualTo("World."));
    }

    [Test]
    public void ParseCrs_WithUsage_StoresBBox()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWithUsage);
        Assert.That(model.Usages, Has.Count.GreaterThanOrEqualTo(1));
        var bbox = model.Usages[0].BBox;
        Assert.That(bbox, Is.Not.Null);
        Assert.That(bbox.South, Is.EqualTo(-90));
        Assert.That(bbox.West, Is.EqualTo(-180));
        Assert.That(bbox.North, Is.EqualTo(90));
        Assert.That(bbox.East, Is.EqualTo(180));
    }

    [Test]
    public void ParseCrs_WithUsage_RoundTripsViaWriter()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWithUsage);
        string written = CoordinateSystemWkt2Writer.Write(model);
        Assert.That(written, Does.Contain("USAGE["));
        Assert.That(written, Does.Contain("SCOPE["));
        Assert.That(written, Does.Contain("AREA["));
        Assert.That(written, Does.Contain("BBOX["));

        // Re-parse and verify
        var reparsed = CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Usages, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(reparsed.Usages[0].Scope, Is.EqualTo("Horizontal component of 3D system."));
    }

    [Test]
    public void ParseVertCrs_WithUsage_StoresMetadata()
    {
        var model = (Wkt2VertCrs)CoordinateSystemWkt2Reader.ParseCrs(VertCrsWithUsage);
        Assert.That(model.Usages, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(model.Usages[0].Scope, Is.EqualTo("Geodesy."));
        Assert.That(model.Usages[0].Area, Is.EqualTo("World."));
    }

    [Test]
    public void ParseCrs_WithRemark_StoresRemark()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWithRemark);
        Assert.That(model.Remark, Is.EqualTo("This is the most common CRS for GPS data."));
    }

    [Test]
    public void ParseCrs_WithRemark_RoundTripsViaWriter()
    {
        var model = CoordinateSystemWkt2Reader.ParseCrs(GeogCrsWithRemark);
        string written = CoordinateSystemWkt2Writer.Write(model);
        Assert.That(written, Does.Contain("REMARK["));

        var reparsed = CoordinateSystemWkt2Reader.ParseCrs(written);
        Assert.That(reparsed.Remark, Is.EqualTo("This is the most common CRS for GPS data."));
    }

    [Test]
    public void ParseCrs_WithoutUsage_HasEmptyUsagesList()
    {
        string wkt = "GEOGCRS[\"Simple\"," +
            "DATUM[\"D\",ELLIPSOID[\"E\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]]," +
            "CS[ellipsoidal,2],AXIS[\"lat\",north],AXIS[\"lon\",east]," +
            "ANGLEUNIT[\"degree\",0.0174532925199433]]";
        var model = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(model.Usages, Is.Not.Null);
        Assert.That(model.Usages, Has.Count.EqualTo(0));
    }
}
