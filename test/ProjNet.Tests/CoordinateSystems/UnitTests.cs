// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="LinearUnit"/> and <see cref="AngularUnit"/>.
/// </summary>
public class UnitTests
{
    // ---- LinearUnit predefined constants ----

    /// <summary>
    /// Verifies that the metre constant has a conversion factor of 1.
    /// </summary>
    [Fact]
    public void LinearUnit_Metre_MetersPerUnitIsOne()
    {
        Assert.Equal(1.0, LinearUnit.Metre.MetersPerUnit);
    }

    /// <summary>
    /// Verifies that the foot constant has the correct conversion factor.
    /// </summary>
    [Fact]
    public void LinearUnit_Foot_MetersPerUnitIsCorrect()
    {
        Assert.Equal(0.3048, LinearUnit.Foot.MetersPerUnit, 12);
    }

    /// <summary>
    /// Verifies that the US survey foot constant has the correct conversion factor.
    /// </summary>
    [Fact]
    public void LinearUnit_USSurveyFoot_MetersPerUnitIsCorrect()
    {
        Assert.Equal(0.304800609601219, LinearUnit.USSurveyFoot.MetersPerUnit, 12);
    }

    /// <summary>
    /// Verifies that the nautical mile constant has a conversion factor of 1852.
    /// </summary>
    [Fact]
    public void LinearUnit_NauticalMile_MetersPerUnitIsCorrect()
    {
        Assert.Equal(1852.0, LinearUnit.NauticalMile.MetersPerUnit);
    }

    /// <summary>
    /// Verifies that Clarke's foot constant has the correct conversion factor.
    /// </summary>
    [Fact]
    public void LinearUnit_ClarkesFoot_MetersPerUnitIsCorrect()
    {
        Assert.Equal(0.3047972654, LinearUnit.ClarkesFoot.MetersPerUnit, 12);
    }

    // ---- LinearUnit construction ----

    /// <summary>
    /// Verifies that the constructor sets MetersPerUnit.
    /// </summary>
    [Fact]
    public void LinearUnit_Constructor_SetsMetersPerUnit()
    {
        var unit = new LinearUnit(0.9144, "yard", "EPSG", 9096, "yd", string.Empty, string.Empty);

        Assert.Equal(0.9144, unit.MetersPerUnit, 12);
    }

    // ---- LinearUnit WKT ----

    /// <summary>
    /// Verifies that WKT output contains unit name and conversion factor.
    /// </summary>
    [Fact]
    public void LinearUnit_WKT_ContainsNameAndMetersPerUnit()
    {
        string wkt = LinearUnit.Metre.WKT;

        Assert.Contains("UNIT[", wkt, StringComparison.Ordinal);
        Assert.Contains("\"metre\"", wkt, StringComparison.Ordinal);
        Assert.Contains("1", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT output includes authority when set.
    /// </summary>
    [Fact]
    public void LinearUnit_WKT_WithAuthority_ContainsAuthority()
    {
        string wkt = LinearUnit.Metre.WKT;

        Assert.Contains("AUTHORITY[\"EPSG\", \"9001\"]", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT output omits authority when not set.
    /// </summary>
    [Fact]
    public void LinearUnit_WKT_WithoutAuthority_OmitsAuthority()
    {
        var unit = new LinearUnit(1.0, "test", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        string wkt = unit.WKT;

        Assert.DoesNotContain("AUTHORITY", wkt, StringComparison.Ordinal);
    }

    // ---- LinearUnit XML ----

    /// <summary>
    /// Verifies that XML output contains the MetersPerUnit attribute.
    /// </summary>
    [Fact]
    public void LinearUnit_XML_ContainsMetersPerUnit()
    {
        string xml = LinearUnit.Metre.XML;

        Assert.Contains("CS_LinearUnit", xml, StringComparison.Ordinal);
        Assert.Contains("MetersPerUnit=\"1\"", xml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that <see cref="LinearUnit.ToXml"/> matches the XML property.
    /// </summary>
    [Fact]
    public void LinearUnit_ToXml_MatchesXmlProperty()
    {
        LinearUnit unit = LinearUnit.USSurveyFoot;
        XElement element = unit.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(unit.XML), element));
    }

    /// <summary>
    /// Verifies that the linear unit WKT node includes authority information when it is available.
    /// </summary>
    [Fact]
    public void LinearUnit_ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        LinearUnit unit = LinearUnit.Metre;
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(unit.ToWktNode());

        Assert.Equal("UNIT", node.Keyword);
        Assert.Equal(3, node.Children.Count);
        Assert.Equal("AUTHORITY", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that the linear unit WKT node omits authority information when it is unavailable.
    /// </summary>
    [Fact]
    public void LinearUnit_ToWktNode_WithoutAuthority_OmitsAuthorityNode()
    {
        LinearUnit unit = new(1.0, "test", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(unit.ToWktNode());

        Assert.Equal(2, node.Children.Count);
    }

    // ---- LinearUnit EqualParams ----

    /// <summary>
    /// Verifies that EqualParams returns true for units with the same conversion factor.
    /// </summary>
    [Fact]
    public void LinearUnit_EqualParams_SameMetersPerUnit_ReturnsTrue()
    {
        var a = new LinearUnit(1.0, "metre", "EPSG", 9001, string.Empty, string.Empty, string.Empty);
        var b = new LinearUnit(1.0, "meter", "OTHER", 1, string.Empty, string.Empty, string.Empty);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for units with different conversion factors.
    /// </summary>
    [Fact]
    public void LinearUnit_EqualParams_DifferentMetersPerUnit_ReturnsFalse()
    {
        Assert.False(LinearUnit.Metre.EqualParams(LinearUnit.Foot));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void LinearUnit_EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(LinearUnit.Metre.EqualParams("not a unit"));
    }

    // ---- AngularUnit predefined constants ----

    /// <summary>
    /// Verifies that the radian constant has a conversion factor of 1.
    /// </summary>
    [Fact]
    public void AngularUnit_Radian_RadiansPerUnitIsOne()
    {
        Assert.Equal(1.0, AngularUnit.Radian.RadiansPerUnit);
    }

    /// <summary>
    /// Verifies that the degree constant has the correct conversion factor.
    /// </summary>
    [Fact]
    public void AngularUnit_Degrees_RadiansPerUnitIsCorrect()
    {
        Assert.Equal(Math.PI / 180.0, AngularUnit.Degrees.RadiansPerUnit, 15);
    }

    /// <summary>
    /// Verifies that the grad constant has the correct conversion factor.
    /// </summary>
    [Fact]
    public void AngularUnit_Grad_RadiansPerUnitIsCorrect()
    {
        Assert.Equal(Math.PI / 200.0, AngularUnit.Grad.RadiansPerUnit, 15);
    }

    /// <summary>
    /// Verifies that the gon constant has the correct conversion factor (equal to grad).
    /// </summary>
    [Fact]
    public void AngularUnit_Gon_RadiansPerUnitEqualsGrad()
    {
        Assert.Equal(AngularUnit.Grad.RadiansPerUnit, AngularUnit.Gon.RadiansPerUnit);
    }

    // ---- AngularUnit construction ----

    /// <summary>
    /// Verifies that the public constructor sets RadiansPerUnit.
    /// </summary>
    [Fact]
    public void AngularUnit_Constructor_SetsRadiansPerUnit()
    {
        var unit = new AngularUnit(0.5);

        Assert.Equal(0.5, unit.RadiansPerUnit);
    }

    // ---- AngularUnit WKT ----

    /// <summary>
    /// Verifies that WKT output contains unit name and radians per unit.
    /// </summary>
    [Fact]
    public void AngularUnit_WKT_ContainsNameAndRadiansPerUnit()
    {
        string wkt = AngularUnit.Degrees.WKT;

        Assert.Contains("UNIT[", wkt, StringComparison.Ordinal);
        Assert.Contains("\"degree\"", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT output includes authority when set.
    /// </summary>
    [Fact]
    public void AngularUnit_WKT_WithAuthority_ContainsAuthority()
    {
        string wkt = AngularUnit.Degrees.WKT;

        Assert.Contains("AUTHORITY[\"EPSG\", \"9102\"]", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT output omits authority for a simple instance.
    /// </summary>
    [Fact]
    public void AngularUnit_WKT_PublicConstructor_OmitsAuthority()
    {
        var unit = new AngularUnit(0.5);
        string wkt = unit.WKT;

        Assert.DoesNotContain("AUTHORITY", wkt, StringComparison.Ordinal);
    }

    // ---- AngularUnit XML ----

    /// <summary>
    /// Verifies that XML output contains the RadiansPerUnit attribute.
    /// </summary>
    [Fact]
    public void AngularUnit_XML_ContainsRadiansPerUnit()
    {
        string xml = AngularUnit.Radian.XML;

        Assert.Contains("CS_AngularUnit", xml, StringComparison.Ordinal);
        Assert.Contains("RadiansPerUnit=\"1\"", xml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that <see cref="AngularUnit.ToXml"/> matches the XML property.
    /// </summary>
    [Fact]
    public void AngularUnit_ToXml_MatchesXmlProperty()
    {
        AngularUnit unit = AngularUnit.Grad;
        XElement element = unit.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(unit.XML), element));
    }

    /// <summary>
    /// Verifies that the angular unit WKT node includes authority information when it is available.
    /// </summary>
    [Fact]
    public void AngularUnit_ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        AngularUnit unit = AngularUnit.Degrees;
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(unit.ToWktNode());

        Assert.Equal("UNIT", node.Keyword);
        Assert.Equal(3, node.Children.Count);
        Assert.Equal("AUTHORITY", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that the angular unit WKT node omits authority information when it is unavailable.
    /// </summary>
    [Fact]
    public void AngularUnit_ToWktNode_WithoutAuthority_OmitsAuthorityNode()
    {
        AngularUnit unit = new(0.5);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(unit.ToWktNode());

        Assert.Equal(2, node.Children.Count);
    }

    // ---- AngularUnit EqualParams ----

    /// <summary>
    /// Verifies that EqualParams returns true for units with the same radians per unit.
    /// </summary>
    [Fact]
    public void AngularUnit_EqualParams_SameRadiansPerUnit_ReturnsTrue()
    {
        var a = new AngularUnit(Math.PI / 180.0);

        Assert.True(AngularUnit.Degrees.EqualParams(a));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for units with different radians per unit.
    /// </summary>
    [Fact]
    public void AngularUnit_EqualParams_DifferentRadiansPerUnit_ReturnsFalse()
    {
        Assert.False(AngularUnit.Degrees.EqualParams(AngularUnit.Radian));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void AngularUnit_EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(AngularUnit.Degrees.EqualParams("not a unit"));
    }

    /// <summary>
    /// Verifies that EqualParams returns false when comparing AngularUnit to LinearUnit.
    /// </summary>
    [Fact]
    public void AngularUnit_EqualParams_LinearUnit_ReturnsFalse()
    {
        Assert.False(AngularUnit.Radian.EqualParams(LinearUnit.Metre));
    }

    // ---- Generic Unit ----

    /// <summary>
    /// Verifies that the full constructor sets conversion factor and metadata.
    /// </summary>
    [Fact]
    public void Unit_Constructor_SetsConversionFactorAndMetadata()
    {
        var unit = new Unit(2.5, "custom", "TEST", 42, "alias", "abbr", "remarks");

        Assert.Equal(2.5, unit.ConversionFactor);
        Assert.Equal("custom", unit.Name);
        Assert.Equal("TEST", unit.Authority);
        Assert.Equal(42, unit.AuthorityCode);
        Assert.Equal("alias", unit.Alias);
        Assert.Equal("abbr", unit.Abbreviation);
        Assert.Equal("remarks", unit.Remarks);
    }

    /// <summary>
    /// Verifies that the simplified constructor sets name and conversion factor.
    /// </summary>
    [Fact]
    public void Unit_SimpleConstructor_SetsNameAndConversionFactor()
    {
        var unit = new Unit("fathom", 1.8288);

        Assert.Equal("fathom", unit.Name);
        Assert.Equal(1.8288, unit.ConversionFactor, 12);
    }

    /// <summary>
    /// Verifies that WKT output includes authority information when present.
    /// </summary>
    [Fact]
    public void Unit_WKT_WithAuthority_ContainsAuthority()
    {
        var unit = new Unit(2.5, "custom", "TEST", 42, string.Empty, string.Empty, string.Empty);
        string wkt = unit.WKT;

        Assert.Contains("UNIT[\"custom\", 2.5", wkt, StringComparison.Ordinal);
        Assert.Contains("AUTHORITY[\"TEST\", \"42\"]", wkt, StringComparison.Ordinal);
        Assert.Equal(wkt, unit.ToString());
    }

    /// <summary>
    /// Verifies that WKT output omits authority information when not set.
    /// </summary>
    [Fact]
    public void Unit_WKT_WithoutAuthority_OmitsAuthority()
    {
        var unit = new Unit("custom", 2.5);
        string wkt = unit.WKT;

        Assert.DoesNotContain("AUTHORITY", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the generic unit WKT node includes authority information when it is available.
    /// </summary>
    [Fact]
    public void Unit_ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        Unit unit = new(2.5, "custom", "TEST", 42, string.Empty, string.Empty, string.Empty);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(unit.ToWktNode());

        Assert.Equal("UNIT", node.Keyword);
        Assert.Equal(3, node.Children.Count);
        Assert.Equal("AUTHORITY", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that the generic unit WKT node omits authority information when it is unavailable.
    /// </summary>
    [Fact]
    public void Unit_ToWktNode_WithoutAuthority_OmitsAuthorityNode()
    {
        Unit unit = new("custom", 2.5);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(unit.ToWktNode());

        Assert.Equal(2, node.Children.Count);
    }

    /// <summary>
    /// Verifies that XML serialization is not implemented for generic units.
    /// </summary>
    [Fact]
    public void Unit_XML_ThrowsNotImplementedException()
    {
        var unit = new Unit("custom", 2.5);

        Assert.Throws<NotImplementedException>(() => _ = unit.XML);
    }

    /// <summary>
    /// Verifies that XML element serialization is not implemented for generic units.
    /// </summary>
    [Fact]
    public void Unit_ToXml_ThrowsNotImplementedException()
    {
        var unit = new Unit("custom", 2.5);

        Assert.Throws<NotImplementedException>(() => unit.ToXml());
    }

    /// <summary>
    /// Verifies that EqualParams returns true for generic units with the same conversion factor.
    /// </summary>
    [Fact]
    public void Unit_EqualParams_SameConversionFactor_ReturnsTrue()
    {
        var a = new Unit(2.5, "custom-a", "TEST", 1, string.Empty, string.Empty, string.Empty);
        var b = new Unit(2.5, "custom-b", "OTHER", 2, string.Empty, string.Empty, string.Empty);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for generic units with different conversion factors.
    /// </summary>
    [Fact]
    public void Unit_EqualParams_DifferentConversionFactor_ReturnsFalse()
    {
        var a = new Unit("custom-a", 2.5);
        var b = new Unit("custom-b", 3.5);

        Assert.False(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void Unit_EqualParams_DifferentType_ReturnsFalse()
    {
        var unit = new Unit("custom", 2.5);

        Assert.False(unit.EqualParams("not a unit"));
    }
}
