// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="BoundCoordinateSystem"/> and <see cref="BoundTransformation"/>.
/// </summary>
public class BoundCoordinateSystemTests
{
    /// <summary>
    /// Verifies that the constructor stores the supplied values and copies the source axes.
    /// </summary>
    [Fact]
    public void Constructor_SetsPropertiesAndCopiesSourceAxes()
    {
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem(
            "Source",
            CreateCustomAxisInfo(),
            AngularUnit.Grad,
            PrimeMeridian.Paris);
        GeographicCoordinateSystem targetCoordinateSystem = GeographicCoordinateSystem.WGS84;
        BoundTransformation transformation = new("Geocentric translations", new Wgs84ConversionInfo(1, 2, 3, 0, 0, 0, 0));
        var system = new BoundCoordinateSystem(
            sourceCoordinateSystem,
            targetCoordinateSystem,
            transformation,
            "Bound source",
            "TEST",
            42,
            "alias",
            "abbr",
            "remarks");

        Assert.Equal("Bound source", system.Name);
        Assert.Equal("TEST", system.Authority);
        Assert.Equal(42, system.AuthorityCode);
        Assert.Equal("alias", system.Alias);
        Assert.Equal("abbr", system.Abbreviation);
        Assert.Equal("remarks", system.Remarks);
        Assert.Same(sourceCoordinateSystem, system.SourceCoordinateSystem);
        Assert.Same(targetCoordinateSystem, system.TargetCoordinateSystem);
        Assert.Same(transformation, system.Transformation);
        Assert.Equal(sourceCoordinateSystem.Dimension, system.Dimension);
        Assert.NotSame(sourceCoordinateSystem.GetAxis(0), system.GetAxis(0));
        Assert.Equal(sourceCoordinateSystem.GetAxis(0).Name, system.GetAxis(0).Name);
        Assert.Equal(sourceCoordinateSystem.GetAxis(0).Orientation, system.GetAxis(0).Orientation);
        Assert.NotSame(sourceCoordinateSystem.GetAxis(1), system.GetAxis(1));
        Assert.Equal(sourceCoordinateSystem.GetAxis(1).Name, system.GetAxis(1).Name);
        Assert.Equal(sourceCoordinateSystem.GetAxis(1).Orientation, system.GetAxis(1).Orientation);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null source coordinate system.
    /// </summary>
    [Fact]
    public void Constructor_NullSourceCoordinateSystem_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BoundCoordinateSystem(
            null!,
            GeographicCoordinateSystem.WGS84,
            CreateWgs84Transformation(),
            "Bound source",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("sourceCoordinateSystem", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null target coordinate system.
    /// </summary>
    [Fact]
    public void Constructor_NullTargetCoordinateSystem_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BoundCoordinateSystem(
            CreateCoordinateSystem("Source"),
            null!,
            CreateWgs84Transformation(),
            "Bound source",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("targetCoordinateSystem", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null bound transformation.
    /// </summary>
    [Fact]
    public void Constructor_NullTransformation_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BoundCoordinateSystem(
            CreateCoordinateSystem("Source"),
            GeographicCoordinateSystem.WGS84,
            null!,
            "Bound source",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty));

        Assert.Equal("transformation", exception.ParamName);
    }

    /// <summary>
    /// Verifies that WKT falls back to the source coordinate system representation.
    /// </summary>
    [Fact]
    public void WKT_DelegatesToSourceCoordinateSystem()
    {
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Source", angularUnit: AngularUnit.Grad, primeMeridian: PrimeMeridian.Paris);
        BoundCoordinateSystem system = CreateSystem(sourceCoordinateSystem: sourceCoordinateSystem);

        Assert.Equal(sourceCoordinateSystem.WKT, system.WKT);
    }

    /// <summary>
    /// Verifies that XML falls back to the source coordinate system representation.
    /// </summary>
    [Fact]
    public void XML_DelegatesToSourceCoordinateSystem()
    {
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Source", angularUnit: AngularUnit.Grad, primeMeridian: PrimeMeridian.Paris);
        BoundCoordinateSystem system = CreateSystem(sourceCoordinateSystem: sourceCoordinateSystem);

        Assert.Equal(sourceCoordinateSystem.XML, system.XML);
    }

    /// <summary>
    /// Verifies that <see cref="BoundCoordinateSystem.ToXml"/> delegates to the source coordinate system.
    /// </summary>
    [Fact]
    public void ToXml_DelegatesToSourceCoordinateSystem()
    {
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Source");
        BoundCoordinateSystem system = CreateSystem(sourceCoordinateSystem: sourceCoordinateSystem);

        Assert.True(XNode.DeepEquals(sourceCoordinateSystem.ToXml(), system.ToXml()));
    }

    /// <summary>
    /// Verifies that <see cref="BoundCoordinateSystem.ToWktNode()"/> exposes the source coordinate system structure.
    /// </summary>
    [Fact]
    public void ToWktNode_ReturnsSourceCoordinateSystemStructure()
    {
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Source");
        BoundCoordinateSystem system = CreateSystem(sourceCoordinateSystem: sourceCoordinateSystem);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal("GEOGCS", node.Keyword);
        Assert.Equal(sourceCoordinateSystem.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that WKT2 output emits a <c>BOUNDCRS</c> node and roundtrips through the native reader.
    /// </summary>
    [Fact]
    public void ToWktNode_WithWkt22019_RoundTripsAsBoundCrs()
    {
        CoordinateSystemFactory factory = new();
        BoundCoordinateSystem system = CreateSystem();
        string wkt = system.ToWktNode(WktVersion.Wkt22019).ToString();
        BoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(factory, wkt);

        Assert.StartsWith("BOUNDCRS[", wkt, StringComparison.Ordinal);
        Assert.True(system.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that <see cref="BoundCoordinateSystem.GetUnits"/> delegates to the source coordinate system.
    /// </summary>
    /// <param name="dimension">The dimension index.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GetUnits_DelegatesToSourceCoordinateSystem(int dimension)
    {
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Source", angularUnit: AngularUnit.Grad);
        BoundCoordinateSystem system = CreateSystem(sourceCoordinateSystem: sourceCoordinateSystem);

        IUnit unit = system.GetUnits(dimension);

        Assert.True(unit.EqualParams(AngularUnit.Grad));
    }

    /// <summary>
    /// Verifies that equivalent bound coordinate systems compare equal.
    /// </summary>
    [Fact]
    public void EqualParams_SameValues_ReturnsTrue()
    {
        BoundCoordinateSystem first = CreateSystem(
            sourceCoordinateSystem: CreateCoordinateSystem("Source one"),
            targetCoordinateSystem: CreateCoordinateSystem("Target one"),
            transformation: CreateWgs84Transformation(),
            name: "First");
        BoundCoordinateSystem second = CreateSystem(
            sourceCoordinateSystem: CreateCoordinateSystem("Source two"),
            targetCoordinateSystem: CreateCoordinateSystem("Target two"),
            transformation: CreateWgs84Transformation(),
            name: "Second");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different target coordinate system causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentTargetCoordinateSystem_ReturnsFalse()
    {
        BoundCoordinateSystem first = CreateSystem(targetCoordinateSystem: CreateCoordinateSystem("Target one", primeMeridian: PrimeMeridian.Greenwich));
        BoundCoordinateSystem second = CreateSystem(targetCoordinateSystem: CreateCoordinateSystem("Target two", primeMeridian: PrimeMeridian.Paris));

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different transformation causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentTransformation_ReturnsFalse()
    {
        BoundCoordinateSystem first = CreateSystem(transformation: new BoundTransformation("Geocentric translations", new Wgs84ConversionInfo(1, 2, 3, 0, 0, 0, 0)));
        BoundCoordinateSystem second = CreateSystem(transformation: new BoundTransformation("Geocentric translations", new Wgs84ConversionInfo(4, 5, 6, 0, 0, 0, 0)));

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

    /// <summary>
    /// Verifies that the factory creates a bound coordinate system with the supplied values.
    /// </summary>
    [Fact]
    public void Factory_CreateBoundCoordinateSystem_ReturnsExpectedSystem()
    {
        var factory = new CoordinateSystemFactory();
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Source");
        GeographicCoordinateSystem targetCoordinateSystem = GeographicCoordinateSystem.WGS84;
        BoundTransformation transformation = CreateWgs84Transformation();

        BoundCoordinateSystem system = factory.CreateBoundCoordinateSystem("Factory bound", sourceCoordinateSystem, targetCoordinateSystem, transformation);

        Assert.Equal("Factory bound", system.Name);
        Assert.Same(sourceCoordinateSystem, system.SourceCoordinateSystem);
        Assert.Same(targetCoordinateSystem, system.TargetCoordinateSystem);
        Assert.Same(transformation, system.Transformation);
    }

    /// <summary>
    /// Verifies the runtime factory normalizes bound sources through the legacy path while keeping the public source and target coordinate systems intact.
    /// </summary>
    [Fact]
    public void CoordinateTransformationFactory_CreateFromCoordinateSystems_WithBoundSource_MatchesLegacyRuntime()
    {
        HorizontalDatum sourceDatum = CreateHorizontalDatum();
        GeographicCoordinateSystem sourceCoordinateSystem = CreateCoordinateSystem("Custom geographic", horizontalDatum: sourceDatum);
        GeographicCoordinateSystem legacySourceCoordinateSystem = CreateCoordinateSystem("Custom geographic", horizontalDatum: CreateHorizontalDatum(CreateWgs84Parameters()));
        GeographicCoordinateSystem targetCoordinateSystem = GeographicCoordinateSystem.WGS84;
        BoundCoordinateSystem boundSource = CreateSystem(
            sourceCoordinateSystem: sourceCoordinateSystem,
            targetCoordinateSystem: targetCoordinateSystem,
            transformation: CreateWgs84Transformation());

        var factory = new CoordinateTransformationFactory();
        ICoordinateTransformation boundTransformation = factory.CreateFromCoordinateSystems(boundSource, targetCoordinateSystem);
        ICoordinateTransformation legacyTransformation = factory.CreateFromCoordinateSystems(legacySourceCoordinateSystem, targetCoordinateSystem);

        double[] boundOutput = boundTransformation.MathTransform.Transform([10d, 50d]);
        double[] legacyOutput = legacyTransformation.MathTransform.Transform([10d, 50d]);

        Assert.Same(boundSource, boundTransformation.SourceCS);
        Assert.Same(targetCoordinateSystem, boundTransformation.TargetCS);
        Assert.Equal(legacyOutput[0], boundOutput[0], 9);
        Assert.Equal(legacyOutput[1], boundOutput[1], 9);
    }

    /// <summary>
    /// Verifies that the WGS84-parameter constructor stores the supplied values.
    /// </summary>
    [Fact]
    public void BoundTransformation_Wgs84Constructor_SetsProperties()
    {
        Wgs84ConversionInfo parameters = new(1, 2, 3, 4, 5, 6, 7);
        BoundTransformation transformation = new("Position Vector transformation", parameters);

        Assert.Equal("Position Vector transformation", transformation.MethodName);
        Assert.Same(parameters, transformation.Wgs84Parameters);
        Assert.Null(transformation.ParameterFileName);
        Assert.True(transformation.UsesWgs84Parameters);
        Assert.False(transformation.UsesParameterFile);
    }

    /// <summary>
    /// Verifies that the parameter-file constructor stores the supplied values.
    /// </summary>
    [Fact]
    public void BoundTransformation_ParameterFileConstructor_SetsProperties()
    {
        BoundTransformation transformation = new("Geographic3D to GravityRelatedHeight (EGM)", "us_nga_egm96_15.tif");

        Assert.Equal("Geographic3D to GravityRelatedHeight (EGM)", transformation.MethodName);
        Assert.Null(transformation.Wgs84Parameters);
        Assert.Equal("us_nga_egm96_15.tif", transformation.ParameterFileName);
        Assert.False(transformation.UsesWgs84Parameters);
        Assert.True(transformation.UsesParameterFile);
    }

    /// <summary>
    /// Verifies that a missing method name is rejected.
    /// </summary>
    /// <param name="methodName">The invalid method name.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void BoundTransformation_InvalidMethodName_ThrowsArgumentException(string? methodName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BoundTransformation(methodName!, CreateWgs84Parameters()));

        Assert.Equal("methodName", exception.ParamName);
    }

    /// <summary>
    /// Verifies that the WGS84-parameter constructor rejects a null parameter object.
    /// </summary>
    [Fact]
    public void BoundTransformation_NullWgs84Parameters_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new BoundTransformation("Geocentric translations", (Wgs84ConversionInfo)null!));

        Assert.Equal("wgs84Parameters", exception.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid parameter file name is rejected.
    /// </summary>
    /// <param name="parameterFileName">The invalid parameter file name.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void BoundTransformation_InvalidParameterFileName_ThrowsArgumentException(string? parameterFileName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new BoundTransformation("Grid interpolation", parameterFileName!));

        Assert.Equal("parameterFileName", exception.ParamName);
    }

    /// <summary>
    /// Verifies that equivalent WGS84-parameter transformations compare equal.
    /// </summary>
    [Fact]
    public void BoundTransformation_EqualsEquivalentWgs84Transformations_ReturnsTrue()
    {
        BoundTransformation first = new("Geocentric translations", CreateWgs84Parameters());
        BoundTransformation second = new("Geocentric translations", CreateWgs84Parameters());

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that different transformation representations do not compare equal.
    /// </summary>
    [Fact]
    public void BoundTransformation_EqualsDifferentRepresentation_ReturnsFalse()
    {
        BoundTransformation first = new("Geocentric translations", CreateWgs84Parameters());
        BoundTransformation second = new("Geocentric translations", "us_nga_egm96_15.tif");

        Assert.False(first.Equals(second));
        Assert.False(first.Equals((object?)second));
    }

    private static BoundCoordinateSystem CreateSystem(
        CoordinateSystem? sourceCoordinateSystem = null,
        CoordinateSystem? targetCoordinateSystem = null,
        BoundTransformation? transformation = null,
        string name = "Bound source",
        string authority = "",
        long authorityCode = -1)
    {
        return new BoundCoordinateSystem(
            sourceCoordinateSystem ?? CreateCoordinateSystem("Source"),
            targetCoordinateSystem ?? GeographicCoordinateSystem.WGS84,
            transformation ?? CreateWgs84Transformation(),
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeographicCoordinateSystem CreateCoordinateSystem(
        string name,
        List<AxisInfo>? axisInfo = null,
        AngularUnit? angularUnit = null,
        PrimeMeridian? primeMeridian = null,
        HorizontalDatum? horizontalDatum = null)
    {
        return new GeographicCoordinateSystem(
            angularUnit ?? AngularUnit.Degrees,
            horizontalDatum ?? HorizontalDatum.WGS84,
            primeMeridian ?? PrimeMeridian.Greenwich,
            axisInfo ?? [new AxisInfo("Lon", AxisOrientationEnum.East), new AxisInfo("Lat", AxisOrientationEnum.North)],
            name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static List<AxisInfo> CreateCustomAxisInfo()
    {
        return [new AxisInfo("Northing", AxisOrientationEnum.North), new AxisInfo("Easting", AxisOrientationEnum.East)];
    }

    private static BoundTransformation CreateWgs84Transformation() => new("Position Vector transformation (geog2D domain)", CreateWgs84Parameters());

    private static HorizontalDatum CreateHorizontalDatum(Wgs84ConversionInfo? wgs84Parameters = null)
        => new(Ellipsoid.GRS80, wgs84Parameters, DatumType.HD_Geocentric, "Custom datum", string.Empty, -1, string.Empty, string.Empty, string.Empty);

    private static Wgs84ConversionInfo CreateWgs84Parameters() => new(1, 2, 3, 4, 5, 6, 7);
}
