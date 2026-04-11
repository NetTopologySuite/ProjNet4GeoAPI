// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="GeocentricCoordinateSystem"/>.
/// </summary>
public class GeocentricCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the predefined WGS84 system exposes the expected metadata and defaults.
    /// </summary>
    [Fact]
    public void WGS84_HasExpectedMetadata()
    {
        GeocentricCoordinateSystem system = GeocentricCoordinateSystem.WGS84;

        Assert.Equal("WGS 84", system.Name);
        Assert.Equal(3, system.Dimension);
        Assert.True(system.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.True(system.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich));
        Assert.Equal("Geocentric X (X)", system.GetAxis(0).Name);
        Assert.Equal("Geocentric Y (Y)", system.GetAxis(1).Name);
        Assert.Equal("Geocentric Z (Z)", system.GetAxis(2).Name);
    }

    /// <summary>
    /// Verifies that the internal constructor stores the supplied values.
    /// </summary>
    [Fact]
    public void Constructor_SetsProperties()
    {
        List<AxisInfo> axisInfo = CreateDefaultAxisInfo();
        var system = new GeocentricCoordinateSystem(
            HorizontalDatum.ED50,
            LinearUnit.Foot,
            PrimeMeridian.Paris,
            axisInfo,
            "Custom geocentric",
            "TEST",
            42,
            "alias",
            "remarks",
            "abbr");

        Assert.Equal("Custom geocentric", system.Name);
        Assert.Equal("TEST", system.Authority);
        Assert.Equal(42, system.AuthorityCode);
        Assert.Equal("alias", system.Alias);
        Assert.Equal("remarks", system.Remarks);
        Assert.Equal("abbr", system.Abbreviation);
        Assert.Same(axisInfo, system.AxisInfo);
        Assert.True(system.HorizontalDatum.EqualParams(HorizontalDatum.ED50));
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Foot));
        Assert.True(system.PrimeMeridian.EqualParams(PrimeMeridian.Paris));
    }

    /// <summary>
    /// Verifies that the constructor rejects a null datum.
    /// </summary>
    [Fact]
    public void Constructor_NullDatum_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new GeocentricCoordinateSystem(
            null!,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich,
            CreateDefaultAxisInfo(),
            "Custom geocentric",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("datum", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null linear unit.
    /// </summary>
    [Fact]
    public void Constructor_NullLinearUnit_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new GeocentricCoordinateSystem(
            HorizontalDatum.WGS84,
            null!,
            PrimeMeridian.Greenwich,
            CreateDefaultAxisInfo(),
            "Custom geocentric",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("linearUnit", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null prime meridian.
    /// </summary>
    [Fact]
    public void Constructor_NullPrimeMeridian_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new GeocentricCoordinateSystem(
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            null!,
            CreateDefaultAxisInfo(),
            "Custom geocentric",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("primeMeridian", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null axis list.
    /// </summary>
    [Fact]
    public void Constructor_NullAxisInfo_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new GeocentricCoordinateSystem(
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich,
            null!,
            "Custom geocentric",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("axisInfo", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects axis lists that do not contain exactly three axes.
    /// </summary>
    [Fact]
    public void Constructor_AxisInfoWithoutThreeAxes_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new GeocentricCoordinateSystem(
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich,
            new List<AxisInfo> { new("X", AxisOrientationEnum.Other), new("Y", AxisOrientationEnum.East) },
            "Custom geocentric",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("Axis info should contain three axes for geocentric coordinate systems", exception.Message);
    }

    /// <summary>
    /// Verifies that WKT omits axis clauses when the default geocentric axes are used.
    /// </summary>
    [Fact]
    public void WKT_WithDefaultAxes_OmitsAxisClauses()
    {
        GeocentricCoordinateSystem system = CreateSystem("Custom geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());

        Assert.DoesNotContain("AXIS[", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT includes custom axis clauses when the axis definitions differ from the defaults.
    /// </summary>
    [Fact]
    public void WKT_WithCustomAxes_IncludesAxisClauses()
    {
        GeocentricCoordinateSystem system = CreateSystem("Custom geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateCustomAxisInfo());

        Assert.Contains("AXIS[\"Longitude\", EAST]", system.WKT, StringComparison.Ordinal);
        Assert.Contains("AXIS[\"Latitude\", NORTH]", system.WKT, StringComparison.Ordinal);
        Assert.Contains("AXIS[\"Height\", UP]", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT includes authority metadata when it is available.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_IncludesAuthorityClause()
    {
        GeocentricCoordinateSystem system = CreateSystem(
            "Custom geocentric",
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich,
            CreateDefaultAxisInfo(),
            "EPSG",
            4984);

        Assert.Contains("AUTHORITY[\"EPSG\", \"4984\"]", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT omits authority metadata when it is unavailable.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthority_OmitsAuthorityClause()
    {
        GeocentricCoordinateSystem system = GeocentricCoordinateSystem.WGS84;

        Assert.DoesNotContain("AUTHORITY[\"\",", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML includes all expected nested elements for a default system.
    /// </summary>
    [Fact]
    public void XML_WithDefaultAxes_ContainsExpectedStructure()
    {
        GeocentricCoordinateSystem system = GeocentricCoordinateSystem.WGS84;
        var xml = XElement.Parse(system.XML);
        XElement inner = Assert.IsType<XElement>(xml.Element("CS_GeocentricCoordinateSystem"));

        Assert.Equal("CS_CoordinateSystem", xml.Name.LocalName);
        Assert.Equal("3", (string?)xml.Attribute("Dimension"));
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.Equal(3, inner.Elements("CS_AxisInfo").Count());
        Assert.NotNull(inner.Element("CS_HorizontalDatum"));
        Assert.NotNull(inner.Element("CS_LinearUnit"));
        Assert.NotNull(inner.Element("CS_PrimeMeridian"));
    }

    /// <summary>
    /// Verifies that <see cref="GeocentricCoordinateSystem.ToXml"/> matches the XML property.
    /// </summary>
    [Fact]
    public void ToXml_MatchesXmlProperty()
    {
        GeocentricCoordinateSystem system = CreateSystem("Custom geocentric", HorizontalDatum.ED50, LinearUnit.Foot, PrimeMeridian.Paris, CreateCustomAxisInfo());
        XElement xml = system.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(system.XML), xml));
    }

    /// <summary>
    /// Verifies that GetUnits returns the linear unit for all valid dimensions.
    /// </summary>
    /// <param name="dimension">The queried dimension index.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetUnits_ValidDimension_ReturnsLinearUnit(int dimension)
    {
        GeocentricCoordinateSystem system = GeocentricCoordinateSystem.WGS84;

        IUnit unit = system.GetUnits(dimension);

        Assert.True(unit.EqualParams(LinearUnit.Metre));
    }

    /// <summary>
    /// Verifies that GetUnits ignores out-of-range dimensions and still returns the shared linear unit.
    /// </summary>
    /// <param name="dimension">The queried out-of-range dimension index.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GetUnits_OutOfRangeDimension_ReturnsLinearUnit(int dimension)
    {
        GeocentricCoordinateSystem system = GeocentricCoordinateSystem.WGS84;

        IUnit unit = system.GetUnits(dimension);

        Assert.True(unit.EqualParams(LinearUnit.Metre));
    }

    /// <summary>
    /// Verifies that ToWktNode omits axis nodes for the default geocentric axis set.
    /// </summary>
    [Fact]
    public void ToWktNode_WithDefaultAxes_OmitsAxisNodes()
    {
        GeocentricCoordinateSystem system = CreateSystem("Custom geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal("GEOCCS", node.Keyword);
        Assert.Equal(4, node.Children.Count);
    }

    /// <summary>
    /// Verifies that ToWktNode includes custom axis nodes when non-default axes are used.
    /// </summary>
    [Fact]
    public void ToWktNode_WithCustomAxes_IncludesAxisNodes()
    {
        GeocentricCoordinateSystem system = CreateSystem("Custom geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateCustomAxisInfo());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal(7, node.Children.Count);
        Assert.Equal("AXIS", Assert.IsType<WktKeywordNode>(node.Children[4]).Keyword);
        Assert.Equal("AXIS", Assert.IsType<WktKeywordNode>(node.Children[5]).Keyword);
        Assert.Equal("AXIS", Assert.IsType<WktKeywordNode>(node.Children[6]).Keyword);
    }

    /// <summary>
    /// Verifies that ToWktNode includes an authority node when authority metadata is present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        GeocentricCoordinateSystem system = CreateSystem(
            "Custom geocentric",
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich,
            CreateDefaultAxisInfo(),
            "EPSG",
            4984);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal("AUTHORITY", Assert.IsType<WktKeywordNode>(node.Children[4]).Keyword);
    }

    /// <summary>
    /// Verifies that equal systems compare equal when datum, unit, and prime meridian all match.
    /// </summary>
    [Fact]
    public void EqualParams_SameValues_ReturnsTrue()
    {
        GeocentricCoordinateSystem first = CreateSystem("A", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());
        GeocentricCoordinateSystem second = CreateSystem("B", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateCustomAxisInfo());

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that differing horizontal datums compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentDatum_ReturnsFalse()
    {
        GeocentricCoordinateSystem first = CreateSystem("A", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());
        GeocentricCoordinateSystem second = CreateSystem("B", HorizontalDatum.ED50, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that differing linear units compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentLinearUnit_ReturnsFalse()
    {
        GeocentricCoordinateSystem first = CreateSystem("A", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());
        GeocentricCoordinateSystem second = CreateSystem("B", HorizontalDatum.WGS84, LinearUnit.Foot, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that differing prime meridians compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentPrimeMeridian_ReturnsFalse()
    {
        GeocentricCoordinateSystem first = CreateSystem("A", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich, CreateDefaultAxisInfo());
        GeocentricCoordinateSystem second = CreateSystem("B", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Paris, CreateDefaultAxisInfo());

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different object types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(GeocentricCoordinateSystem.WGS84.EqualParams("not a coordinate system"));
    }

    private static GeocentricCoordinateSystem CreateSystem(
        string name,
        HorizontalDatum datum,
        LinearUnit linearUnit,
        PrimeMeridian primeMeridian,
        List<AxisInfo> axisInfo,
        string authority = "",
        long authorityCode = -1)
    {
        return new GeocentricCoordinateSystem(
            datum,
            linearUnit,
            primeMeridian,
            axisInfo,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static List<AxisInfo> CreateDefaultAxisInfo()
    {
        return
        [
            new AxisInfo("X", AxisOrientationEnum.Other),
            new AxisInfo("Y", AxisOrientationEnum.East),
            new AxisInfo("Z", AxisOrientationEnum.North),
        ];
    }

    private static List<AxisInfo> CreateCustomAxisInfo()
    {
        return
        [
            new AxisInfo("Longitude", AxisOrientationEnum.East),
            new AxisInfo("Latitude", AxisOrientationEnum.North),
            new AxisInfo("Height", AxisOrientationEnum.Up),
        ];
    }
}
