// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System.Text;
using ProjNet;

/// <summary>
/// Represents an unquoted identifier or literal value in WKT, such as an axis orientation
/// (e.g. <c>NORTH</c>) or a raw WKT fragment.
/// </summary>
public sealed class WktIdentifier : WktNode
{
    private readonly WktTextSlice text;
    private string? name;

    /// <summary>
    /// Initializes a new instance of the <see cref="WktIdentifier"/> class.
    /// </summary>
    /// <param name="name">The identifier name or literal text.</param>
    public WktIdentifier(string name)
    {
        this.name = ArgumentGuard.ThrowIfNull(name, nameof(name));
        this.text = new WktTextSlice(this.name);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktIdentifier"/> class from a source slice.
    /// </summary>
    /// <param name="source">The WKT source string.</param>
    /// <param name="start">The zero-based start index of the identifier text.</param>
    /// <param name="length">The length of the identifier text.</param>
    internal WktIdentifier(string source, int start, int length)
    {
        this.text = new WktTextSlice(source, start, length);
    }

    /// <summary>
    /// Gets the identifier name or literal text.
    /// </summary>
    public string Name => this.name ??= this.text.ToText();

    /// <inheritdoc />
    public override string ToString() => this.Name;

    /// <inheritdoc />
    public override string ToFormattedString(int indentLevel = 0, int indentSize = 4) => this.ToString();

    /// <inheritdoc />
    internal override void AppendTo(StringBuilder builder)
    {
        this.text.AppendTo(builder);
    }
}
