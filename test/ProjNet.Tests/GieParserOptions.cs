// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

/// <summary>
/// Represents the documented type.
/// </summary>
internal sealed class GieParserOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether unknown directives are ignored during parsing.
    /// </summary>
    public bool IgnoreUnknownDirectives { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether multi-line operation directives are allowed.
    /// </summary>
    public bool AllowOperationContinuation { get; set; } = true;
}
