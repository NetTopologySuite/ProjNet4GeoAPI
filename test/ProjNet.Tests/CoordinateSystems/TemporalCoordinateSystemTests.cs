// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="TemporalCoordinateSystem"/>, <see cref="TemporalDatum"/>, and <see cref="TimeUnit"/>.
/// </summary>
public class TemporalCoordinateSystemTests
{
    /// <summary>
    /// Verifies that temporal datum output retains the time origin.
    /// </summary>
    [Fact]
    public void TemporalDatum_ToWktNode_RetainsTimeOrigin()
    {
        var datum = new TemporalDatum("1950-01-01T00:00:00Z", "Unix epoch", "EPSG", 1040, string.Empty, string.Empty, string.Empty);
        string wkt = datum.ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.Contains("TDATUM[\"Unix epoch\"", wkt, StringComparison.Ordinal);
        Assert.Contains("TIMEORIGIN[\"1950-01-01T00:00:00Z\"]", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that time units retain their conversion factor and keyword.
    /// </summary>
    [Fact]
    public void TimeUnit_ToWktNode_UsesTimeUnitKeyword()
    {
        var unit = new TimeUnit(1d, "second", "EPSG", 1040, string.Empty, string.Empty, string.Empty);

        Assert.Equal(1d, unit.ConversionFactor);
        Assert.StartsWith("TIMEUNIT[\"second\"", unit.ToWktNode(WktVersion.Wkt22019).ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that temporal coordinate systems expose their single time unit.
    /// </summary>
    [Fact]
    public void GetUnits_ReturnsTimeUnit()
    {
        TemporalCoordinateSystem coordinateSystem = CreateTemporalCoordinateSystem();

        Assert.True(coordinateSystem.GetUnits(0).EqualParams(coordinateSystem.TimeUnit));
        Assert.ThrowsAny<ArgumentException>(() => coordinateSystem.GetUnits(1));
    }

    /// <summary>
    /// Verifies that WKT2 output uses <c>TIMECRS</c>.
    /// </summary>
    [Fact]
    public void ToWktNode_UsesTimeCrsKeyword()
    {
        string wkt = CreateTemporalCoordinateSystem().ToWktNode(WktVersion.Wkt22019).ToString();

        Assert.StartsWith("TIMECRS[", wkt, StringComparison.Ordinal);
        Assert.Contains("TIMEUNIT[\"second\"", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT2 roundtrips preserve the temporal unit metadata.
    /// </summary>
    [Fact]
    public void ToWktNode_RoundTripsTemporalCoordinateSystemWithTimeUnit()
    {
        TemporalCoordinateSystem original = CreateTemporalCoordinateSystem();
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();

        TemporalCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<TemporalCoordinateSystem>(
            new CoordinateSystemFactory(),
            wkt);

        Assert.True(original.EqualParams(roundTripped));
        TimeUnit unit = Assert.IsType<TimeUnit>(roundTripped.TimeUnit);
        Assert.Equal(original.TimeUnit.ConversionFactor, unit.ConversionFactor);
        Assert.Equal(original.TimeUnit.Name, unit.Name);
    }

    private static TemporalCoordinateSystem CreateTemporalCoordinateSystem()
    {
        return new TemporalCoordinateSystem(
            new TimeUnit(1d, "second", "EPSG", 1040, string.Empty, string.Empty, string.Empty),
            new TemporalDatum("1950-01-01T00:00:00Z", "Unix epoch", "EPSG", 1040, string.Empty, string.Empty, string.Empty),
            new AxisInfo("time", AxisOrientationEnum.Other),
            "Temporal axis",
            "EPSG",
            1041,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
