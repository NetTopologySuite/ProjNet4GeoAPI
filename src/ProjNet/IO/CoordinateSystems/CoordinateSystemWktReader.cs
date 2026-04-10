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
using System.Linq;
using System.Text.RegularExpressions;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Creates an object based on the supplied Well Known Text (WKT).
/// </summary>
public static partial class CoordinateSystemWktReader
{
    private const double RadiansPerArcSecond = 4.84813681109535993589914102357e-6;
    private static readonly string[] CompoundCoordinateSystemDelimiters = [",", "]"];
#if !NET8_0_OR_GREATER
    private static readonly Regex Wkt2IdRegex = new(@"\bID\s*\[(?=\s*"")", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
#endif

    /// <summary>
    /// Reads and parses a WKT-formatted projection string.
    /// </summary>
    /// <param name="wkt">String containing WKT.</param>
    /// <returns>Object representation of the WKT.</returns>
    /// <exception cref="ArgumentException">If a token is not recognized.</exception>
    public static IInfo Parse(string wkt) => Parse(wkt.AsSpan());

    /// <summary>
    /// Reads and parses a WKT-formatted projection text from a character span.
    /// </summary>
    /// <param name="wkt">Character span containing WKT.</param>
    /// <returns>Object representation of the WKT.</returns>
    /// <exception cref="ArgumentException">If a token is not recognized.</exception>
    public static IInfo Parse(ReadOnlySpan<char> wkt)
    {
        if (wkt.IsEmpty || IsWhitespaceOnly(wkt))
        {
            ArgumentGuard.ThrowArgumentNull(nameof(wkt));
        }

        string wktText = wkt.ToString();
        if (TryParseNativeWkt2(wktText, out IInfo? nativeWkt2Info))
        {
            return ArgumentGuard.ThrowIfNull(nativeWkt2Info, nameof(nativeWkt2Info));
        }

        string normalizedWkt = NormalizeWkt(wktText);
        return ParseNormalizedWkt(normalizedWkt);
    }

#if NET8_0_OR_GREATER
    [GeneratedRegex(@"\bID\s*\[(?=\s*"")", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex Wkt2IdRegex();
#endif

    private static bool IsWhitespaceOnly(ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseNativeWkt2(string wkt, out IInfo? info)
    {
        var tokenizer = new WktTokenizer(wkt);
        tokenizer.NextToken();
        switch (tokenizer.GetStringValue())
        {
            case "GEOGCRS":
            case "GEODCRS":
            case "GEODETICCRS":
                if (!ContainsKeywordBlock(wkt, "CS"))
                {
                    info = null;
                    return false;
                }

                info = ReadWkt2GeodeticCoordinateReferenceSystem(tokenizer);
                return true;
            case "PROJCRS":
                if (!ContainsKeywordBlock(wkt, "CONVERSION") || !ContainsKeywordBlock(wkt, "CS"))
                {
                    info = null;
                    return false;
                }

                info = ReadWkt2ProjectedCoordinateSystem(tokenizer);
                return true;
            case "DERIVEDPROJCRS":
                if (!ContainsKeywordBlock(wkt, "DERIVINGCONVERSION") || !ContainsKeywordBlock(wkt, "CS"))
                {
                    info = null;
                    return false;
                }

                info = ReadWkt2DerivedProjectedCoordinateSystem(tokenizer);
                return true;
            case "VERTCRS":
                if ((!ContainsKeywordBlock(wkt, "VDATUM") && !ContainsKeywordBlock(wkt, "ENSEMBLE")) || !ContainsKeywordBlock(wkt, "CS"))
                {
                    info = null;
                    return false;
                }

                info = ReadWkt2VerticalCoordinateSystem(tokenizer);
                return true;
            case "COMPOUNDCRS":
                info = ReadWkt2CompoundCoordinateSystem(tokenizer);
                return true;
            case "BOUNDCRS":
                if (!HasCompleteWkt2BoundCoordinateSystemBlocks(wkt))
                {
                    info = null;
                    return false;
                }

                info = ReadWkt2BoundCoordinateSystem(tokenizer);
                return true;
            default:
                info = null;
                return false;
        }
    }

    private static bool ContainsKeywordBlock(string wkt, string keyword)
    {
        int index = wkt.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            int probeIndex = index + keyword.Length;
            while (probeIndex < wkt.Length && char.IsWhiteSpace(wkt[probeIndex]))
            {
                probeIndex++;
            }

            if (probeIndex < wkt.Length && wkt[probeIndex] == '[')
            {
                return true;
            }

            index = wkt.IndexOf(keyword, index + 1, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool HasCompleteWkt2BoundCoordinateSystemBlocks(string wkt)
    {
        return ContainsKeywordBlock(wkt, "SOURCECRS")
            && ContainsKeywordBlock(wkt, "TARGETCRS")
            && ContainsKeywordBlock(wkt, "ABRIDGEDTRANSFORMATION");
    }

    private static CoordinateSystem ReadWkt2GeodeticCoordinateReferenceSystem(WktTokenizer tokenizer)
    {
        string rootKeyword = tokenizer.GetStringValue();
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

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

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "DATUM":
                    horizontalDatum = ReadWkt2HorizontalDatum(tokenizer);
                    break;
                case "ENSEMBLE":
                    horizontalDatum = ReadWkt2HorizontalDatumEnsemble(tokenizer);
                    break;
                case "BASEGEOGCRS":
                case "BASEGEODCRS":
                    baseGeographicCoordinateSystem = ReadWkt2BaseGeographicCoordinateSystem(tokenizer);
                    break;
                case "DERIVINGCONVERSION":
                    derivingConversion = ReadWkt2DerivingConversion(tokenizer, out AngularUnit? derivingAngularUnit);
                    angularUnit = MergeAxisAngularUnit(angularUnit, derivingAngularUnit);
                    break;
                case "PRIMEM":
                    primeMeridian = ReadWkt2PrimeMeridian(tokenizer);
                    break;
                case "CS":
                    (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(tokenizer);
                    break;
                case "AXIS":
                    axisInfo.Add(ReadWkt2Axis(tokenizer, out AngularUnit? axisAngularUnit, out LinearUnit? axisLinearUnit));
                    angularUnit = MergeAxisAngularUnit(angularUnit, axisAngularUnit);
                    linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
                    break;
                case "ANGLEUNIT":
                    angularUnit = ReadWkt2AngularUnit(tokenizer);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        bool isDerived = baseGeographicCoordinateSystem is not null || derivingConversion is not null;
        if (isDerived)
        {
            if (horizontalDatum is not null)
            {
                ArgumentGuard.ThrowArgument("WKT2 derived geodetic CRS must use BASEGEOGCRS or BASEGEODCRS instead of a top-level DATUM block.");
            }

            if (baseGeographicCoordinateSystem is null)
            {
                ArgumentGuard.ThrowArgument("WKT2 derived geodetic CRS is missing a BASEGEOGCRS or BASEGEODCRS block.");
            }

            if (derivingConversion is null)
            {
                ArgumentGuard.ThrowArgument("WKT2 derived geodetic CRS is missing a DERIVINGCONVERSION block.");
            }

            if (string.IsNullOrWhiteSpace(coordinateSystemType))
            {
                ArgumentGuard.ThrowArgument("WKT2 derived geodetic CRS is missing a CS block.");
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
                ArgumentGuard.ThrowArgument($"WKT2 derived geodetic CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
            }

            if (angularUnit is null)
            {
                ArgumentGuard.ThrowArgument("WKT2 derived geodetic CRS is missing an ANGLEUNIT block.");
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
                string.Empty);
            fittedCoordinateSystem.AxisInfo = axisInfo;
            return fittedCoordinateSystem;
        }

        if (horizontalDatum is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 geodetic CRS is missing a DATUM block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ArgumentGuard.ThrowArgument("WKT2 geodetic CRS is missing a CS block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ArgumentGuard.ThrowArgument($"WKT2 geodetic CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        if (string.Equals(coordinateSystemType, "ellipsoidal", StringComparison.OrdinalIgnoreCase))
        {
            if (coordinateSystemDimension == 3)
            {
                if (angularUnit is null)
                {
                    ArgumentGuard.ThrowArgument("WKT2 ellipsoidal CRS is missing ANGLEUNIT metadata.");
                }

                if (linearUnit is null)
                {
                    ArgumentGuard.ThrowArgument("WKT2 three-dimensional ellipsoidal CRS is missing LENGTHUNIT metadata.");
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
                ArgumentGuard.ThrowArgument("WKT2 ellipsoidal CRS is missing ANGLEUNIT metadata.");
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
                ArgumentGuard.ThrowArgument("WKT2 cartesian geodetic CRS is missing LENGTHUNIT metadata.");
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

    private static (string Type, int Dimension) ReadWkt2CoordinateSystemDefinition(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "CS")
        {
            tokenizer.ReadToken("CS");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        tokenizer.NextToken();
        string coordinateSystemType = tokenizer.GetStringValue();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        int dimension = (int)tokenizer.GetNumericValue();
        tokenizer.NextToken();

        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID" || ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
                tokenizer.NextToken();
                continue;
            }

            throw new NotSupportedException($"WKT2 CS keyword '{tokenizer.GetStringValue()}' is not supported.");
        }

        return (coordinateSystemType, dimension);
    }

    private static AxisInfo ReadWkt2Axis(WktTokenizer tokenizer, out AngularUnit? angularUnit, out LinearUnit? linearUnit)
    {
        if (tokenizer.GetStringValue() != "AXIS")
        {
            tokenizer.ReadToken("AXIS");
        }

        angularUnit = null;
        linearUnit = null;

        WktBracket bracket = tokenizer.ReadOpener();
        string axisName = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        AxisOrientationEnum orientation = ParseWkt2AxisOrientation(tokenizer.GetStringValue());
        tokenizer.NextToken();

        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "ANGLEUNIT":
                    angularUnit = ReadWkt2AngularUnit(tokenizer);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    SkipKeywordNode(tokenizer);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 AXIS keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        return new AxisInfo(axisName, orientation);
    }

    private static AxisOrientationEnum ParseWkt2AxisOrientation(string orientationToken)
    {
        return orientationToken.ToUpperInvariant() switch
        {
            "NORTH" => AxisOrientationEnum.North,
            "SOUTH" => AxisOrientationEnum.South,
            "EAST" => AxisOrientationEnum.East,
            "WEST" => AxisOrientationEnum.West,
            "UP" => AxisOrientationEnum.Up,
            "DOWN" => AxisOrientationEnum.Down,
            "GEOCENTRICX" => AxisOrientationEnum.Other,
            "GEOCENTRICY" => AxisOrientationEnum.East,
            "GEOCENTRICZ" => AxisOrientationEnum.North,
            _ => ArgumentGuard.ThrowArgument<AxisOrientationEnum>($"Invalid WKT2 axis orientation '{orientationToken}'."),
        };
    }

    private static HorizontalDatum ReadWkt2HorizontalDatum(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "DATUM")
        {
            tokenizer.ReadToken("DATUM");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        string authority = string.Empty;
        long authorityCode = -1;
        Ellipsoid? ellipsoid = null;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "ELLIPSOID":
                    ellipsoid = ReadWkt2Ellipsoid(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 DATUM keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (ellipsoid is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 DATUM is missing an ELLIPSOID block.");
        }

        return new HorizontalDatum(ellipsoid, null, DatumType.HD_Geocentric, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static HorizontalDatum ReadWkt2HorizontalDatumEnsemble(WktTokenizer tokenizer)
    {
        DatumEnsemble ensemble = ReadWkt2DatumEnsemble(tokenizer, requireEllipsoid: true);
        Ellipsoid ellipsoid = ArgumentGuard.ThrowIfNull(ensemble.Ellipsoid, nameof(ensemble));
        return new HorizontalDatum(ellipsoid, null, DatumType.HD_Geocentric, ensemble.Name, ensemble.Authority, ensemble.AuthorityCode, string.Empty, string.Empty, string.Empty)
        {
            Ensemble = ensemble,
        };
    }

    private static DatumEnsemble ReadWkt2DatumEnsemble(WktTokenizer tokenizer, bool requireEllipsoid)
    {
        if (tokenizer.GetStringValue() != "ENSEMBLE")
        {
            tokenizer.ReadToken("ENSEMBLE");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        var members = new List<DatumEnsembleMember>();
        Ellipsoid? ellipsoid = null;
        double? accuracy = null;
        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "MEMBER":
                    members.Add(ReadWkt2DatumEnsembleMember(tokenizer));
                    break;
                case "ELLIPSOID":
                    ellipsoid = ReadWkt2Ellipsoid(tokenizer);
                    break;
                case "ENSEMBLEACCURACY":
                    accuracy = ReadWkt2DatumEnsembleAccuracy(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 ENSEMBLE keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (members.Count == 0)
        {
            ArgumentGuard.ThrowArgument("WKT2 ENSEMBLE is missing MEMBER blocks.");
        }

        if (requireEllipsoid && ellipsoid is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 ENSEMBLE is missing an ELLIPSOID block.");
        }

        if (accuracy is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 ENSEMBLE is missing an ENSEMBLEACCURACY block.");
        }

        return new DatumEnsemble(name, members, accuracy.Value, ellipsoid, authority, authorityCode);
    }

    private static DatumEnsembleMember ReadWkt2DatumEnsembleMember(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "MEMBER")
        {
            tokenizer.ReadToken("MEMBER");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID")
            {
                ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
            }
            else if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 MEMBER keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return new DatumEnsembleMember(name, authority, authorityCode);
    }

    private static double ReadWkt2DatumEnsembleAccuracy(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "ENSEMBLEACCURACY")
        {
            tokenizer.ReadToken("ENSEMBLEACCURACY");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        tokenizer.NextToken();
        double accuracy = tokenizer.GetNumericValue();
        tokenizer.NextToken();
        tokenizer.CheckCloser(bracket);
        return accuracy;
    }

    private static Ellipsoid ReadWkt2Ellipsoid(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "ELLIPSOID")
        {
            tokenizer.ReadToken("ELLIPSOID");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double semiMajorAxis = tokenizer.GetNumericValue();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double inverseFlattening = tokenizer.GetNumericValue();

        string authority = string.Empty;
        long authorityCode = -1;
        LinearUnit? axisUnit = null;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "LENGTHUNIT":
                    axisUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 ELLIPSOID keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (axisUnit is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 ELLIPSOID is missing a LENGTHUNIT block.");
        }

        return new Ellipsoid(semiMajorAxis, 0d, inverseFlattening, true, axisUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static PrimeMeridian ReadWkt2PrimeMeridian(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "PRIMEM")
        {
            tokenizer.ReadToken("PRIMEM");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double longitude = tokenizer.GetNumericValue();

        string authority = string.Empty;
        long authorityCode = -1;
        AngularUnit angularUnit = AngularUnit.Degrees;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "ANGLEUNIT":
                    angularUnit = ReadWkt2AngularUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 PRIMEM keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        return new PrimeMeridian(longitude, angularUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static AngularUnit ReadWkt2AngularUnit(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "ANGLEUNIT")
        {
            tokenizer.ReadToken("ANGLEUNIT");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double radiansPerUnit = tokenizer.GetNumericValue();

        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID")
            {
                ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
            }
            else if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 ANGLEUNIT keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return new AngularUnit(radiansPerUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static LinearUnit ReadWkt2LinearUnit(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "LENGTHUNIT")
        {
            tokenizer.ReadToken("LENGTHUNIT");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double metersPerUnit = tokenizer.GetNumericValue();

        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID")
            {
                ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
            }
            else if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 LENGTHUNIT keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return new LinearUnit(metersPerUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static void ReadIdentifierWithUnknownCode(WktTokenizer tokenizer, out string authority, out long authorityCode)
    {
        if (tokenizer.GetStringValue() != "ID")
        {
            tokenizer.ReadToken("ID");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        authority = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        if (tokenizer.GetTokenType() == TokenType.Number)
        {
            authorityCode = (long)tokenizer.GetNumericValue();
        }
        else if (tokenizer.GetTokenType() == TokenType.Word)
        {
            authorityCode = long.TryParse(tokenizer.GetStringValue(), NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedCode)
                ? parsedCode
                : -1;
        }
        else
        {
            authorityCode = long.TryParse(tokenizer.ReadDoubleQuotedWord(), NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedCode)
                ? parsedCode
                : -1;
        }

        tokenizer.ReadCloser(bracket);
    }

    private static void SkipKeywordNode(WktTokenizer tokenizer)
    {
        _ = tokenizer.ReadOpener();
        int depth = 1;
        while (depth > 0)
        {
            TokenType tokenType = tokenizer.NextToken(false);
            if (tokenType == TokenType.Eof)
            {
                ArgumentGuard.ThrowArgument("Unexpected end of input while skipping WKT2 metadata node.");
            }

            string token = tokenizer.GetStringValue();
            if (token is "[" or "(")
            {
                depth++;
            }
            else if (token is "]" or ")")
            {
                depth--;
            }
        }
    }

    private static bool ShouldSkipWkt2MetadataNode(string keyword)
    {
        return keyword is "ANCHOR"
            or "ANCHOREPOCH"
            or "AREA"
            or "BBOX"
            or "DEFININGTRANSFORMATION"
            or "DYNAMIC"
            or "GEOIDMODEL"
            or "MERIDIAN"
            or "ORDER"
            or "REMARK"
            or "SCOPE"
            or "VERSION"
            or "USAGE";
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

    private static ProjectedCoordinateSystem ReadWkt2ProjectedCoordinateSystem(WktTokenizer tokenizer)
    {
        const string rootKeyword = "PROJCRS";

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

        GeographicCoordinateSystem? geographicCS = null;
        Projection? projection = null;
        AngularUnit? baseAngularUnit = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "BASEGEOGCRS":
                case "BASEGEODCRS":
                    geographicCS = ReadWkt2BaseGeographicCoordinateSystem(tokenizer);
                    break;
                case "CONVERSION":
                    projection = ReadWkt2Conversion(tokenizer, out AngularUnit? conversionAngularUnit);
                    baseAngularUnit = MergeAxisAngularUnit(baseAngularUnit, conversionAngularUnit);
                    break;
                case "CS":
                    (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(tokenizer);
                    break;
                case "AXIS":
                    axisInfo.Add(ReadWkt2Axis(tokenizer, out _, out LinearUnit? axisLinearUnit));
                    linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                case "ENSEMBLE":
                    throw new NotSupportedException("WKT2 datum ensembles are not supported.");
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (geographicCS is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 projected CRS is missing a BASEGEOGCRS block.");
        }

        if (projection is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 projected CRS is missing a CONVERSION block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ArgumentGuard.ThrowArgument("WKT2 projected CRS is missing a CS block.");
        }

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 projected coordinate system type '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("WKT2 projected CRS dimensions other than 2 are not supported.");
        }

        if (linearUnit is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 projected CRS is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ArgumentGuard.ThrowArgument($"WKT2 projected CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        geographicCS = ArgumentGuard.ThrowIfNull(geographicCS, nameof(geographicCS));
        projection = ArgumentGuard.ThrowIfNull(projection, nameof(projection));
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));

        if (baseAngularUnit is not null)
        {
            geographicCS.AngularUnit = baseAngularUnit;
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

    private static FittedCoordinateSystem ReadWkt2DerivedProjectedCoordinateSystem(WktTokenizer tokenizer)
    {
        const string rootKeyword = "DERIVEDPROJCRS";

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

        ProjectedCoordinateSystem? baseProjectedCoordinateSystem = null;
        Projection? derivingConversion = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "BASEPROJCRS":
                    baseProjectedCoordinateSystem = ReadWkt2BaseProjectedCoordinateSystem(tokenizer);
                    break;
                case "DERIVINGCONVERSION":
                    derivingConversion = ReadWkt2DerivingConversion(tokenizer, out _);
                    break;
                case "CS":
                    (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(tokenizer);
                    break;
                case "AXIS":
                    axisInfo.Add(ReadWkt2Axis(tokenizer, out _, out LinearUnit? axisLinearUnit));
                    linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (baseProjectedCoordinateSystem is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 derived projected CRS is missing a BASEPROJCRS block.");
        }

        if (derivingConversion is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 derived projected CRS is missing a DERIVINGCONVERSION block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ArgumentGuard.ThrowArgument("WKT2 derived projected CRS is missing a CS block.");
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
            ArgumentGuard.ThrowArgument("WKT2 derived projected CRS is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ArgumentGuard.ThrowArgument($"WKT2 derived projected CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        AffineTransform transform = DerivedCoordinateSystemSupport.CreateAffineTransform(derivingConversion);
        var fittedCoordinateSystem = new FittedCoordinateSystem(
            baseProjectedCoordinateSystem,
            transform,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
        fittedCoordinateSystem.AxisInfo = axisInfo;
        return fittedCoordinateSystem;
    }

    private static ProjectedCoordinateSystem ReadWkt2BaseProjectedCoordinateSystem(WktTokenizer tokenizer)
    {
        const string rootKeyword = "BASEPROJCRS";

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

        GeographicCoordinateSystem? geographicCS = null;
        Projection? projection = null;
        AngularUnit? baseAngularUnit = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "BASEGEOGCRS":
                case "BASEGEODCRS":
                    geographicCS = ReadWkt2BaseGeographicCoordinateSystem(tokenizer);
                    break;
                case "CONVERSION":
                    projection = ReadWkt2Conversion(tokenizer, out AngularUnit? conversionAngularUnit);
                    baseAngularUnit = MergeAxisAngularUnit(baseAngularUnit, conversionAngularUnit);
                    break;
                case "CS":
                    (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(tokenizer);
                    break;
                case "AXIS":
                    axisInfo.Add(ReadWkt2Axis(tokenizer, out _, out LinearUnit? axisLinearUnit));
                    linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (geographicCS is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 base projected CRS is missing a BASEGEOGCRS block.");
        }

        if (projection is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 base projected CRS is missing a CONVERSION block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ArgumentGuard.ThrowArgument("WKT2 base projected CRS is missing a CS block.");
        }

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"WKT2 base projected coordinate system type '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("WKT2 base projected CRS dimensions other than 2 are not supported.");
        }

        if (linearUnit is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 base projected CRS is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ArgumentGuard.ThrowArgument($"WKT2 base projected CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        geographicCS = ArgumentGuard.ThrowIfNull(geographicCS, nameof(geographicCS));
        projection = ArgumentGuard.ThrowIfNull(projection, nameof(projection));
        linearUnit = ArgumentGuard.ThrowIfNull(linearUnit, nameof(linearUnit));

        if (baseAngularUnit is not null)
        {
            geographicCS.AngularUnit = baseAngularUnit;
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

    private static GeographicCoordinateSystem ReadWkt2BaseGeographicCoordinateSystem(WktTokenizer tokenizer)
    {
        string rootKeyword = tokenizer.GetStringValue();
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

        HorizontalDatum? horizontalDatum = null;
        PrimeMeridian? primeMeridian = null;
        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "DATUM":
                    horizontalDatum = ReadWkt2HorizontalDatum(tokenizer);
                    break;
                case "ENSEMBLE":
                    horizontalDatum = ReadWkt2HorizontalDatumEnsemble(tokenizer);
                    break;
                case "PRIMEM":
                    primeMeridian = ReadWkt2PrimeMeridian(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (horizontalDatum is null)
        {
            ArgumentGuard.ThrowArgument($"WKT2 {rootKeyword} is missing a DATUM block.");
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

    private static Projection ReadWkt2Conversion(WktTokenizer tokenizer, out AngularUnit? angularUnit) =>
        ReadWkt2Conversion(tokenizer, "CONVERSION", out angularUnit);

    private static Projection ReadWkt2DerivingConversion(WktTokenizer tokenizer, out AngularUnit? angularUnit) =>
        ReadWkt2Conversion(tokenizer, "DERIVINGCONVERSION", out angularUnit);

    private static Projection ReadWkt2Conversion(WktTokenizer tokenizer, string keyword, out AngularUnit? angularUnit)
    {
        if (tokenizer.GetStringValue() != keyword)
        {
            tokenizer.ReadToken(keyword);
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string conversionName = tokenizer.ReadDoubleQuotedWord();

        string methodName = string.Empty;
        string authority = string.Empty;
        long authorityCode = -1;
        angularUnit = null;
        var parameters = new List<ProjectionParameter>();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "METHOD":
                    methodName = ReadWkt2ProjectionMethod(tokenizer);
                    break;
                case "PARAMETER":
                    parameters.Add(ReadWkt2ProjectionParameter(tokenizer, out AngularUnit? parameterAngularUnit));
                    angularUnit = MergeAxisAngularUnit(angularUnit, parameterAngularUnit);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 {keyword} keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            ArgumentGuard.ThrowArgument($"WKT2 {keyword} is missing a METHOD block.");
        }

        return new Projection(methodName, parameters, conversionName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static string ReadWkt2ProjectionMethod(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "METHOD")
        {
            tokenizer.ReadToken("METHOD");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string methodName = tokenizer.ReadDoubleQuotedWord();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID" || ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 METHOD keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return methodName;
    }

    private static ProjectionParameter ReadWkt2ProjectionParameter(WktTokenizer tokenizer, out AngularUnit? angularUnit)
    {
        if (tokenizer.GetStringValue() != "PARAMETER")
        {
            tokenizer.ReadToken("PARAMETER");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string parameterName = NormalizeWkt2ProjectionParameterName(tokenizer.ReadDoubleQuotedWord());
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double value = tokenizer.GetNumericValue();
        angularUnit = null;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "ANGLEUNIT":
                    angularUnit = ReadWkt2AngularUnit(tokenizer);
                    break;
                case "ID":
                case "LENGTHUNIT":
                case "SCALEUNIT":
                    SkipKeywordNode(tokenizer);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 PARAMETER keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        return new ProjectionParameter(parameterName, value);
    }

    private static string NormalizeWkt2ProjectionParameterName(string parameterName) => ProjectionParameterNameNormalizer.Normalize(parameterName);

    private static VerticalCoordinateSystem ReadWkt2VerticalCoordinateSystem(WktTokenizer tokenizer)
    {
        const string rootKeyword = "VERTCRS";

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

        VerticalDatum? verticalDatum = null;
        LinearUnit? linearUnit = null;
        string? coordinateSystemType = null;
        int coordinateSystemDimension = 0;
        string authority = string.Empty;
        long authorityCode = -1;
        var axisInfo = new List<AxisInfo>();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "VDATUM":
                    verticalDatum = ReadWkt2VerticalDatum(tokenizer);
                    break;
                case "ENSEMBLE":
                    verticalDatum = ReadWkt2VerticalDatumEnsemble(tokenizer);
                    break;
                case "CS":
                    (coordinateSystemType, coordinateSystemDimension) = ReadWkt2CoordinateSystemDefinition(tokenizer);
                    break;
                case "AXIS":
                    axisInfo.Add(ReadWkt2Axis(tokenizer, out _, out LinearUnit? axisLinearUnit));
                    linearUnit = MergeAxisLinearUnit(linearUnit, axisLinearUnit);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "ID":
                    ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (verticalDatum is null)
        {
            ArgumentGuard.ThrowArgument("WKT2 vertical CRS is missing a VDATUM or ENSEMBLE block.");
        }

        if (string.IsNullOrWhiteSpace(coordinateSystemType))
        {
            ArgumentGuard.ThrowArgument("WKT2 vertical CRS is missing a CS block.");
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
            ArgumentGuard.ThrowArgument("WKT2 vertical CRS is missing a LENGTHUNIT block.");
        }

        if (axisInfo.Count != coordinateSystemDimension)
        {
            ArgumentGuard.ThrowArgument($"WKT2 vertical CRS declared dimension {coordinateSystemDimension}, but provided {axisInfo.Count} AXIS blocks.");
        }

        verticalDatum = ArgumentGuard.ThrowIfNull(verticalDatum, nameof(verticalDatum));
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

    private static VerticalDatum ReadWkt2VerticalDatum(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "VDATUM")
        {
            tokenizer.ReadToken("VDATUM");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID")
            {
                ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
            }
            else if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 VDATUM keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return new VerticalDatum(DatumType.VD_GeoidModelDerived, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static VerticalDatum ReadWkt2VerticalDatumEnsemble(WktTokenizer tokenizer)
    {
        DatumEnsemble ensemble = ReadWkt2DatumEnsemble(tokenizer, requireEllipsoid: false);
        return new VerticalDatum(DatumType.VD_GeoidModelDerived, ensemble.Name, ensemble.Authority, ensemble.AuthorityCode, string.Empty, string.Empty, string.Empty)
        {
            Ensemble = ensemble,
        };
    }

    private static CompoundCoordinateSystem ReadWkt2CompoundCoordinateSystem(WktTokenizer tokenizer)
    {
        const string rootKeyword = "COMPOUNDCRS";

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();

        CoordinateSystem? headCoordinateSystem = null;
        CoordinateSystem? tailCoordinateSystem = null;
        string authority = string.Empty;
        long authorityCode = -1;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (headCoordinateSystem is null)
            {
                headCoordinateSystem = ReadCoordinateSystem(null, tokenizer);
            }
            else if (tailCoordinateSystem is null)
            {
                tailCoordinateSystem = ReadCoordinateSystem(null, tokenizer);
            }
            else if (tokenizer.GetStringValue() == "ID")
            {
                ReadIdentifierWithUnknownCode(tokenizer, out authority, out authorityCode);
            }
            else if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
            }

            tokenizer.NextToken();
        }

        headCoordinateSystem = ArgumentGuard.ThrowIfNull(headCoordinateSystem, nameof(headCoordinateSystem));
        tailCoordinateSystem = ArgumentGuard.ThrowIfNull(tailCoordinateSystem, nameof(tailCoordinateSystem));
        return new CompoundCoordinateSystem(headCoordinateSystem, tailCoordinateSystem, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static BoundCoordinateSystem ReadWkt2BoundCoordinateSystem(WktTokenizer tokenizer)
    {
        const string rootKeyword = "BOUNDCRS";

        WktBracket bracket = tokenizer.ReadOpener();
        CoordinateSystem? sourceCoordinateSystem = null;
        CoordinateSystem? targetCoordinateSystem = null;
        BoundTransformation? transformation = null;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "SOURCECRS":
                    sourceCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(tokenizer);
                    EnsureSupportedWkt2BoundSourceCoordinateSystem(sourceCoordinateSystem);
                    break;
                case "TARGETCRS":
                    targetCoordinateSystem = ReadWkt2BoundCoordinateSystemComponent(tokenizer);
                    break;
                case "ABRIDGEDTRANSFORMATION":
                    transformation = ReadWkt2AbridgedTransformationDefinition(tokenizer);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 keyword '{tokenizer.GetStringValue()}' is not supported in {rootKeyword}.");
                    }

                    break;
            }

            tokenizer.NextToken();
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

    private static CoordinateSystem ReadWkt2BoundCoordinateSystemComponent(WktTokenizer tokenizer)
    {
        WktBracket bracket = tokenizer.ReadOpener();
        tokenizer.NextToken();
        CoordinateSystem coordinateSystem = tokenizer.GetStringValue() switch
        {
            "GEOGCRS" or "GEODCRS" or "GEODETICCRS" => ReadWkt2GeodeticCoordinateReferenceSystem(tokenizer),
            _ => ReadCoordinateSystem(null, tokenizer),
        };
        tokenizer.NextToken();
        tokenizer.CheckCloser(bracket);
        return coordinateSystem;
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

    private static BoundTransformation ReadWkt2AbridgedTransformationDefinition(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "ABRIDGEDTRANSFORMATION")
        {
            tokenizer.ReadToken("ABRIDGEDTRANSFORMATION");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        _ = tokenizer.ReadDoubleQuotedWord();

        string methodName = string.Empty;
        string? parameterFileName = null;
        var parameters = new Wgs84ConversionInfo();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "METHOD":
                    methodName = ReadWkt2ProjectionMethod(tokenizer);
                    break;
                case "PARAMETER":
                    ReadWkt2AbridgedTransformationParameter(tokenizer, parameters);
                    break;
                case "PARAMETERFILE":
                    parameterFileName = ReadWkt2AbridgedTransformationParameterFile(tokenizer);
                    break;
                case "ID":
                    SkipKeywordNode(tokenizer);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 ABRIDGEDTRANSFORMATION keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        if (string.IsNullOrWhiteSpace(methodName))
        {
            ArgumentGuard.ThrowArgument("WKT2 ABRIDGEDTRANSFORMATION is missing a METHOD block.");
        }

        return BoundCoordinateSystemSupport.CreateBoundTransformation(
            methodName,
            string.IsNullOrWhiteSpace(parameterFileName) ? parameters : null,
            parameterFileName);
    }

    private static void ReadWkt2AbridgedTransformationParameter(WktTokenizer tokenizer, Wgs84ConversionInfo parameters)
    {
        if (tokenizer.GetStringValue() != "PARAMETER")
        {
            tokenizer.ReadToken("PARAMETER");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string parameterName = NormalizeWkt2BoundTransformationParameterName(tokenizer.ReadDoubleQuotedWord());
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double value = tokenizer.GetNumericValue();

        AngularUnit? angularUnit = null;
        LinearUnit? linearUnit = null;
        double? scaleUnitFactor = null;

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            switch (tokenizer.GetStringValue())
            {
                case "ANGLEUNIT":
                    angularUnit = ReadWkt2AngularUnit(tokenizer);
                    break;
                case "LENGTHUNIT":
                    linearUnit = ReadWkt2LinearUnit(tokenizer);
                    break;
                case "SCALEUNIT":
                    scaleUnitFactor = ReadWkt2ScaleUnitFactor(tokenizer);
                    break;
                case "ID":
                    SkipKeywordNode(tokenizer);
                    break;
                default:
                    if (ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
                    {
                        SkipKeywordNode(tokenizer);
                    }
                    else
                    {
                        throw new NotSupportedException($"WKT2 ABRIDGEDTRANSFORMATION parameter keyword '{tokenizer.GetStringValue()}' is not supported.");
                    }

                    break;
            }

            tokenizer.NextToken();
        }

        ApplyWkt2BoundTransformationParameter(
            parameters,
            parameterName,
            NormalizeWkt2BoundTransformationParameterValue(parameterName, value, angularUnit, linearUnit, scaleUnitFactor));
    }

    private static string ReadWkt2AbridgedTransformationParameterFile(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "PARAMETERFILE")
        {
            tokenizer.ReadToken("PARAMETERFILE");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        _ = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        string parameterFileName = tokenizer.ReadDoubleQuotedWord();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID" || ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 PARAMETERFILE keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return parameterFileName;
    }

    private static double ReadWkt2ScaleUnitFactor(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "SCALEUNIT")
        {
            tokenizer.ReadToken("SCALEUNIT");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        _ = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double unitFactor = tokenizer.GetNumericValue();

        tokenizer.NextToken();
        while (true)
        {
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                continue;
            }

            if (tokenizer.GetStringValue() is "]" or ")")
            {
                tokenizer.CheckCloser(bracket);
                break;
            }

            if (tokenizer.GetStringValue() == "ID" || ShouldSkipWkt2MetadataNode(tokenizer.GetStringValue()))
            {
                SkipKeywordNode(tokenizer);
            }
            else
            {
                throw new NotSupportedException($"WKT2 SCALEUNIT keyword '{tokenizer.GetStringValue()}' is not supported.");
            }

            tokenizer.NextToken();
        }

        return unitFactor;
    }

    private static string NormalizeWkt2BoundTransformationParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        string normalized = parameterName
            .ToUpperInvariant()
            .Trim();
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "(", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ")", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "-", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "/", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, " ", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ".", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "__", "_");

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

    private static string NormalizeWkt(string wkt)
    {
        string normalized = wkt;
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "ELLIPSOID", "SPHEROID");
#if NET8_0_OR_GREATER
        normalized = Wkt2IdRegex().Replace(normalized, "AUTHORITY[");
#else
        normalized = Wkt2IdRegex.Replace(normalized, "AUTHORITY[");
#endif
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "GEODETICCRS[", "GEOGCS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "GEODCRS[", "GEOGCS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "BASEGEODCRS[", "GEOGCS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "BASEGEOGCRS[", "GEOGCS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "PROJECTEDCRS[", "PROJCS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "PROJCRS[", "PROJCS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "VERTCRS[", "VERT_CS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "COMPOUNDCRS[", "COMPD_CS[");
        normalized = StringCompatibility.ReplaceOrdinalIgnoreCase(normalized, "BOUNDCRS[", "BOUNDCRS[");
        return normalized;
    }

    private static IInfo ParseNormalizedWkt(string normalizedWkt)
    {
        var tokenizer = new WktTokenizer(normalizedWkt);
        tokenizer.NextToken();
        string objectName = tokenizer.GetStringValue();
        return objectName switch
        {
            "UNIT" => ReadUnit(tokenizer),
            "SPHEROID" => ReadEllipsoid(tokenizer),
            "DATUM" => ReadHorizontalDatum(tokenizer),
            "PRIMEM" => ReadPrimeMeridian(tokenizer),
            "VERT_CS" or "GEOGCS" or "PROJCS" or "COMPD_CS" or "GEOCCS" or "FITTED_CS" or "LOCAL_CS"
                => ReadCoordinateSystem(normalizedWkt, tokenizer),
            "BOUNDCRS" when HasCompleteWkt2BoundCoordinateSystemBlocks(normalizedWkt) => ReadWkt2BoundCoordinateSystem(tokenizer),
            "BOUNDCRS" => throw new NotSupportedException("BOUNDCRS coordinate system is not supported."),
            _ => ArgumentGuard.ThrowArgument<IInfo>($"'{objectName}' is not recognized."),
        };
    }

    private static void ReadAuthorityWithUnknownCode(WktTokenizer tokenizer, out string authority, out long authorityCode)
    {
        tokenizer.ReadAuthority(out authority, out authorityCode, out bool hasNumericAuthorityCode);
        if (!hasNumericAuthorityCode)
        {
            authorityCode = -1;
        }
    }

    /// <summary>
    /// Returns a IUnit given a piece of WKT.
    /// </summary>
    /// <param name="tokenizer">WktTokenizer that has the WKT.</param>
    /// <returns>An object that implements the IUnit interface.</returns>
    private static Unit ReadUnit(WktTokenizer tokenizer)
    {
        WktBracket bracket = tokenizer.ReadOpener();
        string unitName = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double unitsPerUnit = tokenizer.GetNumericValue();
        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();
        if (tokenizer.GetStringValue() == ",")
        {
            ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
            tokenizer.ReadCloser(bracket);
        }
        else
        {
            tokenizer.CheckCloser(bracket);
        }

        return new Unit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Returns a <see cref="LinearUnit"/> given a piece of WKT.
    /// </summary>
    /// <param name="tokenizer">WktTokenizer that has the WKT.</param>
    /// <returns>An object that implements the IUnit interface.</returns>
    private static LinearUnit ReadLinearUnit(WktTokenizer tokenizer)
    {
        WktBracket bracket = tokenizer.ReadOpener();

        string unitName = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double unitsPerUnit = tokenizer.GetNumericValue();
        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();
        if (tokenizer.GetStringValue() == ",")
        {
            ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
            tokenizer.ReadCloser(bracket);
        }
        else
        {
            tokenizer.CheckCloser(bracket);
        }

        return new LinearUnit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Returns a <see cref="AngularUnit"/> given a piece of WKT.
    /// </summary>
    /// <param name="tokenizer">WktTokenizer that has the WKT.</param>
    /// <returns>An object that implements the IUnit interface.</returns>
    private static AngularUnit ReadAngularUnit(WktTokenizer tokenizer)
    {
        WktBracket bracket = tokenizer.ReadOpener();

        string unitName = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double unitsPerUnit = tokenizer.GetNumericValue();
        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();
        if (tokenizer.GetStringValue() == ",")
        {
            ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
            tokenizer.ReadCloser(bracket);
        }
        else
        {
            tokenizer.CheckCloser(bracket);
        }

        return new AngularUnit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Returns a <see cref="AxisInfo"/> given a piece of WKT.
    /// </summary>
    /// <param name="tokenizer">WktTokenizer that has the WKT.</param>
    /// <returns>An AxisInfo object.</returns>
    private static AxisInfo ReadAxis(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "AXIS")
        {
            tokenizer.ReadToken("AXIS");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string axisName = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        string unitname = tokenizer.GetStringValue();
        tokenizer.ReadCloser(bracket);
        return unitname.ToUpperInvariant() switch
        {
            "DOWN" => new AxisInfo(axisName, AxisOrientationEnum.Down),
            "EAST" => new AxisInfo(axisName, AxisOrientationEnum.East),
            "NORTH" => new AxisInfo(axisName, AxisOrientationEnum.North),
            "OTHER" => new AxisInfo(axisName, AxisOrientationEnum.Other),
            "SOUTH" => new AxisInfo(axisName, AxisOrientationEnum.South),
            "UP" => new AxisInfo(axisName, AxisOrientationEnum.Up),
            "WEST" => new AxisInfo(axisName, AxisOrientationEnum.West),
            _ => ArgumentGuard.ThrowArgument<AxisInfo>($"Invalid axis name '{unitname}' in WKT"),
        };
    }

    private static CoordinateSystem ReadCoordinateSystem(string? coordinateSystem, WktTokenizer tokenizer)
    {
        string coordinateSystemText = coordinateSystem ?? tokenizer.GetStringValue();
        return tokenizer.GetStringValue() switch
        {
            "GEOGCRS" or "GEODCRS" or "GEODETICCRS" => ReadWkt2GeodeticCoordinateReferenceSystem(tokenizer),
            "PROJCRS" => ReadWkt2ProjectedCoordinateSystem(tokenizer),
            "DERIVEDPROJCRS" => ReadWkt2DerivedProjectedCoordinateSystem(tokenizer),
            "VERTCRS" => ReadWkt2VerticalCoordinateSystem(tokenizer),
            "COMPOUNDCRS" => ReadWkt2CompoundCoordinateSystem(tokenizer),
            "GEOGCS" => ReadGeographicCoordinateSystem(tokenizer),
            "PROJCS" => ReadProjectedCoordinateSystem(tokenizer),
            "FITTED_CS" => ReadFittedCoordinateSystem(tokenizer),
            "GEOCCS" => ReadGeocentricCoordinateSystem(tokenizer),
            "COMPD_CS" => ReadCompoundCoordinateSystem(tokenizer),
            "VERT_CS" => ReadVerticalCoordinateSystem(tokenizer),
            "BOUNDCRS" => ReadWkt2BoundCoordinateSystem(tokenizer),
            "LOCAL_CS" => throw new NotSupportedException($"{coordinateSystemText} coordinate system is not supported."),
            _ => throw new InvalidOperationException($"{coordinateSystemText} coordinate system is not recognized."),
        };
    }

    // Reads either 3, 6 or 7 parameter Bursa-Wolf values from TOWGS84 token
    private static Wgs84ConversionInfo ReadWGS84ConversionInfo(WktTokenizer tokenizer)
    {
        // TOWGS84[0,0,0,0,0,0,0]
        WktBracket bracket = tokenizer.ReadOpener();
        var info = new Wgs84ConversionInfo();
        tokenizer.NextToken();
        info.Dx = tokenizer.GetNumericValue();
        tokenizer.ReadToken(",");

        tokenizer.NextToken();
        info.Dy = tokenizer.GetNumericValue();
        tokenizer.ReadToken(",");

        tokenizer.NextToken();
        info.Dz = tokenizer.GetNumericValue();
        tokenizer.NextToken();
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            info.Ex = tokenizer.GetNumericValue();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            info.Ey = tokenizer.GetNumericValue();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            info.Ez = tokenizer.GetNumericValue();

            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                info.Ppm = tokenizer.GetNumericValue();
            }
        }

        if (tokenizer.GetStringValue() != "]")
        {
            tokenizer.ReadCloser(bracket);
        }

        return info;
    }

    private static Ellipsoid ReadEllipsoid(WktTokenizer tokenizer)
    {
        // SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]]
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double majorAxis = tokenizer.GetNumericValue();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double e = tokenizer.GetNumericValue();
        tokenizer.NextToken();
        string authority = string.Empty;
        long authorityCode = -1;

        // Read authority.
        if (tokenizer.GetStringValue() == ",")
        {
            ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
            tokenizer.ReadCloser(bracket);
        }
        else
        {
            tokenizer.CheckCloser(bracket);
        }

        var ellipsoid = new Ellipsoid(majorAxis, 0.0, e, true, LinearUnit.Metre, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        return ellipsoid;
    }

    private static Projection ReadProjection(WktTokenizer tokenizer)
    {
        if (tokenizer.GetStringValue() != "PROJECTION")
        {
            tokenizer.ReadToken("PROJECTION");
        }

        WktBracket bracket = tokenizer.ReadOpener();
        string projectionName = tokenizer.ReadDoubleQuotedWord();
        string authority = string.Empty;
        long authorityCode = -1L;

        tokenizer.NextToken(true);
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            else
            {
                tokenizer.CheckCloser(bracket);
            }
        }
        else
        {
            tokenizer.CheckCloser(bracket);
        }

        tokenizer.ReadToken(",");
        var paramList = new List<ProjectionParameter>();
        tokenizer.NextToken();
        while (tokenizer.GetStringValue() == "PARAMETER")
        {
            bracket = tokenizer.ReadOpener();
            string paramName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double paramValue = tokenizer.GetNumericValue();
            tokenizer.ReadCloser(bracket);
            paramList.Add(new ProjectionParameter(paramName, paramValue));

            // tokenizer.ReadToken(",");
            // tokenizer.NextToken();
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
            }
            else
            {
                break;
            }
        }

        var projection = new Projection(projectionName, paramList, projectionName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        return projection;
    }

    private static ProjectedCoordinateSystem ReadProjectedCoordinateSystem(WktTokenizer tokenizer)
    {
        // PROJCS[
        //     "OSGB 1936 / British National Grid",
        //     GEOGCS[
        //         "OSGB 1936",
        //         DATUM[...]
        //         PRIMEM[...]
        //         AXIS["Geodetic latitude","NORTH"]
        //         AXIS["Geodetic longitude","EAST"]
        //         AUTHORITY["EPSG","4277"]
        //     ],
        //     PROJECTION["Transverse Mercator"],
        //     PARAMETER["latitude_of_natural_origin",49],
        //     PARAMETER["longitude_of_natural_origin",-2],
        //     PARAMETER["scale_factor_at_natural_origin",0.999601272],
        //     PARAMETER["false_easting",400000],
        //     PARAMETER["false_northing",-100000],
        //     AXIS["Easting","EAST"],
        //     AXIS["Northing","NORTH"],
        //     AUTHORITY["EPSG","27700"]
        // ]
        _ = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("GEOGCS");
        GeographicCoordinateSystem geographicCS = ReadGeographicCoordinateSystem(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.NextToken();

        LinearUnit? linearUnit = null;

        if (tokenizer.GetStringValue().Equals("UNIT", StringComparison.OrdinalIgnoreCase))
        {
            linearUnit = ReadLinearUnit(tokenizer);
            tokenizer.ReadToken(",");
        }

        Projection projection = ReadProjection(tokenizer);
        LinearUnit unit = linearUnit ?? ReadLinearUnit(tokenizer);
        var axisInfo = new List<AxisInfo>(2);
        string authority = string.Empty;
        long authorityCode = -1;

        TokenType ct = tokenizer.NextToken();
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            while (tokenizer.GetStringValue() == "AXIS")
            {
                axisInfo.Add(ReadAxis(tokenizer));
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == ",")
                {
                    tokenizer.NextToken();
                }
            }

            while (ct != TokenType.Eol && ct != TokenType.Eof)
            {
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                    break;
                }
                else
                {
                    ct = tokenizer.NextToken();
                }
            }
        }

        // This is default axis values if not specified.
        if (axisInfo.Count == 0)
        {
            axisInfo.Add(new AxisInfo("X", AxisOrientationEnum.East));
            axisInfo.Add(new AxisInfo("Y", AxisOrientationEnum.North));
        }

        var projectedCS = new ProjectedCoordinateSystem(geographicCS.HorizontalDatum, geographicCS, unit, projection, axisInfo, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        return projectedCS;
    }

    private static VerticalCoordinateSystem ReadVerticalCoordinateSystem(WktTokenizer tokenizer)
    {
        // VERT_CS["<name>", <vert datum>, <linear unit>, {<axis>,} {,< authority >}]
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("VERT_DATUM");
        VerticalDatum verticalDatum = ReadVerticalDatum(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("UNIT");
        LinearUnit linearUnit = ReadLinearUnit(tokenizer);

        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();
        AxisInfo? info = null;
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == "AXIS")
            {
                info = ReadAxis(tokenizer);
                tokenizer.NextToken();
            }

            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
            }

            if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
        }

        // This is default axis values if not specified.
        info ??= new AxisInfo("Up", AxisOrientationEnum.Up);

        var verticalCs = new VerticalCoordinateSystem(linearUnit, verticalDatum, info, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        return verticalCs;
    }

    private static CompoundCoordinateSystem ReadCompoundCoordinateSystem(WktTokenizer tokenizer)
    {
        // <compd cs> = COMPD_CS["<name>", <head cs>, <tail cs> {,<authority>}]
        _ = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        CoordinateSystem headcs = ReadCoordinateSystem(null, tokenizer);

        TokenType ct = tokenizer.NextToken();
        while (ct != TokenType.Eol && ct != TokenType.Eof && CompoundCoordinateSystemDelimiters.Contains(tokenizer.GetStringValue()))
        {
            ct = tokenizer.NextToken();
        }

        CoordinateSystem tailcs = ReadCoordinateSystem(null, tokenizer);

        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();

        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
            }
        }

        return new CompoundCoordinateSystem(headcs, tailcs, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static GeocentricCoordinateSystem ReadGeocentricCoordinateSystem(WktTokenizer tokenizer)
    {
        // GEOCCS["<name>", <datum>, <prime meridian>, <linear unit> {,<axis>, <axis>, <axis>} {,<authority>}]
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("DATUM");
        HorizontalDatum horizontalDatum = ReadHorizontalDatum(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("PRIMEM");
        PrimeMeridian primeMeridian = ReadPrimeMeridian(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("UNIT");
        LinearUnit linearUnit = ReadLinearUnit(tokenizer);

        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();

        var info = new List<AxisInfo>(3);
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            while (tokenizer.GetStringValue() == "AXIS")
            {
                info.Add(ReadAxis(tokenizer));
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == ",")
                {
                    tokenizer.NextToken();
                }
            }

            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
            }

            if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
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
            horizontalDatum,
            linearUnit,
            primeMeridian,
            info,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeographicCoordinateSystem ReadGeographicCoordinateSystem(WktTokenizer tokenizer)
    {
        // GEOGCS["OSGB 1936",
        // DATUM["OSGB 1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG","6277"]]
        // PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]]
        // AXIS["Geodetic latitude","NORTH"]
        // AXIS["Geodetic longitude","EAST"]
        // AUTHORITY["EPSG","4277"]
        // ]
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("DATUM");
        HorizontalDatum horizontalDatum = ReadHorizontalDatum(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("PRIMEM");
        PrimeMeridian primeMeridian = ReadPrimeMeridian(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("UNIT");
        AngularUnit angularUnit = ReadAngularUnit(tokenizer);

        string authority = string.Empty;
        long authorityCode = -1;
        tokenizer.NextToken();
        var info = new List<AxisInfo>(2);
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            while (tokenizer.GetStringValue() == "AXIS")
            {
                info.Add(ReadAxis(tokenizer));
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == ",")
                {
                    tokenizer.NextToken();
                }
            }

            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
            }

            if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
        }

        // This is default axis values if not specified.
        if (info.Count == 0)
        {
            info.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
            info.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
        }

        var geographicCS = new GeographicCoordinateSystem(
            angularUnit,
            horizontalDatum,
            primeMeridian,
            info,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
        return geographicCS;
    }

    private static HorizontalDatum ReadHorizontalDatum(WktTokenizer tokenizer)
    {
        // DATUM["OSGB 1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG","6277"]]
        Wgs84ConversionInfo? wgsInfo = null;
        string authority = string.Empty;
        long authorityCode = -1;

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("SPHEROID");
        Ellipsoid ellipsoid = ReadEllipsoid(tokenizer);
        tokenizer.NextToken();
        while (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == "TOWGS84")
            {
                wgsInfo = ReadWGS84ConversionInfo(tokenizer);
                tokenizer.NextToken();
            }
            else if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
        }

        // make an assumption about the datum type.
        var horizontalDatum = new HorizontalDatum(ellipsoid, wgsInfo, DatumType.HD_Geocentric, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);

        return horizontalDatum;
    }

    private static VerticalDatum ReadVerticalDatum(WktTokenizer tokenizer)
    {
        // <vert datum> = VERT_DATUM["<name>", <datum type> {,<authority>}]
        string authority = string.Empty;
        long authorityCode = -1;

        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        var datumType = (DatumType)tokenizer.GetNumericValue();
        tokenizer.NextToken();
        if (tokenizer.GetStringValue() == ",")
        {
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == "AUTHORITY")
            {
                ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
        }

        var verticalDatum = new VerticalDatum(datumType, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);

        return verticalDatum;
    }

    private static PrimeMeridian ReadPrimeMeridian(WktTokenizer tokenizer)
    {
        // PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]]
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        double longitude = tokenizer.GetNumericValue();

        tokenizer.NextToken();
        string authority = string.Empty;
        long authorityCode = -1;
        if (tokenizer.GetStringValue() == ",")
        {
            ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);
            tokenizer.ReadCloser(bracket);
        }
        else
        {
            tokenizer.CheckCloser(bracket);
        }

        // make an assumption about the Angular units - degrees.
        var primeMeridian = new PrimeMeridian(longitude, AngularUnit.Degrees, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);

        return primeMeridian;
    }

    private static FittedCoordinateSystem ReadFittedCoordinateSystem(WktTokenizer tokenizer)
    {
        // FITTED_CS[
        //     "Local coordinate system MNAU (based on Gauss-Krueger)",
        //     PARAM_MT[
        //        "Affine",
        //        PARAMETER["num_row",3],
        //        PARAMETER["num_col",3],
        //        PARAMETER["elt_0_0", 0.883485346527455],
        //        PARAMETER["elt_0_1", -0.468458794848877],
        //        PARAMETER["elt_0_2", 3455869.17937689],
        //        PARAMETER["elt_1_0", 0.468458794848877],
        //        PARAMETER["elt_1_1", 0.883485346527455],
        //        PARAMETER["elt_1_2", 5478710.88035753],
        //        PARAMETER["elt_2_2", 1],
        //     ],
        //     PROJCS["DHDN / Gauss-Kruger zone 3", GEOGCS["DHDN", DATUM["Deutsches_Hauptdreiecksnetz", SPHEROID["Bessel 1841", 6377397.155, 299.1528128, AUTHORITY["EPSG", "7004"]], TOWGS84[612.4, 77, 440.2, -0.054, 0.057, -2.797, 0.525975255930096], AUTHORITY["EPSG", "6314"]], PRIMEM["Greenwich", 0, AUTHORITY["EPSG", "8901"]], UNIT["degree", 0.0174532925199433, AUTHORITY["EPSG", "9122"]], AUTHORITY["EPSG", "4314"]], UNIT["metre", 1, AUTHORITY["EPSG", "9001"]], PROJECTION["Transverse_Mercator"], PARAMETER["latitude_of_origin", 0], PARAMETER["central_meridian", 9], PARAMETER["scale_factor", 1], PARAMETER["false_easting", 3500000], PARAMETER["false_northing", 0], AUTHORITY["EPSG", "31467"]]
        //     AUTHORITY["CUSTOM","12345"]
        // ]
        WktBracket bracket = tokenizer.ReadOpener();
        string name = tokenizer.ReadDoubleQuotedWord();
        tokenizer.ReadToken(",");
        tokenizer.ReadToken("PARAM_MT");
        MathTransform toBaseTransform = MathTransformWktReader.ReadMathTransform(tokenizer);
        tokenizer.ReadToken(",");
        tokenizer.NextToken();
        CoordinateSystem baseCS = ReadCoordinateSystem(null, tokenizer);

        string authority = string.Empty;
        long authorityCode = -1;

        TokenType ct = tokenizer.NextToken();
        while (ct != TokenType.Eol && ct != TokenType.Eof)
        {
            switch (tokenizer.GetStringValue())
            {
                case ",":
                    break;
                case "]":
                case ")":
                    tokenizer.CheckCloser(bracket);

                    break;
                case "AUTHORITY":
                    ReadAuthorityWithUnknownCode(tokenizer, out authority, out authorityCode);

                    // tokenizer.ReadCloser(bracket);
                    break;
            }

            ct = tokenizer.NextToken();
        }

        var fittedCS = new FittedCoordinateSystem(baseCS, toBaseTransform, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        return fittedCS;
    }
}
