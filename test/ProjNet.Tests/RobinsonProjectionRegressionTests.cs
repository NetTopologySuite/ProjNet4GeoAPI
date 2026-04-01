// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for Robinson projection parity with PROJ reference vectors.
/// </summary>
public class RobinsonProjectionRegressionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies Robinson forward projection against PROJ-generated vectors at latitude band midpoints.
    /// </summary>
    [Theory]
    [InlineData(10d, 2.5d, 944391.935119083d, 267379.790905731d)]
    [InlineData(10d, 12.5d, 938180.308475870d, 1336899.020249400d)]
    [InlineData(10d, 42.5d, 859104.434900386d, 4541404.267002712d)]
    [InlineData(10d, 87.5d, 520350.141311250d, 8537587.027768036d)]
    public void RobinsonForwardMatchesProjCubicReference(double longitude, double latitude, double expectedX, double expectedY)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt("robin"));

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);
        double[] projectedPoint = forward.MathTransform.Transform([longitude, latitude]);

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-3d);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return
            $"PROJCS[\"Regression-{projectionName}\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }
}
