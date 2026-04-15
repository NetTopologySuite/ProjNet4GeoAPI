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
/// Validates S2 projection variants.
/// </summary>
public class S2ProjectionTests
{
    private const string Wgs84 = "SPHEROID[\"WGS 84\",6378137,298.257223563]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies aliases resolve from WKT for S2 projection variants.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("s2")]
    [InlineData("S2")]
    [InlineData("s2_projection")]
    [InlineData("S2_Projection")]
    public void SupportsS2AliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, 0d, 0d, 1d));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(0d, 0d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for S2 projection variants.
    /// </summary>
    /// <param name="lat0">Projection latitude of origin in degrees.</param>
    /// <param name="lon0">Projection central meridian in degrees.</param>
    /// <param name="uvToSt">uv_to_st mode code (0=linear, 1=quadratic, 2=tangent, 3=none, null=default quadratic).</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x (S2 unit coordinate).</param>
    /// <param name="expectedY">Expected y (S2 unit coordinate).</param>
    [Theory]
    [InlineData(0d, 0d, 0d, 0d, 0d, 0.5d, 0.5d)]
    [InlineData(0d, 0d, 0d, 0d, 45.19242321598196d, 0.5d, 1d)]
    [InlineData(0d, 0d, 0d, -45d, 0d, 0d, 0.5d)]
    [InlineData(0d, 0d, 0d, 20d, 20.124006563576454d, 0.6819851171331012d, 0.6936645165744716d)]
    [InlineData(0d, 90d, 1d, 90d, 0d, 0.5d, 0.5d)]
    [InlineData(0d, 90d, 1d, 70d, 20.124006563576454d, 0.27682804555233764d, 0.7351848576118168d)]
    [InlineData(0d, 90d, 1d, 110d, 20.124006563576454d, 0.7231719544476624d, 0.7351848576118168d)]
    [InlineData(90d, 0d, 2d, 0d, 90d, 0.5d, 0.5d)]
    [InlineData(90d, 0d, 2d, 20d, 70.12337013762532d, 0.29020309743436806d, 0.4211558922141421d)]
    [InlineData(90d, 0d, 2d, -20d, 70.12337013762532d, 0.29020309743436806d, 0.5788441077858579d)]
    [InlineData(0d, 180d, 3d, 180d, 0d, 0d, 0d)]
    [InlineData(0d, 180d, 3d, 160d, 20.124006563576454d, -0.3873290331489431d, -0.3639702342662023d)]
    [InlineData(0d, 180d, 3d, -160d, 20.124006563576454d, -0.3873290331489431d, 0.3639702342662023d)]
    [InlineData(0d, -90d, null, -90d, 0d, 0.5d, 0.5d)]
    [InlineData(0d, -90d, null, -70d, 20.124006563576454d, 0.26481514238818316d, 0.7231719544476624d)]
    [InlineData(0d, -90d, null, -110d, 20.124006563576454d, 0.26481514238818316d, 0.27682804555233764d)]
    [InlineData(-90d, 0d, 0d, 0d, -90d, 0.5d, 0.5d)]
    [InlineData(-90d, 0d, 0d, 20d, -70.12337013762533d, 0.5622425758450019d, 0.6710100716628344d)]
    [InlineData(-90d, 0d, 0d, -20d, -70.12337013762533d, 0.4377574241549981d, 0.6710100716628344d)]
    public void MatchesProjBuiltinsForwardVectors(
        double lat0,
        double lon0,
        double? uvToSt,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("s2", lat0, lon0, uvToSt));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 2e-12d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 2e-12d);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for S2 projection variants.
    /// </summary>
    /// <param name="lat0">Projection latitude of origin in degrees.</param>
    /// <param name="lon0">Projection central meridian in degrees.</param>
    /// <param name="uvToSt">uv_to_st mode code (0=linear, 1=quadratic, 2=tangent, 3=none, null=default quadratic).</param>
    /// <param name="x">Input x (S2 unit coordinate).</param>
    /// <param name="y">Input y (S2 unit coordinate).</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d, 0d, 0.5d, 0.5d, 0d, 0d)]
    [InlineData(0d, 0d, 0d, 0.5d, 1d, 0d, 45.19242321598196d)]
    [InlineData(0d, 0d, 0d, 0d, 0.5d, -45d, 0d)]
    [InlineData(0d, 0d, 0d, 0.6819851171331012d, 0.6936645165744716d, 20d, 20.124006563576454d)]
    [InlineData(0d, 90d, 1d, 0.5d, 0.5d, 90d, 0d)]
    [InlineData(0d, 90d, 1d, 0.27682804555233764d, 0.7351848576118168d, 70d, 20.124006563576454d)]
    [InlineData(90d, 0d, 2d, 0.5d, 0.5d, 0d, 90d)]
    [InlineData(90d, 0d, 2d, 0.29020309743436806d, 0.4211558922141421d, 20d, 70.12337013762532d)]
    [InlineData(0d, 180d, 3d, 0d, 0d, 180d, 0d)]
    [InlineData(0d, 180d, 3d, -0.3873290331489431d, -0.3639702342662023d, 160d, 20.124006563576454d)]
    [InlineData(0d, -90d, null, 0.5d, 0.5d, -90d, 0d)]
    [InlineData(0d, -90d, null, 0.26481514238818316d, 0.7231719544476624d, -70d, 20.124006563576454d)]
    [InlineData(-90d, 0d, 0d, 0.5d, 0.5d, 0d, -90d)]
    [InlineData(-90d, 0d, 0d, 0.5622425758450019d, 0.6710100716628344d, 20d, -70.12337013762533d)]
    public void MatchesProjBuiltinsInverseVectors(
        double lat0,
        double lon0,
        double? uvToSt,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("s2", lat0, lon0, uvToSt));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9d);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9d);
    }

    /// <summary>
    /// Verifies roundtrip stability for representative S2 faces and UV/ST modes.
    /// </summary>
    /// <param name="lat0">Projection latitude of origin in degrees.</param>
    /// <param name="lon0">Projection central meridian in degrees.</param>
    /// <param name="uvToSt">uv_to_st mode code (0=linear, 1=quadratic, 2=tangent, 3=none, null=default quadratic).</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(0d, 0d, 0d, 20d, 20.124006563576454d)]
    [InlineData(0d, 90d, 1d, 70d, 20.124006563576454d)]
    [InlineData(90d, 0d, 2d, 20d, 70.12337013762532d)]
    [InlineData(0d, 180d, 3d, 160d, 20.124006563576454d)]
    [InlineData(0d, -90d, null, -70d, 20.124006563576454d)]
    [InlineData(-90d, 0d, 0d, 20d, -70.12337013762533d)]
    public void SupportsS2Roundtrip(double lat0, double lon0, double? uvToSt, double longitude, double latitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("s2", lat0, lon0, uvToSt));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-9d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-9d);
    }

    /// <summary>
    /// Verifies invalid <c>uv_to_st</c> values are rejected.
    /// </summary>
    [Fact]
    public void RejectsInvalidUvToStMode()
    {
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
        {
            ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("s2", 0d, 0d, 9d));
            ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
            forward.MathTransform.Transform(CreatePoint(0d, 0d));
        });

        ArgumentException inner = Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Equal("parameters", inner.ParamName);
    }

    private static string BuildProjectedWkt(string projectionName, double lat0, double lon0, double? uvToSt)
    {
        string uvParameter = uvToSt.HasValue
            ? FormattableString.Invariant($",PARAMETER[\"uv_to_st\",{uvToSt.Value.ToString("R", CultureInfo.InvariantCulture)}]")
            : string.Empty;

        return FormattableString.Invariant($"PROJCS[\"Specialty-D3-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{Wgs84}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",{lat0.ToString("R", CultureInfo.InvariantCulture)}],PARAMETER[\"central_meridian\",{lon0.ToString("R", CultureInfo.InvariantCulture)}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{uvParameter},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
