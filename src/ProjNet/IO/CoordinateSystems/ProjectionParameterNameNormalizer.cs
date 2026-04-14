// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.CoordinateSystems;

using System;

/// <summary>
/// Normalizes WKT2 and PROJJSON projection parameter names to the internal parameter aliases.
/// </summary>
internal static class ProjectionParameterNameNormalizer
{
    private const int StackallocThreshold = 128;

    /// <summary>
     /// Normalizes a projection parameter name.
     /// </summary>
     /// <param name="parameterName">The external parameter name to normalize.</param>
     /// <returns>The normalized parameter name.</returns>
    internal static string Normalize(string parameterName)
    {
        string normalized = NormalizeLookupToken(parameterName);

        return normalized switch
        {
            "LONGITUDE_OF_NATURAL_ORIGIN" => "central_meridian",
            "LONGITUDE_OF_FALSE_ORIGIN" => "central_meridian",
            "LONGITUDE_OF_PROJECTION_CENTRE" => "central_meridian",
            "LONGITUDE_OF_ORIGIN" => "central_meridian",
            "LATITUDE_OF_NATURAL_ORIGIN" => "latitude_of_origin",
            "LATITUDE_OF_FALSE_ORIGIN" => "latitude_of_origin",
            "LATITUDE_OF_PROJECTION_CENTRE" => "latitude_of_origin",
            "LATITUDE_OF_ORIGIN" => "latitude_of_origin",
            "LATITUDE_OF_1ST_STANDARD_PARALLEL" => "standard_parallel_1",
            "LATITUDE_OF_2ND_STANDARD_PARALLEL" => "standard_parallel_2",
            "LATITUDE_OF_PSEUDO_STANDARD_PARALLEL" => "standard_parallel_1",
            "EASTING_AT_FALSE_ORIGIN" => "false_easting",
            "EASTING_AT_PROJECTION_CENTRE" => "false_easting",
            "EASTING_AT_NATURAL_ORIGIN" => "false_easting",
            "NORTHING_AT_FALSE_ORIGIN" => "false_northing",
            "NORTHING_AT_PROJECTION_CENTRE" => "false_northing",
            "NORTHING_AT_NATURAL_ORIGIN" => "false_northing",
            "SCALE_FACTOR_AT_NATURAL_ORIGIN" => "scale_factor",
            "SCALE_FACTOR_AT_PROJECTION_CENTRE" => "scale_factor",
            "SCALE_FACTOR_ON_INITIAL_LINE" => "scale_factor",
            "AZIMUTH_OF_INITIAL_LINE" => "azimuth",
            "ANGLE_FROM_RECTIFIED_TO_SKEW_GRID" => "rectified_grid_angle",
            _ => normalized,
        };
    }

    /// <summary>
    /// Normalizes a projection-related parameter name into the uppercase lookup token used by the readers.
    /// </summary>
    /// <param name="parameterName">The external parameter name to normalize.</param>
    /// <returns>The normalized uppercase lookup token.</returns>
    internal static string NormalizeLookupToken(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> trimmed = TrimWhitespace(parameterName.AsSpan());
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        Span<char> buffer = trimmed.Length <= StackallocThreshold
            ? stackalloc char[StackallocThreshold]
            : new char[trimmed.Length];
        int normalizedLength = NormalizeLookupTokenCore(trimmed, buffer);
        return buffer.Slice(0, normalizedLength).ToString();
#else
        char[] buffer = new char[trimmed.Length];
        int normalizedLength = NormalizeLookupTokenCore(trimmed, buffer);
        return new string(buffer, 0, normalizedLength);
#endif
    }

    private static int NormalizeLookupTokenCore(ReadOnlySpan<char> parameterName, Span<char> destination)
    {
        int writeIndex = 0;
        int underscoreRunLength = 0;

        for (int i = 0; i < parameterName.Length; i++)
        {
            char current = parameterName[i];
            if (current is '(' or ')')
            {
                continue;
            }

            char normalizedCharacter = current is '-' or '/' or ' ' or '.'
                ? '_'
                : char.ToUpperInvariant(current);

            if (normalizedCharacter == '_')
            {
                underscoreRunLength++;
                continue;
            }

            FlushUnderscores(destination, ref writeIndex, ref underscoreRunLength);
            destination[writeIndex++] = normalizedCharacter;
        }

        FlushUnderscores(destination, ref writeIndex, ref underscoreRunLength);
        return writeIndex;
    }

    private static ReadOnlySpan<char> TrimWhitespace(ReadOnlySpan<char> value)
    {
        int start = 0;
        while (start < value.Length && char.IsWhiteSpace(value[start]))
        {
            start++;
        }

        int end = value.Length - 1;
        while (end >= start && char.IsWhiteSpace(value[end]))
        {
            end--;
        }

        return end < start ? [] : value.Slice(start, (end - start) + 1);
    }

    private static void FlushUnderscores(Span<char> destination, ref int writeIndex, ref int underscoreRunLength)
    {
        int underscoresToWrite = (underscoreRunLength + 1) / 2;
        for (int i = 0; i < underscoresToWrite; i++)
        {
            destination[writeIndex++] = '_';
        }

        underscoreRunLength = 0;
    }
}
