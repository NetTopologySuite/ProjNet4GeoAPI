// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt;

/// <summary>
/// Creates an object based on the supplied Well Known Text (WKT).
/// </summary>
public static partial class CoordinateSystemWktReader
{
    /// <summary>
    /// Returns a <see cref="Unit"/> from a WKT node.
    /// </summary>
    /// <param name="node">The parsed unit node.</param>
    /// <returns>An object that implements the IUnit interface.</returns>
    private static Unit ReadUnit(WktKeywordNode node)
    {
        return ReadWkt1UnitFromNode(
            node,
            static (unitsPerUnit, unitName, authority, authorityCode) => new Unit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    /// <summary>
    /// Returns a <see cref="LinearUnit"/> from a WKT node.
    /// </summary>
    /// <param name="node">The parsed unit node.</param>
    /// <returns>An object that implements the IUnit interface.</returns>
    private static LinearUnit ReadLinearUnit(WktKeywordNode node)
    {
        return ReadWkt1UnitFromNode(
            node,
            static (unitsPerUnit, unitName, authority, authorityCode) => new LinearUnit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    /// <summary>
    /// Returns a <see cref="AngularUnit"/> from a WKT node.
    /// </summary>
    /// <param name="node">The parsed unit node.</param>
    /// <returns>An object that implements the IUnit interface.</returns>
    private static AngularUnit ReadAngularUnit(WktKeywordNode node)
    {
        return ReadWkt1UnitFromNode(
            node,
            static (unitsPerUnit, unitName, authority, authorityCode) => new AngularUnit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    /// <summary>
    /// Returns a <see cref="AxisInfo"/> from a WKT node.
    /// </summary>
    /// <param name="node">The parsed axis node.</param>
    /// <returns>An AxisInfo object.</returns>
    private static AxisInfo ReadAxis(WktKeywordNode node)
    {
        string axisName = node.GetStringChild(0);
        string unitname = node.GetIdentifierChild(1);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is WktKeywordNode keywordChild)
            {
                throw new NotSupportedException($"WKT1 AXIS keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return unitname.ToUpperInvariant() switch
        {
            "DOWN" => new AxisInfo(axisName, AxisOrientationEnum.Down),
            "EAST" => new AxisInfo(axisName, AxisOrientationEnum.East),
            "NORTH" => new AxisInfo(axisName, AxisOrientationEnum.North),
            "OTHER" => new AxisInfo(axisName, AxisOrientationEnum.Other),
            "SOUTH" => new AxisInfo(axisName, AxisOrientationEnum.South),
            "UP" => new AxisInfo(axisName, AxisOrientationEnum.Up),
            "WEST" => new AxisInfo(axisName, AxisOrientationEnum.West),
            _ => ThrowWktParseException<AxisInfo>($"Invalid axis name '{unitname}' in WKT"),
        };
    }

    private static TUnit ReadWkt1UnitFromNode<TUnit>(WktKeywordNode node, Func<double, string, string, long, TUnit> factory)
    {
        string unitName = node.GetStringChild(0);
        double unitsPerUnit = node.GetNumberChild(1);
        string authority = string.Empty;
        long authorityCode = -1;

        (string Authority, string Code)? authorityNode = node.GetAuthority();
        if (authorityNode.HasValue)
        {
            authority = authorityNode.Value.Authority;
            authorityCode = long.TryParse(authorityNode.Value.Code, NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedCode)
                ? parsedCode
                : -1;
        }

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is WktKeywordNode keywordChild && !string.Equals(keywordChild.Keyword, "AUTHORITY", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"WKT1 {node.Keyword} keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return factory(unitsPerUnit, unitName, authority, authorityCode);
    }

    // Reads either 3, 6 or 7 parameter Bursa-Wolf values from TOWGS84 token
    private static Wgs84ConversionInfo ReadWGS84ConversionInfo(WktKeywordNode node)
    {
        IReadOnlyList<double> values = node.GetAllNumbers();
        if (values.Count is not 3 and not 6 and not 7)
        {
            ThrowWktParseException("WKT1 TOWGS84 must contain 3, 6, or 7 numeric values.");
        }

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is WktKeywordNode keywordChild)
            {
                throw new NotSupportedException($"WKT1 TOWGS84 keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        var info = new Wgs84ConversionInfo
        {
            Dx = values[0],
            Dy = values[1],
            Dz = values[2],
        };

        if (values.Count >= 6)
        {
            info.Ex = values[3];
            info.Ey = values[4];
            info.Ez = values[5];
        }

        if (values.Count == 7)
        {
            info.Ppm = values[6];
        }

        return info;
    }

    private static Ellipsoid ReadEllipsoid(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        double majorAxis = node.GetNumberChild(1);
        double e = node.GetNumberChild(2);
        ReadWkt1Authority(node, out string authority, out long authorityCode);

        return new Ellipsoid(majorAxis, 0.0, e, true, LinearUnit.Metre, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static Projection ReadProjection(WktKeywordNode projectionNode, List<WktKeywordNode> parameterNodes)
    {
        string projectionName = projectionNode.GetStringChild(0);
        ReadWkt1Authority(projectionNode, out string authority, out long authorityCode);

        var paramList = new List<ProjectionParameter>(parameterNodes.Count);
        for (int i = 0; i < parameterNodes.Count; i++)
        {
            paramList.Add(ReadWkt1ProjectionParameter(parameterNodes[i]));
        }

        return new Projection(projectionName, paramList, projectionName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static ProjectionParameter ReadWkt1ProjectionParameter(WktKeywordNode node)
    {
        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is WktKeywordNode keywordChild)
            {
                throw new NotSupportedException($"WKT1 PARAMETER keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new ProjectionParameter(node.GetStringChild(0), node.GetNumberChild(1));
    }

    private static ProjectedCoordinateSystem ReadProjectedCoordinateSystem(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        GeographicCoordinateSystem? geographicCS = null;
        LinearUnit? linearUnit = null;
        WktKeywordNode? projectionNode = null;
        var parameterNodes = new List<WktKeywordNode>();
        var axisInfo = new List<AxisInfo>(2);
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            switch (keywordChild.Keyword)
            {
                case "GEOGCS":
                    geographicCS = ReadGeographicCoordinateSystem(keywordChild);
                    break;
                case "UNIT":
                    linearUnit = ReadLinearUnit(keywordChild);
                    break;
                case "PROJECTION":
                    projectionNode = keywordChild;
                    break;
                case "PARAMETER":
                    parameterNodes.Add(keywordChild);
                    break;
                case "AXIS":
                    axisInfo.Add(ReadAxis(keywordChild));
                    break;
                case "AUTHORITY":
                    ReadWkt1Authority(keywordChild, out authority, out authorityCode);
                    break;
                default:
                    break;
            }
        }

        // This is default axis values if not specified.
        if (axisInfo.Count == 0)
        {
            axisInfo.Add(new AxisInfo("X", AxisOrientationEnum.East));
            axisInfo.Add(new AxisInfo("Y", AxisOrientationEnum.North));
        }

        geographicCS = ArgumentGuard.ThrowIfNull(geographicCS, nameof(geographicCS));
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        Projection projection = ReadProjection(ArgumentGuard.ThrowIfNull(projectionNode, nameof(projectionNode)), parameterNodes);
        return new ProjectedCoordinateSystem(geographicCS.HorizontalDatum, geographicCS, linearUnit, projection, axisInfo, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static VerticalCoordinateSystem ReadVerticalCoordinateSystem(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        VerticalDatum? verticalDatum = null;
        LinearUnit? linearUnit = null;
        string authority = string.Empty;
        long authorityCode = -1;
        AxisInfo? info = null;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            switch (keywordChild.Keyword)
            {
                case "VERT_DATUM":
                    verticalDatum = ReadVerticalDatum(keywordChild);
                    break;
                case "UNIT":
                    linearUnit = ReadLinearUnit(keywordChild);
                    break;
                case "AXIS":
                    info = ReadAxis(keywordChild);
                    break;
                case "AUTHORITY":
                    ReadWkt1Authority(keywordChild, out authority, out authorityCode);
                    break;
                default:
                    break;
            }
        }

        // This is default axis values if not specified.
        info ??= new AxisInfo("Up", AxisOrientationEnum.Up);

        return new VerticalCoordinateSystem(
            ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit)),
            ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum)),
            info,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CompoundCoordinateSystem ReadCompoundCoordinateSystem(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        CoordinateSystem? headcs = null;
        CoordinateSystem? tailcs = null;
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (string.Equals(keywordChild.Keyword, "AUTHORITY", StringComparison.OrdinalIgnoreCase))
            {
                ReadWkt1Authority(keywordChild, out authority, out authorityCode);
            }
            else if (IsCoordinateSystemKeyword(keywordChild.Keyword))
            {
                if (headcs is null)
                {
                    headcs = ReadCoordinateSystemNode(keywordChild);
                }
                else if (tailcs is null)
                {
                    tailcs = ReadCoordinateSystemNode(keywordChild);
                }
            }
        }

        return new CompoundCoordinateSystem(
            ArgumentGuard.ThrowIfNull(headcs, nameof(headcs)),
            ArgumentGuard.ThrowIfNull(tailcs, nameof(tailcs)),
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeocentricCoordinateSystem ReadGeocentricCoordinateSystem(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        HorizontalDatum? horizontalDatum = null;
        PrimeMeridian? primeMeridian = null;
        LinearUnit? linearUnit = null;
        string authority = string.Empty;
        long authorityCode = -1;
        var info = new List<AxisInfo>(3);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            switch (keywordChild.Keyword)
            {
                case "DATUM":
                    horizontalDatum = ReadHorizontalDatum(keywordChild);
                    break;
                case "PRIMEM":
                    primeMeridian = ReadPrimeMeridian(keywordChild);
                    break;
                case "UNIT":
                    linearUnit = ReadLinearUnit(keywordChild);
                    break;
                case "AXIS":
                    info.Add(ReadAxis(keywordChild));
                    break;
                case "AUTHORITY":
                    ReadWkt1Authority(keywordChild, out authority, out authorityCode);
                    break;
                default:
                    break;
            }
        }

        // This is default axis values if not specified.
        if (info.Count == 0)
        {
            info.Add(new AxisInfo("Geocentric X", AxisOrientationEnum.Other));
            info.Add(new AxisInfo("Geocentric Y", AxisOrientationEnum.Other));
            info.Add(new AxisInfo("Geocentric Z", AxisOrientationEnum.North));
        }

        return new GeocentricCoordinateSystem(
            ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum)),
            ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit)),
            ArgumentGuard.ThrowIfNull(primeMeridian, nameof(primeMeridian)),
            info,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeographicCoordinateSystem ReadGeographicCoordinateSystem(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        HorizontalDatum? horizontalDatum = null;
        PrimeMeridian? primeMeridian = null;
        AngularUnit? angularUnit = null;
        string authority = string.Empty;
        long authorityCode = -1;
        var info = new List<AxisInfo>(2);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            switch (keywordChild.Keyword)
            {
                case "DATUM":
                    horizontalDatum = ReadHorizontalDatum(keywordChild);
                    break;
                case "PRIMEM":
                    primeMeridian = ReadPrimeMeridian(keywordChild);
                    break;
                case "UNIT":
                    angularUnit = ReadAngularUnit(keywordChild);
                    break;
                case "AXIS":
                    info.Add(ReadAxis(keywordChild));
                    break;
                case "AUTHORITY":
                    ReadWkt1Authority(keywordChild, out authority, out authorityCode);
                    break;
                default:
                    break;
            }
        }

        // This is default axis values if not specified.
        if (info.Count == 0)
        {
            info.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
            info.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
        }

        return new GeographicCoordinateSystem(
            ArgumentGuard.ThrowIfNull(angularUnit, nameof(angularUnit)),
            ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum)),
            ArgumentGuard.ThrowIfNull(primeMeridian, nameof(primeMeridian)),
            info,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static HorizontalDatum ReadHorizontalDatum(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        Wgs84ConversionInfo? wgsInfo = null;
        string authority = string.Empty;
        long authorityCode = -1;
        Ellipsoid? ellipsoid = null;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            switch (keywordChild.Keyword)
            {
                case "SPHEROID":
                    ellipsoid = ReadEllipsoid(keywordChild);
                    break;
                case "TOWGS84":
                    wgsInfo = ReadWGS84ConversionInfo(keywordChild);
                    break;
                case "AUTHORITY":
                    ReadWkt1Authority(keywordChild, out authority, out authorityCode);
                    break;
                default:
                    break;
            }
        }

        // make an assumption about the datum type.
        return new HorizontalDatum(
            ArgumentGuard.ThrowIfNull(ellipsoid, nameof(ellipsoid)),
            wgsInfo,
            DatumType.HD_Geocentric,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static VerticalDatum ReadVerticalDatum(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        var datumType = (DatumType)node.GetNumberChild(1);
        ReadWkt1Authority(node, out string authority, out long authorityCode);

        return new VerticalDatum(datumType, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static PrimeMeridian ReadPrimeMeridian(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        double longitude = node.GetNumberChild(1);
        ReadWkt1Authority(node, out string authority, out long authorityCode);

        // make an assumption about the Angular units - degrees.
        return new PrimeMeridian(longitude, AngularUnit.Degrees, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static void ReadWkt1Authority(WktKeywordNode node, out string authority, out long authorityCode)
    {
        authority = string.Empty;
        authorityCode = -1;

        if (node.KeywordEquals("AUTHORITY"))
        {
            ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
            if (children.Length < 2)
            {
                return;
            }

            authority = node.GetLeafTextChild(0);
            authorityCode = long.TryParse(node.GetLeafTextChild(1), NumberStyles.Any, CultureInfo.InvariantCulture, out long directCode)
                ? directCode
                : -1;
            return;
        }

        (string Authority, string Code)? authorityNode = node.GetAuthority();
        if (!authorityNode.HasValue)
        {
            return;
        }

        authority = authorityNode.Value.Authority;
        authorityCode = long.TryParse(authorityNode.Value.Code, NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedCode)
            ? parsedCode
            : -1;
    }

    private static FittedCoordinateSystem ReadFittedCoordinateSystem(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);
        MathTransform? toBaseTransform = null;
        CoordinateSystem? baseCS = null;
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (string.Equals(keywordChild.Keyword, "PARAM_MT", StringComparison.OrdinalIgnoreCase))
            {
                toBaseTransform = MathTransformWktReader.ReadMathTransform(keywordChild);
            }
            else if (string.Equals(keywordChild.Keyword, "AUTHORITY", StringComparison.OrdinalIgnoreCase))
            {
                ReadWkt1Authority(keywordChild, out authority, out authorityCode);
            }
            else if (baseCS is null && IsCoordinateSystemKeyword(keywordChild.Keyword))
            {
                baseCS = ReadCoordinateSystemNode(keywordChild);
            }
        }

        return new FittedCoordinateSystem(
            ArgumentGuard.ThrowIfNull(baseCS, nameof(baseCS)),
            ArgumentGuard.ThrowIfNull(toBaseTransform, nameof(toBaseTransform)),
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
