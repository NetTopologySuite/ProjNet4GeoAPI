// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;

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
        ICoordinateTransformation? transformation = services.CreateTransformation(4326, 3857);
        return transformation is null
            ? throw new InvalidOperationException("EPSG:4326->3857 transformation lookup returned null.")
            : transformation;
    }
}
