// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="VerticalCoordinateSystem"/>.
/// </summary>
public class VerticalCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the constructor assigns all properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsProperties()
    {
        LinearUnit linearUnit = LinearUnit.Metre;
        VerticalDatum datum = VerticalDatum.ODN;
        var axis = new AxisInfo("Up", AxisOrientationEnum.Up);

        var vcs = new VerticalCoordinateSystem(
            linearUnit, datum, axis,
            "TestVCS", "TEST", 1234, "alias", "abbr", "remarks");

        Assert.Equal("TestVCS", vcs.Name);
        Assert.Equal("TEST", vcs.Authority);
        Assert.Equal(1234, vcs.AuthorityCode);
        Assert.Same(linearUnit, vcs.LinearUnit);
        Assert.Same(datum, vcs.VerticalDatum);
    }

    /// <summary>
    /// Verifies that the ODN predefined constant has the correct name.
    /// </summary>
    [Fact]
    public void ODN_HasCorrectName()
    {
        VerticalCoordinateSystem odn = VerticalCoordinateSystem.ODN;

        Assert.Equal("Newlyn", odn.Name);
    }

    /// <summary>
    /// Verifies that the ODN predefined constant has the correct authority.
    /// </summary>
    [Fact]
    public void ODN_HasCorrectAuthority()
    {
        VerticalCoordinateSystem odn = VerticalCoordinateSystem.ODN;

        Assert.Equal("EPSG", odn.Authority);
        Assert.Equal(5701, odn.AuthorityCode);
    }

    /// <summary>
    /// Verifies that a vertical coordinate system has exactly one dimension.
    /// </summary>
    [Fact]
    public void Dimension_IsOne()
    {
        VerticalCoordinateSystem odn = VerticalCoordinateSystem.ODN;

        Assert.Equal(1, odn.Dimension);
    }

    /// <summary>
    /// Verifies that the ODN system uses metres as its linear unit.
    /// </summary>
    [Fact]
    public void ODN_LinearUnit_IsMetres()
    {
        VerticalCoordinateSystem odn = VerticalCoordinateSystem.ODN;

        Assert.Equal(1.0, odn.LinearUnit.MetersPerUnit);
    }

    /// <summary>
    /// Verifies that <see cref="VerticalCoordinateSystem.GetUnits"/> returns the linear unit for dimension 0.
    /// </summary>
    [Fact]
    public void GetUnits_DimensionZero_ReturnsLinearUnit()
    {
        VerticalCoordinateSystem odn = VerticalCoordinateSystem.ODN;

        IUnit unit = odn.GetUnits(0);

        Assert.IsType<LinearUnit>(unit);
        Assert.True(odn.LinearUnit.EqualParams(unit));
    }

    /// <summary>
    /// Verifies that <see cref="VerticalCoordinateSystem.GetUnits"/> throws for invalid dimensions.
    /// </summary>
    [Fact]
    public void GetUnits_InvalidDimension_Throws()
    {
        VerticalCoordinateSystem odn = VerticalCoordinateSystem.ODN;

        Assert.ThrowsAny<ArgumentException>(() => odn.GetUnits(1));
    }

    // ---- WKT ----

    /// <summary>
    /// Verifies that the WKT output starts with VERT_CS and contains the system name.
    /// </summary>
    [Fact]
    public void WKT_ContainsVertCsAndName()
    {
        string wkt = VerticalCoordinateSystem.ODN.WKT;

        Assert.StartsWith("VERT_CS[\"Newlyn\"", wkt);
    }

    /// <summary>
    /// Verifies that the WKT output contains the vertical datum.
    /// </summary>
    [Fact]
    public void WKT_ContainsVerticalDatum()
    {
        string wkt = VerticalCoordinateSystem.ODN.WKT;

        Assert.Contains("DATUM[\"Ordnance Datum Newlyn\"", wkt);
    }

    /// <summary>
    /// Verifies that the WKT output contains the linear unit.
    /// </summary>
    [Fact]
    public void WKT_ContainsLinearUnit()
    {
        string wkt = VerticalCoordinateSystem.ODN.WKT;

        Assert.Contains("UNIT[\"metre\", 1", wkt);
    }

    /// <summary>
    /// Verifies that the WKT output contains the authority code.
    /// </summary>
    [Fact]
    public void WKT_ContainsAuthority()
    {
        string wkt = VerticalCoordinateSystem.ODN.WKT;

        Assert.Contains("AUTHORITY[\"EPSG\", \"5701\"]", wkt);
    }

    /// <summary>
    /// Verifies that the WKT omits the default axis info (Up/Up).
    /// </summary>
    [Fact]
    public void WKT_OmitsDefaultAxis()
    {
        string wkt = VerticalCoordinateSystem.ODN.WKT;

        Assert.DoesNotContain("AXIS[", wkt);
    }

    /// <summary>
    /// Verifies that the WKT includes axis info when it differs from the default.
    /// </summary>
    [Fact]
    public void WKT_NonDefaultAxis_IncludesAxisInfo()
    {
        var vcs = new VerticalCoordinateSystem(
            LinearUnit.Metre,
            VerticalDatum.ODN,
            new AxisInfo("Height", AxisOrientationEnum.North),
            "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        string wkt = vcs.WKT;

        Assert.Contains("AXIS[", wkt);
    }

    // ---- XML ----

    /// <summary>
    /// Verifies that the XML output contains expected elements.
    /// </summary>
    [Fact]
    public void XML_ContainsExpectedElements()
    {
        string xml = VerticalCoordinateSystem.ODN.XML;

        Assert.Contains("CS_CoordinateSystem", xml);
        Assert.Contains("CS_VerticalCoordinateSystem", xml);
        Assert.Contains("CS_VerticalDatum", xml);
        Assert.Contains("CS_LinearUnit", xml);
    }

    /// <summary>
    /// Verifies that the XML output contains the dimension attribute.
    /// </summary>
    [Fact]
    public void XML_ContainsDimension()
    {
        string xml = VerticalCoordinateSystem.ODN.XML;

        Assert.Contains("Dimension=\"1\"", xml);
    }

    // ---- EqualParams ----

    /// <summary>
    /// Verifies that EqualParams returns true for equivalent systems.
    /// </summary>
    [Fact]
    public void EqualParams_EquivalentSystems_ReturnsTrue()
    {
        VerticalCoordinateSystem a = VerticalCoordinateSystem.ODN;
        VerticalCoordinateSystem b = VerticalCoordinateSystem.ODN;

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for systems with different linear units.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentLinearUnit_ReturnsFalse()
    {
        VerticalCoordinateSystem a = VerticalCoordinateSystem.ODN;
        var b = new VerticalCoordinateSystem(
            LinearUnit.Foot,
            VerticalDatum.ODN,
            new AxisInfo("Up", AxisOrientationEnum.Up),
            "Newlyn", "EPSG", 5701, string.Empty, string.Empty, string.Empty);

        Assert.False(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(VerticalCoordinateSystem.ODN.EqualParams("not a VCS"));
    }

    // ---- Property setters ----

    /// <summary>
    /// Verifies that <see cref="VerticalCoordinateSystem.LinearUnit"/> can be replaced.
    /// </summary>
    [Fact]
    public void LinearUnit_Setter_UpdatesUnit()
    {
        VerticalCoordinateSystem vcs = VerticalCoordinateSystem.ODN;
        vcs.LinearUnit = LinearUnit.Foot;

        Assert.Equal(0.3048, vcs.LinearUnit.MetersPerUnit, 12);
    }

    /// <summary>
    /// Verifies that <see cref="VerticalCoordinateSystem.VerticalDatum"/> can be replaced.
    /// </summary>
    [Fact]
    public void VerticalDatum_Setter_UpdatesDatum()
    {
        VerticalCoordinateSystem vcs = VerticalCoordinateSystem.ODN;
        var newDatum = new VerticalDatum(
            DatumType.VD_GeoidModelDerived, "Custom", "TEST", 9999,
            string.Empty, string.Empty, string.Empty);
        vcs.VerticalDatum = newDatum;

        Assert.Same(newDatum, vcs.VerticalDatum);
    }
}
