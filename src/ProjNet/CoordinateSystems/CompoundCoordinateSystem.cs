// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// This is a compound coordinate system, which combines the coordinate of two other coordinate systems.
/// For example, a compound 3D coordinate system could be made up of a
/// horizontal coordinate system and a vertical coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// </para>
/// </remarks>
public class CompoundCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundCoordinateSystem"/> class.
    /// A compound coordinate system.
    /// </summary>
    /// <param name="headcs">The head (first) coordinate system.</param>
    /// <param name="tailcs">The tail (second) coordinate system.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Optional information.</param>
    public CompoundCoordinateSystem(CoordinateSystem headcs, CoordinateSystem tailcs, string name, string authority, long authorityCode, string alias, string abbreviation, string remarks)
        : this(headcs, tailcs, name, authority, authorityCode, alias, abbreviation, remarks, CreateAxisInfo(headcs, tailcs), null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundCoordinateSystem"/> class with an explicit default envelope.
    /// </summary>
    /// <param name="headcs">The head (first) coordinate system.</param>
    /// <param name="tailcs">The tail (second) coordinate system.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Optional information.</param>
    /// <param name="defaultEnvelope">Default envelope for the compound domain.</param>
    internal CompoundCoordinateSystem(
        CoordinateSystem headcs,
        CoordinateSystem tailcs,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks,
        double[]? defaultEnvelope)
        : this(headcs, tailcs, name, authority, authorityCode, alias, abbreviation, remarks, CreateAxisInfo(headcs, tailcs), defaultEnvelope)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundCoordinateSystem"/> class with explicit axis metadata.
    /// </summary>
    /// <param name="headcs">The head (first) coordinate system.</param>
    /// <param name="tailcs">The tail (second) coordinate system.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Optional information.</param>
    /// <param name="axisInfo">Axis definitions.</param>
    /// <param name="defaultEnvelope">Default envelope for the compound domain.</param>
    internal CompoundCoordinateSystem(
        CoordinateSystem headcs,
        CoordinateSystem tailcs,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks,
        List<AxisInfo> axisInfo,
        double[]? defaultEnvelope = null)
        : base(name, authority, authorityCode, alias, abbreviation, remarks, axisInfo, defaultEnvelope)
    {
        this.HeadCoordinateSystem = headcs;
        this.TailCoordinateSystem = tailcs;
    }

    /// <summary>
    /// Gets the head coordinate system.
    /// </summary>
    public CoordinateSystem HeadCoordinateSystem { get; }

    /// <summary>
    /// Gets the tail coordinate system.
    /// </summary>
    public CoordinateSystem TailCoordinateSystem { get; }

    /// <inheritdoc/>
    public override string WKT => this.ToWktNode().ToString();

    /// <inheritdoc/>
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Creates a copy of this coordinate system with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="CompoundCoordinateSystem"/> with updated authority metadata.</returns>
    public new CompoundCoordinateSystem WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this coordinate system with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="CompoundCoordinateSystem"/> with the updated name.</returns>
    public new CompoundCoordinateSystem WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

    /// <summary>
    /// Returns an XML representation of this compound coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public override XElement ToXml()
    {
        var innerElement = new XElement("CS_CompoundCoordinateSystem");
        innerElement.Add(this.InfoXmlElement);
        foreach (AxisInfo ai in this.AxisInfo)
        {
            innerElement.Add(ai.ToXml());
        }

        innerElement.Add(this.HeadCoordinateSystem.ToXml());
        innerElement.Add(this.TailCoordinateSystem.ToXml());

        return new XElement(
            "CS_CoordinateSystem",
            new XAttribute("Dimension", this.Dimension.ToString(CultureInfo.InvariantCulture)),
            innerElement);
    }

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.HeadCoordinateSystem.ToWktNode(),
            this.TailCoordinateSystem.ToWktNode(),
        };

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("COMPD_CS", children);
    }

    /// <inheritdoc />
    public override WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            return this.ToWktNode();
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.HeadCoordinateSystem.ToWktNode(version),
            this.TailCoordinateSystem.ToWktNode(version),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("COMPOUNDCRS", children);
    }

    /// <inheritdoc/>
    public override bool EqualParams(object obj)
    {
        return obj is CompoundCoordinateSystem compdCs && this.HeadCoordinateSystem.EqualParams(compdCs.HeadCoordinateSystem) && this.TailCoordinateSystem.EqualParams(compdCs.TailCoordinateSystem);
    }

    /// <inheritdoc/>
    public override IUnit GetUnits(int dimension)
    {
        if (dimension < 0 || dimension >= this.Dimension)
        {
            ArgumentGuard.ThrowArgument("Dimension not valid", nameof(dimension));
        }

        return dimension < this.HeadCoordinateSystem.Dimension
            ? this.HeadCoordinateSystem.GetUnits(dimension)
            : this.TailCoordinateSystem.GetUnits(dimension - this.HeadCoordinateSystem.Dimension);
    }

    private static List<AxisInfo> CreateAxisInfo(CoordinateSystem headcs, CoordinateSystem tailcs)
    {
        headcs = ArgumentGuard.ThrowIfNull(headcs, nameof(headcs));
        tailcs = ArgumentGuard.ThrowIfNull(tailcs, nameof(tailcs));

        var axisInfo = new List<AxisInfo>(headcs.Dimension + tailcs.Dimension);
        axisInfo.AddRange(headcs.AxisInfo);
        axisInfo.AddRange(tailcs.AxisInfo);
        return axisInfo;
    }
}
