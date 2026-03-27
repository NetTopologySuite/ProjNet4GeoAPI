// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Wagner V projection (<c>wag5</c>).
/// </summary>
[Serializable]
internal class Wagner5Projection : MollweideProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Wagner5Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Wagner5Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Wagner5Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Wagner_V";
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var merged = CloneParametersList(parameters);
        ReplaceOrAdd(merged, "moll_cx", 0.90977d);
        ReplaceOrAdd(merged, "moll_cy", 1.65014d);
        ReplaceOrAdd(merged, "moll_cp", 3.00896d);
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
