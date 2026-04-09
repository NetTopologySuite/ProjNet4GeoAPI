// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

/// <summary>
/// Describes the current PROJJSON support level for one reader or writer surface.
/// </summary>
internal enum ProjJsonCoverageStatus
{
    /// <summary>
    /// The feature is supported as-is.
    /// </summary>
    Supported,

    /// <summary>
    /// The feature is accepted only partially or is emitted with reduced fidelity.
    /// </summary>
    Partial,

    /// <summary>
    /// The feature is tolerated but ignored.
    /// </summary>
    Ignored,

    /// <summary>
    /// The feature is not currently supported.
    /// </summary>
    Unsupported,
}
