// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="EngineeringCoordinateSystem"/> and <see cref="EngineeringDatum"/>.
/// </summary>
public class EngineeringCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the constructor stores the engineering-specific properties.
    /// </summary>
    [Fact]
    public void Constructor_SetsEngineeringSpecificProperties()
    {
        EngineeringDatum datum = new("Local plant", "EPSG", 1098, string.Empty, string.Empty, string.Empty);
        var coordinateSystem = new EngineeringCoordinateSystem(
            datum,
            "Cartesian",
            [new AxisInfo("x", AxisOrientationEnum.East), new AxisInfo("y", AxisOrientationEnum.North)],
            [LinearUnit.Metre, LinearUnit.Metre],
            "Plant grid",
            "EPSG",
            5800,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.Same(datum, coordinateSystem.EngineeringDatum);
        Assert.Equal("Cartesian", coordinateSystem.CoordinateSystemType);
        Assert.Equal(2, coordinateSystem.Dimension);
        Assert.Equal(2, coordinateSystem.AxisUnits.Count);
    }

    /// <summary>
    /// Verifies that per-axis units are exposed through <see cref="EngineeringCoordinateSystem.GetUnits"/>.
    /// </summary>
    [Fact]
    public void GetUnits_ReturnsPerAxisUnits()
    {
        EngineeringCoordinateSystem coordinateSystem = CreateEngineeringCoordinateSystem([LinearUnit.Metre, new ParametricUnit(1d, "unity", string.Empty, -1, string.Empty, string.Empty, string.Empty)]);

        Assert.True(coordinateSystem.GetUnits(0).EqualParams(LinearUnit.Metre));
        Assert.True(coordinateSystem.GetUnits(1).EqualParams(new ParametricUnit(1d, "unity", string.Empty, -1, string.Empty, string.Empty, string.Empty)));
    }

    /// <summary>
    /// Verifies that WKT2 output uses <c>ENGCRS</c> and retains mixed axis units.
    /// </summary>
    [Fact]
    public void ToWktNode_WithMixedUnits_UsesAxisLevelUnits()
    {
        EngineeringCoordinateSystem coordinateSystem = CreateEngineeringCoordinateSystem([LinearUnit.Metre, new ParametricUnit(1d, "unity", string.Empty, -1, string.Empty, string.Empty, string.Empty)]);
        string wkt = coordinateSystem.ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.StartsWith("ENGCRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("EDATUM[\"Local plant\"", wkt, StringComparison.Ordinal);
        Assert.Contains("LENGTHUNIT[\"metre\"", wkt, StringComparison.Ordinal);
        Assert.Contains("PARAMETRICUNIT[\"unity\"", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT2 roundtrips preserve mixed per-axis engineering units.
    /// </summary>
    [Fact]
    public void ToWktNode_WithMixedUnits_RoundTripsEngineeringAxisUnits()
    {
        EngineeringCoordinateSystem original = CreateEngineeringCoordinateSystem([LinearUnit.Metre, new ParametricUnit(1d, "unity", string.Empty, -1, string.Empty, string.Empty, string.Empty)]);
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();

        EngineeringCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<EngineeringCoordinateSystem>(wkt);

        Assert.True(original.EqualParams(roundTripped));
        Assert.IsType<LinearUnit>(roundTripped.AxisUnits[0]);
        Assert.IsType<ParametricUnit>(roundTripped.AxisUnits[1]);
        Assert.Equal("unity", roundTripped.AxisUnits[1].Name);
    }

    /// <summary>
    /// Verifies that equivalent engineering coordinate systems compare equal.
    /// </summary>
    [Fact]
    public void EqualParams_IgnoresMetadataButComparesUnits()
    {
        EngineeringCoordinateSystem first = CreateEngineeringCoordinateSystem([LinearUnit.Metre, LinearUnit.Metre], name: "First");
        EngineeringCoordinateSystem second = CreateEngineeringCoordinateSystem([LinearUnit.Metre, LinearUnit.Metre], name: "Second");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that the constructor reports the unit collection when the axis and unit counts differ.
    /// </summary>
    [Fact]
    public void Constructor_MismatchedAxisAndUnitCounts_ThrowsArgumentExceptionWithUnitsParamName()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EngineeringCoordinateSystem(
            new EngineeringDatum("Local plant", "EPSG", 1098, string.Empty, string.Empty, string.Empty),
            "Cartesian",
            [new AxisInfo("x", AxisOrientationEnum.East), new AxisInfo("y", AxisOrientationEnum.North)],
            [LinearUnit.Metre],
            "Plant grid",
            "EPSG",
            5800,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("units", exception.ParamName);
    }

    private static EngineeringCoordinateSystem CreateEngineeringCoordinateSystem(IUnit[] units, string name = "Plant grid")
    {
        return new EngineeringCoordinateSystem(
            new EngineeringDatum("Local plant", "EPSG", 1098, string.Empty, string.Empty, string.Empty),
            "Cartesian",
            [new AxisInfo("x", AxisOrientationEnum.East), new AxisInfo("y", AxisOrientationEnum.North)],
            units,
            name,
            "EPSG",
            5800,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
