using System;
using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2ConcatenatedOperationTests
{
    private const string Wgs84GeogCrs =
        "GEOGCRS[\"WGS 84\"," +
        "DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563]]," +
        "PRIMEM[\"Greenwich\",0]," +
        "CS[ellipsoidal,2]," +
        "AXIS[\"latitude\",north]," +
        "AXIS[\"longitude\",east]," +
        "ANGLEUNIT[\"degree\",0.0174532925199433]]";

    private const string Nad83GeogCrs =
        "GEOGCRS[\"NAD83\"," +
        "DATUM[\"North American Datum 1983\",ELLIPSOID[\"GRS 1980\",6378137,298.257222101]]," +
        "PRIMEM[\"Greenwich\",0]," +
        "CS[ellipsoidal,2]," +
        "AXIS[\"latitude\",north]," +
        "AXIS[\"longitude\",east]," +
        "ANGLEUNIT[\"degree\",0.0174532925199433]]";

    [Test]
    public void Parse_ConcatenatedOperation_Basic()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"UTM zone 28N to JHS height\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"UTM zone 28N\"," +
            "METHOD[\"Transverse Mercator\"]," +
            "PARAMETER[\"Latitude of natural origin\",0]," +
            "PARAMETER[\"Longitude of natural origin\",-15]," +
            "PARAMETER[\"Scale factor at natural origin\",0.9996]," +
            "PARAMETER[\"False easting\",500000]," +
            "PARAMETER[\"False northing\",0]]]," +
            "STEP[CONVERSION[\"Northing change\",METHOD[\"Height Depth Reversal\"]]]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat = (Wkt2ConcatenatedOperation)crs;
        Assert.That(concat.Name, Is.EqualTo("UTM zone 28N to JHS height"));
        Assert.That(concat.SourceCrs, Is.Not.Null);
        Assert.That(concat.TargetCrs, Is.Not.Null);

        Assert.That(concat.Steps, Has.Count.EqualTo(2));

        Assert.That(concat.Steps[0].Name, Is.EqualTo("UTM zone 28N"));
        Assert.That(concat.Steps[0].Method, Is.EqualTo("Transverse Mercator"));
        Assert.That(concat.Steps[0].Parameters, Has.Count.EqualTo(5));
        Assert.That(concat.Steps[0].Parameters[0].Name, Is.EqualTo("Latitude of natural origin"));
        Assert.That(concat.Steps[0].Parameters[0].Value, Is.EqualTo(0));
        Assert.That(concat.Steps[0].Parameters[1].Name, Is.EqualTo("Longitude of natural origin"));
        Assert.That(concat.Steps[0].Parameters[1].Value, Is.EqualTo(-15));
        Assert.That(concat.Steps[0].Parameters[2].Name, Is.EqualTo("Scale factor at natural origin"));
        Assert.That(concat.Steps[0].Parameters[2].Value, Is.EqualTo(0.9996));
        Assert.That(concat.Steps[0].Parameters[3].Name, Is.EqualTo("False easting"));
        Assert.That(concat.Steps[0].Parameters[3].Value, Is.EqualTo(500000));
        Assert.That(concat.Steps[0].Parameters[4].Name, Is.EqualTo("False northing"));
        Assert.That(concat.Steps[0].Parameters[4].Value, Is.EqualTo(0));

        Assert.That(concat.Steps[1].Name, Is.EqualTo("Northing change"));
        Assert.That(concat.Steps[1].Method, Is.EqualTo("Height Depth Reversal"));
    }

    [Test]
    public void Parse_ConcatenatedOperation_WithVersion()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Op with version\"," +
            "VERSION[\"1.0\"]," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Step 1\",METHOD[\"Identity\"]]]," +
            "STEP[CONVERSION[\"Step 2\",METHOD[\"Identity\"]]]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat = (Wkt2ConcatenatedOperation)crs;
        Assert.That(concat.Version, Is.EqualTo("1.0"));
        Assert.That(concat.Steps, Has.Count.EqualTo(2));
        Assert.That(concat.Steps[0].Name, Is.EqualTo("Step 1"));
        Assert.That(concat.Steps[1].Name, Is.EqualTo("Step 2"));
    }

    [Test]
    public void Parse_ConcatenatedOperation_WithOperationAccuracy()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Accurate op\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Step 1\",METHOD[\"Identity\"]]]," +
            "STEP[CONVERSION[\"Step 2\",METHOD[\"Identity\"]]]," +
            "OPERATIONACCURACY[0.1]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat = (Wkt2ConcatenatedOperation)crs;
        Assert.That(concat.OperationAccuracy, Is.Not.Null);
        Assert.That(concat.OperationAccuracy.Value, Is.EqualTo(0.1).Within(1e-10));
    }

    [Test]
    public void Parse_ConcatenatedOperation_WithIdAndRemark()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Op with id\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Step 1\",METHOD[\"Identity\"]]]," +
            "STEP[CONVERSION[\"Step 2\",METHOD[\"Identity\"]]]," +
            "ID[\"EPSG\",\"9999\"]," +
            "REMARK[\"Test concatenated operation\"]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat = (Wkt2ConcatenatedOperation)crs;
        Assert.That(concat.Id, Is.Not.Null);
        Assert.That(concat.Id.Authority, Is.EqualTo("EPSG"));
        Assert.That(concat.Id.Code, Is.EqualTo("9999"));
        Assert.That(concat.Remark, Is.EqualTo("Test concatenated operation"));
    }

    [Test]
    public void Parse_ConcatenatedOperation_WithUsage()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Op with usage\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Step 1\",METHOD[\"Identity\"]]]," +
            "STEP[CONVERSION[\"Step 2\",METHOD[\"Identity\"]]]," +
            "USAGE[SCOPE[\"Navigation\"],AREA[\"World\"],BBOX[-90,-180,90,180]]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat = (Wkt2ConcatenatedOperation)crs;
        Assert.That(concat.Usages, Has.Count.GreaterThan(0));
        Assert.That(concat.Usages[0].Scope, Is.EqualTo("Navigation"));
        Assert.That(concat.Usages[0].Area, Is.EqualTo("World"));
        Assert.That(concat.Usages[0].BBox, Is.Not.Null);
        Assert.That(concat.Usages[0].BBox.South, Is.EqualTo(-90));
        Assert.That(concat.Usages[0].BBox.West, Is.EqualTo(-180));
        Assert.That(concat.Usages[0].BBox.North, Is.EqualTo(90));
        Assert.That(concat.Usages[0].BBox.East, Is.EqualTo(180));
    }

    [Test]
    public void RoundTrip_ConcatenatedOperation()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"UTM zone 28N to JHS height\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"UTM zone 28N\"," +
            "METHOD[\"Transverse Mercator\"]," +
            "PARAMETER[\"Latitude of natural origin\",0]," +
            "PARAMETER[\"Longitude of natural origin\",-15]," +
            "PARAMETER[\"Scale factor at natural origin\",0.9996]," +
            "PARAMETER[\"False easting\",500000]," +
            "PARAMETER[\"False northing\",0]]]," +
            "STEP[CONVERSION[\"Northing change\",METHOD[\"Height Depth Reversal\"]]]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        string output = crs.ToWkt2String();

        var crs2 = CoordinateSystemWkt2Reader.ParseCrs(output);
        Assert.That(crs2, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat2 = (Wkt2ConcatenatedOperation)crs2;
        Assert.That(concat2.Name, Is.EqualTo("UTM zone 28N to JHS height"));
        Assert.That(concat2.SourceCrs, Is.Not.Null);
        Assert.That(concat2.TargetCrs, Is.Not.Null);
        Assert.That(concat2.Steps, Has.Count.EqualTo(2));
        Assert.That(concat2.Steps[0].Name, Is.EqualTo("UTM zone 28N"));
        Assert.That(concat2.Steps[0].Method, Is.EqualTo("Transverse Mercator"));
        Assert.That(concat2.Steps[0].Parameters, Has.Count.EqualTo(5));
        Assert.That(concat2.Steps[0].Parameters[0].Value, Is.EqualTo(0));
        Assert.That(concat2.Steps[0].Parameters[1].Value, Is.EqualTo(-15));
        Assert.That(concat2.Steps[0].Parameters[2].Value, Is.EqualTo(0.9996));
        Assert.That(concat2.Steps[0].Parameters[3].Value, Is.EqualTo(500000));
        Assert.That(concat2.Steps[0].Parameters[4].Value, Is.EqualTo(0));
        Assert.That(concat2.Steps[1].Name, Is.EqualTo("Northing change"));
        Assert.That(concat2.Steps[1].Method, Is.EqualTo("Height Depth Reversal"));
    }

    [Test]
    public void Parse_ConcatenatedOperation_MissingSourceCrs_Throws()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Bad\"," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Step 1\",METHOD[\"Identity\"]]]," +
            "STEP[CONVERSION[\"Step 2\",METHOD[\"Identity\"]]]]";

        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_ConcatenatedOperation_MissingTargetCrs_Throws()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Bad\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Step 1\",METHOD[\"Identity\"]]]," +
            "STEP[CONVERSION[\"Step 2\",METHOD[\"Identity\"]]]]";

        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_ConcatenatedOperation_OnlyOneStep_Throws()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Bad\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Wgs84GeogCrs + "]," +
            "STEP[CONVERSION[\"Only step\",METHOD[\"Identity\"]]]]";

        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_ConcatenatedOperation_WithCoordinateOperation()
    {
        const string wkt =
            "CONCATENATEDOPERATION[\"Complex op\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Nad83GeogCrs + "]," +
            "STEP[COORDINATEOPERATION[\"WGS 84 to NAD83 (1)\"," +
            "SOURCECRS[" + Wgs84GeogCrs + "]," +
            "TARGETCRS[" + Nad83GeogCrs + "]," +
            "METHOD[\"Geocentric translations\"]," +
            "PARAMETER[\"X-axis translation\",0]," +
            "PARAMETER[\"Y-axis translation\",0]," +
            "PARAMETER[\"Z-axis translation\",0]]]," +
            "STEP[CONVERSION[\"Identity\",METHOD[\"Identity\"]]]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2ConcatenatedOperation>());

        var concat = (Wkt2ConcatenatedOperation)crs;
        Assert.That(concat.Steps, Has.Count.EqualTo(2));
        Assert.That(concat.Steps[0].Keyword, Is.EqualTo("COORDINATEOPERATION"));
        Assert.That(concat.Steps[0].Name, Is.EqualTo("WGS 84 to NAD83 (1)"));
        Assert.That(concat.Steps[0].SourceCrs, Is.Not.Null);
        Assert.That(concat.Steps[0].TargetCrs, Is.Not.Null);
        Assert.That(concat.Steps[0].Method, Is.EqualTo("Geocentric translations"));
        Assert.That(concat.Steps[0].Parameters, Has.Count.EqualTo(3));
        Assert.That(concat.Steps[0].Parameters[0].Name, Is.EqualTo("X-axis translation"));
        Assert.That(concat.Steps[0].Parameters[0].Value, Is.EqualTo(0));

        Assert.That(concat.Steps[1].Keyword, Is.EqualTo("CONVERSION"));
        Assert.That(concat.Steps[1].Name, Is.EqualTo("Identity"));
    }
}
