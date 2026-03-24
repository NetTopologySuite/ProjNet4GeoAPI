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
/// Validates Gauss-Schreiber Transverse Mercator (<c>gstmerc</c>) projection support.
/// </summary>
public class Phase5GstmercProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies that gstmerc aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("gstmerc")]
    [InlineData("Gauss_Schreiber_Transverse_Mercator")]
    [InlineData("Gauss_Laborde_Reunion")]
    public void SupportsGstmercAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors for gstmerc.
    /// </summary>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData(2d, 1d, 223413.466406322d, 111769.145040586d)]
    [InlineData(2d, -1d, 223413.466406322d, -111769.145040587d)]
    [InlineData(-2d, 1d, -223413.466406323d, 111769.145040586d)]
    [InlineData(-2d, -1d, -223413.466406323d, -111769.145040587d)]
    public void MatchesProjBuiltinsForwardVectors(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("gstmerc"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors for gstmerc.
    /// </summary>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData(200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData(200d, -100d, 0.001790493d, -0.000895247d)]
    [InlineData(-200d, 100d, -0.001790493d, 0.000895247d)]
    [InlineData(-200d, -100d, -0.001790493d, -0.000895247d)]
    public void MatchesProjBuiltinsInverseVectors(
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("gstmerc"));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies forward/inverse roundtrip stability for gstmerc.
    /// </summary>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="tolerance">Maximum absolute roundtrip delta (degrees).</param>
    [Theory]
    [InlineData(2d, 1d, 1e-9)]
    [InlineData(-2d, -1d, 1e-9)]
    public void SupportsGstmercRoundtrip(double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("gstmerc"));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase5-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
