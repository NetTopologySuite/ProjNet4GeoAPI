// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System.Diagnostics.CodeAnalysis;
using Xunit.Sdk;

/// <summary>
/// Represents a single forward→inverse roundtrip accuracy fixture row.
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "Public visibility is required because xUnit theory methods consume this DTO as a public parameter type.")]
public sealed class RoundtripAccuracyCase : IXunitSerializable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoundtripAccuracyCase"/> class.
    /// </summary>
    public RoundtripAccuracyCase()
    {
    }

    /// <summary>
    /// Gets or sets a human-readable description for this test case.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source EPSG SRID.
    /// </summary>
    public int SourceSrid { get; set; }

    /// <summary>
    /// Gets or sets the target EPSG SRID.
    /// </summary>
    public int TargetSrid { get; set; }

    /// <summary>
    /// Gets or sets the input longitude (geographic source coordinate).
    /// </summary>
    public double InputLon { get; set; }

    /// <summary>
    /// Gets or sets the input latitude (geographic source coordinate).
    /// </summary>
    public double InputLat { get; set; }

    /// <summary>
    /// Gets or sets the expected forward-projected x coordinate from PROJ.
    /// </summary>
    public double ForwardX { get; set; }

    /// <summary>
    /// Gets or sets the expected forward-projected y coordinate from PROJ.
    /// </summary>
    public double ForwardY { get; set; }

    /// <summary>
    /// Gets or sets the longitude obtained by PROJ's inverse transform of the forward result.
    /// </summary>
    public double InverseBackLon { get; set; }

    /// <summary>
    /// Gets or sets the latitude obtained by PROJ's inverse transform of the forward result.
    /// </summary>
    public double InverseBackLat { get; set; }

    /// <summary>
    /// Gets or sets the tolerance in meters for result comparison.
    /// </summary>
    public double ToleranceMeters { get; set; }

    /// <inheritdoc/>
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(this.Description), this.Description);
        info.AddValue(nameof(this.SourceSrid), this.SourceSrid);
        info.AddValue(nameof(this.TargetSrid), this.TargetSrid);
        info.AddValue(nameof(this.InputLon), this.InputLon);
        info.AddValue(nameof(this.InputLat), this.InputLat);
        info.AddValue(nameof(this.ForwardX), this.ForwardX);
        info.AddValue(nameof(this.ForwardY), this.ForwardY);
        info.AddValue(nameof(this.InverseBackLon), this.InverseBackLon);
        info.AddValue(nameof(this.InverseBackLat), this.InverseBackLat);
        info.AddValue(nameof(this.ToleranceMeters), this.ToleranceMeters);
    }

    /// <inheritdoc/>
    public void Deserialize(IXunitSerializationInfo info)
    {
        this.Description = info.GetValue<string>(nameof(this.Description)) ?? string.Empty;
        this.SourceSrid = info.GetValue<int>(nameof(this.SourceSrid));
        this.TargetSrid = info.GetValue<int>(nameof(this.TargetSrid));
        this.InputLon = info.GetValue<double>(nameof(this.InputLon));
        this.InputLat = info.GetValue<double>(nameof(this.InputLat));
        this.ForwardX = info.GetValue<double>(nameof(this.ForwardX));
        this.ForwardY = info.GetValue<double>(nameof(this.ForwardY));
        this.InverseBackLon = info.GetValue<double>(nameof(this.InverseBackLon));
        this.InverseBackLat = info.GetValue<double>(nameof(this.InverseBackLat));
        this.ToleranceMeters = info.GetValue<double>(nameof(this.ToleranceMeters));
    }

    /// <inheritdoc/>
    public override string ToString() => $"{this.Description} ({this.SourceSrid}->{this.TargetSrid})";
}
