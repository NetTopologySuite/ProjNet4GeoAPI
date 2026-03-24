// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

/// <summary>
/// Represents the documented type.
/// </summary>
internal sealed class GieCase
{
    /// <summary>
    /// Gets or sets the source line number of the parsed case.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the PROJ operation string associated with the case.
    /// </summary>
    public string Operation { get; set; }

    /// <summary>
    /// Gets or sets the numeric tolerance value used for comparisons.
    /// </summary>
    public double ToleranceValue { get; set; }

    /// <summary>
    /// Gets or sets the tolerance unit token as parsed from the fixture.
    /// </summary>
    public string ToleranceUnit { get; set; }

    /// <summary>
    /// Gets or sets the transformation direction for the case.
    /// </summary>
    public GieDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the accepted input coordinate tuple.
    /// </summary>
    public double[] Accept { get; set; }

    /// <summary>
    /// Gets or sets the expected output coordinate tuple.
    /// </summary>
    public double[] Expect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the case expects a transformation failure.
    /// </summary>
    public bool ExpectsFailure { get; set; }

    /// <summary>
    /// Gets or sets the expected error code when a failure is expected.
    /// </summary>
    public string ExpectedErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the optional roundtrip count for iterative validation.
    /// </summary>
    public int? RoundtripCount { get; set; }
}
