// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests support for cylindrical map projections, verifying alias resolution from WKT and forward/inverse coordinate roundtrip accuracy.
/// </summary>
public class CylindricalProjectionSupportTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies that Miller projection aliases can be parsed from WKT.
    /// </summary>
    /// <param name="projectionName">The projection alias under test.</param>
    [Theory]
    [InlineData("mill")]
    [InlineData("miller")]
    [InlineData("miller_cylindrical")]
    public void SupportsMillerProjectionAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies that the Miller cylindrical projection supports a forward/inverse coordinate roundtrip within the expected tolerance.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(17.45d, -23.1d, 1e-6d)]
    public void SupportsMillerProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("mill"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that EQC projection aliases can be parsed from WKT.
    /// </summary>
    /// <param name="projectionName">The projection alias under test.</param>
    [Theory]
    [InlineData("eqc")]
    [InlineData("equidistant_cylindrical")]
    [InlineData("plate_carree")]
    [InlineData("equirectangular")]
    public void SupportsEqcProjectionAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies that the Equidistant Cylindrical projection supports a forward/inverse coordinate roundtrip within the expected tolerance.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(-11.25d, 31.8d, 1e-8d)]
    public void SupportsEqcProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("eqc"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that EQC honors true-scale latitude aliases instead of silently falling back to the equatorial default.
    /// </summary>
    [Theory]
    [InlineData("lat_ts")]
    [InlineData("latitude_true_scale")]
    [InlineData("latitude_of_true_scale")]
    public void SupportsEqcTrueScaleLatitudeAliases(string parameterName)
    {
        ProjectedCoordinateSystem projectedWithAlias = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildEqcAliasWkt(parameterName));
        ProjectedCoordinateSystem projectedWithCanonicalParameter = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildEqcAliasWkt("standard_parallel_1"));
        ICoordinateTransformation aliasForward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projectedWithAlias.GeographicCoordinateSystem, projectedWithAlias);
        ICoordinateTransformation canonicalForward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projectedWithCanonicalParameter.GeographicCoordinateSystem, projectedWithCanonicalParameter);

        double[] aliasPoint = aliasForward.MathTransform.Transform(CreatePoint(2d, 0d));
        double[] canonicalPoint = canonicalForward.MathTransform.Transform(CreatePoint(2d, 0d));

        Assert.InRange(System.Math.Abs(aliasPoint[0] - canonicalPoint[0]), 0d, 1e-9d);
        Assert.InRange(System.Math.Abs(aliasPoint[1] - canonicalPoint[1]), 0d, 1e-12d);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for the ellipsoidal EQC path.
    /// </summary>
    /// <param name="standardParallel">The latitude of true scale in degrees.</param>
    /// <param name="latitudeOfOrigin">The latitude of natural origin in degrees.</param>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="expectedX">Expected easting in metres.</param>
    /// <param name="expectedY">Expected northing in metres.</param>
    /// <param name="tolerance">Allowed absolute tolerance in metres.</param>
    [Theory]
    [InlineData(0d, 0d, 10d, 55d, 1113194.91d, 6097230.31d, 0.05d)]
    [InlineData(45d, 0d, 2d, 49d, 157693.670d, 5429627.632d, 0.05d)]
    [InlineData(30d, 45d, 0d, 60d, 0d, 1669128.442d, 0.05d)]
    public void MatchesEqcEllipsoidalForwardVectors(
        double standardParallel,
        double latitudeOfOrigin,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildEqcProjectedWkt(true, "lat_ts", standardParallel, latitudeOfOrigin));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(System.Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(System.Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for the ellipsoidal EQC path.
    /// </summary>
    /// <param name="standardParallel">The latitude of true scale in degrees.</param>
    /// <param name="latitudeOfOrigin">The latitude of natural origin in degrees.</param>
    /// <param name="x">Input easting in metres.</param>
    /// <param name="y">Input northing in metres.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    /// <param name="expectedLatitude">Expected latitude in degrees.</param>
    /// <param name="tolerance">Allowed absolute tolerance in degrees.</param>
    [Theory]
    [InlineData(0d, 0d, 1113194.91d, 6097230.31d, 10d, 55d, 1e-7d)]
    [InlineData(45d, 0d, 157693.670d, 5429627.632d, 2d, 49d, 1e-7d)]
    [InlineData(30d, 45d, 0d, 1669128.442d, 0d, 60d, 1e-7d)]
    public void MatchesEqcEllipsoidalInverseVectors(
        double standardParallel,
        double latitudeOfOrigin,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildEqcProjectedWkt(true, "lat_ts", standardParallel, latitudeOfOrigin));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(System.Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that CEA projection aliases can be parsed from WKT.
    /// </summary>
    /// <param name="projectionName">The projection alias under test.</param>
    [Theory]
    [InlineData("cea")]
    [InlineData("cylindrical_equal_area")]
    [InlineData("lambert_cylindrical_equal_area")]
    [InlineData("equal_area_cylindrical")]
    public void SupportsCeaProjectionAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies that the Cylindrical Equal Area projection supports a forward/inverse coordinate roundtrip within the expected tolerance.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(42.6d, 14.2d, 1e-8d)]
    public void SupportsCeaProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("cea"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that loxim projection aliases can be parsed from WKT.
    /// </summary>
    /// <param name="projectionName">The projection alias under test.</param>
    [Theory]
    [InlineData("loxim")]
    [InlineData("loximuthal")]
    public void SupportsLoximProjectionAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies that the Loximuthal projection supports a forward/inverse coordinate roundtrip within the expected tolerance.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(15.75d, -9.4d, 1e-8d)]
    public void SupportsLoximProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("loxim"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that patterson projection aliases can be parsed from WKT.
    /// </summary>
    /// <param name="projectionName">The projection alias under test.</param>
    [Theory]
    [InlineData("patterson")]
    public void SupportsPattersonProjectionAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies that the Patterson projection supports a forward/inverse coordinate roundtrip within the expected tolerance.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(-98.2d, 37.9d, 1e-8d)]
    public void SupportsPattersonProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("patterson"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return
            $"PROJCS[\"Projection-{projectionName}\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static string BuildEqcAliasWkt(string parameterName)
    {
        return BuildEqcProjectedWkt(false, parameterName, 45d, 0d);
    }

    private static string BuildEqcProjectedWkt(bool useWgs84, string standardParallelParameterName, double standardParallelDegrees, double latitudeOfOriginDegrees)
    {
        string spheroidClause = useWgs84
            ? "SPHEROID[\"WGS 84\",6378137,298.257223563]"
            : "SPHEROID[\"Sphere\",6400000,0]";
        return System.FormattableString.Invariant(
            $"PROJCS[\"Projection-eqc\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"eqc\"],PARAMETER[\"latitude_of_origin\",{latitudeOfOriginDegrees}],PARAMETER[\"central_meridian\",0],PARAMETER[\"{standardParallelParameterName}\",{standardParallelDegrees}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y)
    {
        return [x, y];
    }
}
