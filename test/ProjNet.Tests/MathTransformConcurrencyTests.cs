// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies concurrent read-only use of shared math-transform instances.
/// </summary>
public class MathTransformConcurrencyTests
{
    /// <summary>
    /// Verifies that concurrent callers can share the same forward and inverse Helmert instances without nondeterministic results.
    /// </summary>
    /// <returns>A task that completes when all concurrent transform calls have been verified.</returns>
    [Fact]
    public async Task SharedHelmertTransformsProduceDeterministicConcurrentResults()
    {
        MathTransform forward = CreateTransform("+proj=helmert +convention=coordinate_frame +x=0.67678 +y=0.65495 +z=-0.52827 +rx=-0.022742 +ry=0.012667 +rz=0.022704 +s=-0.01070");
        MathTransform inverse = forward.Inverse();

        double[] source = CreatePoint(3565285.0d, 855949.0d, 5201383.0d);
        double[] expectedForward = forward.Transform(source);
        double[] expectedInverse = inverse.Transform(expectedForward);

        double[][] forwardResults = await TransformConcurrentlyAsync(forward, source, taskCount: 8);
        double[][] inverseResults = await TransformConcurrentlyAsync(inverse, expectedForward, taskCount: 8);

        AssertAllMatch(expectedForward, forwardResults, 1e-9d);
        AssertAllMatch(expectedInverse, inverseResults, 1e-9d);
    }

    private static void AssertAllMatch(double[] expected, double[][] actuals, double tolerance)
    {
        for (int i = 0; i < actuals.Length; i++)
        {
            Assert.Equal(expected.Length, actuals[i].Length);
            for (int j = 0; j < expected.Length; j++)
            {
                Assert.InRange(Math.Abs(actuals[i][j] - expected[j]), 0d, tolerance);
            }
        }
    }

    private static async Task<double[][]> TransformConcurrentlyAsync(MathTransform transform, double[] point, int taskCount)
    {
        using var gate = new ManualResetEventSlim(false);
        Task<double[]>[] tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(() =>
            {
                gate.Wait();
                return transform.Transform(point);
            }))
            .ToArray();

        gate.Set();
        return await Task.WhenAll(tasks).ConfigureAwait(true);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
