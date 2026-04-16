// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures single-point array-transform throughput for projection paths touched by the GIE failure-coverage fixes.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
public class ProjectionSinglePointBenchmarks
{
    private MathTransform guyouForward = null!;
    private MathTransform peirceForward = null!;
    private MathTransform adamsHemisphereForward = null!;
    private MathTransform obliqueMercatorForward = null!;
    private MathTransform obliqueMercatorNoRotationForward = null!;
    private MathTransform laeaInverse = null!;
    private MathTransform orthographicInverse = null!;
    private MathTransform robinsonInverse = null!;

    private double[] guyouInput = null!;
    private double[] peirceInput = null!;
    private double[] adamsHemisphereInput = null!;
    private double[] obliqueMercatorInput = null!;
    private double[] laeaInput = null!;
    private double[] orthographicInput = null!;
    private double[] robinsonInput = null!;

    /// <summary>
    /// Executes one pass of every single-point projection benchmark and validates that the results stay finite.
    /// </summary>
    public static void Validate()
    {
        var benchmark = new ProjectionSinglePointBenchmarks();
        benchmark.GlobalSetup();

        EnsureFinite(benchmark.TransformGuyouSinglePoint());
        EnsureFinite(benchmark.TransformPeirceQuincuncialSinglePoint());
        EnsureFinite(benchmark.TransformAdamsHemisphereSinglePoint());
        EnsureFinite(benchmark.TransformObliqueMercatorSinglePoint());
        EnsureFinite(benchmark.TransformObliqueMercatorNoRotationSinglePoint());
        EnsureFinite(benchmark.TransformLambertAzimuthalEqualAreaInverseSinglePoint());
        EnsureFinite(benchmark.TransformOrthographicInverseSinglePoint());
        EnsureFinite(benchmark.TransformRobinsonInverseSinglePoint());
    }

    /// <summary>
    /// Creates the projection transforms and representative valid source coordinates used by the benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.guyouForward = BenchmarkPipelineTransformFactory.Create("+proj=guyou");
        this.peirceForward = BenchmarkPipelineTransformFactory.Create("+proj=peirce_q +shape=square");
        this.adamsHemisphereForward = BenchmarkPipelineTransformFactory.Create("+proj=adams_hemi");
        this.obliqueMercatorForward = BenchmarkPipelineTransformFactory.Create("+proj=omerc +ellps=GRS80 +lat_1=0.5 +lat_2=2");
        this.obliqueMercatorNoRotationForward = BenchmarkPipelineTransformFactory.Create("+proj=omerc +ellps=GRS80 +lat_1=0.5 +lat_2=2 +no_rot");

        MathTransform laeaForward = BenchmarkPipelineTransformFactory.Create("+proj=laea +R=6371000 +lat_0=45");
        MathTransform orthographicForward = BenchmarkPipelineTransformFactory.Create("+proj=ortho +ellps=WGS84 +lat_0=30");
        MathTransform robinsonForward = BenchmarkPipelineTransformFactory.Create("+proj=robin +a=6400000");

        this.laeaInverse = laeaForward.Inverse();
        this.orthographicInverse = orthographicForward.Inverse();
        this.robinsonInverse = robinsonForward.Inverse();

        this.guyouInput = [12d, 25d];
        this.peirceInput = [-15d, 35d];
        this.adamsHemisphereInput = [40d, 30d];
        this.obliqueMercatorInput = [2d, 1d];
        this.laeaInput = laeaForward.Transform([15d, 20d]);
        this.orthographicInput = orthographicForward.Transform([20d, 40d]);
        this.robinsonInput = robinsonForward.Transform([30d, 12d]);
    }

    /// <summary>
    /// Measures single-point forward throughput for the Guyou projection.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark(Baseline = true)]
    public double[] TransformGuyouSinglePoint() => this.guyouForward.Transform(this.guyouInput);

    /// <summary>
    /// Measures single-point forward throughput for the Peirce quincuncial projection.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformPeirceQuincuncialSinglePoint() => this.peirceForward.Transform(this.peirceInput);

    /// <summary>
    /// Measures single-point forward throughput for the Adams hemisphere-in-a-square projection.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformAdamsHemisphereSinglePoint() => this.adamsHemisphereForward.Transform(this.adamsHemisphereInput);

    /// <summary>
    /// Measures single-point forward throughput for Hotine oblique Mercator in the default rotated-grid mode.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformObliqueMercatorSinglePoint() => this.obliqueMercatorForward.Transform(this.obliqueMercatorInput);

    /// <summary>
    /// Measures single-point forward throughput for Hotine oblique Mercator with <c>+no_rot</c>.
    /// </summary>
    /// <returns>The projected coordinate pair.</returns>
    [Benchmark]
    public double[] TransformObliqueMercatorNoRotationSinglePoint() => this.obliqueMercatorNoRotationForward.Transform(this.obliqueMercatorInput);

    /// <summary>
    /// Measures single-point inverse throughput for spherical Lambert azimuthal equal area.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformLambertAzimuthalEqualAreaInverseSinglePoint() => this.laeaInverse.Transform(this.laeaInput);

    /// <summary>
    /// Measures single-point inverse throughput for oblique ellipsoidal Orthographic.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformOrthographicInverseSinglePoint() => this.orthographicInverse.Transform(this.orthographicInput);

    /// <summary>
    /// Measures single-point inverse throughput for Robinson.
    /// </summary>
    /// <returns>The reconstructed geographic coordinate pair.</returns>
    [Benchmark]
    public double[] TransformRobinsonInverseSinglePoint() => this.robinsonInverse.Transform(this.robinsonInput);

    private static void EnsureFinite(double[] coordinates)
    {
        for (int i = 0; i < coordinates.Length; i++)
        {
            if (double.IsNaN(coordinates[i]) || double.IsInfinity(coordinates[i]))
            {
                throw new InvalidOperationException("Benchmark validation failed: transform produced non-finite values.");
            }
        }
    }
}
