namespace ProjNet.Benchmark
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;

    using BenchmarkDotNet.Attributes;

    using ProjNet.CoordinateSystems;
    using ProjNet.CoordinateSystems.Transformations;
    using ProjNet.Geometries;

    public class PerformanceTests
    {
        private static readonly MathTransform WGS84ToWebMercator = new CoordinateTransformationFactory().CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator).MathTransform;

        private int cnt;

        private double[] xs;

        private double[] ys;

        private XY[] xys;

        private XYZ[] xyzs;

        private double[] xsCopy;

        private double[] ysCopy;

        private XY[] xysCopy;

        private XYZ[] xyzsCopy;

        public static void Validate()
        {
            var instance = new PerformanceTests();
            instance.GlobalSetup();

            instance.SoAOneByOne();
            var firstOutput = instance.xsCopy.Zip(instance.ysCopy, (x, y) => (x, y)).ToArray();

            for (int i = 0; i < firstOutput.Length; i++)
            {
                if (firstOutput[i].Equals((instance.xys[i].X, instance.xys[i].Y)))
                {
                    throw new Exception("Validation failure: transformer isn't actually transforming.");
                }
            }

            instance.SoABatched();
            Validate(instance.xsCopy.Zip(instance.ysCopy, (x, y) => (x, y)).ToArray());

            instance.TightAoSOneByOne();
            Validate(Array.ConvertAll(instance.xysCopy, xy => (xy.X, xy.Y)));

            instance.TightAoSBatched();
            Validate(Array.ConvertAll(instance.xysCopy, xy => (xy.X, xy.Y)));

            instance.LooserAoSOneByOne();
            Validate(Array.ConvertAll(instance.xyzsCopy, xyz => (xyz.X, xyz.Y)));

            instance.LooserAoSBatched();
            Validate(Array.ConvertAll(instance.xyzsCopy, xyz => (xyz.X, xyz.Y)));

            void Validate(ReadOnlySpan<(double x, double y)> nextOutput)
            {
                if (!nextOutput.SequenceEqual(firstOutput))
                {
                    throw new Exception("Validation failure: some transform method is giving different results than another.");
                }
            }
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            string currentFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fullPathToData = Path.Combine(currentFolderPath, "coords.dat.gz");
            using (var reader = new BinaryReader(new GZipStream(File.OpenRead(fullPathToData), CompressionMode.Decompress)))
            {
                this.cnt = reader.ReadInt32();

                this.xs = new double[this.cnt];
                this.ys = new double[this.cnt];
                this.xys = new XY[this.cnt];
                this.xyzs = new XYZ[this.cnt];

                for (int i = 0; i < this.cnt; i++)
                {
                    this.xs[i] = this.xys[i].X = this.xyzs[i].X = reader.ReadDouble();
                }

                for (int i = 0; i < this.cnt; i++)
                {
                    this.ys[i] = this.xys[i].Y = this.xyzs[i].Y = reader.ReadDouble();
                }
            }

            // transforms happen in-place, so at the start of every iteration, we copy the source
            // coordinate data to these throwaway arrays in order to be able to repeat the test
            // without allocating anything.  this slightly hurts accuracy, but the effect appears to
            // be less than 5% of the total test's time, and [IterationSetup] / [IterationCleanup]
            // aren't designed for the kinds of benchmarks we're running here.
            this.xsCopy = new double[this.cnt];
            this.ysCopy = new double[this.cnt];
            this.xysCopy = new XY[this.cnt];
            this.xyzsCopy = new XYZ[this.cnt];
        }

        [Benchmark]
        public void SoAOneByOne()
        {
            this.xs.CopyTo(this.xsCopy.AsSpan());
            this.ys.CopyTo(this.ysCopy.AsSpan());
            for (int i = 0; i < this.cnt; i++)
            {
                WGS84ToWebMercator.Transform(ref this.xsCopy[i], ref this.ysCopy[i]);
            }
        }

        [Benchmark]
        public void SoABatched()
        {
            this.xs.CopyTo(this.xsCopy.AsSpan());
            this.ys.CopyTo(this.ysCopy.AsSpan());
            WGS84ToWebMercator.Transform(this.xsCopy, this.ysCopy);
        }

        [Benchmark]
        public void TightAoSOneByOne()
        {
            this.xys.CopyTo(this.xysCopy.AsSpan());
            for (int i = 0; i < this.cnt; i++)
            {
                WGS84ToWebMercator.Transform(ref this.xysCopy[i].X, ref this.xysCopy[i].Y);
            }
        }

        [Benchmark]
        public void TightAoSBatched()
        {
            this.xys.CopyTo(this.xysCopy.AsSpan());
            WGS84ToWebMercator.Transform(this.xysCopy);
        }

        [Benchmark]
        public void LooserAoSOneByOne()
        {
            this.xyzs.CopyTo(this.xyzsCopy.AsSpan());
            for (int i = 0; i < this.cnt; i++)
            {
                WGS84ToWebMercator.Transform(ref this.xyzsCopy[i].X, ref this.xyzsCopy[i].Y);
            }
        }

        [Benchmark]
        public void LooserAoSBatched()
        {
            this.xyzs.CopyTo(this.xyzsCopy.AsSpan());
            WGS84ToWebMercator.Transform(this.xyzsCopy);
        }
    }
}
