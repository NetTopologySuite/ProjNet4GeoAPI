// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Werenskiold I projection (<c>weren</c>).
/// </summary>
[Serializable]
internal class WerenskioldProjection : PutninsP4PProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WerenskioldProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public WerenskioldProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WerenskioldProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public WerenskioldProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Werenskiold_I";
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        var merged = CloneParametersList(parameters);
        ReplaceOrAdd(merged, "putp4p_cx", 1d);
        ReplaceOrAdd(merged, "putp4p_cy", 4.442882938d);
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
