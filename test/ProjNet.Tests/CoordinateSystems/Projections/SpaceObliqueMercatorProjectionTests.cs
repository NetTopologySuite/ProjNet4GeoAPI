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
/// Validates space oblique mercator projection variants.
/// </summary>
public class SpaceObliqueMercatorProjectionTests
{
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies aliases resolve from WKT for space oblique mercator projection variants.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("som")]
    [InlineData("Space_Oblique_Mercator")]
    [InlineData("misrsom")]
    [InlineData("lsat")]
    public void SupportsSpaceObliqueMercatorAliasesFromWkt(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildAliasWkt(projectionName));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for space oblique mercator projection variants.
    /// </summary>
    [Theory]
    [InlineData("som", Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", 2d, 1d, 18556630.368369825d, 9533394.675311271d, 1e-3d)]
    [InlineData("som", Sphere6400000, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", 2d, 1d, 18641249.279170386d, 9563342.532334166d, 1e-3d)]
    [InlineData("misrsom", Grs80, ",PARAMETER[\"path\",1]", 2d, 1d, 18556630.368369825d, 9533394.675311271d, 1e-3d)]
    [InlineData("misrsom", Sphere6400000, ",PARAMETER[\"path\",1]", 2d, 1d, 18641249.279170386d, 9563342.532334166d, 1e-3d)]
    [InlineData("lsat", Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",2]", 2d, 1d, 18241950.014558550d, 9998256.839822935d, 1e-3d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        string spheroidClause,
        string extraParameters,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for space oblique mercator projection variants.
    /// </summary>
    [Theory]
    [InlineData("som", Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", 200d, 100d, 127.759503988d, 0.001735150d, 2e-9d)]
    [InlineData("som", Sphere6400000, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", 200d, 100d, 127.759505148d, 0.001716231d, 2e-9d)]
    [InlineData("misrsom", Grs80, ",PARAMETER[\"path\",1]", 200d, 100d, 127.759503988d, 0.001735150d, 2e-9d)]
    [InlineData("misrsom", Sphere6400000, ",PARAMETER[\"path\",1]", 200d, 100d, 127.759505148d, 0.001716231d, 2e-9d)]
    [InlineData("lsat", Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",2]", 200d, 100d, 126.000423835d, 0.001723782d, 2e-9d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        string spheroidClause,
        string extraParameters,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable space oblique mercator projection variants.
    /// </summary>
    [Theory]
    [InlineData("som", Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", 2d, 1d)]
    [InlineData("som", Sphere6400000, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", -2d, -1d)]
    [InlineData("misrsom", Grs80, ",PARAMETER[\"path\",1]", 2d, 1d)]
    [InlineData("misrsom", Sphere6400000, ",PARAMETER[\"path\",1]", -2d, -1d)]
    [InlineData("lsat", Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",2]", 2d, 1d)]
    public void SupportsSpaceObliqueMercatorRoundtrip(
        string projectionName,
        string spheroidClause,
        string extraParameters,
        double longitude,
        double latitude)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        GeographicCoordinateSystem geographic = projected.GeographicCoordinateSystem;
        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 2e-7d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 2e-7d);
    }

    /// <summary>
    /// Verifies invalid SOM setup parameters are rejected.
    /// </summary>
    [Theory]
    [InlineData("misrsom", Grs80, ",PARAMETER[\"path\",234]")]
    [InlineData("lsat", Grs80, ",PARAMETER[\"lsat\",0],PARAMETER[\"path\",1]")]
    [InlineData("lsat", Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",252]")]
    [InlineData("som", Grs80, ",PARAMETER[\"inc_angle\",190],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]")]
    [InlineData("som", Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"asc_lon\",127.7605356226]")]
    public void RejectsInvalidSpaceObliqueMercatorParameterSets(string projectionName, string spheroidClause, string extraParameters)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
        {
            ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
            CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        });

        Assert.Equal("parameters", exception.ParamName);
    }

    private static string BuildAliasWkt(string projectionName)
    {
        return projectionName.ToUpperInvariant() switch
        {
            "MISRSOM" => BuildProjectedWkt(projectionName, Grs80, ",PARAMETER[\"path\",1]"),
            "LSAT" => BuildProjectedWkt(projectionName, Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",2]"),
            _ => BuildProjectedWkt(projectionName, Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]"),
        };
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string extraParameters)
    {
        return FormattableString.Invariant($"PROJCS[\"Specialty-D4-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
