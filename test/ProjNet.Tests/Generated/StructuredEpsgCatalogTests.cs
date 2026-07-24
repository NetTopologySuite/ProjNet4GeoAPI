// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests.Generated;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

/// <summary>
/// Tests for the structured EPSG catalog, covering generated lookup behaviour, cache layout, and provider contracts.
/// </summary>
public class StructuredEpsgCatalogTests
{
    /// <summary>
    /// Verifies that <c>EpsgGeneratedCatalog</c> does not expose a <c>StringPool</c> field.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldNotExposeExplicitStringPool()
    {
        FieldInfo? stringPoolField = typeof(EpsgGeneratedCatalog).GetField("StringPool", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(stringPoolField);
    }

    /// <summary>
    /// Verifies that <c>EpsgGeneratedCatalog</c> resolves SRID 4326 through a switch-mapped lookup and that the returned cache index round-trips correctly.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldExposeSwitchMappedSridLookup()
    {
        bool found = EpsgGeneratedCatalog.TryGetCoordinateReference(4326, out EpsgCoordinateReferenceRecord reference, out int cacheIndex);

        Assert.True(found);
        Assert.Equal(4326, reference.Srid);
        Assert.True(cacheIndex >= 0);
        Assert.True(EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int mappedSrid));
        Assert.Equal(4326, mappedSrid);
    }

    /// <summary>
    /// Verifies that <c>EpsgGeneratedCatalog</c> does not expose a <c>CoordinateReferenceSrids</c> array field.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldNotExposeSridArray()
    {
        FieldInfo? sridArrayField = typeof(EpsgGeneratedCatalog).GetField("CoordinateReferenceSrids", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.Null(sridArrayField);
    }

    /// <summary>
    /// Verifies that <c>EpsgCoordinateSystemFactory</c> holds no static <c>Dictionary&lt;,&gt;</c> fields, confirming it does not rely on dictionary-based lookup caches.
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
    /// Verifies that <c>ManagedCoordinateSystemDefinitionProvider</c> implements <c>IManagedCoordinateSystemProvider</c> and exposes more than 7,000 coordinate systems, including SRID 4326 and 3857.
    /// </summary>
    [Fact]
    public void ManagedProviderShouldExposeStructuredCoordinateSystems()
    {
        var provider = new ManagedCoordinateSystemDefinitionProvider();
        IManagedCoordinateSystemProvider managedProvider = Assert.IsType<IManagedCoordinateSystemProvider>(provider, exactMatch: false);

        var coordinateSystems = managedProvider.GetCoordinateSystems().ToList();
        Assert.True(coordinateSystems.Count > 7000);
        Assert.Contains(coordinateSystems, item => item.Srid == 4326);
        Assert.Contains(coordinateSystems, item => item.Srid == 3857);
    }

    /// <summary>
    /// Verifies that <c>EpsgGeneratedCatalog</c> does not expose <c>Conversions</c>, <c>ConversionParameters</c>, or <c>ExplicitOperations</c> array fields.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldNotExposeConversionAndExplicitOperationArrays()
    {
        FieldInfo? conversionsField = typeof(EpsgGeneratedCatalog).GetField("Conversions", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? conversionParametersField = typeof(EpsgGeneratedCatalog).GetField("ConversionParameters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? explicitOperationsField = typeof(EpsgGeneratedCatalog).GetField("ExplicitOperations", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.Null(conversionsField);
        Assert.Null(conversionParametersField);
        Assert.Null(explicitOperationsField);
    }

    /// <summary>
    /// Verifies that operation arrays and explicit-operation lookup moved to <c>EpsgGeneratedOperationsCatalog</c> rather than living on <c>EpsgGeneratedCatalog</c>.
    /// </summary>
    [Fact]
    public void GeneratedOperationArtifactsShouldLiveInDedicatedOperationsCatalog()
    {
        FieldInfo? catalogOperationsField = typeof(EpsgGeneratedCatalog).GetField("Operations", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? catalogOperationParametersField = typeof(EpsgGeneratedCatalog).GetField("OperationParameters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo? catalogExplicitMethod = typeof(EpsgGeneratedCatalog).GetMethod("TryGetExplicitOperationParameters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? dedicatedOperationsField = typeof(EpsgGeneratedOperationsCatalog).GetField("Operations", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        FieldInfo? dedicatedOperationParametersField = typeof(EpsgGeneratedOperationsCatalog).GetField("OperationParameters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo? dedicatedExplicitMethod = typeof(EpsgGeneratedOperationsCatalog).GetMethod("TryGetExplicitOperationParameters", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.Null(catalogOperationsField);
        Assert.Null(catalogOperationParametersField);
        Assert.Null(catalogExplicitMethod);
        Assert.NotNull(dedicatedOperationsField);
        Assert.NotNull(dedicatedOperationParametersField);
        Assert.NotNull(dedicatedExplicitMethod);
    }

    /// <summary>
    /// Verifies that <c>EpsgGeneratedCatalog</c> resolves the projected CRS for SRID 3857 through a switch-based conversion lookup and that all associated conversion parameters are present and named.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldExposeSwitchBasedConversionLookup()
    {
        bool hasReference = EpsgGeneratedCatalog.TryGetCoordinateReference(3857, out EpsgCoordinateReferenceRecord reference, out _);
        Assert.True(hasReference);
        Assert.Equal(2, (int)reference.Kind);

        bool hasProjectedRecord = EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out EpsgProjectedCrsRecord projectedRecord);
        Assert.True(hasProjectedRecord);

        bool hasConversion = EpsgGeneratedCatalog.TryGetConversion(projectedRecord.ConversionCode, out EpsgConversionRecord conversion);
        Assert.True(hasConversion);
        Assert.True(conversion.ParameterCount > 0);

        for (int i = 0; i < conversion.ParameterCount; i++)
        {
            bool hasParameter = EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, i, out EpsgConversionParameterRecord parameter);
            Assert.True(hasParameter);
            Assert.False(string.IsNullOrWhiteSpace(parameter.Name));
        }

        Assert.False(EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, conversion.ParameterCount, out _));
        Assert.False(EpsgGeneratedCatalog.TryGetConversion(-1, out _));
    }

    /// <summary>
    /// Verifies that every projected coordinate reference in the catalog has a consistent conversion record with valid, named parameters.
    /// </summary>
    [Fact]
    public void GeneratedCatalogProjectedConversionsShouldBeConsistentAcrossCatalog()
    {
        for (int cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
        {
            Assert.True(EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int srid));
            Assert.True(EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out EpsgCoordinateReferenceRecord reference, out _));

            if (reference.Kind != EpsgCoordinateSystemKind.Projected)
            {
                continue;
            }

            Assert.True(EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out EpsgProjectedCrsRecord projectedRecord));
            Assert.True(EpsgGeneratedCatalog.TryGetConversion(projectedRecord.ConversionCode, out EpsgConversionRecord conversion));

            for (int parameterIndex = 0; parameterIndex < conversion.ParameterCount; parameterIndex++)
            {
                Assert.True(EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, parameterIndex, out EpsgConversionParameterRecord parameter));
                Assert.False(string.IsNullOrWhiteSpace(parameter.Name));
            }

            Assert.False(EpsgGeneratedCatalog.TryGetConversionParameter(projectedRecord.ConversionCode, conversion.ParameterCount, out _));
        }
    }

    /// <summary>
    /// Verifies that <c>EpsgGeneratedCatalog</c> resolves explicit operation parameters via a fast-path lookup and that the returned translation values are valid numbers.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldExposeExplicitOperationFastPath()
    {
        EpsgOperationRecord explicitOperation = EpsgGeneratedOperationsCatalog.Operations.First(record =>
            record.MethodName.Contains("Geocentric translations", StringComparison.OrdinalIgnoreCase)
            || record.MethodName.Contains("Position Vector transformation", StringComparison.OrdinalIgnoreCase)
            || record.MethodName.Contains("Coordinate Frame rotation", StringComparison.OrdinalIgnoreCase));

        bool found = EpsgGeneratedOperationsCatalog.TryGetExplicitOperationParameters(explicitOperation.OperationCode, out EpsgExplicitOperationRecord parameters);

        Assert.True(found);
        Assert.Equal(explicitOperation.OperationCode, parameters.OperationCode);
        Assert.False(double.IsNaN(parameters.Dx));
        Assert.False(double.IsNaN(parameters.Dy));
        Assert.False(double.IsNaN(parameters.Dz));
    }

    /// <summary>
    /// Verifies that concatenated operations do not carry a synthetic method name from their first sub-step.
    /// </summary>
    [Fact]
    public void GeneratedOperationsCatalogShouldLeaveConcatenatedMethodNamesEmpty()
    {
        EpsgOperationRecord concatenatedOperation = EpsgGeneratedOperationsCatalog.Operations.First(record => record.OperationType == EpsgOperationType.ConcatenatedOperation);

        Assert.True(string.IsNullOrEmpty(concatenatedOperation.MethodName));
    }

    /// <summary>
    /// Verifies that <c>TryGetExplicitOperationParameters</c> returns <see langword="false"/> for an unknown operation code.
    /// </summary>
    [Fact]
    public void GeneratedCatalogShouldReturnFalseForUnknownExplicitOperationCode()
    {
        bool found = EpsgGeneratedOperationsCatalog.TryGetExplicitOperationParameters(-1, out _);
        Assert.False(found);
    }
}
