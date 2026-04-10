// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="CoordinateSystemUtilities"/>.
/// </summary>
public class CoordinateSystemUtilitiesTests
{
    /// <summary>
    /// Verifies that <see cref="CoordinateSystemUtilities.CalcUtmZone"/> returns the expected UTM zone.
    /// </summary>
    /// <param name="longitude">The longitude in decimal degrees.</param>
    /// <param name="expectedZone">The expected UTM zone.</param>
    [Theory]
    [InlineData(-180d, 1L)]
    [InlineData(-174d, 2L)]
    [InlineData(0d, 31L)]
    [InlineData(6d, 32L)]
    [InlineData(179d, 60L)]
    public void CalcUtmZone_ReturnsExpectedZone(double longitude, long expectedZone)
    {
        long zone = CoordinateSystemUtilities.CalcUtmZone(longitude);

        Assert.Equal(expectedZone, zone);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemUtilities.LongitudeToRadians"/> converts valid values.
    /// </summary>
    /// <param name="longitude">The longitude in decimal degrees.</param>
    /// <param name="edge">Whether the closed interval endpoints are accepted.</param>
    /// <param name="expectedRadians">The expected value in radians.</param>
    [Theory]
    [InlineData(0d, false, 0d)]
    [InlineData(180d, true, Math.PI)]
    [InlineData(-90d, false, -Math.PI / 2d)]
    public void LongitudeToRadians_WithValidValue_ReturnsRadians(double longitude, bool edge, double expectedRadians)
    {
        double radians = CoordinateSystemUtilities.LongitudeToRadians(longitude, edge);

        Assert.Equal(expectedRadians, radians, 12);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemUtilities.LongitudeToRadians"/> rejects out-of-range values.
    /// </summary>
    /// <param name="longitude">The longitude in decimal degrees.</param>
    /// <param name="edge">Whether the closed interval endpoints are accepted.</param>
    [Theory]
    [InlineData(-180d, false)]
    [InlineData(180d, false)]
    [InlineData(181d, true)]
    public void LongitudeToRadians_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException(double longitude, bool edge)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CoordinateSystemUtilities.LongitudeToRadians(longitude, edge));
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemUtilities.LatitudeToRadians"/> converts valid values.
    /// </summary>
    /// <param name="latitude">The latitude in decimal degrees.</param>
    /// <param name="edge">Whether the closed interval endpoints are accepted.</param>
    /// <param name="expectedRadians">The expected value in radians.</param>
    [Theory]
    [InlineData(0d, false, 0d)]
    [InlineData(90d, true, Math.PI / 2d)]
    [InlineData(-45d, false, -Math.PI / 4d)]
    public void LatitudeToRadians_WithValidValue_ReturnsRadians(double latitude, bool edge, double expectedRadians)
    {
        double radians = CoordinateSystemUtilities.LatitudeToRadians(latitude, edge);

        Assert.Equal(expectedRadians, radians, 12);
    }

    /// <summary>
    /// Verifies that <see cref="CoordinateSystemUtilities.LatitudeToRadians"/> rejects out-of-range values.
    /// </summary>
    /// <param name="latitude">The latitude in decimal degrees.</param>
    /// <param name="edge">Whether the closed interval endpoints are accepted.</param>
    [Theory]
    [InlineData(-90d, false)]
    [InlineData(90d, false)]
    [InlineData(91d, true)]
    public void LatitudeToRadians_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException(double latitude, bool edge)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CoordinateSystemUtilities.LatitudeToRadians(latitude, edge));
    }
}
