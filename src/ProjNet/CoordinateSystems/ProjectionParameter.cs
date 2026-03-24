// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;

/// <summary>
/// A named projection parameter value.
/// </summary>
/// <remarks>
/// The linear units of parameters' values match the linear units of the containing
/// projected coordinate system. The angular units of parameter values match the
/// angular units of the geographic coordinate system that the projected coordinate
/// system is based on. (Notice that this is different from <see cref="Parameter"/>,
/// where the units are always meters and degrees.)
/// </remarks>
[Serializable]
public class ProjectionParameter
{
    private string name;
    private double val;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionParameter"/> class.
    /// </summary>
    /// <param name="name">Name of parameter.</param>
    /// <param name="value">Parameter value.</param>
    public ProjectionParameter(string name, double value)
    {
        this.Name = name;
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets parameter name.
    /// </summary>
    public string Name
    {
        get { return this.name; }
        set { this.name = value; }
    }

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    /// <remarks>
    /// The linear units of parameter values match the linear units of the containing
    /// projected coordinate system. The angular units of parameter values match the
    /// angular units of the underlying geographic coordinate system.
    /// </remarks>
    public double Value
    {
        get { return this.val; }
        set { this.val = value; }
    }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public string WKT
    {
        get => string.Format(CultureInfo.InvariantCulture.NumberFormat, "PARAMETER[\"{0}\", {1}]", this.Name, this.Value);
    }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public string XML
    {
        get
        {
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, "<CS_ProjectionParameter Name=\"{0}\" Value=\"{1}\"/>", this.Name, this.Value);
        }
    }

    /// <summary>
    /// Returns a string representation of this projection parameter.
    /// </summary>
    /// <returns>A string in the format <c>ProjectionParameter 'name': value</c>.</returns>
    public override string ToString() => $"ProjectionParameter '{this.Name}': {this.Value}";
}
