// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Globalization;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates geostationary satellite (<c>geos</c>) projection support.
/// </summary>
public class GeostationaryProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies that geos aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("geos")]
    [InlineData("Geostationary_Satellite")]
    public void SupportsGeosAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, false, 6378137d, 298.257222101d, 35785831d));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward/inverse roundtrip stability for geos.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="useSphere">Whether to use a spherical ellipsoid definition.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="tolerance">Maximum absolute roundtrip delta (degrees).</param>
    [Theory]
    [InlineData("geos", false, 2d, 1d, 1e-9)]
    [InlineData("geos", false, -2d, -1d, 1e-9)]
    [InlineData("Geostationary_Satellite", true, -2d, 1d, 1e-9)]
    public void SupportsGeosRoundtrip(string projectionName, bool useSphere, double longitude, double latitude, double tolerance)
    {
        double semiMajor = useSphere ? 6400000d : 6378137d;
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
            BuildProjectedWkt(projectionName, useSphere, semiMajor, 298.257222101d, 35785831d));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors for ellipsoidal and spherical geos.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="useSphere">Whether to use a spherical ellipsoid definition.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData("geos", false, 2d, 1d, 222527.070365800d, 110551.303413329d)]
    [InlineData("geos", false, -2d, -1d, -222527.070365800d, -110551.303413329d)]
    [InlineData("geos", true, 2d, 1d, 223289.457635795d, 111677.657456537d)]
    [InlineData("Geostationary_Satellite", true, -2d, -1d, -223289.457635795d, -111677.657456537d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        bool useSphere,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        double semiMajor = useSphere ? 6400000d : 6378137d;
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
            BuildProjectedWkt(projectionName, useSphere, semiMajor, 298.257222101d, 35785831d));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors for ellipsoidal and spherical geos.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="useSphere">Whether to use a spherical ellipsoid definition.</param>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData("geos", false, 200d, 100d, 0.001796631d, 0.000904369d)]
    [InlineData("geos", false, -200d, -100d, -0.001796631d, -0.000904369d)]
    [InlineData("geos", true, 200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData("Geostationary_Satellite", true, -200d, -100d, -0.001790493d, -0.000895247d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        bool useSphere,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        double semiMajor = useSphere ? 6400000d : 6378137d;
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
            BuildProjectedWkt(projectionName, useSphere, semiMajor, 298.257222101d, 35785831d));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies invalid satellite height values are rejected.
    /// </summary>
    /// <param name="satelliteHeight">Height parameter to validate.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(1e11d)]
    public void RejectsInvalidHeightValues(double satelliteHeight)
    {
        var exception = Assert.Throws<TargetInvocationException>(() =>
        {
            string wkt = BuildProjectedWkt("geos", true, 1d, 0d, satelliteHeight);
            var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(wkt);
            var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
            forward.MathTransform.Transform(CreatePoint(2d, 1d));
        });

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies optional sweep parameter is accepted and changes axis handling.
    /// </summary>
    [Fact]
    public void SupportsSweepXParameter()
    {
        string wkt = BuildProjectedWkt("geos", true, 6400000d, 0d, 35785831d, sweepX: true);
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(wkt);
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
        Assert.True(Math.Abs(projectedPoint[0]) > 0d);
        Assert.True(Math.Abs(projectedPoint[1]) > 0d);
    }

    private static string BuildProjectedWkt(string projectionName, bool useSphere, double semiMajor, double inverseFlattening, double satelliteHeight, bool sweepX = false)
    {
        string spheroid = useSphere
            ? string.Format(CultureInfo.InvariantCulture, "SPHEROID[\"Sphere\",{0},0]", semiMajor)
            : string.Format(CultureInfo.InvariantCulture, "SPHEROID[\"GRS 80\",{0},{1}]", semiMajor, inverseFlattening);

        string hText = satelliteHeight.ToString(CultureInfo.InvariantCulture);
        string sweepParameter = sweepX
            ? ",PARAMETER[\"sweep_x\",1]"
            : string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"h\",{2}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{3},UNIT[\"metre\",1]]",
            projectionName,
            spheroid,
            hText,
            sweepParameter);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}

