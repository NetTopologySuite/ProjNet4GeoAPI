// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

/// <summary>
/// Guards the intended sealed-versus-extendable split for concrete classes in the ProjNET assembly.
/// </summary>
public class SealedConsistencyTests
{
    private static readonly HashSet<string> ExtendableConcreteTypes =
    [
        "ProjNet.CoordinateSystemServices",
        "ProjNet.CoordinateSystems.AngularUnit",
        "ProjNet.CoordinateSystems.BoundCoordinateSystem",
        "ProjNet.CoordinateSystems.CompoundCoordinateSystem",
        "ProjNet.CoordinateSystems.CoordinateSystemFactory",
        "ProjNet.CoordinateSystems.Ellipsoid",
        "ProjNet.CoordinateSystems.FittedCoordinateSystem",
        "ProjNet.CoordinateSystems.GeocentricCoordinateSystem",
        "ProjNet.CoordinateSystems.GeographicCoordinateSystem",
        "ProjNet.CoordinateSystems.HorizontalDatum",
        "ProjNet.CoordinateSystems.LinearUnit",
        "ProjNet.CoordinateSystems.PrimeMeridian",
        "ProjNet.CoordinateSystems.ProjectedCoordinateSystem",
        "ProjNet.CoordinateSystems.Projection",
        "ProjNet.CoordinateSystems.Projections.AlbersProjection",
        "ProjNet.CoordinateSystems.Projections.BaconProjection",
        "ProjNet.CoordinateSystems.Projections.Eckert3Projection",
        "ProjNet.CoordinateSystems.Projections.GeneralSinusoidalProjection",
        "ProjNet.CoordinateSystems.Projections.HotineObliqueMercatorProjection",
        "ProjNet.CoordinateSystems.Projections.KrovakProjection",
        "ProjNet.CoordinateSystems.Projections.Mercator",
        "ProjNet.CoordinateSystems.Projections.MollweideProjection",
        "ProjNet.CoordinateSystems.Projections.PolarStereographicProjection",
        "ProjNet.CoordinateSystems.Projections.ProjectionsRegistry",
        "ProjNet.CoordinateSystems.Projections.PutninsP3Projection",
        "ProjNet.CoordinateSystems.Projections.PutninsP4PProjection",
        "ProjNet.CoordinateSystems.Projections.PutninsP5Projection",
        "ProjNet.CoordinateSystems.Projections.PutninsP6Projection",
        "ProjNet.CoordinateSystems.Projections.UrmaevFlatPolarSinusoidalProjection",
        "ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory",
        "ProjNet.CoordinateSystems.Unit",
        "ProjNet.CoordinateSystems.VerticalCoordinateSystem",
        "ProjNet.CoordinateSystems.VerticalDatum",
    ];

    /// <summary>
    /// Verifies that every concrete class in the production assembly is either sealed or explicitly allowlisted as intentionally extensible.
    /// </summary>
    [Fact]
    public void ConcreteClassesAreEitherSealedOrExplicitlyAllowlisted()
    {
        Assembly assembly = typeof(CoordinateSystemServices).Assembly;
        var concreteNonSealedClasses = assembly
            .GetTypes()
            .Where(type => type.IsClass)
            .Where(type => !type.IsAbstract && !type.IsSealed)
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Select(type => type.FullName)
            .OfType<string>()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var unexpectedConcreteNonSealedClasses = concreteNonSealedClasses
            .Where(name => !ExtendableConcreteTypes.Contains(name))
            .ToList();

        var staleAllowlistEntries = ExtendableConcreteTypes
            .Where(name => !concreteNonSealedClasses.Contains(name, StringComparer.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unexpectedConcreteNonSealedClasses.Count == 0,
            "Unexpected non-sealed concrete classes: " + string.Join(", ", unexpectedConcreteNonSealedClasses));
        Assert.True(
            staleAllowlistEntries.Count == 0,
            "Allowlist contains classes that are no longer concrete and non-sealed: " + string.Join(", ", staleAllowlistEntries));
    }
}
