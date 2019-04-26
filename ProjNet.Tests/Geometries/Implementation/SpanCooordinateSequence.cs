using System;
using GeoAPI.Geometries;

namespace ProjNET.Tests.Geometries.Implementation
{
    public class SpanCoordinateSequence : ICoordinateSequence
    {
        private readonly int _dimension;
        private readonly int _measures;
        private readonly Ordinates _ordinates;
        private readonly int[] _ordinateIndirection;
        private readonly double[] _ordinateValues;
        private readonly Coordinate _createCoordinateTemplate;

        private Coordinate[] _cached;

        internal SpanCoordinateSequence(int dimension, int measures, double[] ordinateValues)
        {
            Count = ordinateValues.Length / dimension;

            _dimension = dimension;
            _measures = measures;
            _ordinates = Ordinates.XY;
            if (HasZ) _ordinates |= Ordinates.Z;
            if (HasM) _ordinates |= Ordinates.M;
            _ordinateIndirection = new int[HasZ ? dimension : dimension + 1];
            for (int i = 1; i < _ordinateIndirection.Length; i++)
            {
                if (i == (int)Ordinate.Z && !HasZ)
                    _ordinateIndirection[i] = -1;
                else
                    _ordinateIndirection[i] = i * Count;
            }

            _ordinateValues = ordinateValues;
            _createCoordinateTemplate = CreateCoordinateTemplate();
        }

        private SpanCoordinateSequence(int dimension, int measures, Ordinates ordinates, double[] ordinateValues, int[] ordinateIndirection, Coordinate createCoordinateTemplate)
        {
            Count = ordinateValues.Length / dimension;
            _dimension = dimension;
            _measures = measures;
            _ordinateValues = ordinateValues;
            _ordinates = ordinates;
            _ordinateIndirection = ordinateIndirection;
            _createCoordinateTemplate = createCoordinateTemplate;
        }
        /*
        public ReadOnlySpan<double> XsAsReadonlySpan()
        {
            return new ReadOnlySpan<double>(_ordinateValues, _ordinateIndirection[(int)Ordinate.X], Count);
        }
        public ReadOnlySpan<double> YsAsReadonlySpan()
        {
            return new ReadOnlySpan<double>(_ordinateValues, _ordinateIndirection[(int)Ordinate.Y], Count);
        }
        public ReadOnlySpan<double> ZsAsReadOnlySpan()
        {
            if (HasZ)
                return new ReadOnlySpan<double>(_ordinateValues, _ordinateIndirection[(int)Ordinate.Z], Count);
            return default;
        }
        */
        public Span<double> XsAsSpan()
        {
            return new Span<double>(_ordinateValues, _ordinateIndirection[(int)Ordinate.X], Count);
        }
        public Span<double> YsAsSpan()
        {
            return new Span<double>(_ordinateValues, _ordinateIndirection[(int)Ordinate.Y], Count);
        }
        public Span<double> ZsAsSpan()
        {
            if (HasZ)
                return new Span<double>(_ordinateValues, _ordinateIndirection[(int)Ordinate.Z], Count);
            return default;
        }

        public Coordinate CreateCoordinate() => _createCoordinateTemplate.Copy();

        private Coordinate CreateCoordinateTemplate()
        {
            switch (Ordinates)
            {
                case Ordinates.XYZM:
                    return new CoordinateZM();
                case Ordinates.XYZ:
                    return new CoordinateZ();
                case Ordinates.XYM:
                    return new CoordinateM();
            }
            return new Coordinate();
        }


        public Coordinate GetCoordinate(int i)
        {
            var res = CreateCoordinate();
            res.X = _ordinateValues[_ordinateIndirection[(int) Ordinate.X] + i];
            res.Y = _ordinateValues[_ordinateIndirection[(int) Ordinate.Y] + i];
            if (HasZ) res.Z = _ordinateValues[_ordinateIndirection[(int) Ordinate.Z] + i];
            if (HasM) res.M = _ordinateValues[_ordinateIndirection[(int) Ordinate.M] + i];

            return res;
        }

        public Coordinate GetCoordinateCopy(int i)
        {
            return GetCoordinate(i);
        }

        public void GetCoordinate(int i, Coordinate coord)
        {
            coord.X = _ordinateValues[_ordinateIndirection[(int)Ordinate.X] + i];
            coord.Y = _ordinateValues[_ordinateIndirection[(int)Ordinate.Y] + i];
            if (coord is CoordinateZ)
                coord.Z = HasZ ? _ordinateValues[_ordinateIndirection[(int)Ordinate.Z] + i] : _createCoordinateTemplate.Z;
            if (coord is CoordinateM || coord is CoordinateZM)
                coord.M = HasM ? _ordinateValues[_ordinateIndirection[(int)Ordinate.M] + i] : _createCoordinateTemplate.M;
        }

        public double GetX(int i)
        {
            return _ordinateValues[_ordinateIndirection[(int)Ordinate.X] + i];
        }

        public double GetY(int i)
        {
            return _ordinateValues[_ordinateIndirection[(int)Ordinate.Y] + i];
        }

        public double GetZ(int i)
        {
            return HasZ ? _ordinateValues[_ordinateIndirection[(int)Ordinate.Z] + i] : _createCoordinateTemplate.M;

        }

        public double GetM(int i)
        {
            return HasZ ? _ordinateValues[_ordinateIndirection[(int)Ordinate.Z] + i] : _createCoordinateTemplate.M;
        }

        public double GetOrdinate(int index, Ordinate ordinate)
        {
            if (index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if ((int) ordinate >= _ordinateIndirection.Length)
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            if (_ordinateIndirection[(int) ordinate] < 0)
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            return _ordinateValues[_ordinateIndirection[(int) ordinate] + index];
        }

        public void SetOrdinate(int index, Ordinate ordinate, double value)
        {
            if (index > Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if ((int)ordinate >= _ordinateIndirection.Length)
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            if (_ordinateIndirection[(int)ordinate] < 0)
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            _ordinateValues[_ordinateIndirection[(int) ordinate] + index] = value;
            _cached = null;
        }

        public Coordinate[] ToCoordinateArray()
        {
            if (_cached != null)
                return _cached;
            var res = new Coordinate[Count];
            for (int i = 0; i < Count; i++)
                res[i] = GetCoordinate(i);
            return _cached = res;
        }

        public Envelope ExpandEnvelope(Envelope env)
        {
            //env = env.Copy();
            for (int i = 0, j = _ordinateIndirection[0], k = _ordinateIndirection[1]; i < Count; i++)
                env.ExpandToInclude(_ordinateValues[j++], _ordinateValues[k++]);
            return env;
        }

        public ICoordinateSequence Copy()
        {
            return new SpanCoordinateSequence(_dimension, _measures, _ordinates, (double[])_ordinateValues.Clone(), _ordinateIndirection, _createCoordinateTemplate);
        }

        public int Dimension => _dimension;

        public int Measures => _measures;

        public Ordinates Ordinates => _ordinates;

        public bool HasZ => _dimension - _measures > 2;

        public bool HasM => _measures > 0;

        public int Count { get; }
    }
}