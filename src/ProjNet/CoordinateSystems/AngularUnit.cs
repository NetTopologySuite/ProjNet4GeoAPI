// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// Definition of angular units.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// The predefined angular-unit accessors are thread-safe because they only expose immutable value objects.
/// </para>
/// </remarks>
public class AngularUnit : Info, IUnit
{
    /// <summary>
    /// Equality tolerance value. Values with a difference less than this are considered equal.
    /// </summary>
    private const double EqualityTolerance = 2.0e-17;

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
    /// Gets the number of radians per <see cref="AngularUnit"/>.
    /// </summary>
    public double RadiansPerUnit { get; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT => this.ToWktNode().ToString();

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Creates a copy of this unit with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="AngularUnit"/> with updated authority metadata.</returns>
    public new AngularUnit WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this unit with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="AngularUnit"/> with the updated name.</returns>
    public new AngularUnit WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

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

    /// <summary>
    /// Converts this angular unit to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this angular unit in the requested WKT version.</returns>
    public WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            return this.ToWktNode();
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.RadiansPerUnit),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("ANGLEUNIT", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is AngularUnit angularUnit && Math.Abs(angularUnit.RadiansPerUnit - this.RadiansPerUnit) < EqualityTolerance;
    }
}
