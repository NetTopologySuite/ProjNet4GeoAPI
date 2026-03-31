// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures forward transform throughput for common projection types across 10,000 coordinate pairs.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Benchmark input generation uses deterministic pseudo-random data for repeatability and is not security-sensitive.")]
[MemoryDiagnoser]
public class ProjectionTransformBenchmarks
{
    private const int PointCount = 10_000;

    private MathTransform mercatorTransform = null!;
    private MathTransform utm32NTransform = null!;
    private MathTransform lambert93Transform = null!;
    private MathTransform krovakTransform = null!;

    private double[] longitudes = [];
    private double[] latitudes = [];
    private double[] xBuffer = [];
    private double[] yBuffer = [];

    /// <summary>
    /// Creates coordinate system services, pre-builds transforms, and generates deterministic test coordinates.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new CoordinateSystemServices();

        ICoordinateTransformation mercator = services.CreateTransformation(4326, 3857)
            ?? throw new InvalidOperationException("EPSG:4326->3857 transformation lookup returned null.");
        ICoordinateTransformation utm32N = services.CreateTransformation(4326, 32632)
            ?? throw new InvalidOperationException("EPSG:4326->32632 transformation lookup returned null.");
        ICoordinateTransformation lambert93 = services.CreateTransformation(4326, 2154)
            ?? throw new InvalidOperationException("EPSG:4326->2154 transformation lookup returned null.");
        ICoordinateTransformation krovak = services.CreateTransformation(4326, 5514)
            ?? throw new InvalidOperationException("EPSG:4326->5514 transformation lookup returned null.");

        this.mercatorTransform = mercator.MathTransform;
        this.utm32NTransform = utm32N.MathTransform;
        this.lambert93Transform = lambert93.MathTransform;
        this.krovakTransform = krovak.MathTransform;

        this.longitudes = new double[PointCount];
        this.latitudes = new double[PointCount];
        this.xBuffer = new double[PointCount];
        this.yBuffer = new double[PointCount];

        var random = new Random(42);
        for (int i = 0; i < PointCount; i++)
        {
            this.longitudes[i] = 2d + (random.NextDouble() * 18d);
            this.latitudes[i] = 43d + (random.NextDouble() * 12d);
        }
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:3857 (Web Mercator).
    /// </summary>
    [Benchmark(Baseline = true)]
    public void TransformBatchMercator()
    {
        this.PrepareInput();
        this.mercatorTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:32632 (UTM Zone 32N).
    /// </summary>
    [Benchmark]
    public void TransformBatchUtm32N()
    {
        this.PrepareInput();
        this.utm32NTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:2154 (Lambert 93).
    /// </summary>
    [Benchmark]
    public void TransformBatchLambert93()
    {
        this.PrepareInput();
        this.lambert93Transform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:5514 (Krovak).
    /// </summary>
    [Benchmark]
    public void TransformBatchKrovak()
    {
        this.PrepareInput();
        this.krovakTransform.Transform(this.xBuffer, this.yBuffer);
    }

    private void PrepareInput()
    {
        this.longitudes.CopyTo(this.xBuffer.AsSpan());
        this.latitudes.CopyTo(this.yBuffer.AsSpan());
    }
}
