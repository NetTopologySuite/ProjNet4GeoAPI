// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class AxisSwapTransformationTests
{
    private static readonly double[] GeographicAxisInput = { 12d, 55d };
    private static readonly double[] ProjectedAxisInput = { 500000d, 6100000d };
    private static readonly double[] UnitConversionInput = { 100d, 200d };
    private static readonly double[] RadianConversionInput = { 180d, 90d };

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeographicAxisSwapLonLatToLatLonSwapsCoordinates()
    {
        var source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target NE",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lat", AxisOrientationEnum.North),
            new AxisInfo("Lon", AxisOrientationEnum.East));

        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(GeographicAxisInput);

        Assert.Equal(55d, transformed[0], 12);
        Assert.Equal(12d, transformed[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeographicAxisSwapEastNorthToWestSouthNegatesAxes()
    {
        var source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target WS",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.West),
            new AxisInfo("Lat", AxisOrientationEnum.South));

        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(GeographicAxisInput);

        Assert.Equal(-12d, transformed[0], 12);
        Assert.Equal(-55d, transformed[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ProjectedAxisSwapEastNorthToNorthEastSwapsProjectedAxes()
    {
        var projectionParameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
        };

        var projection = CoordinateSystemFactory.CreateProjection("Mercator", "mercator", projectionParameters);
        var geographic = GeographicCoordinateSystem.WGS84;
        var source = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Source EN",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Target NE",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("North", AxisOrientationEnum.North),
            new AxisInfo("East", AxisOrientationEnum.East));

        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(ProjectedAxisInput);

        Assert.Equal(6100000d, transformed[0], 8);
        Assert.Equal(500000d, transformed[1], 8);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeographicUnitConversionDegreesToRadiansConvertsCoordinates()
    {
        var source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source Degrees",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target Radians",
            AngularUnit.Radian,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(RadianConversionInput);

        Assert.Equal(System.Math.PI, transformed[0], 12);
        Assert.Equal(System.Math.PI / 2d, transformed[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ProjectedUnitConversionMetreToFootConvertsProjectedCoordinates()
    {
        var projection = CreateMercatorProjection();
        var geographic = GeographicCoordinateSystem.WGS84;

        var source = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Source Metre",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Target Foot",
            geographic,
            projection,
            LinearUnit.Foot,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(UnitConversionInput);

        Assert.Equal(328.0839895013123d, transformed[0], 9);
        Assert.Equal(656.1679790026246d, transformed[1], 9);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ProjectedUnitAndAxisConversionMetreEastNorthToFootNorthEastConvertsAndSwaps()
    {
        var projection = CreateMercatorProjection();
        var geographic = GeographicCoordinateSystem.WGS84;

        var source = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Source Metre EN",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Target Foot NE",
            geographic,
            projection,
            LinearUnit.Foot,
            new AxisInfo("North", AxisOrientationEnum.North),
            new AxisInfo("East", AxisOrientationEnum.East));

        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(UnitConversionInput);

        Assert.Equal(656.1679790026246d, transformed[0], 9);
        Assert.Equal(328.0839895013123d, transformed[1], 9);
    }

    private static IProjection CreateMercatorProjection()
    {
        var projectionParameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
        };

        return CoordinateSystemFactory.CreateProjection("Mercator", "mercator", projectionParameters);
    }
}

