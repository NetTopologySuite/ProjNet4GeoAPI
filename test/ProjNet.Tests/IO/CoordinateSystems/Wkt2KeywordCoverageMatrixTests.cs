// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// Guards the milestone-40 WKT2 coverage matrix.
/// </summary>
public class Wkt2KeywordCoverageMatrixTests
{
    /// <summary>
    /// Ensures every tracked keyword has exactly one coverage row.
    /// </summary>
    [Fact]
    public void Rows_CoverEveryTrackedKeywordExactlyOnce()
    {
        Assert.Equal(
            Wkt2KeywordInventoryReference.AllKeywords.OrderBy(keyword => keyword, StringComparer.Ordinal),
            Wkt2KeywordCoverageMatrix.Rows.Select(row => row.Keyword));

        Assert.Equal(
            Wkt2KeywordCoverageMatrix.Rows.Count,
            Wkt2KeywordCoverageMatrix.Rows.Select(row => row.Keyword).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Ensures the matrix remains diff-friendly and fully populated.
    /// </summary>
    [Fact]
    public void Rows_AreSortedAndPopulated()
    {
        Assert.Equal(
            Wkt2KeywordCoverageMatrix.Rows.OrderBy(row => row.Keyword, StringComparer.Ordinal).Select(row => row.Keyword),
            Wkt2KeywordCoverageMatrix.Rows.Select(row => row.Keyword));

        Assert.All(
            Wkt2KeywordCoverageMatrix.Rows,
            row =>
            {
                Assert.False(string.IsNullOrWhiteSpace(row.Reference));
                Assert.False(string.IsNullOrWhiteSpace(row.Notes));
            });
    }

    /// <summary>
    /// Ensures the matrix keeps the expected milestone anchor classifications.
    /// </summary>
    [Fact]
    public void AnchorKeywords_KeepTheirExpectedStatuses()
    {
        var lookup = Wkt2KeywordCoverageMatrix.Rows
            .ToDictionary(row => row.Keyword, row => row.Status, StringComparer.Ordinal);

        Assert.Equal(Wkt2KeywordSupportStatus.Native, lookup["BOUNDCRS"]);
        Assert.Equal(Wkt2KeywordSupportStatus.Native, lookup["GEOGCRS"]);
        Assert.Equal(Wkt2KeywordSupportStatus.Native, lookup["PROJCRS"]);
        Assert.Equal(Wkt2KeywordSupportStatus.LegacyNormalized, lookup["PROJECTEDCRS"]);
        Assert.Equal(Wkt2KeywordSupportStatus.Ignored, lookup["USAGE"]);
        Assert.Equal(Wkt2KeywordSupportStatus.Unsupported, lookup["ENSEMBLE"]);
        Assert.Equal(Wkt2KeywordSupportStatus.Unsupported, lookup["PARAMETRICCRS"]);
        Assert.Equal(Wkt2KeywordSupportStatus.Unsupported, lookup["TIMECRS"]);
    }
}
