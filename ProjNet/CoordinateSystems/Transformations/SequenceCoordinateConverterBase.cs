using System;
using System.Buffers;
using GeoAPI.Geometries;
using ProjNet.Geometries;

namespace ProjNet.CoordinateSystems.Transformations
{
    /// <summary>
    /// 
    /// </summary>
    public class SequenceCoordinateConverterBase
    {
        protected static readonly Action Nop = () => { };

        public MemoryPool<XY> MemoryPoolForXys { get; set; } = MemoryPool<XY>.Shared;

        public MemoryPool<double> MemoryPoolForZs { get; set; } = MemoryPool<double>.Shared;

        public virtual Action ExtractRawCoordinatesFromSequence(ICoordinateSequence sequence, out Span<XY> xys, out Span<double> zs)
        {
            if (sequence == null || sequence.Count < 1)
            {
                xys = default;
                zs = default;
                return Nop;
            }

            IMemoryOwner<XY> xysOwner = null;
            IMemoryOwner<double> zsOwner = null;

            try
            {
                int count = sequence.Count;
                xysOwner = MemoryPoolForXys.Rent(count);
                xys = xysOwner.Memory.Span.Slice(0, count);

                bool hasZ = sequence.HasZ;
                if (hasZ)
                {
                    zsOwner = MemoryPoolForZs.Rent(count);
                    zs = zsOwner.Memory.Span.Slice(0, count);
                }
                else
                {
                    zs = default;
                }

                for (int i = 0; i < xys.Length; i++)
                {
                    xys[i].X = sequence.GetX(i);
                    xys[i].Y = sequence.GetY(i);
                    if (zs.Length != 0)
                    {
                        zs[i] = sequence.GetZ(i);
                    }
                }

                return () =>
                {
                    using (zsOwner)
                    {
                        xysOwner.Dispose();
                    }
                };
            }
            catch
            {
                zsOwner?.Dispose();
                xysOwner?.Dispose();
                throw;
            }
        }

        public void CopyRawCoordinatesToSequence(Span<XY> xys, Span<double> zs, ICoordinateSequence sequence)
        {
            if (sequence == null || sequence.Count < 1)
            {
                return;
            }

            if (sequence.Count < xys.Length)
            {
                throw new ArgumentException("Not enough room in the sequence for the coordinates.");
            }

            if (xys.Length == 0)
            {
                return;
            }

            if (zs.Length == 0)
            {
                if (sequence.HasZ)
                {
                    throw new ArgumentException("can only be empty when sequence does not have Z", nameof(zs));
                }
            }
            else if (xys.Length != zs.Length)
            {
                throw new ArgumentException("spans must be the same length.");
            }

            CopyRawCoordinatesToSequenceCore(xys, zs, sequence);
        }

        protected virtual void CopyRawCoordinatesToSequenceCore(ReadOnlySpan<XY> xys, ReadOnlySpan<double> zs, ICoordinateSequence sequence)
        {
            bool hasZ = sequence.HasZ;
            for (int i = 0; i < xys.Length; i++)
            {
                sequence.SetOrdinate(i, Ordinate.X, xys[i].X);
                sequence.SetOrdinate(i, Ordinate.Y, xys[i].Y);

                // documentation says that the sequence MUST NOT throw if it doesn't support Z
                // and that it SHOULD ignore the call... PackedCoordinateSequence instances will
                // overwrite other values, so we do need to skip.
                if (hasZ)
                {
                    sequence.SetOrdinate(i, Ordinate.Z, zs.Length == 0 ? 0 : zs[i]);
                }
            }
        }
    }
}
