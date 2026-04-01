// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet;

using System;
using System.Text;

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
        return value.Split([oldValue], StringSplitOptions.None).Length > 1
            ? string.Join(newValue, value.Split([oldValue], StringSplitOptions.None))
            : value;
#endif
    }

    /// <summary>
    /// Replaces all ordinal-ignore-case matches of <paramref name="oldValue"/> with <paramref name="newValue"/>.
    /// </summary>
    /// <param name="value">Input string to search.</param>
    /// <param name="oldValue">Substring to replace.</param>
    /// <param name="newValue">Replacement substring.</param>
    /// <returns>The transformed string.</returns>
    internal static string ReplaceOrdinalIgnoreCase(string value, string oldValue, string newValue)
    {
#if NETSTANDARD2_1_OR_GREATER
        return value.Replace(oldValue, newValue, StringComparison.OrdinalIgnoreCase);
#else
        int matchIndex = value.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        int startIndex = 0;
        while (matchIndex >= 0)
        {
            builder.Append(value, startIndex, matchIndex - startIndex);
            builder.Append(newValue);
            startIndex = matchIndex + oldValue.Length;
            matchIndex = value.IndexOf(oldValue, startIndex, StringComparison.OrdinalIgnoreCase);
        }

        builder.Append(value, startIndex, value.Length - startIndex);
        return builder.ToString();
#endif
    }
}
