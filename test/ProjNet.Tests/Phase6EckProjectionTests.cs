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
/// Validates Eckert I-V projection support.
/// </summary>
public class Phase6EckProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for eckert projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("eck1")]
    [InlineData("Eckert_I")]
    [InlineData("eck2")]
    [InlineData("Eckert_II")]
    [InlineData("eck3")]
    [InlineData("Eckert_III")]
    [InlineData("eck4")]
    [InlineData("Eckert_IV")]
    [InlineData("eck5")]
    [InlineData("Eckert_V")]
    public void SupportsEckAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for Eckert I-V.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    [Theory]
    [InlineData("eck1", 2d, 1d, 204680.888202951d, 102912.178426065d)]
    [InlineData("eck1", -2d, -1d, -204680.888202951d, -102912.178426065d)]
    [InlineData("eck2", 2d, 1d, 204472.870907960d, 121633.734975242d)]
    [InlineData("eck2", -2d, -1d, -204472.870907960d, -121633.734975242d)]
    [InlineData("eck3", 2d, 1d, 188652.015721538d, 94328.919337031d)]
    [InlineData("eck3", -2d, -1d, -188652.015721538d, -94328.919337031d)]
    [InlineData("eck4", 2d, 1d, 188646.389356416d, 132268.540174065d)]
    [InlineData("eck4", -2d, -1d, -188646.389356416d, -132268.540174065d)]
    [InlineData("eck5", 2d, 1d, 197031.392134061d, 98523.198847227d)]
    [InlineData("eck5", -2d, -1d, -197031.392134061d, -98523.198847227d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-6);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for Eckert I-V.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    [Theory]
    [InlineData("eck1", 200d, 100d, 0.001943415d, 0.000971702d)]
    [InlineData("eck1", -200d, -100d, -0.001943415d, -0.000971702d)]
    [InlineData("eck2", 200d, 100d, 0.001943415d, 0.000824804d)]
    [InlineData("eck2", -200d, -100d, -0.001943415d, -0.000824804d)]
    [InlineData("eck3", 200d, 100d, 0.002120241d, 0.001060120d)]
    [InlineData("eck3", -200d, -100d, -0.002120241d, -0.001060120d)]
    [InlineData("eck4", 200d, 100d, 0.002120241d, 0.000756015d)]
    [InlineData("eck4", -200d, -100d, -0.002120241d, -0.000756015d)]
    [InlineData("eck5", 200d, 100d, 0.002029979d, 0.001014989d)]
    [InlineData("eck5", -200d, -100d, -0.002029979d, -0.001014989d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies eck4 edge-of-domain vectors from builtins.
    /// </summary>
    /// <param name="x">Projected x meters.</param>
    /// <param name="y">Projected y meters.</param>
    /// <param name="expectedLon">Expected longitude degrees.</param>
    /// <param name="expectedLat">Expected latitude degrees.</param>
    [Theory]
    [InlineData(-8489602.74033281d, 8489602.74033281d, -180d, 90d)]
    [InlineData(8489602.74033281d, 8489602.74033281d, 180d, 90d)]
    [InlineData(-16979205.4807d, 0d, -180d, 0d)]
    [InlineData(16979205.4807d, 0d, 180d, 0d)]
    [InlineData(-8489602.74033281d, -8489602.74033281d, -180d, -90d)]
    [InlineData(8489602.74033281d, -8489602.74033281d, 180d, -90d)]
    public void MatchesProjBuiltinsEck4EdgeCases(double x, double y, double expectedLon, double expectedLat)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("eck4"));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLon), 0d, 1e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLat), 0d, 1e-9);
    }

    /// <summary>
    /// Verifies eck4 out-of-domain inverse inputs are rejected.
    /// </summary>
    /// <param name="x">Projected x meters.</param>
    /// <param name="y">Projected y meters.</param>
    [Theory]
    [InlineData(-8489602.75d, 8489602.74033281d)]
    [InlineData(8489602.75d, 8489602.74033281d)]
    [InlineData(0d, 8489602.75d)]
    [InlineData(-16979205.49d, 0d)]
    [InlineData(16979205.49d, 0d)]
    [InlineData(-8489602.75d, -8489602.74033281d)]
    [InlineData(8489602.75d, -8489602.74033281d)]
    [InlineData(0d, -8489602.75d)]
    public void RejectsEck4OutsideProjectionDomain(double x, double y)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("eck4"));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        Assert.Throws<ArgumentException>(() => inverse.MathTransform.Transform(CreatePoint(x, y)));
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase6-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
