// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents the direct proj2proj parity fixture payload.
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Public visibility is required because xUnit theory methods consume this DTO as a public parameter type.")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "System.Text.Json materializes this DTO via writable List<T>.")]
[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Writable setter is required for fixture deserialization.")]
public sealed class Proj2ProjFixture
{
    /// <summary>
    /// Gets or sets the fixture schema/version marker.
    /// </summary>
    public int FixtureVersion { get; set; }

    /// <summary>
    /// Gets or sets the generator identifier used to produce the fixture.
    /// </summary>
    public string Generator { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parity cases included in the fixture payload.
    /// </summary>
    public List<Proj2ProjCase> Cases { get; set; } = new List<Proj2ProjCase>();
}
