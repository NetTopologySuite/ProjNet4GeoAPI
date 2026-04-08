// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="VerticalDatum"/>.
/// </summary>
public class VerticalDatumTests
{
    /// <summary>
    /// Verifies that the predefined ODN datum exposes the expected metadata.
    /// </summary>
    [Fact]
    public void ODN_HasExpectedMetadata()
    {
        VerticalDatum datum = VerticalDatum.ODN;

        Assert.Equal("Ordnance Datum Newlyn", datum.Name);
        Assert.Equal("EPSG", datum.Authority);
        Assert.Equal(5101, datum.AuthorityCode);
        Assert.Equal(DatumType.VD_GeoidModelDerived, datum.DatumType);
    }

    /// <summary>
    /// Verifies that the constructor assigns all properties.
    /// </summary>
    [Fact]
    public void Constructor_SetsProperties()
    {
        var datum = new VerticalDatum(
            DatumType.VD_Orthometric,
            "Custom datum",
            "TEST",
            42,
            "alias",
            "remarks",
            "abbr");

        Assert.Equal(DatumType.VD_Orthometric, datum.DatumType);
        Assert.Equal("Custom datum", datum.Name);
        Assert.Equal("TEST", datum.Authority);
        Assert.Equal(42, datum.AuthorityCode);
        Assert.Equal("alias", datum.Alias);
        Assert.Equal("remarks", datum.Remarks);
        Assert.Equal("abbr", datum.Abbreviation);
    }

    /// <summary>
    /// Verifies that the datum type can be updated after construction.
    /// </summary>
    [Fact]
    public void DatumType_CanBeUpdated()
    {
        var datum = new VerticalDatum(
            DatumType.VD_Orthometric,
            "Custom datum",
            "TEST",
            42,
            string.Empty,
            string.Empty,
            string.Empty)
        {
            DatumType = DatumType.VD_Depth,
        };

        Assert.Equal(DatumType.VD_Depth, datum.DatumType);
    }

    /// <summary>
    /// Verifies that WKT includes authority information when it is available.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_FormatsExpectedValue()
    {
        VerticalDatum datum = VerticalDatum.ODN;

        Assert.Equal("VERT_DATUM[\"Ordnance Datum Newlyn\", 2005, AUTHORITY[\"EPSG\", \"5101\"]]", datum.WKT);
    }

    /// <summary>
    /// Verifies that WKT omits authority information when it is unavailable.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthority_OmitsAuthorityClause()
    {
        var datum = new VerticalDatum(
            DatumType.VD_Orthometric,
            "Custom datum",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.Equal("VERT_DATUM[\"Custom datum\", 2001]", datum.WKT);
    }

    /// <summary>
    /// Verifies that XML uses the expected element name and embedded info XML.
    /// </summary>
    [Fact]
    public void XML_FormatsExpectedValue()
    {
        VerticalDatum datum = VerticalDatum.ODN;
        var xml = XElement.Parse(datum.XML);
        XElement info = Assert.Single(xml.Elements("CS_Info"));

        Assert.Equal("CS_VerticalDatum", xml.Name.LocalName);
        Assert.Equal("2005", (string?)xml.Attribute("DatumType"));
        Assert.Equal("Ordnance Datum Newlyn", (string?)info.Attribute("Name"));
        Assert.Equal("EPSG", (string?)info.Attribute("Authority"));
        Assert.Equal("5101", (string?)info.Attribute("AuthorityCode"));
    }

    /// <summary>
    /// Verifies that <see cref="VerticalDatum.ToXml"/> returns the expected XML element.
    /// </summary>
    [Fact]
    public void ToXml_ReturnsExpectedElement()
    {
        VerticalDatum datum = VerticalDatum.ODN;

        XElement xml = datum.ToXml();

        Assert.Equal("CS_VerticalDatum", xml.Name.LocalName);
        Assert.Equal("2005", (string?)xml.Attribute("DatumType"));
        Assert.NotNull(xml.Element("CS_Info"));
    }

    /// <summary>
    /// Verifies that <see cref="VerticalDatum.ToWktNode()"/> returns the expected node structure.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_ReturnsExpectedKeywordNode()
    {
        VerticalDatum datum = VerticalDatum.ODN;

        WktKeywordNode node = Assert.IsType<WktKeywordNode>(datum.ToWktNode());

        Assert.Equal("VERT_DATUM", node.Keyword);
        Assert.Equal(3, node.Children.Count);

        WktQuotedString nameNode = Assert.IsType<WktQuotedString>(node.Children[0]);
        Assert.Equal("Ordnance Datum Newlyn", nameNode.Value);

        WktInteger typeNode = Assert.IsType<WktInteger>(node.Children[1]);
        Assert.Equal(2005, typeNode.Value);

        WktKeywordNode authorityNode = Assert.IsType<WktKeywordNode>(node.Children[2]);
        Assert.Equal("AUTHORITY", authorityNode.Keyword);
    }

    /// <summary>
    /// Verifies that the WKT node omits authority when it is unavailable.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthority_OmitsAuthorityNode()
    {
        var datum = new VerticalDatum(
            DatumType.VD_Orthometric,
            "Custom datum",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        WktKeywordNode node = Assert.IsType<WktKeywordNode>(datum.ToWktNode());

        Assert.Equal(2, node.Children.Count);
    }

    /// <summary>
    /// Verifies that equal datums compare equal when the datum type matches.
    /// </summary>
    [Fact]
    public void EqualParams_SameDatumType_ReturnsTrue()
    {
        var first = new VerticalDatum(
            DatumType.VD_Orthometric,
            "A",
            "EPSG",
            1,
            string.Empty,
            string.Empty,
            string.Empty);
        var second = new VerticalDatum(
            DatumType.VD_Orthometric,
            "B",
            "OTHER",
            2,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different datum types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentDatumType_ReturnsFalse()
    {
        var first = new VerticalDatum(
            DatumType.VD_Orthometric,
            "A",
            "EPSG",
            1,
            string.Empty,
            string.Empty,
            string.Empty);
        var second = new VerticalDatum(
            DatumType.VD_Depth,
            "B",
            "OTHER",
            2,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different object types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(VerticalDatum.ODN.EqualParams("not a datum"));
    }
}
