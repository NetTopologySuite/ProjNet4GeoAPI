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
            if (this.Parameters != null)
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
    public Parameter[] DefaultParameters() => Array.Empty<Parameter>();

    /// <summary>
    /// Gets the parameter by its name.
    /// </summary>
    /// <param name="name">The name parameter.</param>
    /// <returns>The transformation result.</returns>
    public Parameter GetParameterByName(string name)
    {
        if (this.Parameters != null)
        {
            // search parameter collection by name
            foreach (var param in this.Parameters)
            {
                if (param != null && param.Name == name)
                {
                    return param;
                }
            }
        }

        return null;
    }
}
