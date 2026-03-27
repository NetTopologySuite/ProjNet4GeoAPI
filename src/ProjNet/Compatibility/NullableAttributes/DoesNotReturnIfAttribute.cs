// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

#nullable enable

using System;

/// <summary>
/// Specifies that the method will not return if the associated <see cref="bool"/> parameter has the specified value.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class DoesNotReturnIfAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DoesNotReturnIfAttribute"/> class.
    /// </summary>
    /// <param name="parameterValue">Parameter value condition that causes the method not to return.</param>
    internal DoesNotReturnIfAttribute(bool parameterValue)
    {
        this.ParameterValue = parameterValue;
    }

    /// <summary>
    /// Gets a value indicating whether the method does not return when the associated parameter equals this value.
    /// </summary>
    internal bool ParameterValue { get; }
}
#endif
