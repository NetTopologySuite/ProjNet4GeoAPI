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
        public override Action ExtractRawCoordinatesFromSequence(ICoordinateSequence sequence, out Span<double> xs, out int strideX, out Span<double> ys, out int strideY, out Span<double> zs, out int strideZ)
        {
            Span<double> xys = null;
            switch (sequence)
            {
                case PackedDoubleCoordinateSequence packedSeq when packedSeq.Dimension == 2:
                    xys = new Span<double>(packedSeq.GetRawCoordinates());
                    xs = xys.Slice(0);
                    strideX = 2;
                    ys = xys.Slice(1);
                    strideY = 2;
                    zs = default;
                    strideZ = 0;
                    return Nop;

                case DotSpatialAffineCoordinateSequence dotSpatialSeq:
                    xys = new Span<double>(dotSpatialSeq.XY);
                    xs = xys.Slice(0);
                    strideX = 2;
                    ys = xys.Slice(1);
                    strideY = 2;
                    if (sequence.HasZ)
                    {
                        zs = dotSpatialSeq.Z;
                        strideZ = 1;
                    }
                    else
                    {
                        zs = default;
                        strideZ = 0;
                    }
                    return Nop;

                case SpanCoordinateSequence spanSeq:
                    xs = spanSeq.XsAsSpan();
                    strideX = 1;
                    ys = spanSeq.YsAsSpan();
                    strideY = 1;
                    zs = spanSeq.ZsAsSpan();
                    strideZ = spanSeq.HasZ ? 1 : 0;
                    return Nop;

                default:
                    return base.ExtractRawCoordinatesFromSequence(sequence, out xs, out strideX, out ys, out strideY, out zs, out strideZ);
            }
        }

        protected override void CopyRawCoordinatesToSequenceCore(Span<double> xs, int strideX, Span<double> ys, int strideY, Span<double> zs, int strideZ, ICoordinateSequence sequence)
        {
            switch (sequence)
            {
                case PackedDoubleCoordinateSequence packedSeq when packedSeq.Dimension == 2:
                    //xs.CopyTo(packedSeq.GetRawCoordinates());
                    break;

                case DotSpatialAffineCoordinateSequence dotSpatialSeq:
                    break;

                case SpanCoordinateSequence spanSeq:
                    break;

                default:
                    base.CopyRawCoordinatesToSequenceCore(xs, strideX, ys, strideY,zs, strideZ, sequence);
                    break;
            }
        }
    }

    internal class CoordinateArraySequenceTransformer : SequenceTransformerBase
    {
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
                var z = xyzOwner.Memory.Span.Slice(2 * ca.Length, ca.Length);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    x[i] = c.X;
                    y[i] = c.Y;
                    z[i] = c.Z;
                }

                transform.Transform(x, y, z);

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
        
            using (var xyOwner = MemoryPool<double>.Shared.Rent(2 * ca.Length))
            {
                var x = xyOwner.Memory.Span.Slice(0, ca.Length);
                var y = xyOwner.Memory.Span.Slice(ca.Length, ca.Length);

                for (int i = 0; i < ca.Length; i++)
                {
                    var c = ca[i];
                    x[i] = c.X;
                    y[i] = c.Y;
                }

                transform.Transform(x, y);

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
            var raw = new Span<double>(s.GetRawCoordinates());
            var xs = raw.Slice(0);
            var ys = raw.Slice(1);
            var dimension = s.Dimension;
            if (!s.HasZ)
            {
                transform.Transform(xs, ys, dimension, dimension);
            }
            else
            {
                var zs = raw.Slice(2);
                transform.Transform(xs, ys, zs, dimension, dimension, dimension);
            }
        }
    }

    internal class DotSpatialSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var s = (DotSpatialAffineCoordinateSequence)sequence;
            var xy = new Span<double>(s.XY);
            var xs = xy.Slice(0);
            var ys = xy.Slice(1);
            transform.Transform(xs, ys, s.Z, 2, 2);
        }
    }

    internal class SpanCoordinateSequenceTransformer : SequenceTransformerBase
    {
        public override void Transform(MathTransform transform, ICoordinateSequence sequence)
        {
            var scs = (SpanCoordinateSequence)sequence;
            var zAsSpan = scs.ZsAsSpan();
            if (zAsSpan.Length > 0)
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan(), zAsSpan);
            else
                transform.Transform(scs.XsAsSpan(), scs.YsAsSpan());
        }
    }

}
