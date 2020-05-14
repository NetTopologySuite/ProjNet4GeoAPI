using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems.Projections
{
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
            AuthorityCode = 9815;
            Name = "Oblique_Mercator";
        }

        public override MathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new ObliqueMercatorProjection(_Parameters.ToProjectionParameter(), this);
            return _inverse;
        }
    }
}
