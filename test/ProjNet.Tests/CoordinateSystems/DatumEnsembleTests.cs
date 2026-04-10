// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for <see cref="DatumEnsemble"/> and <see cref="DatumEnsembleMember"/>.
/// </summary>
public class DatumEnsembleTests
{
    /// <summary>
    /// Verifies the constructor stores the supplied members, accuracy, ellipsoid, and identifier.
    /// </summary>
    [Fact]
    public void Constructor_SetsMembersAccuracyEllipsoidAndIdentifier()
    {
        DatumEnsembleMember[] members =
        [
            new DatumEnsembleMember("World Geodetic System 1984 (Transit)", "EPSG", 1166),
            new DatumEnsembleMember("World Geodetic System 1984 (G730)", "EPSG", 1152),
        ];

        var ensemble = new DatumEnsemble(
            "World Geodetic System 1984 ensemble",
            members,
            2d,
            Ellipsoid.WGS84,
            "EPSG",
            6326);

        Assert.Equal("World Geodetic System 1984 ensemble", ensemble.Name);
        Assert.Equal(2d, ensemble.Accuracy);
        Assert.True(Assert.IsType<Ellipsoid>(ensemble.Ellipsoid).EqualParams(Ellipsoid.WGS84));
        Assert.Equal("EPSG", ensemble.Authority);
        Assert.Equal(6326, ensemble.AuthorityCode);
        Assert.Equal(2, ensemble.Members.Count);
        Assert.Equal("World Geodetic System 1984 (Transit)", ensemble.Members[0].Name);
    }

    /// <summary>
    /// Verifies empty member lists are rejected.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyMembers_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new DatumEnsemble("Invalid", [], 1d));
    }

    /// <summary>
    /// Verifies equivalent ensembles compare equal.
    /// </summary>
    [Fact]
    public void Equals_WithEquivalentValues_ReturnsTrue()
    {
        var left = new DatumEnsemble(
            "European Terrestrial Reference System 1989 ensemble",
            [new DatumEnsembleMember("ETRF89", "EPSG", 1178)],
            0.1d,
            Ellipsoid.GRS80,
            "EPSG",
            6258);
        var right = new DatumEnsemble(
            "European Terrestrial Reference System 1989 ensemble",
            [new DatumEnsembleMember("ETRF89", "EPSG", 1178)],
            0.1d,
            Ellipsoid.GRS80,
            "EPSG",
            6258);

        Assert.True(left.Equals(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies accuracy changes produce different ensembles.
    /// </summary>
    [Fact]
    public void Equals_WithDifferentAccuracy_ReturnsFalse()
    {
        var left = new DatumEnsemble("Vertical ensemble", [new DatumEnsembleMember("A")], 0.02d);
        var right = new DatumEnsemble("Vertical ensemble", [new DatumEnsembleMember("A")], 0.05d);

        Assert.False(left.Equals(right));
    }

    /// <summary>
    /// Verifies member equality uses name and identifier.
    /// </summary>
    [Fact]
    public void MemberEquals_WithDifferentIdentifier_ReturnsFalse()
    {
        var left = new DatumEnsembleMember("World Geodetic System 1984 (Transit)", "EPSG", 1166);
        var right = new DatumEnsembleMember("World Geodetic System 1984 (Transit)", "EPSG", 1152);

        Assert.False(left.Equals(right));
    }
}
