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
/// Validates M7 batch D4 specialty projections.
/// </summary>
public class Phase7SpecialtyProjectionBatchD4Tests
{
    private const string Grs80 = "SPHEROID[\"GRS 80\",6378137,298.257222101]";
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for batch D4 projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("som")]
    [InlineData("Space_Oblique_Mercator")]
    [InlineData("misrsom")]
    [InlineData("lsat")]
    public void SupportsBatchD4AliasesFromWkt(string projectionName)
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
    /// Verifies PROJ builtins forward vectors for batch D4 projections.
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
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for batch D4 projections.
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
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable batch D4 projections.
    /// </summary>
    [Theory]
    [InlineData("som", Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", 2d, 1d)]
    [InlineData("som", Sphere6400000, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]", -2d, -1d)]
    [InlineData("misrsom", Grs80, ",PARAMETER[\"path\",1]", 2d, 1d)]
    [InlineData("misrsom", Sphere6400000, ",PARAMETER[\"path\",1]", -2d, -1d)]
    [InlineData("lsat", Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",2]", 2d, 1d)]
    public void SupportsBatchD4Roundtrip(
        string projectionName,
        string spheroidClause,
        string extraParameters,
        double longitude,
        double latitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
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
    public void RejectsInvalidBatchD4ParameterSets(string projectionName, string spheroidClause, string extraParameters)
    {
        var exception = Assert.Throws<TargetInvocationException>(() =>
        {
            var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, spheroidClause, extraParameters));
            CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        });

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    private static string BuildAliasWkt(string projectionName)
    {
        return projectionName.ToLowerInvariant() switch
        {
            "misrsom" => BuildProjectedWkt(projectionName, Grs80, ",PARAMETER[\"path\",1]"),
            "lsat" => BuildProjectedWkt(projectionName, Grs80, ",PARAMETER[\"lsat\",1],PARAMETER[\"path\",2]"),
            _ => BuildProjectedWkt(projectionName, Grs80, ",PARAMETER[\"inc_angle\",98.30382],PARAMETER[\"ps_rev\",0.06866666666666667],PARAMETER[\"asc_lon\",127.7605356226]"),
        };
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string extraParameters)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-D4-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{2},UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause,
            extraParameters ?? string.Empty);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
