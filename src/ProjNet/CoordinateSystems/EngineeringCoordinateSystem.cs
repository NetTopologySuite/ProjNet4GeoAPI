// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ProjNet.IO.Wkt;

/// <summary>
/// A coordinate system for local engineering reference frames.
/// </summary>
public sealed class EngineeringCoordinateSystem : CoordinateSystem
{
    private readonly List<IUnit> units;

    /// <summary>
    /// Initializes a new instance of the <see cref="EngineeringCoordinateSystem"/> class.
    /// </summary>
    /// <param name="engineeringDatum">Engineering datum.</param>
    /// <param name="coordinateSystemType">Coordinate system type from the WKT2 <c>CS</c> block.</param>
    /// <param name="axisInfo">Axis definitions.</param>
    /// <param name="units">Axis units.</param>
    /// <param name="name">Name.</param>
    /// <param name="authority">Authority name.</param>
    /// <param name="authorityCode">Authority-specific identification code.</param>
    /// <param name="alias">Alias.</param>
    /// <param name="abbreviation">Abbreviation.</param>
    /// <param name="remarks">Provider-supplied remarks.</param>
    public EngineeringCoordinateSystem(
        EngineeringDatum engineeringDatum,
        string coordinateSystemType,
        IReadOnlyList<AxisInfo> axisInfo,
        IReadOnlyList<IUnit> units,
        string name,
        string authority,
        long authorityCode,
        string alias,
        string abbreviation,
        string remarks)
        : base(name, authority, authorityCode, alias, abbreviation, remarks)
    {
        this.EngineeringDatum = ArgumentGuard.ThrowIfNull(engineeringDatum, nameof(engineeringDatum));
        this.CoordinateSystemType = string.IsNullOrWhiteSpace(coordinateSystemType)
            ? ArgumentGuard.ThrowArgument<string>("Engineering coordinate systems require a CS type.", nameof(coordinateSystemType))
            : coordinateSystemType;

        axisInfo = ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));
        units = ArgumentGuard.ThrowIfNull(units, nameof(units));
        if (axisInfo.Count == 0)
        {
            ArgumentGuard.ThrowArgument("Engineering coordinate systems require at least one axis.", nameof(axisInfo));
        }

        if (axisInfo.Count != units.Count)
        {
            ArgumentGuard.ThrowArgument("Engineering coordinate system axes and units must have the same length.");
        }

        this.AxisInfo = axisInfo.Select(axis => new AxisInfo(axis)).ToList();
        this.units = units.Select(unit => ArgumentGuard.ThrowIfNull(unit, nameof(units))).ToList();
    }

    /// <summary>
    /// Gets the engineering datum.
    /// </summary>
    public EngineeringDatum EngineeringDatum { get; }

    /// <summary>
    /// Gets the WKT2 coordinate system type from the <c>CS</c> block.
    /// </summary>
    public string CoordinateSystemType { get; }

    /// <summary>
    /// Gets the units for each engineering axis.
    /// </summary>
    public IReadOnlyList<IUnit> AxisUnits => this.units;

    /// <inheritdoc />
    public override string WKT => this.ToWktNode(WktVersion.Wkt22019).ToString();

    /// <inheritdoc />
    public override string XML => this.ToXml().ToString(SaveOptions.DisableFormatting);

    /// <inheritdoc />
    public override XElement ToXml()
    {
        var innerElement = new XElement(
            "CS_EngineeringCoordinateSystem",
            new XAttribute("CoordinateSystemType", this.CoordinateSystemType));
        innerElement.Add(this.InfoXmlElement);
        foreach (AxisInfo axis in this.AxisInfo)
        {
            innerElement.Add(axis.ToXml());
        }

        innerElement.Add(this.EngineeringDatum.ToXml());
        foreach (IUnit unit in this.units)
        {
            innerElement.Add(CreateUnitXml(unit));
        }

        return new XElement(
            "CS_CoordinateSystem",
            new XAttribute("Dimension", this.Dimension.ToString(CultureInfo.InvariantCulture)),
            innerElement);
    }

    /// <inheritdoc />
    public override IUnit GetUnits(int dimension)
    {
        if (dimension < 0 || dimension >= this.units.Count)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(nameof(dimension), "Engineering coordinate system dimension is out of range.");
        }

        return this.units[dimension];
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
            return this.CreateLegacyWktNode();
        }

        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.EngineeringDatum.ToWktNode(version),
            new WktKeywordNode(
                "CS",
                new WktIdentifier(this.CoordinateSystemType),
                new WktInteger(this.Dimension)),
        };

        bool shareUnit = this.units.Count > 0 && this.units.All(unit => UnitsEqual(this.units[0], unit));
        for (int i = 0; i < this.AxisInfo.Count; i++)
        {
            children.Add(shareUnit
                ? this.AxisInfo[i].ToWktNode(version)
                : CreateAxisNodeWithUnit(this.AxisInfo[i], this.units[i]));
        }

        if (shareUnit)
        {
            children.Add(CreateWkt2UnitNode(this.units[0]));
        }

        WktKeywordNode? idNode = WktVersionSupport.CreateIdNode(this.Authority, this.AuthorityCode);
        if (idNode is not null)
        {
            children.Add(idNode);
        }

        return new WktKeywordNode("ENGCRS", children);
    }

    /// <inheritdoc />
    public override bool EqualParams(object obj)
    {
        if (obj is not EngineeringCoordinateSystem engineeringCoordinateSystem)
        {
            return false;
        }

        if (!this.EngineeringDatum.EqualParams(engineeringCoordinateSystem.EngineeringDatum)
            || !string.Equals(this.CoordinateSystemType, engineeringCoordinateSystem.CoordinateSystemType, StringComparison.OrdinalIgnoreCase)
            || this.Dimension != engineeringCoordinateSystem.Dimension
            || this.units.Count != engineeringCoordinateSystem.units.Count)
        {
            return false;
        }

        for (int i = 0; i < this.Dimension; i++)
        {
            if (this.GetAxis(i).Orientation != engineeringCoordinateSystem.GetAxis(i).Orientation
                || !this.units[i].EqualParams(engineeringCoordinateSystem.units[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool UnitsEqual(IUnit left, IUnit right)
    {
        return left.GetType() == right.GetType() && left.EqualParams(right);
    }

    private static XElement CreateUnitXml(IUnit unit)
    {
        return unit switch
        {
            AngularUnit angularUnit => angularUnit.ToXml(),
            LinearUnit linearUnit => linearUnit.ToXml(),
            TimeUnit timeUnit => timeUnit.ToXml(),
            ParametricUnit parametricUnit => parametricUnit.ToXml(),
            Unit genericUnit => genericUnit.ToXml(),
            _ => throw new NotSupportedException($"Engineering coordinate system XML output does not support unit type '{unit.GetType().Name}'."),
        };
    }

    private static WktNode CreateWkt2UnitNode(IUnit unit)
    {
        return unit switch
        {
            AngularUnit angularUnit => angularUnit.ToWktNode(WktVersion.Wkt22019),
            LinearUnit linearUnit => linearUnit.ToWktNode(WktVersion.Wkt22019),
            TimeUnit timeUnit => timeUnit.ToWktNode(WktVersion.Wkt22019),
            ParametricUnit parametricUnit => parametricUnit.ToWktNode(WktVersion.Wkt22019),
            Unit genericUnit => new WktKeywordNode(
                "SCALEUNIT",
                new WktQuotedString(genericUnit.Name),
                new WktNumber(genericUnit.ConversionFactor)),
            _ => throw new NotSupportedException($"Engineering coordinate system WKT2 output does not support unit type '{unit.GetType().Name}'."),
        };
    }

    private static WktKeywordNode CreateAxisNodeWithUnit(AxisInfo axisInfo, IUnit unit)
    {
        return new WktKeywordNode(
            "AXIS",
            new WktQuotedString(axisInfo.Name),
            new WktIdentifier(GetOrientationIdentifier(axisInfo.Orientation)),
            CreateWkt2UnitNode(unit));
    }

    private static string GetOrientationIdentifier(AxisOrientationEnum orientation)
    {
        return orientation switch
        {
            AxisOrientationEnum.North => "north",
            AxisOrientationEnum.South => "south",
            AxisOrientationEnum.East => "east",
            AxisOrientationEnum.West => "west",
            AxisOrientationEnum.Up => "up",
            AxisOrientationEnum.Down => "down",
            _ => "other",
        };
    }

    private static WktKeywordNode CreateLegacyUnitNode(Unit unit)
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(unit.Name),
            new WktNumber(unit.ConversionFactor),
        };

        if (!string.IsNullOrWhiteSpace(unit.Authority) && unit.AuthorityCode > 0)
        {
            children.Add(new WktKeywordNode(
                "AUTHORITY",
                new WktQuotedString(unit.Authority),
                new WktQuotedString(unit.AuthorityCode.ToString(CultureInfo.InvariantCulture))));
        }

        return new WktKeywordNode("UNIT", children);
    }

    private WktKeywordNode CreateLegacyWktNode()
    {
        var children = new List<WktNode>
        {
            new WktQuotedString(this.Name),
            this.EngineeringDatum.ToWktNode(),
            this.units[0] switch
            {
                AngularUnit angularUnit => angularUnit.ToWktNode(),
                LinearUnit linearUnit => linearUnit.ToWktNode(),
                TimeUnit timeUnit => timeUnit.ToWktNode(),
                ParametricUnit parametricUnit => parametricUnit.ToWktNode(),
                Unit genericUnit => CreateLegacyUnitNode(genericUnit),
                _ => throw new NotSupportedException($"Engineering coordinate system legacy WKT output does not support unit type '{this.units[0].GetType().Name}'."),
            },
        };

        for (int i = 0; i < this.AxisInfo.Count; i++)
        {
            children.Add(this.AxisInfo[i].ToWktNode());
        }

        return new WktKeywordNode("LOCAL_CS", children);
    }
}
