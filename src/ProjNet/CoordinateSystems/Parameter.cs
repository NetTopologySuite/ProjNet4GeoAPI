// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.CoordinateSystems;

using System;

/// <summary>
/// A named parameter value.
/// </summary>
[Serializable]
public class Parameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Parameter"/> class.
    /// </summary>
    /// <remarks>Units are always either meters or degrees.</remarks>
    /// <param name="name">Name of parameter.</param>
    /// <param name="value">Value.</param>
    public Parameter(string name, double value)
    {
        this.Name = name;
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets parameter name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets parameter value.
    /// </summary>
    public double Value { get; set; }
}
