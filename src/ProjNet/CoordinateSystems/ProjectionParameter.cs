// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

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
public sealed class ProjectionParameter
{
    private string name = string.Empty;
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
        get => FormattableString.Invariant($"PARAMETER[\"{this.Name}\", {this.Value}]");
    }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public string XML
    {
        get
        {
            return FormattableString.Invariant($"<CS_ProjectionParameter Name=\"{this.Name}\" Value=\"{this.Value}\"/>");
        }
    }

    /// <summary>
    /// Returns an XML representation of this projection parameter as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        return new XElement(
            "CS_ProjectionParameter",
            new XAttribute("Name", this.Name),
            new XAttribute("Value", this.Value.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Converts this projection parameter to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this projection parameter.</returns>
    public WktNode ToWktNode()
    {
        return new WktKeywordNode(
            "PARAMETER",
            new WktQuotedString(this.Name),
            new WktNumber(this.Value));
    }

    /// <summary>
    /// Converts this projection parameter to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this projection parameter in the requested WKT version.</returns>
    public WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        return version == WktVersion.Wkt1
            ? this.ToWktNode()
            : throw WktVersionSupport.CreateNotSupportedException(nameof(ProjectionParameter), version);
    }

    /// <summary>
    /// Returns a string representation of this projection parameter.
    /// </summary>
    /// <returns>A string in the format <c>ProjectionParameter 'name': value</c>.</returns>
    public override string ToString() => $"ProjectionParameter '{this.Name}': {this.Value}";
}
