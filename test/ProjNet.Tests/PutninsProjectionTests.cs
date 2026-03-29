// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates Putnins projection support for current projection group (<c>putp2</c>, <c>putp3</c>, <c>putp4p</c>, <c>putp5</c>, <c>putp6</c>).
/// </summary>
public class PutninsProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies aliases resolve from WKT for Putnins projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("putp2")]
    [InlineData("Putnins_P2")]
    [InlineData("putp3")]
    [InlineData("Putnins_P3")]
    [InlineData("putp4p")]
    [InlineData("Putnins_P4P")]
    [InlineData("putp5")]
    [InlineData("Putnins_P5")]
    [InlineData("putp6")]
    [InlineData("Putnins_P6")]
    public void SupportsAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for Putnins projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    [Theory]
    [InlineData("putp2", 2d, 1d, 211638.039634339d, 117895.033043380d)]
    [InlineData("putp2", -2d, -1d, -211638.039634339d, -117895.033043380d)]
    [InlineData("putp3", 2d, 1d, 178227.115507794d, 89124.560786088d)]
    [InlineData("putp3", -2d, -1d, -178227.115507794d, -89124.560786088d)]
    [InlineData("putp4p", 2d, 1d, 195241.477349386d, 127796.782307926d)]
    [InlineData("putp4p", -2d, -1d, -195241.477349386d, -127796.782307926d)]
    [InlineData("putp5", 2d, 1d, 226367.213380562d, 113204.568558475d)]
    [InlineData("putp5", -2d, -1d, -226367.213380562d, -113204.568558475d)]
    [InlineData("putp6", 2d, 1d, 226369.395133403d, 110218.523796521d)]
    [InlineData("putp6", -2d, -1d, -226369.395133403d, -110218.523796521d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for Putnins projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    [Theory]
    [InlineData("putp2", 200d, 100d, 0.001889802d, 0.000848202d)]
    [InlineData("putp2", -200d, -100d, -0.001889802d, -0.000848202d)]
    [InlineData("putp3", 200d, 100d, 0.002244050d, 0.001122025d)]
    [InlineData("putp3", -200d, -100d, -0.002244050d, -0.001122025d)]
    [InlineData("putp4p", 200d, 100d, 0.002048528d, 0.000782480d)]
    [InlineData("putp4p", -200d, -100d, -0.002048528d, -0.000782480d)]
    [InlineData("putp5", 200d, 100d, 0.001766713d, 0.000883357d)]
    [InlineData("putp5", -200d, -100d, -0.001766713d, -0.000883357d)]
    [InlineData("putp6", 200d, 100d, 0.001766713d, 0.000907296d)]
    [InlineData("putp6", -200d, -100d, -0.001766713d, -0.000907296d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies roundtrip stability for Putnins projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData("putp2", 2d, 1d)]
    [InlineData("putp3", -2d, -1d)]
    [InlineData("putp4p", 2d, -1d)]
    [InlineData("putp5", -2d, 1d)]
    [InlineData("putp6", 2d, 1d)]
    public void SupportsRoundtrip(string projectionName, double longitude, double latitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-9);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
