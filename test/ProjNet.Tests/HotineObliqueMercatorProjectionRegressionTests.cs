// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for Hotine Oblique Mercator parity with PROJ reference vectors.
/// </summary>
public class HotineObliqueMercatorProjectionRegressionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies exact pole forward projection against PROJ, where polar special-case v values are required.
    /// </summary>
    /// <param name="longitude">Input longitude in degrees.</param>
    /// <param name="latitude">Input latitude in degrees.</param>
    /// <param name="expectedX">Expected projected X from PROJ.</param>
    /// <param name="expectedY">Expected projected Y from PROJ.</param>
    [Theory]
    [InlineData(0d, 90d, 264739.4033272466d, 5179881.4284288045d)]
    [InlineData(0d, -90d, -6812922.0677616373d, -14440412.1883131303d)]
    public void HotineObliqueMercatorForwardAtExactPolesMatchesProjReference(
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt());

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);

        double[] projectedPoint = forward.MathTransform.Transform([longitude, latitude]);

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-3d);
    }

    private static string BuildProjectedWkt()
    {
        return
            "PROJCS[\"Regression-omerc\",GEOGCS[\"Regression-Geog\",DATUM[\"Regression-Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"hotine_oblique_mercator\"],PARAMETER[\"latitude_of_center\",45],PARAMETER[\"longitude_of_center\",0],PARAMETER[\"azimuth\",35.264383770917604],PARAMETER[\"rectified_grid_angle\",35.264383770917604],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }
}
