// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

using System;

/// <summary>
/// Indicates that the specified member requires runtime code generation and may not be compatible with AOT.
/// </summary>
[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Class,
    Inherited = false)]
internal sealed class RequiresDynamicCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresDynamicCodeAttribute"/> class.
    /// </summary>
    /// <param name="message">Message that describes why the member is not AOT-safe.</param>
    internal RequiresDynamicCodeAttribute(string message)
    {
        this.Message = message;
    }

    /// <summary>
    /// Gets the AOT warning message.
    /// </summary>
    internal string Message { get; }

    /// <summary>
    /// Gets or sets the URL with additional guidance.
    /// </summary>
    internal string? Url { get; set; }
}
#endif
