// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for PROJ-style stereographic parity across the supported modes.
/// </summary>
public class StereographicProjectionRegressionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies equatorial ellipsoidal stereographic forward vectors against PROJ builtins.
    /// </summary>
    [Theory]
    [InlineData(2d, 1d, 222644.854550117d, 110610.883474174d)]
    [InlineData(2d, -1d, 222644.854550117d, -110610.883474174d)]
    [InlineData(-2d, 1d, -222644.854550117d, 110610.883474174d)]
    [InlineData(-2d, -1d, -222644.854550117d, -110610.883474174d)]
    public void EquatorialEllipsoidalForwardMatchesProjReference(double longitude, double latitude, double expectedX, double expectedY)
    {
        double[] projectedPoint = CreateForwardTransform(BuildEllipsoidalEquatorialWkt()).MathTransform.Transform([longitude, latitude]);

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-3d);
    }

    /// <summary>
    /// Verifies equatorial ellipsoidal stereographic inverse vectors against PROJ builtins.
    /// </summary>
    [Theory]
    [InlineData(200d, 100d, 0.001796631d, 0.000904369d)]
    [InlineData(200d, -100d, 0.001796631d, -0.000904369d)]
    [InlineData(-200d, 100d, -0.001796631d, 0.000904369d)]
    [InlineData(-200d, -100d, -0.001796631d, -0.000904369d)]
    public void EquatorialEllipsoidalInverseMatchesProjReference(double x, double y, double expectedLongitude, double expectedLatitude)
    {
        double[] geographicPoint = CreateInverseTransform(BuildEllipsoidalEquatorialWkt()).MathTransform.Transform([x, y]);

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 1e-9d);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies equatorial spherical stereographic forward vectors against PROJ builtins.
    /// </summary>
    [Theory]
    [InlineData(2d, 1d, 223407.810259507d, 111737.938996443d)]
    [InlineData(2d, -1d, 223407.810259507d, -111737.938996443d)]
    [InlineData(-2d, 1d, -223407.810259507d, 111737.938996443d)]
    [InlineData(-2d, -1d, -223407.810259507d, -111737.938996443d)]
    public void EquatorialSphericalForwardMatchesProjReference(double longitude, double latitude, double expectedX, double expectedY)
    {
        double[] projectedPoint = CreateForwardTransform(BuildSphericalEquatorialWkt()).MathTransform.Transform([longitude, latitude]);

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-3d);
    }

    /// <summary>
    /// Verifies equatorial spherical stereographic inverse vectors against PROJ builtins.
    /// </summary>
    [Theory]
    [InlineData(200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData(200d, -100d, 0.001790493d, -0.000895247d)]
    [InlineData(-200d, 100d, -0.001790493d, 0.000895247d)]
    [InlineData(-200d, -100d, -0.001790493d, -0.000895247d)]
    public void EquatorialSphericalInverseMatchesProjReference(double x, double y, double expectedLongitude, double expectedLatitude)
    {
        double[] geographicPoint = CreateInverseTransform(BuildSphericalEquatorialWkt()).MathTransform.Transform([x, y]);

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 1e-9d);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies polar ellipsoidal true-scale vectors against PROJ builtins.
    /// </summary>
    [Fact]
    public void PolarEllipsoidalVariantBMatchesProjReference()
    {
        double[] projectedPoint = CreateForwardTransform(BuildPolarEllipsoidalVariantBWkt()).MathTransform.Transform([20d, -70d]);

        Assert.InRange(Math.Abs(projectedPoint[0] - 748315.3282d), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - 2055979.4669d), 0d, 1e-3d);
    }

    /// <summary>
    /// Verifies polar spherical true-scale vectors against PROJ builtins.
    /// </summary>
    [Fact]
    public void PolarSphericalVariantBMatchesProjReference()
    {
        double[] projectedPoint = CreateForwardTransform(BuildPolarSphericalVariantBWkt()).MathTransform.Transform([20d, -70d]);

        Assert.InRange(Math.Abs(projectedPoint[0] - 746100.2968d), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - 2049893.7182d), 0d, 1e-3d);
    }

    private static ICoordinateTransformation CreateForwardTransform(string projectedWkt)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            projectedWkt);
        return CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
    }

    private static ICoordinateTransformation CreateInverseTransform(string projectedWkt)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            projectedWkt);
        return CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
    }

    private static string BuildEllipsoidalEquatorialWkt()
    {
        return "PROJCS[\"Regression-stere-ellipsoidal-equatorial\",GEOGCS[\"Regression-Geog\",DATUM[\"Regression-Datum\",SPHEROID[\"GRS 80\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"stere\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static string BuildSphericalEquatorialWkt()
    {
        return "PROJCS[\"Regression-stere-spherical-equatorial\",GEOGCS[\"Regression-Geog\",DATUM[\"Regression-Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"stere\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static string BuildPolarEllipsoidalVariantBWkt()
    {
        return "PROJCS[\"Regression-stere-ellipsoidal-polar-b\",GEOGCS[\"Regression-Geog\",DATUM[\"Regression-Datum\",SPHEROID[\"GRS 80\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"stere\"],PARAMETER[\"latitude_of_origin\",-90],PARAMETER[\"central_meridian\",0],PARAMETER[\"lat_ts\",-70],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static string BuildPolarSphericalVariantBWkt()
    {
        return "PROJCS[\"Regression-stere-spherical-polar-b\",GEOGCS[\"Regression-Geog\",DATUM[\"Regression-Datum\",SPHEROID[\"Sphere\",6378137,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"stere\"],PARAMETER[\"latitude_of_origin\",-90],PARAMETER[\"central_meridian\",0],PARAMETER[\"lat_ts\",-70],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }
}
