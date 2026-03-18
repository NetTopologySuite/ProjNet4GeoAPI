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
