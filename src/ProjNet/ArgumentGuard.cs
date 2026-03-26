// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet;

using System;
using System.Collections.Generic;
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
    internal static void ThrowIfNull(
        object value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string paramName = null)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNullOrEmpty(
        string value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
    }
#else
        string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }
    }
#endif

    /// <summary>
    /// Throws when <paramref name="value"/> is <see langword="null"/>, empty, or whitespace.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNullOrWhiteSpace(
        string value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    }
#else
        string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
        }
    }
#endif

    /// <summary>
    /// Throws when <paramref name="value"/> is outside the inclusive range <paramref name="min"/> to <paramref name="max"/>.
    /// </summary>
    /// <typeparam name="T">Comparable value type.</typeparam>
    /// <param name="value">Value to validate.</param>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Inclusive upper bound.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfOutOfRange<T>(
        T value,
        T min,
        T max,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string paramName = null)
#else
        string paramName)
#endif
        where T : IComparable<T>
    {
        if (Comparer<T>.Default.Compare(min, max) > 0)
        {
            throw new ArgumentException("The minimum value cannot be greater than the maximum value.", nameof(min));
        }

#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(value, min, paramName ?? nameof(value));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, max, paramName ?? nameof(value));
#else
        if (Comparer<T>.Default.Compare(value, min) < 0 || Comparer<T>.Default.Compare(value, max) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be in the inclusive range [{min}, {max}].");
        }
#endif
    }

    /// <summary>
    /// Throws when <paramref name="value"/> is negative.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNegative(
        double value,
#if NET8_0_OR_GREATER
        [CallerArgumentExpression(nameof(value))] string paramName = null)
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
    /// Throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    internal static void ThrowArgument(string message, string paramName)
    {
        throw new ArgumentException(message, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="message">Exception message.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    internal static void ThrowArgumentOutOfRange(string paramName, object actualValue, string message)
    {
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    /// <param name="paramName">Parameter name for exception reporting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    internal static void ThrowArgumentOutOfRange(string paramName, string message)
    {
        throw new ArgumentOutOfRangeException(paramName, message);
    }
}
