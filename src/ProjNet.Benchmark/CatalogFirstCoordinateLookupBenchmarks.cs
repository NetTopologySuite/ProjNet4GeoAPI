// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using ProjNet;
using ProjNet.CoordinateSystems;

/// <summary>
/// Measures cold-start latency for the first EPSG coordinate system lookup from the managed catalog.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for explicit invocation from Program and benchmark tooling stability.")]
[SuppressMessage("Design", "CA1052:Static holder types should be static", Justification = "BenchmarkDotNet requires a non-static benchmark class type for discovery.")]
[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 12, warmupCount: 0, iterationCount: 1)]
public class CatalogFirstCoordinateLookupBenchmarks
{
    /// <summary>
    /// Resolves EPSG:4326 from a fresh <see cref="CoordinateSystemServices"/> instance.
    /// </summary>
    /// <returns>The resolved coordinate system.</returns>
    [Benchmark(Baseline = true)]
    public static CoordinateSystem FirstGetCoordinateSystem4326()
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
