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
/// Implements PROJ's <c>ups</c> projection with fixed UPS defaults.
/// </summary>
[Serializable]
internal sealed class UpsProjection : PolarStereographicProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public UpsProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    private UpsProjection(IEnumerable<ProjectionParameter> parameters, UpsProjection inverse)
        : base(NormalizeParameters(parameters), inverse)
    {
        this.Name = "Universal_Polar_Stereographic";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new UpsProjection(this.Parameters.ToProjectionParameter(), this);
        }

        return this.inverse;
    }

    private static IEnumerable<ProjectionParameter> NormalizeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var normalized = new ProjectionParameterSet(parameters);
        bool south = Math.Abs(normalized.GetOptionalParameterValue("south", 0d)) > 0d;

        normalized.SetParameterValue("latitude_of_origin", south ? -90d : 90d);
        normalized.SetParameterValue("central_meridian", 0d);
        normalized.SetParameterValue("scale_factor", 0.994d);
        normalized.SetParameterValue("false_easting", 2000000d);
        normalized.SetParameterValue("false_northing", 2000000d);
        return normalized.ToProjectionParameter();
    }
}
