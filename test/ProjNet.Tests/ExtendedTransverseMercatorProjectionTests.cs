// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates exact ETMERC support against PROJ reference vectors.
/// </summary>
public class ExtendedTransverseMercatorProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies that ETMERC aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("etmerc")]
    [InlineData("ETMERC")]
    [InlineData("Extended_Transverse_Mercator")]
    public void SupportsEtmercAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(projectionName));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward ETMERC vectors from PROJ builtins, including the wide-offset hotspot.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="expectedX">Expected easting in metres.</param>
    /// <param name="expectedY">Expected northing in metres.</param>
    [Theory]
    [InlineData(2d, 1d, 222650.796797586d, 110642.229411933d)]
    [InlineData(44.69d, 35.37d, 4168136.489446198d, 4985511.302287407d)]
    public void MatchesProjBuiltinsForwardVectors(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt("etmerc"));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-6d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-6d);
    }

    /// <summary>
    /// Verifies inverse ETMERC vectors from PROJ builtins, including the wide-offset hotspot.
    /// </summary>
    /// <param name="x">Input easting in metres.</param>
    /// <param name="y">Input northing in metres.</param>
    /// <param name="expectedLongitude">Expected longitude in degrees.</param>
    /// <param name="expectedLatitude">Expected latitude in degrees.</param>
    [Theory]
    [InlineData(200d, 100d, 0.00179663056816d, 0.00090436947663d)]
    [InlineData(4168136.489446198d, 4985511.302287407d, 44.69d, 35.37d)]
    public void MatchesProjBuiltinsInverseVectors(
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt("etmerc"));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 1e-10d);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 1e-10d);
    }

    /// <summary>
    /// Verifies forward/inverse roundtrip stability for the exact ETMERC kernel.
    /// </summary>
    [Fact]
    public void SupportsWideOffsetRoundtrip()
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt("etmerc"));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(44.69d, 35.37d));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - 44.69d), 0d, 1e-10d);
        Assert.InRange(Math.Abs(roundtrip[1] - 35.37d), 0d, 1e-10d);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return FormattableString.Invariant(
            $"PROJCS[\"Projection-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"GRS 80\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
