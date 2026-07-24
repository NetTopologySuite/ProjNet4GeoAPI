// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

/// <summary>
/// Identifies the WKT dialect to emit when serializing syntax tree nodes.
/// </summary>
public enum WktVersion
{
    /// <summary>
    /// The legacy OGC simple-features / WKT1 form currently used by the existing writer implementation.
    /// </summary>
    Wkt1 = 0,

    /// <summary>
    /// The ISO 19162:2019 WKT2 form.
    /// </summary>
    Wkt22019 = 1,
}
