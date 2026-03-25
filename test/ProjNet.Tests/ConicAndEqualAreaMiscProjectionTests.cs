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
/// Validates conic and equal-area miscellaneous projections.
/// </summary>
public class ConicAndEqualAreaMiscProjectionTests
{
    private const string Sphere6390000 = "SPHEROID[\"Sphere\",6390000,0]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for conic and equal-area miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("kav5")]
    [InlineData("Kavrayskiy_V")]
    [InlineData("qua_aut")]
    [InlineData("Quartic_Authalic")]
    [InlineData("fouc")]
    [InlineData("Foucaut")]
    [InlineData("mbt_s")]
    [InlineData("McBryde_Thomas_Flat_Polar_Sine")]
    [InlineData("ccon")]
    [InlineData("Central_Conic")]
    [InlineData("lcca")]
    [InlineData("Lambert_Conformal_Conic_Alternative")]
    [InlineData("ocea")]
    [InlineData("Oblique_Cylindrical_Equal_Area")]
    [InlineData("oea")]
    [InlineData("Oblated_Equal_Area")]
    [InlineData("rpoly")]
    [InlineData("Rectangular_Polyconic")]
    [InlineData("tpeqd")]
    [InlineData("Two_Point_Equidistant")]
    public void SupportsConicAndEqualAreaMiscAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildAliasWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for conic and equal-area miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [InlineData("kav5", 2d, 1d, 200360.905308829d, 123685.082476998d, 1e-6d)]
    [InlineData("qua_aut", 2d, 1d, 222613.549033097d, 111318.077887984d, 1e-6d)]
    [InlineData("fouc", 2d, 1d, 222588.120675892d, 111322.316700694d, 1e-6d)]
    [InlineData("mbt_s", 2d, 1d, 204131.517850273d, 121400.330225508d, 1e-6d)]
    [InlineData("lcca", 2d, 1d, 222605.285770237d, 67.806007272d, 1e-6d)]
    [InlineData("ocea", 2d, 1d, 19994423.837934088d, 223322.760576728d, 1e-3d)]
    [InlineData("oea", 2d, 1d, 228926.872097864d, 99870.488430076d, 1e-6d)]
    [InlineData("rpoly", 2d, 1d, 223368.098302014d, 111769.110486991d, 1e-6d)]
    [InlineData("tpeqd", 2d, 1d, -27750.758831679d, -222599.403691777d, 1e-6d)]
    [InlineData("ccon", 24d, 55d, 650031.5410941322d, -4106.161777064670d, 1e-6d)]
    public void MatchesProjBuiltinsForwardVectors(string projectionName, double longitude, double latitude, double expectedX, double expectedY, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildCanonicalWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable conic and equal-area miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [InlineData("kav5", 200d, 100d, 0.001996259d, 0.000808483d, 2e-9d)]
    [InlineData("qua_aut", 200d, 100d, 0.001796631d, 0.000898315d, 2e-9d)]
    [InlineData("fouc", 200d, 100d, 0.001796631d, 0.000898315d, 2e-9d)]
    [InlineData("mbt_s", 200d, 100d, 0.001959383d, 0.000823699d, 2e-9d)]
    [InlineData("lcca", 200d, 100d, 0.001796903d, 1.000904366d, 2e-9d)]
    [InlineData("ocea", 200d, 100d, 179.999104753d, 0.001790493d, 2e-9d)]
    [InlineData("oea", 200d, 100d, 0.001741186d, 0.000987727d, 2e-9d)]
    [InlineData("tpeqd", 200d, 100d, -0.000898556d, 1.251796630d, 2e-9d)]
    [InlineData("ccon", 330000d, -350000d, 19d, 52d, 2e-11d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildCanonicalWkt(projectionName));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable conic and equal-area miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData("kav5", 2d, 1d)]
    [InlineData("qua_aut", -2d, -1d)]
    [InlineData("fouc", 2d, -1d)]
    [InlineData("mbt_s", -2d, 1d)]
    [InlineData("lcca", 2d, 1d)]
    [InlineData("ocea", 2d, 1d)]
    [InlineData("oea", 2d, 1d)]
    [InlineData("tpeqd", 2d, 1d)]
    [InlineData("ccon", 24d, 55d)]
    public void SupportsConicAndEqualAreaMiscRoundtrip(string projectionName, double longitude, double latitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildCanonicalWkt(projectionName));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-7d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-7d);
    }

    /// <summary>
    /// Verifies <c>rpoly</c> remains forward-only.
    /// </summary>
    [Fact]
    public void RectangularPolyconicDoesNotSupportInverse()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildRpolyWkt("rpoly"));
        Assert.Throws<InvalidOperationException>(
            () => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem));
    }

    /// <summary>
    /// Verifies <c>ocea</c> alpha/lonc mode matches builtins vectors.
    /// </summary>
    [Fact]
    public void ObliqueCylindricalEqualAreaSupportsAlphaLoncMode()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildOceaAlphaWkt("ocea"));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.InRange(Math.Abs(projectedPoint[0] - 19994423.837934091687d), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - 223322.760576728586d), 0d, 1e-6d);
    }

    /// <summary>
    /// Verifies <c>tpeqd</c> rejects degenerate pole control points.
    /// </summary>
    [Fact]
    public void TwoPointEquidistantRejectsDegeneratePolarControlPoints()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildTpeqdDegenerateWkt());
        var exception = Assert.Throws<TargetInvocationException>(
            () => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    private static string BuildAliasWkt(string projectionName)
    {
        if (projectionName.Equals("ccon", StringComparison.OrdinalIgnoreCase)
            || projectionName.Equals("central_conic", StringComparison.OrdinalIgnoreCase))
        {
            return BuildCconWkt(projectionName);
        }

        if (projectionName.Equals("lcca", StringComparison.OrdinalIgnoreCase)
            || projectionName.Equals("lambert_conformal_conic_alternative", StringComparison.OrdinalIgnoreCase))
        {
            return BuildLccaWkt(projectionName);
        }

        if (projectionName.Equals("ocea", StringComparison.OrdinalIgnoreCase)
            || projectionName.Equals("oblique_cylindrical_equal_area", StringComparison.OrdinalIgnoreCase))
        {
            return BuildOceaTwoPointWkt(projectionName);
        }

        if (projectionName.Equals("oea", StringComparison.OrdinalIgnoreCase)
            || projectionName.Equals("oblated_equal_area", StringComparison.OrdinalIgnoreCase))
        {
            return BuildOeaWkt(projectionName);
        }

        if (projectionName.Equals("rpoly", StringComparison.OrdinalIgnoreCase)
            || projectionName.Equals("rectangular_polyconic", StringComparison.OrdinalIgnoreCase))
        {
            return BuildRpolyWkt(projectionName);
        }

        if (projectionName.Equals("tpeqd", StringComparison.OrdinalIgnoreCase)
            || projectionName.Equals("two_point_equidistant", StringComparison.OrdinalIgnoreCase))
        {
            return BuildTpeqdWkt(projectionName);
        }

        return BuildProjectedWkt(projectionName, Grs80, null);
    }

    private static string BuildCanonicalWkt(string projectionName)
    {
        return projectionName.ToLowerInvariant() switch
        {
            "ccon" => BuildCconWkt("ccon"),
            "lcca" => BuildLccaWkt("lcca"),
            "ocea" => BuildOceaTwoPointWkt("ocea"),
            "oea" => BuildOeaWkt("oea"),
            "rpoly" => BuildRpolyWkt("rpoly"),
            "tpeqd" => BuildTpeqdWkt("tpeqd"),
            _ => BuildProjectedWkt(projectionName, Grs80, null),
        };
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string extraParameters)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{2},UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            extraParameters ?? string.Empty);
    }

    private static string BuildCconWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",52],PARAMETER[\"central_meridian\",19],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",330000],PARAMETER[\"false_northing\",-350000],PARAMETER[\"lat_1\",52],UNIT[\"metre\",1]]",
            projectionName,
            Sphere6390000);
    }

    private static string BuildLccaWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",1],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2],UNIT[\"metre\",1]]",
            projectionName,
            Grs80);
    }

    private static string BuildOceaTwoPointWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2],PARAMETER[\"lon_1\",0],PARAMETER[\"lon_2\",0],UNIT[\"metre\",1]]",
            projectionName,
            Sphere6400000);
    }

    private static string BuildOceaAlphaWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}-alpha\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",45],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"alpha\",0],PARAMETER[\"lonc\",0],UNIT[\"metre\",1]]",
            projectionName,
            Sphere6400000);
    }

    private static string BuildOeaWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"n\",1],PARAMETER[\"m\",2],PARAMETER[\"theta\",3],UNIT[\"metre\",1]]",
            projectionName,
            Sphere6400000);
    }

    private static string BuildRpolyWkt(string projectionName)
    {
        return BuildProjectedWkt(projectionName, Sphere6400000, null);
    }

    private static string BuildTpeqdWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-C-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2],PARAMETER[\"lon_1\",0],PARAMETER[\"lon_2\",0],UNIT[\"metre\",1]]",
            projectionName,
            Grs80);
    }

    private static string BuildTpeqdDegenerateWkt()
    {
        return "PROJCS[\"Specialty-C-tpeqd-degenerate\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"tpeqd\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"lat_1\",90],PARAMETER[\"lat_2\",90],PARAMETER[\"lon_1\",0],PARAMETER[\"lon_2\",1],UNIT[\"metre\",1]]";
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
