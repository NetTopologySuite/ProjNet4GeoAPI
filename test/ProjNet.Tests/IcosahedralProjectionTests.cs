// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Globalization;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates icosahedral projection variants (<c>airocean</c>, <c>isea</c>).
/// </summary>
public class IcosahedralProjectionTests
{
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";
    private const string Sphere637100718091875 = "SPHEROID[\"Sphere\",6371007.18091875,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for icosahedral projection variants.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    /// <param name="isIsea"><see langword="true"/> for ISEA aliases, <see langword="false"/> for Airocean aliases.</param>
    [Theory]
    [InlineData("airocean", false)]
    [InlineData("Airocean", false)]
    [InlineData("isea", true)]
    [InlineData("Icosahedral_Snyder_Equal_Area", true)]
    public void SupportsIcosahedralAliasesFromWkt(string projectionName, bool isIsea)
    {
        string aliasWkt = isIsea
            ? BuildIseaWkt(projectionName, Sphere6400000, 0d, 0d, 3d, 4d, 0d)
            : BuildAiroceanWkt(projectionName, 0d);
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            aliasWkt);
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for <c>airocean</c> vertical and horizontal orientations.
    /// </summary>
    [Theory]
    [InlineData(0d, 23d, 28d, 13572113.73386754d, 23493648.55327798d, 1e-3d)]
    [InlineData(0d, 71d, 46d, 9714915.991790695d, 23488176.361173604d, 1e-3d)]
    [InlineData(1d, 23d, 28d, 13391387.087562159d, 13572113.73386754d, 1e-3d)]
    [InlineData(1d, 71d, 46d, 13396859.279666536d, 9714915.991790695d, 1e-3d)]
    public void MatchesAiroceanForwardVectors(
        double orientCode,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildAiroceanWkt("airocean", orientCode));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for <c>airocean</c> vertical and horizontal orientations.
    /// </summary>
    [Theory]
    [InlineData(0d, 13600000d, 23500000d, 22.77346472511832d, 27.745464601997153d, 2e-9d)]
    [InlineData(0d, 9700000d, 23500000d, 71.26673004703193d, 45.89205035111361d, 2e-9d)]
    [InlineData(1d, 13400000d, 13600000d, 22.653513921934305d, 27.877587719075937d, 2e-9d)]
    [InlineData(1d, 13400000d, 9700000d, 71.23213038171733d, 46.05944622180928d, 2e-9d)]
    public void MatchesAiroceanInverseVectors(
        double orientCode,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildAiroceanWkt("airocean", orientCode));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies <c>airocean</c> roundtrip stability across both orientations.
    /// </summary>
    [Theory]
    [InlineData(0d, 23d, 28d)]
    [InlineData(0d, -11d, -34d)]
    [InlineData(1d, 23d, 28d)]
    [InlineData(1d, -109d, -46d)]
    public void SupportsAiroceanRoundtrip(double orientCode, double longitude, double latitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildAiroceanWkt("airocean", orientCode));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-7d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-7d);
    }

    /// <summary>
    /// Verifies <c>airocean</c> rejects projected points outside the valid domain.
    /// </summary>
    [Fact]
    public void AiroceanRejectsOutsideDomainInverseInput()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildAiroceanWkt("airocean", 0d));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<ArgumentException>(() => inverse.MathTransform.Transform(CreatePoint(0d, 0d)));
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for <c>isea</c> default and pole orientation.
    /// </summary>
    [Theory]
    [InlineData(Sphere6400000, 0d, 2d, 1d, -1097074.9481534758d, 3442909.3097474533d, 1e-3d)]
    [InlineData(Sphere6400000, 0d, -2d, -1d, -1575486.3537720195d, 3234352.6953102099d, 1e-3d)]
    [InlineData(Sphere637100718091875, 0d, 0d, 0d, -1331454.0746232667d, 3323137.7716348548d, 1e-3d)]
    [InlineData(Sphere637100718091875, 0d, 90d, 0d, 8564460.6391008701d, 593869.2974855418d, 1e-3d)]
    [InlineData(Sphere637100718091875, 1d, 0d, 0d, 0d, -195097.13364071414d, 1e-3d)]
    [InlineData(Sphere637100718091875, 1d, 90d, 0d, 9593072.4354674518d, 0d, 1e-3d)]
    public void MatchesIseaForwardVectors(
        string spheroidClause,
        double orientCode,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildIseaWkt("isea", spheroidClause, orientCode, 0d, 3d, 4d, 0d));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins-derived inverse vectors for <c>isea</c> default and pole orientation.
    /// </summary>
    [Theory]
    [InlineData(Sphere6400000, 0d, -1097074.9481534758d, 3442909.3097474533d, 2d, 1d, 2e-9d)]
    [InlineData(Sphere6400000, 0d, -1575486.3537720195d, 3234352.6953102099d, -2d, -1d, 2e-9d)]
    [InlineData(Sphere637100718091875, 0d, -1331454.0746232667d, 3323137.7716348548d, 0d, 0d, 2e-9d)]
    [InlineData(Sphere637100718091875, 0d, 8564460.6391008701d, 593869.2974855418d, 90d, 0d, 2e-9d)]
    [InlineData(Sphere637100718091875, 1d, 0d, -195097.13364071414d, 0d, 0d, 2e-9d)]
    [InlineData(Sphere637100718091875, 1d, 9593072.4354674518d, 0d, 90d, 0d, 2e-9d)]
    public void MatchesIseaInverseVectors(
        string spheroidClause,
        double orientCode,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildIseaWkt("isea", spheroidClause, orientCode, 0d, 3d, 4d, 0d));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies <c>isea</c> roundtrip stability for the supported inverse parameter sets.
    /// </summary>
    [Theory]
    [InlineData(Sphere6400000, 0d, 2d, 1d, 2e-7d)]
    [InlineData(Sphere6400000, 0d, -2d, -1d, 2e-7d)]
    [InlineData(Sphere637100718091875, 0d, -75d, 45d, 2e-7d)]
    [InlineData(Sphere637100718091875, 1d, -75d, 45d, 2e-7d)]
    public void SupportsIseaRoundtrip(
        string spheroidClause,
        double orientCode,
        double longitude,
        double latitude,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildIseaWkt("isea", spheroidClause, orientCode, 0d, 3d, 4d, 0d));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies unsupported ISEA modes are rejected by projection construction.
    /// </summary>
    [Theory]
    [InlineData(1d, 3d, 4d)]
    [InlineData(2d, 3d, 4d)]
    [InlineData(3d, 3d, 31d)]
    public void IseaRejectsUnsupportedModes(double modeCode, double aperture, double resolution)
    {
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
        {
            ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
                CoordinateSystemFactory,
                BuildIseaWkt("isea", Sphere6400000, 0d, modeCode, aperture, resolution, 0d));
            CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        });

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies ISEA inverse is explicitly limited to the supported planar parameter set.
    /// </summary>
    [Fact]
    public void IseaRejectsInverseOutsideSupportedPlanarSubset()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildIseaWkt("isea", Sphere6400000, 0d, 0d, 3d, 5d, 0d));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(100d, 100d)));
    }

    private static string BuildAiroceanWkt(string projectionName, double orientCode)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D8-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"orient\",{2}],UNIT[\"metre\",1]]",
            projectionName,
            Grs80,
            orientCode.ToString("R", CultureInfo.InvariantCulture));
    }

    private static string BuildIseaWkt(
        string projectionName,
        string spheroidClause,
        double orientCode,
        double modeCode,
        double aperture,
        double resolution,
        double azimuth)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D8-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"orient\",{2}],PARAMETER[\"mode\",{3}],PARAMETER[\"aperture\",{4}],PARAMETER[\"resolution\",{5}],PARAMETER[\"azi\",{6}],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            orientCode.ToString("R", CultureInfo.InvariantCulture),
            modeCode.ToString("R", CultureInfo.InvariantCulture),
            aperture.ToString("R", CultureInfo.InvariantCulture),
            resolution.ToString("R", CultureInfo.InvariantCulture),
            azimuth.ToString("R", CultureInfo.InvariantCulture));
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
