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
/// Validates current projection group projections (<c>cc</c>, <c>gn_sinu</c>, <c>eck6</c>, <c>mbtfps</c>, <c>urm5</c>, <c>urmfps</c>, <c>wag1</c>).
/// </summary>
public class CentralCylindricalAndUrmaevProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies aliases resolve from WKT for covered projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("cc", null)]
    [InlineData("Central_Cylindrical", null)]
    [InlineData("gn_sinu", ",PARAMETER[\"m\",1],PARAMETER[\"n\",2]")]
    [InlineData("General_Sinusoidal", ",PARAMETER[\"m\",1],PARAMETER[\"n\",2]")]
    [InlineData("eck6", null)]
    [InlineData("Eckert_VI", null)]
    [InlineData("mbtfps", null)]
    [InlineData("McBryde_Thomas_Flat_Polar_Sinusoidal", null)]
    [InlineData("urmfps", ",PARAMETER[\"n\",0.5]")]
    [InlineData("Urmaev_Flat_Polar_Sinusoidal", ",PARAMETER[\"n\",0.5]")]
    [InlineData("urm5", ",PARAMETER[\"n\",0.5]")]
    [InlineData("Urmaev_V", ",PARAMETER[\"n\",0.5]")]
    [InlineData("wag1", null)]
    [InlineData("Wagner_I", null)]
    public void SupportsAliasesFromWkt(string projectionName, string? extraParameters)
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
    /// Verifies PROJ builtins forward vectors for covered projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("cc", 2d, 1d, 223402.144255274d, 111712.415540593d, null)]
    [InlineData("gn_sinu", 2d, 1d, 223385.132504696d, 111698.236447187d, ",PARAMETER[\"m\",1],PARAMETER[\"n\",2]")]
    [InlineData("eck6", 2d, 1d, 197021.605628992d, 126640.420733174d, null)]
    [InlineData("mbtfps", 2d, 1d, 204740.117478572d, 121864.729719340d, null)]
    [InlineData("urm5", 2d, 1d, 223393.638433964d, 111696.818785117d, ",PARAMETER[\"n\",0.5]")]
    [InlineData("urmfps", 2d, 1d, 196001.708134192d, 127306.843329993d, ",PARAMETER[\"n\",0.5]")]
    [InlineData("wag1", 2d, 1d, 195986.781561158d, 127310.075060660d, null)]
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
    /// Verifies PROJ builtins inverse vectors for inverse-capable covered projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("cc", 200d, 100d, 0.001790493d, 0.000895247d, null)]
    [InlineData("gn_sinu", 200d, 100d, 0.001790493d, 0.000895247d, ",PARAMETER[\"m\",1],PARAMETER[\"n\",2]")]
    [InlineData("eck6", 200d, 100d, 0.002029979d, 0.000789630d, null)]
    [InlineData("mbtfps", 200d, 100d, 0.001953415d, 0.000820580d, null)]
    [InlineData("urmfps", 200d, 100d, 0.002040721d, 0.000785474d, ",PARAMETER[\"n\",0.5]")]
    [InlineData("wag1", 200d, 100d, 0.002040721d, 0.000785474d, null)]
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
    /// Verifies Urmaev V remains forward-only in this test set.
    /// </summary>
    [Fact]
    public void Urm5DoesNotSupportInverse()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("urm5", false, ",PARAMETER[\"n\",0.5]"));
        Assert.Throws<NotSupportedException>(
            () => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem));
    }

    /// <summary>
    /// Verifies Urmaev V rejects invalid <c>n/alpha</c> combinations.
    /// </summary>
    [Fact]
    public void Urm5RejectsInvalidNAlphaCombination()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("urm5", false, ",PARAMETER[\"n\",1],PARAMETER[\"alpha\",90]"));
        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable covered projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("cc", 2d, 1d, null)]
    [InlineData("gn_sinu", -2d, -1d, ",PARAMETER[\"m\",1],PARAMETER[\"n\",2]")]
    [InlineData("eck6", 2d, -1d, null)]
    [InlineData("mbtfps", -2d, 1d, null)]
    [InlineData("urmfps", 2d, 1d, ",PARAMETER[\"n\",0.5]")]
    [InlineData("wag1", -2d, -1d, null)]
    public void SupportsRoundtrip(string projectionName, double longitude, double latitude, string? extraParameters)
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

    private static string BuildProjectedWkt(string projectionName, bool useWgs84, string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        string spheroidClause = useWgs84
            ? "SPHEROID[\"WGS 84\",6378137,298.257223563]"
            : "SPHEROID[\"Sphere\",6400000,0]";
        return FormattableString.Invariant($"PROJCS[\"Projection-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
