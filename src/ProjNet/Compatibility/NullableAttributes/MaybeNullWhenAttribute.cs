// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

using System;

/// <summary>
/// Specifies that a parameter may be <see langword="null"/> when the associated method returns the specified value.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaybeNullWhenAttribute"/> class.
    /// </summary>
    /// <param name="returnValue">Return value condition that allows <see langword="null"/>.</param>
    internal MaybeNullWhenAttribute(bool returnValue)
    {
        this.ReturnValue = returnValue;
    }

    /// <summary>
    /// Gets a value indicating whether <see langword="null"/> is allowed when the method returns this value.
    /// </summary>
    internal bool ReturnValue { get; }
}
#endif
