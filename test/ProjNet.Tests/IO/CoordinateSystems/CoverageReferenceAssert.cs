// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Guards that coverage-matrix references stay on stable symbol paths instead of file line ranges.
/// </summary>
internal static partial class CoverageReferenceAssert
{
    /// <summary>
    /// Verifies a comma-separated reference list only contains symbol references.
    /// </summary>
    /// <param name="referenceList">The stored matrix reference list.</param>
    internal static void AssertSymbolReferenceList(string referenceList)
    {
        Assert.False(string.IsNullOrWhiteSpace(referenceList));
        Assert.DoesNotContain(".cs:", referenceList, StringComparison.OrdinalIgnoreCase);

        foreach (string reference in referenceList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Assert.Matches(SymbolReferencePattern(), reference);
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex SymbolReferencePattern();
}
