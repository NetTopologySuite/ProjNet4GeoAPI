// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// Definition of temporal units.
/// </summary>
public sealed class TimeUnit : Info, IUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeUnit"/> class.
    /// </summary>
    /// <param name="conversionFactor">Number of seconds per time unit.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public TimeUnit(double conversionFactor, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.ConversionFactor = conversionFactor;
    }

    /// <summary>
    /// Gets the number of seconds per time unit.
    /// </summary>
    public double ConversionFactor { get; }

    /// <summary>
    /// Gets the Well-known text for this object.
    /// </summary>
    public override string WKT => this.ToWktNode().ToString();

    /// <summary>
    /// Gets an XML representation of this object.
    /// </summary>
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Returns an XML representation of this time unit as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement(
            "CS_TimeUnit",
            new XAttribute("SecondsPerUnit", this.ConversionFactor.ToString(CultureInfo.InvariantCulture)));
        element.Add(this.InfoXmlElement);
        return element;
    }

    /// <summary>
    /// Converts this time unit to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this time unit.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktNumber(this.ConversionFactor),
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
    /// Converts this time unit to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this time unit in the requested WKT version.</returns>
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
            new WktNumber(this.ConversionFactor),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("TIMEUNIT", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is TimeUnit timeUnit && timeUnit.ConversionFactor == this.ConversionFactor;
    }
}
