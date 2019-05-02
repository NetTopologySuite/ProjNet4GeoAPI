using System;
using System.Buffers;
using System.Globalization;
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

        protected internal MemoryPool<XY> MemoryPoolForXys { get; set; } = MemoryPool<XY>.Shared;

        protected internal MemoryPool<double> MemoryPoolForDoubles { get; set; } = MemoryPool<double>.Shared;

        public virtual Action ExtractRawCoordinatesFromSequence(ICoordinateSequence sequence,
            out Span<double> xs, out int strideX,
            out Span<double> ys, out int strideY,
            out Span<double> zs, out int strideZ)
        {

            if (sequence == null || sequence.Count < 1)
            {
                strideX = strideY = strideZ = 0;
                xs = ys = zs = default;
                return Nop;
            }

            IMemoryOwner<double> xsOwner = null, ysOwner = null, zsOwner = null;
            strideX = strideY = 1;
            try
            {
                int count = sequence.Count;
                xsOwner = MemoryPoolForDoubles.Rent(count);
                xs = xsOwner.Memory.Span.Slice(0, count);
                ysOwner = MemoryPoolForDoubles.Rent(count);
                ys = ysOwner.Memory.Span.Slice(0, count);
                bool hasZ = sequence.HasZ;
                if (hasZ)
                {
                    strideZ = 1;
                    zsOwner = MemoryPoolForDoubles.Rent(count);
                    zs = zsOwner.Memory.Span.Slice(0, count);
                }
                else
                {
                    strideZ = 0;
                    zs = default;
                }

                for (int i = 0; i < count; i++)
                {
                    xs[i] = sequence.GetX(i);
                    ys[i] = sequence.GetY(i);
                    if (zs.Length != 0)
                    {
                        zs[i] = sequence.GetZ(i);
                    }
                }

                var xsOwnerTmp = xsOwner;
                var ysOwnerTmp = ysOwner;
                var zsOwnerTmp = zsOwner;
                return () =>
                {
                    using (zsOwnerTmp)
                    {
                        ysOwnerTmp.Dispose();
                        xsOwnerTmp.Dispose();
                    }
                };
            }
            catch
            {
                zsOwner?.Dispose();
                ysOwner?.Dispose();
                xsOwner?.Dispose();
                throw;
            }

        }

        public void CopyRawCoordinatesToSequence(Span<double> xs, int strideX, Span<double> ys, int strideY, Span<double> zs, int strideZ, ICoordinateSequence sequence)
        {
            if (sequence == null || sequence.Count < 1)
            {
                return;
            }

            if (xs == null || xs.Length == 0)
            {
                return;
            }

            if (ys == null || ys.Length == 0)
            {
                return;
            }

            if (sequence.HasZ && zs.Length == 0)
            {
                throw new ArgumentException("can only be empty when sequence does not have Z", nameof(zs));
            }

            int elementsX = xs.Length / strideX + xs.Length % strideX == 0 ? 0 : 1;
            if (sequence.Count < elementsX)
            {
                throw new ArgumentException("Not enough room in the sequence for the coordinates.");
            }

            int elementsY = ys.Length / strideY + ys.Length % strideY == 0 ? 0 : 1;
            if (elementsX != elementsY)
            {
                throw new ArgumentException("Provided spans don't provide same amount of ordinates");
            }
            int elementsZ = zs.Length == 0 ? 0 : zs.Length / strideZ + zs.Length % strideZ == 0 ? 0 : 1;
            if (elementsZ > 0 && elementsX != elementsZ)
            {
                throw new ArgumentException("Provided spans don't provide same amount of ordinates");
            }

            CopyRawCoordinatesToSequenceCore(xs, strideX, ys, strideY, zs, strideZ, sequence);
        }

        protected virtual void CopyRawCoordinatesToSequenceCore(Span<double> xs, int strideX, Span<double> ys, int strideY, Span<double> zs, int strideZ, ICoordinateSequence sequence)
        {
            bool hasZ = sequence.HasZ;
            for (int i = 0, j = 0, k = 0; i < xs.Length; i+=strideX,j+=strideY)
            {
                sequence.SetOrdinate(i, Ordinate.X, xs[i]);
                sequence.SetOrdinate(i, Ordinate.Y, ys[j]);

                // documentation says that the sequence MUST NOT throw if it doesn't support Z
                // and that it SHOULD ignore the call... PackedCoordinateSequence instances will
                // overwrite other values, so we do need to skip.
                if (hasZ)
                {
                    sequence.SetOrdinate(i, Ordinate.Z, zs.Length == 0 ? 0 : zs[k += strideZ]);
                }
            }
        }
    }
}
