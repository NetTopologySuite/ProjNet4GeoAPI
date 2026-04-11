// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A one-dimensional temporal coordinate system.
/// </summary>
public sealed class TemporalCoordinateSystem : CoordinateSystem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemporalCoordinateSystem"/> class.
    /// </summary>
    /// <param name="timeUnit">Time unit.</param>
    /// <param name="temporalDatum">Temporal datum.</param>
    /// <param name="axisInfo">Axis information.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public TemporalCoordinateSystem(
        TimeUnit timeUnit,
        TemporalDatum temporalDatum,
        AxisInfo axisInfo,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks, CreateAxisInfo(axisInfo), null)
    {
        this.TimeUnit = ArgumentGuard.ThrowIfNull(timeUnit, nameof(timeUnit));
        this.TemporalDatum = ArgumentGuard.ThrowIfNull(temporalDatum, nameof(temporalDatum));
    }

    /// <summary>
    /// Gets the temporal datum.
    /// </summary>
    public TemporalDatum TemporalDatum { get; }

    /// <summary>
    /// Gets the time unit.
    /// </summary>
    public TimeUnit TimeUnit { get; }

    /// <inheritdoc />
    public override string WKT => this.ToWktNode(WktVersion.Wkt22019).ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <inheritdoc />
    public override XElement ToXml()
    {
        var innerElement = new XElement("CS_TemporalCoordinateSystem");
        innerElement.Add(this.InfoXmlElement);
        innerElement.Add(this.GetAxis(0).ToXml());
        innerElement.Add(this.TemporalDatum.ToXml());
        innerElement.Add(this.TimeUnit.ToXml());
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
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(dimension), "Temporal coordinate systems have only one dimension.");
        }

        return this.TimeUnit;
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
                this.TemporalDatum.ToWktNode(),
                this.TimeUnit.ToWktNode(),
                this.GetAxis(0).ToWktNode());
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.TemporalDatum.ToWktNode(version),
            new WktKeywordNode(
                "CS",
                new WktIdentifier("temporal"),
                new WktInteger(this.Dimension)),
            this.GetAxis(0).ToWktNode(version),
            this.TimeUnit.ToWktNode(version),
        };

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("TIMECRS", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        return obj is TemporalCoordinateSystem temporalCoordinateSystem
            && temporalCoordinateSystem.TemporalDatum.EqualParams(this.TemporalDatum)
            && temporalCoordinateSystem.TimeUnit.EqualParams(this.TimeUnit)
            && temporalCoordinateSystem.GetAxis(0).Orientation == this.GetAxis(0).Orientation;
    }

    private static List<AxisInfo> CreateAxisInfo(AxisInfo axisInfo)
        => [ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo))];
}
