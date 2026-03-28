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
/// Validates Swiss Oblique Mercator (<c>somerc</c>) projection support.
/// </summary>
public class SwissObliqueMercatorProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies that somerc aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("somerc")]
    [InlineData("Swiss_Oblique_Mercator")]
    public void SupportsSomercAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] result = transform.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward/inverse roundtrip stability for somerc aliases.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="tolerance">Maximum absolute roundtrip delta (degrees).</param>
    [Theory]
    [InlineData("somerc", 2d, 1d, 1e-9)]
    [InlineData("somerc", -2d, -1d, 1e-9)]
    [InlineData("Swiss_Oblique_Mercator", 0.25d, -0.5d, 1e-9)]
    public void SupportsSomercRoundtrip(string projectionName, double longitude, double latitude, double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, false));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies forward values against PROJ builtins vectors for ellipsoidal and spherical somerc.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="useSphere">Whether to use a spherical ellipsoid definition.</param>
    /// <param name="longitude">Input longitude (degrees).</param>
    /// <param name="latitude">Input latitude (degrees).</param>
    /// <param name="expectedX">Expected x result (meters).</param>
    /// <param name="expectedY">Expected y result (meters).</param>
    [Theory]
    [InlineData("somerc", false, 2d, 1d, 222638.981586547d, 110579.965218249d)]
    [InlineData("somerc", false, 2d, -1d, 222638.981586547d, -110579.965218251d)]
    [InlineData("somerc", true, 2d, 1d, 223402.144255274d, 111706.743574944d)]
    [InlineData("Swiss_Oblique_Mercator", true, -2d, -1d, -223402.144255274d, -111706.743574945d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        bool useSphere,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, useSphere));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies inverse values against PROJ builtins vectors for ellipsoidal and spherical somerc.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="useSphere">Whether to use a spherical ellipsoid definition.</param>
    /// <param name="x">Input x (meters).</param>
    /// <param name="y">Input y (meters).</param>
    /// <param name="expectedLongitude">Expected longitude (degrees).</param>
    /// <param name="expectedLatitude">Expected latitude (degrees).</param>
    [Theory]
    [InlineData("somerc", false, 200d, 100d, 0.001796631d, 0.000904369d)]
    [InlineData("somerc", false, -200d, -100d, -0.001796631d, -0.000904369d)]
    [InlineData("somerc", true, 200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData("Swiss_Oblique_Mercator", true, -200d, -100d, -0.001790493d, -0.000895247d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        bool useSphere,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, useSphere));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 1e-9);
    }

    private static string BuildProjectedWkt(string projectionName, bool useSphere)
    {
        string spheroid = useSphere
            ? "SPHEROID[\"Sphere\",6400000,0]"
            : "SPHEROID[\"GRS 80\",6378137,298.257222101]";

        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            spheroid);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
