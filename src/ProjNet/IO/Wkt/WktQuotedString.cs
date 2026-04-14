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
    private string? value;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktQuotedString"/> class.
    /// </summary>
    /// <param name="value">The string value (without surrounding quotes).</param>
    public WktQuotedString(string value)
    {
        value = ArgumentGuard.ThrowIfNull(value, nameof(value));
        this.rawContent = CreateRawContent(value);
        this.value = value;
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
    }

    /// <summary>
    /// Gets the unquoted string value.
    /// </summary>
    public string Value => this.value ??= DecodeValue(this.rawContent);

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder(this.rawContent.Length + 2);
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
        this.rawContent.AppendTo(builder);
        builder.Append('"');
    }

    private static WktTextSlice CreateRawContent(string value)
    {
        int quoteCount = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '"')
            {
                quoteCount++;
            }
        }

        if (quoteCount == 0)
        {
            return new WktTextSlice(value);
        }

        var builder = new StringBuilder(value.Length + quoteCount);
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (current == '"')
            {
                builder.Append('"');
            }

            builder.Append(current);
        }

        return new WktTextSlice(builder.ToString());
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
}
