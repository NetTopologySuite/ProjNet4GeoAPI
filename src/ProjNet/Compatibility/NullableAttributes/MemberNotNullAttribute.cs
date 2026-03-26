// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

#nullable enable annotations

using System;

/// <summary>
/// Specifies that the listed fields and properties are non-null when the attributed method returns successfully.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
internal sealed class MemberNotNullAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullAttribute"/> class.
    /// </summary>
    /// <param name="member">Member guaranteed to be non-null.</param>
    internal MemberNotNullAttribute(string member)
    {
        this.Members = [member];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullAttribute"/> class.
    /// </summary>
    /// <param name="members">Members guaranteed to be non-null.</param>
    internal MemberNotNullAttribute(params string[] members)
    {
        this.Members = members;
    }

    /// <summary>
    /// Gets members that are guaranteed to be non-null.
    /// </summary>
    internal string[] Members { get; }
}
#endif
