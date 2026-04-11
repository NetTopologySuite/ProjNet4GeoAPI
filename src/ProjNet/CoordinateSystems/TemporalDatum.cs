// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A temporal datum used by temporal coordinate reference systems.
/// </summary>
public sealed class TemporalDatum : Datum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemporalDatum"/> class.
    /// </summary>
    /// <param name="timeOrigin">Time origin as declared in the WKT2 <c>TIMEORIGIN</c> block.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    public TemporalDatum(string timeOrigin, string name, string authority, long authorityCode, string alias, string remarks, string abbreviation)
        : base(DatumType.TdOther, name, authority, authorityCode, alias, remarks, abbreviation)
    {
        this.TimeOrigin = string.IsNullOrWhiteSpace(timeOrigin)
            ? ArgumentGuard.ThrowArgument<string>("Temporal datums require a time origin.", nameof(timeOrigin))
            : timeOrigin;
    }

    /// <summary>
    /// Gets or sets the declared time origin.
    /// </summary>
    public string TimeOrigin { get; set; }

    /// <inheritdoc />
    public override string WKT => this.ToWktNode().ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Returns an XML representation of this temporal datum as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement("CS_TemporalDatum", new XAttribute("TimeOrigin", this.TimeOrigin));
        element.Add(this.InfoXmlElement);
        return element;
    }

    /// <summary>
    /// Converts this temporal datum to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this temporal datum.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("LOCAL_DATUM", children);
    }

    /// <summary>
    /// Converts this temporal datum to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this temporal datum in the requested WKT version.</returns>
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
            new WktKeywordNode("TIMEORIGIN", new WktQuotedString(this.TimeOrigin)),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("TDATUM", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is TemporalDatum temporalDatum
            && base.EqualParams(temporalDatum)
            && string.Equals(this.TimeOrigin, temporalDatum.TimeOrigin, StringComparison.Ordinal);
    }
}
