// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Globalization;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates near-sided perspective (<c>nsper</c>/<c>tpers</c>) and Laborde (<c>labrd</c>) projection support.
/// </summary>
public class NearSidedPerspectiveLabordeProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies that nsper aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("nsper")]
    [InlineData("Near_Sided_Perspective")]
    [InlineData("tpers")]
    [InlineData("Tilted_Perspective")]
    public void SupportsPerspectiveAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildNsperProjectedWkt(projectionName, 6400000d, 1000000d, 0d, 0d, null, null));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors for nsper.
    /// </summary>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData(2d, 1d, 222239.816114100d, 111153.763991925d)]
    [InlineData(2d, -1d, 222239.816114100d, -111153.763991925d)]
    [InlineData(-2d, 1d, -222239.816114100d, 111153.763991925d)]
    [InlineData(-2d, -1d, -222239.816114100d, -111153.763991925d)]
    public void MatchesProjBuiltinsNsperForwardVectors(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildNsperProjectedWkt("nsper", 6400000d, 1000000d, 0d, 0d, null, null));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors for nsper.
    /// </summary>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData(200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData(200d, -100d, 0.001790493d, -0.000895247d)]
    [InlineData(-200d, 100d, -0.001790493d, 0.000895247d)]
    [InlineData(-200d, -100d, -0.001790493d, -0.000895247d)]
    public void MatchesProjBuiltinsNsperInverseVectors(
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildNsperProjectedWkt("nsper", 6400000d, 1000000d, 0d, 0d, null, null));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies polar and oblique nsper setups from builtins.
    /// </summary>
    /// <param name="latitudeOfOrigin">lat_0 parameter (degrees).</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData(90d, 45d, 45d, 0.4555d, -0.4555d)]
    [InlineData(-90d, -45d, -45d, -0.4555d, 0.4555d)]
    [InlineData(45d, 45d, 45d, 0.4767d, 0.1396d)]
    public void MatchesProjBuiltinsNsperPolarAndObliqueCases(
        double latitudeOfOrigin,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildNsperProjectedWkt("nsper", 1d, 3d, latitudeOfOrigin, 0d, null, null));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 5e-5);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 5e-5);
    }

    /// <summary>
    /// Verifies tpers vectors from builtins (+azi and +tilt variants).
    /// </summary>
    /// <param name="tilt">Tilt angle in degrees.</param>
    /// <param name="azimuth">Azimuth angle in degrees.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData(null, 20d, 2d, 1d, 170820.288955531d, 180460.865555805d)]
    [InlineData(20d, null, 2d, 1d, 213598.340357101d, 113687.930830744d)]
    public void MatchesProjBuiltinsTpersForwardVectors(
        double? tilt,
        double? azimuth,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildNsperProjectedWkt("tpers", 6400000d, 1000000d, 0d, 0d, tilt, azimuth));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies Laborde aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("labrd")]
    [InlineData("Laborde")]
    public void SupportsLabordeAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildLabrdProjectedWkt(projectionName, 2d, 0.5d, 0d));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors for labrd.
    /// </summary>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData(2d, 1d, 166973.166090228d, -110536.912730266d)]
    [InlineData(2d, -1d, 166973.168287157d, -331761.993650884d)]
    [InlineData(-2d, 1d, -278345.500519976d, -110469.032642032d)]
    [InlineData(-2d, -1d, -278345.504185270d, -331829.870790275d)]
    public void MatchesProjBuiltinsLabrdForwardVectors(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildLabrdProjectedWkt("labrd", 2d, 0.5d, 0d));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 5e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 5e-7);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors for labrd.
    /// </summary>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData(200d, 100d, 0.501797719d, 2.000904357d)]
    [InlineData(200d, -100d, 0.501797717d, 1.999095641d)]
    [InlineData(-200d, 100d, 0.498202281d, 2.000904357d)]
    [InlineData(-200d, -100d, 0.498202283d, 1.999095641d)]
    public void MatchesProjBuiltinsLabrdInverseVectors(
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildLabrdProjectedWkt("labrd", 2d, 0.5d, 0d));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies invalid perspective height values are rejected.
    /// </summary>
    /// <param name="h">Height parameter to validate.</param>
    [Theory]
    [InlineData(0d)]
    [InlineData(1e11d)]
    public void RejectsInvalidNsperHeight(double h)
    {
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
        {
            string wkt = BuildNsperProjectedWkt("nsper", 1d, h, 0d, 0d, null, null);
            ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
            ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
            forward.MathTransform.Transform(CreatePoint(2d, 1d));
        });

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies labrd rejects invalid lat_0 value.
    /// </summary>
    [Fact]
    public void RejectsInvalidLabrdLatitudeOfOrigin()
    {
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
        {
            string wkt = BuildLabrdProjectedWkt("labrd", 0d, 0.5d, 0d);
            ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
            ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
            forward.MathTransform.Transform(CreatePoint(2d, 1d));
        });

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    private static string BuildNsperProjectedWkt(
        string projectionName,
        double semiMajor,
        double height,
        double latitudeOfOrigin,
        double centralMeridian,
        double? tilt,
        double? azimuth)
    {
        string tiltParameter = tilt.HasValue
            ? string.Format(CultureInfo.InvariantCulture, ",PARAMETER[\"tilt\",{0}]", tilt.Value)
            : string.Empty;
        string azimuthParameter = azimuth.HasValue
            ? string.Format(CultureInfo.InvariantCulture, ",PARAMETER[\"azi\",{0}]", azimuth.Value)
            : string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",{1},0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",{2}],PARAMETER[\"central_meridian\",{3}],PARAMETER[\"h\",{4}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{5}{6},UNIT[\"metre\",1]]",
            projectionName,
            semiMajor.ToString("R", CultureInfo.InvariantCulture),
            latitudeOfOrigin.ToString("R", CultureInfo.InvariantCulture),
            centralMeridian.ToString("R", CultureInfo.InvariantCulture),
            height.ToString("R", CultureInfo.InvariantCulture),
            tiltParameter,
            azimuthParameter);
    }

    private static string BuildLabrdProjectedWkt(string projectionName, double latitudeOfOrigin, double centralMeridian, double azimuth)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"GRS 80\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",{1}],PARAMETER[\"central_meridian\",{2}],PARAMETER[\"azi\",{3}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            latitudeOfOrigin.ToString("R", CultureInfo.InvariantCulture),
            centralMeridian.ToString("R", CultureInfo.InvariantCulture),
            azimuth.ToString("R", CultureInfo.InvariantCulture));
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
