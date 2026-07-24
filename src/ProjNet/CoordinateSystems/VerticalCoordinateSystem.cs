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
/// A 1D coordinate system suitable vertical coordinates.
/// </summary>
/// <remarks>
/// <para>
/// Thread safety: Instances are immutable after construction and may be shared across threads.
/// </para>
/// </remarks>
public class VerticalCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalCoordinateSystem"/> class.
    /// Creates an instance of a VerticalCoordinateSystem.
    /// </summary>
    /// <param name="linearUnit">The linear unit.</param>
    /// <param name="verticalDatum">The vertical datum.</param>
    /// <param name="axisInfo">Axis information.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public VerticalCoordinateSystem(
        LinearUnit linearUnit,
        VerticalDatum verticalDatum,
        AxisInfo axisInfo,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : this(linearUnit, verticalDatum, CreateSingleAxisInfo(axisInfo), name, authority, authorityCode, alias, abbreviation, remarks)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VerticalCoordinateSystem"/> class with explicit axis metadata.
    /// </summary>
    /// <param name="linearUnit">The linear unit.</param>
    /// <param name="verticalDatum">The vertical datum.</param>
    /// <param name="axisInfo">Axis information.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    /// <param name="defaultEnvelope">Default envelope for the coordinate system domain.</param>
    /// <param name="boundGridTransformation">Retained WKT2 vertical bound-grid metadata for this coordinate system.</param>
    internal VerticalCoordinateSystem(
        LinearUnit linearUnit,
        VerticalDatum verticalDatum,
        List<AxisInfo> axisInfo,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks,
        double[]? defaultEnvelope = null,
        VerticalBoundGridTransformation? boundGridTransformation = null)
        : base(name, authority, authorityCode, alias, abbreviation, remarks, ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo)), defaultEnvelope)
    {
        this.VerticalDatum = ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum));
        this.LinearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        this.BoundGridTransformation = boundGridTransformation;
    }

    /// <summary>
    /// Gets the VerticalDatum.
    /// </summary>
    public VerticalDatum VerticalDatum { get; }

    /// <summary>
    /// Gets the LinearUnit.
    /// </summary>
    public LinearUnit LinearUnit { get; }

    /// <summary>
    /// Gets creates a meter unit coordinate system with <see cref="VerticalDatum.ODN"/>.
    /// </summary>
    public static VerticalCoordinateSystem ODN =>
        new(
            LinearUnit.Metre,
            VerticalDatum.ODN,
            new AxisInfo("Up", AxisOrientationEnum.Up),
            "Newlyn",
            "EPSG",
            5701,
            string.Empty,
            "ODN",
            string.Empty);

    /// <inheritdoc/>
    public override string WKT => this.ToWktNode().ToString();

    /// <inheritdoc/>
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <summary>
    /// Gets the retained WKT2 vertical <c>BOUNDCRS</c> grid-binding metadata when available.
    /// </summary>
    internal VerticalBoundGridTransformation? BoundGridTransformation { get; }

    /// <summary>
    /// Creates a copy of this coordinate system with updated authority metadata.
    /// </summary>
    /// <param name="authority">Replacement authority name.</param>
    /// <param name="code">Replacement authority-specific identification code.</param>
    /// <returns>A new <see cref="VerticalCoordinateSystem"/> with updated authority metadata.</returns>
    public new VerticalCoordinateSystem WithAuthority(string authority, long code) => InfoAuthorityCloneHelper.CloneWithAuthority(this, authority, code);

    /// <summary>
    /// Creates a copy of this coordinate system with an updated name.
    /// </summary>
    /// <param name="name">Replacement name.</param>
    /// <returns>A new <see cref="VerticalCoordinateSystem"/> with the updated name.</returns>
    public new VerticalCoordinateSystem WithName(string name) => InfoAuthorityCloneHelper.CloneWithName(this, name);

    /// <summary>
    /// Returns an XML representation of this vertical coordinate system as an <see cref="XElement"/>.
    /// </summary>
    /// <returns>An <see cref="XElement"/> containing the XML representation.</returns>
    public override XElement ToXml()
    {
        var innerElement = new XElement("CS_VerticalCoordinateSystem");
        innerElement.Add(this.InfoXmlElement);
        foreach (AxisInfo ai in this.AxisInfo)
        {
            innerElement.Add(ai.ToXml());
        }

        innerElement.Add(this.VerticalDatum.ToXml());
        innerElement.Add(this.LinearUnit.ToXml());

        return new XElement(
            "CS_CoordinateSystem",
            new XAttribute("Dimension", this.Dimension.ToString(CultureInfo.InvariantCulture)),
            innerElement);
    }

    /// <inheritdoc/>
    public override bool EqualParams(object obj)
    {
        if (obj is not VerticalCoordinateSystem vcs)
        {
            return false;
        }

        if (vcs.Dimension != this.Dimension)
        {
            return false;
        }

        for (int i = 0; i < vcs.AxisInfo.Count; i++)
        {
            if (vcs.AxisInfo[i].Orientation != this.AxisInfo[i].Orientation)
            {
                return false;
            }
        }

        return vcs.LinearUnit.EqualParams(this.LinearUnit) &&
                vcs.VerticalDatum.EqualParams(this.VerticalDatum);
    }

    /// <inheritdoc/>
    public override IUnit GetUnits(int dimension)
    {
        if (dimension != 0)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(dimension), "Vertical Coordinate Systems have only one dimension");
        }

        return this.LinearUnit;
    }

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.VerticalDatum.ToWktNode(),
            this.LinearUnit.ToWktNode(),
        };

        // Skip axis info if they contain default values
        if (this.AxisInfo.Count != 1 ||
            this.AxisInfo[0].Name != "Up" || this.AxisInfo[0].Orientation != AxisOrientationEnum.Up)
        {
            children.Add(this.GetAxis(0).ToWktNode());
        }

        if (!string.IsNullOrWhiteSpace(this.Authority) && this.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(this.Authority),
                new WktQuotedString(this.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("VERT_CS", children);
    }

    /// <inheritdoc />
    public override WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            return this.ToWktNode();
        }

        if (this.BoundGridTransformation is not null)
        {
            BoundCoordinateSystem boundCoordinateSystem = BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(this)
                ?? throw new NotSupportedException("WKT2 VERTCRS output for retained bound-grid metadata could not be normalized to BOUNDCRS.");
            return BoundCoordinateSystemSupport.CreateWkt2BoundCoordinateSystemNode(boundCoordinateSystem);
        }

        if (this.AxisInfo.Count != 1)
        {
            throw new InvalidOperationException($"Vertical coordinate system '{this.Name}' must provide exactly one axis for WKT2 output.");
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.VerticalDatum.ToWktNode(version),
            new WktKeywordNode(
                "CS",
                new WktIdentifier("vertical"),
                new WktInteger(this.Dimension)),
            this.GetAxis(0).ToWktNode(version),
            this.LinearUnit.ToWktNode(version),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("VERTCRS", children);
    }

    /// <summary>
    /// Creates a copy of this vertical coordinate system with retained WKT2 bound-grid metadata.
    /// </summary>
    /// <param name="boundGridTransformation">The bound-grid metadata to attach to the clone.</param>
    /// <returns>A cloned coordinate system carrying the supplied bound-grid metadata.</returns>
    internal VerticalCoordinateSystem WithBoundGridTransformation(VerticalBoundGridTransformation boundGridTransformation)
    {
        boundGridTransformation = ArgumentGuard.ThrowIfNull(boundGridTransformation, nameof(boundGridTransformation));
        return new VerticalCoordinateSystem(
            this.LinearUnit,
            this.VerticalDatum,
            CloneAxisInfo(this.AxisInfo),
            this.Name,
            this.Authority,
            this.AuthorityCode,
            this.Alias,
            this.Abbreviation,
            this.Remarks,
            this.DefaultEnvelope,
            boundGridTransformation);
    }

    private static List<AxisInfo> CreateSingleAxisInfo(AxisInfo axisInfo)
        => [ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo))];

    private static List<AxisInfo> CloneAxisInfo(List<AxisInfo> axisInfo)
    {
        var clone = new List<AxisInfo>(axisInfo.Count);
        for (int i = 0; i < axisInfo.Count; i++)
        {
            clone.Add(new AxisInfo(axisInfo[i]));
        }

        return clone;
    }
}
