// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies the default numerical derivative implementation for representative math transform types.
/// </summary>
public class MathTransformDerivativeTests
{
    /// <summary>
    /// Verifies that affine derivatives return the non-translating linear matrix with the expected dimensions.
    /// </summary>
    [Fact]
    public void Derivative_OnAffineTransform_ReturnsLinearJacobian()
    {
        AffineTransform transform = new(2d, 3d, 5d, 7d, 11d, 13d);

        double[,] derivative = transform.Derivative([10d, 20d]);

        Assert.Equal(2, derivative.GetLength(0));
        Assert.Equal(2, derivative.GetLength(1));
        AssertInTolerance(derivative[0, 0], 2d, 1e-6d);
        AssertInTolerance(derivative[0, 1], 3d, 1e-6d);
        AssertInTolerance(derivative[1, 0], 7d, 1e-6d);
        AssertInTolerance(derivative[1, 1], 11d, 1e-6d);
    }

    /// <summary>
    /// Verifies that EPSG:4326 to EPSG:3857 derivatives match the analytic Web Mercator Jacobian.
    /// </summary>
    [Fact]
    public void Derivative_OnWebMercatorTransform_MatchesAnalyticJacobian()
    {
        CoordinateSystemServices services = new();
        ICoordinateTransformation transformation = Assert.IsType<ICoordinateTransformation>(services.CreateTransformation(4326, 3857), exactMatch: false);
        double[,] derivative = transformation.MathTransform.Derivative([10d, 10d]);
        double metresPerDegree = 6378137d * Math.PI / 180d;
        double latitudeRadians = 10d * Math.PI / 180d;
        double expectedLatitudeScale = metresPerDegree / Math.Cos(latitudeRadians);

        Assert.Equal(2, derivative.GetLength(0));
        Assert.Equal(2, derivative.GetLength(1));
        AssertInTolerance(derivative[0, 0], metresPerDegree, 1e-2d);
        AssertInTolerance(derivative[0, 1], 0d, 1e-6d);
        AssertInTolerance(derivative[1, 0], 0d, 1e-6d);
        AssertInTolerance(derivative[1, 1], expectedLatitudeScale, 1e-2d);
    }

    private static void AssertInTolerance(double actual, double expected, double tolerance)
    {
        Assert.InRange(Math.Abs(actual - expected), 0d, tolerance);
    }
}
