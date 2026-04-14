// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using ProjNet.IO.CoordinateSystems;
using Xunit;

/// <summary>
/// Verifies normalization of projection parameter names imported from WKT2 and PROJJSON.
/// </summary>
public class ProjectionParameterNameNormalizerTests
{
    /// <summary>
    /// Verifies that known WKT2 parameter aliases map to the internal canonical names.
    /// </summary>
    /// <param name="parameterName">External parameter name.</param>
    /// <param name="expected">Expected internal canonical name.</param>
    [Theory]
    [InlineData("Longitude of natural origin", "central_meridian")]
    [InlineData("longitude-of-projection-centre", "central_meridian")]
    [InlineData("Latitude of 1st standard parallel", "standard_parallel_1")]
    [InlineData("Latitude of 2nd standard parallel", "standard_parallel_2")]
    [InlineData("Scale factor at projection centre", "scale_factor")]
    [InlineData("Azimuth of initial line", "azimuth")]
    [InlineData("Angle from rectified to skew grid", "rectified_grid_angle")]
    public void Normalize_WithKnownAliases_ReturnsCanonicalName(string parameterName, string expected)
    {
        string actual = ProjectionParameterNameNormalizer.Normalize(parameterName);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Verifies that unknown parameter names are still normalized deterministically.
    /// </summary>
    [Fact]
    public void Normalize_WithUnknownName_ReturnsNormalizedToken()
    {
        string actual = ProjectionParameterNameNormalizer.Normalize("Semi-major.axis/length");

        Assert.Equal("SEMI_MAJOR_AXIS_LENGTH", actual);
    }

    /// <summary>
    /// Verifies that the raw lookup-token normalization preserves the legacy single-pass underscore-collapse semantics.
    /// </summary>
    /// <param name="parameterName">External parameter name.</param>
    /// <param name="expected">Expected normalized lookup token.</param>
    [Theory]
    [InlineData("Semi-major.axis/length", "SEMI_MAJOR_AXIS_LENGTH")]
    [InlineData("Alpha__beta", "ALPHA_BETA")]
    [InlineData("Alpha___beta", "ALPHA__BETA")]
    [InlineData("  (Alpha) / beta  ", "ALPHA__BETA")]
    public void NormalizeLookupToken_WithMixedSeparators_ReturnsExpectedToken(string parameterName, string expected)
    {
        string actual = ProjectionParameterNameNormalizer.NormalizeLookupToken(parameterName);

        Assert.Equal(expected, actual);
    }

    /// <summary>
     /// Verifies that empty and whitespace-only names normalize to an empty string.
     /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_WithEmptyOrWhitespace_ReturnsEmptyString(string parameterName)
    {
        string actual = ProjectionParameterNameNormalizer.Normalize(parameterName);

        Assert.Equal(string.Empty, actual);
    }
}
