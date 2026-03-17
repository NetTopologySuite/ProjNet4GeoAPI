using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

namespace ProjNET.Tests.Generated;

public class StructuredEpsgCatalogTests
{
    [Fact]
    public void GeneratedCatalog_ShouldNotExposeExplicitStringPool()
    {
        var stringPoolField = typeof(EpsgGeneratedCatalog).GetField("StringPool", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(stringPoolField);
    }

    [Fact]
    public void GeneratedCatalog_ShouldExposeSwitchMappedSridLookup()
    {
        var found = EpsgGeneratedCatalog.TryGetCoordinateReference(4326, out var reference, out var cacheIndex);

        Assert.True(found);
        Assert.Equal(4326, reference.Srid);
        Assert.True(cacheIndex >= 0);
        Assert.True(EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out var mappedSrid));
        Assert.Equal(4326, mappedSrid);
    }

    [Fact]
    public void GeneratedCatalog_ShouldNotExposeSridArray()
    {
        var sridArrayField = typeof(EpsgGeneratedCatalog).GetField("CoordinateReferenceSrids", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(sridArrayField);
    }

    [Fact]
    public void CoordinateSystemFactory_ShouldNotUseDictionaryLookupCaches()
    {
        var dictionaryFields = typeof(EpsgCoordinateSystemFactory)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            .ToList();

        Assert.Empty(dictionaryFields);
    }

    [Fact]
    public void ManagedProvider_ShouldExposeStructuredCoordinateSystems()
    {
        var provider = new ManagedCoordinateSystemDefinitionProvider();
        var managedProvider = Assert.IsAssignableFrom<IManagedCoordinateSystemProvider>(provider);

        var coordinateSystems = managedProvider.GetCoordinateSystems().ToList();
        Assert.True(coordinateSystems.Count > 7000);
        Assert.Contains(coordinateSystems, item => item.Key == 4326);
        Assert.Contains(coordinateSystems, item => item.Key == 3857);
    }
}
