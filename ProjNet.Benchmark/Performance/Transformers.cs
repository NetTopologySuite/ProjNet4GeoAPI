using System;
using System.Buffers;
using System.Runtime.InteropServices;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries.Implementation;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;
using ProjNET.Tests.Geometries.Implementation;

namespace ProjNET.Benchmark.Performance
{
    internal class OptimizedCoordinateSequenceConverter : SequenceCoordinateConverterBase
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

    internal class CoordinateArraySequenceTransformer : SequenceTransformerBase
    {
        private readonly double[] _inZ = {0};
        private readonly double[] _outZ = {0};

        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (CoordinateArraySequence) sequence;

            if (s.HasZ && transform.DimSource > 2)
                TransformWithZ(transform, s.ToCoordinateArray());
            else
                Transform(transform, s.ToCoordinateArray());
        }

        private void TransformWithZ(MathTransform transform, Coordinate[] ca)
        {
            using (var xyzOwner = MemoryPool<double>.Shared.Rent(ca.Length * 3))
            {
                var x = xyzOwner.Memory.Span.Slice(0, ca.Length);
                var y = xyzOwner.Memory.Span.Slice(ca.Length, ca.Length);
                var z = xyzOwner.Memory.Span.Slice(2* ca.Length, ca.Length);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    x[i] = c.X;
                    y[i] = c.Y;
                    z[i] = c.Z;
                }

                transform.Transform(x, y, z, x, y, z);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    c.X = x[i];
                    c.Y = y[i];
                    c.Z = z[i];
                }
            }
        }

        private void Transform(MathTransform transform, Coordinate[] ca)
        {
        
            using (var xyOwner = MemoryPool<double>.Shared.Rent(2*ca.Length))
            {
                var x = xyOwner.Memory.Span.Slice(0, ca.Length);
                var y = xyOwner.Memory.Span.Slice(ca.Length, ca.Length);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    x[i] = c.X;
                    y[i] = c.Y;
                }

                transform.Transform(x, y, x, y);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    c.X = x[i];
                    c.Y = y[i];
                }
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

    internal class DotSpatialSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (DotSpatialAffineCoordinateSequence)sequence;
            var xy = MemoryMarshal.Cast<double, XY>(s.XY);
            transform.Transform(xy, s.Z, xy, s.Z);
        }
    }

    internal class SpanCoordinateSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var scs = (SpanCoordinateSequence)sequence;
            var zAsSpan = scs.ZsAsSpan();
            if (zAsSpan.Length > 0)
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan(), zAsSpan,
                    scs.XsAsSpan(), scs.YsAsSpan(), zAsSpan);
            else
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan(), scs.XsAsSpan(), scs.YsAsSpan());
        }
    }

}
