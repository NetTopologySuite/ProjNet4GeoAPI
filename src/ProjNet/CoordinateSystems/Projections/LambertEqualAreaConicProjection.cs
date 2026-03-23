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

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements PROJ's <c>leac</c> projection by normalizing parameters to the Albers implementation.
/// </summary>
[Serializable]
internal sealed class LambertEqualAreaConicProjection : AlbersProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LambertEqualAreaConicProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public LambertEqualAreaConicProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private LambertEqualAreaConicProjection(IEnumerable<ProjectionParameter> parameters, LambertEqualAreaConicProjection inverse)
        : base(NormalizeParameters(parameters), inverse)
    {
        this.Name = "Lambert_Equal_Area_Conic";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new LambertEqualAreaConicProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    private static IEnumerable<ProjectionParameter> NormalizeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var normalized = new ProjectionParameterSet(parameters);
        double lat1 = normalized.GetParameterValue("lat_1", "standard_parallel_1");
        bool south = Math.Abs(normalized.GetOptionalParameterValue("south", 0d)) > 0d;

        // PROJ leac uses the pole (+90 / -90) as first standard parallel and lat_1 as second.
        normalized.SetParameterValue("lat_1", lat1);
        normalized.SetParameterValue("standard_parallel_1", south ? -90d : 90d);
        normalized.SetParameterValue("standard_parallel_2", lat1);
        return normalized.ToProjectionParameter();
    }
}
