// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.CoordinateSystems;

/// <summary>
/// An enumeration of possible bracket types.
/// </summary>
internal enum WktBracket
{
    /// <summary>
    /// Bracket type not specified.
    /// </summary>
    DontCare,

    /// <summary>
    /// Opener &quot;<c>(</c>&quot;, closer &quot;<c>)</c>&quot;.
    /// </summary>
    Round,

    /// <summary>
    /// Opener &quot;<c>[</c>&quot;, closer &quot;<c>]</c>&quot;.
    /// </summary>
    Square,

    // Brace
}
