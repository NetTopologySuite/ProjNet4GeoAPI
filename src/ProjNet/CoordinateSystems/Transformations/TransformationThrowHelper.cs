// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides cold throw paths for transformation runtime failures.
/// </summary>
internal static class TransformationThrowHelper
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> for runtime transformation failures.
    /// </summary>
    /// <param name="message">The failure message.</param>
    [DoesNotReturn]
    internal static void ThrowInvalidOperation(string message)
    {
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> for runtime transformation failures from expression contexts.
    /// </summary>
    /// <typeparam name="T">The nominal return type.</typeparam>
    /// <param name="message">The failure message.</param>
    /// <returns>Never returns.</returns>
    [DoesNotReturn]
    internal static T ThrowInvalidOperation<T>(string message)
    {
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> for unsupported transformation operations.
    /// </summary>
    /// <param name="message">The failure message.</param>
    [DoesNotReturn]
    internal static void ThrowNotSupported(string message)
    {
        throw new NotSupportedException(message);
    }
}
