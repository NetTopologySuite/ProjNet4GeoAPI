// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System.Globalization;

/// <summary>
/// Represents a numeric value in WKT, e.g. <c>6378137</c>.
/// </summary>
public sealed class WktNumber : WktNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WktNumber"/> class.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    public WktNumber(double value) => this.Value = value;

    /// <summary>
    /// Gets the numeric value.
    /// </summary>
    public double Value { get; }

    /// <inheritdoc />
    public override string ToString() => this.Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4) => this.ToString();
}
