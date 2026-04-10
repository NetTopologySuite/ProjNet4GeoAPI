// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// Details of axis. This is used to label axes, and indicate the orientation.
/// </summary>
public sealed class AxisInfo
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
    /// Initializes a new instance of the <see cref="AxisInfo"/> class by copying an existing axis definition.
    /// </summary>
    /// <param name="axisInfo">The axis definition to copy.</param>
    public AxisInfo(AxisInfo axisInfo)
    {
        axisInfo = ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));
        this.Name = axisInfo.Name;
        this.Orientation = axisInfo.Orientation;
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
            return FormattableString.Invariant($"<CS_AxisInfo Name=\"{this.Name}\" Orientation=\"{this.Orientation.ToString().ToUpperInvariant()}\"/>");
        }
    }

    /// <summary>
    /// Returns an XML representation of this axis info as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        return new XElement(
            "CS_AxisInfo",
            new XAttribute("Name", this.Name),
            new XAttribute("Orientation", this.Orientation.ToString().ToUpperInvariant()));
    }

    /// <summary>
    /// Converts this axis info to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this axis info.</returns>
    public WktNode ToWktNode()
    {
        return new WktKeywordNode(
            "AXIS",
            new WktQuotedString(this.Name),
            new WktIdentifier(this.Orientation.ToString().ToUpperInvariant()));
    }

    /// <summary>
    /// Converts this axis info to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this axis info in the requested WKT version.</returns>
    public WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        return version switch
        {
            WktVersion.Wkt1 => this.ToWktNode(),
            WktVersion.Wkt22019 => new WktKeywordNode(
                "AXIS",
                new WktQuotedString(this.Name),
                new WktIdentifier(this.Orientation switch
                {
                    AxisOrientationEnum.North => "north",
                    AxisOrientationEnum.South => "south",
                    AxisOrientationEnum.East => "east",
                    AxisOrientationEnum.West => "west",
                    AxisOrientationEnum.Up => "up",
                    AxisOrientationEnum.Down => "down",
                    _ => "other",
                })),
            _ => throw WktVersionSupport.CreateNotSupportedException(nameof(AxisInfo), version),
        };
    }
}
