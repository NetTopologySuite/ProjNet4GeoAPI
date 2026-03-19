// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.CoordinateSystems.Projections
{
    using System;
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems.Transformations;

    /// <summary>
    /// Represents the documented type.
    /// </summary>
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
