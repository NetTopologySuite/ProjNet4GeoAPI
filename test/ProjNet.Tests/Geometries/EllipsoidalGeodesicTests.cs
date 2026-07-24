// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using Xunit;

/// <summary>
/// Verifies the shared ellipsoidal geodesic helpers used by multiple projection kernels.
/// </summary>
public class EllipsoidalGeodesicTests
{
    /// <summary>
    /// Verifies Vincenty's inverse solution against the published Flinders Peak to Buninyong benchmark.
    /// </summary>
    [Fact]
    public void TryVincentyInverse_WithPublishedWgs84Benchmark_ReturnsExpectedDistanceAndAzimuth()
    {
        GeodesicParameters wgs84 = CreateWgs84Parameters();
        double latitude1 = DmsToRadians(-37, 57, 3.7203d);
        double longitude1 = DmsToRadians(144, 25, 29.5244d);
        double latitude2 = DmsToRadians(-37, 39, 10.1561d);
        double longitude2 = DmsToRadians(143, 55, 35.3839d);

        bool solved = EllipsoidalGeodesic.TryVincentyInverse(
            wgs84.SemiMinor,
            wgs84.Flattening,
            wgs84.EccentricityPrimeSquared,
            latitude1,
            longitude1,
            latitude2,
            longitude2,
            out double distance,
            out double azimuth);

        Assert.True(solved);
        Assert.Equal(54972.271d, distance, 3);
        Assert.Equal(DmsToRadians(306, 52, 5.37d), NormalizePositiveAzimuth(azimuth), 7);
    }

    /// <summary>
    /// Verifies Vincenty's direct solution against the published Flinders Peak to Buninyong benchmark.
    /// </summary>
    [Fact]
    public void TryVincentyDirect_WithPublishedWgs84Benchmark_ReturnsExpectedEndpoint()
    {
        GeodesicParameters wgs84 = CreateWgs84Parameters();
        double latitude1 = DmsToRadians(-37, 57, 3.7203d);
        double longitude1 = DmsToRadians(144, 25, 29.5244d);
        double azimuth1 = DmsToRadians(306, 52, 5.37d);

        bool solved = EllipsoidalGeodesic.TryVincentyDirect(
            wgs84.SemiMinor,
            wgs84.Flattening,
            wgs84.EccentricityPrimeSquared,
            latitude1,
            longitude1,
            azimuth1,
            54972.271d,
            out double latitude2,
            out double longitude2);

        Assert.True(solved);
        Assert.Equal(DmsToRadians(-37, 39, 10.1561d), latitude2, 9);
        Assert.Equal(DmsToRadians(143, 55, 35.3839d), longitude2, 9);
    }

    /// <summary>
    /// Verifies that Vincenty's inverse solver reports non-convergence for a nearly antipodal case.
    /// </summary>
    [Fact]
    public void TryVincentyInverse_WithNearlyAntipodalPoints_ReturnsFalse()
    {
        GeodesicParameters wgs84 = CreateWgs84Parameters();

        bool solved = EllipsoidalGeodesic.TryVincentyInverse(
            wgs84.SemiMinor,
            wgs84.Flattening,
            wgs84.EccentricityPrimeSquared,
            0d,
            0d,
            DegreesToRadians(0.5d),
            DegreesToRadians(179.7d),
            out double distance,
            out double azimuth);

        Assert.False(solved);
        Assert.Equal(0d, distance);
        Assert.Equal(0d, azimuth);
    }

    /// <summary>
    /// Verifies that the shared helpers reduce to the expected great-circle behavior on a sphere.
    /// </summary>
    [Fact]
    public void VincentyHelpers_WithZeroFlattening_MatchEquatorialGreatCircleExpectations()
    {
        const double radius = 6371000d;
        const double distance = 1000000d;
        double latitude1 = 0d;
        double longitude1 = DegreesToRadians(10d);
        double azimuth1 = DegreesToRadians(90d);
        double expectedLongitude2 = longitude1 + (distance / radius);

        bool directSolved = EllipsoidalGeodesic.TryVincentyDirect(
            radius,
            0d,
            0d,
            latitude1,
            longitude1,
            azimuth1,
            distance,
            out double latitude2,
            out double longitude2);

        Assert.True(directSolved);
        Assert.Equal(0d, latitude2, 12);
        Assert.Equal(expectedLongitude2, longitude2, 12);

        bool inverseSolved = EllipsoidalGeodesic.TryVincentyInverse(
            radius,
            0d,
            0d,
            latitude1,
            longitude1,
            latitude2,
            longitude2,
            out double inverseDistance,
            out double inverseAzimuth);

        Assert.True(inverseSolved);
        Assert.Equal(distance, inverseDistance, 6);
        Assert.Equal(azimuth1, inverseAzimuth, 12);
    }

    private static double DegreesToRadians(double degrees)
        => degrees * (Math.PI / 180d);

    private static double DmsToRadians(int degrees, int minutes, double seconds)
    {
        double sign = degrees < 0 ? -1d : 1d;
        double absoluteDegrees = Math.Abs(degrees) + (minutes / 60d) + (seconds / 3600d);
        return sign * DegreesToRadians(absoluteDegrees);
    }

    private static double NormalizePositiveAzimuth(double azimuth)
        => azimuth >= 0d ? azimuth : azimuth + (2d * Math.PI);

    private static GeodesicParameters CreateWgs84Parameters()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        double semiMajor = ellipsoid.SemiMajorAxis;
        double semiMinor = ellipsoid.SemiMinorAxis;
        double flattening = 1d / ellipsoid.InverseFlattening;
        double eccentricityPrimeSquared = ((semiMajor * semiMajor) / (semiMinor * semiMinor)) - 1d;
        return new GeodesicParameters(semiMajor, semiMinor, flattening, eccentricityPrimeSquared);
    }

    private readonly record struct GeodesicParameters(
        double SemiMajor,
        double SemiMinor,
        double Flattening,
        double EccentricityPrimeSquared);
}
