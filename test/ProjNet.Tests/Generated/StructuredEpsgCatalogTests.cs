// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests.Generated;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class StructuredEpsgCatalogTests
{
    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldNotExposeExplicitStringPool()
    {
        var stringPoolField = typeof(EpsgGeneratedCatalog).GetField("StringPool", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(stringPoolField);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldExposeSwitchMappedSridLookup()
    {
        bool found = EpsgGeneratedCatalog.TryGetCoordinateReference(4326, out var reference, out int cacheIndex);

        Assert.True(found);
        Assert.Equal(4326, reference.Srid);
        Assert.True(cacheIndex >= 0);
        Assert.True(EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int mappedSrid));
        Assert.Equal(4326, mappedSrid);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldNotExposeSridArray()
    {
        var sridArrayField = typeof(EpsgGeneratedCatalog).GetField("CoordinateReferenceSrids", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(sridArrayField);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CoordinateSystemFactoryShouldNotUseDictionaryLookupCaches()
    {
        var dictionaryFields = typeof(EpsgCoordinateSystemFactory)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            .ToList();

        Assert.Empty(dictionaryFields);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void ManagedProviderShouldExposeStructuredCoordinateSystems()
    {
        var provider = new ManagedCoordinateSystemDefinitionProvider();
        var managedProvider = Assert.IsAssignableFrom<IManagedCoordinateSystemProvider>(provider);

        var coordinateSystems = managedProvider.GetCoordinateSystems().ToList();
        Assert.True(coordinateSystems.Count > 7000);
        Assert.Contains(coordinateSystems, item => item.Srid == 4326);
        Assert.Contains(coordinateSystems, item => item.Srid == 3857);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldNotExposeConversionAndExplicitOperationArrays()
    {
        var conversionsField = typeof(EpsgGeneratedCatalog).GetField("Conversions", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var conversionParametersField = typeof(EpsgGeneratedCatalog).GetField("ConversionParameters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var explicitOperationsField = typeof(EpsgGeneratedCatalog).GetField("ExplicitOperations", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.Null(conversionsField);
        Assert.Null(conversionParametersField);
        Assert.Null(explicitOperationsField);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldExposeSwitchBasedConversionLookup()
    {
        bool hasReference = EpsgGeneratedCatalog.TryGetCoordinateReference(3857, out var reference, out _);
        Assert.True(hasReference);
        Assert.Equal(2, (int)reference.Kind);

        bool hasProjectedRecord = EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out var projectedRecord);
        Assert.True(hasProjectedRecord);

        bool hasConversion = EpsgGeneratedCatalog.TryGetConversion(projectedRecord.ConversionCode, out var conversion);
        Assert.True(hasConversion);
        Assert.True(conversion.ParameterCount > 0);

        for (int i = 0; i < conversion.ParameterCount; i++)
        {
            bool hasParameter = EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, i, out var parameter);
            Assert.True(hasParameter);
            Assert.False(string.IsNullOrWhiteSpace(parameter.Name));
        }

        Assert.False(EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, conversion.ParameterCount, out _));
        Assert.False(EpsgGeneratedCatalog.TryGetConversion(-1, out _));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogProjectedConversionsShouldBeConsistentAcrossCatalog()
    {
        for (int cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
        {
            Assert.True(EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int srid));
            Assert.True(EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out var reference, out _));

            if (reference.Kind != EpsgCoordinateSystemKind.Projected)
            {
                continue;
            }

            Assert.True(EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out var projectedRecord));
            Assert.True(EpsgGeneratedCatalog.TryGetConversion(projectedRecord.ConversionCode, out var conversion));

            for (int parameterIndex = 0; parameterIndex < conversion.ParameterCount; parameterIndex++)
            {
                Assert.True(EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, parameterIndex, out var parameter));
                Assert.False(string.IsNullOrWhiteSpace(parameter.Name));
            }

            Assert.False(EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, conversion.ParameterCount, out _));
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldExposeExplicitOperationFastPath()
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

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldReturnFalseForUnknownExplicitOperationCode()
    {
        bool found = EpsgGeneratedCatalog.TryGetExplicitOperationParameters(-1, out _);
        Assert.False(found);
    }
}
