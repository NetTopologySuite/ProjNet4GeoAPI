// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="Info"/>.
/// </summary>
public class InfoTests
{
    /// <summary>
    /// Verifies that the constructor stores the supplied metadata and <see cref="Info.ToString"/> returns WKT.
    /// </summary>
    [Fact]
    public void Constructor_SetsMetadataAndToStringReturnsWkt()
    {
        var info = new TestInfo(
            "WGS 84",
            "EPSG",
            4326,
            "alias",
            "abbr",
            "remarks",
            "GEOGCS[\"WGS 84\"]",
            "<Info />");

        Assert.Equal("WGS 84", info.Name);
        Assert.Equal("EPSG", info.Authority);
        Assert.Equal(4326, info.AuthorityCode);
        Assert.Equal("alias", info.Alias);
        Assert.Equal("abbr", info.Abbreviation);
        Assert.Equal("remarks", info.Remarks);
        Assert.Equal("GEOGCS[\"WGS 84\"]", info.ToString());
    }

    /// <summary>
    /// Verifies that the mutable metadata properties can be updated after construction.
    /// </summary>
    [Fact]
    public void Properties_CanBeUpdated()
    {
        var info = new TestInfo(
            "Initial",
            "AUTH",
            1,
            "alias",
            "abbr",
            "remarks",
            "WKT",
            "XML")
        {
            Name = "Updated",
            Authority = "EPSG",
            AuthorityCode = 3857,
            Alias = "webmerc",
            Abbreviation = "wm",
            Remarks = "updated remarks",
        };

        Assert.Equal("Updated", info.Name);
        Assert.Equal("EPSG", info.Authority);
        Assert.Equal(3857, info.AuthorityCode);
        Assert.Equal("webmerc", info.Alias);
        Assert.Equal("wm", info.Abbreviation);
        Assert.Equal("updated remarks", info.Remarks);
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXml"/> includes the supported metadata attributes in the expected order.
    /// </summary>
    [Fact]
    public void InfoXml_WithMetadata_IncludesExpectedAttributes()
    {
        var info = new TestInfo(
            "WGS 84",
            "EPSG",
            4326,
            "alias",
            "abbr",
            "remarks",
            "WKT",
            "XML");

        Assert.Equal(
            "<CS_Info AuthorityCode=\"4326\" Abbreviation=\"abbr\" Authority=\"EPSG\" Name=\"WGS 84\"/>",
            info.InfoXml);
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXml"/> omits optional attributes when the values are blank or not positive.
    /// </summary>
    [Fact]
    public void InfoXml_WithBlankMetadata_OmitsOptionalAttributes()
    {
        var info = new TestInfo(
            " ",
            "\t",
            0,
            "alias",
            string.Empty,
            "remarks",
            "WKT",
            "XML");

        Assert.Equal("<CS_Info/>", info.InfoXml);
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXmlElement"/> includes the expected attributes and values.
    /// </summary>
    [Fact]
    public void InfoXmlElement_WithMetadata_IncludesExpectedAttributes()
    {
        var info = new TestInfo(
            "WGS 84",
            "EPSG",
            4326,
            "alias",
            "abbr",
            "remarks",
            "WKT",
            "XML");

        XElement xml = info.InfoXmlElement;

        Assert.Equal("CS_Info", xml.Name.LocalName);
        Assert.Equal("4326", (string?)xml.Attribute("AuthorityCode"));
        Assert.Equal("abbr", (string?)xml.Attribute("Abbreviation"));
        Assert.Equal("EPSG", (string?)xml.Attribute("Authority"));
        Assert.Equal("WGS 84", (string?)xml.Attribute("Name"));
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXmlElement"/> omits optional attributes when the values are blank or not positive.
    /// </summary>
    [Fact]
    public void InfoXmlElement_WithBlankMetadata_OmitsOptionalAttributes()
    {
        var info = new TestInfo(
            " ",
            "\t",
            -1,
            "alias",
            string.Empty,
            "remarks",
            "WKT",
            "XML");

        XElement xml = info.InfoXmlElement;

        Assert.Empty(xml.Attributes());
    }

    private sealed class TestInfo : Info
    {
        private readonly string wkt;
        private readonly string xml;

        internal TestInfo(
            string name,
            string authority,
            long code,
            string alias,
            string abbreviation,
            string remarks,
            string wkt,
            string xml)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            this.wkt = wkt;
            this.xml = xml;
        }

        public override string WKT => this.wkt;

        public override string XML => this.xml;

        public override bool EqualParams(object obj) => ReferenceEquals(this, obj);
    }
}
