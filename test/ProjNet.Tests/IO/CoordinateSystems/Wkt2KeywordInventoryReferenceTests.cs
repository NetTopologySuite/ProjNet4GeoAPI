// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// Guards the milestone-40 WKT2 keyword reference set.
/// </summary>
public class Wkt2KeywordInventoryReferenceTests
{
    /// <summary>
    /// Ensures the primary inventory rows stay stable and easy to diff.
    /// </summary>
    [Fact]
    public void InventoryKeywords_AreUniqueSortedAndUppercase()
    {
        Assert.Equal(
            Wkt2KeywordInventoryReference.InventoryKeywords.OrderBy(keyword => keyword, StringComparer.Ordinal),
            Wkt2KeywordInventoryReference.InventoryKeywords);

        Assert.Equal(
            Wkt2KeywordInventoryReference.InventoryKeywords.Count,
            Wkt2KeywordInventoryReference.InventoryKeywords.Distinct(StringComparer.Ordinal).Count());

        Assert.All(
            Wkt2KeywordInventoryReference.InventoryKeywords,
            keyword => Assert.Equal(keyword, keyword.ToUpperInvariant()));
    }

    /// <summary>
    /// Ensures the secondary spellings are tracked separately from the primary inventory rows.
    /// </summary>
    [Fact]
    public void AdditionalKeywords_DoNotOverlapWithPrimaryInventoryRows()
    {
        IReadOnlyCollection<string> overlap = Wkt2KeywordInventoryReference.InventoryKeywords
            .Intersect(Wkt2KeywordInventoryReference.AdditionalKeywords, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(overlap);
    }

    /// <summary>
    /// Ensures the inventory contains the milestone anchor keywords for the current WKT2 backlog.
    /// </summary>
    [Fact]
    public void AllKeywords_ContainMilestone40AnchorKeywords()
    {
        string[] expectedKeywords =
        [
            "ABRIDGEDTRANSFORMATION",
            "BOUNDCRS",
            "COMPOUNDCRS",
            "COORDINATEMETADATA",
            "DERIVEDPROJCRS",
            "ENGCRS",
            "ENSEMBLE",
            "GEODCRS",
            "GEOGCRS",
            "PARAMETERFILE",
            "PARAMETRICCRS",
            "PROJCRS",
            "TIMECRS",
            "VERTCRS",
        ];

        foreach (string keyword in expectedKeywords)
        {
            Assert.Contains(keyword, Wkt2KeywordInventoryReference.AllKeywords);
        }
    }

    /// <summary>
    /// Ensures the WKT2 reference set does not silently pull in WKT1-only structural keywords.
    /// </summary>
    /// <param name="keyword">A WKT1-only compatibility keyword.</param>
    [Theory]
    [InlineData("PROJECTION")]
    [InlineData("SPHEROID")]
    [InlineData("UNIT")]
    public void AllKeywords_ExcludeWkt1OnlyStructuralKeywords(string keyword)
    {
        Assert.DoesNotContain(keyword, Wkt2KeywordInventoryReference.AllKeywords);
    }
}
