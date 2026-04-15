// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="ParametricCoordinateSystem"/>, <see cref="ParametricDatum"/>, and <see cref="ParametricUnit"/>.
/// </summary>
public class ParametricCoordinateSystemTests
{
    /// <summary>
    /// Verifies that parametric units retain their conversion factor and keyword.
    /// </summary>
    [Fact]
    public void ParametricUnit_ToWktNode_UsesParametricUnitKeyword()
    {
        var unit = new ParametricUnit(0.1d, "pressure", "EPSG", 0, string.Empty, string.Empty, string.Empty);

        Assert.Equal(0.1d, unit.ConversionFactor);
        Assert.StartsWith("PARAMETRICUNIT[\"pressure\"", unit.ToWktNode(WktVersion.Wkt22019).ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that parametric datum output uses <c>PDATUM</c>.
    /// </summary>
    [Fact]
    public void ParametricDatum_ToWktNode_UsesPdatumKeyword()
    {
        var datum = new ParametricDatum("Reservoir datum", "EPSG", 0, string.Empty, string.Empty, string.Empty);

        Assert.StartsWith("PDATUM[\"Reservoir datum\"", datum.ToWktNode(WktVersion.Wkt22019).ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that parametric coordinate systems expose their single unit.
    /// </summary>
    [Fact]
    public void GetUnits_ReturnsParametricUnit()
    {
        ParametricCoordinateSystem coordinateSystem = CreateParametricCoordinateSystem();

        Assert.True(coordinateSystem.GetUnits(0).EqualParams(coordinateSystem.ParametricUnit));
        Assert.ThrowsAny<ArgumentException>(() => coordinateSystem.GetUnits(1));
    }

    /// <summary>
    /// Verifies that WKT2 output uses <c>PARAMETRICCRS</c>.
    /// </summary>
    [Fact]
    public void ToWktNode_UsesParametricCrsKeyword()
    {
        string wkt = CreateParametricCoordinateSystem().ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.StartsWith("PARAMETRICCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("PARAMETRICUNIT[\"pressure\"", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT2 roundtrips preserve the parametric unit metadata.
    /// </summary>
    [Fact]
    public void ToWktNode_RoundTripsParametricCoordinateSystemWithParametricUnit()
    {
        ParametricCoordinateSystem original = CreateParametricCoordinateSystem();
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();

        ParametricCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<ParametricCoordinateSystem>(
            new CoordinateSystemFactory(),
            wkt);

        Assert.True(original.EqualParams(roundTripped));
        ParametricUnit unit = Assert.IsType<ParametricUnit>(roundTripped.ParametricUnit);
        Assert.Equal(original.ParametricUnit.ConversionFactor, unit.ConversionFactor);
        Assert.Equal(original.ParametricUnit.Name, unit.Name);
    }

    private static ParametricCoordinateSystem CreateParametricCoordinateSystem()
    {
        return new ParametricCoordinateSystem(
            new ParametricUnit(0.1d, "pressure", "EPSG", 0, string.Empty, string.Empty, string.Empty),
            new ParametricDatum("Reservoir datum", "EPSG", 0, string.Empty, string.Empty, string.Empty),
            new AxisInfo("pressure", AxisOrientationEnum.Up),
            "Reservoir pressure",
            "EPSG",
            0,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
