// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using BenchmarkDotNet.Running;

/// <summary>
/// Represents the documented type.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        PerformanceTests.Validate();
        ProjParityBenchmarks.Validate();
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
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
