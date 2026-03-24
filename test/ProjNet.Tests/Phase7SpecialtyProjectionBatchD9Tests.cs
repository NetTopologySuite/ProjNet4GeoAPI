// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Globalization;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M7 batch D9 projection aliases and vectors (<c>leac</c>, <c>ups</c>).
/// </summary>
public class Phase7SpecialtyProjectionBatchD9Tests
{
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for batch D9 projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("leac")]
    [InlineData("ups")]
    [InlineData("webmerc")]
    public void SupportsBatchD9AliasesFromWkt(string projectionName)
    {
        string wkt = projectionName.Equals("leac", StringComparison.OrdinalIgnoreCase)
            ? BuildLeacWkt(projectionName, Grs80, 0d, false)
            : BuildUpsWkt(projectionName, Grs80, false);
        if (projectionName.Equals("webmerc", StringComparison.OrdinalIgnoreCase))
        {
            wkt = BuildWebMercWkt(projectionName, Grs80);
        }

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(wkt);
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for <c>leac</c>.
    /// </summary>
    [Theory]
    [InlineData(Grs80, 2d, 1d, 220685.140542979d, 112983.500889396d, 1e-3d)]
    [InlineData(Grs80, 2d, -1d, 224553.312279826d, -108128.636744873d, 1e-3d)]
    [InlineData(Grs80, -2d, 1d, -220685.140542979d, 112983.500889396d, 1e-3d)]
    [InlineData(Grs80, -2d, -1d, -224553.312279826d, -108128.636744873d, 1e-3d)]
    [InlineData(Sphere6400000, 2d, 1d, 221432.868592852d, 114119.454526532d, 1e-3d)]
    [InlineData(Sphere6400000, 2d, -1d, 225331.724127111d, -109245.829435056d, 1e-3d)]
    [InlineData(Sphere6400000, -2d, 1d, -221432.868592852d, 114119.454526532d, 1e-3d)]
    [InlineData(Sphere6400000, -2d, -1d, -225331.724127111d, -109245.829435056d, 1e-3d)]
    public void MatchesLeacForwardVectors(
        string spheroidClause,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildLeacWkt("leac", spheroidClause, 0d, false));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for <c>leac</c>.
    /// </summary>
    [Theory]
    [InlineData(Grs80, 200d, 100d, 0.001796645d, 0.000904352d, 2e-9d)]
    [InlineData(Grs80, 200d, -100d, 0.001796616d, -0.000904387d, 2e-9d)]
    [InlineData(Grs80, -200d, 100d, -0.001796645d, 0.000904352d, 2e-9d)]
    [InlineData(Grs80, -200d, -100d, -0.001796616d, -0.000904387d, 2e-9d)]
    [InlineData(Sphere6400000, 200d, 100d, 0.001790507d, 0.000895229d, 2e-9d)]
    [InlineData(Sphere6400000, 200d, -100d, 0.001790479d, -0.000895264d, 2e-9d)]
    [InlineData(Sphere6400000, -200d, 100d, -0.001790507d, 0.000895229d, 2e-9d)]
    [InlineData(Sphere6400000, -200d, -100d, -0.001790479d, -0.000895264d, 2e-9d)]
    public void MatchesLeacInverseVectors(
        string spheroidClause,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildLeacWkt("leac", spheroidClause, 0d, false));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for <c>ups</c>.
    /// </summary>
    [Theory]
    [InlineData(2d, 1d, 2433455.563438467d, -10412543.301512826d, 1e-3d)]
    [InlineData(2d, -1d, 2448749.118568199d, -10850493.419804076d, 1e-3d)]
    [InlineData(-2d, 1d, 1566544.436561533d, -10412543.301512826d, 1e-3d)]
    [InlineData(-2d, -1d, 1551250.881431801d, -10850493.419804076d, 1e-3d)]
    public void MatchesUpsForwardVectors(double longitude, double latitude, double expectedX, double expectedY, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildUpsWkt("ups", Grs80, false));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for <c>ups</c>.
    /// </summary>
    [Theory]
    [InlineData(200d, 100d, -44.998567498d, 64.918236287d, 2e-9d)]
    [InlineData(200d, -100d, -44.995702709d, 64.917020251d, 2e-9d)]
    [InlineData(-200d, 100d, -45.004297076d, 64.915804281d, 2e-9d)]
    [InlineData(-200d, -100d, -45.001432287d, 64.914588378d, 2e-9d)]
    public void MatchesUpsInverseVectors(double x, double y, double expectedLongitude, double expectedLatitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildUpsWkt("ups", Grs80, false));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies UPS rejects spherical ellipsoids (matching PROJ semantics).
    /// </summary>
    [Fact]
    public void UpsRejectsSphericalEllipsoid()
    {
        var exception = Assert.Throws<TargetInvocationException>(() =>
        {
            var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildUpsWkt("ups", Sphere6400000, false));
            CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        });

        Assert.IsType<NotSupportedException>(exception.InnerException);
    }

    private static string BuildLeacWkt(string projectionName, string spheroidClause, double standardParallel1, bool south)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-D9-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"standard_parallel_1\",{2}],PARAMETER[\"south\",{3}],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            standardParallel1.ToString("R", CultureInfo.InvariantCulture),
            south ? "1" : "0");
    }

    private static string BuildUpsWkt(string projectionName, string spheroidClause, bool south)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-D9-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",{2}],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"south\",{3}],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            south ? "-90" : "90",
            south ? "1" : "0");
    }

    private static string BuildWebMercWkt(string projectionName, string spheroidClause)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-D9-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
