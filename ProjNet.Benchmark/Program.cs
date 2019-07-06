using BenchmarkDotNet.Running;

namespace ProjNet.Benchmark
{
    class Program
    {
        static void Main()
        {
            PerformanceTests.Validate();
            BenchmarkRunner.Run<PerformanceTests>();
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
}
