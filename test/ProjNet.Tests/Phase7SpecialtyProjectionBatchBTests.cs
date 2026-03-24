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
/// Validates M7 batch B specialty projections.
/// </summary>
public class Phase7SpecialtyProjectionBatchBTests
{
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";
    private const string Sphere6370997 = "SPHEROID[\"Sphere\",6370997,0]";
    private const string Sphere1 = "SPHEROID[\"Sphere\",1,0]";
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for batch B projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="extraParameters">Optional additional projection parameters.</param>
    [Theory]
    [InlineData("tobmerc", Sphere6370997, null)]
    [InlineData("Tobler_Mercator", Sphere6370997, null)]
    [InlineData("calcofi", Grs80, null)]
    [InlineData("Cal_Coop_Ocean_Fish_Invest_Lines_Stations", Grs80, null)]
    [InlineData("mbtfpp", Sphere6400000, null)]
    [InlineData("McBryde_Thomas_Flat_Polar_Parabolic", Sphere6400000, null)]
    [InlineData("mbtfpq", Sphere6400000, null)]
    [InlineData("McBryde_Thomas_Flat_Polar_Quartic", Sphere6400000, null)]
    [InlineData("imoll", Sphere6400000, null)]
    [InlineData("Interrupted_Mollweide", Sphere6400000, null)]
    [InlineData("imoll_o", Sphere6400000, null)]
    [InlineData("Interrupted_Mollweide_Oceanic_View", Sphere6400000, null)]
    [InlineData("igh_o", Sphere6400000, null)]
    [InlineData("Interrupted_Goode_Homolosine_Oceanic_View", Sphere6400000, null)]
    public void SupportsBatchBAliasesFromWkt(string projectionName, string spheroidClause, string extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
        ArgumentNullException.ThrowIfNull(spheroidClause);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies aliases resolve from WKT for <c>col_urban</c> with required non-default parameters.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("col_urban")]
    [InlineData("Colombia_Urban")]
    public void SupportsColombiaUrbanAliasesFromWkt(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildColUrbanWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(-74.25d, 4.8d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for batch B projections.
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
    [InlineData("tobmerc", Sphere6370997, 2d, 1d, 222322.011656333081d, 111200.520030584055d, null, 1e-6)]
    [InlineData("calcofi", Grs80, 2d, 1d, 508.444872150d, -1171.764860418d, null, 1e-6)]
    [InlineData("mbtfpp", Sphere6400000, 2d, 1d, 206804.786929820d, 120649.762565793d, null, 1e-6)]
    [InlineData("mbtfpq", Sphere6400000, 2d, 1d, 209391.854738393d, 119161.040199055d, null, 1e-6)]
    [InlineData("imoll", Sphere6400000, 2d, 1d, -912080.283811148372d, 124066.283433859542d, null, 1e-6)]
    [InlineData("imoll_o", Sphere6400000, 2d, 1d, -1357849.196080365917d, 124066.283433859542d, null, 1e-6)]
    [InlineData("igh_o", Sphere6400000, 2d, 1d, 223197.992883418d, 111701.072127637d, null, 1e-6)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        string spheroidClause,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        string extraParameters,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
        ArgumentNullException.ThrowIfNull(spheroidClause);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vector for <c>calcofi</c> when non-zero lon0/x0/y0 are provided and internally ignored.
    /// </summary>
    [Fact]
    public void MatchesCalcofiForwardVectorWithIgnoredLon0AndOffsets()
    {
        const double expectedX = 301.769827d;
        const double expectedY = -1567.849822d;

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildCalcofiCustomWkt());
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(10d, 50d));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-6);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vector for <c>col_urban</c>.
    /// </summary>
    [Fact]
    public void MatchesColombiaUrbanForwardVector()
    {
        const double expectedX = 80859.033d;
        const double expectedY = 122543.174d;

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildColUrbanWkt("col_urban"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(-74.25d, 4.8d));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-3);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable batch B projections.
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
    [InlineData("tobmerc", Sphere6370997, 200d, 100d, 0.001798644059d, 0.000899322029d, null, 2e-9)]
    [InlineData("calcofi", Grs80, 200d, 100d, -110.363307925d, 12.032056976d, null, 2e-9)]
    [InlineData("mbtfpp", Sphere6400000, 200d, 100d, 0.001933954d, 0.000828837d, null, 2e-9)]
    [InlineData("mbtfpq", Sphere6400000, 200d, 100d, 0.001910106d, 0.000839185d, null, 2e-9)]
    [InlineData("imoll", Sphere6400000, 200d, 100d, 11.074062190626d, 0.000806005080d, null, 2e-9)]
    [InlineData("imoll_o", Sphere6400000, 200d, 100d, 15.502891574921d, 0.000806005080d, null, 2e-9)]
    [InlineData("igh_o", Sphere6400000, 200d, 100d, 0.001790494d, 0.000895247d, null, 2e-9)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        string spheroidClause,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        string extraParameters,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
        ArgumentNullException.ThrowIfNull(spheroidClause);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable batch B projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="extraParameters">Optional additional projection parameters.</param>
    [Theory]
    [InlineData("tobmerc", Sphere6370997, 2d, 75d, null)]
    [InlineData("calcofi", Grs80, 2d, 1d, null)]
    [InlineData("mbtfpp", Sphere6400000, 2d, 1d, null)]
    [InlineData("mbtfpq", Sphere6400000, 2d, 1d, null)]
    [InlineData("imoll", Sphere6400000, -39.99d, 0.1d, null)]
    [InlineData("imoll_o", Sphere6400000, -89.99d, 0.1d, null)]
    [InlineData("igh_o", Sphere6400000, 170d, 70d, null)]
    public void SupportsBatchBRoundtrip(
        string projectionName,
        string spheroidClause,
        double longitude,
        double latitude,
        string extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
        ArgumentNullException.ThrowIfNull(spheroidClause);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-8);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-8);
    }

    /// <summary>
    /// Verifies roundtrip stability for <c>col_urban</c>.
    /// </summary>
    [Fact]
    public void SupportsColombiaUrbanRoundtrip()
    {
        const double longitude = -74.25d;
        const double latitude = 4.8d;

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildColUrbanWkt("col_urban"));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-8);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-8);
    }

    /// <summary>
    /// Verifies Tobler-Mercator rejects pole input.
    /// </summary>
    /// <param name="latitude">Input latitude at the pole.</param>
    [Theory]
    [InlineData(90d)]
    [InlineData(-90d)]
    public void ToblerMercatorRejectsPoles(double latitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("tobmerc", Sphere6370997, null));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        Assert.Throws<ArgumentException>(() => forward.MathTransform.Transform(CreatePoint(0d, latitude)));
    }

    /// <summary>
    /// Verifies Tobler-Mercator with unit sphere remains numerically stable near zero.
    /// </summary>
    [Fact]
    public void ToblerMercatorUnitSphereInverseNearZeroIsStable()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("tobmerc", Sphere1, null));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(0d, 1e-15d));

        Assert.InRange(Math.Abs(geographicPoint[0]), 0d, 1e-13);
        Assert.InRange(Math.Abs(geographicPoint[1] - 1e-15d), 0d, 1e-13);
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string extraParameters)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-B-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{2},UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            extraParameters ?? string.Empty);
    }

    private static string BuildColUrbanWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-B-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",4.68048611111111],PARAMETER[\"central_meridian\",-74.1465916666667],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",92334.879],PARAMETER[\"false_northing\",109320.965],PARAMETER[\"h_0\",2550],UNIT[\"metre\",1]]",
            projectionName,
            Grs80);
    }

    private static string BuildCalcofiCustomWkt()
    {
        return "PROJCS[\"Phase7-B-calcofi-custom\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",400,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"calcofi\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",50],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",10000],PARAMETER[\"false_northing\",500000],UNIT[\"metre\",1]]";
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
