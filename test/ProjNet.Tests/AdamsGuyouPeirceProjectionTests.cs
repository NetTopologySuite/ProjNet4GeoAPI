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
/// Validates Adams, Guyou, and Peirce projection families.
/// </summary>
public class AdamsGuyouPeirceProjectionTests
{
    private const string Sphere6370997 = "SPHEROID[\"Sphere\",6370997,0]";
    private const string Wgs84 = "SPHEROID[\"WGS 84\",6378137,298.257223563]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for Adams, Guyou, and Peirce projection families.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("guyou")]
    [InlineData("Guyou")]
    [InlineData("peirce_q")]
    [InlineData("Peirce_Quincuncial")]
    [InlineData("adams_hemi")]
    [InlineData("Adams_Hemisphere_In_A_Square")]
    [InlineData("adams_ws1")]
    [InlineData("Adams_World_In_A_Square_I")]
    [InlineData("adams_ws2")]
    [InlineData("Adams_World_In_A_Square_II")]
    public void SupportsAdamsGuyouPeirceAliasesFromWkt(string projectionName)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, Sphere6370997, null));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ fixture forward vectors for Adams/Guyou/Peirce family projections.
    /// </summary>
    [Theory]
    [InlineData("adams_hemi", Sphere6370997, -89.9433443609d, -87.0825895518d, -2032451.307d, -14670658.595d, null, 2e-3d)]
    [InlineData("adams_ws1", Sphere6370997, -159.5146913398d, -89.9552061084d, -350717.162d, -11748881.092d, null, 2e-3d)]
    [InlineData("adams_ws2", Sphere6370997, -169.9316998581d, -89.6983443874d, -2757243.603d, -13694037.516d, null, 2e-3d)]
    [InlineData("guyou", Sphere6370997, -89.3858632536d, -85.7390309668d, -671252.534d, -11805089.168d, null, 2e-3d)]
    [InlineData("guyou", "SPHEROID[\"Sphere\",1,0]", 0d, 90d, 0d, 1.85407d, null, 1e-5d)]
    [InlineData("peirce_q", Sphere6370997, -159.2003712209d, -89.5537263306d, -16684778.66d, 16659858.26d, ",PARAMETER[\"shape\",0]", 0.2d)]
    [InlineData("peirce_q", Sphere6370997, -159.2003712209d, -89.5537263306d, 11829925.59d, 46389.53d, ",PARAMETER[\"shape\",4]", 0.2d)]
    [InlineData("peirce_q", Sphere6370997, -159.2003712209d, -89.5537263306d, 17621.38d, 46389.53d, ",PARAMETER[\"shape\",4],PARAMETER[\"scrollx\",0.75]", 0.2d)]
    [InlineData("peirce_q", Sphere6370997, -159.2003712209d, -89.5537263306d, -17621.38d, 11765914.68d, ",PARAMETER[\"shape\",5]", 0.2d)]
    [InlineData("peirce_q", Sphere6370997, -159.2003712209d, -89.5537263306d, -17621.38d, -46389.53d, ",PARAMETER[\"shape\",5],PARAMETER[\"scrolly\",-0.25]", 0.2d)]
    [InlineData("peirce_q", Sphere6370997, -179.2332724818d, 70.8217746040d, -28794.10d, 2152288.77d, ",PARAMETER[\"shape\",2]", 0.2d)]
    [InlineData("peirce_q", Sphere6370997, -159.2003712209d, -89.5537263306d, -17621.38d, 46389.53d, ",PARAMETER[\"shape\",3]", 0.2d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        string spheroidClause,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        string? extraParameters,
        double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ fixture inverse vectors for inverse-capable Adams, Guyou, and Peirce projection families.
    /// </summary>
    [Theory]
    [InlineData("adams_ws2", Wgs84, 0d, 0d, 0d, 0d, null, 2e-9d)]
    [InlineData("adams_ws2", Wgs84, 2021909.611d, 4162291.966d, 40d, 60d, null, 5e-9d)]
    [InlineData("peirce_q", Sphere6370997, 0d, 0d, 0d, 90d, ",PARAMETER[\"shape\",0]", 2e-9d)]
    [InlineData("peirce_q", Sphere6370997, 8361921.234827488d, -8361921.234827488d, 0d, 0d, ",PARAMETER[\"shape\",0]", 0.1d)]
    [InlineData("peirce_q", Sphere6370997, 11825542.552198235d, 0d, 90d, 0d, ",PARAMETER[\"shape\",1]", 0.1d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        string spheroidClause,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        string? extraParameters,
        double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable Adams, Guyou, and Peirce projection families.
    /// </summary>
    [Theory]
    [InlineData("adams_ws2", Wgs84, 40d, 60d, null, 2e-7d)]
    [InlineData("adams_ws2", Wgs84, -179.999d, 0d, null, 2e-6d)]
    [InlineData("peirce_q", Sphere6370997, 45d, 45d, ",PARAMETER[\"shape\",0]", 1e-6d)]
    [InlineData("peirce_q", Sphere6370997, 90d, 0d, ",PARAMETER[\"shape\",1]", 1e-6d)]
    public void SupportsAdamsGuyouPeirceRoundtrip(string projectionName, string spheroidClause, double longitude, double latitude, string? extraParameters, double tolerance)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies Adams/Guyou forward-only and Peirce shape-dependent inverse behavior.
    /// </summary>
    /// <param name="projectionName">Projection name.</param>
    /// <param name="extraParameters">Optional projection parameters.</param>
    [Theory]
    [InlineData("guyou", null)]
    [InlineData("adams_hemi", null)]
    [InlineData("adams_ws1", null)]
    [InlineData("peirce_q", ",PARAMETER[\"shape\",2]")]
    [InlineData("peirce_q", ",PARAMETER[\"shape\",3]")]
    [InlineData("peirce_q", ",PARAMETER[\"shape\",4]")]
    [InlineData("peirce_q", ",PARAMETER[\"shape\",5]")]
    public void ForwardOnlyVariantsDoNotSupportInverse(string projectionName, string? extraParameters)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, Sphere6370997, extraParameters));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(200d, 100d)));
    }

    /// <summary>
    /// Verifies invalid Peirce shape and scroll ranges are rejected.
    /// </summary>
    [Theory]
    [InlineData(",PARAMETER[\"shape\",9]")]
    [InlineData(",PARAMETER[\"shape\",4],PARAMETER[\"scrollx\",1.5]")]
    [InlineData(",PARAMETER[\"shape\",5],PARAMETER[\"scrolly\",-1.5]")]
    public void RejectsInvalidPeirceParameters(string? extraParameters)
    {
        Assert.Throws<System.Reflection.TargetInvocationException>(() =>
        {
            var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("peirce_q", Sphere6370997, extraParameters));
            CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        });
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string? extraParameters)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Specialty-D6-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{2},UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            extraParameters ?? string.Empty);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
