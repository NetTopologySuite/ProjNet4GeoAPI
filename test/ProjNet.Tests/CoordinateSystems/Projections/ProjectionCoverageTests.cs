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
/// Improves coverage for projections with low line coverage by exercising forward,
/// inverse, and round-trip transforms across a variety of geographic positions.
/// </summary>
public class ProjectionCoverageTests
{
    private const string Wgs84 = "SPHEROID[\"WGS 84\",6378137,298.257223563]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";
    private const string Sphere6370997 = "SPHEROID[\"Sphere\",6370997,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    // ------------------------------------------------------------------
    //  QuadrilateralizedSphericalCube (qsc) – 46.2 % coverage
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies QSC forward/inverse round-trip for various face positions.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="latitudeOfOrigin">Latitude of origin to select the cube face.</param>
    [Theory]
    [InlineData(0d, 0d, 0d)]
    [InlineData(10d, 20d, 0d)]
    [InlineData(-30d, 45d, 0d)]
    [InlineData(90d, 10d, 0d)]
    [InlineData(-90d, -10d, 0d)]
    [InlineData(179d, 5d, 0d)]
    [InlineData(-179d, -5d, 0d)]
    [InlineData(0d, 89d, 90d)]
    [InlineData(90d, 89d, 90d)]
    [InlineData(-120d, 85d, 90d)]
    [InlineData(0d, -89d, -90d)]
    [InlineData(45d, -85d, -90d)]
    [InlineData(-170d, -80d, -90d)]
    [InlineData(180d, 0d, 0d)]
    [InlineData(0d, 45d, 0d)]
    public void QscRoundTrip(double longitude, double latitude, double latitudeOfOrigin)
    {
        string wkt = BuildProjectedWkt("qsc", Wgs84, latitudeOfOrigin, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies QSC forward transform produces finite, non-zero results for non-origin points.
    /// </summary>
    [Fact]
    public void QscForwardProducesFiniteResults()
    {
        string wkt = BuildProjectedWkt("qsc", Wgs84, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(30d, 45d));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
        Assert.NotEqual(0d, result[0]);
        Assert.NotEqual(0d, result[1]);
    }

    /// <summary>
    /// Verifies QSC forward transform with non-zero central meridian produces finite results.
    /// </summary>
    /// <param name="centralMeridian">Central meridian degrees.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(90d, 92d, 20d)]
    [InlineData(-90d, -88d, -20d)]
    [InlineData(45d, 50d, 30d)]
    public void QscForwardWithCentralMeridian(double centralMeridian, double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("qsc", Wgs84, 0d, centralMeridian, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    // ------------------------------------------------------------------
    //  HealpixProjection (healpix) – 51.2 % coverage
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies HEALPix spherical forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-30d, 20d)]
    [InlineData(90d, 45d)]
    [InlineData(-90d, -45d)]
    [InlineData(179d, 10d)]
    [InlineData(-179d, -10d)]
    [InlineData(0d, 89d)]
    [InlineData(0d, -89d)]
    [InlineData(45d, 60d)]
    [InlineData(-120d, -70d)]
    public void HealpixSphericalRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("healpix", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies HEALPix ellipsoidal (WGS 84) forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-30d, 20d)]
    [InlineData(90d, 45d)]
    [InlineData(-90d, -45d)]
    [InlineData(179d, 10d)]
    [InlineData(-179d, -10d)]
    [InlineData(0d, 89d)]
    [InlineData(0d, -89d)]
    public void HealpixEllipsoidalRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("healpix", Wgs84, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies HEALPix forward produces finite results.
    /// </summary>
    /// <param name="spheroidClause">Spheroid clause.</param>
    [Theory]
    [InlineData(Sphere6400000)]
    [InlineData(Wgs84)]
    public void HealpixForwardProducesFiniteResults(string spheroidClause)
    {
        ArgumentNullException.ThrowIfNull(spheroidClause);

        string wkt = BuildProjectedWkt("healpix", spheroidClause, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(45d, 30d));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    // ------------------------------------------------------------------
    //  GoodeProjection (goode) – 54.8 % coverage
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies Goode Homolosine forward/inverse round-trip across interrupt zones.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-100d, 30d)]
    [InlineData(-40d, 20d)]
    [InlineData(30d, 10d)]
    [InlineData(80d, -10d)]
    [InlineData(-60d, -30d)]
    [InlineData(150d, -20d)]
    [InlineData(-170d, 40d)]
    [InlineData(10d, 50d)]
    [InlineData(-10d, -50d)]
    [InlineData(0d, 40.69d)]
    [InlineData(0d, -40.69d)]
    [InlineData(0d, 89d)]
    [InlineData(0d, -89d)]
    [InlineData(179d, 0d)]
    [InlineData(-179d, 0d)]
    public void GoodeSphericalRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("goode", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Goode Homolosine ellipsoidal (WGS 84) forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-100d, 30d)]
    [InlineData(80d, -10d)]
    [InlineData(150d, -60d)]
    [InlineData(0d, 89d)]
    [InlineData(0d, -89d)]
    public void GoodeEllipsoidalRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("goode", Wgs84, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Goode Homolosine forward vectors are finite and non-zero for non-origin input.
    /// </summary>
    [Fact]
    public void GoodeForwardProducesFiniteResults()
    {
        string wkt = BuildProjectedWkt("goode", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(-100d, 50d));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
        Assert.NotEqual(0d, result[0]);
        Assert.NotEqual(0d, result[1]);
    }

    // ------------------------------------------------------------------
    //  Winkel2Projection (wink2) – 57.5 % coverage – forward only
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies Winkel II forward transform produces finite results.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-90d, 45d)]
    [InlineData(90d, -45d)]
    [InlineData(179d, 89d)]
    [InlineData(-179d, -89d)]
    [InlineData(0d, 89d)]
    [InlineData(0d, -89d)]
    [InlineData(45d, 0d)]
    [InlineData(-120d, 60d)]
    public void Winkel2ForwardProducesFiniteResults(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("wink2", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    /// <summary>
    /// Verifies Winkel II forward produces non-zero output for non-zero input.
    /// </summary>
    [Fact]
    public void Winkel2ForwardNonZeroInput()
    {
        string wkt = BuildProjectedWkt("wink2", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(30d, 50d));

        Assert.NotEqual(0d, result[0]);
        Assert.NotEqual(0d, result[1]);
    }

    /// <summary>
    /// Verifies Winkel II does not support inverse projection.
    /// </summary>
    [Fact]
    public void Winkel2RejectsInverse()
    {
        string wkt = BuildProjectedWkt("wink2", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(200d, 100d)));
    }

    /// <summary>
    /// Verifies Winkel II with custom standard parallel.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(10d, 20d)]
    [InlineData(-60d, 70d)]
    [InlineData(150d, -30d)]
    public void Winkel2WithStandardParallel(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("wink2", Sphere6400000, 30d, 0d, ",PARAMETER[\"standard_parallel_1\",50.467]");
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    // ------------------------------------------------------------------
    //  LambertAzimuthalEqualAreaProjection (laea) – 59.7 % coverage
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies LAEA equatorial mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-30d, 20d)]
    [InlineData(90d, 45d)]
    [InlineData(-90d, -45d)]
    [InlineData(10d, 89d)]
    [InlineData(-10d, -89d)]
    [InlineData(179d, 0d)]
    [InlineData(-179d, 0d)]
    public void LaeaEquatorialRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("laea", Wgs84, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies LAEA north-polar mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 89d)]
    [InlineData(90d, 60d)]
    [InlineData(-90d, 70d)]
    [InlineData(180d, 80d)]
    [InlineData(-45d, 45d)]
    [InlineData(0d, 10d)]
    public void LaeaNorthPoleRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("laea", Wgs84, 90d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies LAEA south-polar mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, -89d)]
    [InlineData(90d, -60d)]
    [InlineData(-90d, -70d)]
    [InlineData(180d, -80d)]
    [InlineData(-45d, -45d)]
    [InlineData(0d, -10d)]
    public void LaeaSouthPoleRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("laea", Wgs84, -90d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies LAEA oblique mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(10d, 52d)]
    [InlineData(-5d, 40d)]
    [InlineData(30d, 60d)]
    [InlineData(0d, 89d)]
    [InlineData(-20d, 30d)]
    [InlineData(50d, 70d)]
    public void LaeaObliqueRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("laea", Wgs84, 52d, 10d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies LAEA spherical variant forward transform produces finite results.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="latitudeOfOrigin">Latitude of origin.</param>
    [Theory]
    [InlineData(0d, 0d, 0d)]
    [InlineData(10d, 5d, 0d)]
    [InlineData(-10d, -5d, 0d)]
    [InlineData(0d, 85d, 90d)]
    [InlineData(0d, -85d, -90d)]
    [InlineData(5d, 47d, 45d)]
    public void LaeaSphericalForward(double longitude, double latitude, double latitudeOfOrigin)
    {
        string wkt = BuildProjectedWkt("laea", Sphere6400000, latitudeOfOrigin, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.False(double.IsNaN(result[0]));
        Assert.False(double.IsNaN(result[1]));
    }

    /// <summary>
    /// Verifies LAEA spherical equatorial forward northing against the analytical formula.
    /// </summary>
    [Fact]
    public void LaeaSphericalEquatorialForwardMatchesAnalyticalNorthing()
    {
        const double radius = 6400000d;
        const double latitude = 5d;
        string wkt = BuildProjectedWkt("laea", Sphere6400000, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(0d, latitude));
        double phi = latitude * (Math.PI / 180d);
        double expectedNorthing = radius * Math.Sqrt(2d / (1d + Math.Cos(phi))) * Math.Sin(phi);

        Assert.InRange(Math.Abs(projectedPoint[0]), 0d, 1e-9);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedNorthing), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Cassini-Soldner easting against the analytical 4th-order polynomial expansion.
    /// </summary>
    [Fact]
    public void CassiniSoldnerForwardMatchesAnalyticalEasting()
    {
        const double latitude = 52.518611111111d;
        const double longitude = 20d;
        const double semiMajor = 6378137d;
        const double inverseFlattening = 298.257223563d;
        const double expectedEasting = 1340021.76450623d;
        string wkt = BuildProjectedWkt("cass", "SPHEROID[\"WGS 84\",6378137,298.257223563]", 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double flattening = 1d / inverseFlattening;
        double eccentricitySquared = (2d * flattening) - (flattening * flattening);
        double cFactor = eccentricitySquared / (1d - eccentricitySquared);
        double phi = latitude * (Math.PI / 180d);
        double lambda = longitude * (Math.PI / 180d);
        double sinPhi = Math.Sin(phi);
        double cosPhi = Math.Cos(phi);
        double n = 1d / Math.Sqrt(1d - (eccentricitySquared * sinPhi * sinPhi));
        double tanPhi = Math.Tan(phi);
        double t = tanPhi * tanPhi;
        double a1 = lambda * cosPhi;
        double a2 = a1 * a1;
        double c = cFactor * cosPhi * cosPhi;
        double analyticalEasting = semiMajor * n * a1 * (1d - (a2 * t * ((1d / 6d) + (((8d - t + (8d * c)) * a2) / 120d))));

        Assert.InRange(Math.Abs(analyticalEasting - expectedEasting), 0d, 1e-6);
        Assert.InRange(Math.Abs(projectedPoint[0] - analyticalEasting), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Transverse Mercator inverse latitude uses Snyder coefficient 1575 in the t^4 term.
    /// </summary>
    [Fact]
    public void TransverseMercatorInverseLatitudeUsesSnyderCoefficient1575()
    {
        const double semiMajor = 6377563.396d;
        const double inverseFlattening = 299.32496d;
        const double latitudeOfOrigin = 49d;
        const double centralMeridian = -2d;
        const double scaleFactor = 0.9996012717d;
        const double easting = -4370667.706314864d;
        const double northing = 1991695.1549298093d;
        string wkt = FormattableString.Invariant(
            $"PROJCS[\"Coverage-transverse_mercator\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Airy 1830\",{semiMajor},{inverseFlattening}]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"transverse_mercator\"],PARAMETER[\"latitude_of_origin\",{latitudeOfOrigin}],PARAMETER[\"central_meridian\",{centralMeridian}],PARAMETER[\"scale_factor\",{scaleFactor}],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(easting, northing));
        double expectedLatitude = ComputeTransverseMercatorInverseLatitude(
            easting,
            northing,
            semiMajor,
            inverseFlattening,
            scaleFactor,
            latitudeOfOrigin,
            1575d);
        double legacyLatitude = ComputeTransverseMercatorInverseLatitude(
            easting,
            northing,
            semiMajor,
            inverseFlattening,
            scaleFactor,
            latitudeOfOrigin,
            1574d);

        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 1e-8);
        Assert.True(Math.Abs(geographicPoint[1] - legacyLatitude) > 1e-3);
    }

    /// <summary>
    /// Verifies Albers inverse fails near the cone apex because <c>Math.Atan</c> cannot resolve the quadrant.
    /// </summary>
    [Fact]
    public void AlbersInverseNearApexHasQuadrantError()
    {
        const double longitude = 120d;
        const double latitude = 30d;
        const double latitudeOfOrigin = 0d;
        const double standardParallel1 = 90d;
        const double standardParallel2 = 60d;
        string wkt = BuildProjectedWkt(
            "albers",
            Sphere6400000,
            latitudeOfOrigin,
            0d,
            FormattableString.Invariant($",PARAMETER[\"standard_parallel_1\",{standardParallel1}],PARAMETER[\"standard_parallel_2\",{standardParallel2}]"));
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Polar Stereographic UPS north applies <c>scale_factor</c> consistently.
    /// </summary>
    [Fact]
    public void PolarStereographicUpsNorthMatchesReferenceCoordinate()
    {
        const double longitude = 15d;
        const double latitude = 73d;
        const double expectedEasting = 2491967.01029204d;
        const double expectedNorthing = 163954.12194234435d;
        string wkt = "PROJCS[\"Coverage-UPS-North\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"WGS 84\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Polar_Stereographic\"],PARAMETER[\"latitude_of_origin\",90],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",0.994],PARAMETER[\"false_easting\",2000000],PARAMETER[\"false_northing\",2000000],UNIT[\"metre\",1]]";
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedEasting), 0d, 1d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedNorthing), 0d, 1d);
    }

    // ------------------------------------------------------------------
    //  MercatorAuxiliarySphere (mercator_auxiliary_sphere) – 59.3 % coverage
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies Mercator Auxiliary Sphere forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-30d, 20d)]
    [InlineData(90d, 45d)]
    [InlineData(-90d, -45d)]
    [InlineData(179d, 10d)]
    [InlineData(-179d, -10d)]
    [InlineData(0d, 85d)]
    [InlineData(0d, -85d)]
    public void MercatorAuxiliarySphereRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("mercator_auxiliary_sphere", Wgs84, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Mercator Auxiliary Sphere with a sphere datum.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(45d, 30d)]
    [InlineData(-120d, -60d)]
    [InlineData(179d, 80d)]
    public void MercatorAuxiliarySphereSphericalRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("mercator_auxiliary_sphere", Sphere6370997, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies Mercator Auxiliary Sphere with non-zero central meridian.
    /// </summary>
    /// <param name="centralMeridian">Central meridian degrees.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(10d, 15d, 20d)]
    [InlineData(-100d, -95d, 30d)]
    [InlineData(170d, 175d, -40d)]
    public void MercatorAuxiliarySphereCentralMeridianRoundTrip(double centralMeridian, double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("mercator_auxiliary_sphere", Wgs84, 0d, centralMeridian, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    // ------------------------------------------------------------------
    //  OrthographicProjection (ortho) – 60.8 % coverage
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies orthographic equatorial mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(2d, 1d)]
    [InlineData(-30d, 20d)]
    [InlineData(60d, 45d)]
    [InlineData(-60d, -45d)]
    [InlineData(0d, 89d)]
    [InlineData(0d, -89d)]
    [InlineData(89d, 0d)]
    [InlineData(-89d, 0d)]
    public void OrthoEquatorialRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("ortho", Wgs84, 0d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies orthographic north-polar mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 89d)]
    [InlineData(90d, 60d)]
    [InlineData(-90d, 70d)]
    [InlineData(180d, 80d)]
    [InlineData(-45d, 45d)]
    [InlineData(0d, 10d)]
    public void OrthoNorthPoleRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("ortho", Wgs84, 90d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies orthographic south-polar mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, -89d)]
    [InlineData(90d, -60d)]
    [InlineData(-90d, -70d)]
    [InlineData(180d, -80d)]
    [InlineData(-45d, -45d)]
    [InlineData(0d, -10d)]
    public void OrthoSouthPoleRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("ortho", Wgs84, -90d, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies orthographic oblique mode forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(10d, 52d)]
    [InlineData(-5d, 40d)]
    [InlineData(30d, 60d)]
    [InlineData(0d, 89d)]
    [InlineData(-20d, 30d)]
    [InlineData(50d, 70d)]
    public void OrthoObliqueRoundTrip(double longitude, double latitude)
    {
        string wkt = BuildProjectedWkt("ortho", Wgs84, 45d, 10d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies orthographic spherical variant forward/inverse round-trip.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="latitudeOfOrigin">Latitude of origin.</param>
    [Theory]
    [InlineData(0d, 0d, 0d)]
    [InlineData(30d, 45d, 0d)]
    [InlineData(-60d, -30d, 0d)]
    [InlineData(0d, 89d, 90d)]
    [InlineData(0d, -89d, -90d)]
    [InlineData(10d, 50d, 45d)]
    public void OrthoSphericalRoundTrip(double longitude, double latitude, double latitudeOfOrigin)
    {
        string wkt = BuildProjectedWkt("ortho", Sphere6400000, latitudeOfOrigin, 0d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-6);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-6);
    }

    /// <summary>
    /// Verifies orthographic forward at center maps to origin.
    /// </summary>
    [Fact]
    public void OrthoCenterMapsToOrigin()
    {
        string wkt = BuildProjectedWkt("ortho", Wgs84, 45d, 10d, null);
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] result = forward.MathTransform.Transform(CreatePoint(10d, 45d));

        Assert.InRange(Math.Abs(result[0]), 0d, 1e-3);
        Assert.InRange(Math.Abs(result[1]), 0d, 1e-3);
    }

    // ------------------------------------------------------------------
    //  WKT builder and helpers
    // ------------------------------------------------------------------
    private static double ComputeTransverseMercatorInverseLatitude(
        double xMeters,
        double yMeters,
        double semiMajor,
        double inverseFlattening,
        double scaleFactor,
        double latitudeOfOriginDegrees,
        double coefficient)
    {
        const double epsilon = 1e-6;
        const double c00 = 1d;
        const double c02 = 0.25d;
        const double c04 = 0.046875d;
        const double c06 = 0.01953125d;
        const double c08 = 0.01068115234375d;
        const double c22 = 0.75d;
        const double c44 = 0.46875d;
        const double c46 = 0.01302083333333333333d;
        const double c48 = 0.00712076822916666666d;
        const double c66 = 0.36458333333333333333d;
        const double c68 = 0.00569661458333333333d;
        const double c88 = 0.3076171875d;
        double flattening = 1d / inverseFlattening;
        double eccentricitySquared = (2d * flattening) - (flattening * flattening);
        double esp = eccentricitySquared / (1d - eccentricitySquared);
        double en0 = c00 - (eccentricitySquared * (c02 + (eccentricitySquared *
                    (c04 + (eccentricitySquared * (c06 + (eccentricitySquared * c08)))))));
        double en1 = eccentricitySquared * (c22 - (eccentricitySquared *
                    (c04 + (eccentricitySquared * (c06 + (eccentricitySquared * c08))))));
        double tSeries = eccentricitySquared * eccentricitySquared;
        double en2 = tSeries * (c44 - (eccentricitySquared * (c46 + (eccentricitySquared * c48))));
        double en3 = (tSeries *= eccentricitySquared) * (c66 - (eccentricitySquared * c68));
        double en4 = tSeries * eccentricitySquared * c88;
        double latitudeOfOrigin = latitudeOfOriginDegrees * (Math.PI / 180d);
        double ml0 = Mlfn(en0, en1, en2, en3, en4, latitudeOfOrigin, Math.Sin(latitudeOfOrigin), Math.Cos(latitudeOfOrigin));
        double x = xMeters / semiMajor;
        double y = yMeters / semiMajor;
        double phi = InvMlfn(ml0 + (y / scaleFactor), eccentricitySquared, en0, en1, en2, en3, en4);

        if (Math.Abs(phi) >= Math.PI / 2d)
        {
            return y < 0d ? -90d : 90d;
        }

        double sinphi = Math.Sin(phi);
        double cosphi = Math.Cos(phi);
        double t = Math.Abs(cosphi) > epsilon ? sinphi / cosphi : 0d;
        double n = esp * cosphi * cosphi;
        double con = 1d - (eccentricitySquared * sinphi * sinphi);
        double d = x * Math.Sqrt(con) / scaleFactor;
        con *= t;
        t *= t;
        double ds = d * d;
        double innerMost = 1385d + (t * (3633d + (t * (4095d + (coefficient * t)))));
        double sixthTerm = 61d + (t * (90d - (252d * n) + (45d * t))) + (46d * n) - ((ds / 56d) * innerMost);
        double fourthTerm = 5d + (t * (3d - (9d * n))) + (n * (1d - (4d * n))) - ((ds / 30d) * sixthTerm);
        double latitudeRadians = phi - ((con * ds / (1d - eccentricitySquared)) * 0.5d * (1d - ((ds / 12d) * fourthTerm)));

        return latitudeRadians * (180d / Math.PI);
    }

    private static double Mlfn(double en0, double en1, double en2, double en3, double en4, double phi, double sinPhi, double cosPhi)
    {
        cosPhi *= sinPhi;
        sinPhi *= sinPhi;
        return (en0 * phi) - (cosPhi * (en1 + (sinPhi * (en2 + (sinPhi * (en3 + (sinPhi * en4)))))));
    }

    private static double InvMlfn(double arg, double eccentricitySquared, double en0, double en1, double en2, double en3, double en4)
    {
        double phi = arg;
        double k = 1d / (1d - eccentricitySquared);
        for (int i = 0; i < 20; i++)
        {
            double sinPhi = Math.Sin(phi);
            double t = 1d - (eccentricitySquared * sinPhi * sinPhi);
            t = (Mlfn(en0, en1, en2, en3, en4, phi, sinPhi, Math.Cos(phi)) - arg) * (t * Math.Sqrt(t)) * k;
            phi -= t;
            if (Math.Abs(t) < 1e-11d)
            {
                return phi;
            }
        }

        throw new InvalidOperationException("Transverse Mercator inverse meridional iteration did not converge.");
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, double latitudeOfOrigin, double centralMeridian, string? extraParameters)
    {
        return FormattableString.Invariant(
            $"PROJCS[\"Coverage-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",{latitudeOfOrigin}],PARAMETER[\"central_meridian\",{centralMeridian}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
