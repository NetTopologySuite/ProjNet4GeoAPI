using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

using BenchmarkDotNet.Attributes;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;

namespace ProjNet.Benchmark
{
    public class PerformanceTests
    {
        private static readonly MathTransform WGS84ToWebMercator = new CoordinateTransformationFactory().CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator).MathTransform;

        private int _cnt;

        private double[] _xs;

        private double[] _ys;

        private XY[] _xys;

        private XYZ[] _xyzs;

        private double[] _xsCopy;

        private double[] _ysCopy;

        private XY[] _xysCopy;

        private XYZ[] _xyzsCopy;

        public static void Validate()
        {
            var instance = new PerformanceTests();
            instance.GlobalSetup();

            instance.SoAOneByOne();
            var firstOutput = instance._xsCopy.Zip(instance._ysCopy, (x, y) => (x, y)).ToArray();

            for (int i = 0; i < firstOutput.Length; i++)
            {
                if (firstOutput[i].Equals((instance._xys[i].X, instance._xys[i].Y)))
                {
                    throw new Exception("Validation failure: transformer isn't actually transforming.");
                }
            }

            instance.SoABatched();
            Validate(instance._xsCopy.Zip(instance._ysCopy, (x, y) => (x, y)).ToArray());

            instance.TightAoSOneByOne();
            Validate(Array.ConvertAll(instance._xysCopy, xy => (xy.X, xy.Y)));

            instance.TightAoSBatched();
            Validate(Array.ConvertAll(instance._xysCopy, xy => (xy.X, xy.Y)));

            instance.LooserAoSOneByOne();
            Validate(Array.ConvertAll(instance._xyzsCopy, xyz => (xyz.X, xyz.Y)));

            instance.LooserAoSBatched();
            Validate(Array.ConvertAll(instance._xyzsCopy, xyz => (xyz.X, xyz.Y)));

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
                _cnt = reader.ReadInt32();

                _xs = new double[_cnt];
                _ys = new double[_cnt];
                _xys = new XY[_cnt];
                _xyzs = new XYZ[_cnt];

                for (int i = 0; i < _cnt; i++)
                {
                    _xs[i] = _xys[i].X = _xyzs[i].X = reader.ReadDouble();
                }

                for (int i = 0; i < _cnt; i++)
                {
                    _ys[i] = _xys[i].Y = _xyzs[i].Y = reader.ReadDouble();
                }
            }

            // transforms happen in-place, so at the start of every iteration, we copy the source
            // coordinate data to these throwaway arrays in order to be able to repeat the test
            // without allocating anything.  this slightly hurts accuracy, but the effect appears to
            // be less than 5% of the total test's time, and [IterationSetup] / [IterationCleanup]
            // aren't designed for the kinds of benchmarks we're running here.
            _xsCopy = new double[_cnt];
            _ysCopy = new double[_cnt];
            _xysCopy = new XY[_cnt];
            _xyzsCopy = new XYZ[_cnt];
        }

        [Benchmark]
        public void SoAOneByOne()
        {
            _xs.CopyTo(_xsCopy.AsSpan());
            _ys.CopyTo(_ysCopy.AsSpan());
            for (int i = 0; i < _cnt; i++)
            {
                WGS84ToWebMercator.Transform(ref _xsCopy[i], ref _ysCopy[i]);
            }
        }

        [Benchmark]
        public void SoABatched()
        {
            _xs.CopyTo(_xsCopy.AsSpan());
            _ys.CopyTo(_ysCopy.AsSpan());
            WGS84ToWebMercator.Transform(_xsCopy, _ysCopy);
        }

        [Benchmark]
        public void TightAoSOneByOne()
        {
            _xys.CopyTo(_xysCopy.AsSpan());
            for (int i = 0; i < _cnt; i++)
            {
                WGS84ToWebMercator.Transform(ref _xysCopy[i].X, ref _xysCopy[i].Y);
            }
        }

        [Benchmark]
        public void TightAoSBatched()
        {
            _xys.CopyTo(_xysCopy.AsSpan());
            WGS84ToWebMercator.Transform(_xysCopy);
        }

        [Benchmark]
        public void LooserAoSOneByOne()
        {
            _xyzs.CopyTo(_xyzsCopy.AsSpan());
            for (int i = 0; i < _cnt; i++)
            {
                WGS84ToWebMercator.Transform(ref _xyzsCopy[i].X, ref _xyzsCopy[i].Y);
            }
        }

        [Benchmark]
        public void LooserAoSBatched()
        {
            _xyzs.CopyTo(_xyzsCopy.AsSpan());
            WGS84ToWebMercator.Transform(_xyzsCopy);
        }
    }
}
