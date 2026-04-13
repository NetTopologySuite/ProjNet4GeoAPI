// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="FittedCoordinateSystem"/>.
/// </summary>
public class FittedCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the constructor stores the supplied values and copies the base axes.
    /// </summary>
    [Fact]
    public void Constructor_SetsPropertiesAndCopiesBaseAxes()
    {
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem(CreateCustomAxisInfo(), AngularUnit.Grad);
        AffineTransform transform = CreateTransform();
        var system = new FittedCoordinateSystem(
            baseCoordinateSystem,
            transform,
            "Custom fitted",
            "TEST",
            42,
            "alias",
            "remarks",
            "abbr");

        Assert.Equal("Custom fitted", system.Name);
        Assert.Equal("TEST", system.Authority);
        Assert.Equal(42, system.AuthorityCode);
        Assert.Equal("alias", system.Alias);
        Assert.Equal("remarks", system.Remarks);
        Assert.Equal("abbr", system.Abbreviation);
        Assert.Same(baseCoordinateSystem, system.BaseCoordinateSystem);
        Assert.Same(transform, system.ToBaseTransform);
        Assert.Equal(baseCoordinateSystem.Dimension, system.Dimension);
        Assert.Same(baseCoordinateSystem.GetAxis(0), system.GetAxis(0));
        Assert.Same(baseCoordinateSystem.GetAxis(1), system.GetAxis(1));
    }

    /// <summary>
    /// Verifies that the constructor rejects a null base coordinate system.
    /// </summary>
    [Fact]
    public void Constructor_NullBaseCoordinateSystem_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new FittedCoordinateSystem(
            null!,
            CreateTransform(),
            "Custom fitted",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("baseSystem", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null transform.
    /// </summary>
    [Fact]
    public void Constructor_NullTransform_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new FittedCoordinateSystem(
            GeographicCoordinateSystem.WGS84,
            null!,
            "Custom fitted",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("transform", exception.ParamName);
    }

    /// <summary>
    /// Verifies the factory preserves explicit fitted-axis metadata when it is supplied.
    /// </summary>
    [Fact]
    public void Factory_WithCustomAxes_PreservesSuppliedAxisInfo()
    {
        CoordinateSystemFactory factory = new();
        List<AxisInfo> fittedAxes =
        [
            new AxisInfo("Local latitude", AxisOrientationEnum.North),
            new AxisInfo("Local longitude", AxisOrientationEnum.East),
        ];

        FittedCoordinateSystem system = factory.CreateFittedCoordinateSystem(
            "Custom fitted",
            GeographicCoordinateSystem.WGS84,
            CreateTransform(),
            fittedAxes);

        Assert.Equal("Local latitude", system.GetAxis(0).Name);
        Assert.Equal(AxisOrientationEnum.North, system.GetAxis(0).Orientation);
        Assert.Equal("Local longitude", system.GetAxis(1).Name);
        Assert.Equal(AxisOrientationEnum.East, system.GetAxis(1).Orientation);
    }

    /// <summary>
    /// Verifies that WKT contains the fitted keyword, transform, and base coordinate system.
    /// </summary>
    [Fact]
    public void WKT_ContainsTransformAndBaseCoordinateSystem()
    {
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem();
        AffineTransform transform = CreateTransform();
        FittedCoordinateSystem system = CreateSystem(baseCoordinateSystem, transform, authority: "EPSG", authorityCode: 910001);

        Assert.Equal($"FITTED_CS[\"Custom fitted\", {transform.WKT}, {baseCoordinateSystem.WKT}]", system.WKT);
    }

    /// <summary>
    /// Verifies that XML is not supported.
    /// </summary>
    [Fact]
    public void XML_ThrowsNotSupportedException()
    {
        FittedCoordinateSystem system = CreateSystem();

        Assert.Throws<NotSupportedException>(() => system.XML);
    }

    /// <summary>
    /// Verifies that <see cref="FittedCoordinateSystem.ToXml"/> is not supported.
    /// </summary>
    [Fact]
    public void ToXml_ThrowsNotSupportedException()
    {
        FittedCoordinateSystem system = CreateSystem();

        Assert.Throws<NotSupportedException>(() => system.ToXml());
    }

    /// <summary>
    /// Verifies that <see cref="FittedCoordinateSystem.ToWktNode()"/> exposes the fitted coordinate system structure.
    /// </summary>
    [Fact]
    public void ToWktNode_ReturnsExpectedStructure()
    {
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem();
        AffineTransform transform = CreateTransform();
        FittedCoordinateSystem system = CreateSystem(baseCoordinateSystem, transform);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal("FITTED_CS", node.Keyword);
        Assert.Equal("Custom fitted", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
        Assert.Equal(transform.WKT, Assert.IsType<WktIdentifier>(node.Children[1]).Name);
        Assert.Equal("GEOGCS", Assert.IsType<WktKeywordNode>(node.Children[2]).Keyword);
        Assert.Equal(system.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="FittedCoordinateSystem.ToBase"/> returns the transform WKT.
    /// </summary>
    [Fact]
    public void ToBase_ReturnsTransformWkt()
    {
        AffineTransform transform = CreateTransform();
        FittedCoordinateSystem system = CreateSystem(transform: transform);

        Assert.Equal(transform.WKT, system.ToBase());
    }

    /// <summary>
    /// Verifies that <see cref="FittedCoordinateSystem.GetUnits"/> delegates to the base coordinate system.
    /// </summary>
    /// <param name="dimension">The dimension index.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    public void GetUnits_DelegatesToBaseCoordinateSystem(int dimension)
    {
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem(angularUnit: AngularUnit.Grad);
        FittedCoordinateSystem system = CreateSystem(baseCoordinateSystem: baseCoordinateSystem);
        IUnit unit = system.GetUnits(dimension);

        Assert.True(unit.EqualParams(AngularUnit.Grad));
    }

    /// <summary>
    /// Verifies that equivalent fitted coordinate systems compare equal.
    /// </summary>
    [Fact]
    public void EqualParams_SameValues_ReturnsTrue()
    {
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem();
        AffineTransform transform = CreateTransform();
        FittedCoordinateSystem first = CreateSystem(baseCoordinateSystem, transform, name: "First");
        FittedCoordinateSystem second = CreateSystem(baseCoordinateSystem, transform, name: "Second");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different base coordinate system causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentBaseCoordinateSystem_ReturnsFalse()
    {
        FittedCoordinateSystem first = CreateSystem(baseCoordinateSystem: CreateBaseCoordinateSystem());
        FittedCoordinateSystem second = CreateSystem(baseCoordinateSystem: CreateBaseCoordinateSystem(primeMeridian: PrimeMeridian.Paris));

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different transform causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentTransform_ReturnsFalse()
    {
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem();
        FittedCoordinateSystem first = CreateSystem(baseCoordinateSystem, CreateTransform(translationX: 10, translationY: 20));
        FittedCoordinateSystem second = CreateSystem(baseCoordinateSystem, CreateTransform(translationX: 30, translationY: 40));

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different fitted-axis orientation causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentAxisOrientation_ReturnsFalse()
    {
        CoordinateSystemFactory factory = new();
        GeographicCoordinateSystem baseCoordinateSystem = CreateBaseCoordinateSystem();
        FittedCoordinateSystem first = factory.CreateFittedCoordinateSystem(
            "First",
            baseCoordinateSystem,
            CreateTransform(),
            [new AxisInfo("Latitude", AxisOrientationEnum.North), new AxisInfo("Longitude", AxisOrientationEnum.East)]);
        FittedCoordinateSystem second = factory.CreateFittedCoordinateSystem(
            "Second",
            baseCoordinateSystem,
            CreateTransform(),
            [new AxisInfo("Latitude", AxisOrientationEnum.East), new AxisInfo("Longitude", AxisOrientationEnum.North)]);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different object type causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(CreateSystem().EqualParams("not a coordinate system"));
    }

    private static FittedCoordinateSystem CreateSystem(
        CoordinateSystem? baseCoordinateSystem = null,
        MathTransform? transform = null,
        string name = "Custom fitted",
        string authority = "",
        long authorityCode = -1)
    {
        return new FittedCoordinateSystem(
            baseCoordinateSystem ?? CreateBaseCoordinateSystem(),
            transform ?? CreateTransform(),
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeographicCoordinateSystem CreateBaseCoordinateSystem(
        List<AxisInfo>? axisInfo = null,
        AngularUnit? angularUnit = null,
        HorizontalDatum? horizontalDatum = null,
        PrimeMeridian? primeMeridian = null)
    {
        return new GeographicCoordinateSystem(
            angularUnit ?? AngularUnit.Degrees,
            horizontalDatum ?? HorizontalDatum.WGS84,
            primeMeridian ?? PrimeMeridian.Greenwich,
            axisInfo ?? CreateDefaultAxisInfo(),
            "Base geographic",
            "EPSG",
            4326,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static AffineTransform CreateTransform(double translationX = 10, double translationY = 20)
    {
        return new AffineTransform(
            1,
            0,
            translationX,
            0,
            1,
            translationY);
    }

    private static List<AxisInfo> CreateDefaultAxisInfo()
    {
        return
        [
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North),
        ];
    }

    private static List<AxisInfo> CreateCustomAxisInfo()
    {
        return
        [
            new AxisInfo("Longitude", AxisOrientationEnum.East),
            new AxisInfo("Latitude", AxisOrientationEnum.North),
        ];
    }
}
