// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

/// <summary>
/// Represents one row of the milestone-40 WKT2 coverage inventory.
/// </summary>
/// <param name="Keyword">The WKT2 keyword.</param>
/// <param name="Status">The current support classification.</param>
/// <param name="Reference">The primary reader reference for that classification.</param>
/// <param name="Notes">A short explanation of the current behavior.</param>
internal sealed record Wkt2KeywordCoverageRow(
    string Keyword,
    Wkt2KeywordSupportStatus Status,
    string Reference,
    string Notes);
