// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Implements the spherical Putnins P6' projection (<c>putp6p</c>).
/// </summary>
/// <remarks>
/// This projection specializes <see cref="PutninsP6Projection"/> with the Putnins P6'
/// parameter set, so its numerical behavior follows the same verified base formulation.
/// </remarks>
internal class PutninsP6PrimeProjection : PutninsP6Projection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP6PrimeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public PutninsP6PrimeProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PutninsP6PrimeProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public PutninsP6PrimeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Putnins_P6P";
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        List<ProjectionParameter> merged = CloneParametersList(parameters);
        ReplaceOrAdd(merged, "putp6_cx", 0.44329d);
        ReplaceOrAdd(merged, "putp6_cy", 0.80404d);
        ReplaceOrAdd(merged, "putp6_a", 6d);
        ReplaceOrAdd(merged, "putp6_b", 5.61125d);
        ReplaceOrAdd(merged, "putp6_d", 3d);
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
