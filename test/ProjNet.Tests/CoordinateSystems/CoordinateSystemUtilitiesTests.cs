// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
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
    [InlineData(180d, 60L)]
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

    /// <summary>
    /// Verifies that the obsolete <see cref="MapProjection.CalcUtmZone"/> helper forwards to <see cref="CoordinateSystemUtilities"/>.
    /// </summary>
    [Fact]
    public void ObsoleteCalcUtmZone_MatchesCoordinateSystemUtilities()
    {
        Assert.Equal(
            CoordinateSystemUtilities.CalcUtmZone(15d),
            CompatibilityProjection.ForwardCalcUtmZone(15d));
    }

    /// <summary>
    /// Verifies that the obsolete <see cref="MapProjection"/> angle helpers forward to <see cref="CoordinateSystemUtilities"/>.
    /// </summary>
    [Fact]
    public void ObsoleteAngleHelpers_MatchCoordinateSystemUtilities()
    {
        _ = new CompatibilityProjection();

        Assert.Equal(
            CoordinateSystemUtilities.LongitudeToRadians(45d, edge: false),
            CompatibilityProjection.ForwardLongitudeToRadians(45d, edge: false),
            12);
        Assert.Equal(
            CoordinateSystemUtilities.LatitudeToRadians(-30d, edge: true),
            CompatibilityProjection.ForwardLatitudeToRadians(-30d, edge: true),
            12);
    }

    /// <summary>
    /// Verifies that the obsolete <see cref="MapProjection.Adjust_lon"/> helper still normalizes longitudes to [-π, π].
    /// </summary>
    [Theory]
    [InlineData(0d, 0d)]
    [InlineData(Math.PI, Math.PI)]
    [InlineData(-Math.PI, -Math.PI)]
    [InlineData(1.5d * Math.PI, -0.5d * Math.PI)]
    [InlineData(-1.5d * Math.PI, 0.5d * Math.PI)]
    [InlineData(2d * Math.PI, 0d)]
    [InlineData(-2d * Math.PI, 0d)]
    [InlineData(5d * Math.PI, Math.PI)]
    [InlineData(-5d * Math.PI, -Math.PI)]
    [InlineData((20d * Math.PI) + 0.25d, 0.25d)]
    [InlineData((-20d * Math.PI) - 0.25d, -0.25d)]
    public void ObsoleteAdjustLon_NormalizesToCanonicalInterval(double longitude, double expected)
    {
        Assert.Equal(expected, CompatibilityProjection.ForwardAdjustLon(longitude), 12);
    }

    private sealed class CompatibilityProjection : MapProjection
    {
        internal CompatibilityProjection()
            : base(
                [
                    new ProjectionParameter("semi_major", 6378137d),
                    new ProjectionParameter("semi_minor", 6356752.314245179d),
                    new ProjectionParameter("unit", 1d),
                    new ProjectionParameter("central_meridian", 0d),
                ])
        {
        }

        public override MathTransform Inverse() => this;

        internal static long ForwardCalcUtmZone(double longitude)
        {
#pragma warning disable CS0618
            return CalcUtmZone(longitude);
#pragma warning restore CS0618
        }

        internal static double ForwardLongitudeToRadians(double longitude, bool edge)
        {
#pragma warning disable CS0618
            return LongitudeToRadians(longitude, edge);
#pragma warning restore CS0618
        }

        internal static double ForwardLatitudeToRadians(double latitude, bool edge)
        {
#pragma warning disable CS0618
            return LatitudeToRadians(latitude, edge);
#pragma warning restore CS0618
        }

        internal static double ForwardAdjustLon(double longitude)
        {
#pragma warning disable CS0618
            return Adjust_lon(longitude);
#pragma warning restore CS0618
        }

        protected override void MetersToRadians(ref double x, ref double y)
        {
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
        }
    }
}
