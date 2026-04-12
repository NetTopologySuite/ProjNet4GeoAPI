// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

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
