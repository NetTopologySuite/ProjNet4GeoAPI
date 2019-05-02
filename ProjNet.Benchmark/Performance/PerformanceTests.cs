using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNET.Tests.Geometries.Implementation;

namespace ProjNET.Benchmark.Performance
{
    public class PerformanceTests
    {
        private MathTransform _mathTransform;

        private IGeometry[] _geoms;

        private SequenceTransformerBase _optimizedSequenceTransformer;

        public PerformanceTests() { }

        public PerformanceTests(bool verify)
        {
            IGeometry[][] results =
            {
                Run(true, nameof(CoordinateArraySequenceFactory), "Converter"),
                Run(true, nameof(CoordinateArraySequenceFactory), "Transformer"),
                Run(true, nameof(PackedCoordinateSequenceFactory), "Converter"),
                Run(true, nameof(PackedCoordinateSequenceFactory), "Transformer"),
                Run(true, nameof(DotSpatialAffineCoordinateSequenceFactory), "Converter"),
                Run(true, nameof(DotSpatialAffineCoordinateSequenceFactory), "Transformer"),
                Run(true, nameof(SpanCoordinateSequenceFactory), "Converter"),
                Run(true, nameof(SpanCoordinateSequenceFactory), "Transformer"),
                Run(false, nameof(CoordinateArraySequenceFactory), "Converter"),
                Run(false, nameof(CoordinateArraySequenceFactory), "Transformer"),
                Run(false, nameof(PackedCoordinateSequenceFactory), "Converter"),
                Run(false, nameof(PackedCoordinateSequenceFactory), "Transformer"),
                Run(false, nameof(SpanCoordinateSequenceFactory), "Converter"),
                Run(false, nameof(SpanCoordinateSequenceFactory), "Transformer"),
            };

            var expected = results[0];

            for (int i = 1; i < results.Length; i++)
            {
                var actual = results[i];
                if (actual.Length != expected.Length)
                {
                    throw new Exception($"i: {i}{Environment.NewLine}expect: {expected.Length}{Environment.NewLine}actual: {actual.Length}");
                }

                for (int j = 0; j < expected.Length; j++)
                {
                    if (!expected[j].EqualsExact(actual[j]))
                    {
                        throw new Exception($"i: {i}{Environment.NewLine}j: {j}{Environment.NewLine}expect: {expected[j]}{Environment.NewLine}actual: {actual[j]}");
                    }
                }
            }

            IGeometry[] Run(bool runWithDefault, string sequenceFactory, string implementation)
            {
                SequenceFactory = sequenceFactory;
                Implementation = implementation;
                Initialize();
                if (runWithDefault)
                {
                    RunDefault();
                }
                else
                {
                    RunOptimized();
                }

                return _geoms;
            }
        }

        [TestCase(true, nameof(DotSpatialAffineCoordinateSequenceFactory), "Converter")]
        [TestCase(false, nameof(CoordinateArraySequenceFactory), "Converter")]
        [TestCase(true, nameof(CoordinateArraySequenceFactory), "Converter")]
        [TestCase(false, nameof(CoordinateArraySequenceFactory), "Transformer")]
        [TestCase(true, nameof(CoordinateArraySequenceFactory), "Transformer")]
        [TestCase(false, nameof(SpanCoordinateSequenceFactory), "Transformer")]
        [TestCase(true, nameof(SpanCoordinateSequenceFactory), "Transformer")]
        public void Test(bool opt, string sf, string im)
        {
            SequenceFactory = sf;
            Implementation = im;
            Initialize();
            if (opt) RunOptimized();
            else RunDefault();

        }

        [Params(nameof(CoordinateArraySequenceFactory), nameof(PackedCoordinateSequenceFactory), nameof(DotSpatialAffineCoordinateSequenceFactory), nameof(SpanCoordinateSequenceFactory))]
        public string SequenceFactory;

        [Params("Converter", "Transformer")]
        public string Implementation;

        [GlobalSetup]
        public void Initialize()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(), new CoordinateTransformationFactory(), new []
                { new KeyValuePair<int, string>(25832, "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"25832\"]]") }
            );
            _mathTransform = (MathTransform)css.CreateTransformation(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator).MathTransform;

            ICoordinateSequenceFactory sequenceFactory;
            switch (SequenceFactory)
            {
                case nameof(CoordinateArraySequenceFactory):
                    sequenceFactory = CoordinateArraySequenceFactory.Instance;
                    _optimizedSequenceTransformer = new CoordinateArraySequenceTransformer();
                    break;

                case nameof(PackedCoordinateSequenceFactory):
                    sequenceFactory = PackedCoordinateSequenceFactory.DoubleFactory;
                    _optimizedSequenceTransformer = new PackedDoubleSequenceTransformer();
                    break;

                case nameof(DotSpatialAffineCoordinateSequenceFactory):
                    sequenceFactory = DotSpatialAffineCoordinateSequenceFactory.Instance;
                    _optimizedSequenceTransformer = new DotSpatialSequenceTransformer();
                    break;

                case nameof(SpanCoordinateSequenceFactory):
                    sequenceFactory = SpanCoordinateSequenceFactory.Instance;
                    _optimizedSequenceTransformer = new SpanCoordinateSequenceTransformer();
                    break;

                default:
                    throw new Exception("update this block when you update the params");
            }

            var gf = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 4326, sequenceFactory);
            var wktReader = new WKTReader(gf) { IsOldNtsCoordinateSyntaxAllowed = false, HandleOrdinates = Ordinates.XY};
            _geoms = Directory.EnumerateFiles("TestData", "*.wkt")
                .SelectMany(file => new WKTFileReader(file, wktReader).Read())
                .ToArray();
        }

        [Benchmark(Baseline = true)]
        public void RunDefault()
        {
            if (Implementation == "Converter")
            {
                MathTransform.SequenceCoordinateConverter = new SequenceCoordinateConverterBase();
                MathTransform.SequenceTransformer = null;
            }
            else
            {
                MathTransform.SequenceCoordinateConverter = null;
                MathTransform.SequenceTransformer = new SequenceTransformerBase();
            }

            Run();
        }

        [Benchmark]
        public void RunOptimized()
        {
            if (Implementation == "Converter")
            {
                MathTransform.SequenceCoordinateConverter = new OptimizedCoordinateSequenceConverter();
                MathTransform.SequenceTransformer = null;
            }
            else
            {
                MathTransform.SequenceCoordinateConverter = null;
                MathTransform.SequenceTransformer = _optimizedSequenceTransformer;
            }

            Run();
        }

        private void Run()
        {
            foreach (var geometry in _geoms)
            {
                Transform(geometry, _mathTransform);
            }
        }

        public static void Transform(IGeometry geometry, MathTransform transform)
        {
            switch (geometry)
            {
                case IGeometryCollection _:
                    for (int i = 0; i < geometry.NumGeometries; i++)
                    {
                        Transform(geometry.GetGeometryN(i), transform);
                    }

                    break;

                case IPoint p:
                    transform.Transform(p.CoordinateSequence);
                    break;

                case ILineString l:
                    transform.Transform(l.CoordinateSequence);
                    break;

                case IPolygon po:
                    transform.Transform(po.ExteriorRing.CoordinateSequence);
                    foreach (var hole in po.InteriorRings)
                    {
                        transform.Transform(hole.CoordinateSequence);
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
