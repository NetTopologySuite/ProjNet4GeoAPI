// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

using System;

/// <summary>
/// Indicates that the specified method requires dynamic access to code that may be removed by trimming.
/// </summary>
[AttributeUsage(
    AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Class,
    Inherited = false)]
internal sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class.
    /// </summary>
    /// <param name="message">Message that describes why the member is not trimming-safe.</param>
    internal RequiresUnreferencedCodeAttribute(string message)
    {
        this.Message = message;
    }

    /// <summary>
    /// Gets the trimming warning message.
    /// </summary>
    internal string Message { get; }

    /// <summary>
    /// Gets or sets the URL with additional guidance.
    /// </summary>
    internal string? Url { get; set; }
}
#endif
