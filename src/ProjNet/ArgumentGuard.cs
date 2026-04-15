// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides lightweight argument validation helpers shared across targets.
/// </summary>
internal static class ArgumentGuard
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> when <paramref name="value"/> is <see langword="null"/>.
    /// Returns the non-null value for inline assignment scenarios.
    /// </summary>
    /// <typeparam name="T">Reference type of the value being validated.</typeparam>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <returns>The validated non-null <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    internal static T ThrowIfNull<T>(
        [NotNull] T? value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
#else
        string paramName)
        where T : class
    {
        return value is null ? throw new ArgumentNullException(paramName) : value;
    }
#endif

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> when <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNull(
        object? value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
    }
#else
        string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
#endif

    /// <summary>
    /// Throws when <paramref name="value"/> is <see langword="null"/> or empty.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <returns>The validated non-null, non-empty <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string ThrowIfNullOrEmpty(
        string? value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }
#else
        string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value.Length == 0 ? throw new ArgumentException("Value cannot be empty.", paramName) : value;
    }
#endif

    /// <summary>
    /// Throws when <paramref name="value"/> is <see langword="null"/>, empty, or whitespace.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <returns>The validated non-null, non-empty, non-whitespace <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string ThrowIfNullOrWhiteSpace(
        string? value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }
#else
        string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be empty or whitespace.", paramName) : value;
    }
#endif

    /// <summary>
    /// Throws when <paramref name="value"/> is negative.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNegative(
        double value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
#else
        string paramName)
#endif
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName ?? nameof(value));
#else
        if (value < 0d)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value cannot be negative.");
        }
#endif
    }

    /// <summary>
    /// Throws when <paramref name="value"/> is not a finite number.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <param name="message">Exception message when the value is not finite.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotFinite(
        double value,
        string paramName,
        string message = "Value must be finite.")
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, message);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowArgumentNull(string paramName)
    {
        throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> with parameter context.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowArgument(string message, string paramName)
    {
        throw new ArgumentException(message, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="message">Exception message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowArgument(string message)
    {
        throw new ArgumentException(message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> and satisfies expression contexts.
    /// </summary>
    /// <typeparam name="T">Return type used by the caller expression.</typeparam>
    /// <param name="message">Exception message.</param>
    /// <returns>This method always throws; no value is returned.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static T ThrowArgument<T>(string message)
    {
        throw new ArgumentException(message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> with parameter context and satisfies expression contexts.
    /// </summary>
    /// <typeparam name="T">Return type used by the caller expression.</typeparam>
    /// <param name="message">Exception message.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <returns>This method always throws; no value is returned.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static T ThrowArgument<T>(string message, string paramName)
    {
        throw new ArgumentException(message, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> with actual value context.
    /// </summary>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <param name="actualValue">Actual out-of-range value.</param>
    /// <param name="message">Exception message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRange(string paramName, object actualValue, string message)
    {
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRange(string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    /// <param name="message">Exception message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRange(string paramName, string message)
    {
        throw new ArgumentOutOfRangeException(paramName, message);
    }
}
