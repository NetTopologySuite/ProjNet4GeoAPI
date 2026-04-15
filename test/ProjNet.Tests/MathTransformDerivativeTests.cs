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
    /// Gets representative projection operations used to validate local derivative linearization.
    /// </summary>
    public static TheoryData<string, double, double> ProjectionDerivativeCases =>
        new()
        {
            { "+proj=merc +ellps=WGS84", 10d, 45d },
            { "+proj=tmerc +ellps=WGS84 +lat_0=0 +lon_0=9 +k_0=0.9996 +x_0=500000 +y_0=0", 10d, 45d },
            { "+proj=utm +ellps=GRS80 +zone=32", 10d, 55d },
            { "+proj=lcc +lon_0=3 +lat_0=46.5 +lat_1=44 +lat_2=49 +x_0=700000 +y_0=6600000 +ellps=GRS80", 3d, 47d },
            { "+proj=aea +ellps=GRS80 +lat_1=43 +lat_2=62 +lat_0=30 +lon_0=10 +x_0=0 +y_0=0", 12d, 50d },
            { "+proj=laea +ellps=GRS80 +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000", 12d, 50d },
        };

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

    /// <summary>
    /// Verifies representative projection derivatives locally linearize the transform around the sample point.
    /// </summary>
    /// <param name="operation">The projection operation string.</param>
    /// <param name="longitude">The sample longitude in degrees.</param>
    /// <param name="latitude">The sample latitude in degrees.</param>
    [Theory]
    [MemberData(nameof(ProjectionDerivativeCases))]
    public void Derivative_OnProjectionTransforms_LinearizesLocalOffsets(string operation, double longitude, double latitude)
    {
        MathTransform transform = CreateTransform(operation);
        double[] point = [longitude, latitude];
        double[] baseValue = transform.Transform(point);
        double[,] derivative = transform.Derivative(point);

        Assert.Equal(baseValue.Length, derivative.GetLength(0));
        Assert.Equal(point.Length, derivative.GetLength(1));

        AssertLocalLinearization([1e-6d, 0d]);
        AssertLocalLinearization([0d, 1e-6d]);
        AssertLocalLinearization([1e-6d, -2e-6d]);

        void AssertLocalLinearization(double[] delta)
        {
            double[] displacedPoint = [point[0] + delta[0], point[1] + delta[1]];
            double[] displacedValue = transform.Transform(displacedPoint);

            for (int targetIndex = 0; targetIndex < displacedValue.Length; targetIndex++)
            {
                double predictedOffset = 0d;
                for (int sourceIndex = 0; sourceIndex < delta.Length; sourceIndex++)
                {
                    predictedOffset += derivative[targetIndex, sourceIndex] * delta[sourceIndex];
                }

                double actualOffset = displacedValue[targetIndex] - baseValue[targetIndex];
                AssertInTolerance(actualOffset, predictedOffset, 5e-3d);
            }
        }
    }

    private static void AssertInTolerance(double actual, double expected, double tolerance)
    {
        Assert.InRange(Math.Abs(actual - expected), 0d, tolerance);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }
}
