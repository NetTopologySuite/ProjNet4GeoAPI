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

namespace ProjNET.Tests.Performance
{
    public class PerformanceTests
    {
        private readonly CoordinateSystemServices _css = new CoordinateSystemServices(new CoordinateSystemFactory(), new CoordinateTransformationFactory());

        [TestCase(@"TestData\africa.wkt")]
        [TestCase(@"TestData\europe.wkt")]
        [TestCase(@"TestData\world.wkt")]
        public void TestPerformance(string pathToWktFile)
        {
            if (!Path.IsPathRooted(pathToWktFile))
                pathToWktFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), pathToWktFile);

            if (!File.Exists(pathToWktFile))
                throw new IgnoreException($"File '{pathToWktFile}' not found.");

            Console.WriteLine(pathToWktFile);
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
#endif
        }

        private void DoTestPerformance(ICoordinateSequenceFactory factory, string pathToWktFile,
#if SequenceCoordinateConverter
            SequenceCoordinateConverterBase c = null)
#else
            SequenceTransformerBase c = null)
#endif
        {
            const int numIterations = 50;
            var gf = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 4326, factory);
            var wktFileReader = new WKTFileReader(pathToWktFile, new WKTReader(gf));

#if SequenceCoordinateConverter
            MathTransform.SequenceCoordinateConverter = c;
#else
            MathTransform.SequenceTransformer = c;
#endif
            var geometries = wktFileReader.Read();
            var stopwatch = new Stopwatch();

            var mt = _css.CreateTransformation(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WebMercator).MathTransform; 
            var gf2 = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating), 3857, gf.CoordinateSequenceFactory);

            long elapsedMs = 0;
            for (int i = 0; i <= numIterations; i++)
            {
                var transformed = new List<IGeometry>(geometries.Count);

                stopwatch.Restart();
                foreach (var geometry in geometries)
                {
                    transformed.Add(Transform(geometry, mt, gf2));
                }
                stopwatch.Stop();

                elapsedMs += stopwatch.ElapsedMilliseconds;
            }

#if SequenceCoordinateConverter
            string util = MathTransform.SequenceCoordinateConverter.GetType().Name;
#else
            string util = MathTransform.SequenceTransformer.GetType().Name;
#endif
            Console.WriteLine($"Transformation of {geometries.Count} geometries using {gf.CoordinateSequenceFactory.GetType().Name} with {util} took ~{elapsedMs / numIterations} ms");

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
#if !SequenceCoordinateConverter

    public class CoordinateArraySequenceTransformer : SequenceTransformerBase
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

            var xyIn = MemoryMarshal.Cast<double, XY>(new ReadOnlySpan<double>(xy));
            var zIn = new ReadOnlySpan<double>(z);
            var xyOut = MemoryMarshal.Cast<double, XY>(new Span<double>(xy));
            var zOut = new Span<double>(z);

            transform.Transform(xyIn, zIn, xyOut, zOut);

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

    public class PackedDoubleSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (PackedDoubleCoordinateSequence) sequence;
            var raw = s.GetRawCoordinates();
            if (s.Dimension == 2)
            {
                var xyIn = MemoryMarshal.Cast<double, XY>(new ReadOnlySpan<double>(raw));
                var xyOut = MemoryMarshal.Cast<double, XY>(new Span<double>(raw));
                var zArr = new double[sequence.Count];
                var zOut = new Span<double>(zArr);
                var zIn = new ReadOnlySpan<double>(zArr);

                transform.Transform(xyIn, zIn, xyOut, zOut);
            }
            else if (s.Dimension == 3 && s.HasZ)
            {
                var xyzIn = MemoryMarshal.Cast<double, XYZ>(new ReadOnlySpan<double>(raw));
                var xyzOut = MemoryMarshal.Cast<double, XYZ>(raw);

                transform.Transform(xyzIn, xyzOut);
            }
            else
                base.Transform(transform, sequence);
        }
    }

    public class DotSpatialSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (DotSpatialAffineCoordinateSequence)sequence;
            if (s.Dimension == 2 || (s.Dimension > 2 && !s.HasZ))
            {
                var xyIn = MemoryMarshal.Cast<double, XY>(new ReadOnlySpan<double>(s.XY));
                var xyOut = MemoryMarshal.Cast<double, XY>(s.XY);
                var zArr = new double[s.Count];
                var zOut = new Span<double>(zArr);
                var zIn = new ReadOnlySpan<double>(zArr);

                transform.Transform(xyIn, zIn, xyOut, zOut);
            }
            else if (s.Dimension > 2 && s.HasZ)
            {
                var xyIn = MemoryMarshal.Cast<double, XY>(new ReadOnlySpan<double>(s.XY));
                var xyOut = MemoryMarshal.Cast<double, XY>(s.XY);
                var zOut = new Span<double>(s.Z);
                var zIn = new ReadOnlySpan<double>(s.Z);

                transform.Transform(xyIn, zIn, xyOut, zOut);
            }
            else
                base.Transform(transform, sequence);
        }
    }

#endif
}