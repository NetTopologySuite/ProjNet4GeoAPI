// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2002 Urban Science Applications, Inc.
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from GeoTools.NET.

namespace ProjNet.IO.CoordinateSystems;

/// <summary>
/// Represents the type of token created by the StreamTokenizer class.
/// </summary>
internal enum TokenType
{
    /// <summary>
    /// Indicates that the token is a word.
    /// </summary>
    Word,

    /// <summary>
    /// Indicates that the token is a number.
    /// </summary>
    Number,

    /// <summary>
    /// Indicates that the end of line has been read. The field can only have this value if the eolIsSignificant method has been called with the argument true.
    /// </summary>
    Eol,

    /// <summary>
    /// Indicates that the end of the input stream has been reached.
    /// </summary>
    Eof,

    /// <summary>
    /// Indictaes that the token is white space (space, tab, newline).
    /// </summary>
    Whitespace,

    /// <summary>
    /// Characters that are not whitespace, numbers, etc...
    /// </summary>
    Symbol,
}
