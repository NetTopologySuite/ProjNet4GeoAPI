// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System.Globalization;

/// <summary>
/// Represents an integer value in WKT, e.g. <c>4326</c>.
/// </summary>
public sealed class WktInteger : WktNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WktInteger"/> class.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public WktInteger(int value) => this.Value = value;

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public int Value { get; }

    /// <inheritdoc />
    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4) => this.ToString();
}
