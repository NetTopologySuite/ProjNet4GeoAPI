// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for <see cref="TransformationMath"/> static helper methods.
/// </summary>
public class TransformationMathTests
{
    // ---- IsFinite ----

    /// <summary>
    /// Verifies that finite values are recognized.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.Epsilon)]
    public void IsFinite_FiniteValue_ReturnsTrue(double value)
    {
        Assert.True(TransformationMath.IsFinite(value));
    }

    /// <summary>
    /// Verifies that NaN is not finite.
    /// </summary>
    [Fact]
    public void IsFinite_NaN_ReturnsFalse()
    {
        Assert.False(TransformationMath.IsFinite(double.NaN));
    }

    /// <summary>
    /// Verifies that positive infinity is not finite.
    /// </summary>
    [Fact]
    public void IsFinite_PositiveInfinity_ReturnsFalse()
    {
        Assert.False(TransformationMath.IsFinite(double.PositiveInfinity));
    }

    /// <summary>
    /// Verifies that negative infinity is not finite.
    /// </summary>
    [Fact]
    public void IsFinite_NegativeInfinity_ReturnsFalse()
    {
        Assert.False(TransformationMath.IsFinite(double.NegativeInfinity));
    }

    // ---- IsValidObservationEpoch ----

    /// <summary>
    /// Verifies that a normal finite epoch that differs from the sentinel is valid.
    /// </summary>
    [Fact]
    public void IsValidObservationEpoch_ValidEpoch_ReturnsTrue()
    {
        Assert.True(TransformationMath.IsValidObservationEpoch(2020.5, 0.0));
    }

    /// <summary>
    /// Verifies that NaN is not a valid observation epoch.
    /// </summary>
    [Fact]
    public void IsValidObservationEpoch_NaN_ReturnsFalse()
    {
        Assert.False(TransformationMath.IsValidObservationEpoch(double.NaN, 0.0));
    }

    /// <summary>
    /// Verifies that infinity is not a valid observation epoch.
    /// </summary>
    [Fact]
    public void IsValidObservationEpoch_Infinity_ReturnsFalse()
    {
        Assert.False(TransformationMath.IsValidObservationEpoch(double.PositiveInfinity, 0.0));
    }

    /// <summary>
    /// Verifies that the missing sentinel value is not a valid observation epoch.
    /// </summary>
    [Fact]
    public void IsValidObservationEpoch_MissingSentinel_ReturnsFalse()
    {
        const double sentinel = -999.0;

        Assert.False(TransformationMath.IsValidObservationEpoch(sentinel, sentinel));
    }

    /// <summary>
    /// Verifies that zero is valid when the sentinel is a different value.
    /// </summary>
    [Fact]
    public void IsValidObservationEpoch_ZeroWithNonZeroSentinel_ReturnsTrue()
    {
        Assert.True(TransformationMath.IsValidObservationEpoch(0.0, -1.0));
    }

    /// <summary>
    /// Verifies that zero is invalid when the sentinel is also zero.
    /// </summary>
    [Fact]
    public void IsValidObservationEpoch_ZeroWithZeroSentinel_ReturnsFalse()
    {
        Assert.False(TransformationMath.IsValidObservationEpoch(0.0, 0.0));
    }

    // ---- NormalizeLongitudeDegrees ----

    /// <summary>
    /// Verifies that values within [-180, 180] are returned unchanged.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(45.0)]
    [InlineData(-45.0)]
    [InlineData(180.0)]
    [InlineData(-180.0)]
    [InlineData(90.0)]
    public void NormalizeLongitudeDegrees_InRange_ReturnsUnchanged(double longitude)
    {
        Assert.Equal(longitude, TransformationMath.NormalizeLongitudeDegrees(longitude), 12);
    }

    /// <summary>
    /// Verifies normalization of positive values exceeding 180.
    /// </summary>
    [Theory]
    [InlineData(270.0, -90.0)]
    [InlineData(360.0, 0.0)]
    [InlineData(540.0, 180.0)]
    [InlineData(181.0, -179.0)]
    public void NormalizeLongitudeDegrees_PositiveExcess_NormalizesCorrectly(double input, double expected)
    {
        Assert.Equal(expected, TransformationMath.NormalizeLongitudeDegrees(input), 12);
    }

    /// <summary>
    /// Verifies normalization of negative values below -180.
    /// </summary>
    [Theory]
    [InlineData(-270.0, 90.0)]
    [InlineData(-360.0, 0.0)]
    [InlineData(-540.0, -180.0)]
    [InlineData(-181.0, 179.0)]
    public void NormalizeLongitudeDegrees_NegativeExcess_NormalizesCorrectly(double input, double expected)
    {
        Assert.Equal(expected, TransformationMath.NormalizeLongitudeDegrees(input), 12);
    }
}
