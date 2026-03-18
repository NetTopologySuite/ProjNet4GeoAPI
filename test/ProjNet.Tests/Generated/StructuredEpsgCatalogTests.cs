namespace ProjNET.Tests.Generated;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

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
        bool found = EpsgGeneratedCatalog.TryGetCoordinateReference(4326, out var reference, out int cacheIndex);

        Assert.True(found);
        Assert.Equal(4326, reference.Srid);
        Assert.True(cacheIndex >= 0);
        Assert.True(EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int mappedSrid));
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

    [Fact]
    public void GeneratedCatalog_ShouldExposeExplicitOperationFastPath()
    {
        var explicitOperation = EpsgGeneratedCatalog.Operations.First(record =>
            record.MethodName.Contains("Geocentric translations", StringComparison.OrdinalIgnoreCase)
            || record.MethodName.Contains("Position Vector transformation", StringComparison.OrdinalIgnoreCase)
            || record.MethodName.Contains("Coordinate Frame rotation", StringComparison.OrdinalIgnoreCase));

        bool found = EpsgGeneratedCatalog.TryGetExplicitOperationParameters(explicitOperation.OperationCode, out var parameters);

        Assert.True(found);
        Assert.Equal(explicitOperation.OperationCode, parameters.OperationCode);
        Assert.False(double.IsNaN(parameters.Dx));
        Assert.False(double.IsNaN(parameters.Dy));
        Assert.False(double.IsNaN(parameters.Dz));
    }

    [Fact]
    public void GeneratedCatalog_ShouldReturnFalseForUnknownExplicitOperationCode()
    {
        bool found = EpsgGeneratedCatalog.TryGetExplicitOperationParameters(-1, out _);
        Assert.False(found);
    }
}
