using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNET.Tests.Geometries.Implementation
{
    /// <summary>
    /// A utility class that applies a <see cref="MathTransform"/> to a <see cref="CoordinateSequence"/>.
    /// </summary>
    public class SequenceTransformerBase
    {
        /// <summary>
        /// Method to apply a <see cref="MathTransform"/> to a <see cref="CoordinateSequence"/>.
        /// </summary>
        /// <param name="transform">The <see cref="MathTransform"/></param>
        /// <param name="sequence">The <see cref="CoordinateSequence"/></param>
        public virtual void Transform(MathTransform transform, CoordinateSequence sequence)
        {
            bool readZ = sequence.HasZ && transform.DimSource > 2;
            bool writeZ = sequence.HasZ && transform.DimTarget > 2;
            for (int i = 0; i < sequence.Count; i++)
            {
                double x = sequence.GetX(i);
                double y = sequence.GetY(i);
                double z = readZ ? sequence.GetZ(i) : 0d;
                (x, y, z) = transform.Transform(x, y, z);
                sequence.SetOrdinate(i, Ordinate.X, x);
                sequence.SetOrdinate(i, Ordinate.Y, y);

                if (writeZ) sequence.SetOrdinate(i, Ordinate.Z, z);
            }
        }
    }
}
