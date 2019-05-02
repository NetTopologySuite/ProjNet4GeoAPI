using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
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

        [TestCase(@"TestData\africa.wkt")]
        [TestCase(@"TestData\europe.wkt")]
        [TestCase(@"TestData\world.wkt")]
        public void TestPerformance(string pathToWktFile)
        {
            if (!Path.IsPathRooted(pathToWktFile))
                pathToWktFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), pathToWktFile);

            if (!File.Exists(pathToWktFile))
                throw new IgnoreException($"File '{pathToWktFile}' not found.");

            //Console.WriteLine(pathToWktFile);
#if WithSpans
#if SequenceCoordinateConverter
            MathTransform.SequenceCoordinateConverter = null;
            DoTestPerformance(CoordinateArraySequenceFactory.Instance, pathToWktFile);
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, pathToWktFile);
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, pathToWktFile);
#else
            DoTestPerformance(CoordinateArraySequenceFactory.Instance, pathToWktFile);
            DoTestPerformance(CoordinateArraySequenceFactory.Instance, pathToWktFile, new CoordinateArraySequenceTransformer());
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, pathToWktFile);
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, pathToWktFile, new PackedDoubleSequenceTransformer());
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, pathToWktFile);
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, pathToWktFile, new DotSpatialSequenceTransformer());
            DoTestPerformance(SpanCoordinateSequenceFactory.Instance, pathToWktFile, null);
            DoTestPerformance(SpanCoordinateSequenceFactory.Instance, pathToWktFile, new SpanCoordinateSequenceTransformer());
#endif
#else
            DoTestPerformance(CoordinateArraySequenceFactory.Instance, pathToWktFile);
            DoTestPerformance(PackedCoordinateSequenceFactory.DoubleFactory, pathToWktFile);
            DoTestPerformance(DotSpatialAffineCoordinateSequenceFactory.Instance, pathToWktFile);
#endif
        }

        private void DoTestPerformance(ICoordinateSequenceFactory factory, string pathToWktFile
#if WithSpans
#if SequenceCoordinateConverter
            , SequenceCoordinateConverterBase c = null)
#else
            , SequenceTransformerBase c = null)
#endif
#else
        )
#endif
        {
            const int numIterations = 25;
            var gf = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 4326, factory);
            var wktFileReader = new WKTFileReader(pathToWktFile, new WKTReader(gf));

#if WithSpans
#if SequenceCoordinateConverter
            MathTransform.SequenceCoordinateConverter = c;
#else
            MathTransform.SequenceTransformer = c;
#endif
#endif
            var geometries = wktFileReader.Read();
            var stopwatch = new Stopwatch();

            var mt = _css.CreateTransformation(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator).MathTransform; 
            var gf2 = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 3857, gf.CoordinateSequenceFactory);

            long elapsedMs = 0;
            long numCoordinates = 0;
            for (int i = 0; i <= numIterations; i++)
            {
                var transformed = new List<IGeometry>(geometries.Count);

                stopwatch.Restart();
                foreach (var geometry in geometries)
                {
                    transformed.Add(Transform(geometry, mt, gf2));
                    if (i == 0) numCoordinates += geometry.NumPoints;
                }
                stopwatch.Stop();

                elapsedMs += stopwatch.ElapsedMilliseconds;
            }

#if (WithSpans)
#if SequenceCoordinateConverter
            string util = MathTransform.SequenceCoordinateConverter.GetType().Name;
#else
            string util = MathTransform.SequenceTransformer.GetType().Name;
#endif
#else
            string util = "no span";
#endif
            Console.WriteLine($"| {gf.CoordinateSequenceFactory.GetType().Name} | {util} | {geometries.Count} | {numCoordinates} |  ~{elapsedMs / numIterations} ms |");

        }

        private static IGeometry Transform(IGeometry geometry, IMathTransform transfrom, IGeometryFactory factory)
        {
            if (geometry is IGeometryCollection)
            {
                var res = new IGeometry[geometry.NumGeometries];
                for (int i = 0; i < geometry.NumGeometries; i++)
                    res[i] = Transform(geometry.GetGeometryN(i), transfrom, factory);
                return factory.BuildGeometry(res);
            }

            if (geometry is IPoint p)
                return factory.CreatePoint(transfrom.Transform(p.CoordinateSequence));

            if (geometry is ILineString l)
                return factory.CreateLineString(transfrom.Transform(l.CoordinateSequence));

            if (geometry is IPolygon po)
            {
                var holes = new ILinearRing[po.NumInteriorRings];
                for (int i = 0; i < po.NumInteriorRings; i++)
                {
                    var ring = CoordinateSequences.EnsureValidRing(factory.CoordinateSequenceFactory, transfrom.Transform(po.InteriorRings[i].CoordinateSequence));
                    holes[i] = factory.CreateLinearRing(ring);
                }

                var shell = CoordinateSequences.EnsureValidRing(
                    factory.CoordinateSequenceFactory, transfrom.Transform(po.ExteriorRing.CoordinateSequence));

                return CoordinateSequences.IsRing(shell)
                    ? factory.CreatePolygon(factory.CreateLinearRing(shell), holes)
                    : null;
            }

            throw new NotSupportedException();
        }
    }
#if WithSpans
#if !SequenceCoordinateConverter

    internal class CoordinateArraySequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (CoordinateArraySequence) sequence;
            var ca = s.ToCoordinateArray();

            var xy = new double[2 * ca.Length];
            var z = new double[ca.Length];
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
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (PackedDoubleCoordinateSequence) sequence;
            var raw = s.GetRawCoordinates();
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
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
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
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var scs = (SpanCoordinateSequence)sequence;
            var inZs = scs.ZsAsSpan();
            if (inZs.Length > 0)
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan(), inZs);
            else
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan());

        }
    }

#endif
#endif
}
