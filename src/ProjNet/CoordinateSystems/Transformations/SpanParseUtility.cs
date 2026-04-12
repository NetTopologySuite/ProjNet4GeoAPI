// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Globalization;

/// <summary>
/// Represents the outcome of parsing a comma-separated numeric token.
/// </summary>
internal enum CsvParseStatus
{
    /// <summary>
    /// The token was parsed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The token contained more numeric values than the caller can accept.
    /// </summary>
    TooManyValues,

    /// <summary>
    /// The token contained a non-finite or invalid numeric value.
    /// </summary>
    InvalidValue,
}

/// <summary>
/// Provides shared span-based parsing helpers for runtime transformation arguments.
/// </summary>
internal static class SpanParseUtility
{
    /// <summary>
    /// Parses a comma-separated list of doubles into the provided destination span.
    /// Empty segments are ignored and surrounding whitespace is trimmed.
    /// </summary>
    /// <param name="token">Token to parse.</param>
    /// <param name="destination">Destination span receiving parsed values.</param>
    /// <param name="parsedCount">Receives the number of parsed values.</param>
    /// <returns>The parse status.</returns>
    internal static CsvParseStatus TryParseCsvValues(ReadOnlySpan<char> token, Span<double> destination, out int parsedCount)
    {
        parsedCount = 0;
        int segmentStart = 0;
        for (int i = 0; i <= token.Length; i++)
        {
            bool atDelimiter = i < token.Length && token[i] == ',';
            if (i != token.Length && !atDelimiter)
            {
                continue;
            }

            ReadOnlySpan<char> segment = TrimWhitespace(token[segmentStart..i]);
            if (!segment.IsEmpty)
            {
                if (parsedCount >= destination.Length)
                {
                    return CsvParseStatus.TooManyValues;
                }

                if (!TryParseFiniteDouble(segment, out destination[parsedCount]))
                {
                    return CsvParseStatus.InvalidValue;
                }

                parsedCount++;
            }

            segmentStart = i + 1;
        }

        return CsvParseStatus.Success;
    }

    /// <summary>
    /// Parses a string token as a finite double using invariant culture.
    /// </summary>
    /// <param name="token">Token to parse.</param>
    /// <param name="value">Receives the parsed value.</param>
    /// <returns><see langword="true"/> when a finite number was parsed; otherwise <see langword="false"/>.</returns>
    internal static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && TryParseFiniteDouble(token.AsSpan(), out value);
    }

    private static bool TryParseFiniteDouble(ReadOnlySpan<char> token, out double value)
    {
#if NETSTANDARD2_0
        bool parsed = double.TryParse(token.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
#else
        bool parsed = double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
#endif

        return parsed && !double.IsNaN(value) && !double.IsInfinity(value);
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
}
