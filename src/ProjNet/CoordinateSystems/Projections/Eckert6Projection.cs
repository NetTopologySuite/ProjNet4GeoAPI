// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;

/// <summary>
/// Implements the spherical Eckert VI projection (<c>eck6</c>).
/// </summary>
[Serializable]
internal class Eckert6Projection : GeneralSinusoidalProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert6Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public Eckert6Projection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Eckert6Projection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public Eckert6Projection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(MergeParameters(parameters), inverse)
    {
        this.Name = "Eckert_VI";
    }

    private static List<ProjectionParameter> MergeParameters(IEnumerable<ProjectionParameter> parameters)
    {
        List<ProjectionParameter> merged = CloneParametersList(parameters);
        ReplaceOrAdd(merged, "m", 1d);
        ReplaceOrAdd(merged, "n", 2.570796326794896619231321691d);
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
