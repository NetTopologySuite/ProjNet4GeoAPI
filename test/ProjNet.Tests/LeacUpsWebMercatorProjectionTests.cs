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
/// Validates LEAC, UPS, and Web Mercator projection variants.
/// </summary>
public class LeacUpsWebMercatorProjectionTests
{
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for LEAC, UPS, and Web Mercator projection variants.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("leac")]
    [InlineData("ups")]
    [InlineData("webmerc")]
    public void SupportsLeacUpsWebMercatorAliasesFromWkt(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
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

    /// <summary>
    /// Verifies that batched Web Mercator forward transformation matches point-wise transformation results.
    /// </summary>
    [Fact]
    public void WebMercatorBatchTransformMatchesPointwiseTransform()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildWebMercWkt("webmerc", Grs80));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] batchedLongitudes = [-170d, -120.5d, -45d, 0d, 37.5d, 89.9d, 120.25d, 170d];
        double[] batchedLatitudes = [-80d, -65d, -30.5d, -1d, 1d, 30.5d, 65d, 80d];
        double[] expectedLongitudes = (double[])batchedLongitudes.Clone();
        double[] expectedLatitudes = (double[])batchedLatitudes.Clone();

        for (int i = 0; i < expectedLongitudes.Length; i++)
        {
            forward.MathTransform.Transform(ref expectedLongitudes[i], ref expectedLatitudes[i]);
        }

        forward.MathTransform.Transform(batchedLongitudes, batchedLatitudes);

        for (int i = 0; i < batchedLongitudes.Length; i++)
        {
            Assert.InRange(Math.Abs(batchedLongitudes[i] - expectedLongitudes[i]), 0d, 1e-9d);
            Assert.InRange(Math.Abs(batchedLatitudes[i] - expectedLatitudes[i]), 0d, 1e-9d);
        }
    }

    /// <summary>
    /// Verifies that batched Web Mercator forward transformation propagates NaN inputs consistently with point-wise transformation.
    /// </summary>
    [Fact]
    public void WebMercatorBatchTransformPropagatesNaNConsistently()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildWebMercWkt("webmerc", Grs80));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] batchedLongitudes = [0d, double.NaN, 10d, 20d, double.NaN, -40d, 50d, 60d];
        double[] batchedLatitudes = [0d, 5d, double.NaN, 15d, 20d, double.NaN, 30d, 40d];
        double[] expectedLongitudes = (double[])batchedLongitudes.Clone();
        double[] expectedLatitudes = (double[])batchedLatitudes.Clone();

        for (int i = 0; i < expectedLongitudes.Length; i++)
        {
            forward.MathTransform.Transform(ref expectedLongitudes[i], ref expectedLatitudes[i]);
        }

        forward.MathTransform.Transform(batchedLongitudes, batchedLatitudes);

        for (int i = 0; i < batchedLongitudes.Length; i++)
        {
            Assert.Equal(double.IsNaN(expectedLongitudes[i]), double.IsNaN(batchedLongitudes[i]));
            Assert.Equal(double.IsNaN(expectedLatitudes[i]), double.IsNaN(batchedLatitudes[i]));
            if (!double.IsNaN(expectedLongitudes[i]))
            {
                Assert.InRange(Math.Abs(batchedLongitudes[i] - expectedLongitudes[i]), 0d, 1e-9d);
            }

            if (!double.IsNaN(expectedLatitudes[i]))
            {
                Assert.InRange(Math.Abs(batchedLatitudes[i] - expectedLatitudes[i]), 0d, 1e-9d);
            }
        }
    }

    /// <summary>
    /// Verifies that batched Web Mercator forward transformation rejects pole latitude inputs.
    /// </summary>
    [Fact]
    public void WebMercatorBatchTransformRejectsPoleLatitude()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildWebMercWkt("webmerc", Grs80));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] batchedLongitudes = [0d, 10d, 20d, 30d, 40d, 50d, 60d, 70d];
        double[] batchedLatitudes = [0d, 10d, 20d, 30d, 40d, 50d, 60d, 90d];

        Assert.Throws<ArgumentException>(() => forward.MathTransform.Transform(batchedLongitudes, batchedLatitudes));
    }

    private static string BuildLeacWkt(string projectionName, string spheroidClause, double standardParallel1, bool south)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D9-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"standard_parallel_1\",{2}],PARAMETER[\"south\",{3}],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            standardParallel1.ToString("R", CultureInfo.InvariantCulture),
            south ? "1" : "0");
    }

    private static string BuildUpsWkt(string projectionName, string spheroidClause, bool south)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D9-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",{2}],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"south\",{3}],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            south ? "-90" : "90",
            south ? "1" : "0");
    }

    private static string BuildWebMercWkt(string projectionName, string spheroidClause)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D9-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
