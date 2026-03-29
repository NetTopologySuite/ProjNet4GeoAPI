// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Benchmarks CRS-to-CRS transform throughput using scenarios aligned with PROJ's bench_proj_trans utility.
/// </summary>
/// <remarks>
/// The benchmark suite focuses on forward and inverse EPSG pipeline throughput and includes a deterministic
/// noise variant analogous to PROJ's <c>--noise-x</c>/<c>--noise-y</c> options.
/// </remarks>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for explicit invocation from Program and benchmark tooling stability.")]
[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Benchmark input generation uses deterministic pseudo-random data for repeatability and is not security-sensitive.")]
[MemoryDiagnoser]
public class ProjParityBenchmarks
{
    private const double NoiseXDegrees = 1e-4;
    private const double NoiseYDegrees = 1e-4;

    private static readonly CoordinateSystemServices CoordinateSystemServices = new();

    private static readonly ICoordinateTransformation Wgs84ToWebMercator =
        CoordinateSystemServices.CreateTransformation(4326, 3857)
        ?? throw new InvalidOperationException("EPSG:4326->3857 transformation lookup returned null.");

    private static readonly ICoordinateTransformation Wgs84ToUtm32N =
        CoordinateSystemServices.CreateTransformation(4326, 32632)
        ?? throw new InvalidOperationException("EPSG:4326->32632 transformation lookup returned null.");

    private static readonly ICoordinateTransformation Wgs84ToUtm31N =
        CoordinateSystemServices.CreateTransformation(4326, 32631)
        ?? throw new InvalidOperationException("EPSG:4326->32631 transformation lookup returned null.");

    private static readonly ICoordinateTransformation Utm31NToWgs84 =
        CoordinateSystemServices.CreateTransformation(32631, 4326)
        ?? throw new InvalidOperationException("EPSG:32631->4326 transformation lookup returned null.");

    private static readonly ICoordinateTransformation Wgs84ToLambert93 =
        CoordinateSystemServices.CreateTransformation(4326, 2154)
        ?? throw new InvalidOperationException("EPSG:4326->2154 transformation lookup returned null.");

    private static readonly ICoordinateTransformation Lambert93ToWgs84 =
        CoordinateSystemServices.CreateTransformation(2154, 4326)
        ?? throw new InvalidOperationException("EPSG:2154->4326 transformation lookup returned null.");

    private static readonly ICoordinateTransformation WebMercatorToWgs84 =
        CoordinateSystemServices.CreateTransformation(3857, 4326)
        ?? throw new InvalidOperationException("EPSG:3857->4326 transformation lookup returned null.");

    private double[] longitudes = Array.Empty<double>();
    private double[] latitudes = Array.Empty<double>();
    private double[] xBuffer = Array.Empty<double>();
    private double[] yBuffer = Array.Empty<double>();
    private double[] noiseX = Array.Empty<double>();
    private double[] noiseY = Array.Empty<double>();

    /// <summary>
    /// Gets or sets the number of coordinates processed per benchmark invocation.
    /// </summary>
    [Params(10000)]
    public int PointCount { get; set; }

    /// <summary>
    /// Executes one pass of every benchmark scenario and validates that all outputs are finite.
    /// </summary>
    public static void Validate()
    {
        static void EnsureFinite(double[] xs, double[] ys)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                if (double.IsNaN(xs[i]) || double.IsInfinity(xs[i]) ||
                    double.IsNaN(ys[i]) || double.IsInfinity(ys[i]))
                {
                    throw new InvalidOperationException("Benchmark validation failed: transform produced non-finite values.");
                }
            }
        }

        var benchmark = new ProjParityBenchmarks { PointCount = 4 };
        benchmark.GlobalSetup();

        benchmark.Wgs84ToWebMercatorBatched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Wgs84ToUtm32NBatched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Wgs84ToUtm31NBatched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Utm31NToWgs84Batched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Wgs84ToLambert93Batched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Lambert93ToWgs84Batched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.WebMercatorToWgs84Batched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Wgs84ToWebMercatorBatchedWithNoise();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);
    }

    /// <summary>
    /// Allocates and seeds deterministic coordinate buffers used by all throughput benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.longitudes = new double[this.PointCount];
        this.latitudes = new double[this.PointCount];
        this.xBuffer = new double[this.PointCount];
        this.yBuffer = new double[this.PointCount];
        this.noiseX = new double[this.PointCount];
        this.noiseY = new double[this.PointCount];

        var random = new Random(20260317);
        for (int i = 0; i < this.PointCount; i++)
        {
            this.longitudes[i] = -179d + (random.NextDouble() * 358d);
            this.latitudes[i] = -85d + (random.NextDouble() * 170d);
            this.noiseX[i] = (2d * random.NextDouble()) - 1d;
            this.noiseY[i] = (2d * random.NextDouble()) - 1d;
        }
    }

    /// <summary>
    /// Measures batched forward throughput for EPSG:4326 to EPSG:3857.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Wgs84ToWebMercatorBatched()
    {
        this.PrepareInput();
        Wgs84ToWebMercator.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures per-point forward throughput for EPSG:4326 to EPSG:3857.
    /// </summary>
    [Benchmark]
    public void Wgs84ToWebMercatorOneByOne()
    {
        this.PrepareInput();
        for (int i = 0; i < this.PointCount; i++)
        {
            Wgs84ToWebMercator.MathTransform.Transform(ref this.xBuffer[i], ref this.yBuffer[i]);
        }
    }

    /// <summary>
    /// Measures batched forward throughput for EPSG:4326 to EPSG:32632.
    /// </summary>
    [Benchmark]
    public void Wgs84ToUtm32NBatched()
    {
        this.PrepareInput();
        Wgs84ToUtm32N.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward throughput for EPSG:4326 to EPSG:32631.
    /// </summary>
    [Benchmark]
    public void Wgs84ToUtm31NBatched()
    {
        this.PrepareInput();
        Wgs84ToUtm31N.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched inverse throughput for EPSG:32631 to EPSG:4326.
    /// </summary>
    [Benchmark]
    public void Utm31NToWgs84Batched()
    {
        this.PrepareProjectedInput(Wgs84ToUtm31N);
        Utm31NToWgs84.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward throughput for EPSG:4326 to EPSG:2154.
    /// </summary>
    [Benchmark]
    public void Wgs84ToLambert93Batched()
    {
        this.PrepareInput();
        Wgs84ToLambert93.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched inverse throughput for EPSG:2154 to EPSG:4326.
    /// </summary>
    [Benchmark]
    public void Lambert93ToWgs84Batched()
    {
        this.PrepareProjectedInput(Wgs84ToLambert93);
        Lambert93ToWgs84.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures round-trip batched throughput via EPSG:3857 and back to EPSG:4326.
    /// </summary>
    [Benchmark]
    public void WebMercatorToWgs84Batched()
    {
        this.PrepareInput();
        Wgs84ToWebMercator.MathTransform.Transform(this.xBuffer, this.yBuffer);
        WebMercatorToWgs84.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures EPSG:4326 to EPSG:3857 throughput with deterministic coordinate perturbation.
    /// </summary>
    /// <remarks>
    /// The perturbation model follows the PROJ benchmark pattern:
    /// <c>value + noise * uniform(-1, 1)</c> for each axis.
    /// </remarks>
    [Benchmark]
    public void Wgs84ToWebMercatorBatchedWithNoise()
    {
        this.PrepareInput();
        this.ApplyNoise(this.xBuffer.AsSpan(), this.yBuffer.AsSpan(), NoiseXDegrees, NoiseYDegrees);
        Wgs84ToWebMercator.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    private void PrepareInput()
    {
        this.longitudes.CopyTo(this.xBuffer.AsSpan());
        this.latitudes.CopyTo(this.yBuffer.AsSpan());
    }

    private void PrepareProjectedInput(ICoordinateTransformation forwardTransform)
    {
        this.PrepareInput();
        forwardTransform.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    private void ApplyNoise(Span<double> xs, Span<double> ys, double noiseX, double noiseY)
    {
        for (int i = 0; i < this.PointCount; i++)
        {
            if (noiseX != 0d)
            {
                xs[i] += noiseX * this.noiseX[i];
            }

            if (noiseY != 0d)
            {
                ys[i] += noiseY * this.noiseY[i];
            }
        }
    }
}
