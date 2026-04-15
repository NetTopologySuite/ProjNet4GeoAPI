// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
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

    /// <summary>
    /// Reads and parses a WKT-formatted projection string.
    /// </summary>
    /// <param name="wkt">String containing WKT.</param>
    /// <returns>Object representation of the WKT.</returns>
    /// <exception cref="ArgumentException">If a token is not recognized.</exception>
    public static IInfo Parse(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            ArgumentGuard.ThrowArgumentNull(nameof(wkt));
        }

        return ParseCore(wkt.AsSpan(), wkt);
    }

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

        return ParseCore(wkt, sourceText: null);
    }

    [DoesNotReturn]
    private static void ThrowWktParseException(string message)
    {
        throw new WktParseException(message);
    }

    [DoesNotReturn]
    private static T ThrowWktParseException<T>(string message)
    {
        throw new WktParseException(message);
    }

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

    private static IInfo ParseCore(ReadOnlySpan<char> wkt, string? sourceText)
    {
        if (ShouldBypassNativeWkt2(wkt))
        {
            return ParseNormalizedWkt(sourceText ?? wkt.ToString());
        }

        string wktText = sourceText ?? wkt.ToString();
        if (TryParseNativeWkt2(wktText, out IInfo? nativeWkt2Info))
        {
            return ArgumentGuard.ThrowIfNull(nativeWkt2Info, nameof(nativeWkt2Info));
        }

        string normalizedWkt = NormalizeWkt(wktText);
        return ParseNormalizedWkt(normalizedWkt);
    }

    private static bool ShouldBypassNativeWkt2(ReadOnlySpan<char> wkt)
    {
        return TryGetRootKeyword(wkt, out ReadOnlySpan<char> keyword)
            && IsWkt1OnlyRootKeyword(keyword);
    }

    private static bool TryGetRootKeyword(ReadOnlySpan<char> wkt, out ReadOnlySpan<char> keyword)
    {
        int index = 0;
        while (index < wkt.Length && char.IsWhiteSpace(wkt[index]))
        {
            index++;
        }

        int start = index;
        while (index < wkt.Length)
        {
            char current = wkt[index];
            if ((current >= 'A' && current <= 'Z') ||
                (current >= 'a' && current <= 'z') ||
                (current >= '0' && current <= '9') ||
                current == '_')
            {
                index++;
                continue;
            }

            break;
        }

        if (index == start)
        {
            keyword = default;
            return false;
        }

        keyword = wkt.Slice(start, index - start);
        while (index < wkt.Length && char.IsWhiteSpace(wkt[index]))
        {
            index++;
        }

        return index < wkt.Length && (wkt[index] == '[' || wkt[index] == '(');
    }

    private static bool IsWkt1OnlyRootKeyword(ReadOnlySpan<char> keyword)
    {
        return KeywordEqualsOrdinalIgnoreCase(keyword, "UNIT")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "SPHEROID")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "DATUM")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "PRIMEM")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "GEOGCS")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "PROJCS")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "GEOCCS")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "COMPD_CS")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "VERT_CS")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "FITTED_CS")
            || KeywordEqualsOrdinalIgnoreCase(keyword, "LOCAL_CS");
    }

    private static bool KeywordEqualsOrdinalIgnoreCase(ReadOnlySpan<char> keyword, string expected)
    {
        if (keyword.Length != expected.Length)
        {
            return false;
        }

        for (int i = 0; i < keyword.Length; i++)
        {
            char current = keyword[i];
            if (current >= 'a' && current <= 'z')
            {
                current = (char)(current - ('a' - 'A'));
            }

            if (current != expected[i])
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
        catch (WktParseException)
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

    private static CoordinateSystem ReadCoordinateSystemNode(WktKeywordNode node)
    {
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
            _ => ThrowWktParseException<CoordinateSystem>($"'{node.Keyword}' is not recognized."),
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
        StringBuilder? builder = null;
        int copyStart = 0;
        int index = 0;
        while (index < wkt.Length)
        {
            if (!IsWktKeywordCharacter(wkt[index]))
            {
                index++;
                continue;
            }

            int keywordStart = index;
            while (index < wkt.Length && IsWktKeywordCharacter(wkt[index]))
            {
                index++;
            }

            ReadOnlySpan<char> keyword = wkt.AsSpan(keywordStart, index - keywordStart);
            int openerIndex = index;
            while (openerIndex < wkt.Length && char.IsWhiteSpace(wkt[openerIndex]))
            {
                openerIndex++;
            }

            if (openerIndex >= wkt.Length || (wkt[openerIndex] != '[' && wkt[openerIndex] != '('))
            {
                continue;
            }

            if (!TryGetNormalizedWktKeyword(keyword, wkt.AsSpan(openerIndex), openerIndex != index, out string? normalizedKeyword))
            {
                continue;
            }

            builder ??= new StringBuilder(wkt.Length);
            builder.Append(wkt, copyStart, keywordStart - copyStart);
            builder.Append(normalizedKeyword);
            builder.Append(wkt[openerIndex]);
            copyStart = openerIndex + 1;
            index = copyStart;
        }

        if (builder is null)
        {
            return wkt;
        }

        builder.Append(wkt, copyStart, wkt.Length - copyStart);
        return builder.ToString();
    }

    private static bool TryGetNormalizedWktKeyword(ReadOnlySpan<char> keyword, ReadOnlySpan<char> openerAndRemainder, bool hadWhitespaceBeforeOpener, out string? normalizedKeyword)
    {
        normalizedKeyword = null;
        bool requiresQuotedFirstValue = false;

        if (KeywordEqualsOrdinalIgnoreCase(keyword, "ELLIPSOID"))
        {
            normalizedKeyword = "SPHEROID";
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "ID"))
        {
            normalizedKeyword = "AUTHORITY";
            requiresQuotedFirstValue = true;
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "GEODETICCRS") || KeywordEqualsOrdinalIgnoreCase(keyword, "GEODCRS"))
        {
            normalizedKeyword = "GEOGCS";
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "BASEGEODCRS") || KeywordEqualsOrdinalIgnoreCase(keyword, "BASEGEOGCRS"))
        {
            normalizedKeyword = "GEOGCS";
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "PROJECTEDCRS") || KeywordEqualsOrdinalIgnoreCase(keyword, "PROJCRS"))
        {
            normalizedKeyword = "PROJCS";
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "VERTCRS"))
        {
            normalizedKeyword = "VERT_CS";
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "COMPOUNDCRS"))
        {
            normalizedKeyword = "COMPD_CS";
        }
        else if (KeywordEqualsOrdinalIgnoreCase(keyword, "BOUNDCRS"))
        {
            normalizedKeyword = "BOUNDCRS";
        }
        else
        {
            return false;
        }

        if (requiresQuotedFirstValue)
        {
            int valueIndex = 1;
            while (valueIndex < openerAndRemainder.Length && char.IsWhiteSpace(openerAndRemainder[valueIndex]))
            {
                valueIndex++;
            }

            if (valueIndex >= openerAndRemainder.Length || openerAndRemainder[valueIndex] != '"')
            {
                normalizedKeyword = null;
                return false;
            }
        }

        if (!hadWhitespaceBeforeOpener && KeywordEqualsOrdinal(keyword, normalizedKeyword))
        {
            normalizedKeyword = null;
            return false;
        }

        return true;
    }

    private static bool IsWktKeywordCharacter(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z')
            || (value >= '0' && value <= '9')
            || value == '_';
    }

    private static bool KeywordEqualsOrdinal(ReadOnlySpan<char> keyword, string expected)
    {
        if (keyword.Length != expected.Length)
        {
            return false;
        }

        for (int i = 0; i < keyword.Length; i++)
        {
            if (keyword[i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

    private static IInfo ParseNormalizedWkt(string normalizedWkt)
    {
        var tokenizer = new WktTokenizer(normalizedWkt);
        tokenizer.NextToken();
        var rootNode = WktKeywordNode.ParseSubtree(tokenizer);
        return rootNode.Keyword switch
        {
            "UNIT" => ReadUnit(rootNode),
            "SPHEROID" => ReadEllipsoid(rootNode),
            "DATUM" => ReadHorizontalDatum(rootNode),
            "PRIMEM" => ReadPrimeMeridian(rootNode),
            "VERT_CS" or "GEOGCS" or "PROJCS" or "COMPD_CS" or "GEOCCS" or "FITTED_CS" or "LOCAL_CS"
                => ReadCoordinateSystemNode(rootNode),
            "BOUNDCRS" => throw new NotSupportedException("BOUNDCRS coordinate system is not supported."),
            _ => ThrowWktParseException<IInfo>($"'{rootNode.Keyword}' is not recognized."),
        };
    }
}
