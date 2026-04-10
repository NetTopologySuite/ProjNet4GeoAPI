// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

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
}
