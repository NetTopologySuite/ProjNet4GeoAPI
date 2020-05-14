using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems.Projections
{
    [Serializable] 
    internal class PseudoMercator : Mercator
    {
        public PseudoMercator(IEnumerable<ProjectionParameter> parameters)
            :this(parameters, null)
        {
            
        }
        protected PseudoMercator(IEnumerable<ProjectionParameter> parameters, Mercator inverse)
            :base(VerifyParameters(parameters), inverse)
        {
            Name = "Pseudo-Mercator";
            Authority = "EPSG";
            AuthorityCode = 3856;
        }

        private static IEnumerable<ProjectionParameter> VerifyParameters(IEnumerable<ProjectionParameter> parameters)
        {
            var p = new ProjectionParameterSet(parameters);
            double semi_major = p.GetParameterValue("semi_major");
            p.SetParameterValue("semi_minor", semi_major);
            p.SetParameterValue("scale_factor", 1);

            return p.ToProjectionParameter();
        }

        public override MathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new PseudoMercator(_Parameters.ToProjectionParameter(), this);
            return _inverse;
        }
    }
}
