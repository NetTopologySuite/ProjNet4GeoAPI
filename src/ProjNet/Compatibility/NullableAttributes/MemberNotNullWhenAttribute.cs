// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis;

using System;

/// <summary>
/// Specifies that the listed fields and properties are non-null when the attributed method returns the specified value.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
internal sealed class MemberNotNullWhenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullWhenAttribute"/> class.
    /// </summary>
    /// <param name="returnValue">Return value that guarantees non-null members.</param>
    /// <param name="member">Member guaranteed to be non-null.</param>
    internal MemberNotNullWhenAttribute(bool returnValue, string member)
    {
        this.ReturnValue = returnValue;
        this.Members = [member];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberNotNullWhenAttribute"/> class.
    /// </summary>
    /// <param name="returnValue">Return value that guarantees non-null members.</param>
    /// <param name="members">Members guaranteed to be non-null.</param>
    internal MemberNotNullWhenAttribute(bool returnValue, params string[] members)
    {
        this.ReturnValue = returnValue;
        this.Members = members;
    }

    /// <summary>
    /// Gets a value indicating whether members are guaranteed non-null for this return value.
    /// </summary>
    internal bool ReturnValue { get; }

    /// <summary>
    /// Gets members that are guaranteed to be non-null.
    /// </summary>
    internal string[] Members { get; }
}
#endif
