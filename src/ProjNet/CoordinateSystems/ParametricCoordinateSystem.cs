// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A one-dimensional parametric coordinate system.
/// </summary>
public sealed class ParametricCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParametricCoordinateSystem"/> class.
    /// </summary>
    /// <param name="parametricUnit">Parametric unit.</param>
    /// <param name="parametricDatum">Parametric datum.</param>
    /// <param name="axisInfo">Axis information.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public ParametricCoordinateSystem(
        ParametricUnit parametricUnit,
        ParametricDatum parametricDatum,
        AxisInfo axisInfo,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.ParametricUnit = ArgumentGuard.ThrowIfNull(parametricUnit, nameof(parametricUnit));
        this.ParametricDatum = ArgumentGuard.ThrowIfNull(parametricDatum, nameof(parametricDatum));
        this.AxisInfo = [ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo))];
    }

    /// <summary>
    /// Gets the parametric datum.
    /// </summary>
    public ParametricDatum ParametricDatum { get; }

    /// <summary>
    /// Gets the parametric unit.
    /// </summary>
    public ParametricUnit ParametricUnit { get; }

    /// <inheritdoc />
    public override string WKT => this.ToWktNode(WktVersion.Wkt22019).ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <inheritdoc />
    public override XElement ToXml()
    {
        var innerElement = new XElement("CS_ParametricCoordinateSystem");
        innerElement.Add(this.InfoXmlElement);
        innerElement.Add(this.GetAxis(0).ToXml());
        innerElement.Add(this.ParametricDatum.ToXml());
        innerElement.Add(this.ParametricUnit.ToXml());
        return new XElement(
            "CS_CoordinateSystem",
            new XAttribute("Dimension", this.Dimension.ToString(CultureInfo.InvariantCulture)),
            innerElement);
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension)
    {
        if (dimension != 0)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(dimension), "Parametric coordinate systems have only one dimension.");
        }

        return this.ParametricUnit;
    }

    /// <inheritdoc />
    public override WktNode ToWktNode()
    {
        return this.ToWktNode(WktVersion.Wkt22019);
    }

    /// <inheritdoc />
    public override WktNode ToWktNode(WktVersion version)
    {
        WktVersionSupport.ThrowIfUnknown(version);
        if (version == WktVersion.Wkt1)
        {
            return new WktKeywordNode(
                "LOCAL_CS",
                new WktQuotedString(this.Name),
                this.ParametricDatum.ToWktNode(),
                this.ParametricUnit.ToWktNode(),
                this.GetAxis(0).ToWktNode());
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.ParametricDatum.ToWktNode(version),
            new WktKeywordNode(
                "CS",
                new WktIdentifier("parametric"),
                new WktInteger(this.Dimension)),
            this.GetAxis(0).ToWktNode(version),
            this.ParametricUnit.ToWktNode(version),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("PARAMETRICCRS", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is ParametricCoordinateSystem parametricCoordinateSystem
            && parametricCoordinateSystem.ParametricDatum.EqualParams(this.ParametricDatum)
            && parametricCoordinateSystem.ParametricUnit.EqualParams(this.ParametricUnit)
            && parametricCoordinateSystem.GetAxis(0).Orientation == this.GetAxis(0).Orientation;
    }
}
