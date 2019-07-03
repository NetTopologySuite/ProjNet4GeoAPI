using System;
using NetTopologySuite.Geometries;

namespace ProjNET.Tests.Geometries.Implementation
{
    public class SpanCoordinateSequence : CoordinateSequence
    {
        private readonly int[] _ordinateIndirection;
        private readonly double[] _ordinateValues;
        private readonly Coordinate _createCoordinateTemplate;

        private Coordinate[] _cached;

        internal SpanCoordinateSequence(int dimension, int measures, double[] ordinateValues)
            :base(ordinateValues.Length / dimension, dimension, measures)
        {
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
            :base(ordinateValues.Length / dimension,  dimension, measures)
        {
            _ordinateValues = ordinateValues;
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

        public override Coordinate CreateCoordinate() => _createCoordinateTemplate.Copy();

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


        public override Coordinate GetCoordinate(int i)
        {
            var res = CreateCoordinate();
            res.X = _ordinateValues[_ordinateIndirection[(int) Ordinate.X] + i];
            res.Y = _ordinateValues[_ordinateIndirection[(int) Ordinate.Y] + i];
            if (HasZ) res.Z = _ordinateValues[_ordinateIndirection[(int) Ordinate.Z] + i];
            if (HasM) res.M = _ordinateValues[_ordinateIndirection[(int) Ordinate.M] + i];

            return res;
        }

        public override Coordinate GetCoordinateCopy(int i)
        {
            return GetCoordinate(i);
        }

        public override void GetCoordinate(int i, Coordinate coord)
        {
            coord.X = _ordinateValues[_ordinateIndirection[(int)Ordinate.X] + i];
            coord.Y = _ordinateValues[_ordinateIndirection[(int)Ordinate.Y] + i];
            if (coord is CoordinateZ)
                coord.Z = HasZ ? _ordinateValues[_ordinateIndirection[(int)Ordinate.Z] + i] : _createCoordinateTemplate.Z;
            if (coord is CoordinateM || coord is CoordinateZM)
                coord.M = HasM ? _ordinateValues[_ordinateIndirection[(int)Ordinate.M] + i] : _createCoordinateTemplate.M;
        }

        public override double GetX(int i)
        {
            return _ordinateValues[_ordinateIndirection[(int)Ordinate.X] + i];
        }

        public override double GetY(int i)
        {
            return _ordinateValues[_ordinateIndirection[(int)Ordinate.Y] + i];
        }

        public override double GetZ(int i)
        {
            return HasZ ? _ordinateValues[_ordinateIndirection[(int)Ordinate.Z] + i] : _createCoordinateTemplate.Z;
        }

        public override double GetM(int i)
        {
            return HasM ? _ordinateValues[_ordinateIndirection[(int)Ordinate.M] + i] : _createCoordinateTemplate.M;
        }

        public override double GetOrdinate(int index, int ordinate)
        {
            if (unchecked((uint)index >= (uint)Count))
                throw new ArgumentOutOfRangeException(nameof(index));

            if (unchecked((uint)ordinate >= (uint)_ordinateIndirection.Length))
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            if (_ordinateIndirection[ordinate] < 0)
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            return _ordinateValues[_ordinateIndirection[ordinate] + index];
        }

        public override void SetOrdinate(int index, int ordinate, double value)
        {
            if (unchecked((uint)index >= (uint)Count))
                throw new ArgumentOutOfRangeException(nameof(index));

            if (unchecked((uint)ordinate >= (uint)_ordinateIndirection.Length))
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            if (_ordinateIndirection[ordinate] < 0)
                throw new ArgumentOutOfRangeException(nameof(ordinate));

            _ordinateValues[_ordinateIndirection[ordinate] + index] = value;
            _cached = null;
        }

        public override Coordinate[] ToCoordinateArray()
        {
            if (_cached != null)
                return _cached;
            var res = new Coordinate[Count];
            for (int i = 0; i < Count; i++)
                res[i] = GetCoordinate(i);
            return _cached = res;
        }

        public override Envelope ExpandEnvelope(Envelope env)
        {
            //env = env.Copy();
            for (int i = 0, j = _ordinateIndirection[0], k = _ordinateIndirection[1]; i < Count; i++)
                env.ExpandToInclude(_ordinateValues[j++], _ordinateValues[k++]);
            return env;
        }

        public override CoordinateSequence Copy()
        {
            return new SpanCoordinateSequence(Dimension, Measures, Ordinates, (double[])_ordinateValues.Clone(), _ordinateIndirection, _createCoordinateTemplate);
        }
    }
}
