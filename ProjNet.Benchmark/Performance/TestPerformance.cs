using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNET.Tests.Performance
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
                Run(true, nameof(CoordinateArraySequenceFactory), "SequenceCoordinateConverter"),
                Run(true, nameof(CoordinateArraySequenceFactory), "SequenceTransformer"),
                Run(true, nameof(PackedCoordinateSequenceFactory), "SequenceCoordinateConverter"),
                Run(true, nameof(PackedCoordinateSequenceFactory), "SequenceTransformer"),
                Run(true, nameof(DotSpatialAffineCoordinateSequenceFactory), "SequenceCoordinateConverter"),
                Run(true, nameof(DotSpatialAffineCoordinateSequenceFactory), "SequenceTransformer"),
                Run(false, nameof(CoordinateArraySequenceFactory), "SequenceCoordinateConverter"),
                Run(false, nameof(CoordinateArraySequenceFactory), "SequenceTransformer"),
                Run(false, nameof(PackedCoordinateSequenceFactory), "SequenceCoordinateConverter"),
                Run(false, nameof(PackedCoordinateSequenceFactory), "SequenceTransformer"),
                Run(false, nameof(DotSpatialAffineCoordinateSequenceFactory), "SequenceCoordinateConverter"),
                Run(false, nameof(DotSpatialAffineCoordinateSequenceFactory), "SequenceTransformer"),
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
                    RunWithSequenceAwareOptimizations();
                }

                return _geoms;
            }
        }

        [Params(nameof(CoordinateArraySequenceFactory), nameof(PackedCoordinateSequenceFactory), nameof(DotSpatialAffineCoordinateSequenceFactory))]
        public string SequenceFactory;

        [Params("SequenceCoordinateConverter", "SequenceTransformer")]
        public string Implementation;

        [GlobalSetup]
        public void Initialize()
        {
            var css = new CoordinateSystemServices(new CoordinateSystemFactory(), new CoordinateTransformationFactory());
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

                default:
                    throw new Exception("update this block when you update the params");
            }

            var gf = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 4326, sequenceFactory);
            var wktReader = new WKTReader(gf) { IsOldNtsCoordinateSyntaxAllowed = false };
            _geoms = Directory.EnumerateFiles("TestData", "*.wkt")
                              .SelectMany(file => new WKTFileReader(file, wktReader).Read())
                              .ToArray();
        }

        [Benchmark(Baseline = true)]
        public void RunDefault()
        {
            if (Implementation == "SequenceCoordinateConverter")
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
        public void RunWithSequenceAwareOptimizations()
        {
            if (Implementation == "SequenceCoordinateConverter")
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
                    transform.TransformInPlace(p.CoordinateSequence);
                    break;

                case ILineString l:
                    transform.TransformInPlace(l.CoordinateSequence);
                    break;

                case IPolygon po:
                    transform.TransformInPlace(po.ExteriorRing.CoordinateSequence);
                    foreach (var hole in po.InteriorRings)
                    {
                        transform.TransformInPlace(hole.CoordinateSequence);
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class OptimizedCoordinateSequenceConverter : SequenceCoordinateConverterBase
    {
        public override Action ExtractRawCoordinatesFromSequence(ICoordinateSequence sequence, out Span<XY> xys, out Span<double> zs)
        {
            switch (sequence)
            {
                case PackedDoubleCoordinateSequence packedSeq when packedSeq.Dimension == 2:
                    xys = MemoryMarshal.Cast<double, XY>(packedSeq.GetRawCoordinates());
                    zs = default;
                    return Nop;

                case DotSpatialAffineCoordinateSequence dotSpatialSeq:
                    xys = MemoryMarshal.Cast<double, XY>(dotSpatialSeq.XY);
                    zs = dotSpatialSeq.Z;
                    return Nop;

                default:
                    return base.ExtractRawCoordinatesFromSequence(sequence, out xys, out zs);
            }
        }

        protected override void CopyRawCoordinatesToSequenceCore(ReadOnlySpan<XY> xys, ReadOnlySpan<double> zs, ICoordinateSequence sequence)
        {
            switch (sequence)
            {
                case PackedDoubleCoordinateSequence packedSeq when packedSeq.Dimension == 2:
                    xys.CopyTo(MemoryMarshal.Cast<double, XY>(packedSeq.GetRawCoordinates()));
                    break;

                case DotSpatialAffineCoordinateSequence dotSpatialSeq:
                    xys.CopyTo(MemoryMarshal.Cast<double, XY>(dotSpatialSeq.XY));
                    zs.CopyTo(dotSpatialSeq.Z);
                    break;

                default:
                    base.CopyRawCoordinatesToSequenceCore(xys, zs, sequence);
                    break;
            }
        }
    }

    public class CoordinateArraySequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (CoordinateArraySequence) sequence;
            var ca = s.ToCoordinateArray();

            using (var xyOwner = MemoryPool<XY>.Shared.Rent(ca.Length))
            using (var zOwner = s.HasZ ? MemoryPool<double>.Shared.Rent(ca.Length) : null)
            {
                var xy = xyOwner.Memory.Span.Slice(0, ca.Length);
                Span<double> z = default;
                if (zOwner != null && transform.DimSource > 2)
                {
                    z = zOwner.Memory.Span.Slice(0, ca.Length);
                }

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    xy[i].X = c.X;
                    xy[i].Y = c.Y;
                    if (z.Length != 0)
                    {
                        z[i] = c.Z;
                    }
                }

                transform.Transform(xy, z, xy, z);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    c.X = xy[i].X;
                    c.Y = xy[i].Y;
                    if (z.Length != 0)
                    {
                        c.Z = z[i];
                    }
                }
            }
        }
    }

    public class PackedDoubleSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (PackedDoubleCoordinateSequence) sequence;
            var raw = s.GetRawCoordinates();
            if (s.Dimension == 2)
            {
                var xy = MemoryMarshal.Cast<double, XY>(raw);
                transform.Transform(xy, default, xy, default);
            }
            else if (s.Dimension == 3 && s.HasZ)
            {
                var xyz = MemoryMarshal.Cast<double, XYZ>(raw);
                transform.Transform(xyz, xyz);
            }
            else
            {
                base.Transform(transform, sequence);
            }
        }
    }

    public class DotSpatialSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (DotSpatialAffineCoordinateSequence)sequence;
            var xy = MemoryMarshal.Cast<double, XY>(s.XY);
            transform.Transform(xy, s.Z, xy, s.Z);
        }
    }
}