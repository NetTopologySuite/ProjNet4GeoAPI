// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.Wkt;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for the WKT node types and the ToWktNode methods.
/// </summary>
public class WktNodeTests
{
    [Fact]
    public void WktQuotedString_ToString_WrapsValueInQuotes()
    {
        var node = new WktQuotedString("WGS 84");
        Assert.Equal("\"WGS 84\"", node.ToString());
    }

    [Fact]
    public void WktQuotedString_Value_ReturnsUnquotedValue()
    {
        var node = new WktQuotedString("WGS 84");
        Assert.Equal("WGS 84", node.Value);
    }

    [Fact]
    public void WktNumber_ToString_FormatsPositiveInteger()
    {
        var node = new WktNumber(6378137);
        Assert.Equal("6378137", node.ToString());
    }

    [Fact]
    public void WktNumber_ToString_FormatsNegativeValue()
    {
        var node = new WktNumber(-87);
        Assert.Equal("-87", node.ToString());
    }

    [Fact]
    public void WktNumber_ToString_FormatsDecimalValue()
    {
        var node = new WktNumber(298.257223563);
        Assert.Equal("298.257223563", node.ToString());
    }

    [Fact]
    public void WktNumber_ToString_FormatsVerySmallValue()
    {
        var node = new WktNumber(0.017453292519943295);
        string result = node.ToString();
        Assert.Contains("0.017453292519943", result);
    }

    [Fact]
    public void WktNumber_ToString_FormatsZero()
    {
        var node = new WktNumber(0);
        Assert.Equal("0", node.ToString());
    }

    [Fact]
    public void WktNumber_ToString_FormatsLargeValue()
    {
        var node = new WktNumber(10000000);
        Assert.Equal("10000000", node.ToString());
    }

    [Fact]
    public void WktInteger_ToString_FormatsPositiveValue()
    {
        var node = new WktInteger(4326);
        Assert.Equal("4326", node.ToString());
    }

    [Fact]
    public void WktInteger_ToString_FormatsNegativeValue()
    {
        var node = new WktInteger(-1);
        Assert.Equal("-1", node.ToString());
    }

    [Fact]
    public void WktInteger_ToString_FormatsZero()
    {
        var node = new WktInteger(0);
        Assert.Equal("0", node.ToString());
    }

    [Fact]
    public void WktIdentifier_ToString_ReturnsNameAsIs()
    {
        var node = new WktIdentifier("NORTH");
        Assert.Equal("NORTH", node.ToString());
    }

    [Fact]
    public void WktKeywordNode_ToString_CompactFormat()
    {
        var node = new WktKeywordNode(
            "UNIT",
            new WktQuotedString("degree"),
            new WktNumber(0.0174532925199433));

        string result = node.ToString();
        Assert.StartsWith("UNIT[\"degree\", 0.017453292519943", result);
        Assert.EndsWith("]", result);
    }

    [Fact]
    public void WktKeywordNode_ToString_NestedKeywords()
    {
        var node = new WktKeywordNode(
            "AUTHORITY",
            new WktQuotedString("EPSG"),
            new WktQuotedString("4326"));

        Assert.Equal("AUTHORITY[\"EPSG\", \"4326\"]", node.ToString());
    }

    [Fact]
    public void WktKeywordNode_ToString_EmptyChildren()
    {
        var node = new WktKeywordNode("EMPTY");
        Assert.Equal("EMPTY[]", node.ToString());
    }

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
        Assert.Contains("\n", result);
        Assert.Contains("    ", result);
        Assert.Contains("AUTHORITY", result);
    }

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

    [Fact]
    public void AngularUnit_ToWktNode_MatchesWkt()
    {
        AngularUnit unit = AngularUnit.Degrees;
        var node = unit.ToWktNode();
        Assert.Equal(unit.WKT, node.ToString());
    }

    [Fact]
    public void LinearUnit_ToWktNode_MatchesWkt()
    {
        LinearUnit unit = LinearUnit.Metre;
        var node = unit.ToWktNode();
        Assert.Equal(unit.WKT, node.ToString());
    }

    [Fact]
    public void Ellipsoid_ToWktNode_MatchesWkt()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        var node = ellipsoid.ToWktNode();
        Assert.Equal(ellipsoid.WKT, node.ToString());
    }

    [Fact]
    public void PrimeMeridian_ToWktNode_MatchesWkt()
    {
        PrimeMeridian pm = PrimeMeridian.Greenwich;
        var node = pm.ToWktNode();
        Assert.Equal(pm.WKT, node.ToString());
    }

    [Fact]
    public void HorizontalDatum_ToWktNode_MatchesWkt()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;
        var node = datum.ToWktNode();
        Assert.Equal(datum.WKT, node.ToString());
    }

    [Fact]
    public void HorizontalDatum_ToWktNode_WithWgs84Parameters_MatchesWkt()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;
        var node = datum.ToWktNode();
        Assert.Equal(datum.WKT, node.ToString());
    }

    [Fact]
    public void VerticalDatum_ToWktNode_MatchesWkt()
    {
        VerticalDatum datum = VerticalDatum.ODN;
        var node = datum.ToWktNode();
        Assert.Equal(datum.WKT, node.ToString());
    }

    [Fact]
    public void AxisInfo_ToWktNode_MatchesWkt()
    {
        var axis = new AxisInfo("Lon", AxisOrientationEnum.East);
        var node = axis.ToWktNode();
        Assert.Equal(axis.WKT, node.ToString());
    }

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

    [Fact]
    public void ProjectionParameter_ToWktNode_MatchesWkt()
    {
        var param = new ProjectionParameter("central_meridian", 15.0);
        var node = param.ToWktNode();
        Assert.Equal(param.WKT, node.ToString());
    }

    [Fact]
    public void Wgs84ConversionInfo_ToWktNode_MatchesWkt()
    {
        var info = new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0);
        var node = info.ToWktNode();
        Assert.Equal(info.WKT, node.ToString());
    }

    [Fact]
    public void Wgs84ConversionInfo_ToWktNode_AllZeros_MatchesWkt()
    {
        var info = new Wgs84ConversionInfo();
        var node = info.ToWktNode();
        Assert.Equal(info.WKT, node.ToString());
    }

    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_MatchesWkt()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var node = gcs.ToWktNode();
        Assert.Equal(gcs.WKT, node.ToString());
    }

    [Fact]
    public void ProjectedCoordinateSystem_ToWktNode_MatchesWkt()
    {
        ProjectedCoordinateSystem pcs = ProjectedCoordinateSystem.WebMercator;
        var node = pcs.ToWktNode();
        Assert.Equal(pcs.WKT, node.ToString());
    }

    [Fact]
    public void ProjectedCoordinateSystem_UTM_ToWktNode_MatchesWkt()
    {
        var pcs = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        var node = pcs.ToWktNode();
        Assert.Equal(pcs.WKT, node.ToString());
    }

    [Fact]
    public void VerticalCoordinateSystem_ToWktNode_MatchesWkt()
    {
        VerticalCoordinateSystem vcs = VerticalCoordinateSystem.ODN;
        var node = vcs.ToWktNode();
        Assert.Equal(vcs.WKT, node.ToString());
    }

    [Fact]
    public void GeocentricCoordinateSystem_ToWktNode_MatchesWkt()
    {
        GeocentricCoordinateSystem gcc = GeocentricCoordinateSystem.WGS84;
        var node = gcc.ToWktNode();
        Assert.Equal(gcc.WKT, node.ToString());
    }

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

    [Fact]
    public void GeographicCoordinateSystem_ToWktNode_ContainsDatumChild()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var keywordNode = (WktKeywordNode)gcs.ToWktNode();

        WktKeywordNode datumNode = Assert.IsType<WktKeywordNode>(keywordNode.Children[1]);
        Assert.Equal("DATUM", datumNode.Keyword);
    }

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
        Assert.Contains("UNIT[", result);
        Assert.Contains("AUTHORITY[\"EPSG\", \"9102\"]", result);
    }

    [Fact]
    public void ToFormattedString_PreservesNodeContent()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var node = (WktKeywordNode)gcs.ToWktNode();

        string compact = node.ToString();
        string formatted = node.ToFormattedString();

        Assert.Contains("GEOGCS", formatted);
        Assert.Contains("DATUM", formatted);
        Assert.Contains("WGS 84", formatted);

        // Formatted version should have newlines when there are keyword children
        Assert.Contains("\n", formatted);
    }
}
