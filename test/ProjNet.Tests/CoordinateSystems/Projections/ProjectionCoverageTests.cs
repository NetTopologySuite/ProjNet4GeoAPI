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
    private static string BuildProjectedWkt(string projectionName, string spheroidClause, double latitudeOfOrigin, double centralMeridian, string? extraParameters)
    {
        return FormattableString.Invariant(
            $"PROJCS[\"Coverage-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",{latitudeOfOrigin}],PARAMETER[\"central_meridian\",{centralMeridian}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
