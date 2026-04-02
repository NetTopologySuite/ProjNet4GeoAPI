// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="CoordinateSystem"/>.
/// </summary>
public class CoordinateSystemTests
{
    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.Dimension"/> reflects the configured axis count.
    /// </summary>
    [Fact]
    public void Dimension_ReturnsAxisCount()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        Assert.Equal(2, coordinateSystem.Dimension);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.AxisInfo"/> rejects <see langword="null"/>.
    /// </summary>
    [Fact]
    public void AxisInfo_SetToNull_ThrowsArgumentNullException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        Assert.Throws<ArgumentNullException>(() => coordinateSystem.SetAxisInfo(null!));
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.DefaultEnvelope"/> can be updated and retrieved.
    /// </summary>
    [Fact]
    public void DefaultEnvelope_CanBeUpdated()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();
        double[] expectedEnvelope = [-180d, -90d, 180d, 90d];

        coordinateSystem.DefaultEnvelope = expectedEnvelope;

        Assert.Same(expectedEnvelope, coordinateSystem.DefaultEnvelope);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.ToWktNode"/> uses the WKT string by default.
    /// </summary>
    [Fact]
    public void ToWktNode_DefaultImplementation_ReturnsIdentifier()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        WktIdentifier node = Assert.IsType<WktIdentifier>(coordinateSystem.ToWktNode());

        Assert.Equal("CS_WKT", node.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.ToXml"/> throws by default.
    /// </summary>
    [Fact]
    public void ToXml_DefaultImplementation_ThrowsNotImplementedException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        Assert.Throws<NotImplementedException>(() => coordinateSystem.ToXml());
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.GetAxis"/> returns the configured axis for a valid dimension.
    /// </summary>
    [Fact]
    public void GetAxis_WithValidDimension_ReturnsAxis()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        AxisInfo axis = coordinateSystem.GetAxis(1);

        Assert.Equal("Latitude", axis.Name);
        Assert.Equal(AxisOrientationEnum.North, axis.Orientation);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.GetAxis"/> rejects negative dimensions.
    /// </summary>
    [Fact]
    public void GetAxis_WithNegativeDimension_ThrowsArgumentException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => coordinateSystem.GetAxis(-1));
        Assert.Contains("AxisInfo not available for dimension -1", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.GetAxis"/> rejects dimensions beyond the configured axis count.
    /// </summary>
    [Fact]
    public void GetAxis_WithOutOfRangeDimension_ThrowsArgumentException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => coordinateSystem.GetAxis(2));
        Assert.Contains("AxisInfo not available for dimension 2", exception.Message, StringComparison.Ordinal);
    }

    private static TestCoordinateSystem CreateCoordinateSystem()
    {
        return new TestCoordinateSystem(
            [
                new AxisInfo("Longitude", AxisOrientationEnum.East),
                new AxisInfo("Latitude", AxisOrientationEnum.North),
            ]);
    }

    private sealed class TestCoordinateSystem : CoordinateSystem
    {
        internal TestCoordinateSystem(List<AxisInfo> axisInfo)
            : base("Test CS", "AUTH", 1, "alias", "abbr", "remarks")
        {
            this.AxisInfo = axisInfo;
        }

        public override string WKT => "CS_WKT";

        public override string XML => "<CS />";

        public override IUnit GetUnits(int dimension) => LinearUnit.Metre;

        public override bool EqualParams(object obj) => obj is TestCoordinateSystem;

        internal void SetAxisInfo(List<AxisInfo> axisInfo)
        {
            this.AxisInfo = axisInfo;
        }
    }
}
