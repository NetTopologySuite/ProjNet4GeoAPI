using System;
using BenchmarkDotNet.Attributes;
using ProjNet;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.Benchmark
{
    [MemoryDiagnoser]
    public class ProjParityBenchmarks
    {
        private static readonly CoordinateSystemServices CoordinateSystemServices = new CoordinateSystemServices();

        private static readonly ICoordinateTransformation Wgs84ToWebMercator =
            CoordinateSystemServices.CreateTransformation(4326, 3857);

        private static readonly ICoordinateTransformation Wgs84ToUtm32N =
            CoordinateSystemServices.CreateTransformation(4326, 32632);

        private static readonly ICoordinateTransformation WebMercatorToWgs84 =
            CoordinateSystemServices.CreateTransformation(3857, 4326);

        [Params(10000)]
        public int PointCount { get; set; }

        private double[] _longitudes;
        private double[] _latitudes;
        private double[] _xBuffer;
        private double[] _yBuffer;

        public static void Validate()
        {
            var benchmark = new ProjParityBenchmarks { PointCount = 4 };
            benchmark.GlobalSetup();

            benchmark.Wgs84ToWebMercatorBatched();
            EnsureFinite(benchmark._xBuffer, benchmark._yBuffer);

            benchmark.Wgs84ToUtm32NBatched();
            EnsureFinite(benchmark._xBuffer, benchmark._yBuffer);

            benchmark.WebMercatorToWgs84Batched();
            EnsureFinite(benchmark._xBuffer, benchmark._yBuffer);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _longitudes = new double[PointCount];
            _latitudes = new double[PointCount];
            _xBuffer = new double[PointCount];
            _yBuffer = new double[PointCount];

            var random = new Random(20260317);
            for (int i = 0; i < PointCount; i++)
            {
                _longitudes[i] = -179d + (random.NextDouble() * 358d);
                _latitudes[i] = -85d + (random.NextDouble() * 170d);
            }
        }

        [Benchmark(Baseline = true)]
        public void Wgs84ToWebMercatorBatched()
        {
            PrepareInput();
            Wgs84ToWebMercator.MathTransform.Transform(_xBuffer, _yBuffer);
        }

        [Benchmark]
        public void Wgs84ToWebMercatorOneByOne()
        {
            PrepareInput();
            for (int i = 0; i < PointCount; i++)
            {
                Wgs84ToWebMercator.MathTransform.Transform(ref _xBuffer[i], ref _yBuffer[i]);
            }
        }

        [Benchmark]
        public void Wgs84ToUtm32NBatched()
        {
            PrepareInput();
            Wgs84ToUtm32N.MathTransform.Transform(_xBuffer, _yBuffer);
        }

        [Benchmark]
        public void WebMercatorToWgs84Batched()
        {
            PrepareInput();
            Wgs84ToWebMercator.MathTransform.Transform(_xBuffer, _yBuffer);
            WebMercatorToWgs84.MathTransform.Transform(_xBuffer, _yBuffer);
        }

        private void PrepareInput()
        {
            _longitudes.CopyTo(_xBuffer.AsSpan());
            _latitudes.CopyTo(_yBuffer.AsSpan());
        }

        private static void EnsureFinite(double[] xs, double[] ys)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                if (double.IsNaN(xs[i]) || double.IsInfinity(xs[i]) ||
                    double.IsNaN(ys[i]) || double.IsInfinity(ys[i]))
                {
                    throw new InvalidOperationException("Benchmark validation failed: transform produced non-finite values.");
                }
            }
        }
    }
}
