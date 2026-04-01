// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="PrimeMeridian"/>.
/// </summary>
public class PrimeMeridianTests
{
    /// <summary>
    /// Verifies that the built-in prime meridians expose the expected metadata.
    /// </summary>
    /// <param name="key">Well-known prime meridian key.</param>
    /// <param name="expectedName">Expected meridian name.</param>
    /// <param name="expectedAuthorityCode">Expected authority code.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    [Theory]
    [InlineData("Greenwich", "Greenwich", 8901L, 0.0)]
    [InlineData("Lisbon", "Lisbon", 8902L, -9.0754862)]
    [InlineData("Paris", "Paris", 8903L, 2.5969213)]
    [InlineData("Bogota", "Bogota", 8904L, -74.04513)]
    [InlineData("Madrid", "Madrid", 8905L, -3.411658)]
    [InlineData("Rome", "Rome", 8906L, 12.27084)]
    [InlineData("Bern", "Bern", 8907L, 7.26225)]
    [InlineData("Jakarta", "Jakarta", 8908L, 106.482779)]
    [InlineData("Ferro", "Ferro", 8909L, -17.66666666666667)]
    [InlineData("Brussels", "Brussels", 8910L, 4.220471)]
    [InlineData("Stockholm", "Stockholm", 8911L, 18.03298)]
    [InlineData("Athens", "Athens", 8912L, 23.4258815)]
    [InlineData("Oslo", "Oslo", 8913L, 10.43225)]
    public void KnownPrimeMeridians_ExposeExpectedMetadata(
        string key,
        string expectedName,
        long expectedAuthorityCode,
        double expectedLongitude)
    {
        PrimeMeridian meridian = GetKnownPrimeMeridian(key);

        Assert.Equal(expectedName, meridian.Name);
        Assert.Equal("EPSG", meridian.Authority);
        Assert.Equal(expectedAuthorityCode, meridian.AuthorityCode);
        Assert.Equal(expectedLongitude, meridian.Longitude, 12);
        Assert.True(meridian.AngularUnit.EqualParams(AngularUnit.Degrees));
    }

    /// <summary>
    /// Verifies that the constructor assigns all properties.
    /// </summary>
    [Fact]
    public void Constructor_SetsProperties()
    {
        var meridian = new PrimeMeridian(1.25, AngularUnit.Grad, "Custom", "AUTH", 42, "alias", "abbr", "remarks");

        Assert.Equal(1.25, meridian.Longitude, 12);
        Assert.True(meridian.AngularUnit.EqualParams(AngularUnit.Grad));
        Assert.Equal("Custom", meridian.Name);
        Assert.Equal("AUTH", meridian.Authority);
        Assert.Equal(42, meridian.AuthorityCode);
        Assert.Equal("alias", meridian.Alias);
        Assert.Equal("abbr", meridian.Abbreviation);
        Assert.Equal("remarks", meridian.Remarks);
    }

    /// <summary>
    /// Verifies that longitude and angular unit can be updated after construction.
    /// </summary>
    [Fact]
    public void Properties_CanBeUpdated()
    {
        var meridian = new PrimeMeridian(1.25, AngularUnit.Grad, "Custom", "AUTH", 42, string.Empty, string.Empty, string.Empty)
        {
            Longitude = 2.5,
            AngularUnit = AngularUnit.Radian,
        };

        Assert.Equal(2.5, meridian.Longitude, 12);
        Assert.True(meridian.AngularUnit.EqualParams(AngularUnit.Radian));
    }

    /// <summary>
    /// Verifies that WKT includes the authority clause when authority information is available.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_FormatsExpectedValue()
    {
        PrimeMeridian meridian = PrimeMeridian.Greenwich;

        Assert.Equal("PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]]", meridian.WKT);
    }

    /// <summary>
    /// Verifies that WKT omits the authority clause when authority information is unavailable.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthority_OmitsAuthorityClause()
    {
        var meridian = new PrimeMeridian(1.25, AngularUnit.Grad, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.Equal("PRIMEM[\"Custom\", 1.25]", meridian.WKT);
    }

    /// <summary>
    /// Verifies that XML contains the expected element name, longitude attribute, and child elements.
    /// </summary>
    [Fact]
    public void XML_ContainsExpectedStructure()
    {
        var meridian = new PrimeMeridian(1.25, AngularUnit.Grad, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var xml = XElement.Parse(meridian.XML);

        Assert.Equal("CS_PrimeMeridian", xml.Name.LocalName);
        Assert.Equal("1.25", (string?)xml.Attribute("Longitude"));
        Assert.NotNull(xml.Element("CS_Info"));
        Assert.NotNull(xml.Element("CS_AngularUnit"));
    }

    /// <summary>
    /// Verifies that <see cref="PrimeMeridian.ToXml"/> matches the XML property.
    /// </summary>
    [Fact]
    public void ToXml_MatchesXmlProperty()
    {
        PrimeMeridian meridian = PrimeMeridian.Greenwich;
        XElement element = meridian.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(meridian.XML), element));
    }

    /// <summary>
    /// Verifies that the WKT node includes authority information when available.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        PrimeMeridian meridian = PrimeMeridian.Greenwich;
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(meridian.ToWktNode());

        Assert.Equal("PRIMEM", node.Keyword);
        Assert.Equal(3, node.Children.Count);
        Assert.IsType<WktKeywordNode>(node.Children[2]);
    }

    /// <summary>
    /// Verifies that the WKT node omits authority information when it is unavailable.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthority_OmitsAuthorityNode()
    {
        var meridian = new PrimeMeridian(1.25, AngularUnit.Grad, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(meridian.ToWktNode());

        Assert.Equal(2, node.Children.Count);
    }

    /// <summary>
    /// Verifies that equality ignores metadata when longitude and angular unit match.
    /// </summary>
    [Fact]
    public void EqualParams_SameParametersDifferentMetadata_ReturnsTrue()
    {
        var first = new PrimeMeridian(1.25, AngularUnit.Grad, "First", "EPSG", 1, "a1", "abbr1", "r1");
        var second = new PrimeMeridian(1.25, AngularUnit.Grad, "Second", "OTHER", 2, "a2", "abbr2", "r2");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different longitude breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentLongitude_ReturnsFalse()
    {
        var first = new PrimeMeridian(1.25, AngularUnit.Grad, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new PrimeMeridian(2.5, AngularUnit.Grad, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different angular unit breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentAngularUnit_ReturnsFalse()
    {
        var first = new PrimeMeridian(1.25, AngularUnit.Grad, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new PrimeMeridian(1.25, AngularUnit.Degrees, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different object types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(PrimeMeridian.Greenwich.EqualParams("not a meridian"));
    }

    private static PrimeMeridian GetKnownPrimeMeridian(string key)
    {
        return key switch
        {
            "Greenwich" => PrimeMeridian.Greenwich,
            "Lisbon" => PrimeMeridian.Lisbon,
            "Paris" => PrimeMeridian.Paris,
            "Bogota" => PrimeMeridian.Bogota,
            "Madrid" => PrimeMeridian.Madrid,
            "Rome" => PrimeMeridian.Rome,
            "Bern" => PrimeMeridian.Bern,
            "Jakarta" => PrimeMeridian.Jakarta,
            "Ferro" => PrimeMeridian.Ferro,
            "Brussels" => PrimeMeridian.Brussels,
            "Stockholm" => PrimeMeridian.Stockholm,
            "Athens" => PrimeMeridian.Athens,
            "Oslo" => PrimeMeridian.Oslo,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
    }
}
