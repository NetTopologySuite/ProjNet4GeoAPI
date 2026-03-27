// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;

/// <summary>
/// Details of axis. This is used to label axes, and indicate the orientation.
/// </summary>
[Serializable]
public class AxisInfo
{
    private string name = string.Empty;
    private AxisOrientationEnum orientation;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxisInfo"/> class.
    /// </summary>
    /// <param name="name">Name of axis.</param>
    /// <param name="orientation">Axis orientation.</param>
    public AxisInfo(string name, AxisOrientationEnum orientation)
    {
        this.Name = name;
        this.Orientation = orientation;
    }

    /// <summary>
    /// Gets or sets human readable name for axis. Possible values are X, Y, Long, Lat or any other short string.
    /// </summary>
    public string Name
    {
        get => this.name;
        set => this.name = value;
    }

    /// <summary>
    /// Gets or sets enumerated value for orientation.
    /// </summary>
    public AxisOrientationEnum Orientation
    {
        get => this.orientation;
        set => this.orientation = value;
    }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public string WKT
    {
        get
        {
            return $"AXIS[\"{this.Name}\", {this.Orientation.ToString().ToUpperInvariant()}]";
        }
    }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public string XML
    {
        get
        {
            return string.Format(
                CultureInfo.InvariantCulture.NumberFormat,
                "<CS_AxisInfo Name=\"{0}\" Orientation=\"{1}\"/>",
                this.Name,
                this.Orientation.ToString()
                .ToUpperInvariant());
        }
    }
}
