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
    /// Verifies that the constructor rejects <see langword="null"/> axis definitions.
    /// </summary>
    [Fact]
    public void Constructor_NullAxisInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TestCoordinateSystem(null!, null));
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.DefaultEnvelope"/> exposes the configured envelope.
    /// </summary>
    [Fact]
    public void DefaultEnvelope_ReturnsConfiguredEnvelope()
    {
        double[] expectedEnvelope = [-180d, -90d, 180d, 90d];
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem(defaultEnvelope: expectedEnvelope);

        Assert.Equal(expectedEnvelope, coordinateSystem.DefaultEnvelope);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.DefaultEnvelope"/> returns a defensive copy.
    /// </summary>
    [Fact]
    public void DefaultEnvelope_GetterReturnsDefensiveCopy()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem(defaultEnvelope: [-180d, -90d, 180d, 90d]);

        double[] firstRead = coordinateSystem.DefaultEnvelope;
        double[] secondRead = coordinateSystem.DefaultEnvelope;
        firstRead[0] = 0d;

        Assert.NotSame(firstRead, secondRead);
        Assert.Equal(-180d, secondRead[0]);
    }

    /// <summary>
    /// Verifies that the constructor clones the assigned default envelope array.
    /// </summary>
    [Fact]
    public void Constructor_ClonesAssignedDefaultEnvelope()
    {
        double[] sourceEnvelope = [-180d, -90d, 180d, 90d];
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem(defaultEnvelope: sourceEnvelope);
        sourceEnvelope[0] = 0d;

        Assert.Equal(-180d, coordinateSystem.DefaultEnvelope[0]);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.ToWktNode()"/> uses the WKT string by default.
    /// </summary>
    [Fact]
    public void ToWktNode_DefaultImplementation_ReturnsIdentifier()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        WktIdentifier node = Assert.IsType<WktIdentifier>(coordinateSystem.ToWktNode());

        Assert.Equal("CS_WKT", node.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.ToXml"/> throws by default when XML serialization is unsupported.
    /// </summary>
    [Fact]
    public void ToXml_DefaultImplementation_ThrowsNotSupportedException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        Assert.Throws<NotSupportedException>(() => coordinateSystem.ToXml());
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
    public void GetAxis_WithNegativeDimension_ThrowsArgumentOutOfRangeException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => coordinateSystem.GetAxis(-1));
        Assert.Equal("dimension", exception.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystem.GetAxis"/> rejects dimensions beyond the configured axis count.
    /// </summary>
    [Fact]
    public void GetAxis_WithOutOfRangeDimension_ThrowsArgumentOutOfRangeException()
    {
        TestCoordinateSystem coordinateSystem = CreateCoordinateSystem();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => coordinateSystem.GetAxis(2));
        Assert.Equal("dimension", exception.ParamName);
    }

    private static TestCoordinateSystem CreateCoordinateSystem(List<AxisInfo>? axisInfo = null, double[]? defaultEnvelope = null)
    {
        return new TestCoordinateSystem(
            axisInfo ??
            [
                new AxisInfo("Longitude", AxisOrientationEnum.East),
                new AxisInfo("Latitude", AxisOrientationEnum.North),
            ],
            defaultEnvelope);
    }

    private sealed class TestCoordinateSystem : CoordinateSystem
    {
        internal TestCoordinateSystem(List<AxisInfo> axisInfo, double[]? defaultEnvelope)
            : base("Test CS", "AUTH", 1, "alias", "abbr", "remarks", axisInfo, defaultEnvelope)
        {
        }

        public override string WKT => "CS_WKT";

        public override string XML => "<CS />";

        public override IUnit GetUnits(int dimension) => LinearUnit.Metre;

        public override bool EqualParams(object obj) => obj is TestCoordinateSystem;
    }
}
