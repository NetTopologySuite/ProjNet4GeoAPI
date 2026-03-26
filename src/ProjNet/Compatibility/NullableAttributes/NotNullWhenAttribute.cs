// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

#nullable enable annotations

using System;

/// <summary>
/// Specifies that a parameter is not <see langword="null"/> when the associated method returns the specified value.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNullWhenAttribute"/> class.
    /// </summary>
    /// <param name="returnValue">Return value condition that guarantees non-null.</param>
    internal NotNullWhenAttribute(bool returnValue)
    {
        this.ReturnValue = returnValue;
    }

    /// <summary>
    /// Gets a value indicating whether the value is guaranteed non-null when the method returns this value.
    /// </summary>
    internal bool ReturnValue { get; }
}
#endif
