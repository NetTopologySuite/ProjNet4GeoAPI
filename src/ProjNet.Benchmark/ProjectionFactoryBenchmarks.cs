// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures projection creation overhead through <see cref="ProjectionsRegistry.CreateProjection(string, IEnumerable{ProjectionParameter})"/>.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
public class ProjectionFactoryBenchmarks
{
    private ProjectionParameter[] mercatorParameters = null!;
    private ProjectionParameter[] transverseMercatorParameters = null!;
    private ProjectionParameter[] schParameters = null!;

    /// <summary>
    /// Prepares the projection parameter sets used by the factory benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.mercatorParameters =
        [
            new ProjectionParameter("semi_major", 6378137d),
            new ProjectionParameter("semi_minor", 6356752.314245179d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("unit", 1d),
        ];

        this.transverseMercatorParameters =
        [
            new ProjectionParameter("semi_major", 6378137d),
            new ProjectionParameter("semi_minor", 6356752.314245179d),
            new ProjectionParameter("central_meridian", 9d),
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("scale_factor", 0.9996d),
            new ProjectionParameter("false_easting", 500000d),
            new ProjectionParameter("false_northing", 0d),
            new ProjectionParameter("unit", 1d),
        ];

        this.schParameters =
        [
            new ProjectionParameter("semi_major", 6378137d),
            new ProjectionParameter("semi_minor", 6356752.314245179d),
            new ProjectionParameter("plat_0", 30d),
            new ProjectionParameter("plon_0", 45d),
            new ProjectionParameter("phdg_0", -12d),
        ];
    }

    /// <summary>
    /// Creates a Mercator projection via the registry.
    /// </summary>
    /// <returns>The created projection transform.</returns>
    [Benchmark(Baseline = true)]
    public MathTransform CreateMercatorProjection()
    {
        return ProjectionsRegistry.CreateProjection("mercator", this.mercatorParameters);
    }

    /// <summary>
    /// Creates a Transverse Mercator projection via the registry.
    /// </summary>
    /// <returns>The created projection transform.</returns>
    [Benchmark]
    public MathTransform CreateTransverseMercatorProjection()
    {
        return ProjectionsRegistry.CreateProjection("Transverse_Mercator", this.transverseMercatorParameters);
    }

    /// <summary>
    /// Creates an SCH transform via the registry, exercising the list-backed special-case constructor.
    /// </summary>
    /// <returns>The created SCH transform.</returns>
    [Benchmark]
    public MathTransform CreateSchProjection()
    {
        return ProjectionsRegistry.CreateProjection("sch", this.schParameters);
    }
}
