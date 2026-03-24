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
using System.Globalization;

/// <summary>
/// Details of axis. This is used to label axes, and indicate the orientation.
/// </summary>
[Serializable]
public class AxisInfo
{
    private string name;
    private AxisOrientationEnum orientation;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxisInfo"/> class.
    /// Initializes a new instance of an AxisInfo.
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
