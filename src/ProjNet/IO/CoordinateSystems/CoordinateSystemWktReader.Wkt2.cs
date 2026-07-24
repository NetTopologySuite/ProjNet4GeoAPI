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
    private static CoordinateSystem ReadWkt2GeodeticCoordinateReferenceSystem(WktKeywordNode node)
    {
        string rootKeyword = node.Keyword;
        string name = node.GetStringChild(0);

        HorizontalDatum? horizontalDatum = null;
        GeographicCoordinateSystem? baseGeographicCoordinateSystem = null;
        Projection? derivingConversion = null;
        PrimeMeridian? primeMeridian = null;
        AngularUnit? angularUnit = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("DATUM"))
            {
                horizontalDatum = ReadWkt2HorizontalDatum(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ENSEMBLE"))
            {
                horizontalDatum = ReadWkt2HorizontalDatumEnsemble(keywordChild);
            }
            else if (keywordChild.KeywordEquals("BASEGEOGCRS") || keywordChild.KeywordEquals("BASEGEODCRS"))
            {
                baseGeographicCoordinateSystem = ReadWkt2BaseGeographicCoordinateSystem(keywordChild);
            }
            else if (keywordChild.KeywordEquals("DERIVINGCONVERSION"))
            {
                derivingConversion = ReadWkt2DerivingConversion(keywordChild, out AngularUnit? derivingAngularUnit);
                angularUnit = MergeAxisAngularUnit(angularUnit, derivingAngularUnit);
            }
            else if (keywordChild.KeywordEquals("PRIMEM"))
            {
                primeMeridian = ReadWkt2PrimeMeridian(keywordChild);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                axisInfo.Add(ReadWkt2Axis(keywordChild, out AngularUnit? axisAngularUnit, out LinearUnit? axisLinearUnit));
                angularUnit = MergeAxisAngularUnit(angularUnit, axisAngularUnit);
                linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
            }
            else if (keywordChild.KeywordEquals("ANGLEUNIT"))
            {
                angularUnit = ReadWkt2AngularUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("LENGTHUNIT"))
            {
                linearUnit = ReadWkt2LinearUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        bool isDerived = baseGeographicCoordinateSystem is not null || derivingConversion is not null;
        if (isDerived)
        {
            if (horizontalDatum is not null)
            {
                ThrowWktParseException("WKT2 derived geodetic CRS must use BASEGEOGCRS or BASEGEODCRS instead of a top-level DATUM block.");
            }

            if (baseGeographicCoordinateSystem is null)
            {
                ThrowWktParseException("WKT2 derived geodetic CRS is missing a BASEGEOGCRS or BASEGEODCRS block.");
            }

            if (derivingConversion is null)
            {
                ThrowWktParseException("WKT2 derived geodetic CRS is missing a DERIVINGCONVERSION block.");
            }

            if (string.IsNullOrWhiteSpace(coordinateSystemType))
            {
                ThrowWktParseException("WKT2 derived geodetic CRS is missing a CS block.");
            }

            if (!string.Equals(coordinateSystemType, "ellipsoidal", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"WKT2 derived geodetic coordinate system type '{coordinateSystemType}' is not supported.");
            }

            if (coordinateSystemDimension != 2)
            {
                throw new NotSupportedException("WKT2 derived geodetic CRS dimensions other than 2 are not supported.");
            }

            if (axisInfo.Count != coordinateSystemDimension)
            {
                ThrowWktParseException($"WKT2 derived geodetic CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
            }

            if (angularUnit is null)
            {
                ThrowWktParseException("WKT2 derived geodetic CRS is missing an ANGLEUNIT block.");
            }

            AffineTransform transform = DerivedCoordinateSystemSupport.CreateAffineTransform(derivingConversion);
            var fittedCoordinateSystem = new FittedCoordinateSystem(
                baseGeographicCoordinateSystem,
                transform,
                name,
                authority,
                authorityCode,
                string.Empty,
                string.Empty,
                string.Empty,
                axisInfo);
            return fittedCoordinateSystem;
        }

        if (horizontalDatum is null)
        {
            ThrowWktParseException("WKT2 geodetic CRS is missing a DATUM block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ThrowWktParseException("WKT2 geodetic CRS is missing a CS block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ThrowWktParseException($"WKT2 geodetic CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        if (string.Equals(coordinateSystemType, "ellipsoidal", StringComparison.OrdinalIgnoreCase))
        {
            if (coordinateSystemDimension == 3)
            {
                if (angularUnit is null)
                {
                    ThrowWktParseException("WKT2 ellipsoidal CRS is missing ANGLEUNIT metadata.");
                }

                if (linearUnit is null)
                {
                    ThrowWktParseException("WKT2 three-dimensional ellipsoidal CRS is missing LENGTHUNIT metadata.");
                }

                primeMeridian ??= PrimeMeridian.Greenwich;
                return CreateOperationalWkt2EllipsoidalHeightCompoundCoordinateSystem(
                    name,
                    authority,
                    authorityCode,
                    horizontalDatum,
                    primeMeridian,
                    angularUnit,
                    linearUnit,
                    axisInfo);
            }

            if (coordinateSystemDimension != 2)
            {
                throw new NotSupportedException("WKT2 ellipsoidal CRS dimensions other than 2 are not supported.");
            }

            if (angularUnit is null)
            {
                ThrowWktParseException("WKT2 ellipsoidal CRS is missing ANGLEUNIT metadata.");
            }

            primeMeridian ??= PrimeMeridian.Greenwich;
            return new GeographicCoordinateSystem(
                angularUnit,
                horizontalDatum,
                primeMeridian,
                axisInfo,
                name,
                authority,
                authorityCode,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        if (string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            if (coordinateSystemDimension != 3)
            {
                throw new NotSupportedException("WKT2 cartesian geodetic CRS dimensions other than 3 are not supported.");
            }

            if (linearUnit is null)
            {
                ThrowWktParseException("WKT2 cartesian geodetic CRS is missing LENGTHUNIT metadata.");
            }

            primeMeridian ??= PrimeMeridian.Greenwich;
            return new GeocentricCoordinateSystem(
                horizontalDatum,
                linearUnit,
                primeMeridian,
                axisInfo,
                name,
                authority,
                authorityCode,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        throw new NotSupportedException($"WKT2 coordinate system type '{coordinateSystemType}' is not supported.");
    }

    private static CompoundCoordinateSystem CreateOperationalWkt2EllipsoidalHeightCompoundCoordinateSystem(
        string name,
        string authority,
        long authorityCode,
        HorizontalDatum horizontalDatum,
        PrimeMeridian primeMeridian,
        AngularUnit angularUnit,
        LinearUnit linearUnit,
        List<AxisInfo> axisInfo)
    {
        var head = new GeographicCoordinateSystem(
            angularUnit,
            horizontalDatum,
            primeMeridian,
            [new AxisInfo(axisInfo[0]), new AxisInfo(axisInfo[1])],
            name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        var tail = new VerticalCoordinateSystem(
            linearUnit,
            new VerticalDatum(DatumType.VD_Ellipsoidal, "Ellipsoidal height datum", string.Empty, -1, string.Empty, string.Empty, string.Empty),
            new AxisInfo(axisInfo[2]),
            axisInfo[2].Name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        return new CompoundCoordinateSystem(head, tail, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static (string Type, int Dimension) ReadWkt2CoordinateSystemDefinition(WktKeywordNode node)
    {
        if (!node.KeywordEquals("CS"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in CS.");
        }

        string coordinateSystemType = node.GetIdentifierChild(0);
        int dimension = checked((int)node.GetNumberChild(1));

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ID") ||
                ShouldSkipWkt2MetadataNode(keywordChild))
            {
                continue;
            }

            throw new NotSupportedException($"WKT2 CS keyword '{keywordChild.Keyword}' is not supported.");
        }

        return (coordinateSystemType, dimension);
    }

    private static AxisInfo ReadWkt2Axis(WktKeywordNode node, out AngularUnit? angularUnit, out LinearUnit? linearUnit)
    {
        (AxisInfo axis, IUnit? unit) = ReadWkt2AxisDefinition(node);
        WktKeywordNode? unitNode = node.FindChild("ANGLEUNIT", "LENGTHUNIT", "SCALEUNIT", "TIMEUNIT", "PARAMETRICUNIT");

        if (unit is not null && unit is not AngularUnit && unit is not LinearUnit)
        {
            throw new NotSupportedException($"WKT2 AXIS keyword '{ArgumentGuard.ThrowIfNull(unitNode, nameof(unitNode)).Keyword}' is not supported.");
        }

        angularUnit = unit as AngularUnit;
        linearUnit = unit as LinearUnit;
        return axis;
    }

    private static AxisOrientationEnum ParseWkt2AxisOrientation(string orientationToken)
    {
        if (string.Equals(orientationToken, "NORTH", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.North;
        }

        if (string.Equals(orientationToken, "SOUTH", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.South;
        }

        if (string.Equals(orientationToken, "EAST", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.East;
        }

        if (string.Equals(orientationToken, "WEST", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.West;
        }

        if (string.Equals(orientationToken, "UP", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.Up;
        }

        if (string.Equals(orientationToken, "DOWN", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.Down;
        }

        if (string.Equals(orientationToken, "OTHER", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.Other;
        }

        if (string.Equals(orientationToken, "GEOCENTRICX", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.Other;
        }

        if (string.Equals(orientationToken, "GEOCENTRICY", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.East;
        }

        if (string.Equals(orientationToken, "GEOCENTRICZ", StringComparison.OrdinalIgnoreCase))
        {
            return AxisOrientationEnum.North;
        }

        return ThrowWktParseException<AxisOrientationEnum>($"Invalid WKT2 axis orientation '{orientationToken}'.");
    }

    private static (AxisInfo Axis, IUnit? Unit) ReadWkt2AxisDefinition(WktKeywordNode node)
    {
        if (!node.KeywordEquals("AXIS"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in AXIS.");
        }

        IUnit? unit = null;
        string axisName = node.GetStringChild(0);
        AxisOrientationEnum orientation = ParseWkt2AxisOrientation(node.GetIdentifierChild(1));

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ANGLEUNIT")
                || keywordChild.KeywordEquals("LENGTHUNIT")
                || keywordChild.KeywordEquals("SCALEUNIT")
                || keywordChild.KeywordEquals("TIMEUNIT")
                || keywordChild.KeywordEquals("PARAMETRICUNIT"))
            {
                unit = ReadWkt2Unit(keywordChild);
            }
            else if (!keywordChild.KeywordEquals("ID"))
            {
                if (!ShouldSkipWkt2MetadataNode(keywordChild))
                {
                    throw new NotSupportedException($"WKT2 AXIS keyword '{keywordChild.Keyword}' is not supported.");
                }
            }
        }

        return (new AxisInfo(axisName, orientation), unit);
    }

    private static HorizontalDatum ReadWkt2HorizontalDatum(WktKeywordNode node)
    {
        if (!node.KeywordEquals("DATUM"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in DATUM.");
        }

        string name = node.GetStringChild(0);
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

            if (keywordChild.KeywordEquals("ELLIPSOID"))
            {
                ellipsoid = ReadWkt2Ellipsoid(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 DATUM keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (ellipsoid is null)
        {
            ThrowWktParseException("WKT2 DATUM is missing an ELLIPSOID block.");
        }

        return new HorizontalDatum(ellipsoid, null, DatumType.HD_Geocentric, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static HorizontalDatum ReadWkt2HorizontalDatumEnsemble(WktKeywordNode node)
    {
        DatumEnsemble ensemble = ReadWkt2DatumEnsemble(node, requireEllipsoid: true);
        Ellipsoid ellipsoid = ArgumentGuard.ThrowIfNull(ensemble.Ellipsoid, nameof(ensemble));
        return new HorizontalDatum(ellipsoid, null, DatumType.HD_Geocentric, ensemble.Name, ensemble.Authority, ensemble.AuthorityCode, string.Empty, string.Empty, string.Empty, ensemble);
    }

    private static DatumEnsemble ReadWkt2DatumEnsemble(WktKeywordNode node, bool requireEllipsoid)
    {
        if (!node.KeywordEquals("ENSEMBLE"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in ENSEMBLE.");
        }

        string name = node.GetStringChild(0);
        var members = new List<DatumEnsembleMember>();
        Ellipsoid? ellipsoid = null;
        double? accuracy = null;
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("MEMBER"))
            {
                members.Add(ReadWkt2DatumEnsembleMember(keywordChild));
            }
            else if (keywordChild.KeywordEquals("ELLIPSOID"))
            {
                ellipsoid = ReadWkt2Ellipsoid(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ENSEMBLEACCURACY"))
            {
                accuracy = ReadWkt2DatumEnsembleAccuracy(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 ENSEMBLE keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (members.Count == 0)
        {
            ThrowWktParseException("WKT2 ENSEMBLE is missing MEMBER blocks.");
        }

        if (requireEllipsoid && ellipsoid is null)
        {
            ThrowWktParseException("WKT2 ENSEMBLE is missing an ELLIPSOID block.");
        }

        if (accuracy is null)
        {
            ThrowWktParseException("WKT2 ENSEMBLE is missing an ENSEMBLEACCURACY block.");
        }

        return new DatumEnsemble(name, members, accuracy.Value, ellipsoid, authority, authorityCode);
    }

    private static DatumEnsembleMember ReadWkt2DatumEnsembleMember(WktKeywordNode node)
    {
        if (!node.KeywordEquals("MEMBER"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in MEMBER.");
        }

        string name = node.GetStringChild(0);
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 MEMBER keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new DatumEnsembleMember(name, authority, authorityCode);
    }

    private static double ReadWkt2DatumEnsembleAccuracy(WktKeywordNode node)
    {
        if (!node.KeywordEquals("ENSEMBLEACCURACY"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in ENSEMBLEACCURACY.");
        }

        return node.GetNumberChild(0);
    }

    private static Ellipsoid ReadWkt2Ellipsoid(WktKeywordNode node)
    {
        if (!node.KeywordEquals("ELLIPSOID"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in ELLIPSOID.");
        }

        string name = node.GetStringChild(0);
        double semiMajorAxis = node.GetNumberChild(1);
        double inverseFlattening = node.GetNumberChild(2);
        string authority = string.Empty;
        long authorityCode = -1;
        LinearUnit? axisUnit = null;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("LENGTHUNIT"))
            {
                axisUnit = ReadWkt2LinearUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 ELLIPSOID keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (axisUnit is null)
        {
            ThrowWktParseException("WKT2 ELLIPSOID is missing a LENGTHUNIT block.");
        }

        return new Ellipsoid(semiMajorAxis, 0d, inverseFlattening, true, axisUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static PrimeMeridian ReadWkt2PrimeMeridian(WktKeywordNode node)
    {
        if (!node.KeywordEquals("PRIMEM"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in PRIMEM.");
        }

        string name = node.GetStringChild(0);
        double longitude = node.GetNumberChild(1);
        string authority = string.Empty;
        long authorityCode = -1;
        AngularUnit angularUnit = AngularUnit.Degrees;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ANGLEUNIT"))
            {
                angularUnit = ReadWkt2AngularUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 PRIMEM keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new PrimeMeridian(longitude, angularUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static AngularUnit ReadWkt2AngularUnit(WktKeywordNode node)
    {
        return ReadWkt2UnitFromNode(
            node,
            "ANGLEUNIT",
            static (conversionFactor, name, authority, authorityCode) => new AngularUnit(conversionFactor, name, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    private static LinearUnit ReadWkt2LinearUnit(WktKeywordNode node)
    {
        return ReadWkt2UnitFromNode(
            node,
            "LENGTHUNIT",
            static (conversionFactor, name, authority, authorityCode) => new LinearUnit(conversionFactor, name, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    private static Unit ReadWkt2ScaleUnit(WktKeywordNode node)
    {
        return ReadWkt2UnitFromNode(
            node,
            "SCALEUNIT",
            static (conversionFactor, name, authority, authorityCode) => new Unit(conversionFactor, name, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    private static TimeUnit ReadWkt2TimeUnit(WktKeywordNode node)
    {
        return ReadWkt2UnitFromNode(
            node,
            "TIMEUNIT",
            static (conversionFactor, name, authority, authorityCode) => new TimeUnit(conversionFactor, name, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    private static ParametricUnit ReadWkt2ParametricUnit(WktKeywordNode node)
    {
        return ReadWkt2UnitFromNode(
            node,
            "PARAMETRICUNIT",
            static (conversionFactor, name, authority, authorityCode) => new ParametricUnit(conversionFactor, name, authority, authorityCode, string.Empty, string.Empty, string.Empty));
    }

    private static IUnit ReadWkt2Unit(WktKeywordNode node)
    {
        if (node.KeywordEquals("ANGLEUNIT"))
        {
            return ReadWkt2AngularUnit(node);
        }

        if (node.KeywordEquals("LENGTHUNIT"))
        {
            return ReadWkt2LinearUnit(node);
        }

        if (node.KeywordEquals("SCALEUNIT"))
        {
            return ReadWkt2ScaleUnit(node);
        }

        if (node.KeywordEquals("TIMEUNIT"))
        {
            return ReadWkt2TimeUnit(node);
        }

        if (node.KeywordEquals("PARAMETRICUNIT"))
        {
            return ReadWkt2ParametricUnit(node);
        }

        throw new NotSupportedException($"WKT2 unit keyword '{node.Keyword}' is not supported.");
    }

    private static TUnit ReadWkt2UnitFromNode<TUnit>(WktKeywordNode node, string expectedKeyword, Func<double, string, string, long, TUnit> factory)
    {
        if (!node.KeywordEquals(expectedKeyword))
        {
            throw new NotSupportedException($"WKT2 unit keyword '{node.Keyword}' is not supported.");
        }

        string name = node.GetStringChild(0);
        double conversionFactor = node.GetNumberChild(1);
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
                continue;
            }

            if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 {expectedKeyword} keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return factory(conversionFactor, name, authority, authorityCode);
    }

    private static bool Wkt2UnitsEqual(IUnit left, IUnit right)
    {
        return left.GetType() == right.GetType() && left.EqualParams(right);
    }

    private static List<IUnit> ResolveWkt2CoordinateSystemUnits(IUnit? rootUnit, List<IUnit?> axisUnits, int dimension, string context, bool allowMixedUnits)
    {
        if (axisUnits.Count != dimension)
        {
            ThrowWktParseException($"{context} declared dimension {dimension}, but provided {axisUnits.Count} AXIS blocks.");
        }

        var resolvedUnits = new List<IUnit>(dimension);
        for (int i = 0; i < axisUnits.Count; i++)
        {
            IUnit? axisUnit = axisUnits[i];
            if (axisUnit is null)
            {
                if (rootUnit is null)
                {
                    ThrowWktParseException($"{context} axis {i.ToString(CultureInfo.InvariantCulture)} is missing a unit definition.");
                }

                axisUnit = rootUnit;
            }
            else if (rootUnit is not null && !Wkt2UnitsEqual(rootUnit, axisUnit) && !allowMixedUnits)
            {
                throw new NotSupportedException($"{context} axis-specific units must match the root unit.");
            }

            resolvedUnits.Add(ArgumentGuard.ThrowIfNull(axisUnit, nameof(axisUnit)));
        }

        return resolvedUnits;
    }

    private static EngineeringDatum ReadWkt2EngineeringDatum(WktKeywordNode node)
    {
        if (!node.KeywordEquals("EDATUM") &&
            !node.KeywordEquals("ENGINEERINGDATUM"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in engineering datum.");
        }

        string name = node.GetStringChild(0);
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 {node.Keyword} keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new EngineeringDatum(name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static TemporalDatum ReadWkt2TemporalDatum(WktKeywordNode node)
    {
        if (!node.KeywordEquals("TDATUM") &&
            !node.KeywordEquals("TIMEDATUM"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in temporal datum.");
        }

        string name = node.GetStringChild(0);
        string timeOrigin = string.Empty;
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("TIMEORIGIN"))
            {
                timeOrigin = keywordChild.GetStringChild(0);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 {node.Keyword} keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (string.IsNullOrWhiteSpace(timeOrigin))
        {
            ThrowWktParseException("WKT2 temporal datum is missing a TIMEORIGIN block.");
        }

        return new TemporalDatum(timeOrigin, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static ParametricDatum ReadWkt2ParametricDatum(WktKeywordNode node)
    {
        if (!node.KeywordEquals("PDATUM") &&
            !node.KeywordEquals("PARAMETRICDATUM"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in parametric datum.");
        }

        string name = node.GetStringChild(0);
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 {node.Keyword} keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new ParametricDatum(name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static EngineeringCoordinateSystem ReadWkt2EngineeringCoordinateSystem(WktKeywordNode node)
    {
        string rootKeyword = node.Keyword;
        string name = node.GetStringChild(0);

        EngineeringDatum? engineeringDatum = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        IUnit? rootUnit = null;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();
        var axisUnits = new List<IUnit?>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("EDATUM") || keywordChild.KeywordEquals("ENGINEERINGDATUM"))
            {
                engineeringDatum = ReadWkt2EngineeringDatum(keywordChild);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                (AxisInfo axis, IUnit? unit) = ReadWkt2AxisDefinition(keywordChild);
                axisInfo.Add(axis);
                axisUnits.Add(unit);
            }
            else if (keywordChild.KeywordEquals("ANGLEUNIT")
                || keywordChild.KeywordEquals("LENGTHUNIT")
                || keywordChild.KeywordEquals("SCALEUNIT")
                || keywordChild.KeywordEquals("TIMEUNIT")
                || keywordChild.KeywordEquals("PARAMETRICUNIT"))
            {
                rootUnit = ReadWkt2Unit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        if (engineeringDatum is null)
        {
            ThrowWktParseException("WKT2 engineering CRS is missing an EDATUM or ENGINEERINGDATUM block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ThrowWktParseException("WKT2 engineering CRS is missing a CS block.");
        }

        List<IUnit> resolvedUnits = ResolveWkt2CoordinateSystemUnits(rootUnit, axisUnits, coordinateSystemDimension, "WKT2 engineering CRS", allowMixedUnits: true);
        engineeringDatum = ArgumentGuard.ThrowIfNull(engineeringDatum, nameof(engineeringDatum));
        return new EngineeringCoordinateSystem(
            engineeringDatum,
            ArgumentGuard.ThrowIfNull(coordinateSystemType, nameof(coordinateSystemType)),
            axisInfo,
            resolvedUnits,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static TemporalCoordinateSystem ReadWkt2TemporalCoordinateSystem(WktKeywordNode node)
    {
        const string rootKeyword = "TIMECRS";
        string name = node.GetStringChild(0);

        TemporalDatum? temporalDatum = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        IUnit? rootUnit = null;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();
        var axisUnits = new List<IUnit?>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("TDATUM") || keywordChild.KeywordEquals("TIMEDATUM"))
            {
                temporalDatum = ReadWkt2TemporalDatum(keywordChild);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                (AxisInfo axis, IUnit? unit) = ReadWkt2AxisDefinition(keywordChild);
                axisInfo.Add(axis);
                axisUnits.Add(unit);
            }
            else if (keywordChild.KeywordEquals("TIMEUNIT"))
            {
                rootUnit = ReadWkt2TimeUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        if (temporalDatum is null)
        {
            ThrowWktParseException("WKT2 temporal CRS is missing a TDATUM or TIMEDATUM block.");
        }

        if (!string.Equals(coordinateSystemType, "temporal", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 temporal coordinate system type '{coordinateSystemType}' is not supported.");
        }

        List<IUnit> resolvedUnits = ResolveWkt2CoordinateSystemUnits(rootUnit, axisUnits, coordinateSystemDimension, "WKT2 temporal CRS", allowMixedUnits: false);
        if (resolvedUnits.Count != 1 || resolvedUnits[0] is not TimeUnit timeUnit)
        {
            throw new NotSupportedException("WKT2 temporal CRS requires TIMEUNIT metadata.");
        }

        temporalDatum = ArgumentGuard.ThrowIfNull(temporalDatum, nameof(temporalDatum));
        return new TemporalCoordinateSystem(
            timeUnit,
            temporalDatum,
            axisInfo[0],
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ParametricCoordinateSystem ReadWkt2ParametricCoordinateSystem(WktKeywordNode node)
    {
        const string rootKeyword = "PARAMETRICCRS";
        string name = node.GetStringChild(0);

        ParametricDatum? parametricDatum = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        IUnit? rootUnit = null;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();
        var axisUnits = new List<IUnit?>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("PDATUM") || keywordChild.KeywordEquals("PARAMETRICDATUM"))
            {
                parametricDatum = ReadWkt2ParametricDatum(keywordChild);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                (AxisInfo axis, IUnit? unit) = ReadWkt2AxisDefinition(keywordChild);
                axisInfo.Add(axis);
                axisUnits.Add(unit);
            }
            else if (keywordChild.KeywordEquals("PARAMETRICUNIT"))
            {
                rootUnit = ReadWkt2ParametricUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        if (parametricDatum is null)
        {
            ThrowWktParseException("WKT2 parametric CRS is missing a PDATUM or PARAMETRICDATUM block.");
        }

        if (!string.Equals(coordinateSystemType, "parametric", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 parametric coordinate system type '{coordinateSystemType}' is not supported.");
        }

        List<IUnit> resolvedUnits = ResolveWkt2CoordinateSystemUnits(rootUnit, axisUnits, coordinateSystemDimension, "WKT2 parametric CRS", allowMixedUnits: false);
        if (resolvedUnits.Count != 1 || resolvedUnits[0] is not ParametricUnit parametricUnit)
        {
            throw new NotSupportedException("WKT2 parametric CRS requires PARAMETRICUNIT metadata.");
        }

        parametricDatum = ArgumentGuard.ThrowIfNull(parametricDatum, nameof(parametricDatum));
        return new ParametricCoordinateSystem(
            parametricUnit,
            parametricDatum,
            axisInfo[0],
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CoordinateOperation ReadWkt2CoordinateOperation(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);

        CoordinateSystem? sourceCoordinateSystem = null;
        CoordinateSystem? targetCoordinateSystem = null;
        string methodName = string.Empty;
        string authority = string.Empty;
        long authorityCode = -1;
        var parameters = new List<Parameter>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("SOURCECRS"))
            {
                sourceCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(keywordChild);
            }
            else if (keywordChild.KeywordEquals("TARGETCRS"))
            {
                targetCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(keywordChild);
            }
            else if (keywordChild.KeywordEquals("METHOD"))
            {
                methodName = ReadWkt2ProjectionMethod(keywordChild);
            }
            else if (keywordChild.KeywordEquals("PARAMETER"))
            {
                parameters.Add(ReadWkt2CoordinateOperationParameter(keywordChild));
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 COORDINATEOPERATION keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (sourceCoordinateSystem is null)
        {
            ThrowWktParseException("WKT2 coordinate operation is missing a SOURCECRS block.");
        }

        if (targetCoordinateSystem is null)
        {
            ThrowWktParseException("WKT2 coordinate operation is missing a TARGETCRS block.");
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            ThrowWktParseException("WKT2 coordinate operation is missing a METHOD block.");
        }

        sourceCoordinateSystem = ArgumentGuard.ThrowIfNull(sourceCoordinateSystem, nameof(sourceCoordinateSystem));
        targetCoordinateSystem = ArgumentGuard.ThrowIfNull(targetCoordinateSystem, nameof(targetCoordinateSystem));
        return new CoordinateOperation(
            methodName,
            parameters,
            sourceCoordinateSystem,
            targetCoordinateSystem,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static Parameter ReadWkt2CoordinateOperationParameter(WktKeywordNode node)
    {
        string parameterName = node.GetStringChild(0);
        double value = node.GetNumberChild(1);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (!keywordChild.KeywordEquals("ANGLEUNIT")
                && !keywordChild.KeywordEquals("LENGTHUNIT")
                && !keywordChild.KeywordEquals("SCALEUNIT")
                && !keywordChild.KeywordEquals("TIMEUNIT")
                && !keywordChild.KeywordEquals("PARAMETRICUNIT")
                && !keywordChild.KeywordEquals("ID")
                && !ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 COORDINATEOPERATION PARAMETER keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new Parameter(parameterName, value);
    }

    private static ConcatenatedOperation ReadWkt2ConcatenatedOperation(WktKeywordNode node)
    {
        string name = node.GetStringChild(0);

        CoordinateSystem? sourceCoordinateSystem = null;
        CoordinateSystem? targetCoordinateSystem = null;
        string authority = string.Empty;
        long authorityCode = -1;
        var steps = new List<CoordinateOperation>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("SOURCECRS"))
            {
                sourceCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(keywordChild);
            }
            else if (keywordChild.KeywordEquals("TARGETCRS"))
            {
                targetCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(keywordChild);
            }
            else if (keywordChild.KeywordEquals("STEP"))
            {
                steps.Add(ReadWkt2ConcatenatedOperationStep(keywordChild));
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 CONCATENATEDOPERATION keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (sourceCoordinateSystem is null)
        {
            ThrowWktParseException("WKT2 concatenated operation is missing a SOURCECRS block.");
        }

        if (targetCoordinateSystem is null)
        {
            ThrowWktParseException("WKT2 concatenated operation is missing a TARGETCRS block.");
        }

        sourceCoordinateSystem = ArgumentGuard.ThrowIfNull(sourceCoordinateSystem, nameof(sourceCoordinateSystem));
        targetCoordinateSystem = ArgumentGuard.ThrowIfNull(targetCoordinateSystem, nameof(targetCoordinateSystem));
        return new ConcatenatedOperation(
            steps,
            sourceCoordinateSystem,
            targetCoordinateSystem,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CoordinateOperation ReadWkt2ConcatenatedOperationStep(WktKeywordNode node)
    {
        WktKeywordNode? operationNode = node.FindChild("COORDINATEOPERATION");
        if (operationNode is null)
        {
            WktKeywordNode? firstChild = null;
            ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] is WktKeywordNode keywordChild)
                {
                    firstChild = keywordChild;
                    break;
                }
            }

            throw new NotSupportedException($"WKT2 STEP keyword '{firstChild?.Keyword ?? string.Empty}' is not supported.");
        }

        return ReadWkt2CoordinateOperation(operationNode);
    }

    private static void ReadIdentifierWithUnknownCode(WktKeywordNode node, out string authority, out long authorityCode)
    {
        if (!node.KeywordEquals("ID"))
        {
            throw new NotSupportedException($"WKT2 identifier keyword '{node.Keyword}' is not supported.");
        }

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        if (children.Length < 2)
        {
            ThrowWktParseException("WKT2 ID is missing an authority code.");
        }

        authority = node.GetLeafTextChild(0);
        authorityCode = long.TryParse(node.GetLeafTextChild(1), NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedCode)
            ? parsedCode
            : -1;
    }

    private static bool ShouldSkipWkt2MetadataNode(WktKeywordNode node)
    {
        return node.KeywordEquals("ANCHOR")
            || node.KeywordEquals("ANCHOREPOCH")
            || node.KeywordEquals("AREA")
            || node.KeywordEquals("BBOX")
            || node.KeywordEquals("DEFININGTRANSFORMATION")
            || node.KeywordEquals("DYNAMIC")
            || node.KeywordEquals("GEOIDMODEL")
            || node.KeywordEquals("MERIDIAN")
            || node.KeywordEquals("ORDER")
            || node.KeywordEquals("REMARK")
            || node.KeywordEquals("SCOPE")
            || node.KeywordEquals("VERSION")
            || node.KeywordEquals("USAGE");
    }

    private static AngularUnit? MergeAxisAngularUnit(AngularUnit? current, AngularUnit? candidate)
    {
        if (candidate is null)
        {
            return current;
        }

        if (current is null || current.EqualParams(candidate))
        {
            return candidate;
        }

        throw new NotSupportedException("WKT2 axis-specific ANGLEUNIT values must match within the same CRS.");
    }

    private static GeographicCoordinateSystem OverrideGeographicAngularUnit(GeographicCoordinateSystem geographicCoordinateSystem, AngularUnit? angularUnit)
    {
        geographicCoordinateSystem = ArgumentGuard.ThrowIfNull(geographicCoordinateSystem, nameof(geographicCoordinateSystem));
        if (angularUnit is null || geographicCoordinateSystem.AngularUnit.EqualParams(angularUnit))
        {
            return geographicCoordinateSystem;
        }

        return new GeographicCoordinateSystem(
            angularUnit,
            geographicCoordinateSystem.HorizontalDatum,
            geographicCoordinateSystem.PrimeMeridian,
            CloneAxisInfoList(geographicCoordinateSystem.AxisInfo),
            geographicCoordinateSystem.Name,
            geographicCoordinateSystem.Authority,
            geographicCoordinateSystem.AuthorityCode,
            geographicCoordinateSystem.Alias,
            geographicCoordinateSystem.Abbreviation,
            geographicCoordinateSystem.Remarks);
    }

    private static List<AxisInfo> CloneAxisInfoList(List<AxisInfo> axisInfo)
    {
        axisInfo = ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));

        var clone = new List<AxisInfo>(axisInfo.Count);
        for (int i = 0; i < axisInfo.Count; i++)
        {
            clone.Add(new AxisInfo(axisInfo[i]));
        }

        return clone;
    }

    private static LinearUnit? MergeAxisLinearUnit(LinearUnit? current, LinearUnit? candidate)
    {
        if (candidate is null)
        {
            return current;
        }

        if (current is null || current.EqualParams(candidate))
        {
            return candidate;
        }

        throw new NotSupportedException("WKT2 axis-specific LENGTHUNIT values must match within the same CRS.");
    }

    private static CoordinateSystem ReadWkt2ProjectedCoordinateSystem(WktKeywordNode node) =>
        ReadWkt2ProjectedCoordinateSystemCore(node, "PROJCRS", "projected CRS", "projected coordinate system", allowOperationalEllipsoidalHeightCompound: true);

    private static FittedCoordinateSystem ReadWkt2DerivedProjectedCoordinateSystem(WktKeywordNode node)
    {
        const string rootKeyword = "DERIVEDPROJCRS";

        ProjectedCoordinateSystem? baseProjectedCoordinateSystem = null;
        Projection? derivingConversion = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("BASEPROJCRS"))
            {
                baseProjectedCoordinateSystem = ReadWkt2BaseProjectedCoordinateSystem(keywordChild);
            }
            else if (keywordChild.KeywordEquals("DERIVINGCONVERSION"))
            {
                derivingConversion = ReadWkt2DerivingConversion(keywordChild, out _);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                axisInfo.Add(ReadWkt2Axis(keywordChild, out _, out LinearUnit? axisLinearUnit));
                linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
            }
            else if (keywordChild.KeywordEquals("LENGTHUNIT"))
            {
                linearUnit = ReadWkt2LinearUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        string name = node.GetStringChild(0);
        if (baseProjectedCoordinateSystem is null)
        {
            ThrowWktParseException("WKT2 derived projected CRS is missing a BASEPROJCRS block.");
        }

        if (derivingConversion is null)
        {
            ThrowWktParseException("WKT2 derived projected CRS is missing a DERIVINGCONVERSION block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ThrowWktParseException("WKT2 derived projected CRS is missing a CS block.");
        }

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 derived projected coordinate system type '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("WKT2 derived projected CRS dimensions other than 2 are not supported.");
        }

        if (linearUnit is null)
        {
            ThrowWktParseException("WKT2 derived projected CRS is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ThrowWktParseException($"WKT2 derived projected CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        baseProjectedCoordinateSystem = ArgumentGuard.ThrowIfNull(baseProjectedCoordinateSystem, nameof(baseProjectedCoordinateSystem));
        derivingConversion = ArgumentGuard.ThrowIfNull(derivingConversion, nameof(derivingConversion));
        AffineTransform transform = DerivedCoordinateSystemSupport.CreateAffineTransform(derivingConversion);
        var fittedCoordinateSystem = new FittedCoordinateSystem(
            baseProjectedCoordinateSystem,
            transform,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty,
            axisInfo);
        return fittedCoordinateSystem;
    }

    private static ProjectedCoordinateSystem ReadWkt2BaseProjectedCoordinateSystem(WktKeywordNode node) =>
        (ProjectedCoordinateSystem)ReadWkt2ProjectedCoordinateSystemCore(
            node,
            "BASEPROJCRS",
            "base projected CRS",
            "base projected coordinate system",
            allowOperationalEllipsoidalHeightCompound: false);

    private static CoordinateSystem ReadWkt2ProjectedCoordinateSystemCore(
        WktKeywordNode node,
        string rootKeyword,
        string crsContext,
        string coordinateSystemContext,
        bool allowOperationalEllipsoidalHeightCompound)
    {
        if (!node.KeywordEquals(rootKeyword))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in {rootKeyword}.");
        }

        GeographicCoordinateSystem? geographicCS = null;
        Projection? projection = null;
        AngularUnit? baseAngularUnit = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("BASEGEOGCRS") || keywordChild.KeywordEquals("BASEGEODCRS"))
            {
                geographicCS = ReadWkt2BaseGeographicCoordinateSystem(keywordChild);
            }
            else if (keywordChild.KeywordEquals("CONVERSION"))
            {
                projection = ReadWkt2Conversion(keywordChild, out AngularUnit? conversionAngularUnit);
                baseAngularUnit = MergeAxisAngularUnit(baseAngularUnit, conversionAngularUnit);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                axisInfo.Add(ReadWkt2Axis(keywordChild, out _, out LinearUnit? axisLinearUnit));
                linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
            }
            else if (keywordChild.KeywordEquals("LENGTHUNIT"))
            {
                linearUnit = ReadWkt2LinearUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (keywordChild.KeywordEquals("ENSEMBLE"))
            {
                throw new NotSupportedException("WKT2 datum ensembles are not supported.");
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        string name = node.GetStringChild(0);
        if (geographicCS is null)
        {
            ThrowWktParseException($"WKT2 {crsContext} is missing a BASEGEOGCRS block.");
        }

        if (projection is null)
        {
            ThrowWktParseException($"WKT2 {crsContext} is missing a CONVERSION block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ThrowWktParseException($"WKT2 {crsContext} is missing a CS block.");
        }

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 {coordinateSystemContext} type '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 2 && coordinateSystemDimension != 3)
        {
            throw new NotSupportedException($"WKT2 {crsContext} dimensions other than 2 or 3 are not supported.");
        }

        if (linearUnit is null)
        {
            ThrowWktParseException($"WKT2 {crsContext} is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ThrowWktParseException($"WKT2 {crsContext} declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        geographicCS = ArgumentGuard.ThrowIfNull(geographicCS, nameof(geographicCS));
        projection = ArgumentGuard.ThrowIfNull(projection, nameof(projection));
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        geographicCS = OverrideGeographicAngularUnit(geographicCS, baseAngularUnit);
        if (coordinateSystemDimension == 3)
        {
            if (!allowOperationalEllipsoidalHeightCompound)
            {
                throw new NotSupportedException($"WKT2 {crsContext} dimensions other than 2 are not supported.");
            }

            return CreateOperationalWkt2ProjectedEllipsoidalHeightCompoundCoordinateSystem(
                name,
                authority,
                authorityCode,
                geographicCS,
                linearUnit,
                projection,
                axisInfo);
        }

        return new ProjectedCoordinateSystem(
            geographicCS.HorizontalDatum,
            geographicCS,
            linearUnit,
            projection,
            axisInfo,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CompoundCoordinateSystem CreateOperationalWkt2ProjectedEllipsoidalHeightCompoundCoordinateSystem(
        string name,
        string authority,
        long authorityCode,
        GeographicCoordinateSystem geographicCoordinateSystem,
        LinearUnit linearUnit,
        Projection projection,
        List<AxisInfo> axisInfo)
    {
        var head = new ProjectedCoordinateSystem(
            geographicCoordinateSystem.HorizontalDatum,
            geographicCoordinateSystem,
            linearUnit,
            projection,
            [new AxisInfo(axisInfo[0]), new AxisInfo(axisInfo[1])],
            name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        var tail = new VerticalCoordinateSystem(
            linearUnit,
            new VerticalDatum(DatumType.VD_Ellipsoidal, "Ellipsoidal height datum", string.Empty, -1, string.Empty, string.Empty, string.Empty),
            new AxisInfo(axisInfo[2]),
            axisInfo[2].Name,
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        return new CompoundCoordinateSystem(head, tail, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static GeographicCoordinateSystem ReadWkt2BaseGeographicCoordinateSystem(WktKeywordNode node)
    {
        string rootKeyword = node.Keyword;
        string name = node.GetStringChild(0);
        HorizontalDatum? horizontalDatum = null;
        PrimeMeridian? primeMeridian = null;
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("DATUM"))
            {
                horizontalDatum = ReadWkt2HorizontalDatum(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ENSEMBLE"))
            {
                horizontalDatum = ReadWkt2HorizontalDatumEnsemble(keywordChild);
            }
            else if (keywordChild.KeywordEquals("PRIMEM"))
            {
                primeMeridian = ReadWkt2PrimeMeridian(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        if (horizontalDatum is null)
        {
            ThrowWktParseException($"WKT2 {rootKeyword} is missing a DATUM block.");
        }

        horizontalDatum = ArgumentGuard.ThrowIfNull(horizontalDatum, nameof(horizontalDatum));
        primeMeridian ??= PrimeMeridian.Greenwich;
        return new GeographicCoordinateSystem(
            AngularUnit.Degrees,
            horizontalDatum,
            primeMeridian,
            new List<AxisInfo>
            {
                new("Geodetic latitude (Lat)", AxisOrientationEnum.North),
                new("Geodetic longitude (Lon)", AxisOrientationEnum.East),
            },
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static Projection ReadWkt2Conversion(WktKeywordNode node, out AngularUnit? angularUnit)
    {
        return ReadWkt2Conversion(node, "CONVERSION", out angularUnit);
    }

    private static Projection ReadWkt2DerivingConversion(WktKeywordNode node, out AngularUnit? angularUnit)
    {
        return ReadWkt2Conversion(node, "DERIVINGCONVERSION", out angularUnit);
    }

    private static Projection ReadWkt2Conversion(WktKeywordNode node, string keyword, out AngularUnit? angularUnit)
    {
        string conversionName = node.GetStringChild(0);

        string methodName = string.Empty;
        string authority = string.Empty;
        long authorityCode = -1;
        angularUnit = null;
        var parameters = new List<ProjectionParameter>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("METHOD"))
            {
                methodName = ReadWkt2ProjectionMethod(keywordChild);
            }
            else if (keywordChild.KeywordEquals("PARAMETER"))
            {
                parameters.Add(ReadWkt2ProjectionParameter(keywordChild, out AngularUnit? parameterAngularUnit));
                angularUnit = MergeAxisAngularUnit(angularUnit, parameterAngularUnit);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 {keyword} keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            ThrowWktParseException($"WKT2 {keyword} is missing a METHOD block.");
        }

        return new Projection(methodName, parameters, conversionName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static string ReadWkt2ProjectionMethod(WktKeywordNode node)
    {
        string methodName = node.GetStringChild(0);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (!keywordChild.KeywordEquals("ID") && !ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 METHOD keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return methodName;
    }

    private static ProjectionParameter ReadWkt2ProjectionParameter(WktKeywordNode node, out AngularUnit? angularUnit)
    {
        string parameterName = NormalizeWkt2ProjectionParameterName(node.GetStringChild(0));
        double value = node.GetNumberChild(1);
        angularUnit = null;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ANGLEUNIT"))
            {
                angularUnit = ReadWkt2AngularUnit(keywordChild);
            }
            else if (!keywordChild.KeywordEquals("ID")
                && !keywordChild.KeywordEquals("LENGTHUNIT")
                && !keywordChild.KeywordEquals("SCALEUNIT"))
            {
                if (!ShouldSkipWkt2MetadataNode(keywordChild))
                {
                    throw new NotSupportedException($"WKT2 PARAMETER keyword '{keywordChild.Keyword}' is not supported.");
                }
            }
        }

        return new ProjectionParameter(parameterName, value);
    }

    private static string NormalizeWkt2ProjectionParameterName(string parameterName) => ProjectionParameterNameNormalizer.Normalize(parameterName);

    private static VerticalCoordinateSystem ReadWkt2VerticalCoordinateSystem(WktKeywordNode node)
    {
        const string rootKeyword = "VERTCRS";

        VerticalDatum? verticalDatum = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("VDATUM"))
            {
                verticalDatum = ReadWkt2VerticalDatum(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ENSEMBLE"))
            {
                verticalDatum = ReadWkt2VerticalDatumEnsemble(keywordChild);
            }
            else if (keywordChild.KeywordEquals("CS"))
            {
                (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(keywordChild);
            }
            else if (keywordChild.KeywordEquals("AXIS"))
            {
                axisInfo.Add(ReadWkt2Axis(keywordChild, out _, out LinearUnit? axisLinearUnit));
                linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
            }
            else if (keywordChild.KeywordEquals("LENGTHUNIT"))
            {
                linearUnit = ReadWkt2LinearUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        string name = node.GetStringChild(0);
        if (verticalDatum is null)
        {
            ThrowWktParseException("WKT2 vertical CRS is missing a VDATUM or ENSEMBLE block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ThrowWktParseException("WKT2 vertical CRS is missing a CS block.");
        }

        if (!string.Equals(coordinateSystemType, "vertical", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 vertical coordinate system type '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 1)
        {
            throw new NotSupportedException("WKT2 vertical CRS dimensions other than 1 are not supported.");
        }

        if (linearUnit is null)
        {
            ThrowWktParseException("WKT2 vertical CRS is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ThrowWktParseException($"WKT2 vertical CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        verticalDatum = ApplyVerticalDatumTypeForAxis(ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum)), axisInfo[0]);
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));
        return new VerticalCoordinateSystem(
            linearUnit,
            verticalDatum,
            axisInfo[0],
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static VerticalDatum ReadWkt2VerticalDatum(WktKeywordNode node)
    {
        if (!node.KeywordEquals("VDATUM"))
        {
            throw new NotSupportedException($"WKT2 keyword '{node.Keyword}' is not supported in VDATUM.");
        }

        string name = node.GetStringChild(0);
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 VDATUM keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return new VerticalDatum(DatumType.VD_GeoidModelDerived, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static VerticalDatum ReadWkt2VerticalDatumEnsemble(WktKeywordNode node)
    {
        DatumEnsemble ensemble = ReadWkt2DatumEnsemble(node, requireEllipsoid: false);
        return new VerticalDatum(DatumType.VD_GeoidModelDerived, ensemble.Name, ensemble.Authority, ensemble.AuthorityCode, string.Empty, string.Empty, string.Empty, ensemble);
    }

    private static VerticalDatum ApplyVerticalDatumTypeForAxis(VerticalDatum verticalDatum, AxisInfo axisInfo)
    {
        axisInfo = ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));

        DatumType datumType = axisInfo.Orientation == AxisOrientationEnum.Down
            ? DatumType.VD_Depth
            : DatumType.VD_GeoidModelDerived;
        if (verticalDatum.DatumType == datumType)
        {
            return verticalDatum;
        }

        return new VerticalDatum(
            datumType,
            verticalDatum.Name,
            verticalDatum.Authority,
            verticalDatum.AuthorityCode,
            verticalDatum.Alias,
            verticalDatum.Remarks,
            verticalDatum.Abbreviation,
            verticalDatum.Ensemble);
    }

    private static CompoundCoordinateSystem ReadWkt2CompoundCoordinateSystem(WktKeywordNode node)
    {
        const string rootKeyword = "COMPOUNDCRS";
        string name = node.GetStringChild(0);

        CoordinateSystem? headCoordinateSystem = null;
        CoordinateSystem? tailCoordinateSystem = null;
        string authority = string.Empty;
        long authorityCode = -1;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (headCoordinateSystem is null)
            {
                headCoordinateSystem = ReadWkt2CoordinateSystemNode(keywordChild);
            }
            else if (tailCoordinateSystem is null)
            {
                tailCoordinateSystem = ReadWkt2CoordinateSystemNode(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ID"))
            {
                ReadIdentifierWithUnknownCode(keywordChild, out authority, out authorityCode);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        headCoordinateSystem = ArgumentGuard.ThrowIfNull(headCoordinateSystem, nameof(headCoordinateSystem));
        tailCoordinateSystem = ArgumentGuard.ThrowIfNull(tailCoordinateSystem, nameof(tailCoordinateSystem));
        return new CompoundCoordinateSystem(headCoordinateSystem, tailCoordinateSystem, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static BoundCoordinateSystem ReadWkt2BoundCoordinateSystem(WktKeywordNode node)
    {
        const string rootKeyword = "BOUNDCRS";

        CoordinateSystem? sourceCoordinateSystem = null;
        CoordinateSystem? targetCoordinateSystem = null;
        BoundTransformation? transformation = null;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("SOURCECRS"))
            {
                sourceCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(keywordChild);
                EnsureSupportedWkt2BoundSourceCoordinateSystem(sourceCoordinateSystem);
            }
            else if (keywordChild.KeywordEquals("TARGETCRS"))
            {
                targetCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(keywordChild);
            }
            else if (keywordChild.KeywordEquals("ABRIDGEDTRANSFORMATION"))
            {
                transformation = ReadWkt2AbridgedTransformationDefinition(keywordChild);
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {rootKeyword}.");
            }
        }

        sourceCoordinateSystem = ArgumentGuard.ThrowIfNull(sourceCoordinateSystem, nameof(sourceCoordinateSystem));
        targetCoordinateSystem = ArgumentGuard.ThrowIfNull(targetCoordinateSystem, nameof(targetCoordinateSystem));
        transformation = ArgumentGuard.ThrowIfNull(transformation, nameof(transformation));

        return new BoundCoordinateSystem(
            sourceCoordinateSystem,
            targetCoordinateSystem,
            transformation,
            sourceCoordinateSystem.Name,
            sourceCoordinateSystem.Authority,
            sourceCoordinateSystem.AuthorityCode,
            sourceCoordinateSystem.Alias,
            sourceCoordinateSystem.Abbreviation,
            sourceCoordinateSystem.Remarks);
    }

    private static CoordinateSystem ReadWkt2BoundCoordinateSystemComponent(WktKeywordNode node)
    {
        WktKeywordNode? coordinateSystemNode = null;
        bool foundCoordinateSystemNode = false;
        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (!foundCoordinateSystemNode)
            {
                coordinateSystemNode = keywordChild;
                foundCoordinateSystemNode = true;
            }
            else if (!ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 keyword '{keywordChild.Keyword}' is not supported in {node.Keyword}.");
            }
        }

        return ReadWkt2CoordinateSystemNode(ArgumentGuard.ThrowIfNull(coordinateSystemNode, nameof(coordinateSystemNode)));
    }

    private static void EnsureSupportedWkt2BoundSourceCoordinateSystem(CoordinateSystem coordinateSystem)
    {
        if (coordinateSystem is BoundCoordinateSystem boundCoordinateSystem)
        {
            EnsureSupportedWkt2BoundSourceCoordinateSystem(boundCoordinateSystem.SourceCoordinateSystem);
            return;
        }

        if (coordinateSystem is VerticalCoordinateSystem || BoundCoordinateSystemSupport.TryGetHorizontalDatum(coordinateSystem, out _))
        {
            return;
        }

        throw new NotSupportedException(
            $"WKT2 BOUNDCRS source coordinate system type '{BoundCoordinateSystemSupport.GetCoordinateSystemKeyword(coordinateSystem)}' is not supported.");
    }

    private static BoundTransformation ReadWkt2AbridgedTransformationDefinition(WktKeywordNode node)
    {
        _ = node.GetStringChild(0);

        string methodName = string.Empty;
        string? parameterFileName = null;
        var parameters = new Wgs84ConversionInfo();

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("METHOD"))
            {
                methodName = ReadWkt2ProjectionMethod(keywordChild);
            }
            else if (keywordChild.KeywordEquals("PARAMETER"))
            {
                ReadWkt2AbridgedTransformationParameter(keywordChild, parameters);
            }
            else if (keywordChild.KeywordEquals("PARAMETERFILE"))
            {
                parameterFileName = ReadWkt2AbridgedTransformationParameterFile(keywordChild);
            }
            else if (!keywordChild.KeywordEquals("ID"))
            {
                if (!ShouldSkipWkt2MetadataNode(keywordChild))
                {
                    throw new NotSupportedException($"WKT2 ABRIDGEDTRANSFORMATION keyword '{keywordChild.Keyword}' is not supported.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            ThrowWktParseException("WKT2 ABRIDGEDTRANSFORMATION is missing a METHOD block.");
        }

        return BoundCoordinateSystemSupport.CreateBoundTransformation(
            methodName,
            string.IsNullOrWhiteSpace(parameterFileName) ? parameters : null,
            parameterFileName);
    }

    private static void ReadWkt2AbridgedTransformationParameter(WktKeywordNode node, Wgs84ConversionInfo parameters)
    {
        string parameterName = NormalizeWkt2BoundTransformationParameterName(node.GetStringChild(0));
        double value = node.GetNumberChild(1);

        AngularUnit? angularUnit = null;
        LinearUnit? linearUnit = null;
        double? scaleUnitFactor = null;

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (keywordChild.KeywordEquals("ANGLEUNIT"))
            {
                angularUnit = ReadWkt2AngularUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("LENGTHUNIT"))
            {
                linearUnit = ReadWkt2LinearUnit(keywordChild);
            }
            else if (keywordChild.KeywordEquals("SCALEUNIT"))
            {
                scaleUnitFactor = ReadWkt2ScaleUnitFactor(keywordChild);
            }
            else if (!keywordChild.KeywordEquals("ID"))
            {
                if (!ShouldSkipWkt2MetadataNode(keywordChild))
                {
                    throw new NotSupportedException($"WKT2 ABRIDGEDTRANSFORMATION parameter keyword '{keywordChild.Keyword}' is not supported.");
                }
            }
        }

        ApplyWkt2BoundTransformationParameter(
            parameters,
            parameterName,
            NormalizeWkt2BoundTransformationParameterValue(parameterName, value, angularUnit, linearUnit, scaleUnitFactor));
    }

    private static string ReadWkt2AbridgedTransformationParameterFile(WktKeywordNode node)
    {
        _ = node.GetStringChild(0);
        string parameterFileName = node.GetStringChild(1);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (!keywordChild.KeywordEquals("ID") && !ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 PARAMETERFILE keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return parameterFileName;
    }

    private static double ReadWkt2ScaleUnitFactor(WktKeywordNode node)
    {
        _ = node.GetStringChild(0);
        double unitFactor = node.GetNumberChild(1);

        ReadOnlySpan<WktNode> children = node.GetChildrenSpan();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is not WktKeywordNode keywordChild)
            {
                continue;
            }

            if (!keywordChild.KeywordEquals("ID") && !ShouldSkipWkt2MetadataNode(keywordChild))
            {
                throw new NotSupportedException($"WKT2 SCALEUNIT keyword '{keywordChild.Keyword}' is not supported.");
            }
        }

        return unitFactor;
    }

    private static string NormalizeWkt2BoundTransformationParameterName(string parameterName)
    {
        string normalized = ProjectionParameterNameNormalizer.NormalizeLookupToken(parameterName);

        return normalized switch
        {
            "X_AXIS_TRANSLATION" => "dx",
            "Y_AXIS_TRANSLATION" => "dy",
            "Z_AXIS_TRANSLATION" => "dz",
            "X_AXIS_ROTATION" => "ex",
            "Y_AXIS_ROTATION" => "ey",
            "Z_AXIS_ROTATION" => "ez",
            "SCALE_DIFFERENCE" => "ppm",
            _ => normalized,
        };
    }

    private static double NormalizeWkt2BoundTransformationParameterValue(
        string parameterName,
        double value,
        AngularUnit? angularUnit,
        LinearUnit? linearUnit,
        double? scaleUnitFactor)
    {
        return parameterName switch
        {
            "dx" or "dy" or "dz" => linearUnit is null ? value : value * linearUnit.MetersPerUnit,
            "ex" or "ey" or "ez" => angularUnit is null ? value : (value * angularUnit.RadiansPerUnit) / RadiansPerArcSecond,
            "ppm" => scaleUnitFactor.HasValue ? value * scaleUnitFactor.Value * 1000000d : value,
            _ => throw new NotSupportedException($"WKT2 BOUNDCRS transformation parameter '{parameterName}' is not supported."),
        };
    }

    private static void ApplyWkt2BoundTransformationParameter(Wgs84ConversionInfo parameters, string parameterName, double value)
    {
        BoundCoordinateSystemSupport.AssignTransformationParameter(parameterName, value, parameters);
    }
}
