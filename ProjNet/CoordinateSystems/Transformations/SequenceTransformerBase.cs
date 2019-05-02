using GeoAPI.Geometries;

namespace ProjNet.CoordinateSystems.Transformations
{
    public class SequenceTransformerBase
    {
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
