namespace ProjNet.CoordinateSystems.Projections
{
    using System;
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems.Transformations;

    [Serializable]
    internal class PseudoMercator : Mercator
    {
        public PseudoMercator(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {

        }

        protected PseudoMercator(IEnumerable<ProjectionParameter> parameters, Mercator inverse)
            : base(VerifyParameters(parameters), inverse)
        {
            this.Name = "Pseudo-Mercator";
            this.Authority = "EPSG";
            this.AuthorityCode = 3856;
        }

        private static IEnumerable<ProjectionParameter> VerifyParameters(IEnumerable<ProjectionParameter> parameters)
        {
            var p = new ProjectionParameterSet(parameters);
            double semi_major = p.GetParameterValue("semi_major");
            p.SetParameterValue("semi_minor", semi_major);
            p.SetParameterValue("scale_factor", 1);

            return p.ToProjectionParameter();
        }

        /// <inheritdoc/>
        public override MathTransform Inverse()
        {
            if (this.inverse == null)
            {
                this.inverse = new PseudoMercator(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }
    }
}
