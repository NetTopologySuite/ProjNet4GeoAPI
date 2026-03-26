// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

using System;

/// <summary>
/// Specifies that an output is non-null if the named parameter is non-null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotNullIfNotNullAttribute"/> class.
    /// </summary>
    /// <param name="parameterName">Name of the parameter whose null-state governs the target value.</param>
    internal NotNullIfNotNullAttribute(string parameterName)
    {
        this.ParameterName = parameterName;
    }

    /// <summary>
    /// Gets the parameter name that controls the target null-state.
    /// </summary>
    internal string ParameterName { get; }
}
#endif
