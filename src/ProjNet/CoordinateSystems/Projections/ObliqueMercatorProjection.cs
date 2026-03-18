namespace ProjNet.CoordinateSystems.Projections
{
    using System;
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems.Transformations;

    [Serializable]
    internal class ObliqueMercatorProjection : HotineObliqueMercatorProjection
    {
        public ObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public ObliqueMercatorProjection(IEnumerable<ProjectionParameter> parameters, ObliqueMercatorProjection inverse)
            : base(parameters, inverse)
        {
            this.AuthorityCode = 9815;
            this.Name = "Oblique_Mercator";
        }

        /// <inheritdoc/>
        public override MathTransform Inverse()
        {
            if (this.inverse == null)
            {
                this.inverse = new ObliqueMercatorProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }
    }
}
