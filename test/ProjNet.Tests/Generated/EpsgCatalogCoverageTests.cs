// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests.Generated;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

/// <summary>
/// Verifies broad managed EPSG catalog instantiation coverage and representative structural invariants.
/// </summary>
public class EpsgCatalogCoverageTests
{
    private static readonly Dictionary<EpsgCoordinateSystemKind, int> ExpectedKindCounts =
        new Dictionary<EpsgCoordinateSystemKind, int>
        {
            [EpsgCoordinateSystemKind.Geographic2D] = 900,
            [EpsgCoordinateSystemKind.Geocentric] = 252,
            [EpsgCoordinateSystemKind.Projected] = 5398,
            [EpsgCoordinateSystemKind.Vertical] = 263,
            [EpsgCoordinateSystemKind.Compound] = 368,
        };

    private static readonly Lazy<List<CatalogCoordinateReference>> CatalogCoordinateReferences = new(GetCatalogCoordinateReferences);
    private static readonly Lazy<Dictionary<int, CoordinateSystem>> InstantiatedCoordinateSystems = new(GetInstantiatedCoordinateSystems);

    /// <summary>
    /// Verifies that the managed EPSG provider instantiates every coordinate reference in the generated catalog with the expected runtime type.
    /// </summary>
    [Fact]
    public void ManagedProviderShouldInstantiateEveryCatalogCoordinateReference()
    {
        Dictionary<int, CoordinateSystem> instantiated = InstantiatedCoordinateSystems.Value;
        List<CatalogCoordinateReference> catalog = CatalogCoordinateReferences.Value;
        List<string> failures = [];
        var instantiatedByKind = new Dictionary<EpsgCoordinateSystemKind, int>();

        foreach (CatalogCoordinateReference entry in catalog)
        {
            if (!instantiated.TryGetValue(entry.Srid, out CoordinateSystem? coordinateSystem))
            {
                failures.Add($"{entry.Srid} ({entry.Kind}) was not instantiated.");
                continue;
            }

            if (!CoordinateSystemMatchesKind(coordinateSystem, entry.Kind))
            {
                failures.Add($"{entry.Srid} expected {entry.Kind} but instantiated as {coordinateSystem.GetType().Name}.");
                continue;
            }

            instantiatedByKind[entry.Kind] = GetKindCount(instantiatedByKind, entry.Kind) + 1;
        }

        Assert.True(failures.Count == 0, BuildFailureMessage(failures));
        Assert.Equal(EpsgGeneratedCatalog.CoordinateReferenceCount, instantiated.Count);

        foreach (KeyValuePair<EpsgCoordinateSystemKind, int> expected in ExpectedKindCounts)
        {
            Assert.Equal(expected.Value, GetKindCount(instantiatedByKind, expected.Key));
        }
    }

    /// <summary>
    /// Verifies that all generated compound coordinate systems instantiate and retain the expected horizontal-plus-vertical structure.
    /// </summary>
    [Fact]
    public void ManagedProviderShouldInstantiateEveryCompoundCoordinateReferenceWithExpectedStructure()
    {
        Dictionary<int, CoordinateSystem> instantiated = InstantiatedCoordinateSystems.Value;
        var compounds = new List<(int Srid, CompoundCoordinateSystem CoordinateSystem)>();

        foreach (CatalogCoordinateReference entry in CatalogCoordinateReferences.Value.Where(entry => entry.Kind == EpsgCoordinateSystemKind.Compound))
        {
            Assert.True(instantiated.TryGetValue(entry.Srid, out CoordinateSystem? coordinateSystem), $"Compound SRID {entry.Srid} was not instantiated.");
            compounds.Add((entry.Srid, Assert.IsType<CompoundCoordinateSystem>(coordinateSystem)));
        }

        Assert.Equal(ExpectedKindCounts[EpsgCoordinateSystemKind.Compound], compounds.Count);

        Assert.All(compounds, compoundEntry =>
        {
            CompoundCoordinateSystem compound = compoundEntry.CoordinateSystem;
            VerticalCoordinateSystem vertical = Assert.IsType<VerticalCoordinateSystem>(compound.TailCoordinateSystem);

            Assert.Equal(compoundEntry.Srid, compound.AuthorityCode);
            Assert.True(compound.HeadCoordinateSystem.Dimension >= 2);
            Assert.Equal(1, vertical.Dimension);
            Assert.Equal(compound.HeadCoordinateSystem.Dimension + vertical.Dimension, compound.Dimension);
        });
    }

    /// <summary>
    /// Verifies that generated vertical CRS with a downward axis are classified as depth datums instead of generic geoid-model-derived datums.
    /// </summary>
    [Fact]
    public void ManagedProviderShouldClassifyDownAxisVerticalCoordinateSystemsAsDepth()
    {
        Dictionary<int, CoordinateSystem> instantiated = InstantiatedCoordinateSystems.Value;
        List<VerticalCoordinateSystem> downAxisVerticalCoordinateSystems = [];

        foreach (CatalogCoordinateReference entry in CatalogCoordinateReferences.Value.Where(entry => entry.Kind == EpsgCoordinateSystemKind.Vertical))
        {
            Assert.True(instantiated.TryGetValue(entry.Srid, out CoordinateSystem? coordinateSystem), $"Vertical SRID {entry.Srid} was not instantiated.");
            VerticalCoordinateSystem vertical = Assert.IsType<VerticalCoordinateSystem>(coordinateSystem);
            if (vertical.GetAxis(0).Orientation == AxisOrientationEnum.Down)
            {
                downAxisVerticalCoordinateSystems.Add(vertical);
            }
        }

        Assert.Equal(24, downAxisVerticalCoordinateSystems.Count);
        Assert.All(downAxisVerticalCoordinateSystems, coordinateSystem =>
            Assert.Equal(DatumType.VD_Depth, coordinateSystem.VerticalDatum.DatumType));
    }

    private static List<CatalogCoordinateReference> GetCatalogCoordinateReferences()
    {
        var result = new List<CatalogCoordinateReference>(EpsgGeneratedCatalog.CoordinateReferenceCount);
        for (int cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
        {
            if (!EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int srid))
            {
                throw new InvalidOperationException($"Catalog cache index {cacheIndex} did not map to an SRID.");
            }

            if (!EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out EpsgCoordinateReferenceRecord reference, out _))
            {
                throw new InvalidOperationException($"Catalog SRID {srid} could not be resolved back to a coordinate reference.");
            }

            result.Add(new CatalogCoordinateReference(srid, reference.Kind));
        }

        return result;
    }

    private static Dictionary<int, CoordinateSystem> GetInstantiatedCoordinateSystems()
    {
        return new ManagedCoordinateSystemDefinitionProvider()
            .GetCoordinateSystems()
            .ToDictionary(entry => entry.Srid, entry => entry.CoordinateSystem);
    }

    private static bool CoordinateSystemMatchesKind(CoordinateSystem coordinateSystem, EpsgCoordinateSystemKind kind)
    {
        return coordinateSystem switch
        {
            GeographicCoordinateSystem when kind == EpsgCoordinateSystemKind.Geographic2D => true,
            GeocentricCoordinateSystem when kind == EpsgCoordinateSystemKind.Geocentric => true,
            ProjectedCoordinateSystem when kind == EpsgCoordinateSystemKind.Projected => true,
            VerticalCoordinateSystem when kind == EpsgCoordinateSystemKind.Vertical => true,
            CompoundCoordinateSystem when kind == EpsgCoordinateSystemKind.Compound => true,
            _ => false,
        };
    }

    private static string BuildFailureMessage(List<string> failures)
    {
        const int failurePreviewLimit = 20;
        IEnumerable<string> preview = failures.Take(failurePreviewLimit);
        string suffix = failures.Count > failurePreviewLimit
            ? $"{Environment.NewLine}... and {failures.Count - failurePreviewLimit} more."
            : string.Empty;
        return $"Managed EPSG provider failed to instantiate {failures.Count} coordinate references:{Environment.NewLine}{string.Join(Environment.NewLine, preview)}{suffix}";
    }

    private static int GetKindCount(Dictionary<EpsgCoordinateSystemKind, int> counts, EpsgCoordinateSystemKind kind)
    {
        return counts.TryGetValue(kind, out int count) ? count : 0;
    }

    private readonly record struct CatalogCoordinateReference(int Srid, EpsgCoordinateSystemKind Kind);
}
