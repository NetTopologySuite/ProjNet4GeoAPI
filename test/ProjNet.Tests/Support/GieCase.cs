// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using Xunit.Sdk;

/// <summary>
/// Represents a single GIE fixture test case, including the operation string, tolerance, input and expected coordinates, and transformation direction.
/// </summary>
public sealed class GieCase : IXunitSerializable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GieCase"/> class.
    /// </summary>
    public GieCase()
    {
    }

    /// <summary>
    /// Gets or sets the source line number of the parsed case.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the PROJ operation string associated with the case.
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the numeric tolerance value used for comparisons.
    /// </summary>
    public double ToleranceValue { get; set; }

    /// <summary>
    /// Gets or sets the tolerance unit token as parsed from the fixture.
    /// </summary>
    public string ToleranceUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transformation direction for the case.
    /// </summary>
    public GieDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the accepted input coordinate tuple.
    /// </summary>
    public double[] Accept { get; set; } = [];

    /// <summary>
    /// Gets or sets the expected output coordinate tuple.
    /// </summary>
    public double[] Expect { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the case expects a transformation failure.
    /// </summary>
    public bool ExpectsFailure { get; set; }

    /// <summary>
    /// Gets or sets the expected error code when a failure is expected.
    /// </summary>
    public string ExpectedErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional roundtrip count for iterative validation.
    /// </summary>
    public int? RoundtripCount { get; set; }

    /// <inheritdoc/>
    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(this.LineNumber), this.LineNumber);
        info.AddValue(nameof(this.Operation), this.Operation);
        info.AddValue(nameof(this.ToleranceValue), this.ToleranceValue);
        info.AddValue(nameof(this.ToleranceUnit), this.ToleranceUnit);
        info.AddValue(nameof(this.Direction), (int)this.Direction);
        info.AddValue(nameof(this.ExpectsFailure), this.ExpectsFailure);
        info.AddValue(nameof(this.ExpectedErrorCode), this.ExpectedErrorCode);

        info.AddValue("Accept.Length", this.Accept.Length);
        for (int i = 0; i < this.Accept.Length; i++)
        {
            info.AddValue($"Accept[{i}]", this.Accept[i]);
        }

        info.AddValue("Expect.Length", this.Expect.Length);
        for (int i = 0; i < this.Expect.Length; i++)
        {
            info.AddValue($"Expect[{i}]", this.Expect[i]);
        }

        info.AddValue("RoundtripCount.HasValue", this.RoundtripCount.HasValue);
        if (this.RoundtripCount.HasValue)
        {
            info.AddValue("RoundtripCount.Value", this.RoundtripCount.Value);
        }
    }

    /// <inheritdoc/>
    public void Deserialize(IXunitSerializationInfo info)
    {
        this.LineNumber = info.GetValue<int>(nameof(this.LineNumber));
        this.Operation = info.GetValue<string>(nameof(this.Operation)) ?? string.Empty;
        this.ToleranceValue = info.GetValue<double>(nameof(this.ToleranceValue));
        this.ToleranceUnit = info.GetValue<string>(nameof(this.ToleranceUnit)) ?? string.Empty;
        this.Direction = (GieDirection)info.GetValue<int>(nameof(this.Direction));
        this.ExpectsFailure = info.GetValue<bool>(nameof(this.ExpectsFailure));
        this.ExpectedErrorCode = info.GetValue<string>(nameof(this.ExpectedErrorCode)) ?? string.Empty;

        int acceptLength = info.GetValue<int>("Accept.Length");
        this.Accept = new double[acceptLength];
        for (int i = 0; i < acceptLength; i++)
        {
            this.Accept[i] = info.GetValue<double>($"Accept[{i}]");
        }

        int expectLength = info.GetValue<int>("Expect.Length");
        this.Expect = new double[expectLength];
        for (int i = 0; i < expectLength; i++)
        {
            this.Expect[i] = info.GetValue<double>($"Expect[{i}]");
        }

        if (info.GetValue<bool>("RoundtripCount.HasValue"))
        {
            this.RoundtripCount = info.GetValue<int>("RoundtripCount.Value");
        }
    }

    /// <inheritdoc/>
    public override string ToString() => $"L{this.LineNumber} {this.Direction} {this.Operation}";
}
