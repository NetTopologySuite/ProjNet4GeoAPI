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
/// A 1D coordinate system suitable vertical coordinates.
/// </summary>
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
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.VerticalDatum = ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum));
        this.AxisInfo = [ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo))];
        this.LinearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
    }

    /// <summary>
    /// Gets or sets the VerticalDatum.
    /// </summary>
    public VerticalDatum VerticalDatum { get; set; }

    /// <summary>
    /// Gets or sets the LinearUnit.
    /// </summary>
    public LinearUnit LinearUnit { get; set; }

    /// <summary>
    /// Gets creates a meter unit coordinate system with <see cref="VerticalDatum.ODN"/>.
    /// </summary>
    public static VerticalCoordinateSystem ODN =>
        new(
            new LinearUnit(1, "metre", "EPSG", 9001, string.Empty, "m", string.Empty),
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
    public override string XML
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendFormat(
                CultureInfo.InvariantCulture.NumberFormat,
                "<CS_CoordinateSystem Dimension=\"{0}\"><CS_VerticalCoordinateSystem>{1}",
                this.Dimension,
                this.InfoXml);
            foreach (AxisInfo ai in this.AxisInfo)
            {
                sb.Append(ai.XML);
            }

            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0}{1}</CS_VerticalCoordinateSystem></CS_CoordinateSystem>",
                this.VerticalDatum.XML,
                this.LinearUnit.XML);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets or sets the retained WKT2 vertical <c>BOUNDCRS</c> grid-binding metadata when available.
    /// </summary>
    internal VerticalBoundGridTransformation? BoundGridTransformation { get; set; }

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
            ArgumentGuard.ThrowArgument($"Vertical coordinate system '{this.Name}' must provide exactly one axis for WKT2 output.");
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
}
