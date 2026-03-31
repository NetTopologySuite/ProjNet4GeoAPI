// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

/// <summary>
/// Represents a quoted string value in WKT, e.g. <c>"WGS 84"</c>.
/// </summary>
public sealed class WktQuotedString : WktNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WktQuotedString"/> class.
    /// </summary>
    /// <param name="value">The string value (without surrounding quotes).</param>
    public WktQuotedString(string value) => this.Value = value;

    /// <summary>
    /// Gets the unquoted string value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => $"\"{this.Value}\"";

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4) => this.ToString();
}
