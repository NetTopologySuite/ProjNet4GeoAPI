// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// Definition of angular units.
/// </summary>
public class AngularUnit : Info, IUnit
{
    /// <summary>
    /// Equality tolerance value. Values with a difference less than this are considered equal.
    /// </summary>
    private const double EqualityTolerance = 2.0e-17;
    private double radiansPerUnit;

    /// <summary>
    /// Initializes a new instance of the <see cref="AngularUnit"/> class.
    /// </summary>
    /// <param name="radiansPerUnit">Radians per unit.</param>
    public AngularUnit(double radiansPerUnit)
        : this(
        radiansPerUnit, string.Empty, string.Empty, -1, string.Empty, string.Empty, string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AngularUnit"/> class.
    /// </summary>
    /// <param name="radiansPerUnit">Radians per unit.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    internal AngularUnit(double radiansPerUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.RadiansPerUnit = radiansPerUnit;
    }

    /// <summary>
    /// Gets the degree unit of angle (1° = π/180 radians).
    /// </summary>
    public static AngularUnit Degrees => new(0.017453292519943295769236907684886, "degree", "EPSG", 9102, "deg", string.Empty, "=pi/180 radians");

    /// <summary>
    /// Gets the radian angular unit, the SI standard unit of angle.
    /// </summary>
    public static AngularUnit Radian => new(1, "radian", "EPSG", 9101, "rad", string.Empty, "SI standard unit.");

    /// <summary>
    /// Gets the grad unit of angle (1 grad = π/200 radians).
    /// </summary>
    public static AngularUnit Grad => new(0.015707963267948966192313216916398, "grad", "EPSG", 9105, "gr", string.Empty, "=pi/200 radians.");

    /// <summary>
    /// Gets the gon unit of angle (1 gon = π/200 radians; equivalent to a grad).
    /// </summary>
    public static AngularUnit Gon => new(0.015707963267948966192313216916398, "gon", "EPSG", 9106, "g", string.Empty, "=pi/200 radians.");

    /// <summary>
    /// Gets or sets the number of radians per <see cref="AngularUnit"/>.
    /// </summary>
    public double RadiansPerUnit
    {
        get => this.radiansPerUnit;
        set => this.radiansPerUnit = value;
    }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", this.Name, this.RadiansPerUnit);
            if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", AUTHORITY[\"{0}\", \"{1}\"]", this.Authority, this.AuthorityCode);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML
    {
        get
        {
            return FormattableString.Invariant($"<CS_AngularUnit RadiansPerUnit=\"{this.RadiansPerUnit}\">{this.InfoXml}</CS_AngularUnit>");
        }
    }

    /// <summary>
    /// Returns an XML representation of this angular unit as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement(
            "CS_AngularUnit",
            new XAttribute("RadiansPerUnit", this.RadiansPerUnit.ToString(CultureInfo.InvariantCulture)));
        element.Add(this.InfoXmlElement);
        return element;
    }

    /// <summary>
    /// Converts this angular unit to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this angular unit.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.RadiansPerUnit),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("UNIT", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is AngularUnit angularUnit && Math.Abs(angularUnit.RadiansPerUnit - this.RadiansPerUnit) < EqualityTolerance;
    }
}
