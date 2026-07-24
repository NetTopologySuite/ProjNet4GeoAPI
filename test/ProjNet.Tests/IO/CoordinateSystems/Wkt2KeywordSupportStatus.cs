// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

/// <summary>
/// Describes how a WKT2 keyword is currently handled by the milestone-40 inventory.
/// </summary>
internal enum Wkt2KeywordSupportStatus
{
    /// <summary>
    /// The keyword is handled directly by the native WKT2 reader.
    /// </summary>
    Native,

    /// <summary>
    /// The keyword is only handled through the legacy normalization fallback.
    /// </summary>
    LegacyNormalized,

    /// <summary>
    /// The keyword is partially supported through reusable native reader components but not yet as a complete standalone construct.
    /// </summary>
    Partial,

    /// <summary>
    /// The keyword is accepted but skipped as non-operational metadata.
    /// </summary>
    Ignored,

    /// <summary>
    /// The keyword is currently outside the supported reader surface.
    /// </summary>
    Unsupported,
}
