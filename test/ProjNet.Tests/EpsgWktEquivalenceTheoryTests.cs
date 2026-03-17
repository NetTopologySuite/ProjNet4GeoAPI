using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
using Xunit;

namespace ProjNET.Tests;

public class EpsgWktEquivalenceTheoryTests
{
    private const string FixtureRelativePath = "Generated/epsg-wkt-equivalence-fixture.json";

    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value));

    public static IEnumerable<object[]> EpsgFixtureRows()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, FixtureRelativePath.Replace('/', Path.DirectorySeparatorChar));
        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));

        var rows = document.RootElement
            .EnumerateArray()
            .Select(item => new
            {
                Srid = item.GetProperty("srid").GetInt32(),
                Wkt = item.GetProperty("wkt").GetString() ?? string.Empty,
            })
            .OrderBy(item => item.Srid)
            .Select(item => new object[] { item.Srid, item.Wkt })
            .ToArray();

        return rows;
    }

    [Theory]
    [MemberData(nameof(EpsgFixtureRows))]
    public void GeneratedCatalogWkt_ShouldBeEquivalentToCommittedEpsgFixture(int srid, string expectedWkt)
    {
        Assert.True(CatalogDefinitions.Value.TryGetValue(srid, out var generatedWkt), $"SRID {srid} not found in managed EPSG catalog.");
        Assert.True(AreEquivalent(expectedWkt, generatedWkt), $"WKT mismatch for SRID {srid}.");
    }

    private static bool AreEquivalent(string expectedWkt, string actualWkt)
    {
        if (string.Equals(Normalize(expectedWkt), Normalize(actualWkt), StringComparison.Ordinal))
        {
            return true;
        }

        var coordinateSystemFactory = new CoordinateSystemFactory();
        try
        {
            var expected = coordinateSystemFactory.CreateFromWkt(expectedWkt);
            var actual = coordinateSystemFactory.CreateFromWkt(actualWkt);
            return expected != null && actual != null && expected.EqualParams(actual);
        }
        catch
        {
            return false;
        }
    }

    private static string Normalize(string wkt) => string.Concat(wkt.Where(c => !char.IsWhiteSpace(c)));
}
