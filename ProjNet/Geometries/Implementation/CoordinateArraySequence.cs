using System;
using GeoAPI.Geometries;

namespace ProjNet.Geometries.Implementation
{
    internal sealed class CoordinateArraySequence : ICoordinateSequence
    {
        
        private readonly Coordinate[] coordinates;

        internal CoordinateArraySequence(Coordinate[] coordinates) => this.coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));

        internal CoordinateArraySequence(int size, bool hasZ)
        {
            coordinates = new Coordinate[size];
            for (var i = 0; i < size; i++)
                coordinates[i] = hasZ ? new Coordinate() : new CoordinateZ();
        }

        public int Dimension => coordinates.Length > 0 ? coordinates[0] is CoordinateZ ? 3 : 2 : 3;
        public int Measures => 0;

        public Ordinates Ordinates => HasZ ? Ordinates.XYZ : Ordinates.XY;
        public bool HasZ => Dimension == 3;
        public bool HasM => false;
        public int Count => this.coordinates.Length;

        public double GetX(int index) => this.coordinates[index].X;
        public double GetY(int index) => this.coordinates[index].Y;
        public double GetZ(int index) => this.coordinates[index].Z;
        public double GetM(int index) => throw new NotSupportedException();

        public Coordinate GetCoordinateCopy(int i) => this.coordinates[i].Copy();

        public Coordinate CreateCoordinate() => new CoordinateZ();

        public Coordinate GetCoordinate(int i) => this.coordinates[i];

        public ICoordinateSequence Copy() => new CoordinateArraySequence(Array.ConvertAll(this.coordinates, c => c.Copy()));
        public Coordinate[] ToCoordinateArray() => this.coordinates;

        public Envelope ExpandEnvelope(Envelope env)
        {
            foreach (Coordinate coordinate in this.coordinates)
            {
                env.ExpandToInclude(coordinate);
            }

            return env;
        }

        public void GetCoordinate(int index, Coordinate coord)
        {
            coord.X = this.coordinates[index].X;
            coord.Y = this.coordinates[index].Y;
            coord.Z = this.coordinates[index].Z;
        }

        public double GetOrdinate(int index, Ordinate ordinate)
        {
            Coordinate coordinate = this.coordinates[index];
            switch (ordinate)
            {
                case Ordinate.X:
                    return coordinate.X;

                case Ordinate.Y:
                    return coordinate.Y;

                case Ordinate.Z:
                    return coordinate.Z;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetOrdinate(int index, Ordinate ordinate, double value)
        {
            Coordinate coordinate = this.coordinates[index];
            switch (ordinate)
            {
                case Ordinate.X:
                    coordinate.X = value;
                    break;

                case Ordinate.Y:
                    coordinate.Y = value;
                    break;

                case Ordinate.Z:
                    coordinate.Z = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}