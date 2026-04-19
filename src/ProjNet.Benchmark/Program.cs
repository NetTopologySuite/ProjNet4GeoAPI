// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Running;

/// <summary>
/// Entry point for benchmark validation and BenchmarkDotNet execution.
/// </summary>
/// <remarks>
/// The startup sequence first runs deterministic sanity validation, then delegates to BenchmarkDotNet
/// for full benchmark execution and reporting.
/// </remarks>
internal static class Program
{
    private static void Main(string[] args)
    {
        bool curatedValidation = ContainsArgument(args, "--curated");
        string[] benchmarkArguments = RemoveArgument(args, "--curated");

        if (ContainsArgument(args, "--validate"))
        {
            if (curatedValidation)
            {
                ValidateCuratedBenchmarks();
            }
            else
            {
                ValidateBenchmarks();
            }

            return;
        }

        if (!IsBenchmarkChildProcess(args))
        {
            if (curatedValidation)
            {
                ValidateCuratedBenchmarks();
            }
            else
            {
                ValidateBenchmarks();
            }
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(benchmarkArguments);
    }

    private static void ValidateCuratedBenchmarks()
    {
        CatalogFirstTransformationLookupBenchmarks.Validate();
        WktParsingBenchmarks.Validate();
        ProjectionTransformBenchmarks.Validate();
        ProjParityBenchmarks.Validate();
        TransformationFactoryBenchmarks.Validate();
    }

    private static void ValidateBenchmarks()
    {
        PerformanceTests.Validate();
        CatalogFirstTransformationLookupBenchmarks.Validate();
        WktParsingBenchmarks.Validate();
        ProjectionTransformBenchmarks.Validate();
        ProjParityBenchmarks.Validate();
        TransformationFactoryBenchmarks.Validate();
        ProjectionSinglePointBenchmarks.Validate();
    }

    private static bool IsBenchmarkChildProcess(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--benchmarkName", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] RemoveArgument(string[] args, string argument)
    {
        var filteredArguments = new List<string>(args.Length);
        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], argument, StringComparison.Ordinal))
            {
                filteredArguments.Add(args[i]);
            }
        }

        return filteredArguments.ToArray();
    }

    private static bool ContainsArgument(string[] args, string argument)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], argument, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // here's how I generated coords.dat.gz (set TestDataPath and add references + usings, of course):
#if false
    private static void GenerateTestData()
    {
        const string TestDataPath = @"C:\Path\To\TestData";
        var lst = new List<Coordinate>();
        foreach (var fl in new[] { "africa.wkt", "europe.wkt", "world.wkt" })
        {
            var wkt = new WKTFileReader(Path.Combine(TestDataPath, fl), new WKTReader());
            lst.AddRange(wkt.Read().SelectMany(g => g.Coordinates));
        }

        using var writer = new BinaryWriter(new GZipStream(File.Create(Path.Combine(TestDataPath, "coords.dat.gz")), CompressionLevel.Optimal));
        writer.Write(lst.Count);
        foreach (var coord in lst)
        {
            writer.Write(coord.X);
        }

        foreach (var coord in lst)
        {
            writer.Write(coord.Y);
        }
    }
#endif
}
