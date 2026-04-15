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

    private CoordinateSystemServices services = null!;
    private MathTransform? mercatorTransform;
    private MathTransform? utm32NTransform;
    private MathTransform? lambert93Transform;
    private MathTransform? krovakTransform;

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
        this.services = new CoordinateSystemServices();

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
        this.GetOrCreateTransform(ref this.mercatorTransform, 4326, 3857).Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:32632 (UTM Zone 32N).
    /// </summary>
    [Benchmark]
    public void TransformBatchUtm32N()
    {
        this.PrepareInput();
        this.GetOrCreateTransform(ref this.utm32NTransform, 4326, 32632).Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:2154 (Lambert 93).
    /// </summary>
    [Benchmark]
    public void TransformBatchLambert93()
    {
        this.PrepareInput();
        this.GetOrCreateTransform(ref this.lambert93Transform, 4326, 2154).Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Measures batched forward transform throughput for EPSG:4326 to EPSG:5514 (Krovak).
    /// </summary>
    [Benchmark]
    public void TransformBatchKrovak()
    {
        this.PrepareInput();
        this.GetOrCreateTransform(ref this.krovakTransform, 4326, 5514).Transform(this.xBuffer, this.yBuffer);
    }

    private void PrepareInput()
    {
        this.longitudes.CopyTo(this.xBuffer.AsSpan());
        this.latitudes.CopyTo(this.yBuffer.AsSpan());
    }

    private MathTransform GetOrCreateTransform(ref MathTransform? transform, int sourceSrid, int targetSrid)
    {
        if (transform is not null)
        {
            return transform;
        }

        ICoordinateTransformation projection = this.services.CreateTransformation(sourceSrid, targetSrid)
            ?? throw new InvalidOperationException(FormattableString.Invariant($"EPSG:{sourceSrid}->{targetSrid} transformation lookup returned null."));
        transform = projection.MathTransform;
        return transform;
    }
}
