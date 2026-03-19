// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
namespace ProjNET.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

public class Phase5AxisSwapTransformationTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    [Fact]
    public void GeographicAxisSwap_LonLatToLatLon_SwapsCoordinates()
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
        double[] transformed = transform.Transform(new[] { 12d, 55d });

        Assert.Equal(55d, transformed[0], 12);
        Assert.Equal(12d, transformed[1], 12);
    }

    [Fact]
    public void GeographicAxisSwap_EastNorthToWestSouth_NegatesAxes()
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
        double[] transformed = transform.Transform(new[] { 12d, 55d });

        Assert.Equal(-12d, transformed[0], 12);
        Assert.Equal(-55d, transformed[1], 12);
    }

    [Fact]
    public void ProjectedAxisSwap_EastNorthToNorthEast_SwapsProjectedAxes()
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
        double[] transformed = transform.Transform(new[] { 500000d, 6100000d });

        Assert.Equal(6100000d, transformed[0], 8);
        Assert.Equal(500000d, transformed[1], 8);
    }
}
