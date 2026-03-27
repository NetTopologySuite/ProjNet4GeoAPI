using System;
using NUnit.Framework;
using ProjNet.CoordinateSystems.Wkt2;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT;

[TestFixture]
public class WKT2TimeCrsTests
{
    [Test]
    public void Parse_TimeCrs_WithTDatum()
    {
        const string wkt =
            "TIMECRS[\"GPS Time\"," +
            "TDATUM[\"Time origin\",TIMEORIGIN[\"1980-01-06T00:00:00.0Z\"],CALENDAR[\"proleptic Gregorian\"]]," +
            "CS[temporal,1]," +
            "AXIS[\"time (T)\",future,ORDER[1]]," +
            "TIMEUNIT[\"day\",86400]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2TimeCrs>());

        var timeCrs = (Wkt2TimeCrs)crs;
        Assert.That(timeCrs.Name, Is.EqualTo("GPS Time"));
        Assert.That(timeCrs.Keyword, Is.EqualTo("TIMECRS"));

        Assert.That(timeCrs.Datum, Is.Not.Null);
        Assert.That(timeCrs.Datum.Name, Is.EqualTo("Time origin"));
        Assert.That(timeCrs.Datum.Keyword, Is.EqualTo("TDATUM"));
        Assert.That(timeCrs.Datum.Calendar, Is.EqualTo("proleptic Gregorian"));
        Assert.That(timeCrs.Datum.TimeOrigin, Is.EqualTo("1980-01-06T00:00:00.0Z"));

        Assert.That(timeCrs.CoordinateSystem, Is.Not.Null);
        Assert.That(timeCrs.CoordinateSystem.Type, Is.EqualTo("temporal"));
        Assert.That(timeCrs.CoordinateSystem.Dimension, Is.EqualTo(1));
        Assert.That(timeCrs.CoordinateSystem.Axes, Has.Count.EqualTo(1));
        Assert.That(timeCrs.CoordinateSystem.Axes[0].Name, Is.EqualTo("time (T)"));
        Assert.That(timeCrs.CoordinateSystem.Axes[0].Direction, Is.EqualTo("future"));
        Assert.That(timeCrs.CoordinateSystem.Axes[0].Order, Is.EqualTo(1));

        Assert.That(timeCrs.CoordinateSystem.Unit, Is.Not.Null);
        Assert.That(timeCrs.CoordinateSystem.Unit.Name, Is.EqualTo("day"));
        Assert.That(timeCrs.CoordinateSystem.Unit.ConversionFactor, Is.EqualTo(86400));
        Assert.That(timeCrs.CoordinateSystem.Unit.Keyword, Is.EqualTo("TIMEUNIT"));
    }

    [Test]
    public void Parse_TimeCrs_WithTimedatum()
    {
        const string wkt =
            "TIMECRS[\"Modified Julian Date\"," +
            "TIMEDATUM[\"Modified Julian\",TIMEORIGIN[\"1858-11-17\"]]," +
            "CS[temporal,1]," +
            "AXIS[\"time\",future]," +
            "TIMEUNIT[\"day\",86400]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2TimeCrs>());

        var timeCrs = (Wkt2TimeCrs)crs;
        Assert.That(timeCrs.Name, Is.EqualTo("Modified Julian Date"));
        Assert.That(timeCrs.Datum.Name, Is.EqualTo("Modified Julian"));
        Assert.That(timeCrs.Datum.Keyword, Is.EqualTo("TIMEDATUM"));
        Assert.That(timeCrs.Datum.TimeOrigin, Is.EqualTo("1858-11-17"));
    }

    [Test]
    public void Parse_TimeCrs_WithIdAndRemark()
    {
        const string wkt =
            "TIMECRS[\"DateTime\"," +
            "TDATUM[\"DateTime\",TIMEORIGIN[\"0001-01-01T00:00:00\"]]," +
            "CS[temporal,1]," +
            "AXIS[\"time\",future]," +
            "TIMEUNIT[\"second\",1]," +
            "ID[\"PROJ\",\"TDATETIME\"]," +
            "REMARK[\"For DateTime\"]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2TimeCrs>());

        var timeCrs = (Wkt2TimeCrs)crs;
        Assert.That(timeCrs.Name, Is.EqualTo("DateTime"));
        Assert.That(timeCrs.Id, Is.Not.Null);
        Assert.That(timeCrs.Id.Authority, Is.EqualTo("PROJ"));
        Assert.That(timeCrs.Id.Code, Is.EqualTo("TDATETIME"));
        Assert.That(timeCrs.Remark, Is.EqualTo("For DateTime"));
    }

    [Test]
    public void Parse_TimeCrs_WithUsage()
    {
        const string wkt =
            "TIMECRS[\"Unix Time\"," +
            "TDATUM[\"Unix epoch\",TIMEORIGIN[\"1970-01-01T00:00:00Z\"]]," +
            "CS[temporal,1]," +
            "AXIS[\"time\",future]," +
            "TIMEUNIT[\"second\",1]," +
            "USAGE[SCOPE[\"Satellite\"],AREA[\"World\"],BBOX[-90,-180,90,180]]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2TimeCrs>());

        var timeCrs = (Wkt2TimeCrs)crs;
        Assert.That(timeCrs.Name, Is.EqualTo("Unix Time"));
        Assert.That(timeCrs.Usages, Has.Count.EqualTo(1));
        Assert.That(timeCrs.Usages[0].Scope, Is.EqualTo("Satellite"));
        Assert.That(timeCrs.Usages[0].Area, Is.EqualTo("World"));
        Assert.That(timeCrs.Usages[0].BBox, Is.Not.Null);
        Assert.That(timeCrs.Usages[0].BBox.South, Is.EqualTo(-90));
        Assert.That(timeCrs.Usages[0].BBox.West, Is.EqualTo(-180));
        Assert.That(timeCrs.Usages[0].BBox.North, Is.EqualTo(90));
        Assert.That(timeCrs.Usages[0].BBox.East, Is.EqualTo(180));
    }

    [Test]
    public void RoundTrip_TimeCrs()
    {
        const string wkt =
            "TIMECRS[\"GPS Time\"," +
            "TDATUM[\"Time origin\",TIMEORIGIN[\"1980-01-06T00:00:00.0Z\"],CALENDAR[\"proleptic Gregorian\"]]," +
            "CS[temporal,1]," +
            "AXIS[\"time (T)\",future,ORDER[1]]," +
            "TIMEUNIT[\"day\",86400]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        string output = crs.ToWkt2String();

        var crs2 = CoordinateSystemWkt2Reader.ParseCrs(output);
        Assert.That(crs2, Is.InstanceOf<Wkt2TimeCrs>());

        var timeCrs2 = (Wkt2TimeCrs)crs2;
        Assert.That(timeCrs2.Name, Is.EqualTo("GPS Time"));
        Assert.That(timeCrs2.Datum.Name, Is.EqualTo("Time origin"));
        Assert.That(timeCrs2.Datum.Keyword, Is.EqualTo("TDATUM"));
        Assert.That(timeCrs2.Datum.Calendar, Is.EqualTo("proleptic Gregorian"));
        Assert.That(timeCrs2.Datum.TimeOrigin, Is.EqualTo("1980-01-06T00:00:00.0Z"));
        Assert.That(timeCrs2.CoordinateSystem.Type, Is.EqualTo("temporal"));
        Assert.That(timeCrs2.CoordinateSystem.Dimension, Is.EqualTo(1));
        Assert.That(timeCrs2.CoordinateSystem.Axes, Has.Count.EqualTo(1));
        Assert.That(timeCrs2.CoordinateSystem.Axes[0].Name, Is.EqualTo("time (T)"));
        Assert.That(timeCrs2.CoordinateSystem.Axes[0].Direction, Is.EqualTo("future"));
        Assert.That(timeCrs2.CoordinateSystem.Unit.Name, Is.EqualTo("day"));
        Assert.That(timeCrs2.CoordinateSystem.Unit.ConversionFactor, Is.EqualTo(86400));
    }

    [Test]
    public void Parse_TimeCrs_MissingDatum_Throws()
    {
        const string wkt =
            "TIMECRS[\"Bad\"," +
            "CS[temporal,1]," +
            "AXIS[\"time\",future]," +
            "TIMEUNIT[\"day\",86400]]";

        Assert.Throws<ArgumentException>(() => CoordinateSystemWkt2Reader.ParseCrs(wkt));
    }

    [Test]
    public void Parse_TimeCrs_WithUnit()
    {
        const string wkt =
            "TIMECRS[\"Julian Date\"," +
            "TDATUM[\"Julian\",TIMEORIGIN[\"-4713-11-24T12:00:00Z\"]]," +
            "CS[temporal,1]," +
            "AXIS[\"time\",future]," +
            "UNIT[\"day\",86400]]";

        var crs = CoordinateSystemWkt2Reader.ParseCrs(wkt);
        Assert.That(crs, Is.InstanceOf<Wkt2TimeCrs>());

        var timeCrs = (Wkt2TimeCrs)crs;
        Assert.That(timeCrs.Name, Is.EqualTo("Julian Date"));
        Assert.That(timeCrs.CoordinateSystem.Unit, Is.Not.Null);
        Assert.That(timeCrs.CoordinateSystem.Unit.Name, Is.EqualTo("day"));
        Assert.That(timeCrs.CoordinateSystem.Unit.ConversionFactor, Is.EqualTo(86400));
        Assert.That(timeCrs.CoordinateSystem.Unit.Keyword, Is.EqualTo("UNIT"));
        Assert.That(timeCrs.Datum.TimeOrigin, Is.EqualTo("-4713-11-24T12:00:00Z"));
    }
}
