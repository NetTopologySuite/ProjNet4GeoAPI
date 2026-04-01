// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Contains regression tests for ellipsoidal projection paths that previously used spherical formulas.
/// </summary>
public class EllipsoidalProjectionRegressionTests
{
    private const string Wgs84 = "SPHEROID[\"WGS 84\",6378137,298.257223563]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies Equal Earth ellipsoidal vectors from PROJ builtins.
    /// </summary>
    [Fact]
    public void EqualEarthEllipsoidalMatchesProjBuiltinsVectors()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt("eqearth", Wgs84, null));

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(-70d, -31.2d));

        Assert.InRange(Math.Abs(projectedPoint[0] - (-6241081.64d)), 0d, 1e-2d);
        Assert.InRange(Math.Abs(projectedPoint[1] - (-3907019.16d)), 0d, 1e-2d);

        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected,
            projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(-6241081.64d, -3907019.16d));

        Assert.InRange(Math.Abs(geographicPoint[0] - (-70d)), 0d, 1e-9d);
        Assert.InRange(Math.Abs(geographicPoint[1] - (-31.2d)), 0d, 1e-9d);
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, string? extraParameters)
    {
        return FormattableString.Invariant(
            $"PROJCS[\"Regression-{projectionName}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{extraParameters ?? string.Empty},UNIT[\"metre\",1]]");
    }

    private static double[] CreatePoint(double x, double y)
    {
        return [x, y];
    }
}
