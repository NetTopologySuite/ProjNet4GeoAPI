using System;

namespace ProjNet.CoordinateSystems.Transformations
{
    [Serializable]
    internal sealed class IdentityMathTransform : MathTransform
    {
        private readonly int _dimension;

        internal IdentityMathTransform(int dimension)
        {
            _dimension = dimension < 2 ? 2 : dimension;
        }

        public override int DimSource => _dimension;

        public override int DimTarget => _dimension;

        public override bool Identity()
        {
            return true;
        }

        public override string WKT => $"PARAM_MT[\"Identity\",PARAMETER[\"dimension\",{_dimension}]]";

        public override string XML => throw new NotImplementedException();

        public override MathTransform Inverse()
        {
            return this;
        }

        public override void Invert()
        {
        }

        public override void Transform(ref double x, ref double y, ref double z)
        {
        }
    }
}
