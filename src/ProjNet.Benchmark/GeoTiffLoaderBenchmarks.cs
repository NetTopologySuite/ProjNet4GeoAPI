// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Measures GeoTIFF grid-loader throughput for representative fixtures that exercise GDAL metadata parsing.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
[SimpleJob]
public class GeoTiffLoaderBenchmarks
{
    private string horizontalGridPath = string.Empty;
    private string verticalGridPath = string.Empty;

    /// <summary>
    /// Resolves representative GeoTIFF fixtures once per benchmark run.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        this.horizontalGridPath = BenchmarkFixtureResolver.ResolveGridPath("test_hgrid.tif");
        this.verticalGridPath = BenchmarkFixtureResolver.ResolveGridPath("test_vgrid_uint16_with_scale_offset.tif");
    }

    /// <summary>
    /// Measures loading a representative horizontal GeoTIFF grid with metadata-defined interpolation settings.
    /// </summary>
    /// <returns>The number of loaded grid pages.</returns>
    [Benchmark(Baseline = true)]
    public int LoadHorizontalGrid()
    {
        return GeoTiffGridLoader.LoadHorizontal(this.horizontalGridPath).Count;
    }

    /// <summary>
    /// Measures loading a representative vertical GeoTIFF grid with scale/offset metadata.
    /// </summary>
    /// <returns>The number of loaded grid pages.</returns>
    [Benchmark]
    public int LoadVerticalGridWithScaleOffset()
    {
        return GeoTiffGridLoader.LoadVertical(this.verticalGridPath).Count;
    }
}
