// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Verifies that cloning well-known static coordinate-system model accessors does not mutate subsequent accessor results.
/// </summary>
public class StaticInstanceImmutabilityTests
{
    /// <summary>
    /// Verifies that representative static accessors stay unchanged after clone-style metadata updates.
    /// </summary>
    [Fact]
    public void StaticAccessors_RemainUnchangedAfterMetadataCloneOperations()
    {
        AssertCloneIsolation(
            () => Ellipsoid.WGS84,
            ellipsoid => ellipsoid.WithAuthority("TEST", 17030).WithName("Mutated ellipsoid"),
            "WGS 84",
            "EPSG",
            7030);
        AssertCloneIsolation(
            () => LinearUnit.Metre,
            unit => unit.WithAuthority("TEST", 19001).WithName("Mutated metre"),
            "metre",
            "EPSG",
            9001);
        AssertCloneIsolation(
            () => HorizontalDatum.WGS84,
            datum => datum.WithAuthority("TEST", 16326).WithName("Mutated datum"),
            "World Geodetic System 1984",
            "EPSG",
            6326);
        AssertCloneIsolation(
            () => GeographicCoordinateSystem.WGS84,
            coordinateSystem => coordinateSystem.WithAuthority("TEST", 14326).WithName("Mutated geographic CRS"),
            "WGS 84",
            "EPSG",
            4326);
        AssertCloneIsolation(
            () => VerticalCoordinateSystem.ODN,
            coordinateSystem => coordinateSystem.WithAuthority("TEST", 15701).WithName("Mutated vertical CRS"),
            "Newlyn",
            "EPSG",
            5701);
    }

    private static void AssertCloneIsolation<TInfo>(
        Func<TInfo> accessor,
        Func<TInfo, TInfo> cloneFactory,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode)
        where TInfo : Info
    {
        TInfo original = accessor();
        TInfo clone = cloneFactory(original);
        TInfo fresh = accessor();

        Assert.NotSame(original, clone);
        Assert.Equal(expectedName, original.Name);
        Assert.Equal(expectedAuthority, original.Authority);
        Assert.Equal(expectedAuthorityCode, original.AuthorityCode);
        Assert.Equal(expectedName, fresh.Name);
        Assert.Equal(expectedAuthority, fresh.Authority);
        Assert.Equal(expectedAuthorityCode, fresh.AuthorityCode);
        Assert.NotEqual(expectedName, clone.Name);
        Assert.NotEqual(expectedAuthority, clone.Authority);
        Assert.NotEqual(expectedAuthorityCode, clone.AuthorityCode);
    }
}
