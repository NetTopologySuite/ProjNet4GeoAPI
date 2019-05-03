using GeoAPI.Geometries;

namespace ProjNet.CoordinateSystems.Transformations
{
    /// <summary>
    /// A utility class that applies a <see cref="MathTransform"/> to a <see cref="ICoordinateSequence"/>.
    /// </summary>
    public class SequenceTransformerBase
    {
        /// <summary>
        /// Method to apply a <see cref="MathTransform"/> to a <see cref="ICoordinateSequence"/>.
        /// </summary>
        /// <param name="transform">The <see cref="MathTransform"/></param>
        /// <param name="sequence">The <see cref="ICoordinateSequence"/></param>
        public virtual void Transform(MathTransform transform, ICoordinateSequence sequence)
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
