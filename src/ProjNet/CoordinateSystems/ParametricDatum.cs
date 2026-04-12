// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A parametric datum used by parametric coordinate reference systems.
/// </summary>
public sealed class ParametricDatum : Datum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParametricDatum"/> class.
    /// </summary>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    public ParametricDatum(string name, string authority, long authorityCode, string alias, string remarks, string abbreviation)
        : base(DatumType.PD_Other, name, authority, authorityCode, alias, remarks, abbreviation)
    {
    }

    /// <inheritdoc />
    public override string WKT => this.ToWktNode().ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Creates a copy of this datum with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="ParametricDatum"/> with updated authority metadata.</returns>
    public new ParametricDatum WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this datum with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="ParametricDatum"/> with the updated name.</returns>
    public new ParametricDatum WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

    /// <summary>
    /// Creates a copy of this datum with updated retained datum-ensemble metadata.
    /// </summary>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to keep this datum non-ensemble-backed.</param>
    /// <returns>A new <see cref="ParametricDatum"/> with updated ensemble metadata.</returns>
    /// <exception cref="System.NotSupportedException">Thrown when <paramref name="ensemble"/> is not <see langword="null"/>.</exception>
    public new ParametricDatum WithEnsemble(DatumEnsemble? ensemble) => InfoAuthorityCloneHelper.CloneWithEnsemble(this, ensemble);

    /// <summary>
    /// Returns an XML representation of this parametric datum as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public XElement ToXml()
    {
        var element = new XElement("CS_ParametricDatum");
        element.Add(this.InfoXmlElement);
        return element;
    }

    /// <summary>
    /// Converts this parametric datum to a WKT syntax tree node.
    /// </summary>
    /// <returns>A <see cref="WktNode"/> representing this parametric datum.</returns>
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
    /// Converts this parametric datum to a WKT syntax tree node for the requested WKT version.
    /// </summary>
    /// <param name="version">The WKT dialect to emit.</param>
    /// <returns>A <see cref="WktNode"/> representing this parametric datum in the requested WKT version.</returns>
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

        return new WktKeywordNode("PDATUM", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is ParametricDatum parametricDatum && base.EqualParams(parametricDatum);
    }
}
