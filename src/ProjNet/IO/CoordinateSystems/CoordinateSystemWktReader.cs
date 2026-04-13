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
using ProjNet.IO.Wkt;

/// <summary>
/// Creates an object based on the supplied Well Known Text (WKT).
/// </summary>
public static partial class CoordinateSystemWktReader
{
    private const double RadiansPerArcSecond = 4.84813681109535993589914102357e-6d;
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
        WktKeywordNode rootNode;
        try
        {
            rootNode = WktKeywordNode.ParseTree(tokenizer);
        }
        catch (ArgumentException)
        {
            info = null;
            return false;
        }

        info = rootNode.Keyword switch
        {
            "GEOGCRS" or "GEODCRS" or "GEODETICCRS" when rootNode.FindChild("CS") is not null
                => ReadWkt2GeodeticCoordinateReferenceSystem(rootNode),
            "PROJCRS" when rootNode.FindChild("CONVERSION") is not null && rootNode.FindChild("CS") is not null
                => ReadWkt2ProjectedCoordinateSystem(rootNode),
            "DERIVEDPROJCRS" when rootNode.FindChild("DERIVINGCONVERSION") is not null && rootNode.FindChild("CS") is not null
                => ReadWkt2DerivedProjectedCoordinateSystem(rootNode),
            "VERTCRS" when rootNode.FindChild("VDATUM", "ENSEMBLE") is not null && rootNode.FindChild("CS") is not null
                => ReadWkt2VerticalCoordinateSystem(rootNode),
            "ENGCRS" or "ENGINEERINGCRS" => ReadWkt2EngineeringCoordinateSystem(rootNode),
            "TIMECRS" => ReadWkt2TemporalCoordinateSystem(rootNode),
            "PARAMETRICCRS" => ReadWkt2ParametricCoordinateSystem(rootNode),
            "COORDINATEOPERATION" => ReadWkt2CoordinateOperation(rootNode),
            "CONCATENATEDOPERATION" => ReadWkt2ConcatenatedOperation(rootNode),
            "COMPOUNDCRS" => ReadWkt2CompoundCoordinateSystem(rootNode),
            "BOUNDCRS" when rootNode.FindChild("SOURCECRS") is not null
                && rootNode.FindChild("TARGETCRS") is not null
                && rootNode.FindChild("ABRIDGEDTRANSFORMATION") is not null
                => ReadWkt2BoundCoordinateSystem(rootNode),
            _ => null,
        };

        return info is not null;
    }

    private static T ParseNodeWithTokenizer<T>(WktKeywordNode rootNode, Func<WktTokenizer, T> reader)
    {
        ArgumentGuard.ThrowIfNull(rootNode, nameof(rootNode));
        ArgumentGuard.ThrowIfNull(reader, nameof(reader));

        var tokenizer = new WktTokenizer(rootNode.ToString());
        tokenizer.NextToken();
        return reader(tokenizer);
    }

    private static string GetWktNodeText(WktNode node)
    {
        return node switch
        {
            WktQuotedString quotedString => quotedString.Value,
            WktIdentifier identifier => identifier.Name,
            WktInteger integer => integer.Value.ToString(CultureInfo.InvariantCulture),
            WktNumber number => number.Value.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"Expected a leaf WKT node value but found '{node.GetType().Name}'.", nameof(node)),
        };
    }

    private static CoordinateSystem ReadCoordinateSystemNode(WktKeywordNode node)
    {
        ArgumentGuard.ThrowIfNull(node, nameof(node));

        return node.Keyword switch
        {
            "GEOGCRS" or "GEODCRS" or "GEODETICCRS" => ReadWkt2GeodeticCoordinateReferenceSystem(node),
            "PROJCRS" => ReadWkt2ProjectedCoordinateSystem(node),
            "DERIVEDPROJCRS" => ReadWkt2DerivedProjectedCoordinateSystem(node),
            "VERTCRS" => ReadWkt2VerticalCoordinateSystem(node),
            "ENGCRS" or "ENGINEERINGCRS" => ReadWkt2EngineeringCoordinateSystem(node),
            "TIMECRS" => ReadWkt2TemporalCoordinateSystem(node),
            "PARAMETRICCRS" => ReadWkt2ParametricCoordinateSystem(node),
            "COMPOUNDCRS" => ReadWkt2CompoundCoordinateSystem(node),
            "BOUNDCRS" => ReadWkt2BoundCoordinateSystem(node),
            "GEOGCS" => ReadGeographicCoordinateSystem(node),
            "PROJCS" => ReadProjectedCoordinateSystem(node),
            "FITTED_CS" => ReadFittedCoordinateSystem(node),
            "GEOCCS" => ReadGeocentricCoordinateSystem(node),
            "COMPD_CS" => ReadCompoundCoordinateSystem(node),
            "VERT_CS" => ReadVerticalCoordinateSystem(node),
            _ => ParseNodeWithTokenizer(node, ReadCoordinateSystem),
        };
    }

    private static CoordinateSystem ReadWkt2CoordinateSystemNode(WktKeywordNode node)
    {
        return ReadCoordinateSystemNode(node);
    }

    private static bool IsCoordinateSystemKeyword(string keyword)
    {
        return keyword is "GEOGCRS"
            or "GEODCRS"
            or "GEODETICCRS"
            or "PROJCRS"
            or "DERIVEDPROJCRS"
            or "VERTCRS"
            or "ENGCRS"
            or "ENGINEERINGCRS"
            or "TIMECRS"
            or "PARAMETRICCRS"
            or "COMPOUNDCRS"
            or "BOUNDCRS"
            or "GEOGCS"
            or "PROJCS"
            or "FITTED_CS"
            or "GEOCCS"
            or "COMPD_CS"
            or "VERT_CS"
            or "LOCAL_CS";
    }

    // The WKT1 fallback still accepts historical hybrid inputs such as PROJECTEDCRS plus
    // PROJECTION/PARAMETER siblings and spaced ID[...] metadata that do not satisfy the native WKT2 path.
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
        var rootNode = WktKeywordNode.ParseSubtree(tokenizer);
        return rootNode.Keyword switch
        {
            "UNIT" => ParseNodeWithTokenizer(rootNode, ReadUnit),
            "SPHEROID" => ParseNodeWithTokenizer(rootNode, ReadEllipsoid),
            "DATUM" => ParseNodeWithTokenizer(rootNode, ReadHorizontalDatum),
            "PRIMEM" => ParseNodeWithTokenizer(rootNode, ReadPrimeMeridian),
            "VERT_CS" or "GEOGCS" or "PROJCS" or "COMPD_CS" or "GEOCCS" or "FITTED_CS" or "LOCAL_CS"
                => ParseNodeWithTokenizer(rootNode, ReadCoordinateSystem),
            "BOUNDCRS" => throw new NotSupportedException("BOUNDCRS coordinate system is not supported."),
            _ => ArgumentGuard.ThrowArgument<IInfo>($"'{rootNode.Keyword}' is not recognized."),
        };
    }
}
