// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies canonical transformation behavior against fixed reference points.
/// </summary>
public class VerificationSuiteTests
{
    /// <summary>
    /// Verifies WGS84 to WebMercator conversion against reference coordinates.
    /// </summary>
    /// <param name="lon">Input longitude.</param>
    /// <param name="lat">Input latitude.</param>
    /// <param name="expectedX">Expected projected X.</param>
    /// <param name="expectedY">Expected projected Y.</param>
    [Theory]
    [InlineData(0d, 0d, 0d, 0d)]
    [InlineData(10d, 10d, 1113194.90793274d, 1118889.97485796d)]
    [InlineData(-75d, 35d, -8348961.80949552d, 4163881.14406429d)]
    public void Wgs84ToWebMercatorMatchesReferencePoints(double lon, double lat, double expectedX, double expectedY)
    {
        var services = CreateCanonicalServices();
        var transform = Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(4326, 3857));
        double[] result = transform.MathTransform.Transform(new[] { lon, lat });

        AssertCoordinate(expectedX, expectedY, result[0], result[1], 1e-6);
    }

    /// <summary>
    /// Verifies WebMercator to WGS84 conversion against reference coordinates.
    /// </summary>
    /// <param name="x">Input projected X.</param>
    /// <param name="y">Input projected Y.</param>
    /// <param name="expectedLon">Expected longitude.</param>
    /// <param name="expectedLat">Expected latitude.</param>
    [Theory]
    [InlineData(0d, 0d, 0d, 0d)]
    [InlineData(1113194.90793274d, 1118889.97485796d, 10d, 10d)]
    [InlineData(-8348961.80949552d, 4163881.14406429d, -75d, 35d)]
    public void WebMercatorToWgs84MatchesReferencePoints(double x, double y, double expectedLon, double expectedLat)
    {
        var services = CreateCanonicalServices();
        var transform = Assert.IsAssignableFrom<ICoordinateTransformation>(services.CreateTransformation(3857, 4326));
        double[] result = transform.MathTransform.Transform(new[] { x, y });

        AssertCoordinate(expectedLon, expectedLat, result[0], result[1], 1e-9);
    }

    /// <summary>
    /// Verifies that legacy coordinate system service lookup APIs return consistent results.
    /// </summary>
    [Fact]
    public void LegacyCoordinateSystemServicesLookupsRemainConsistent()
    {
        var services = CreateCanonicalServices();

        var bySrid = Assert.IsAssignableFrom<CoordinateSystem>(services.GetCoordinateSystem(4326));
        var byAuthority = Assert.IsAssignableFrom<CoordinateSystem>(services.GetCoordinateSystem("EPSG", 4326));
        bool found = services.TryGetCoordinateSystem("EPSG", 4326, out var byTryGet);
        int? srid = services.GetSRID("EPSG", 4326);

        Assert.NotNull(bySrid);
        Assert.NotNull(byAuthority);
        Assert.True(found);
        Assert.NotNull(byTryGet);
        Assert.Equal(4326, srid);
        Assert.Same(bySrid, byAuthority);
        Assert.Same(bySrid, byTryGet);
    }

    private static CoordinateSystemServices CreateCanonicalServices()
    {
        return new CoordinateSystemServices(new[]
        {
            new System.Collections.Generic.KeyValuePair<int, string>(4326, GeographicCoordinateSystem.WGS84.WKT),
            new System.Collections.Generic.KeyValuePair<int, string>(3857, ProjectedCoordinateSystem.WebMercator.WKT),
        });
    }

    private static void AssertCoordinate(double expectedX, double expectedY, double actualX, double actualY, double tolerance)
    {
        Assert.InRange(actualX, expectedX - tolerance, expectedX + tolerance);
        Assert.InRange(actualY, expectedY - tolerance, expectedY + tolerance);
    }
}
