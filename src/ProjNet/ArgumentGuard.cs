// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides lightweight argument validation helpers shared across targets.
/// </summary>
internal static class ArgumentGuard
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> when <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNull(object value, string paramName)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
    }
}
