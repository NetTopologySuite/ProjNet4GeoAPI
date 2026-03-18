namespace ProjNet.CoordinateSystems.Transformations
{
    using System;

    [Serializable]
    internal sealed class IdentityMathTransform : MathTransform
    {
        private readonly int dimension;

        internal IdentityMathTransform(int dimension)
        {
            this.dimension = dimension < 2 ? 2 : dimension;
        }

        /// <inheritdoc/>
        public override int DimSource => this.dimension;

        /// <inheritdoc/>
        public override int DimTarget => this.dimension;

        /// <inheritdoc/>
        public override bool Identity()
        {
            return true;
        }

        /// <inheritdoc/>
        public override string WKT => $"PARAM_MT[\"Identity\",PARAMETER[\"dimension\",{this.dimension}]]";

        /// <inheritdoc/>
        public override string XML => throw new NotImplementedException();

        /// <inheritdoc/>
        public override MathTransform Inverse()
        {
            return this;
        }

        /// <inheritdoc/>
        public override void Invert()
        {
        }

        /// <inheritdoc/>
        public override void Transform(ref double x, ref double y, ref double z)
        {
        }
    }
}
