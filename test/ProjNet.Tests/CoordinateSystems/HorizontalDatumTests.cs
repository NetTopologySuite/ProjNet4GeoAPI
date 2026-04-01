// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="HorizontalDatum"/>.
/// </summary>
public class HorizontalDatumTests
{
    /// <summary>
    /// Verifies that the predefined WGS84 datum exposes the expected metadata.
    /// </summary>
    [Fact]
    public void WGS84_HasExpectedMetadata()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;

        Assert.Equal("World Geodetic System 1984", datum.Name);
        Assert.Equal("EPSG", datum.Authority);
        Assert.Equal(6326, datum.AuthorityCode);
        Assert.Equal(DatumType.HD_Geocentric, datum.DatumType);
        Assert.True(datum.Ellipsoid.EqualParams(Ellipsoid.WGS84));
        Assert.Null(datum.Wgs84Parameters);
    }

    /// <summary>
    /// Verifies that the predefined WGS72 datum exposes the expected metadata and Bursa-Wolf parameters.
    /// </summary>
    [Fact]
    public void WGS72_HasExpectedMetadata()
    {
        HorizontalDatum datum = HorizontalDatum.WGS72;
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(datum.Wgs84Parameters);

        Assert.Equal("World Geodetic System 1972", datum.Name);
        Assert.Equal("EPSG", datum.Authority);
        Assert.Equal(6322, datum.AuthorityCode);
        Assert.Equal(DatumType.HD_Geocentric, datum.DatumType);
        Assert.True(datum.Ellipsoid.EqualParams(Ellipsoid.WGS72));
        Assert.Equal(0d, parameters.Dx);
        Assert.Equal(0d, parameters.Dy);
        Assert.Equal(4.5d, parameters.Dz);
        Assert.Equal(0d, parameters.Ex);
        Assert.Equal(0d, parameters.Ey);
        Assert.Equal(0.554d, parameters.Ez);
        Assert.Equal(0.219d, parameters.Ppm);
    }

    /// <summary>
    /// Verifies that the predefined ETRF89 datum exposes the expected metadata and zero WGS84 parameters.
    /// </summary>
    [Fact]
    public void ETRF89_HasExpectedMetadata()
    {
        HorizontalDatum datum = HorizontalDatum.ETRF89;
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(datum.Wgs84Parameters);

        Assert.Equal("European Terrestrial Reference System 1989", datum.Name);
        Assert.Equal("EPSG", datum.Authority);
        Assert.Equal(6258, datum.AuthorityCode);
        Assert.Equal("ETRF89", datum.Alias);
        Assert.Equal(DatumType.HD_Geocentric, datum.DatumType);
        Assert.True(datum.Ellipsoid.EqualParams(Ellipsoid.GRS80));
        Assert.True(parameters.HasZeroValuesOnly);
    }

    /// <summary>
    /// Verifies that the predefined ED50 datum exposes the expected metadata and WGS84 conversion parameters.
    /// </summary>
    [Fact]
    public void ED50_HasExpectedMetadata()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(datum.Wgs84Parameters);

        Assert.Equal("European Datum 1950", datum.Name);
        Assert.Equal("EPSG", datum.Authority);
        Assert.Equal(6230, datum.AuthorityCode);
        Assert.Equal("ED50", datum.Alias);
        Assert.Equal(DatumType.HD_Geocentric, datum.DatumType);
        Assert.True(datum.Ellipsoid.EqualParams(Ellipsoid.International1924));
        Assert.Equal(-87d, parameters.Dx);
        Assert.Equal(-98d, parameters.Dy);
        Assert.Equal(-121d, parameters.Dz);
    }

    /// <summary>
    /// Verifies that datums created through the public factory store the supplied values.
    /// </summary>
    [Fact]
    public void FactoryCreateHorizontalDatum_SetsProperties()
    {
        Wgs84ConversionInfo parameters = new(1d, 2d, 3d, 4d, 5d, 6d, 7d);
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.Clarke1866, parameters);

        Assert.Equal("Custom datum", datum.Name);
        Assert.Equal(string.Empty, datum.Authority);
        Assert.Equal(-1, datum.AuthorityCode);
        Assert.Equal(DatumType.HD_Classic, datum.DatumType);
        Assert.True(datum.Ellipsoid.EqualParams(Ellipsoid.Clarke1866));
        Assert.Same(parameters, datum.Wgs84Parameters);
    }

    /// <summary>
    /// Verifies that WKT omits both optional clauses when neither WGS84 parameters nor authority metadata are present.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthorityAndWithoutWgs84_OmitsOptionalClauses()
    {
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.GRS80, null);

        Assert.Equal($"DATUM[\"Custom datum\", {Ellipsoid.GRS80.WKT}]", datum.WKT);
    }

    /// <summary>
    /// Verifies that WKT includes Bursa-Wolf parameters but omits authority when only WGS84 parameters are present.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthorityAndWithWgs84_IncludesTowgs84Only()
    {
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d));

        Assert.Equal($"DATUM[\"Custom datum\", {Ellipsoid.GRS80.WKT}, TOWGS84[1, 2, 3, 4, 5, 6, 7]]", datum.WKT);
    }

    /// <summary>
    /// Verifies that WKT includes authority when it is available but omits WGS84 parameters when absent.
    /// </summary>
    [Fact]
    public void WKT_WithAuthorityAndWithoutWgs84_IncludesAuthorityOnly()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;

        Assert.DoesNotContain("TOWGS84", datum.WKT, System.StringComparison.Ordinal);
        Assert.Contains("AUTHORITY[\"EPSG\", \"6326\"]", datum.WKT, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT includes both Bursa-Wolf parameters and authority metadata when both are available.
    /// </summary>
    [Fact]
    public void WKT_WithAuthorityAndWithWgs84_IncludesAllOptionalClauses()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;

        Assert.Contains("TOWGS84[-87, -98, -121, 0, 0, 0, 0]", datum.WKT, System.StringComparison.Ordinal);
        Assert.Contains("AUTHORITY[\"EPSG\", \"6230\"]", datum.WKT, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML omits WGS84 conversion information when the datum has no Bursa-Wolf parameters.
    /// </summary>
    [Fact]
    public void XML_WithoutWgs84_OmitsConversionElement()
    {
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        var xml = XElement.Parse(datum.XML);

        Assert.Equal("CS_HorizontalDatum", xml.Name.LocalName);
        Assert.Equal("1001", (string?)xml.Attribute("DatumType"));
        Assert.NotNull(xml.Element("CS_Info"));
        Assert.NotNull(xml.Element("CS_Ellipsoid"));
        Assert.Null(xml.Element("CS_WGS84ConversionInfo"));
    }

    /// <summary>
    /// Verifies that XML includes WGS84 conversion information when Bursa-Wolf parameters are present.
    /// </summary>
    [Fact]
    public void XML_WithWgs84_IncludesConversionElement()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;
        var xml = XElement.Parse(datum.XML);

        Assert.Equal("CS_HorizontalDatum", xml.Name.LocalName);
        Assert.Equal("1002", (string?)xml.Attribute("DatumType"));
        Assert.NotNull(xml.Element("CS_WGS84ConversionInfo"));
    }

    /// <summary>
    /// Verifies that <see cref="HorizontalDatum.ToXml"/> matches the XML property when no WGS84 parameters are present.
    /// </summary>
    [Fact]
    public void ToXml_WithoutWgs84_MatchesXmlProperty()
    {
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        XElement xml = datum.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(datum.XML), xml));
    }

    /// <summary>
    /// Verifies that <see cref="HorizontalDatum.ToXml"/> matches the XML property when WGS84 parameters are present.
    /// </summary>
    [Fact]
    public void ToXml_WithWgs84_MatchesXmlProperty()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;
        XElement xml = datum.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(datum.XML), xml));
    }

    /// <summary>
    /// Verifies that the WKT node contains only the name and ellipsoid when no optional clauses are present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthorityAndWithoutWgs84_ReturnsNameAndEllipsoidOnly()
    {
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(datum.ToWktNode());

        Assert.Equal("DATUM", node.Keyword);
        Assert.Equal(2, node.Children.Count);
        Assert.IsType<WktQuotedString>(node.Children[0]);
        Assert.Equal("SPHEROID", Assert.IsType<WktKeywordNode>(node.Children[1]).Keyword);
    }

    /// <summary>
    /// Verifies that the WKT node includes Bursa-Wolf parameters but no authority when only WGS84 parameters are present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthorityAndWithWgs84_OmitsAuthorityNode()
    {
        HorizontalDatum datum = CreateDatum("Custom datum", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d));
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(datum.ToWktNode());

        Assert.Equal(3, node.Children.Count);
        Assert.Equal("TOWGS84", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that the WKT node includes authority when the datum has authority metadata but no WGS84 parameters.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthorityAndWithoutWgs84_IncludesAuthorityNode()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(datum.ToWktNode());

        Assert.Equal(3, node.Children.Count);
        Assert.Equal("AUTHORITY", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
    }

    /// <summary>
    /// Verifies that the WKT node includes both Bursa-Wolf parameters and authority metadata when both are present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthorityAndWithWgs84_IncludesTowgs84AndAuthorityNodes()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(datum.ToWktNode());

        Assert.Equal(4, node.Children.Count);
        Assert.Equal("TOWGS84", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
        Assert.Equal("AUTHORITY", Assert.IsType<WktKeywordNode>(node.Children[3]).Keyword);
    }

    /// <summary>
    /// Verifies that equal custom datums compare equal when ellipsoid, datum type, and WGS84 parameters all match.
    /// </summary>
    [Fact]
    public void EqualParams_SameValuesWithoutWgs84_ReturnsTrue()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.GRS80, null);

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that equal datums with Bursa-Wolf parameters compare equal.
    /// </summary>
    [Fact]
    public void EqualParams_SameValuesWithWgs84_ReturnsTrue()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d));
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d));

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a datum with Bursa-Wolf parameters does not compare equal to one without them.
    /// </summary>
    [Fact]
    public void EqualParams_OneHasWgs84Parameters_ReturnsFalse()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d));

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different Bursa-Wolf parameters compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentWgs84Parameters_ReturnsFalse()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d));
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.GRS80, new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 8d));

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different ellipsoids compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentEllipsoid_ReturnsFalse()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.Clarke1866, null);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that datums compare equal when both ellipsoids are absent and the remaining parameters match.
    /// </summary>
    [Fact]
    public void EqualParams_BothEllipsoidsNull_ReturnsTrue()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.Clarke1866, null);
        first.Ellipsoid = null!;
        second.Ellipsoid = null!;

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that datums compare unequal when only the left ellipsoid is absent.
    /// </summary>
    [Fact]
    public void EqualParams_LeftEllipsoidNull_ReturnsFalse()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        first.Ellipsoid = null!;

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that datums compare unequal when only the right ellipsoid is absent.
    /// </summary>
    [Fact]
    public void EqualParams_RightEllipsoidNull_ReturnsFalse()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        second.Ellipsoid = null!;

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different datum types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentDatumType_ReturnsFalse()
    {
        HorizontalDatum first = CreateDatum("A", DatumType.HD_Classic, Ellipsoid.GRS80, null);
        HorizontalDatum second = CreateDatum("B", DatumType.HD_Geocentric, Ellipsoid.GRS80, null);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different object types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(HorizontalDatum.WGS84.EqualParams("not a datum"));
    }

    private static HorizontalDatum CreateDatum(
        string name,
        DatumType datumType,
        Ellipsoid ellipsoid,
        Wgs84ConversionInfo? toWgs84)
    {
        return new CoordinateSystemFactory().CreateHorizontalDatum(name, datumType, ellipsoid, toWgs84);
    }
}
