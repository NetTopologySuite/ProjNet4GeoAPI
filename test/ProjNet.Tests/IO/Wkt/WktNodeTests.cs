// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.Wkt;

using System;
using System.Collections.Generic;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for the WKT node types and the ToWktNode methods.
/// </summary>
public class WktNodeTests
{
    private static readonly CoordinateSystemServices CoordinateSystemServices = new();

    /// <summary>
    /// Verifies that <see cref="WktQuotedString.ToString"/> wraps the stored value in double quotes.
    /// </summary>
    [Fact]
    public void WktQuotedString_ToString_WrapsValueInQuotes()
    {
        var node = new WktQuotedString("WGS 84");
        Assert.Equal("\"WGS 84\"", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktQuotedString.Value"/> returns the original string without surrounding quotes.
    /// </summary>
    [Fact]
    public void WktQuotedString_Value_ReturnsUnquotedValue()
    {
        var node = new WktQuotedString("WGS 84");
        Assert.Equal("WGS 84", node.Value);
    }

    /// <summary>
    /// Verifies that <see cref="WktQuotedString.ToFormattedString"/> matches the compact representation for a leaf node.
    /// </summary>
    [Fact]
    public void WktQuotedString_ToFormattedString_ReturnsQuotedValue()
    {
        var node = new WktQuotedString("WGS 84");
        Assert.Equal("\"WGS 84\"", node.ToFormattedString());
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToString"/> formats a positive integer value without a decimal point.
    /// </summary>
    [Fact]
    public void WktNumber_ToString_FormatsPositiveInteger()
    {
        var node = new WktNumber(6378137);
        Assert.Equal("6378137", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToString"/> formats a negative value with a leading minus sign.
    /// </summary>
    [Fact]
    public void WktNumber_ToString_FormatsNegativeValue()
    {
        var node = new WktNumber(-87);
        Assert.Equal("-87", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToString"/> preserves the full precision of a decimal value.
    /// </summary>
    [Fact]
    public void WktNumber_ToString_FormatsDecimalValue()
    {
        var node = new WktNumber(298.257223563);
        Assert.Equal("298.257223563", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToString"/> retains sufficient precision for a very small decimal value
    /// such as the radian-per-degree conversion factor.
    /// </summary>
    [Fact]
    public void WktNumber_ToString_FormatsVerySmallValue()
    {
        var node = new WktNumber(0.017453292519943295);
        string result = node.ToString();
        Assert.Contains("0.017453292519943", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToString"/> formats zero as <c>0</c>.
    /// </summary>
    [Fact]
    public void WktNumber_ToString_FormatsZero()
    {
        var node = new WktNumber(0);
        Assert.Equal("0", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToString"/> formats a large integer value without scientific notation.
    /// </summary>
    [Fact]
    public void WktNumber_ToString_FormatsLargeValue()
    {
        var node = new WktNumber(10000000);
        Assert.Equal("10000000", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.Value"/> returns the original numeric value.
    /// </summary>
    [Fact]
    public void WktNumber_Value_ReturnsOriginalValue()
    {
        var node = new WktNumber(298.257223563);
        Assert.Equal(298.257223563, node.Value, 12);
    }

    /// <summary>
    /// Verifies that <see cref="WktNumber.ToFormattedString"/> matches the compact representation for a leaf node.
    /// </summary>
    [Fact]
    public void WktNumber_ToFormattedString_MatchesToString()
    {
        var node = new WktNumber(298.257223563);
        Assert.Equal(node.ToString(), node.ToFormattedString());
    }

    /// <summary>
    /// Verifies that <see cref="WktInteger.ToString"/> formats a positive integer value correctly.
    /// </summary>
    [Fact]
    public void WktInteger_ToString_FormatsPositiveValue()
    {
        var node = new WktInteger(4326);
        Assert.Equal("4326", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktInteger.ToString"/> formats a negative integer value with a leading minus sign.
    /// </summary>
    [Fact]
    public void WktInteger_ToString_FormatsNegativeValue()
    {
        var node = new WktInteger(-1);
        Assert.Equal("-1", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktInteger.ToString"/> formats zero as <c>0</c>.
    /// </summary>
    [Fact]
    public void WktInteger_ToString_FormatsZero()
    {
        var node = new WktInteger(0);
        Assert.Equal("0", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktInteger.Value"/> returns the original integer value.
    /// </summary>
    [Fact]
    public void WktInteger_Value_ReturnsOriginalValue()
    {
        var node = new WktInteger(4326);
        Assert.Equal(4326, node.Value);
    }

    /// <summary>
    /// Verifies that <see cref="WktInteger.ToFormattedString"/> matches the compact representation for a leaf node.
    /// </summary>
    [Fact]
    public void WktInteger_ToFormattedString_MatchesToString()
    {
        var node = new WktInteger(4326);
        Assert.Equal(node.ToString(), node.ToFormattedString());
    }

    /// <summary>
    /// Verifies that <see cref="WktIdentifier.ToString"/> returns the identifier name without any quoting or modification.
    /// </summary>
    [Fact]
    public void WktIdentifier_ToString_ReturnsNameAsIs()
    {
        var node = new WktIdentifier("NORTH");
        Assert.Equal("NORTH", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktIdentifier.Name"/> returns the original identifier text.
    /// </summary>
    [Fact]
    public void WktIdentifier_Name_ReturnsOriginalText()
    {
        var node = new WktIdentifier("NORTH");
        Assert.Equal("NORTH", node.Name);
    }

    /// <summary>
    /// Verifies that <see cref="WktIdentifier.ToFormattedString"/> matches the compact representation for a leaf node.
    /// </summary>
    [Fact]
    public void WktIdentifier_ToFormattedString_MatchesToString()
    {
        var node = new WktIdentifier("NORTH");
        Assert.Equal(node.ToString(), node.ToFormattedString());
    }

    /// <summary>
    /// Verifies that <see cref="WktKeywordNode.ToString"/> produces a compact single-line WKT expression with the
    /// keyword name followed by bracket-enclosed, comma-separated children.
    /// </summary>
    [Fact]
    public void WktKeywordNode_ToString_CompactFormat()
    {
        var node = new WktKeywordNode(
            "UNIT",
            new WktQuotedString("degree"),
            new WktNumber(0.0174532925199433));

        string result = node.ToString();
        Assert.StartsWith("UNIT[\"degree\", 0.017453292519943", result, StringComparison.Ordinal);
        Assert.EndsWith("]", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that <see cref="WktKeywordNode.ToString"/> correctly serializes quoted-string children into a
    /// single-line compact WKT expression.
    /// </summary>
    [Fact]
    public void WktKeywordNode_ToString_NestedKeywords()
    {
        var node = new WktKeywordNode(
            "AUTHORITY",
            new WktQuotedString("EPSG"),
            new WktQuotedString("4326"));

        Assert.Equal("AUTHORITY[\"EPSG\", \"4326\"]", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktKeywordNode.ToString"/> produces an empty-bracket expression when the node
    /// has no children.
    /// </summary>
    [Fact]
    public void WktKeywordNode_ToString_EmptyChildren()
    {
        var node = new WktKeywordNode("EMPTY");
        Assert.Equal("EMPTY[]", node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="WktKeywordNode.ToFormattedString"/> produces the same single-line output as
    /// <see cref="WktKeywordNode.ToString"/> when all children are leaf nodes.
    /// </summary>
    [Fact]
    public void WktKeywordNode_ToFormattedString_SimpleNode_NoIndentation()
    {
        var node = new WktKeywordNode(
            "AUTHORITY",
            new WktQuotedString("EPSG"),
            new WktQuotedString("4326"));

        string result = node.ToFormattedString();
        Assert.Equal("AUTHORITY[\"EPSG\", \"4326\"]", result);
    }

    /// <summary>
    /// Verifies that <see cref="WktKeywordNode.ToFormattedString"/> introduces newlines and indentation when the
    /// node contains nested keyword children.
    /// </summary>
    [Fact]
    public void WktKeywordNode_ToFormattedString_WithComplexChildren_UsesNewlines()
    {
        var inner = new WktKeywordNode(
            "AUTHORITY",
            new WktQuotedString("EPSG"),
            new WktQuotedString("7030"));

        var node = new WktKeywordNode(
            "SPHEROID",
            new WktQuotedString("WGS 84"),
            new WktNumber(6378137),
            new WktNumber(298.257223563),
            inner);

        string result = node.ToFormattedString();
        Assert.Contains("\n", result, StringComparison.Ordinal);
        Assert.Contains("    ", result, StringComparison.Ordinal);
        Assert.Contains("AUTHORITY", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the <see cref="WktKeywordNode"/> constructor accepting an
    /// <see cref="IReadOnlyList{T}"/> of <see cref="WktNode"/> correctly builds the node and
    /// produces the expected compact WKT string.
    /// </summary>
    [Fact]
    public void WktKeywordNode_IReadOnlyListConstructor_Works()
    {
        IReadOnlyList<WktNode> children = new WktNode[]
        {
            new WktQuotedString("test"),
            new WktNumber(42),
        };

        var node = new WktKeywordNode("TEST", children);
        Assert.Equal("TEST[\"test\", 42]", node.ToString());
    }

    /// <summary>
    /// Verifies that <c>AngularUnit.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the unit.
    /// </summary>
    [Fact]
    public void AngularUnit_ToWktNode_MatchesWkt()
    {
        AngularUnit unit = AngularUnit.Degrees;
        var node = unit.ToWktNode();
        Assert.Equal(unit.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that helper objects keep their existing WKT1 node output when routed through the versioned API.
    /// </summary>
    [Fact]
    public void HelperObjects_ToWktNode_WithWkt1Version_MatchesParameterlessOutput()
    {
        AxisInfo axis = new("Lon", AxisOrientationEnum.East);
        Projection projection = Assert.IsType<Projection>(ProjectedCoordinateSystem.WebMercator.Projection);
        ProjectionParameter parameter = new("central_meridian", 15d);
        Wgs84ConversionInfo wgs84 = new(-87, -98, -121, 0, 0, 0, 0);

        Assert.Equal(AngularUnit.Degrees.ToWktNode().ToString(), AngularUnit.Degrees.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(LinearUnit.Metre.ToWktNode().ToString(), LinearUnit.Metre.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(Ellipsoid.WGS84.ToWktNode().ToString(), Ellipsoid.WGS84.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(PrimeMeridian.Greenwich.ToWktNode().ToString(), PrimeMeridian.Greenwich.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(HorizontalDatum.WGS84.ToWktNode().ToString(), HorizontalDatum.WGS84.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(VerticalDatum.ODN.ToWktNode().ToString(), VerticalDatum.ODN.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(axis.ToWktNode().ToString(), axis.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(projection.ToWktNode().ToString(), projection.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(parameter.ToWktNode().ToString(), parameter.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(wgs84.ToWktNode().ToString(), wgs84.ToWktNode(WktVersion.Wkt1).ToString());
    }

    /// <summary>
    /// Verifies that <c>LinearUnit.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the unit.
    /// </summary>
    [Fact]
    public void LinearUnit_ToWktNode_MatchesWkt()
    {
        LinearUnit unit = LinearUnit.Metre;
        var node = unit.ToWktNode();
        Assert.Equal(unit.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>Ellipsoid.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the ellipsoid.
    /// </summary>
    [Fact]
    public void Ellipsoid_ToWktNode_MatchesWkt()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        var node = ellipsoid.ToWktNode();
        Assert.Equal(ellipsoid.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>PrimeMeridian.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the prime meridian.
    /// </summary>
    [Fact]
    public void PrimeMeridian_ToWktNode_MatchesWkt()
    {
        PrimeMeridian pm = PrimeMeridian.Greenwich;
        var node = pm.ToWktNode();
        Assert.Equal(pm.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>HorizontalDatum.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the datum.
    /// </summary>
    [Fact]
    public void HorizontalDatum_ToWktNode_MatchesWkt()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;
        var node = datum.ToWktNode();
        Assert.Equal(datum.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>HorizontalDatum.ToWktNode()</c> correctly encodes Bursa-Wolf TOWGS84 parameters when
    /// they are present, using the ED50 datum as the test case.
    /// </summary>
    [Fact]
    public void HorizontalDatum_ToWktNode_WithWgs84Parameters_MatchesWkt()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;
        var node = datum.ToWktNode();
        Assert.Equal(datum.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>VerticalDatum.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the datum.
    /// </summary>
    [Fact]
    public void VerticalDatum_ToWktNode_MatchesWkt()
    {
        VerticalDatum datum = VerticalDatum.ODN;
        var node = datum.ToWktNode();
        Assert.Equal(datum.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>AxisInfo.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property for a named east-oriented axis.
    /// </summary>
    [Fact]
    public void AxisInfo_ToWktNode_MatchesWkt()
    {
        var axis = new AxisInfo("Lon", AxisOrientationEnum.East);
        var node = axis.ToWktNode();
        Assert.Equal(axis.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>AxisInfo.ToWktNode()</c> produces a node matching the <c>WKT</c> property for every
    /// defined <see cref="AxisOrientationEnum"/> value.
    /// </summary>
    [Fact]
    public void AxisInfo_ToWktNode_AllOrientations()
    {
        AxisOrientationEnum[] orientations = new[]
        {
            AxisOrientationEnum.North,
            AxisOrientationEnum.South,
            AxisOrientationEnum.East,
            AxisOrientationEnum.West,
            AxisOrientationEnum.Up,
            AxisOrientationEnum.Down,
            AxisOrientationEnum.Other,
        };

        foreach (AxisOrientationEnum orientation in orientations)
        {
            var axis = new AxisInfo("Test", orientation);
            var node = axis.ToWktNode();
            Assert.Equal(axis.WKT, node.ToString());
        }
    }

    /// <summary>
    /// Verifies that <c>ProjectionParameter.ToWktNode()</c> produces a node whose compact string representation
    /// matches the <c>WKT</c> property of the parameter.
    /// </summary>
    [Fact]
    public void ProjectionParameter_ToWktNode_MatchesWkt()
    {
        var param = new ProjectionParameter("central_meridian", 15.0);
        var node = param.ToWktNode();
        Assert.Equal(param.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>Wgs84ConversionInfo.ToWktNode()</c> produces a node matching the <c>WKT</c> property
    /// for a non-zero Bursa-Wolf parameter set.
    /// </summary>
    [Fact]
    public void Wgs84ConversionInfo_ToWktNode_MatchesWkt()
    {
        var info = new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0);
        var node = info.ToWktNode();
        Assert.Equal(info.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>Wgs84ConversionInfo.ToWktNode()</c> produces a node matching the <c>WKT</c> property
    /// when all Bursa-Wolf parameters are zero.
    /// </summary>
    [Fact]
    public void Wgs84ConversionInfo_ToWktNode_AllZeros_MatchesWkt()
    {
        var info = new Wgs84ConversionInfo();
        var node = info.ToWktNode();
        Assert.Equal(info.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>GeographicCoordinateSystem.ToWktNode()</c> produces a node whose compact string
    /// representation matches the <c>WKT</c> property for the WGS84 geographic coordinate system.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_MatchesWkt()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var node = gcs.ToWktNode();
        Assert.Equal(gcs.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>ProjectedCoordinateSystem.ToWktNode()</c> produces a node whose compact string
    /// representation matches the <c>WKT</c> property for the Web Mercator projection.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_ToWktNode_MatchesWkt()
    {
        ProjectedCoordinateSystem pcs = ProjectedCoordinateSystem.WebMercator;
        var node = pcs.ToWktNode();
        Assert.Equal(pcs.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>ProjectedCoordinateSystem.ToWktNode()</c> produces a node whose compact string
    /// representation matches the <c>WKT</c> property for a WGS84 UTM projected coordinate system.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_UTM_ToWktNode_MatchesWkt()
    {
        var pcs = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        var node = pcs.ToWktNode();
        Assert.Equal(pcs.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>VerticalCoordinateSystem.ToWktNode()</c> produces a node whose compact string
    /// representation matches the <c>WKT</c> property of the coordinate system.
    /// </summary>
    [Fact]
    public void VerticalCoordinateSystem_ToWktNode_MatchesWkt()
    {
        VerticalCoordinateSystem vcs = VerticalCoordinateSystem.ODN;
        var node = vcs.ToWktNode();
        Assert.Equal(vcs.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <c>GeocentricCoordinateSystem.ToWktNode()</c> produces a node whose compact string
    /// representation matches the <c>WKT</c> property of the coordinate system.
    /// </summary>
    [Fact]
    public void GeocentricCoordinateSystem_ToWktNode_MatchesWkt()
    {
        GeocentricCoordinateSystem gcc = GeocentricCoordinateSystem.WGS84;
        var node = gcc.ToWktNode();
        Assert.Equal(gcc.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that coordinate systems keep their existing WKT1 node output when routed through the versioned API.
    /// </summary>
    [Fact]
    public void CoordinateSystems_ToWktNode_WithWkt1Version_MatchesParameterlessOutput()
    {
        GeographicCoordinateSystem geographic = GeographicCoordinateSystem.WGS84;
        ProjectedCoordinateSystem projected = ProjectedCoordinateSystem.WebMercator;
        VerticalCoordinateSystem vertical = VerticalCoordinateSystem.ODN;
        GeocentricCoordinateSystem geocentric = GeocentricCoordinateSystem.WGS84;
        CompoundCoordinateSystem compound = new CoordinateSystemFactory().CreateCompoundCoordinateSystem("WGS84 + ODN", geographic, vertical);

        Assert.Equal(geographic.ToWktNode().ToString(), geographic.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(projected.ToWktNode().ToString(), projected.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(vertical.ToWktNode().ToString(), vertical.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(geocentric.ToWktNode().ToString(), geocentric.ToWktNode(WktVersion.Wkt1).ToString());
        Assert.Equal(compound.ToWktNode().ToString(), compound.ToWktNode(WktVersion.Wkt1).ToString());
    }

    /// <summary>
    /// Verifies that <c>GEOGCRS</c> WKT2 output for WGS84 roundtrips through the native WKT2 reader without changing parameters.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsWgs84()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem original = GeographicCoordinateSystem.WGS84;

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        GeographicCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(factory, wkt);

        Assert.StartsWith("GEOGCRS[", wkt, StringComparison.Ordinal);
        Assert.DoesNotContain("PRIMEM[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 <c>GEOGCRS</c> output preserves non-Greenwich prime meridians and roundtrips through the reader.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCustomPrimeMeridian()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem original = factory.CreateGeographicCoordinateSystem(
            "Custom Paris geographic",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Paris,
            new AxisInfo("Lat", AxisOrientationEnum.North),
            new AxisInfo("Lon", AxisOrientationEnum.East));

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        GeographicCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(factory, wkt);

        Assert.Contains("PRIMEM[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that datum shifts encoded as WGS84 conversion parameters are rejected until a dedicated WKT2 <c>BOUNDCRS</c> writer exists.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_WithWkt22019AndWgs84Parameters_ThrowsNotSupportedException()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem original = factory.CreateGeographicCoordinateSystem(
            "ED50 test",
            AngularUnit.Degrees,
            HorizontalDatum.ED50,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => original.ToWktNode(WktVersion.Wkt22019));

        Assert.Contains("BOUNDCRS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that WKT2 <c>PROJCRS</c> output roundtrips a transverse Mercator projected CRS through the native WKT2 reader.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsTransverseMercator()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem geographic = factory.CreateGeographicCoordinateSystem(
            "WGS 84",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Geodetic latitude (Lat)", AxisOrientationEnum.North),
            new AxisInfo("Geodetic longitude (Lon)", AxisOrientationEnum.East));
        IProjection projection = factory.CreateProjection(
            "UTM zone 33N",
            "Transverse_Mercator",
            new List<ProjectionParameter>
            {
                new("latitude_of_origin", 0),
                new("central_meridian", 15),
                new("scale_factor", 0.9996),
                new("false_easting", 500000),
                new("false_northing", 0),
            });
        ProjectedCoordinateSystem original = factory.CreateProjectedCoordinateSystem(
            "WGS 84 / UTM zone 33N",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(factory, wkt);

        Assert.StartsWith("PROJCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("BASEGEOGCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("METHOD[\"Transverse Mercator\"]", wkt, StringComparison.Ordinal);
        Assert.DoesNotContain("GEOGCS[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 <c>PROJCRS</c> output uses method-specific false-origin parameter names for Lambert Conic Conformal (2SP).
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_ToWktNode_WithWkt22019_UsesLambert2SpFalseOriginParameterNames()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem geographic = factory.CreateGeographicCoordinateSystem(
            "BD72 geographic",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Geodetic latitude (Lat)", AxisOrientationEnum.North),
            new AxisInfo("Geodetic longitude (Lon)", AxisOrientationEnum.East));
        IProjection projection = factory.CreateProjection(
            "Belgian Lambert 72",
            "Lambert_Conformal_Conic_2SP",
            new List<ProjectionParameter>
            {
                new("latitude_of_origin", 90),
                new("central_meridian", 4.36748666666694),
                new("standard_parallel_1", 51.1666672333336),
                new("standard_parallel_2", 49.8333339000003),
                new("false_easting", 150000.013),
                new("false_northing", 5400088.438),
            });
        ProjectedCoordinateSystem original = factory.CreateProjectedCoordinateSystem(
            "BD72 / Belgian Lambert 72",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(factory, wkt);

        Assert.Contains("METHOD[\"Lambert Conic Conformal (2SP)\"]", wkt, StringComparison.Ordinal);
        Assert.Contains("PARAMETER[\"Latitude of false origin\"", wkt, StringComparison.Ordinal);
        Assert.Contains("PARAMETER[\"Longitude of false origin\"", wkt, StringComparison.Ordinal);
        Assert.Contains("PARAMETER[\"Easting at false origin\"", wkt, StringComparison.Ordinal);
        Assert.Contains("PARAMETER[\"Northing at false origin\"", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 <c>PROJCRS</c> output preserves non-Greenwich prime meridians and non-degree parameter units.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCustomPrimeMeridianAndGradParameters()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem geographic = factory.CreateGeographicCoordinateSystem(
            "NTF (Paris)",
            AngularUnit.Grad,
            HorizontalDatum.WGS84,
            PrimeMeridian.Paris,
            new AxisInfo("Geodetic latitude (Lat)", AxisOrientationEnum.North),
            new AxisInfo("Geodetic longitude (Lon)", AxisOrientationEnum.East));
        IProjection projection = factory.CreateProjection(
            "Lambert Nord France",
            "Lambert_Conformal_Conic_1SP",
            new List<ProjectionParameter>
            {
                new("latitude_of_origin", 55),
                new("central_meridian", 0),
                new("scale_factor", 0.999877341),
                new("false_easting", 600000),
                new("false_northing", 200000),
            });
        ProjectedCoordinateSystem original = factory.CreateProjectedCoordinateSystem(
            "NTF (Paris) / Lambert Nord France",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(factory, wkt);

        Assert.Contains("PRIMEM[\"Paris\"", wkt, StringComparison.Ordinal);
        Assert.Contains("ANGLEUNIT[\"grad\"", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 <c>PROJCRS</c> output rejects projected CRSs whose base datum would require a dedicated <c>BOUNDCRS</c> writer.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_ToWktNode_WithWkt22019AndWgs84Parameters_ThrowsNotSupportedException()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem geographic = factory.CreateGeographicCoordinateSystem(
            "ED50 test",
            AngularUnit.Degrees,
            HorizontalDatum.ED50,
            PrimeMeridian.Greenwich,
            new AxisInfo("Geodetic latitude (Lat)", AxisOrientationEnum.North),
            new AxisInfo("Geodetic longitude (Lon)", AxisOrientationEnum.East));
        IProjection projection = factory.CreateProjection(
            "ED50 TM",
            "Transverse_Mercator",
            new List<ProjectionParameter>
            {
                new("latitude_of_origin", 0),
                new("central_meridian", 9),
                new("scale_factor", 0.9996),
                new("false_easting", 500000),
                new("false_northing", 0),
            });
        ProjectedCoordinateSystem original = factory.CreateProjectedCoordinateSystem(
            "ED50 / TM test",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => original.ToWktNode(WktVersion.Wkt22019));

        Assert.Contains("BOUNDCRS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that WKT2 <c>GEODCRS</c> output roundtrips a geocentric coordinate system through the native WKT2 reader.
    /// </summary>
    [Fact]
    public void GeocentricCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsWgs84()
    {
        CoordinateSystemFactory factory = new();
        GeocentricCoordinateSystem original = factory.CreateGeocentricCoordinateSystem(
            "WGS 84 geocentric",
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        GeocentricCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeocentricCoordinateSystem>(factory, wkt);

        Assert.StartsWith("GEODCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("AXIS[\"X\", geocentricX]", wkt, StringComparison.Ordinal);
        Assert.Contains("AXIS[\"Y\", geocentricY]", wkt, StringComparison.Ordinal);
        Assert.Contains("AXIS[\"Z\", geocentricZ]", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 <c>VERTCRS</c> output roundtrips a vertical coordinate system through the native WKT2 reader.
    /// </summary>
    [Fact]
    public void VerticalCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsOdn()
    {
        CoordinateSystemFactory factory = new();
        VerticalCoordinateSystem original = VerticalCoordinateSystem.ODN;

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        VerticalCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(factory, wkt);

        Assert.StartsWith("VERTCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("VDATUM[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 <c>COMPOUNDCRS</c> output roundtrips a compound coordinate system through the native WKT2 reader.
    /// </summary>
    [Fact]
    public void CompoundCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsGeographicAndVerticalComponents()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem geographic = factory.CreateGeographicCoordinateSystem(
            "WGS 84",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        CompoundCoordinateSystem original = factory.CreateCompoundCoordinateSystem("WGS 84 + ODN", geographic, VerticalCoordinateSystem.ODN);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        CompoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(factory, wkt);

        Assert.StartsWith("COMPOUNDCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("GEOGCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("VERTCRS[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that WKT2 output for fitted coordinate systems remains explicitly unsupported until a derived-CRS writer exists.
    /// </summary>
    [Fact]
    public void FittedCoordinateSystem_ToWktNode_WithWkt22019_ThrowsNotSupportedException()
    {
        CoordinateSystemFactory factory = new();
        FittedCoordinateSystem original = factory.CreateFittedCoordinateSystem(
            "Fitted test",
            GeographicCoordinateSystem.WGS84,
            new AffineTransform(1, 0, 0, 0, 1, 0),
            new List<AxisInfo>
            {
                new("Lon", AxisOrientationEnum.East),
                new("Lat", AxisOrientationEnum.North),
            });

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => original.ToWktNode(WktVersion.Wkt22019));

        Assert.Contains("derived", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that a catalog geographic CRS can roundtrip through WKT1 parsing and WKT2 writing without losing parameters.
    /// </summary>
    /// <param name="srid">The EPSG SRID to validate.</param>
    [Theory]
    [InlineData(4326)]
    public void GeographicCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCatalogWkt1(int srid)
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem original = ParseCatalogWkt1<GeographicCoordinateSystem>(factory, srid);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        GeographicCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(factory, wkt);

        Assert.StartsWith("GEOGCRS[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that a catalog geocentric CRS can roundtrip through WKT1 parsing and WKT2 writing without losing parameters.
    /// </summary>
    /// <param name="srid">The EPSG SRID to validate.</param>
    [Theory]
    [InlineData(4978)]
    public void GeocentricCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCatalogWkt1(int srid)
    {
        CoordinateSystemFactory factory = new();
        GeocentricCoordinateSystem original = ParseCatalogWkt1<GeocentricCoordinateSystem>(factory, srid);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        GeocentricCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeocentricCoordinateSystem>(factory, wkt);

        Assert.StartsWith("GEODCRS[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that a catalog projected CRS can roundtrip through WKT1 parsing and WKT2 writing while preserving the projection definition.
    /// </summary>
    /// <param name="srid">The EPSG SRID to validate.</param>
    [Theory]
    [InlineData(32632)]
    public void ProjectedCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCatalogWkt1(int srid)
    {
        CoordinateSystemFactory factory = new();
        ProjectedCoordinateSystem original = ParseCatalogWkt1<ProjectedCoordinateSystem>(factory, srid);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(factory, wkt);

        Assert.StartsWith("PROJCRS[", wkt, StringComparison.Ordinal);
        AssertProjectedRoundTripEquivalent(original, parsed);
    }

    /// <summary>
    /// Verifies that a catalog vertical CRS can roundtrip through WKT1 parsing and WKT2 writing without losing parameters.
    /// </summary>
    /// <param name="srid">The EPSG SRID to validate.</param>
    [Theory]
    [InlineData(5701)]
    public void VerticalCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCatalogWkt1(int srid)
    {
        CoordinateSystemFactory factory = new();
        VerticalCoordinateSystem original = ParseCatalogWkt1<VerticalCoordinateSystem>(factory, srid);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        VerticalCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(factory, wkt);

        Assert.StartsWith("VERTCRS[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that a catalog compound CRS can roundtrip through WKT1 parsing and WKT2 writing without losing parameters.
    /// </summary>
    /// <param name="srid">The EPSG SRID to validate.</param>
    [Theory]
    [InlineData(9518)]
    public void CompoundCoordinateSystem_ToWktNode_WithWkt22019_RoundTripsCatalogWkt1(int srid)
    {
        CoordinateSystemFactory factory = new();
        CompoundCoordinateSystem original = ParseCatalogWkt1<CompoundCoordinateSystem>(factory, srid);

        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        CompoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(factory, wkt);

        Assert.StartsWith("COMPOUNDCRS[", wkt, StringComparison.Ordinal);
        Assert.True(original.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that <c>GeographicCoordinateSystem.ToWktNode()</c> returns a <see cref="WktKeywordNode"/>
    /// with the <c>GEOGCS</c> keyword and a quoted system name as its first child.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_ReturnsKeywordNode()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var node = gcs.ToWktNode();

        WktKeywordNode keywordNode = Assert.IsType<WktKeywordNode>(node);
        Assert.Equal("GEOGCS", keywordNode.Keyword);
        Assert.True(keywordNode.Children.Count > 0);

        WktQuotedString nameNode = Assert.IsType<WktQuotedString>(keywordNode.Children[0]);
        Assert.Equal("WGS 84", nameNode.Value);
    }

    /// <summary>
    /// Verifies that the <see cref="WktKeywordNode"/> returned by <c>GeographicCoordinateSystem.ToWktNode()</c>
    /// has a <c>DATUM</c> keyword node as its second child.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_ContainsDatumChild()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var keywordNode = (WktKeywordNode)gcs.ToWktNode();

        WktKeywordNode datumNode = Assert.IsType<WktKeywordNode>(keywordNode.Children[1]);
        Assert.Equal("DATUM", datumNode.Keyword);
    }

    /// <summary>
    /// Verifies that a <see cref="WktKeywordNode"/> containing another <see cref="WktKeywordNode"/> as a child
    /// correctly serializes both the outer and inner keyword nodes in the compact WKT output.
    /// </summary>
    [Fact]
    public void NestedKeywordNodes_ProduceCorrectWkt()
    {
        var authorityNode = new WktKeywordNode(
            "AUTHORITY",
            new WktQuotedString("EPSG"),
            new WktQuotedString("9102"));

        var unitNode = new WktKeywordNode(
            "UNIT",
            new WktQuotedString("degree"),
            new WktNumber(0.0174532925199433),
            authorityNode);

        string result = unitNode.ToString();
        Assert.Contains("UNIT[", result, StringComparison.Ordinal);
        Assert.Contains("AUTHORITY[\"EPSG\", \"9102\"]", result, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that <see cref="WktKeywordNode.ToFormattedString"/> retains all semantic content present in the
    /// compact representation and introduces newlines when the node tree contains nested keyword children.
    /// </summary>
    [Fact]
    public void ToFormattedString_PreservesNodeContent()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var node = (WktKeywordNode)gcs.ToWktNode();

        string compact = node.ToString();
        string formatted = node.ToFormattedString();

        Assert.Contains("GEOGCS", formatted, StringComparison.Ordinal);
        Assert.Contains("DATUM", formatted, StringComparison.Ordinal);
        Assert.Contains("WGS 84", formatted, StringComparison.Ordinal);

        // Formatted version should have newlines when there are keyword children
        Assert.Contains("\n", formatted, StringComparison.Ordinal);
    }

    private static TCoordinateSystem ParseCatalogWkt1<TCoordinateSystem>(CoordinateSystemFactory factory, int srid)
        where TCoordinateSystem : CoordinateSystem
    {
        CoordinateSystem coordinateSystem = Assert.IsAssignableFrom<CoordinateSystem>(CoordinateSystemServices.GetCoordinateSystem(srid));
        return CoordinateSystemTestHelpers.RequireCoordinateSystem<TCoordinateSystem>(factory, coordinateSystem.WKT);
    }

    private static void AssertProjectedRoundTripEquivalent(ProjectedCoordinateSystem original, ProjectedCoordinateSystem parsed)
    {
        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.Authority, parsed.Authority);
        Assert.Equal(original.AuthorityCode, parsed.AuthorityCode);
        Assert.True(original.LinearUnit.EqualParams(parsed.LinearUnit));
        Assert.True(original.Projection.EqualParams(parsed.Projection));
        Assert.True(original.GeographicCoordinateSystem.HorizontalDatum.EqualParams(parsed.GeographicCoordinateSystem.HorizontalDatum));
        Assert.True(original.GeographicCoordinateSystem.AngularUnit.EqualParams(parsed.GeographicCoordinateSystem.AngularUnit));
        Assert.True(original.GeographicCoordinateSystem.PrimeMeridian.EqualParams(parsed.GeographicCoordinateSystem.PrimeMeridian));
        Assert.Equal(original.AxisInfo.Count, parsed.AxisInfo.Count);

        for (int i = 0; i < original.AxisInfo.Count; i++)
        {
            Assert.Equal(original.AxisInfo[i].Name, parsed.AxisInfo[i].Name);
            Assert.Equal(original.AxisInfo[i].Orientation, parsed.AxisInfo[i].Orientation);
        }
    }
}
