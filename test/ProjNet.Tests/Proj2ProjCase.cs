// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents a single direct proj2proj parity fixture row.
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Public visibility is required because xUnit theory methods consume this DTO as a public parameter type.")]
public sealed class Proj2ProjCase
{
    /// <summary>
    /// Gets or sets the EPSG operation code for the parity case.
    /// </summary>
    public int OperationCode { get; set; }

    /// <summary>
    /// Gets or sets the source SRID.
    /// </summary>
    public int SourceSrid { get; set; }

    /// <summary>
    /// Gets or sets the target SRID.
    /// </summary>
    public int TargetSrid { get; set; }

    /// <summary>
    /// Gets or sets the source CRS WKT definition.
    /// </summary>
    public string SourceWkt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target CRS WKT definition.
    /// </summary>
    public string TargetWkt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input x coordinate.
    /// </summary>
    public double InputX { get; set; }

    /// <summary>
    /// Gets or sets the input y coordinate.
    /// </summary>
    public double InputY { get; set; }

    /// <summary>
    /// Gets or sets the expected x coordinate.
    /// </summary>
    public double ExpectedX { get; set; }

    /// <summary>
    /// Gets or sets the expected y coordinate.
    /// </summary>
    public double ExpectedY { get; set; }

    /// <summary>
    /// Gets or sets the tolerance in meters for result comparison.
    /// </summary>
    public double ToleranceMeters { get; set; }
}
