// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates van der Grinten I (<c>vandg</c>) projection support.
/// </summary>
public class Phase5VandgProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies that vandg aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("vandg")]
    [InlineData("VanDerGrinten")]
    [InlineData("van_der_grinten")]
    public void SupportsVandgAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, 6400000d));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward/inverse roundtrip stability for vandg aliases.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="tolerance">Maximum absolute roundtrip delta (degrees).</param>
    [Theory]
    [InlineData("vandg", 2d, 1d, 2e-8)]
    [InlineData("vandg", -2d, -1d, 2e-8)]
    [InlineData("VanDerGrinten", 30d, -20d, 2e-8)]
    public void SupportsVandgRoundtrip(string projectionName, double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, 6400000d));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData("vandg", 2d, 1d, 223395.249543407d, 111704.596633675d)]
    [InlineData("vandg", 2d, -1d, 223395.249543407d, -111704.596633675d)]
    [InlineData("vandergrinten", -2d, 1d, -223395.249543407d, 111704.596633675d)]
    [InlineData("van_der_grinten_i", -2d, -1d, -223395.249543407d, -111704.596633675d)]
    [InlineData("vandg", 179.9d, 50d, 18549161.7268d, 7731305.7162d)]
    [InlineData("vandg", 180.1d, 50d, -18549161.7268d, 7731305.7162d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, 6400000d));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 3.5e-4);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 3.5e-4);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData("vandg", 200d, 100d, 0.001790494d, 0.000895247d)]
    [InlineData("vandergrinten", 200d, -100d, 0.001790494d, -0.000895247d)]
    [InlineData("van_der_grinten", -200d, 100d, -0.001790494d, 0.000895247d)]
    [InlineData("van_der_grinten_i", -200d, -100d, -0.001790494d, -0.000895247d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, 6400000d));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 1e-9);
    }

    private static string BuildProjectedWkt(string projectionName, double radius)
    {
        string radiusText = radius.ToString(CultureInfo.InvariantCulture);
        return
            $"PROJCS[\"Phase5-{projectionName}\",GEOGCS[\"Sphere\",DATUM[\"Sphere_Datum\",SPHEROID[\"Sphere\",{radiusText},0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
