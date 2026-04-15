// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Centralizes cold projection exception paths to keep hot transform methods smaller.
/// </summary>
internal static class ProjectionThrowHelper
{
    /// <summary>
    /// Throws when a projected coordinate lies outside the supported projection domain.
    /// </summary>
    [DoesNotReturn]
    internal static void ThrowOutsideProjectionDomain()
    {
        throw new InvalidOperationException("Input data outside projection domain.");
    }

    /// <summary>
    /// Throws when a projected coordinate lies outside the supported projection domain.
    /// </summary>
    /// <typeparam name="T">The return type required by the calling expression.</typeparam>
    /// <returns>This method never returns.</returns>
    [DoesNotReturn]
    internal static T ThrowOutsideProjectionDomain<T>()
    {
        throw new InvalidOperationException("Input data outside projection domain.");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> with the supplied message.
    /// </summary>
    /// <param name="message">Failure description.</param>
    [DoesNotReturn]
    internal static void ThrowInvalidOperation(string message)
    {
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> with the supplied message.
    /// </summary>
    /// <typeparam name="T">The return type required by the calling expression.</typeparam>
    /// <param name="message">Failure description.</param>
    /// <returns>This method never returns.</returns>
    [DoesNotReturn]
    internal static T ThrowInvalidOperation<T>(string message)
    {
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> with the supplied message.
    /// </summary>
    /// <param name="message">Failure description.</param>
    [DoesNotReturn]
    internal static void ThrowNotSupported(string message)
    {
        throw new NotSupportedException(message);
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/> with the supplied message.
    /// </summary>
    /// <typeparam name="T">The return type required by the calling expression.</typeparam>
    /// <param name="message">Failure description.</param>
    /// <returns>This method never returns.</returns>
    [DoesNotReturn]
    internal static T ThrowNotSupported<T>(string message)
    {
        throw new NotSupportedException(message);
    }
}
