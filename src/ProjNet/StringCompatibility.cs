// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet;

using System;

/// <summary>
/// Provides compatibility helpers for string operations across target frameworks.
/// </summary>
internal static class StringCompatibility
{
    /// <summary>
    /// Replaces all ordinal matches of <paramref name="oldValue"/> with <paramref name="newValue"/>.
    /// </summary>
    /// <param name="value">Input string to search.</param>
    /// <param name="oldValue">Substring to replace.</param>
    /// <param name="newValue">Replacement substring.</param>
    /// <returns>The transformed string.</returns>
    internal static string ReplaceOrdinal(string value, string oldValue, string newValue)
    {
#if NETSTANDARD2_1_OR_GREATER
        return value.Replace(oldValue, newValue, StringComparison.Ordinal);
#else
        return value.Replace(oldValue, newValue);
#endif
    }
}
