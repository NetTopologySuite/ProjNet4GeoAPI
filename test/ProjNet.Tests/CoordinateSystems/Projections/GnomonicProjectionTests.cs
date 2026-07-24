// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates gnomonic projection behavior against PROJ reference vectors.
/// </summary>
public class GnomonicProjectionTests
{
    private const double ForwardTolerance = 5e-5d;
    private const double InverseTolerance = 2e-7d;

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies ellipsoidal forward vectors for the gnomonic projection.
    /// </summary>
    /// <param name="latitudeOfOrigin">Projection center latitude.</param>
    /// <param name="longitude">Input longitude.</param>
    /// <param name="latitude">Input latitude.</param>
    /// <param name="expectedX">Expected x coordinate.</param>
    /// <param name="expectedY">Expected y coordinate.</param>
    [Theory]
    [InlineData(0d, 10d, 80d, 0.176333043342897d, 5.723194021247466d)]
    [InlineData(0d, 20d, 70d, 0.364056496605216d, 2.903717319916686d)]
    [InlineData(0d, 80d, 80d, 5.713366365209261d, 32.729848361175208d)]
    [InlineData(0d, 0d, 89.99d, 0d, 5700.9221603850146d)]
    [InlineData(90d, 45d, 45d, 0.707863156628200d, -0.707863156628200d)]
    [InlineData(-90d, 45d, -45d, 0.707863156628200d, 0.707863156628200d)]
    [InlineData(45d, 0d, 0d, 0d, -0.989689577444773d)]
    [InlineData(45d, 0d, 90d, 0d, 1.002503117123815d)]
    [InlineData(45d, 0d, -45d, 0d, -154.86226463965525d)]
    public void EllipsoidalForwardMatchesProjReference(
        double latitudeOfOrigin,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(latitudeOfOrigin));

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, ForwardTolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, ForwardTolerance);
    }

    /// <summary>
    /// Verifies ellipsoidal inverse vectors for the gnomonic projection.
    /// </summary>
    /// <param name="latitudeOfOrigin">Projection center latitude.</param>
    /// <param name="x">Input x coordinate.</param>
    /// <param name="y">Input y coordinate.</param>
    /// <param name="expectedLongitude">Expected longitude.</param>
    /// <param name="expectedLatitude">Expected latitude.</param>
    [Theory]
    [InlineData(0d, 0.176333043342897d, 5.723194021247466d, 10d, 80d)]
    [InlineData(0d, 0.364056496605216d, 2.903717319916686d, 20d, 70d)]
    [InlineData(0d, 5.713366365209261d, 32.729848361175208d, 80d, 80d)]
    [InlineData(0d, 0d, 5700.9221603850146d, 0d, 89.99d)]
    [InlineData(90d, 0.707863156628200d, -0.707863156628200d, 45d, 45d)]
    [InlineData(90d, 0d, -127.48350842637615d, 0d, 0d)]
    [InlineData(45d, 0d, -0.989689577444773d, 0d, 0d)]
    [InlineData(45d, 0d, 1.002503117123815d, 0d, 90d)]
    [InlineData(45d, 0d, -154.86226463965525d, 0d, -45d)]
    public void EllipsoidalInverseMatchesProjReference(
        double latitudeOfOrigin,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(latitudeOfOrigin));

        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected,
            projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, InverseTolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, InverseTolerance);
    }

    /// <summary>
    /// Verifies points beyond the supported ellipsoidal domain fail in the forward direction.
    /// </summary>
    [Theory]
    [InlineData(0d, 180d, 89.99d)]
    [InlineData(90d, 0d, -0.5d)]
    [InlineData(90d, 90d, -0.5d)]
    [InlineData(-90d, 0d, 0.5d)]
    [InlineData(-90d, 90d, 0.5d)]
    [InlineData(45d, 0d, -45.5d)]
    public void EllipsoidalForwardOutsideDomainReturnsNaN(double latitudeOfOrigin, double longitude, double latitude)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(latitudeOfOrigin));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.True(double.IsNaN(projectedPoint[0]));
        Assert.True(double.IsNaN(projectedPoint[1]));
    }

    private static string BuildProjectedWkt(double latitudeOfOrigin)
    {
        return FormattableString.Invariant(
            $"PROJCS[\"Regression-gnom-{latitudeOfOrigin}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"GIE Ellipsoid\",1,200]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"gnom\"],PARAMETER[\"latitude_of_origin\",{latitudeOfOrigin}],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
