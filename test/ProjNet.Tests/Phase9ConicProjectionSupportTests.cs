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

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class Phase9ConicProjectionSupportTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Validates that Equidistant Conic aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("eqdc")]
    [InlineData("equidistant_conic")]
    [InlineData("equidistant_conic_(spherical)")]
    public void SupportsEqdcProjectionAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(9.6d, 43.2d, 1e-8d)]
    public void SupportsEqdcProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("eqdc"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Validates that Bonne aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("bonne")]
    public void SupportsBonneProjectionAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(12.8d, 31.4d, 1e-8d)]
    public void SupportsBonneProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("bonne"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Validates that Perspective Conic aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("pconic")]
    [InlineData("perspective_conic")]
    public void SupportsPconicProjectionAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(-7.25d, 44.1d, 1e-8d)]
    public void SupportsPconicProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("pconic"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return
            $"PROJCS[\"Phase9-{projectionName}\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"standard_parallel_1\",20],PARAMETER[\"standard_parallel_2\",50],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static double[] CreatePoint(double x, double y)
    {
        return new[] { x, y };
    }
}
