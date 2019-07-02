using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.IO;
using NUnit.Framework;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;
using ProjNET.Tests.Geometries.Implementation;

namespace ProjNET.Tests.Performance
{
    public class PerformanceTests
    {
        private readonly CoordinateSystemServices _css = new CoordinateSystemServices(new CoordinateSystemFactory(), new CoordinateTransformationFactory());

        [SetUp]
        public void TestSetup()
        {
            Console.WriteLine($"| Factory | Utility | Geometries | Coordinates | Avg. elapsed  Time |");
            Console.WriteLine($"|---------|---------|-----------:|------------:|-------------------:|");
        }

        [TestCase("africa.wkt")]
        [TestCase("europe.wkt")]
        [TestCase("world.wkt")]
        public void TestPerformance(string wktFileName)
        {
            string fullPathToWktFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                                    "TestData",
                                                    wktFileName);

            if (!File.Exists(fullPathToWktFile))
            {
                Assert.Ignore($"File '{wktFileName}' not found.");
            }

            //Console.WriteLine(pathToWktFile);
            DoTestPerformance(CoordinateArraySequenceFactory.Instance, fullPathToWktFile, new SequenceCoordinateConverterBase());
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, fullPathToWktFile, new SequenceCoordinateConverterBase());
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, fullPathToWktFile, new SequenceCoordinateConverterBase());
            DoTestPerformance(SpanCoordinateSequenceFactory.Instance, fullPathToWktFile, new SequenceCoordinateConverterBase());

            DoTestPerformance(CoordinateArraySequenceFactory.Instance, fullPathToWktFile, new SequenceTransformerBase());
            DoTestPerformance(CoordinateArraySequenceFactory.Instance, fullPathToWktFile, new CoordinateArraySequenceTransformer());
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, fullPathToWktFile, new SequenceTransformerBase());
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, fullPathToWktFile, new PackedDoubleSequenceTransformer());
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, fullPathToWktFile, new SequenceTransformerBase());
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, fullPathToWktFile, new DotSpatialSequenceTransformer());
            DoTestPerformance(SpanCoordinateSequenceFactory.Instance, fullPathToWktFile, new SequenceTransformerBase());
            DoTestPerformance(SpanCoordinateSequenceFactory.Instance, fullPathToWktFile, new SpanCoordinateSequenceTransformer());
        }

        private void DoTestPerformance(CoordinateSequenceFactory factory, string pathToWktFile, object transformUtility)
        {
            const int numIterations = 25;
            var gf = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 4326, factory);
            var wktFileReader = new WKTFileReader(pathToWktFile, new WKTReader(gf));

            var geometries = wktFileReader.Read();
            var stopwatch = new Stopwatch();

            var mt = _css.CreateTransformation(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator).MathTransform; 
            var gf2 = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 3857, gf.CoordinateSequenceFactory);

            long elapsedMs = 0;
            long numCoordinates = 0;
            for (int i = 0; i <= numIterations; i++)
            {
                var transformed = new List<Geometry>(geometries.Count);

                if (transformUtility is SequenceCoordinateConverterBase sc)
                {
                    stopwatch.Restart();
                    foreach (var geometry in geometries)
                    {
                        transformed.Add(Transform(geometry, mt, gf2, sc));
                        if (i == 0) numCoordinates += geometry.NumPoints;
                    }

                    stopwatch.Stop();
                } else if (transformUtility is SequenceTransformerBase st)
                {
                    stopwatch.Restart();
                    foreach (var geometry in geometries)
                    {
                        transformed.Add(Transform(geometry, mt, gf2, st));
                        if (i == 0) numCoordinates += geometry.NumPoints;
                    }

                    stopwatch.Stop();
                }

                elapsedMs += stopwatch.ElapsedMilliseconds;
            }

            string util = transformUtility.GetType().Name;
            Console.WriteLine($"| {gf.CoordinateSequenceFactory.GetType().Name} | {util} | {geometries.Count} | {numCoordinates} |  ~{elapsedMs / numIterations} ms |");

        }

        private static Geometry Transform(Geometry geometry, MathTransform transform, GeometryFactory factory, SequenceCoordinateConverterBase sc)
        {
            if (geometry is GeometryCollection)
            {
                var res = new Geometry[geometry.NumGeometries];
                for (int i = 0; i < geometry.NumGeometries; i++)
                    res[i] = Transform(geometry.GetGeometryN(i), transform, factory, sc);
                return factory.BuildGeometry(res);
            }

            if (geometry is Point p)
                return factory.CreatePoint(transform.Transform(p.CoordinateSequence, sc));

            if (geometry is LineString l)
                return factory.CreateLineString(transform.Transform(l.CoordinateSequence, sc));

            if (geometry is Polygon po)
            {
                var holes = new LinearRing[po.NumInteriorRings];
                for (int i = 0; i < po.NumInteriorRings; i++)
                {
                    var ring = CoordinateSequences.EnsureValidRing(factory.CoordinateSequenceFactory, transform.Transform(po.InteriorRings[i].CoordinateSequence, sc));
                    holes[i] = factory.CreateLinearRing(ring);
                }

                var shell = CoordinateSequences.EnsureValidRing(
                    factory.CoordinateSequenceFactory, transform.Transform(po.ExteriorRing.CoordinateSequence, sc));

                return CoordinateSequences.IsRing(shell)
                    ? factory.CreatePolygon(factory.CreateLinearRing(shell), holes)
                    : null;
            }

            throw new NotSupportedException();
        }

        private static Geometry Transform(Geometry geometry, MathTransform transform, GeometryFactory factory, SequenceTransformerBase st)
        {
            if (geometry is GeometryCollection)
            {
                var res = new Geometry[geometry.NumGeometries];
                for (int i = 0; i < geometry.NumGeometries; i++)
                    res[i] = Transform(geometry.GetGeometryN(i), transform, factory, st);
                return factory.BuildGeometry(res);
            }

            if (geometry is Point p)
                return factory.CreatePoint(transform.Transform(p.CoordinateSequence, st));

            if (geometry is LineString l)
                return factory.CreateLineString(transform.Transform(l.CoordinateSequence, st));

            if (geometry is Polygon po)
            {
                var holes = new LinearRing[po.NumInteriorRings];
                for (int i = 0; i < po.NumInteriorRings; i++)
                {
                    var ring = CoordinateSequences.EnsureValidRing(factory.CoordinateSequenceFactory, transform.Transform(po.InteriorRings[i].CoordinateSequence, st));
                    holes[i] = factory.CreateLinearRing(ring);
                }

                var shell = CoordinateSequences.EnsureValidRing(
                    factory.CoordinateSequenceFactory, transform.Transform(po.ExteriorRing.CoordinateSequence, st));

                return CoordinateSequences.IsRing(shell)
                    ? factory.CreatePolygon(factory.CreateLinearRing(shell), holes)
                    : null;
            }

            throw new NotSupportedException();
        }

    }

    internal class CoordinateArraySequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, CoordinateSequence sequence)
        {
            var s = (CoordinateArraySequence) sequence;
            var ca = s.ToCoordinateArray();

            double[] xy = new double[2 * ca.Length];
            double[] z = new double[ca.Length];
            for (int i = 0, j = 0; i < ca.Length; i++)
            {
                xy[j++] = ca[i].X;
                xy[j++] = ca[i].Y;
            }
            if (s.HasZ & transform.DimSource > 2)
            {
                for (int i = 0; i < ca.Length; i++)
                    z[i] = ca[i].Z;
            }

            var xys = MemoryMarshal.Cast<double, XY>(new Span<double>(xy));
            var zs = new Span<double>(z);

            transform.Transform(xys, zs);

            for (int i = 0, j = 0; i < ca.Length; i++)
            {
                ca[i].X = xy[j++];
                ca[i].Y = xy[j++];
            }
            if (s.HasZ & transform.DimTarget > 2)
            {
                for (int i = 0; i < ca.Length; i++)
                    ca[i].Z = z[i];
            }
        }
    }

    internal class PackedDoubleSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, CoordinateSequence sequence)
        {
            var s = (PackedDoubleCoordinateSequence) sequence;
            double[] raw = s.GetRawCoordinates();
            if (s.Dimension == 2)
            {
                var xs = new Span<double>(raw).Slice(0, raw.Length - 1);
                var ys = new Span<double>(raw).Slice(1, raw.Length - 1);
                transform.Transform(xs, ys, 2, 2);
            }
            else if (s.Dimension == 3 && s.HasZ)
            {
                var xs = new Span<double>(raw).Slice(0, raw.Length - 2);
                var ys = new Span<double>(raw).Slice(1, raw.Length - 2);
                var zs = new Span<double>(raw).Slice(1, raw.Length - 2);
                transform.Transform(xs, ys, zs, 3, 3);
            }
            else
                base.Transform(transform, sequence);
        }
    }

    internal class DotSpatialSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, CoordinateSequence sequence)
        {
            var s = (DotSpatialAffineCoordinateSequence)sequence;
            int length = 2 * s.Count - 1;
            if (s.Dimension == 2 || (s.Dimension > 2 && !s.HasZ))
            {
                var xs = new Span<double>(s.XY).Slice(0, length);
                var ys = new Span<double>(s.XY).Slice(1, length);

                transform.Transform(xs, ys, 2, 2);
            }
            else if (s.Dimension > 2 && s.HasZ)
            {
                var xIn = new Span<double>(s.XY).Slice(0, length);
                var yIn = new Span<double>(s.XY).Slice(1, length);
                var zIn = new Span<double>(s.Z);

                transform.Transform(xIn, yIn, zIn, 2, 2);
            }
            else
                base.Transform(transform, sequence);
        }
    }

    internal class SpanCoordinateSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, CoordinateSequence sequence)
        {
            var scs = (SpanCoordinateSequence)sequence;
            var inZs = scs.ZsAsSpan();
            if (inZs.Length > 0)
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan(), inZs);
            else
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan());

        }
    }
}
