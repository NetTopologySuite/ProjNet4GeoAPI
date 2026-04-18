// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates simple conic and IMW-style projections.
/// </summary>
public class SimpleConicAndImwProjectionTests
{
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies aliases resolve from WKT for simple conic and IMW-style projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="extraParameters">Optional additional projection parameters.</param>
    [Theory]
    [InlineData("euler", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("murd1", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("murd2", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("murd3", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("tissot", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("vitk1", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("imw_p", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("International_Map_of_the_World_Polyconic", Grs80, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("mbt_fps", Sphere6400000, null)]
    [InlineData("McBryde_Thomas_Flat_Pole_Sine", Sphere6400000, null)]
    [InlineData("bertin1953", Sphere6400000, null)]
    [InlineData("Bertin_1953", Sphere6400000, null)]
    public void SupportsSimpleConicAndImwAliasesFromWkt(string projectionName, string spheroidClause, string? extraParameters)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for simple conic and IMW-style projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    /// <param name="extraParameters">Optional additional projection parameters.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [InlineData("euler", Grs80, 2d, 1d, 222597.634659108d, 111404.240549919d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("murd1", Grs80, 2d, 1d, 222600.813473554d, 111404.244180546d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("murd2", Grs80, 2d, 1d, 222588.099751230d, 111426.140027412d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("murd3", Grs80, 2d, 1d, 222600.814077577d, 111404.246601372d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("tissot", Grs80, 2d, 1d, 222641.078699631d, 54347.828487281d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("vitk1", Grs80, 2d, 1d, 222607.171211458d, 111404.251442435d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("imw_p", Grs80, 2d, 1d, 222588.441139376d, 55321.128653810d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 1e-6d)]
    [InlineData("mbt_fps", Sphere6400000, 2d, 1d, 198798.176129850d, 125512.017254531d, null, 1e-6d)]
    [InlineData("bertin1953", Sphere6400000, 16.5d, 42d, 0d, 0d, null, 1e-6d)]
    [InlineData("bertin1953", Sphere6400000, 0d, 0d, -1665321.948851200d, -4385446.772108800d, null, 1e-5d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        string spheroidClause,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        string? extraParameters,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable simple conic and IMW-style projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    /// <param name="extraParameters">Optional additional projection parameters.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [InlineData("euler", Grs80, 200d, 100d, 0.001796281d, 0.000898315d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("murd1", Grs80, 200d, 100d, 0.001796255d, 0.000898315d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("murd2", Grs80, 200d, 100d, 0.001796357d, 0.000897887d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("murd3", Grs80, 200d, 100d, 0.001796255d, 0.000898315d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("tissot", Grs80, 200d, 100d, 0.001796281d, 0.513444955d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("vitk1", Grs80, 200d, 100d, 0.001796204d, 0.000898315d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("imw_p", Grs80, 200d, 100d, 0.001796699d, 0.500904924d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]", 2e-9d)]
    [InlineData("mbt_fps", Sphere6400000, 200d, 100d, 0.002011971d, 0.000796712d, null, 2e-9d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        string spheroidClause,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        string? extraParameters,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable simple conic and IMW-style projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="extraParameters">Optional additional projection parameters.</param>
    [Theory]
    [InlineData("euler", Grs80, 2d, 1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("murd1", Grs80, -2d, -1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("murd2", Grs80, 2d, -1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("murd3", Grs80, -2d, 1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("tissot", Grs80, 2d, 1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("vitk1", Grs80, 2d, 1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("imw_p", Grs80, 2d, 1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("mbt_fps", Sphere6400000, 2d, 1d, null)]
    public void SupportsSimpleConicAndImwRoundtrip(string projectionName, string spheroidClause, double longitude, double latitude, string? extraParameters)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-7d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-7d);
    }

    /// <summary>
    /// Verifies Bertin 1953 remains forward-only.
    /// </summary>
    [Fact]
    public void Bertin1953DoesNotSupportInverse()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("bertin1953", Sphere6400000, null));
        Assert.Throws<NotSupportedException>(
            () => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem));
    }

    /// <summary>
    /// Verifies IMW Polyconic supports the special <c>lat_1=0</c>, <c>lat_2=10</c> branch.
    /// </summary>
    [Fact]
    public void ImwPolyconicSupportsLat1ZeroBranch()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt("imw_p", Grs80, ",PARAMETER[\"lat_1\",0],PARAMETER[\"lat_2\",10]"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(0.000898315284d, 0d));
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(100d, 0d));

        Assert.InRange(Math.Abs(projectedPoint[0] - 100d), 0d, 1e-6d);
        Assert.InRange(Math.Abs(projectedPoint[1]), 0d, 1e-6d);
        Assert.InRange(Math.Abs(geographicPoint[0] - 0.000898315284d), 0d, 1e-12d);
        Assert.InRange(Math.Abs(geographicPoint[1]), 0d, 1e-12d);
    }

    /// <summary>
    /// Verifies conic variants reject degenerate standard parallels.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    [Theory]
    [InlineData("euler")]
    [InlineData("murd1")]
    [InlineData("murd2")]
    [InlineData("murd3")]
    [InlineData("tissot")]
    [InlineData("vitk1")]
    public void SimpleConicVariantsRejectDegenerateStandardParallels(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(projectionName, Sphere6400000, ",PARAMETER[\"lat_1\",1],PARAMETER[\"lat_2\",1]"));
        Assert.Throws<ArgumentException>(
            () => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected));
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string? extraParameters)
    {
        return FormattableString.Invariant($"PROJCS[\"Specialty-D1-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
