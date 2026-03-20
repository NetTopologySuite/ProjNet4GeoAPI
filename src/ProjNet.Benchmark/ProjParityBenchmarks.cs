// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.Benchmark;

using System;
using BenchmarkDotNet.Attributes;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Represents the documented type.
/// </summary>
[MemoryDiagnoser]
public class ProjParityBenchmarks
{
    private static readonly CoordinateSystemServices CoordinateSystemServices = new CoordinateSystemServices();

    private static readonly ICoordinateTransformation Wgs84ToWebMercator =
        CoordinateSystemServices.CreateTransformation(4326, 3857);

    private static readonly ICoordinateTransformation Wgs84ToUtm32N =
        CoordinateSystemServices.CreateTransformation(4326, 32632);

    private static readonly ICoordinateTransformation WebMercatorToWgs84 =
        CoordinateSystemServices.CreateTransformation(3857, 4326);

    private double[] longitudes;
    private double[] latitudes;
    private double[] xBuffer;
    private double[] yBuffer;

    /// <summary>
    /// Gets the documented value.
    /// </summary>
    [Params(10000)]
    public int PointCount { get; set; }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    public static void Validate()
    {
        var benchmark = new ProjParityBenchmarks { PointCount = 4 };
        benchmark.GlobalSetup();

        benchmark.Wgs84ToWebMercatorBatched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.Wgs84ToUtm32NBatched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);

        benchmark.WebMercatorToWgs84Batched();
        EnsureFinite(benchmark.xBuffer, benchmark.yBuffer);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.longitudes = new double[this.PointCount];
        this.latitudes = new double[this.PointCount];
        this.xBuffer = new double[this.PointCount];
        this.yBuffer = new double[this.PointCount];

        var random = new Random(20260317);
        for (int i = 0; i < this.PointCount; i++)
        {
            this.longitudes[i] = -179d + (random.NextDouble() * 358d);
            this.latitudes[i] = -85d + (random.NextDouble() * 170d);
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Wgs84ToWebMercatorBatched()
    {
        this.PrepareInput();
        Wgs84ToWebMercator.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Performs the documented operation.
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
    /// Performs the documented operation.
    /// </summary>
    [Benchmark]
    public void Wgs84ToUtm32NBatched()
    {
        this.PrepareInput();
        Wgs84ToUtm32N.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Benchmark]
    public void WebMercatorToWgs84Batched()
    {
        this.PrepareInput();
        Wgs84ToWebMercator.MathTransform.Transform(this.xBuffer, this.yBuffer);
        WebMercatorToWgs84.MathTransform.Transform(this.xBuffer, this.yBuffer);
    }

    private void PrepareInput()
    {
        this.longitudes.CopyTo(this.xBuffer.AsSpan());
        this.latitudes.CopyTo(this.yBuffer.AsSpan());
    }

    private static void EnsureFinite(double[] xs, double[] ys)
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
}
