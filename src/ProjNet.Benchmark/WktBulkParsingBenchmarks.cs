// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Attributes;
using ProjNet.Data;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;

/// <summary>
/// Measures bulk WKT parsing throughput across the full managed EPSG catalog.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
[SimpleJob]
public class WktBulkParsingBenchmarks
{
    private string[] catalogWkt1 = Array.Empty<string>();
    private string[] catalogWkt2 = Array.Empty<string>();

    /// <summary>
    /// Materializes the managed EPSG catalog as WKT1 and WKT2 string arrays once per benchmark run.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        CoordinateSystemEntry[] entries = new ManagedCoordinateSystemDefinitionProvider()
            .GetCoordinateSystems()
            .OrderBy(static entry => entry.Srid)
            .ToArray();

        this.catalogWkt1 = entries
            .Select(static entry => entry.CoordinateSystem.WKT)
            .ToArray();

        this.catalogWkt2 = entries
            .Select(static entry => entry.CoordinateSystem.ToWktNode(WktVersion.Wkt22019).ToString())
            .ToArray();
    }

    /// <summary>
    /// Parses all managed EPSG catalog entries in WKT1 form.
    /// </summary>
    /// <returns>A checksum derived from the parsed entries.</returns>
    [Benchmark(Baseline = true)]
    public int ParseAllCatalogWkt1()
    {
        return ParseAll(this.catalogWkt1);
    }

    /// <summary>
    /// Parses all managed EPSG catalog entries in WKT2:2019 form.
    /// </summary>
    /// <returns>A checksum derived from the parsed entries.</returns>
    [Benchmark]
    public int ParseAllCatalogWkt2()
    {
        return ParseAll(this.catalogWkt2);
    }

    private static int ParseAll(string[] wkts)
    {
        int checksum = 0;
        foreach (string wkt in wkts)
        {
            checksum += CoordinateSystemWktReader.Parse(wkt).Name.Length;
        }

        return checksum;
    }
}
