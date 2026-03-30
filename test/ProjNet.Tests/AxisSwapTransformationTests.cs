// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for axis-swap and unit-conversion behavior in coordinate transformations built by
/// <see cref="CoordinateTransformationFactory"/>.
/// </summary>
public class AxisSwapTransformationTests
{
    private static readonly double[] GeographicAxisInput = [12d, 55d];
    private static readonly double[] ProjectedAxisInput = [500000d, 6100000d];
    private static readonly double[] UnitConversionInput = [100d, 200d];
    private static readonly double[] RadianConversionInput = [180d, 90d];

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies that a transformation from a Lon/Lat (East/North) to a Lat/Lon (North/East) geographic
    /// coordinate system swaps the two coordinate values.
    /// </summary>
    [Fact]
    public void GeographicAxisSwapLonLatToLatLonSwapsCoordinates()
    {
        GeographicCoordinateSystem source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        GeographicCoordinateSystem target = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target NE",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lat", AxisOrientationEnum.North),
            new AxisInfo("Lon", AxisOrientationEnum.East));

        MathTransform transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(GeographicAxisInput);

        Assert.Equal(55d, transformed[0], 12);
        Assert.Equal(12d, transformed[1], 12);
    }

    /// <summary>
    /// Verifies that a transformation from an East/North to a West/South geographic coordinate system
    /// negates both coordinate values.
    /// </summary>
    [Fact]
    public void GeographicAxisSwapEastNorthToWestSouthNegatesAxes()
    {
        GeographicCoordinateSystem source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        GeographicCoordinateSystem target = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target WS",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.West),
            new AxisInfo("Lat", AxisOrientationEnum.South));

        MathTransform transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(GeographicAxisInput);

        Assert.Equal(-12d, transformed[0], 12);
        Assert.Equal(-55d, transformed[1], 12);
    }

    /// <summary>
    /// Verifies that a transformation from an East/North to a North/East projected coordinate system
    /// swaps the easting and northing values.
    /// </summary>
    [Fact]
    public void ProjectedAxisSwapEastNorthToNorthEastSwapsProjectedAxes()
    {
        var projectionParameters = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0d),
            new("central_meridian", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
        };

        IProjection projection = CoordinateSystemFactory.CreateProjection("Mercator", "mercator", projectionParameters);
        GeographicCoordinateSystem geographic = GeographicCoordinateSystem.WGS84;
        ProjectedCoordinateSystem source = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Source EN",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        ProjectedCoordinateSystem target = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Target NE",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("North", AxisOrientationEnum.North),
            new AxisInfo("East", AxisOrientationEnum.East));

        MathTransform transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(ProjectedAxisInput);

        Assert.Equal(6100000d, transformed[0], 8);
        Assert.Equal(500000d, transformed[1], 8);
    }

    /// <summary>
    /// Verifies that a transformation between two geographic coordinate systems that share the same axes
    /// but differ only in angular unit converts degree values to the equivalent radian values.
    /// </summary>
    [Fact]
    public void GeographicUnitConversionDegreesToRadiansConvertsCoordinates()
    {
        GeographicCoordinateSystem source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source Degrees",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        GeographicCoordinateSystem target = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target Radians",
            AngularUnit.Radian,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        MathTransform transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(RadianConversionInput);

        Assert.Equal(System.Math.PI, transformed[0], 12);
        Assert.Equal(System.Math.PI / 2d, transformed[1], 12);
    }

    /// <summary>
    /// Verifies that a transformation between two projected coordinate systems that share the same axes
    /// but differ only in linear unit converts metre values to the equivalent foot values.
    /// </summary>
    [Fact]
    public void ProjectedUnitConversionMetreToFootConvertsProjectedCoordinates()
    {
        IProjection projection = CreateMercatorProjection();
        GeographicCoordinateSystem geographic = GeographicCoordinateSystem.WGS84;

        ProjectedCoordinateSystem source = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Source Metre",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        ProjectedCoordinateSystem target = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Target Foot",
            geographic,
            projection,
            LinearUnit.Foot,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        MathTransform transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(UnitConversionInput);

        Assert.Equal(328.0839895013123d, transformed[0], 9);
        Assert.Equal(656.1679790026246d, transformed[1], 9);
    }

    /// <summary>
    /// Verifies that a transformation from a metre East/North to a foot North/East projected coordinate
    /// system simultaneously converts units from metres to feet and swaps the axis order.
    /// </summary>
    [Fact]
    public void ProjectedUnitAndAxisConversionMetreEastNorthToFootNorthEastConvertsAndSwaps()
    {
        IProjection projection = CreateMercatorProjection();
        GeographicCoordinateSystem geographic = GeographicCoordinateSystem.WGS84;

        ProjectedCoordinateSystem source = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Source Metre EN",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        ProjectedCoordinateSystem target = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "Target Foot NE",
            geographic,
            projection,
            LinearUnit.Foot,
            new AxisInfo("North", AxisOrientationEnum.North),
            new AxisInfo("East", AxisOrientationEnum.East));

        MathTransform transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target).MathTransform;
        double[] transformed = transform.Transform(UnitConversionInput);

        Assert.Equal(656.1679790026246d, transformed[0], 9);
        Assert.Equal(328.0839895013123d, transformed[1], 9);
    }

    private static IProjection CreateMercatorProjection()
    {
        var projectionParameters = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0d),
            new("central_meridian", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
        };

        return CoordinateSystemFactory.CreateProjection("Mercator", "mercator", projectionParameters);
    }
}
