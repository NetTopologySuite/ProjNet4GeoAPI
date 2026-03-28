// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;

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
    public static long ManagedHeapIncreaseAfterFirstTransformationLookup()
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
