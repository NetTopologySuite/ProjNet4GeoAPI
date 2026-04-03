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
/// Validates globular and miscellaneous specialty projections.
/// </summary>
public class GlobularAndMiscProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies aliases resolve from WKT for globular and miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("august", null)]
    [InlineData("August_Epicycloidal", null)]
    [InlineData("bacon", null)]
    [InlineData("Bacon_Globular", null)]
    [InlineData("apian", null)]
    [InlineData("Apian_Globular_I", null)]
    [InlineData("ortel", null)]
    [InlineData("Ortelius_Oval", null)]
    [InlineData("comill", null)]
    [InlineData("Compact_Miller", null)]
    [InlineData("denoy", null)]
    [InlineData("Denoyer_Semi_Elliptical", null)]
    [InlineData("fouc_s", null)]
    [InlineData("Foucaut_Sinusoidal", null)]
    [InlineData("gins8", null)]
    [InlineData("Ginsburg_VIII", null)]
    [InlineData("lagrng", ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"W\",2]")]
    [InlineData("Lagrange", ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"W\",2]")]
    [InlineData("larr", null)]
    [InlineData("Larrivee", null)]
    [InlineData("lask", null)]
    [InlineData("Laskowski", null)]
    [InlineData("tcc", null)]
    [InlineData("Transverse_Central_Cylindrical", null)]
    public void SupportsGlobularAndMiscAliasesFromWkt(string projectionName, string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false, extraParameters));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for globular and miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("august", 2d, 1d, 223404.978180972d, 111722.340289763d, null)]
    [InlineData("bacon", 2d, 1d, 223334.132555965d, 175450.725922666d, null)]
    [InlineData("apian", 2d, 1d, 223374.577355253d, 111701.072127637d, null)]
    [InlineData("ortel", 2d, 1d, 223374.577355253d, 111701.072127637d, null)]
    [InlineData("comill", 2d, 1d, 223402.144255274d, 110611.859089459d, null)]
    [InlineData("denoy", 2d, 1d, 223377.422876954d, 111701.072127637d, null)]
    [InlineData("fouc_s", 2d, 1d, 223402.144255274d, 111695.401198614d, null)]
    [InlineData("gins8", 2d, 1d, 194350.250939590d, 111703.907635335d, null)]
    [InlineData("lagrng", 2d, 1d, 111703.375917226d, 27929.831908033d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"W\",2]")]
    [InlineData("larr", 2d, 1d, 223393.637624201d, 111707.215961256d, null)]
    [InlineData("lask", 2d, 1d, 217928.275907355d, 112144.329220142d, null)]
    [InlineData("tcc", 2d, 1d, 223458.844192458d, 111769.145040586d, null)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false, extraParameters));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable globular and miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("comill", 200d, 100d, 0.001790493d, 0.000904107d, null)]
    [InlineData("fouc_s", 200d, 100d, 0.001790493d, 0.000895247d, null)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false, extraParameters));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies forward-only globular and miscellaneous projections reject inverse.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("august", null)]
    [InlineData("bacon", null)]
    [InlineData("apian", null)]
    [InlineData("ortel", null)]
    [InlineData("denoy", null)]
    [InlineData("gins8", null)]
    [InlineData("larr", null)]
    [InlineData("lask", null)]
    [InlineData("tcc", null)]
    public void ForwardOnlyProjectionsDoNotSupportInverse(string projectionName, string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false, extraParameters));
        Assert.Throws<NotSupportedException>(
            () => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem));
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable globular and miscellaneous projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("comill", 2d, 1d, null)]
    [InlineData("fouc_s", -2d, -1d, null)]
    [InlineData("lagrng", 2d, -1d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"W\",2]")]
    public void SupportsGlobularAndMiscRoundtrip(string projectionName, double longitude, double latitude, string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false, extraParameters));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-9);
    }

    /// <summary>
    /// Verifies Lagrange rejects invalid W.
    /// </summary>
    [Fact]
    public void LagrangeRejectsInvalidW()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("lagrng", false, ",PARAMETER[\"W\",-1],PARAMETER[\"lat_1\",0.5]"));
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies Lagrange rejects invalid lat_1.
    /// </summary>
    [Fact]
    public void LagrangeRejectsInvalidLat1()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("lagrng", false, ",PARAMETER[\"lat_1\",90.00001]"));
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    private static string BuildProjectedWkt(string projectionName, bool useWgs84, string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        string spheroidClause = useWgs84
            ? "SPHEROID[\"WGS 84\",6378137,298.257223563]"
            : "SPHEROID[\"Sphere\",6400000,0]";
        return FormattableString.Invariant($"PROJCS[\"Specialty-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
