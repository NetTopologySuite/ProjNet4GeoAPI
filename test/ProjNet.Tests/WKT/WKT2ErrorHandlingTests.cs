using System;
using NUnit.Framework;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2ErrorHandlingTests
{
    [Test]
    public void ParseCrs_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CoordinateSystemWkt2Reader.ParseCrs(null));
    }

    [Test]
    public void ParseCrs_EmptyString_ThrowsArgumentException()
    {
        // Empty or whitespace should throw ArgumentNullException (null-check catches whitespace too)
        Assert.Throws<ArgumentNullException>(() => CoordinateSystemWkt2Reader.ParseCrs(""));
    }

    [Test]
    public void ParseCrs_WhitespaceOnly_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => CoordinateSystemWkt2Reader.ParseCrs("   "));
    }

    [Test]
    public void ParseCrs_UnrecognizedRootKeyword_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs("UNKNOWNCRS[\"test\"]"));
    }

    [Test]
    public void ParseCrs_GeogCrsWithoutDatum_ThrowsArgumentException()
    {
        string wkt = "GEOGCRS[\"No Datum\",CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east],ANGLEUNIT[\"degree\",0.0174532925199433]]";
        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void ParseCrs_ConversionWithoutMethod_ThrowsArgumentException()
    {
        string wkt = "PROJCRS[\"No method\",BASEGEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east]],CONVERSION[\"Missing Method\",PARAMETER[\"false_easting\",0]],CS[cartesian,2],AXIS[\"easting\",east],AXIS[\"northing\",north],LENGTHUNIT[\"metre\",1]]";
        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void ParseCrs_BoundCrsWithoutSourceCrs_ThrowsArgumentException()
    {
        string wkt = "BOUNDCRS[\"No source\",TARGETCRS[GEOGCRS[\"WGS 84\",DATUM[\"World Geodetic System 1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],CS[ellipsoidal,2],AXIS[\"latitude\",north],AXIS[\"longitude\",east]]],ABRIDGEDTRANSFORMATION[\"test\",METHOD[\"Geocentric translations\"],PARAMETER[\"X-axis translation\",0],PARAMETER[\"Y-axis translation\",0],PARAMETER[\"Z-axis translation\",0]]]";
        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void ParseCrs_CompoundCrsWithoutComponents_ThrowsArgumentException()
    {
        string wkt = "COMPOUNDCRS[\"Empty compound\"]";
        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_UnsupportedCrsType_ThrowsNotSupportedException()
    {
        // VERTCRS is parseable to model but not convertible to ProjNet
        string wkt = "VERTCRS[\"EGM96 height\",VDATUM[\"EGM96 geoid\"],CS[vertical,1],AXIS[\"gravity-related height (H)\",up,ORDER[1]],LENGTHUNIT[\"metre\",1],ID[\"EPSG\",5773]]";
        Assert.Throws<NotSupportedException>(() => CoordinateSystemWkt2Reader.Parse(wkt));
    }

    [Test]
    public void Parse_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CoordinateSystemWkt2Reader.Parse(null));
    }
}
