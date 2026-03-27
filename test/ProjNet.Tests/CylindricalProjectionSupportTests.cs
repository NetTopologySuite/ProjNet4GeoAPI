// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class CylindricalProjectionSupportTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

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
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(17.45d, -23.1d, 1e-6d)]
    public void SupportsMillerProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("mill"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

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
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(-11.25d, 31.8d, 1e-8d)]
    public void SupportsEqcProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("eqc"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
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
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(42.6d, 14.2d, 1e-8d)]
    public void SupportsCeaProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("cea"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

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
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(15.75d, -9.4d, 1e-8d)]
    public void SupportsLoximProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("loxim"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

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
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(1000d, 2000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="tolerance">Allowed roundtrip tolerance.</param>
    [Theory]
    [InlineData(-98.2d, 37.9d, 1e-8d)]
    public void SupportsPattersonProjectionRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("patterson"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

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

    private static double[] CreatePoint(double x, double y)
    {
        return new[] { x, y };
    }
}
