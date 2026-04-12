// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies the dedicated Lambert equal-area conic normalization layer over the Albers projection.
/// </summary>
public class LambertEqualAreaConicProjectionTests
{
    /// <summary>
    /// Verifies that the dedicated Lambert projection matches the equivalent normalized Albers projection and round-trips correctly.
    /// </summary>
    /// <param name="south">Whether to exercise the southern-hemisphere variant.</param>
    /// <param name="longitude">Sample longitude in degrees.</param>
    /// <param name="latitude">Sample latitude in degrees.</param>
    [Theory]
    [InlineData(false, -75d, 35d)]
    [InlineData(true, 25d, -35d)]
    public void LambertEqualAreaConicProjection_MatchesNormalizedAlbersAndRoundTrips(bool south, double longitude, double latitude)
    {
        LambertEqualAreaConicProjection lambert = CreateLambertProjection(south);
        AlbersProjection albers = CreateNormalizedAlbersProjection(south);

        double[] expected = albers.Transform([longitude, latitude]);
        double[] actual = lambert.Transform([longitude, latitude]);
        LambertEqualAreaConicProjection inverse = Assert.IsType<LambertEqualAreaConicProjection>(lambert.Inverse());
        double[] roundTripped = inverse.Transform(actual);

        Assert.Equal(expected[0], actual[0], 10);
        Assert.Equal(expected[1], actual[1], 10);
        Assert.Equal(longitude, roundTripped[0], 8);
        Assert.Equal(latitude, roundTripped[1], 8);
    }

    private static LambertEqualAreaConicProjection CreateLambertProjection(bool south)
    {
        return new LambertEqualAreaConicProjection(CreateLambertParameters(south));
    }

    private static AlbersProjection CreateNormalizedAlbersProjection(bool south)
    {
        return new AlbersProjection(CreateNormalizedAlbersParameters(south));
    }

    private static List<ProjectionParameter> CreateLambertParameters(bool south)
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        double latitudeOfCenter = south ? -30d : 30d;
        double latitudeOfStandardParallel = south ? -40d : 40d;
        double centralMeridian = south ? 20d : -96d;
        double falseEasting = south ? 500000d : 0d;
        double falseNorthing = south ? 1000000d : 0d;

        List<ProjectionParameter> parameters =
        [
            new ProjectionParameter("semi_major", ellipsoid.SemiMajorAxis),
            new ProjectionParameter("semi_minor", ellipsoid.SemiMinorAxis),
            new ProjectionParameter("central_meridian", centralMeridian),
            new ProjectionParameter("latitude_of_center", latitudeOfCenter),
            new ProjectionParameter("lat_1", latitudeOfStandardParallel),
            new ProjectionParameter("false_easting", falseEasting),
            new ProjectionParameter("false_northing", falseNorthing),
            new ProjectionParameter("unit", 1d),
        ];

        if (south)
        {
            parameters.Add(new ProjectionParameter("south", 1d));
        }

        return parameters;
    }

    private static IEnumerable<ProjectionParameter> CreateNormalizedAlbersParameters(bool south)
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        double latitudeOfCenter = south ? -30d : 30d;
        double latitudeOfStandardParallel = south ? -40d : 40d;
        double centralMeridian = south ? 20d : -96d;
        double falseEasting = south ? 500000d : 0d;
        double falseNorthing = south ? 1000000d : 0d;

        return
        [
            new ProjectionParameter("semi_major", ellipsoid.SemiMajorAxis),
            new ProjectionParameter("semi_minor", ellipsoid.SemiMinorAxis),
            new ProjectionParameter("central_meridian", centralMeridian),
            new ProjectionParameter("latitude_of_center", latitudeOfCenter),
            new ProjectionParameter("standard_parallel_1", south ? -90d : 90d),
            new ProjectionParameter("standard_parallel_2", latitudeOfStandardParallel),
            new ProjectionParameter("false_easting", falseEasting),
            new ProjectionParameter("false_northing", falseNorthing),
            new ProjectionParameter("unit", 1d),
        ];
    }
}
