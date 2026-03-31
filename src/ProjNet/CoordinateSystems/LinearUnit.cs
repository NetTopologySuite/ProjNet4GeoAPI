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
/// Definition of linear units.
/// </summary>
public class LinearUnit : Info, IUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LinearUnit"/> class.
    /// </summary>
    /// <param name="metersPerUnit">Number of meters per <see cref="LinearUnit" />.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public LinearUnit(double metersPerUnit, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.MetersPerUnit = metersPerUnit;
    }

    /// <summary>
    /// Gets the meters linear unit.
    /// Also known as International metre. SI standard unit.
    /// </summary>
    public static LinearUnit Metre => new(1.0, "metre", "EPSG", 9001, "m", string.Empty, "Also known as International metre. SI standard unit.");

    /// <summary>
    /// Gets the foot linear unit (1ft = 0.3048m).
    /// </summary>
    public static LinearUnit Foot => new(0.3048, "foot", "EPSG", 9002, "ft", string.Empty, string.Empty);

    /// <summary>
    /// Gets the US Survey foot linear unit (1ftUS = 0.304800609601219m).
    /// </summary>
    public static LinearUnit USSurveyFoot => new(0.304800609601219, "US survey foot", "EPSG", 9003, "American foot", "ftUS", "Used in USA.");

    /// <summary>
    /// Gets the Nautical Mile linear unit (1NM = 1852m).
    /// </summary>
    public static LinearUnit NauticalMile => new(1852, "nautical mile", "EPSG", 9030, "NM", string.Empty, string.Empty);

    /// <summary>
    /// Gets clarke's foot.
    /// </summary>
    /// <remarks>
    /// Assumes Clarke's 1865 ratio of 1 British foot = 0.3047972654 French legal metres applies to the international metre.
    /// Used in older Australian, southern African &amp; British West Indian mapping.
    /// </remarks>
    public static LinearUnit ClarkesFoot => new(0.3047972654, "Clarke's foot", "EPSG", 9005, "Clarke's foot", string.Empty, "Assumes Clarke's 1865 ratio of 1 British foot = 0.3047972654 French legal metres applies to the international metre. Used in older Australian, southern African & British West Indian mapping.");

    /// <summary>
    /// Gets or sets the number of meters per <see cref="LinearUnit"/>.
    /// </summary>
    public double MetersPerUnit { get; set; }

    /// <summary>
    /// Gets the Well-known text for this object
    /// as defined in the simple features specification.
    /// </summary>
    public override string WKT
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, "UNIT[\"{0}\", {1}", this.Name, this.MetersPerUnit);
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
            return FormattableString.Invariant($"<CS_LinearUnit MetersPerUnit=\"{this.MetersPerUnit}\">{this.InfoXml}</CS_LinearUnit>");
        }
    }

    /// <summary>
    /// Returns an XML representation of this linear unit as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement(
            "CS_LinearUnit",
            new XAttribute("MetersPerUnit", this.MetersPerUnit.ToString(CultureInfo.InvariantCulture)));
        element.Add(this.InfoXmlElement);
        return element;
    }

    /// <summary>
    /// Converts this linear unit to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this linear unit.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.MetersPerUnit),
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
        return obj is LinearUnit linearUnit && linearUnit.MetersPerUnit == this.MetersPerUnit;
    }
}
