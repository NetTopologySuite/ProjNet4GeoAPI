// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

internal static class GieParser
{
    private static readonly char[] WhiteSpaceSeparators = { ' ', '\t' };

    public static IReadOnlyList<GieCase> ParseFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Parse(File.ReadAllText(path));
    }

    public static IReadOnlyList<GieCase> Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var parsedCases = new List<GieCase>();
        string currentOperation = null;
        double currentToleranceValue = 0d;
        string currentToleranceUnit = "m";
        GieDirection currentDirection = GieDirection.Forward;
        double[] pendingAccept = null;

        string[] lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string stripped = StripInlineComment(lines[i]).Trim();
            if (stripped.Length == 0 || IsTagLine(stripped))
            {
                continue;
            }

            string directive;
            string payload;
            SplitDirective(stripped, out directive, out payload);

            if (directive.Equals("operation", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePayload(payload, "operation", i + 1);
                currentOperation = payload;
                currentDirection = GieDirection.Forward;
                pendingAccept = null;
            }
            else if (directive.Equals("tolerance", StringComparison.OrdinalIgnoreCase))
            {
                ParseTolerance(payload, i + 1, out currentToleranceValue, out currentToleranceUnit);
            }
            else if (directive.Equals("direction", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePayload(payload, "direction", i + 1);
                currentDirection = ParseDirection(payload, i + 1);
            }
            else if (directive.Equals("accept", StringComparison.OrdinalIgnoreCase))
            {
                EnsureOperationDeclared(currentOperation, i + 1, "accept");
                pendingAccept = ParseVector(payload, i + 1, "accept");
            }
            else if (directive.Equals("expect", StringComparison.OrdinalIgnoreCase))
            {
                EnsureOperationDeclared(currentOperation, i + 1, "expect");
                if (pendingAccept is null)
                {
                    throw new FormatException("Found 'expect' without preceding 'accept' at line " + (i + 1).ToString(CultureInfo.InvariantCulture) + ".");
                }

                var expected = ParseVector(payload, i + 1, "expect");
                parsedCases.Add(
                    new GieCase
                    {
                        Operation = currentOperation,
                        ToleranceValue = currentToleranceValue,
                        ToleranceUnit = currentToleranceUnit,
                        Direction = currentDirection,
                        Accept = pendingAccept,
                        Expect = expected,
                    });
                pendingAccept = null;
            }
            else
            {
                throw new FormatException("Unsupported GIE directive '" + directive + "' at line " + (i + 1).ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        if (pendingAccept is not null)
        {
            throw new FormatException("Dangling 'accept' without matching 'expect' at end of input.");
        }

        return parsedCases;
    }

    private static void EnsureOperationDeclared(string operation, int lineNumber, string directive)
    {
        if (operation is null)
        {
            throw new FormatException(
                "Found '" + directive + "' before any 'operation' declaration at line " + lineNumber.ToString(CultureInfo.InvariantCulture) + ".");
        }
    }

    private static bool IsTagLine(string line)
    {
        return line.StartsWith('<') && line.EndsWith('>');
    }

    private static string StripInlineComment(string line)
    {
        if (line is null)
        {
            return string.Empty;
        }

        int commentIndex = line.IndexOf('#', StringComparison.Ordinal);
        return commentIndex < 0 ? line : line.Substring(0, commentIndex);
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

        directive = line.Substring(0, splitIndex);
        payload = line.Substring(splitIndex + 1).Trim();
    }

    private static void ParseTolerance(string payload, int lineNumber, out double value, out string unit)
    {
        EnsurePayload(payload, "tolerance", lineNumber);
        string[] tokens = payload.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new FormatException("Missing tolerance value at line " + lineNumber.ToString(CultureInfo.InvariantCulture) + ".");
        }

        value = ParseNumber(tokens[0], lineNumber, "tolerance");
        unit = tokens.Length > 1 ? tokens[1] : "m";
    }

    private static GieDirection ParseDirection(string payload, int lineNumber)
    {
        string normalized = payload.Trim();
        if (normalized.Equals("forward", StringComparison.OrdinalIgnoreCase))
        {
            return GieDirection.Forward;
        }

        if (normalized.Equals("inverse", StringComparison.OrdinalIgnoreCase))
        {
            return GieDirection.Inverse;
        }

        throw new FormatException("Unsupported direction '" + payload + "' at line " + lineNumber.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static double[] ParseVector(string payload, int lineNumber, string directiveName)
    {
        EnsurePayload(payload, directiveName, lineNumber);
        string[] tokens = payload.Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            throw new FormatException(
                "Directive '" + directiveName + "' requires at least two numeric values at line " + lineNumber.ToString(CultureInfo.InvariantCulture) + ".");
        }

        var values = new double[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            values[i] = ParseNumber(tokens[i], lineNumber, directiveName);
        }

        return values;
    }

    private static double ParseNumber(string token, int lineNumber, string directiveName)
    {
        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }

        throw new FormatException(
            "Failed to parse numeric value '" + token + "' in directive '" + directiveName + "' at line " + lineNumber.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static void EnsurePayload(string payload, string directiveName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new FormatException("Directive '" + directiveName + "' is missing payload at line " + lineNumber.ToString(CultureInfo.InvariantCulture) + ".");
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
}
