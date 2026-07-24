// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems;

/// <summary>
/// Measures base-typed metadata cloning for representative <see cref="Info"/> model types.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
[SimpleJob]
public class InfoCloneBenchmarks
{
    private Info genericUnit = null!;
    private Info projectedCoordinateSystem = null!;

    /// <summary>
    /// Captures representative info-backed model objects once per run.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.projectedCoordinateSystem = ProjectedCoordinateSystem.WebMercator;
        this.genericUnit = new Unit("unity", 1d);
    }

    /// <summary>
    /// Measures authority cloning for a base-typed projected coordinate system.
    /// </summary>
    /// <returns>A cloned projected coordinate system.</returns>
    [Benchmark(Baseline = true)]
    public Info CloneProjectedCoordinateSystemAuthority()
    {
        return this.projectedCoordinateSystem.WithAuthority("TEST", 5001);
    }

    /// <summary>
    /// Measures name cloning for a base-typed projected coordinate system.
    /// </summary>
    /// <returns>A cloned projected coordinate system.</returns>
    [Benchmark]
    public Info CloneProjectedCoordinateSystemName()
    {
        return this.projectedCoordinateSystem.WithName("Projected clone");
    }

    /// <summary>
    /// Measures authority cloning for a base-typed generic unit.
    /// </summary>
    /// <returns>A cloned generic unit.</returns>
    [Benchmark]
    public Info CloneGenericUnitAuthority()
    {
        return this.genericUnit.WithAuthority("TEST", 6001);
    }

    /// <summary>
    /// Measures name cloning for a base-typed generic unit.
    /// </summary>
    /// <returns>A cloned generic unit.</returns>
    [Benchmark]
    public Info CloneGenericUnitName()
    {
        return this.genericUnit.WithName("custom unity");
    }
}
