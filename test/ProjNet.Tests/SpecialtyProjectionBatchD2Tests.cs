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
/// Validates specialty batch D2 specialty projections.
/// </summary>
public class SpecialtyProjectionBatchD2Tests
{
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";
    private const string Sphere6370997 = "SPHEROID[\"Sphere\",6370997,0]";
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";
    private const string Clarke66 = "SPHEROID[\"Clarke 1866\",6378206.4,294.9786982]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for batch D2 projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    [Theory]
    [InlineData("qsc", Grs80)]
    [InlineData("Quadrilateralized_Spherical_Cube", Grs80)]
    [InlineData("rouss", Grs80)]
    [InlineData("Roussilhe_Stereographic", Grs80)]
    [InlineData("mil_os", Sphere6400000)]
    [InlineData("Miller_Oblated_Stereographic", Sphere6400000)]
    [InlineData("lee_os", Sphere6400000)]
    [InlineData("Lee_Oblated_Stereographic", Sphere6400000)]
    [InlineData("gs48", Sphere6370997)]
    [InlineData("Modified_Stereographic_48_US", Sphere6370997)]
    [InlineData("alsk", Clarke66)]
    [InlineData("Modified_Stereographic_Alaska", Clarke66)]
    [InlineData("gs50", Clarke66)]
    [InlineData("Modified_Stereographic_50_US", Clarke66)]
    public void SupportsBatchD2AliasesFromWkt(string projectionName, string spheroidClause)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for batch D2 projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [InlineData("qsc", Grs80, 2d, 1d, 304638.450843852d, 164123.870923794d, 1e-6d)]
    [InlineData("rouss", Grs80, 2d, 1d, 222644.894131617d, 110611.091868370d, 1e-6d)]
    [InlineData("mil_os", Sphere6400000, 2d, 1d, -1908527.949594205d, -1726237.473061448d, 1e-6d)]
    [InlineData("lee_os", Sphere6400000, 2d, 1d, -25564478.952605054d, 154490848.828625500d, 1e-6d)]
    [InlineData("gs48", Sphere6370997, -119d, 40d, -1923908.446529346d, 355874.658944479d, 1e-6d)]
    [InlineData("alsk", Clarke66, -160d, 55d, -513253.146950842d, -968928.031867943d, 1e-6d)]
    [InlineData("gs50", Clarke66, -130d, 45d, -771831.518853336d, 48465.166491305d, 1e-6d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        string spheroidClause,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for batch D2 projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    /// <param name="tolerance">Absolute tolerance.</param>
    [Theory]
    [InlineData("qsc", Grs80, 200d, 100d, 0.001321341d, 0.000610653d, 2e-9d)]
    [InlineData("rouss", Grs80, 200d, 100d, 0.001796631d, 0.000904369d, 2e-9d)]
    [InlineData("mil_os", Sphere6400000, 200d, 100d, 20.002036394d, 18.000968347d, 2e-9d)]
    [InlineData("lee_os", Sphere6400000, 200d, 100d, -164.997479458d, -9.998758861d, 2e-9d)]
    [InlineData("gs48", Sphere6370997, -1923000d, 355000d, -118.987112613d, 39.994449789d, 2e-9d)]
    [InlineData("alsk", Clarke66, -500000d, -950000d, -159.830804303d, 55.183195262d, 2e-9d)]
    [InlineData("gs50", Clarke66, -800000d, 500000d, -131.171390467d, 49.084969746d, 2e-9d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        string spheroidClause,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable batch D2 projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="spheroidClause">Spheroid clause.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData("qsc", Grs80, 2d, 1d)]
    [InlineData("rouss", Grs80, 2d, 1d)]
    [InlineData("mil_os", Sphere6400000, 2d, 1d)]
    [InlineData("lee_os", Sphere6400000, -164.997479458d, -9.998758861d)]
    [InlineData("gs48", Sphere6370997, -95d, 35d)]
    [InlineData("alsk", Clarke66, -145d, 60d)]
    [InlineData("gs50", Clarke66, -80d, 36d)]
    public void SupportsBatchD2Roundtrip(string projectionName, string spheroidClause, double longitude, double latitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-7d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-7d);
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D2-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
