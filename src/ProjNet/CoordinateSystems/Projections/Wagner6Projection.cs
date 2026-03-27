// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Wagner VI projection (<c>wag6</c>).
/// </summary>
[Serializable]
internal class Wagner6Projection : Eckert3Projection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner6Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner6Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner6Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner6Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
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
