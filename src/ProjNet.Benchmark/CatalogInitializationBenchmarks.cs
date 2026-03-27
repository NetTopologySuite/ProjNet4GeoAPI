// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures cold-start latency for the first EPSG coordinate system lookup from the managed catalog.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for explicit invocation from Program and benchmark tooling stability.")]
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 12, warmupCount: 0, iterationCount: 1)]
public class CatalogFirstCoordinateLookupBenchmarks
{
    /// <summary>
    /// Resolves EPSG:4326 from a fresh <see cref="CoordinateSystemServices"/> instance.
    /// </summary>
    /// <returns>The resolved coordinate system.</returns>
    [Benchmark(Baseline = true)]
    public CoordinateSystem FirstGetCoordinateSystem4326()
    {
        var services = new CoordinateSystemServices();
        CoordinateSystem? coordinateSystem = services.GetCoordinateSystem(4326);
        if (coordinateSystem is null)
        {
            throw new InvalidOperationException("EPSG:4326 lookup returned null.");
        }

        return coordinateSystem;
    }
}

/// <summary>
/// Measures cold-start latency for first-time EPSG operation-resolution paths that can touch operation catalogs.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for explicit invocation from Program and benchmark tooling stability.")]
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 12, warmupCount: 0, iterationCount: 1)]
public class CatalogFirstTransformationLookupBenchmarks
{
    /// <summary>
    /// Creates an EPSG:4326 to EPSG:3857 transformation from a fresh <see cref="CoordinateSystemServices"/> instance.
    /// </summary>
    /// <returns>The resolved transformation.</returns>
    [Benchmark(Baseline = true)]
    public ICoordinateTransformation FirstCreateTransformation4326To3857()
    {
        var services = new CoordinateSystemServices();
        return services.CreateTransformation(4326, 3857);
    }
}

/// <summary>
/// Estimates retained managed heap growth after first-time transformation lookup and catalog initialization.
/// </summary>
/// <remarks>
/// This benchmark complements MemoryDiagnoser allocation metrics with a coarse retained-heap delta.
/// </remarks>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for explicit invocation from Program and benchmark tooling stability.")]
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 12, warmupCount: 0, iterationCount: 1)]
public class CatalogRetainedMemoryBenchmarks
{
    /// <summary>
    /// Computes managed heap growth after creating an EPSG:4326 to EPSG:3857 transformation.
    /// </summary>
    /// <returns>Estimated retained managed bytes after first lookup.</returns>
    [Benchmark(Baseline = true)]
    public long ManagedHeapIncreaseAfterFirstTransformationLookup()
    {
        ForceFullCollection();
        long before = GC.GetTotalMemory(forceFullCollection: true);

        var services = new CoordinateSystemServices();
        ICoordinateTransformation transformation = services.CreateTransformation(4326, 3857);

        ForceFullCollection();
        long after = GC.GetTotalMemory(forceFullCollection: true);

        GC.KeepAlive(services);
        GC.KeepAlive(transformation);
        return Math.Max(0L, after - before);
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
