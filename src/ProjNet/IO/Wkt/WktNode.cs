// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

/// <summary>
/// Abstract base class for all WKT (Well-Known Text) syntax tree nodes.
/// </summary>
public abstract class WktNode
{
    /// <summary>
    /// Writes this node as a compact WKT string (single line, no extra whitespace).
    /// </summary>
    /// <returns>A compact WKT string representation of this node.</returns>
    public abstract override string ToString();

    /// <summary>
    /// Writes this node as a formatted WKT string with indentation.
    /// </summary>
    /// <param name="indentLevel">The current indentation level.</param>
    /// <param name="indentSize">The number of spaces per indentation level.</param>
    /// <returns>A formatted WKT string with indentation.</returns>
    public abstract string ToFormattedString(int indentLevel = 0, int indentSize = 4);
}
