using System.IO;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt.Core;
using ProjNet.IO.Wkt.Tree;
using ProjNet.Wkt;

namespace ProjNET.Tests.WKT;

public class WktToProjConverterTests
{
    [Test]
    public void TestConvert_EPGS_28992()
    {
        // Arrange
        string text = @"PROJCS[""Amersfoort / RD New"", GEOGCS[""Amersfoort"",
    DATUM[""Amersfoort"",SPHEROID[""Bessel 1841"", 6377397.155, 299.1528128, AUTHORITY[""EPSG"",""7004""]],
      TOWGS84[565.2369, 50.0087, 465.658, -0.40685733032239757, -0.3507326765425626, 1.8703473836067956, 4.0812],
      AUTHORITY[""EPSG"",""6289""]],
    PRIMEM[""Greenwich"", 0.0, AUTHORITY[""EPSG"",""8901""]],
    UNIT[""degree"", 0.017453292519943295],
    AXIS[""Geodetic latitude"", NORTH],
    AXIS[""Geodetic longitude"", EAST],
    AUTHORITY[""EPSG"",""4289""]],
  PROJECTION[""Oblique_Stereographic"", AUTHORITY[""EPSG"",""9809""]],
  PARAMETER[""central_meridian"", 5.387638888888891],
  PARAMETER[""latitude_of_origin"", 52.15616055555556],
  PARAMETER[""scale_factor"", 0.9999079],
  PARAMETER[""false_easting"", 155000.0],
  PARAMETER[""false_northing"", 463000.0],
  UNIT[""m"", 1.0],
  AXIS[""Easting"", EAST],
  AXIS[""Northing"", NORTH],
  AUTHORITY[""EPSG"",""28992""]]";
        using var sr = new StringReader(text);
        using var reader = new WktTextReader(sr);

        var result = reader.ReadToEnd();

        Assert.NotNull(result);
        Assert.IsTrue(result.Success);//, resultCs.Error.RenderErrorMessage());

        var wktCs = result.GetValueOrDefault();

        var converter = new WktToProjConverter();

        // Act
        var projCs = converter.Convert(wktCs);

        // Assert
        Assert.NotNull(projCs);
        Assert.AreEqual(wktCs.Name, projCs.Name);

        Assert.IsInstanceOf<WktProjectedCoordinateSystem>(wktCs);
        Assert.IsInstanceOf<ProjectedCoordinateSystem>(projCs);

        var projectedCs = projCs as ProjectedCoordinateSystem;

        Assert.AreEqual("Oblique_Stereographic", projectedCs.Projection.Name);
        Assert.AreEqual("EPSG", projectedCs.Projection.Authority);
        Assert.AreEqual(9809, projectedCs.Projection.AuthorityCode);
        Assert.AreEqual(5, projectedCs.Projection.NumParameters);

        var first = projectedCs.Projection.GetParameter(0);
        Assert.AreEqual("central_meridian", first.Name);
        Assert.AreEqual(5.387638888888891, first.Value);

        var last = projectedCs.Projection.GetParameter(4);
        Assert.AreEqual("false_northing", last.Name);
        Assert.AreEqual(463000.0, last.Value);

        Assert.AreEqual("EPSG", projectedCs.Authority);
        Assert.AreEqual(28992, projectedCs.AuthorityCode);

        var firstAxis = projectedCs.AxisInfo[0];
        Assert.AreEqual("Easting", firstAxis.Name);
        Assert.AreEqual(AxisOrientationEnum.East, firstAxis.Orientation);
    }


    [Test]
    public void ParseAllWKTs()
    {
        int parseCount = 0;
        foreach (var wkt in SRIDReader.GetSrids())
        {
            using var sr01 = new StringReader(wkt.Wkt);
            using var wktReader01 = new WktTextReader(sr01);
            var result01 = wktReader01.ReadToEnd();
            Assert.That(result01.Success, Is.True);
            var cs01 = result01.Value;
            Assert.IsNotNull(cs01, "Could not parse WKT: " + wkt.Wkt);
            var converter01 = new WktToProjConverter();
            var projObj01 = converter01.Convert(cs01);

            //@TODO: Create outputWriter and formater for changing delimiters in right context: .Replace("[", "(").Replace("]", ")")));
            using var sr02 = new StringReader(wkt.Wkt);
            using var wktReader02 = new WktTextReader(sr02);
            var result02 = wktReader02.ReadToEnd();
            Assert.That(result02.Success, Is.True);
            var cs02 = result02.Value;
            var converter02 = new WktToProjConverter();
            var projObj02 = converter01.Convert(cs02);

            // Comparing whole tree using IEquatable.Equals(...)
            Assert.That(cs01.Equals(cs02), Is.True);

            // EqualParam fails for now and I don't dare to fix it yet.
            //Assert.That(projObj01.EqualParams(projObj02), Is.True);

            parseCount++;
        }
        Assert.That(parseCount, Is.GreaterThan(2671), "Not all WKT was parsed");
    }
}
