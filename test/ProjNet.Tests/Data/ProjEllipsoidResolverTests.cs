// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies the shared PROJ ellipsoid-resolution helpers used by runtime transformation argument parsing.
/// </summary>
public class ProjEllipsoidResolverTests
{
    /// <summary>
    /// Verifies optional ellipsoid resolution falls back to WGS84 when no ellipsoid tokens are supplied.
    /// </summary>
    [Fact]
    public void TryResolveEllipsoidOrDefault_NoDefinition_UsesWgs84()
    {
        bool resolved = ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            new Dictionary<string, string>(),
            includeDatumToken: true,
            allowClarke1880Ign: false,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor);

        Assert.True(resolved);
        Assert.Equal(Ellipsoid.WGS84.SemiMajorAxis, semiMajor, 9);
        Assert.Equal(Ellipsoid.WGS84.SemiMinorAxis, semiMinor, 9);
    }

    /// <summary>
    /// Verifies invalid explicit-axis tokens are ignored by the lenient optional resolver and therefore fall back to WGS84.
    /// </summary>
    [Fact]
    public void TryResolveEllipsoidOrDefault_InvalidExplicitAxis_DefaultsToWgs84()
    {
        var args = new Dictionary<string, string>
        {
            ["a"] = "not-a-number",
        };

        bool resolved = ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            includeDatumToken: true,
            allowClarke1880Ign: false,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor);

        Assert.True(resolved);
        Assert.Equal(Ellipsoid.WGS84.SemiMajorAxis, semiMajor, 9);
        Assert.Equal(Ellipsoid.WGS84.SemiMinorAxis, semiMinor, 9);
    }

    /// <summary>
    /// Verifies unsupported named ellipsoids still fail the lenient optional resolver.
    /// </summary>
    [Fact]
    public void TryResolveEllipsoidOrDefault_UnsupportedNamedEllipsoid_ReturnsFalse()
    {
        var args = new Dictionary<string, string>
        {
            ["ellps"] = "unknown",
        };

        bool resolved = ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            includeDatumToken: true,
            allowClarke1880Ign: false,
            allowBessel: false,
            out _,
            out _);

        Assert.False(resolved);
    }

    /// <summary>
    /// Verifies known Airy and Bessel tokens resolve through the shared ellipsoid accessors.
    /// </summary>
    /// <param name="token">The PROJ ellipsoid token.</param>
    /// <param name="allowBessel"><see langword="true"/> when the resolver should accept Bessel tokens.</param>
    /// <param name="expectedAccessorName">The expected <see cref="Ellipsoid"/> accessor name.</param>
    [Theory]
    [InlineData("airy", false, nameof(Ellipsoid.Airy1830))]
    [InlineData("osgb36", false, nameof(Ellipsoid.Airy1830))]
    [InlineData("bessel", true, nameof(Ellipsoid.Bessel1841))]
    [InlineData("potsdam", true, nameof(Ellipsoid.Bessel1841))]
    public void TryResolveKnownEllipsoid_KnownAccessorBackedTokens_UseEllipsoidStatics(
        string token,
        bool allowBessel,
        string expectedAccessorName)
    {
        bool resolved = ProjEllipsoidResolver.TryResolveKnownEllipsoid(
            token,
            allowClarke1880Ign: false,
            allowBessel,
            out double semiMajor,
            out double semiMinor);

        Ellipsoid expectedEllipsoid = expectedAccessorName switch
        {
            nameof(Ellipsoid.Airy1830) => Ellipsoid.Airy1830,
            nameof(Ellipsoid.Bessel1841) => Ellipsoid.Bessel1841,
            _ => throw new InvalidOperationException($"Unexpected ellipsoid accessor '{expectedAccessorName}'."),
        };

        Assert.True(resolved);
        Assert.Equal(expectedEllipsoid.SemiMajorAxis, semiMajor, 12);
        Assert.Equal(expectedEllipsoid.SemiMinorAxis, semiMinor, 12);
    }

    /// <summary>
    /// Verifies the diagnostic overload reports operation-specific messages for non-positive explicit axes.
    /// </summary>
    [Fact]
    public void TryResolveEllipsoidOrDefault_DiagnosticNonPositiveAxis_ReturnsOperationSpecificReason()
    {
        var args = new Dictionary<string, string>
        {
            ["a"] = "0",
        };

        bool resolved = ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            operationName: "defmodel",
            allowClarke1880Ign: true,
            allowBessel: false,
            out _,
            out _,
            out string? skipReason);

        Assert.False(resolved);
        Assert.Equal("defmodel +a must be positive.", skipReason);
    }

    /// <summary>
    /// Verifies the required resolver honors named-ellipsoid lookup, semi-major overrides, and explicit shape overrides in PROJ order.
    /// </summary>
    [Fact]
    public void TryResolveRequiredEllipsoidWithOverrides_NamedEllipsoidAndOverrides_AppliesProjOrder()
    {
        var args = new Dictionary<string, string>
        {
            ["ellps"] = "grs80",
            ["a"] = "7000000",
            ["rf"] = "2",
        };

        bool resolved = ProjEllipsoidResolver.TryResolveRequiredEllipsoidWithOverrides(
            args,
            operationName: "xyzgridshift",
            allowClarke1880Ign: true,
            allowBessel: true,
            out double semiMajor,
            out double semiMinor,
            out string? skipReason);

        Assert.True(resolved);
        Assert.Null(skipReason);
        Assert.Equal(7000000d, semiMajor, 9);
        Assert.Equal(3500000d, semiMinor, 9);
    }

    /// <summary>
    /// Verifies the required resolver reports the operation-specific missing-definition message when no ellipsoid definition exists.
    /// </summary>
    [Fact]
    public void TryResolveRequiredEllipsoidWithOverrides_MissingDefinition_ReturnsOperationSpecificReason()
    {
        var args = new Dictionary<string, string>
        {
            ["rf"] = "298.257223563",
        };

        bool resolved = ProjEllipsoidResolver.TryResolveRequiredEllipsoidWithOverrides(
            args,
            operationName: "xyzgridshift",
            allowClarke1880Ign: true,
            allowBessel: true,
            out _,
            out _,
            out string? skipReason);

        Assert.False(resolved);
        Assert.Equal("xyzgridshift requires ellipsoid definition (+ellps, +datum, +r, or +a with optional +b/+rf/+f/+es).", skipReason);
    }
}
