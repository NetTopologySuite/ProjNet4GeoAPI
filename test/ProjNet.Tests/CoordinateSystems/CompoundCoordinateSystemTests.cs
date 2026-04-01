// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="CompoundCoordinateSystem"/>.
/// </summary>
public class CompoundCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the constructor stores metadata, component systems, and merged axes.
    /// </summary>
    [Fact]
    public void Constructor_SetsPropertiesAndAxes()
    {
        GeographicCoordinateSystem head = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem tail = VerticalCoordinateSystem.ODN;
        var system = new CompoundCoordinateSystem(head, tail, "Custom compound", "EPSG", 9900, "alias", "abbr", "remarks");

        Assert.Equal("Custom compound", system.Name);
        Assert.Equal("EPSG", system.Authority);
        Assert.Equal(9900, system.AuthorityCode);
        Assert.Equal("alias", system.Alias);
        Assert.Equal("abbr", system.Abbreviation);
        Assert.Equal("remarks", system.Remarks);
        Assert.Same(head, system.HeadCoordinateSystem);
        Assert.Same(tail, system.TailCoordinateSystem);
        Assert.Equal(head.Dimension + tail.Dimension, system.Dimension);
        Assert.Same(head.GetAxis(0), system.GetAxis(0));
        Assert.Same(head.GetAxis(1), system.GetAxis(1));
        Assert.Same(tail.GetAxis(0), system.GetAxis(2));
    }

    /// <summary>
    /// Verifies that the head and tail component systems can be replaced after construction.
    /// </summary>
    [Fact]
    public void PropertySetters_UpdateHeadAndTailCoordinateSystems()
    {
        CompoundCoordinateSystem system = CreateSystem();
        GeocentricCoordinateSystem newHead = GeocentricCoordinateSystem.WGS84;
        VerticalCoordinateSystem newTail = CreateFootVerticalCoordinateSystem();

        system.HeadCoordinateSystem = newHead;
        system.TailCoordinateSystem = newTail;

        Assert.Same(newHead, system.HeadCoordinateSystem);
        Assert.Same(newTail, system.TailCoordinateSystem);
    }

    /// <summary>
    /// Verifies that WKT omits the authority clause when no authority metadata is available.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthority_FormatsExpectedValue()
    {
        GeographicCoordinateSystem head = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem tail = VerticalCoordinateSystem.ODN;
        var system = new CompoundCoordinateSystem(head, tail, "Custom compound", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.Equal($"COMPD_CS[\"Custom compound\",{head.WKT},{tail.WKT}]", system.WKT);
    }

    /// <summary>
    /// Verifies that WKT omits the authority clause when the authority code is not positive.
    /// </summary>
    [Fact]
    public void WKT_WithAuthorityNameButNonPositiveCode_OmitsAuthorityClause()
    {
        GeographicCoordinateSystem head = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem tail = VerticalCoordinateSystem.ODN;
        CompoundCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 0);

        Assert.Equal($"COMPD_CS[\"Custom compound\",{head.WKT},{tail.WKT}]", system.WKT);
    }

    /// <summary>
    /// Verifies that WKT includes the authority clause when authority metadata is present.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_FormatsExpectedValue()
    {
        GeographicCoordinateSystem head = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem tail = VerticalCoordinateSystem.ODN;
        var system = new CompoundCoordinateSystem(head, tail, "Custom compound", "EPSG", 9900, string.Empty, string.Empty, string.Empty);

        Assert.Equal($"COMPD_CS[\"Custom compound\",{head.WKT},{tail.WKT},AUTHORITY[\"EPSG\",\"9900\"]]", system.WKT);
    }

    /// <summary>
    /// Verifies that XML contains the expected outer and inner elements.
    /// </summary>
    [Fact]
    public void XML_ContainsExpectedStructure()
    {
        CompoundCoordinateSystem system = CreateSystem();
        var xml = XElement.Parse(system.XML);
        XElement inner = Assert.IsType<XElement>(xml.Element("CS_CompoundCoordinateSystem"));

        Assert.Equal("CS_CoordinateSystem", xml.Name.LocalName);
        Assert.Equal("3", (string?)xml.Attribute("Dimension"));
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.Equal(3, new System.Collections.Generic.List<XElement>(inner.Elements("CS_AxisInfo")).Count);
        Assert.Equal(2, new System.Collections.Generic.List<XElement>(inner.Elements("CS_CoordinateSystem")).Count);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundCoordinateSystem.ToXml"/> matches the XML property.
    /// </summary>
    [Fact]
    public void ToXml_MatchesXmlProperty()
    {
        CompoundCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 9900);
        XElement xml = system.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(system.XML), xml));
    }

    /// <summary>
    /// Verifies that GetUnits returns the head coordinate system unit for head dimensions.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GetUnits_HeadDimension_ReturnsHeadUnit(int dimension)
    {
        CompoundCoordinateSystem system = CreateSystem();

        IUnit unit = system.GetUnits(dimension);

        Assert.True(unit.EqualParams(AngularUnit.Degrees));
    }

    /// <summary>
    /// Verifies that GetUnits returns the tail coordinate system unit for tail dimensions.
    /// </summary>
    [Fact]
    public void GetUnits_TailDimension_ReturnsTailUnit()
    {
        CompoundCoordinateSystem system = CreateSystem();

        IUnit unit = system.GetUnits(2);

        Assert.True(unit.EqualParams(LinearUnit.Metre));
    }

    /// <summary>
    /// Verifies that GetUnits rejects invalid dimension indices.
    /// </summary>
    /// <param name="dimension">The invalid dimension index.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GetUnits_InvalidDimension_ThrowsArgumentException(int dimension)
    {
        CompoundCoordinateSystem system = CreateSystem();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => system.GetUnits(dimension));

        Assert.Equal("dimension", exception.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundCoordinateSystem.ToWktNode"/> matches WKT when no authority metadata is present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthority_MatchesWkt()
    {
        CompoundCoordinateSystem system = CreateSystem();
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal("COMPD_CS", node.Keyword);
        Assert.Equal(3, node.Children.Count);
        Assert.Equal("Custom compound", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
        Assert.Equal("GEOGCS", Assert.IsType<WktKeywordNode>(node.Children[1]).Keyword);
        Assert.Equal("VERT_CS", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundCoordinateSystem.ToWktNode"/> omits authority when the code is not positive.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthorityNameButNonPositiveCode_OmitsAuthorityNode()
    {
        CompoundCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 0);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal(3, node.Children.Count);
        Assert.Equal("Custom compound", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
        Assert.Equal("GEOGCS", Assert.IsType<WktKeywordNode>(node.Children[1]).Keyword);
        Assert.Equal("VERT_CS", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundCoordinateSystem.ToWktNode"/> includes an authority node when metadata is present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        CompoundCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 9900);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());
        WktKeywordNode authorityNode;

        Assert.Equal(4, node.Children.Count);
        Assert.Equal("Custom compound", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
        Assert.Equal("GEOGCS", Assert.IsType<WktKeywordNode>(node.Children[1]).Keyword);
        Assert.Equal("VERT_CS", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
        authorityNode = Assert.IsType<WktKeywordNode>(node.Children[3]);
        Assert.Equal("AUTHORITY", authorityNode.Keyword);
        Assert.Equal("EPSG", Assert.IsType<WktQuotedString>(authorityNode.Children[0]).Value);
        Assert.Equal("9900", Assert.IsType<WktQuotedString>(authorityNode.Children[1]).Value);
    }

    /// <summary>
    /// Verifies that equal compound systems compare equal.
    /// </summary>
    [Fact]
    public void EqualParams_SameValues_ReturnsTrue()
    {
        CompoundCoordinateSystem first = CreateSystem(name: "A");
        CompoundCoordinateSystem second = CreateSystem(name: "B");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that differing head coordinate systems compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentHead_ReturnsFalse()
    {
        CompoundCoordinateSystem first = CreateSystem();
        CompoundCoordinateSystem second = CreateSystem(head: GeocentricCoordinateSystem.WGS84);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that differing tail coordinate systems compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentTail_ReturnsFalse()
    {
        CompoundCoordinateSystem first = CreateSystem();
        CompoundCoordinateSystem second = CreateSystem(tail: CreateFootVerticalCoordinateSystem());

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different object types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(CreateSystem().EqualParams("not a coordinate system"));
    }

    private static CompoundCoordinateSystem CreateSystem(
        string name = "Custom compound",
        CoordinateSystem? head = null,
        CoordinateSystem? tail = null,
        string authority = "",
        long authorityCode = -1)
    {
        return new CompoundCoordinateSystem(
            head ?? GeographicCoordinateSystem.WGS84,
            tail ?? VerticalCoordinateSystem.ODN,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static VerticalCoordinateSystem CreateFootVerticalCoordinateSystem()
    {
        return new VerticalCoordinateSystem(
            LinearUnit.Foot,
            VerticalDatum.ODN,
            new AxisInfo("Up", AxisOrientationEnum.Up),
            "Foot height",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
