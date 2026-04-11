// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using Xunit.v3;

/// <summary>
/// Marks a test with the upstream GitHub issue that it verifies.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class GitHubIssueAttribute : Attribute, ITraitAttribute
{
    private readonly int issueNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubIssueAttribute"/> class.
    /// </summary>
    /// <param name="issueNumber">The upstream GitHub issue number.</param>
    public GitHubIssueAttribute(int issueNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(issueNumber);
        this.issueNumber = issueNumber;
    }

    /// <summary>
    /// Gets the upstream GitHub issue number.
    /// </summary>
    public int IssueNumber => this.issueNumber;

    /// <summary>
    /// Gets the xUnit traits emitted by this attribute.
    /// </summary>
    /// <returns>The xUnit traits describing the GitHub issue linkage.</returns>
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
    [
        new("Category", "GitHub Issue"),
        new("GitHubIssue", $"#{this.issueNumber}"),
    ];
}
