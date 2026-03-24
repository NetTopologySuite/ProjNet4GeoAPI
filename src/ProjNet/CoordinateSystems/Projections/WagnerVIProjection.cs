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
/// Implements the spherical Wagner VI projection (<c>wag6</c>).
/// </summary>
[Serializable]
internal class WagnerVIProjection : Eckert3Projection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WagnerVIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public WagnerVIProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WagnerVIProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public WagnerVIProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Wagner_VI";
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var merged = CloneParametersList(parameters);
        ReplaceOrAdd(merged, "eck3_a", 0d);
        ReplaceOrAdd(merged, "eck3_b", 0.30396355092701331433d);
        ReplaceOrAdd(merged, "eck3_cx", 1d);
        ReplaceOrAdd(merged, "eck3_cy", 1d);
        return merged;
    }

    private static void ReplaceOrAdd(List<ProjectionParameter> parameters, string name, double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(parameters[i].Name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }
}


