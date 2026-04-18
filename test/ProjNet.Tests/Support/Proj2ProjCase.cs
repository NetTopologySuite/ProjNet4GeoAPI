// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System.Diagnostics.CodeAnalysis;
using Xunit.Sdk;

/// <summary>
/// Represents a single direct proj2proj parity fixture row.
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Public visibility is required because xUnit theory methods consume this DTO as a public parameter type.")]
public sealed class Proj2ProjCase : IXunitSerializable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Proj2ProjCase"/> class.
    /// </summary>
    public Proj2ProjCase()
    {
    }

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

    /// <inheritdoc/>
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(this.OperationCode), this.OperationCode);
        info.AddValue(nameof(this.SourceSrid), this.SourceSrid);
        info.AddValue(nameof(this.TargetSrid), this.TargetSrid);
        info.AddValue(nameof(this.SourceWkt), this.SourceWkt);
        info.AddValue(nameof(this.TargetWkt), this.TargetWkt);
        info.AddValue(nameof(this.InputX), this.InputX);
        info.AddValue(nameof(this.InputY), this.InputY);
        info.AddValue(nameof(this.ExpectedX), this.ExpectedX);
        info.AddValue(nameof(this.ExpectedY), this.ExpectedY);
        info.AddValue(nameof(this.ToleranceMeters), this.ToleranceMeters);
    }

    /// <inheritdoc/>
    public void Deserialize(IXunitSerializationInfo info)
    {
        this.OperationCode = info.GetValue<int>(nameof(this.OperationCode));
        this.SourceSrid = info.GetValue<int>(nameof(this.SourceSrid));
        this.TargetSrid = info.GetValue<int>(nameof(this.TargetSrid));
        this.SourceWkt = info.GetValue<string>(nameof(this.SourceWkt)) ?? string.Empty;
        this.TargetWkt = info.GetValue<string>(nameof(this.TargetWkt)) ?? string.Empty;
        this.InputX = info.GetValue<double>(nameof(this.InputX));
        this.InputY = info.GetValue<double>(nameof(this.InputY));
        this.ExpectedX = info.GetValue<double>(nameof(this.ExpectedX));
        this.ExpectedY = info.GetValue<double>(nameof(this.ExpectedY));
        this.ToleranceMeters = info.GetValue<double>(nameof(this.ToleranceMeters));
    }

    /// <inheritdoc/>
    public override string ToString() => $"Op{this.OperationCode} {this.SourceSrid}->{this.TargetSrid}";
}
