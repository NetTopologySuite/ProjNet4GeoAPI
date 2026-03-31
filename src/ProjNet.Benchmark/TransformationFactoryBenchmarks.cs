// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures the overhead of <see cref="CoordinateTransformationFactory.CreateFromCoordinateSystems(CoordinateSystem, CoordinateSystem)"/>.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
public class TransformationFactoryBenchmarks
{
    private CoordinateSystem wgs84Cs = null!;
    private CoordinateSystem mercatorCs = null!;
    private CoordinateSystem utm32NCs = null!;
    private CoordinateSystem lambert93Cs = null!;

    /// <summary>
    /// Resolves and caches coordinate systems for subsequent factory benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new CoordinateSystemServices();

        this.wgs84Cs = services.GetCoordinateSystem(4326)
            ?? throw new InvalidOperationException("EPSG:4326 lookup returned null.");
        this.mercatorCs = services.GetCoordinateSystem(3857)
            ?? throw new InvalidOperationException("EPSG:3857 lookup returned null.");
        this.utm32NCs = services.GetCoordinateSystem(32632)
            ?? throw new InvalidOperationException("EPSG:32632 lookup returned null.");
        this.lambert93Cs = services.GetCoordinateSystem(2154)
            ?? throw new InvalidOperationException("EPSG:2154 lookup returned null.");
    }

    /// <summary>
    /// Creates a WGS84 to Web Mercator transformation via <see cref="CoordinateTransformationFactory"/>.
    /// </summary>
    /// <returns>The created coordinate transformation.</returns>
    [Benchmark(Baseline = true)]
    public ICoordinateTransformation CreateTransformWgs84ToMercator()
    {
        return new CoordinateTransformationFactory().CreateFromCoordinateSystems(this.wgs84Cs, this.mercatorCs);
    }

    /// <summary>
    /// Creates a WGS84 to UTM Zone 32N transformation via <see cref="CoordinateTransformationFactory"/>.
    /// </summary>
    /// <returns>The created coordinate transformation.</returns>
    [Benchmark]
    public ICoordinateTransformation CreateTransformWgs84ToUtm32N()
    {
        return new CoordinateTransformationFactory().CreateFromCoordinateSystems(this.wgs84Cs, this.utm32NCs);
    }

    /// <summary>
    /// Creates a UTM Zone 32N to Lambert 93 transformation via <see cref="CoordinateTransformationFactory"/>.
    /// </summary>
    /// <returns>The created coordinate transformation.</returns>
    [Benchmark]
    public ICoordinateTransformation CreateTransformUtm32NToLambert93()
    {
        return new CoordinateTransformationFactory().CreateFromCoordinateSystems(this.utm32NCs, this.lambert93Cs);
    }
}
