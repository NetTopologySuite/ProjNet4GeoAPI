// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/// <summary>
/// Parses PROJ GIE fixture text into strongly typed test cases.
/// </summary>
internal static class GieParser
{
    private static readonly char[] WhiteSpaceSeparators = [' ', '\t'];

    /// <summary>
    /// Parses a GIE fixture file from disk.
    /// </summary>
    /// <param name="path">Path to the fixture file.</param>
    /// <param name="options">Optional parser behavior options.</param>
    /// <returns>Parsed GIE cases.</returns>
    public static IReadOnlyList<GieCase> ParseFile(string path, GieParserOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Parse(File.ReadAllText(path), options);
    }

    /// <summary>
    /// Parses GIE fixture content provided as raw text.
    /// </summary>
    /// <param name="content">Fixture content text.</param>
    /// <param name="options">Optional parser behavior options.</param>
    /// <returns>Parsed GIE cases.</returns>
    public static IReadOnlyList<GieCase> Parse(string content, GieParserOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        options ??= new GieParserOptions();

        var parsedCases = new List<GieCase>();
        string? currentOperation = null;
        double currentToleranceValue = 0d;
        string currentToleranceUnit = "m";
        GieDirection currentDirection = GieDirection.Forward;
        int? currentRoundtrip = null;
        double[]? pendingAccept = null;

        foreach (LogicalLine logicalLine in EnumerateLogicalLines(content))
        {
            string stripped = logicalLine.Content;
            int lineNumber = logicalLine.LineNumber;
            if (stripped.Length == 0 || IsTagLine(stripped) || IsSeparatorLine(stripped))
            {
                continue;
            }

            if (options.AllowOperationContinuation && currentOperation is not null && stripped.StartsWith('+'))
            {
                currentOperation += $" {stripped}";
                continue;
            }

            SplitDirective(stripped, out string directive, out string payload);

            if (directive.Equals("operation", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePayload(payload, "operation", lineNumber);
                currentOperation = payload;
                currentToleranceValue = 0d;
                currentToleranceUnit = "m";
                currentDirection = GieDirection.Forward;
                currentRoundtrip = null;
                pendingAccept = null;
            }
            else if (directive.Equals("tolerance", StringComparison.OrdinalIgnoreCase))
            {
                ParseTolerance(payload, lineNumber, out currentToleranceValue, out currentToleranceUnit);
            }
            else if (directive.Equals("direction", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePayload(payload, "direction", lineNumber);
                currentDirection = ParseDirection(payload, lineNumber);
            }
            else if (directive.Equals("roundtrip", StringComparison.OrdinalIgnoreCase))
            {
                currentRoundtrip = ParseRoundtrip(payload, lineNumber);
            }
            else if (directive.Equals("accept", StringComparison.OrdinalIgnoreCase))
            {
                EnsureOperationDeclared(currentOperation, lineNumber, "accept");
                pendingAccept = ParseVector(payload, lineNumber, "accept");
            }
            else if (directive.Equals("expect", StringComparison.OrdinalIgnoreCase))
            {
                EnsureOperationDeclared(currentOperation, lineNumber, "expect");

                if (IsFailureExpectation(payload))
                {
                    parsedCases.Add(
                        new GieCase
                        {
                            LineNumber = lineNumber,
                            Operation = currentOperation ?? string.Empty,
                            ToleranceValue = currentToleranceValue,
                            ToleranceUnit = currentToleranceUnit,
                            Direction = currentDirection,
                            Accept = pendingAccept ?? [],
                            Expect = [],
                            ExpectsFailure = true,
                            ExpectedErrorCode = ParseExpectedErrorCode(payload),
                            RoundtripCount = currentRoundtrip,
                        });
                    pendingAccept = null;
                    continue;
                }

                if (pendingAccept is null)
                {
                    if (options.IgnoreUnknownDirectives)
                    {
                        continue;
                    }

                    throw new FormatException($"Found 'expect' without preceding 'accept' at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
                }

                double[] expected = ParseVector(payload, lineNumber, "expect");
                double[] accepted = pendingAccept;
                parsedCases.Add(
                    new GieCase
                    {
                        LineNumber = lineNumber,
                        Operation = currentOperation ?? string.Empty,
                        ToleranceValue = currentToleranceValue,
                        ToleranceUnit = currentToleranceUnit,
                        Direction = currentDirection,
                        Accept = accepted,
                        Expect = expected,
                        RoundtripCount = currentRoundtrip,
                    });
                pendingAccept = null;
            }
            else if (!options.IgnoreUnknownDirectives)
            {
                throw new FormatException($"Unsupported GIE directive '{directive}' at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
            }
        }

        if (pendingAccept is not null)
        {
            return options.IgnoreUnknownDirectives
                ? (IReadOnlyList<GieCase>)parsedCases
                : throw new FormatException("Dangling 'accept' without matching 'expect' at end of input.");
        }

        return parsedCases;
    }

    private static IEnumerable<LogicalLine> EnumerateLogicalLines(string content)
    {
        string[] lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        string? current = null;
        int currentStartLine = 1;

        for (int i = 0; i < lines.Length; i++)
        {
            string stripped = StripInlineComment(lines[i]).Trim();
            if (stripped.Length == 0 && current is null)
            {
                continue;
            }

            bool hasContinuation = stripped.EndsWith('\\');
            if (hasContinuation)
            {
                stripped = stripped[..^1].TrimEnd();
            }

            if (current is null)
            {
                current = stripped;
                currentStartLine = i + 1;
            }
            else
            {
                current += $" {stripped}";
            }

            if (hasContinuation)
            {
                continue;
            }

            yield return new LogicalLine(current, currentStartLine);
            current = null;
        }

        if (current is not null)
        {
            yield return new LogicalLine(current, currentStartLine);
        }
    }

    private static bool IsTagLine(string line)
    {
        return line.StartsWith('<') && line.EndsWith('>');
    }

    private static bool IsSeparatorLine(string line)
    {
        if (line.Length < 3)
        {
            return false;
        }

        char first = line[0];
        if (first != '-' && first != '=')
        {
            return false;
        }

        for (int i = 1; i < line.Length; i++)
        {
            if (line[i] != first)
            {
                return false;
            }
        }

        return true;
    }

    private static string StripInlineComment(string line)
    {
        if (line is null)
        {
            return string.Empty;
        }

        int commentIndex = line.IndexOf('#', StringComparison.Ordinal);
        return commentIndex < 0 ? line : line[..commentIndex];
    }

    private static void SplitDirective(string line, out string directive, out string payload)
    {
        int splitIndex = FindFirstWhiteSpace(line);
        if (splitIndex < 0)
        {
            directive = line;
            payload = string.Empty;
            return;
        }

        directive = line[..splitIndex];
        payload = line[(splitIndex + 1)..].Trim();
    }

    private static int ParseRoundtrip(string payload, int lineNumber)
    {
        EnsurePayload(payload, "roundtrip", lineNumber);
        string[] tokens = payload.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        return (int)ParseNumber(tokens[0], lineNumber, "roundtrip");
    }

    private static void ParseTolerance(string payload, int lineNumber, out double value, out string unit)
    {
        EnsurePayload(payload, "tolerance", lineNumber);
        string[] tokens = payload.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new FormatException($"Missing tolerance value at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
        }

        string firstToken = tokens[0];
        unit = tokens.Length > 1 ? tokens[1] : "m";

        if (TryParseCompactTolerance(firstToken, out double compactValue, out string compactUnit))
        {
            value = compactValue;
            if (tokens.Length == 1 && !string.IsNullOrWhiteSpace(compactUnit))
            {
                unit = compactUnit;
            }

            return;
        }

        value = ParseNumber(firstToken, lineNumber, "tolerance");
    }

    private static GieDirection ParseDirection(string payload, int lineNumber)
    {
        string normalized = payload.Trim();
        if (normalized.Equals("forward", StringComparison.OrdinalIgnoreCase))
        {
            return GieDirection.Forward;
        }

        return normalized.Equals("inverse", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("reverse", StringComparison.OrdinalIgnoreCase)
            ? GieDirection.Inverse
            : throw new FormatException($"Unsupported direction '{payload}' at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static bool IsFailureExpectation(string payload)
    {
        return payload.StartsWith("failure", StringComparison.OrdinalIgnoreCase);
    }

    private static string ParseExpectedErrorCode(string payload)
    {
        string[] tokens = payload.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            if (tokens[i].Equals("errno", StringComparison.OrdinalIgnoreCase))
            {
                return tokens[i + 1];
            }
        }

        return string.Empty;
    }

    private static double[] ParseVector(string payload, int lineNumber, string directiveName)
    {
        EnsurePayload(payload, directiveName, lineNumber);
        string[] tokens = payload.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            throw new FormatException(
                $"Directive '{directiveName}' requires at least two numeric values at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
        }

        double[] values = new double[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            values[i] = ParseNumber(tokens[i], lineNumber, directiveName);
        }

        return values;
    }

    private static double ParseNumber(string token, int lineNumber, string directiveName)
    {
        string normalizedToken = token.Replace("_", string.Empty, StringComparison.Ordinal);
        if (double.TryParse(normalizedToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }

        return TryParseDmsCoordinate(normalizedToken, out value)
            ? value
            : throw new FormatException(
            $"Failed to parse numeric value '{token}' in directive '{directiveName}' at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static void EnsureOperationDeclared(string? operation, int lineNumber, string directive)
    {
        if (operation is null)
        {
            throw new FormatException(
                $"Found '{directive}' before any 'operation' declaration at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private static void EnsurePayload(string payload, string directiveName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new FormatException($"Directive '{directiveName}' is missing payload at line {lineNumber.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private static int FindFirstWhiteSpace(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == ' ' || value[i] == '\t')
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryParseCompactTolerance(string token, out double value, out string unit)
    {
        value = 0d;
        unit = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        int index = 0;
        while (index < token.Length && (char.IsDigit(token[index]) || token[index] == '.' || token[index] == '-' || token[index] == '+' || token[index] == 'e' || token[index] == 'E'))
        {
            index++;
        }

        if (index <= 0 || index >= token.Length)
        {
            return false;
        }

        string numberPart = token[..index];
        string unitPart = token[index..];
        if (!double.TryParse(numberPart, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        unit = unitPart;
        return true;
    }

    private static bool TryParseDmsCoordinate(string token, out double value)
    {
        value = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string text = token.Trim();
        int sign = 1;

        char last = text[text.Length - 1];
        if (last == 'W' || last == 'w' || last == 'S' || last == 's')
        {
            sign = -1;
            text = text[..^1];
        }
        else if (last == 'E' || last == 'e' || last == 'N' || last == 'n')
        {
            text = text[..^1];
        }

        if (text.StartsWith('-'))
        {
            sign *= -1;
            text = text[1..];
        }
        else if (text.StartsWith('+'))
        {
            text = text[1..];
        }

        int dIndex = text.IndexOf('d', StringComparison.Ordinal);
        if (dIndex < 0)
        {
            dIndex = text.IndexOf('D', StringComparison.Ordinal);
        }

        int mIndex = text.IndexOf('\'', StringComparison.Ordinal);
        if (dIndex <= 0 || mIndex <= dIndex)
        {
            return false;
        }

        string degreesToken = text[..dIndex];
        string minutesToken = text.Substring(dIndex + 1, mIndex - dIndex - 1);
        if (!double.TryParse(degreesToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
        {
            return false;
        }

        if (!double.TryParse(minutesToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
        {
            return false;
        }

        double seconds = 0d;
        int secondsMarker = text.IndexOf('"', StringComparison.Ordinal);
        if (secondsMarker > mIndex + 1)
        {
            string secondsToken = text.Substring(mIndex + 1, secondsMarker - mIndex - 1);
            if (!double.TryParse(secondsToken, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
            {
                return false;
            }
        }

        value = sign * (degrees + (minutes / 60d) + (seconds / 3600d));
        return true;
    }

    private readonly struct LogicalLine
    {
        public LogicalLine(string content, int lineNumber)
        {
            this.Content = content;
            this.LineNumber = lineNumber;
        }

        public string Content { get; }

        public int LineNumber { get; }
    }
}
