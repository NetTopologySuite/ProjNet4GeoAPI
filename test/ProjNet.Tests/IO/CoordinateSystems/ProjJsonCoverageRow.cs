// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

/// <summary>
/// Represents one row of the milestone-40 PROJJSON coverage matrix.
/// </summary>
/// <param name="Feature">The tracked PROJJSON type or structural feature.</param>
/// <param name="ReaderStatus">The current reader support status.</param>
/// <param name="ReaderReference">The primary reader symbol reference.</param>
/// <param name="WriterStatus">The current writer support status.</param>
/// <param name="WriterReference">The primary writer symbol reference.</param>
/// <param name="Notes">A short explanation of the current behavior.</param>
internal sealed record ProjJsonCoverageRow(
    string Feature,
    ProjJsonCoverageStatus ReaderStatus,
    string ReaderReference,
    ProjJsonCoverageStatus WriterStatus,
    string WriterReference,
    string Notes);
