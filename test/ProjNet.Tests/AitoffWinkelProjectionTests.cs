// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates Aitoff/Winkel projection support and related aliases.
/// </summary>
public class AitoffWinkelProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies that Aitoff and Winkel aliases resolve from WKT.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("aitoff")]
    [InlineData("wink1")]
    [InlineData("wintri")]
    [InlineData("winkel_i")]
    [InlineData("winkel_tripel")]
    public void SupportsAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, null));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies roundtrip stability for Aitoff and Winkel variants.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="latitude1">Optional lat_1 value in degrees for projections that support it.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="tolerance">Maximum absolute roundtrip delta (degrees).</param>
    [Theory]
    [InlineData("aitoff", null, 2d, 1d, 1e-8)]
    [InlineData("wink1", null, -2d, -1d, 1e-8)]
    [InlineData("wintri", 0d, -2d, 1d, 1e-8)]
    public void SupportsRoundtrip(string projectionName, double? latitude1, double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, latitude1));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies that Winkel II forward projection matches PROJ vectors for edge inputs.
    /// </summary>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData(-179.999d, 89.999d, -10052657.852d, 10053040.641d)]
    [InlineData(179.999d, 89.999d, 10052657.852d, 10053040.641d)]
    [InlineData(-179.999d, -89.999d, -10052657.852d, -10053040.641d)]
    [InlineData(179.999d, -89.999d, 10052657.852d, -10053040.641d)]
    public void WinkelIiMatchesProjBuiltinsForwardEdgeVectors(double longitude, double latitude, double expectedX, double expectedY)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("wink2", 0.5d));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-3);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="latitude1">Optional lat_1 value in degrees for projections that support it.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData("aitoff", null, 2d, 1d, 223379.458811696d, 111706.742883853d)]
    [InlineData("wink1", null, 2d, 1d, 223385.131640953d, 111701.072127637d)]
    [InlineData("wink2", 0.5d, 2d, 1d, 223387.396433786d, 124752.032797445d)]
    [InlineData("wintri", 0d, 2d, 1d, 223390.801533485d, 111703.907505745d)]
    [InlineData("winkel_tripel", 0d, -2d, -1d, -223390.801533485d, -111703.907505745d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double? latitude1,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, latitude1));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="latitude1">Optional lat_1 value in degrees for projections that support it.</param>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData("aitoff", null, 200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData("wink1", null, -200d, -100d, -0.001790493d, -0.000895247d)]
    [InlineData("wintri", 0d, -200d, 100d, -0.001790493d, 0.000895247d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double? latitude1,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, latitude1));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        ArgumentNullException.ThrowIfNull(projectionName);
        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies that Winkel II aliases resolve when required lat_1 is provided.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("wink2")]
    [InlineData("winkel_ii")]
    public void SupportsWinkelIiAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, 0.5d));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    private static string BuildProjectedWkt(string projectionName, double? latitude1)
    {
        string lat1Parameter = latitude1.HasValue
            ? $",PARAMETER[\"standard_parallel_1\",{latitude1.Value.ToString(CultureInfo.InvariantCulture)}]"
            : string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"Sphere\",DATUM[\"Sphere_Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{1},UNIT[\"metre\",1]]",
            projectionName,
            lat1Parameter);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}

