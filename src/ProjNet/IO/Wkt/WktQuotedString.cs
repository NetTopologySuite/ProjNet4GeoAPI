// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System;
using System.Text;
using ProjNet;

/// <summary>
/// Represents a quoted string value in WKT, e.g. <c>"WGS 84"</c>.
/// </summary>
public sealed class WktQuotedString : WktNode
{
    private readonly WktTextSlice rawContent;
    private readonly bool usesRawContent;
    private string? value;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktQuotedString"/> class.
    /// </summary>
    /// <param name="value">The string value (without surrounding quotes).</param>
    public WktQuotedString(string value)
    {
        this.value = ArgumentGuard.ThrowIfNull(value, nameof(value));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktQuotedString"/> class from raw quoted content.
    /// </summary>
    /// <param name="source">The WKT source string.</param>
    /// <param name="contentStart">The zero-based start index of the content inside the quotes.</param>
    /// <param name="contentLength">The length of the raw content inside the quotes.</param>
    internal WktQuotedString(string source, int contentStart, int contentLength)
    {
        this.rawContent = new WktTextSlice(source, contentStart, contentLength);
        this.usesRawContent = true;
    }

    /// <summary>
    /// Gets the unquoted string value.
    /// </summary>
    public string Value => this.value ??= DecodeValue(this.rawContent);

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder(this.Value.Length + 2);
        this.AppendTo(builder);
        return builder.ToString();
    }

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4) => this.ToString();

    /// <inheritdoc />
    internal override void AppendTo(StringBuilder builder)
    {
        ArgumentGuard.ThrowIfNull(builder, nameof(builder));
        builder.Append('"');
        if (this.usesRawContent)
        {
            this.rawContent.AppendTo(builder);
        }
        else
        {
            AppendEscapedValue(builder, this.Value);
        }

        builder.Append('"');
    }

    private static string DecodeValue(WktTextSlice rawContent)
    {
        if (!rawContent.Contains('"'))
        {
            return rawContent.ToText();
        }

        ReadOnlySpan<char> rawSpan = rawContent.AsSpan();
        var builder = new StringBuilder(rawSpan.Length);
        for (int i = 0; i < rawSpan.Length; i++)
        {
            char current = rawSpan[i];
            if (current == '"' && i + 1 < rawSpan.Length && rawSpan[i + 1] == '"')
            {
                builder.Append('"');
                i++;
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static void AppendEscapedValue(StringBuilder builder, string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (current == '"')
            {
                builder.Append('"');
                builder.Append('"');
            }
            else
            {
                builder.Append(current);
            }
        }
    }
}
