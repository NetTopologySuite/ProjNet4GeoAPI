// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;

/// <summary>
/// Simple class that implements the IParameterInfo interface for providing general set of the parameters.
/// It allows discovering the names, and for setting and getting parameter values.
/// </summary>
[Serializable]
internal class ParameterInfo
{
    /// <summary>
    /// Gets the number of parameters expected.
    /// </summary>
    public int NumParameters
    {
        get
        {
            if (this.Parameters is not null)
            {
                return this.Parameters.Count;
            }

            return 0;
        }
    }

    /// <summary>
    /// Gets or sets the parameters set for this projection.
    /// </summary>
    public List<Parameter> Parameters
    {
        get;
        set;
    }

    /// <summary>
    /// Returns the default parameters for this projection.
    /// </summary>
    /// <returns>The transformation result.</returns>
    public Parameter[] DefaultParameters() => [];

    /// <summary>
    /// Gets the parameter by its name.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <returns>The transformation result.</returns>
    public Parameter GetParameterByName(string name)
    {
        if (this.Parameters is not null)
        {
            // search parameter collection by name
            foreach (var param in this.Parameters)
            {
                if (param is not null && param.Name == name)
                {
                    return param;
                }
            }
        }

        return null;
    }
}
