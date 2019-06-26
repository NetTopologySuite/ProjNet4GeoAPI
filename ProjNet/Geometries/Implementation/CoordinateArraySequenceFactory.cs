using GeoAPI.Geometries;

namespace ProjNet.Geometries.Implementation
{
    internal sealed class CoordinateArraySequenceFactory : ICoordinateSequenceFactory
    {
        public ICoordinateSequence Create(Coordinate[] coordinates)
        {
            if (coordinates == null || coordinates.Length == 0)
                return new CoordinateArraySequence(new Coordinate[0]);
            return new CoordinateArraySequence(coordinates);
        }

        public ICoordinateSequence Create(ICoordinateSequence coordSeq)
        {
            var res = new CoordinateArraySequence(coordSeq.Count, coordSeq.HasZ);
            for (int i = 0; i < coordSeq.Count; i++)
            {
                res.SetOrdinate(i, Ordinate.X, coordSeq.GetX(i));
                res.SetOrdinate(i, Ordinate.Y, coordSeq.GetY(i));
                if (res.HasZ)
                    res.SetOrdinate(i, Ordinate.Z, coordSeq.GetZ(i));
            }

            return res;
        }

        public ICoordinateSequence Create(int size, int dimension)
        {
            return Create(size, dimension, 0);
        }

        public ICoordinateSequence Create(int size, int dimension, int measures)
        {
            var arr = new Coordinate[size];
            for (int i = 0; i < size; i++)
            {
                if (dimension < 2) dimension = 2;
                arr[i] = new Coordinate();
                if (dimension > 3) dimension = 3;
                arr[i] = new CoordinateZ();
            }

            return new CoordinateArraySequence(arr);

        }

        public ICoordinateSequence Create(int size, Ordinates ordinates)
        {
            ordinates &= Ordinates.XYZ;
            var dimension = 2;
            if ((ordinates & Ordinates.Z) == Ordinates.Z) dimension++;
            return Create(size, dimension, 0);
        }

        public Ordinates Ordinates => Ordinates.XYZ;
    }
}
