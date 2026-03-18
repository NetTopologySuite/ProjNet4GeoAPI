// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.Benchmark;

using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {
        PerformanceTests.Validate();
        ProjParityBenchmarks.Validate();
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    // here's how I generated coords.dat.gz (set TestDataPath and add references + usings, of course):
#if false
    static void GenerateTestData()
    {
        const string TestDataPath = @"C:\Path\To\TestData";
        var lst = new List<Coordinate>();
        foreach (var fl in new[] { "africa.wkt", "europe.wkt", "world.wkt" })
        {
            var wkt = new WKTFileReader(Path.Combine(TestDataPath, fl), new WKTReader());
            lst.AddRange(wkt.Read().SelectMany(g => g.Coordinates));
        }

        using (var writer = new BinaryWriter(new GZipStream(File.Create(Path.Combine(TestDataPath, "coords.dat.gz")), CompressionLevel.Optimal)))
        {
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
    }
#endif
}
