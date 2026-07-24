// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="HorizontalCoordinateSystem"/>.
/// </summary>
public class HorizontalCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the constructor stores the horizontal datum and axes for a valid two-axis definition.
    /// </summary>
    [Fact]
    public void Constructor_WithTwoAxes_SetsDatumAndAxisInfo()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;
        List<AxisInfo> axisInfo =
        [
            new AxisInfo("Longitude", AxisOrientationEnum.East),
            new AxisInfo("Latitude", AxisOrientationEnum.North),
        ];

        TestHorizontalCoordinateSystem coordinateSystem = new(datum, axisInfo);

        Assert.Same(datum, coordinateSystem.HorizontalDatum);
        Assert.Equal(2, coordinateSystem.Dimension);
        Assert.Same(axisInfo[0], coordinateSystem.GetAxis(0));
        Assert.Same(axisInfo[1], coordinateSystem.GetAxis(1));
    }

    /// <summary>
    /// Verifies that the constructor rejects a missing datum.
    /// </summary>
    [Fact]
    public void Constructor_WithNullDatum_ThrowsArgumentNullException()
    {
        List<AxisInfo> axisInfo =
        [
            new AxisInfo("Longitude", AxisOrientationEnum.East),
            new AxisInfo("Latitude", AxisOrientationEnum.North),
        ];

        Assert.Throws<ArgumentNullException>(() => new TestHorizontalCoordinateSystem(null!, axisInfo));
    }

    /// <summary>
    /// Verifies that the constructor rejects a missing axis list.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAxisInfo_ThrowsArgumentNullException()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;

        Assert.Throws<ArgumentNullException>(() => new TestHorizontalCoordinateSystem(datum, null!));
    }

    /// <summary>
    /// Verifies that the constructor rejects axis lists that do not contain exactly two axes.
    /// </summary>
    [Fact]
    public void Constructor_WithNonTwoAxisList_ThrowsArgumentException()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;
        List<AxisInfo> axisInfo =
        [
            new AxisInfo("Longitude", AxisOrientationEnum.East),
        ];

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TestHorizontalCoordinateSystem(datum, axisInfo));
        Assert.Equal("axisInfo", exception.ParamName);
    }

    private sealed class TestHorizontalCoordinateSystem : HorizontalCoordinateSystem
    {
        internal TestHorizontalCoordinateSystem(HorizontalDatum datum, List<AxisInfo> axisInfo)
            : base(datum, axisInfo, "Test HCS", "AUTH", 1, "alias", "remarks", "abbr")
        {
        }

        public override string WKT => "HCS_WKT";

        public override string XML => "<HCS />";

        public override IUnit GetUnits(int dimension) => AngularUnit.Degrees;

        public override bool EqualParams(object obj) => obj is TestHorizontalCoordinateSystem;
    }
}
