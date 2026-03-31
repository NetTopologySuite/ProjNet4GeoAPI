// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

/// <summary>
/// Represents an unquoted identifier or literal value in WKT, such as an axis orientation
/// (e.g. <c>NORTH</c>) or a raw WKT fragment.
/// </summary>
public sealed class WktIdentifier : WktNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WktIdentifier"/> class.
    /// </summary>
    /// <param name="name">The identifier name or literal text.</param>
    public WktIdentifier(string name) => this.Name = name;

    /// <summary>
    /// Gets the identifier name or literal text.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => this.Name;

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4) => this.ToString();
}
