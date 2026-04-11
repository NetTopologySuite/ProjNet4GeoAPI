// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A local engineering datum used by engineering coordinate reference systems.
/// </summary>
public sealed class EngineeringDatum : Datum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EngineeringDatum"/> class.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    public EngineeringDatum(string name, string authority, long authorityCode, string alias, string remarks, string abbreviation)
        : base(DatumType.LdEngineering, name, authority, authorityCode, alias, remarks, abbreviation)
    {
    }

    /// <inheritdoc />
    public override string WKT => this.ToWktNode().ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Returns an XML representation of this engineering datum as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement(
            "CS_EngineeringDatum",
            new XAttribute("DatumType", ((int)this.DatumType).ToString(CultureInfo.InvariantCulture)));
        element.Add(this.InfoXmlElement);
        return element;
    }

    /// <summary>
    /// Converts this engineering datum to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this engineering datum.</returns>
    public WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            new WktInteger((int)this.DatumType),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("LOCAL_DATUM", children);
    }

    /// <summary>
    /// Converts this engineering datum to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this engineering datum in the requested WKT version.</returns>
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
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("EDATUM", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is EngineeringDatum engineeringDatum && base.EqualParams(engineeringDatum);
    }
}
