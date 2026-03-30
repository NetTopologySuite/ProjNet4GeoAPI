// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using ProjNet.Data.Generated;
using Xunit;

/// <summary>
/// Tests for the coordinate operation resolution engine and transformation factory.
/// </summary>
public class OperationResolutionEngineTests
{
    private static readonly double[] GeographicSamplePoint = [13.1234d, 52.9876d];
    private static readonly double[] UtmSamplePoint = [500000d, 4649776.22482d];

    private readonly CoordinateTransformationFactory coordinateTransformationFactory = new();
    private readonly CoordinateSystemFactory coordinateSystemFactory = new();

    /// <summary>
    /// Verifies that creating a transformation between identical projected coordinate systems produces an identity transform.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithSameProjectedCoordinateSystemUsesIdentityTransform()
    {
        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, source);
        double[] output = transformation.MathTransform.Transform(UtmSamplePoint);

        Assert.True(transformation.MathTransform.Identity());
        Assert.Equal(500000d, output[0], 12);
        Assert.Equal(4649776.22482d, output[1], 12);
    }

    /// <summary>
    /// Verifies that creating a transformation between equivalent geographic coordinate systems parsed from WKT produces an identity transform.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithEquivalentGeographicCoordinateSystemsUsesIdentityTransform()
    {
        GeographicCoordinateSystem source = GeographicCoordinateSystem.WGS84;
        GeographicCoordinateSystem target = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(this.coordinateSystemFactory, source.WKT);
        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] output = transformation.MathTransform.Transform(GeographicSamplePoint);

        Assert.True(transformation.MathTransform.Identity());
        Assert.Equal(13.1234d, output[0], 12);
        Assert.Equal(52.9876d, output[1], 12);
    }

    /// <summary>
    /// Verifies that when projected coordinate systems carry EPSG authority codes, the engine selects the highest-ranked catalogued operation over the fallback path.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithProjectedPairHavingDirectMetadataPrefersMetadataCandidate()
    {
        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        source.Authority = "EPSG";
        source.AuthorityCode = 28992;
        target.Authority = "EPSG";
        target.AuthorityCode = 23031;

        ICoordinateTransformation metadataTransformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] metadataOutput = metadataTransformation.MathTransform.Transform(UtmSamplePoint);

        var fallbackSource = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var fallbackTarget = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        fallbackSource.Authority = string.Empty;
        fallbackSource.AuthorityCode = -1;
        fallbackTarget.Authority = string.Empty;
        fallbackTarget.AuthorityCode = -1;
        ICoordinateTransformation fallbackTransformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(fallbackSource, fallbackTarget);
        double[] fallbackOutput = fallbackTransformation.MathTransform.Transform(UtmSamplePoint);

        Assert.Equal("EPSG", metadataTransformation.Authority);
        Assert.Equal(1044, metadataTransformation.AuthorityCode);
        Assert.Equal(fallbackOutput[0], metadataOutput[0], 9);
        Assert.Equal(fallbackOutput[1], metadataOutput[1], 9);

        ConcatenatedTransform concatenated = Assert.IsType<ConcatenatedTransform>(metadataTransformation.MathTransform);
        Assert.Equal(2, concatenated.CoordinateTransformationList.Count);
        Assert.DoesNotContain(concatenated.CoordinateTransformationList, ContainsGeographicOrGeocentricCoordinateSystem);
    }

    /// <summary>
    /// Verifies that projected coordinate systems without EPSG authority codes produce a direct projected-to-projected transformation without intermediate geographic steps.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithProjectedFallbackPairUsesDirectProj2ProjCorePath()
    {
        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        source.Authority = string.Empty;
        source.AuthorityCode = -1;
        target.Authority = string.Empty;
        target.AuthorityCode = -1;

        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        ConcatenatedTransform concatenated = Assert.IsType<ConcatenatedTransform>(transformation.MathTransform);
        Assert.Equal(2, concatenated.CoordinateTransformationList.Count);
        Assert.DoesNotContain(concatenated.CoordinateTransformationList, ContainsGeographicOrGeocentricCoordinateSystem);
    }

    /// <summary>
    /// Verifies that projected coordinate systems without an EPSG authority fall back to a transformation with an empty authority and a code of -1.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithProjectedPairWithoutEpsgAuthorityUsesLegacyFallback()
    {
        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        source.Authority = string.Empty;
        source.AuthorityCode = -1;
        target.Authority = string.Empty;
        target.AuthorityCode = -1;

        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        Assert.Equal(string.Empty, transformation.Authority);
        Assert.Equal(-1, transformation.AuthorityCode);
    }

    /// <summary>
    /// Verifies that when only grid-based operations exist for a coordinate pair and PROJNET_GRID_REQUIRED is set, creating the transformation throws an <see cref="InvalidOperationException"/> with a DataUnavailable prefix.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithGridOnlyDirectOperationsThrowsDeterministicDataUnavailable()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        List<CoordinateOperationDefinition>? gridOnlyPair = provider.GetDefinitions()
            .Where(definition => definition.SourceSrid > 0 && definition.TargetSrid > 0 && definition.SourceSrid != definition.TargetSrid)
            .GroupBy(definition => new { definition.SourceSrid, definition.TargetSrid })
            .Select(group => group.ToList())
            .FirstOrDefault(group => group.All(definition => !string.IsNullOrWhiteSpace(definition.ParameterFileName)));

        Assert.NotNull(gridOnlyPair);

        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        source.Authority = "EPSG";
        source.AuthorityCode = Assert.IsType<List<CoordinateOperationDefinition>>(gridOnlyPair)[0].SourceSrid;
        target.Authority = "EPSG";
        target.AuthorityCode = Assert.IsType<List<CoordinateOperationDefinition>>(gridOnlyPair)[0].TargetSrid;

        string? originalRequiredMode = Environment.GetEnvironmentVariable("PROJNET_GRID_REQUIRED");
        try
        {
            Environment.SetEnvironmentVariable("PROJNET_GRID_REQUIRED", "true");
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target));
            Assert.StartsWith("DataUnavailable:", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PROJNET_GRID_REQUIRED", originalRequiredMode);
        }
    }

    /// <summary>
    /// Verifies that when a coordinate pair has both grid-based and parameter-based catalogued operations, the engine selects a non-grid operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithMixedGridAndNonGridDirectOperationsFallsBackToAvailableMetadataOperation()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        List<CoordinateOperationDefinition>? mixedPair = provider.GetDefinitions()
            .Where(definition => definition.SourceSrid > 0 && definition.TargetSrid > 0 && definition.SourceSrid != definition.TargetSrid)
            .GroupBy(definition => new { definition.SourceSrid, definition.TargetSrid })
            .Select(group => group.ToList())
            .FirstOrDefault(group =>
                group.Any(definition => !string.IsNullOrWhiteSpace(definition.ParameterFileName))
                && group.Any(definition => string.IsNullOrWhiteSpace(definition.ParameterFileName)));

        Assert.NotNull(mixedPair);

        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        source.Authority = "EPSG";
        source.AuthorityCode = mixedPair[0].SourceSrid;
        target.Authority = "EPSG";
        target.AuthorityCode = mixedPair[0].TargetSrid;

        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        Assert.Equal("EPSG", transformation.Authority);
        Assert.DoesNotContain("Grid:", transformation.Remarks ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a geographic-to-geographic transformation using a supported EPSG datum operation includes an explicit datum transform step.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithSupportedGeographicEpsgOperationUsesExplicitDatumTransform()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var services = new CoordinateSystemServices();
        CoordinateOperationDefinition operation = GetRankedOperations(provider)
            .First(definition =>
                IsExplicitMethodSupported(definition.MethodName)
                && string.IsNullOrWhiteSpace(definition.ParameterFileName)
                && services.GetCoordinateSystem(definition.SourceSrid) is GeographicCoordinateSystem
                && services.GetCoordinateSystem(definition.TargetSrid) is GeographicCoordinateSystem);

        GeographicCoordinateSystem source = Assert.IsType<GeographicCoordinateSystem>(services.GetCoordinateSystem(operation.SourceSrid));
        GeographicCoordinateSystem target = Assert.IsType<GeographicCoordinateSystem>(services.GetCoordinateSystem(operation.TargetSrid));

        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        Assert.Equal("EPSG", transformation.Authority);
        Assert.True(IsExplicitMethodSupported(provider.GetDefinitions().First(definition => definition.OperationCode == transformation.AuthorityCode).MethodName));
        Assert.True(ContainsDatumTransform(transformation.MathTransform));
    }

    /// <summary>
    /// Verifies that a projected-to-projected transformation whose base geographic coordinate systems have a supported EPSG datum operation includes an explicit datum transform step.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithProjectedPairUsingSupportedBaseGeographicOperationUsesExplicitDatumTransform()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var services = new CoordinateSystemServices();
        var projectedRecords = EnumerateProjectedCrsRecords().ToList();

        var candidates = (
            from operation in GetRankedOperations(provider)
            where IsExplicitMethodSupported(operation.MethodName)
                && string.IsNullOrWhiteSpace(operation.ParameterFileName)
            from sourceProjected in projectedRecords.Where(record => record.BaseSrid == operation.SourceSrid).Take(1)
            from targetProjected in projectedRecords.Where(record => record.BaseSrid == operation.TargetSrid).Take(1)
            select new
            {
                operation,
                SourceProjectedSrid = sourceProjected.Srid,
                TargetProjectedSrid = targetProjected.Srid,
            }).Take(200);

        foreach (var candidate in candidates)
        {
            ProjectedCoordinateSystem sourceTemplate = Assert.IsType<ProjectedCoordinateSystem>(services.GetCoordinateSystem(candidate.SourceProjectedSrid));
            ProjectedCoordinateSystem targetTemplate = Assert.IsType<ProjectedCoordinateSystem>(services.GetCoordinateSystem(candidate.TargetProjectedSrid));
            ProjectedCoordinateSystem source = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, sourceTemplate.WKT);
            ProjectedCoordinateSystem target = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, targetTemplate.WKT);

            source.Authority = string.Empty;
            source.AuthorityCode = -1;
            target.Authority = string.Empty;
            target.AuthorityCode = -1;
            source.GeographicCoordinateSystem.Authority = "EPSG";
            source.GeographicCoordinateSystem.AuthorityCode = candidate.operation.SourceSrid;
            target.GeographicCoordinateSystem.Authority = "EPSG";
            target.GeographicCoordinateSystem.AuthorityCode = candidate.operation.TargetSrid;

            ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            if (!"EPSG".Equals(transformation.Authority, StringComparison.Ordinal))
            {
                continue;
            }

            if (!ContainsDatumTransform(transformation.MathTransform))
            {
                continue;
            }

            Assert.True(IsExplicitMethodSupported(provider.GetDefinitions().First(definition => definition.OperationCode == transformation.AuthorityCode).MethodName));
            return;
        }

        Assert.Fail("No projected candidate produced an explicit EPSG datum transformation from base geographic metadata.");
    }

    /// <summary>
    /// Verifies that a transformation between fitted coordinate systems is correctly composed by routing through the base coordinate systems.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithFittedSourceAndTargetComposesViaBaseCoordinateSystems()
    {
        var sourceBase = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var targetBase = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        var sourceToBase = new AffineTransform(new double[,]
        {
            { 1d, 0d, 1000d },
            { 0d, 1d, -500d },
            { 0d, 0d, 1d },
        });

        var targetToBase = new AffineTransform(new double[,]
        {
            { 1d, 0d, -2000d },
            { 0d, 1d, 250d },
            { 0d, 0d, 1d },
        });

        FittedCoordinateSystem sourceFitted = this.coordinateSystemFactory.CreateFittedCoordinateSystem(
            "source-fitted",
            sourceBase,
            sourceToBase,
            []);
        FittedCoordinateSystem targetFitted = this.coordinateSystemFactory.CreateFittedCoordinateSystem(
            "target-fitted",
            targetBase,
            targetToBase,
            []);

        ICoordinateTransformation transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(sourceFitted, targetFitted);
        ICoordinateTransformation baseTransformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(sourceBase, targetBase);

        double[] input = [500000d, 4649776.22482d];
        double[] transformed = transformation.MathTransform.Transform(input);

        double[] baseInput = sourceToBase.Transform(input);
        double[] baseOutput = baseTransformation.MathTransform.Transform(baseInput);
        double[] expected = targetToBase.Inverse().Transform(baseOutput);

        Assert.Equal(expected[0], transformed[0], 9);
        Assert.Equal(expected[1], transformed[1], 9);
    }

    private static IEnumerable<EpsgProjectedCrsRecord> EnumerateProjectedCrsRecords()
    {
        for (int cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
        {
            if (!EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int srid))
            {
                continue;
            }

            if (!EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out EpsgCoordinateReferenceRecord reference, out _))
            {
                continue;
            }

            if (reference.Kind != EpsgCoordinateSystemKind.Projected)
            {
                continue;
            }

            if (EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out EpsgProjectedCrsRecord projectedRecord))
            {
                yield return projectedRecord;
            }
        }
    }

    private static bool ContainsGeographicOrGeocentricCoordinateSystem(ICoordinateTransformationCore transformation)
    {
        if (transformation.SourceCS is GeographicCoordinateSystem || transformation.TargetCS is GeographicCoordinateSystem)
        {
            return true;
        }

        if (transformation.SourceCS is GeocentricCoordinateSystem || transformation.TargetCS is GeocentricCoordinateSystem)
        {
            return true;
        }

        if (transformation is ConcatenatedTransform nested)
        {
            foreach (ICoordinateTransformationCore item in nested.CoordinateTransformationList)
            {
                if (ContainsGeographicOrGeocentricCoordinateSystem(item))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsDatumTransform(MathTransform mathTransform)
    {
        if (mathTransform is DatumTransform)
        {
            return true;
        }

        return mathTransform is ConcatenatedTransform concatenated && concatenated.CoordinateTransformationList.Any(ContainsDatumTransform);
    }

    private static bool ContainsDatumTransform(ICoordinateTransformationCore transformation)
    {
        if (transformation is CoordinateTransformation coordinateTransformation)
        {
            return ContainsDatumTransform(coordinateTransformation.MathTransform);
        }

        return transformation is ConcatenatedTransform concatenated && concatenated.CoordinateTransformationList.Any(ContainsDatumTransform);
    }

    private static bool IsExplicitMethodSupported(string methodName)
    {
        string normalized = NormalizeMethodName(methodName);
        return normalized.Contains("geocentrictranslations", StringComparison.Ordinal)
            || normalized.Contains("positionvectortransformation", StringComparison.Ordinal)
            || normalized.Contains("coordinateframerotation", StringComparison.Ordinal)
            || normalized.Contains("molodensky", StringComparison.Ordinal);
    }

    private static string NormalizeMethodName(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string([.. value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
    }

    private static IOrderedEnumerable<CoordinateOperationDefinition> GetRankedOperations(ManagedCoordinateOperationDefinitionProvider provider)
    {
        return provider.GetDefinitions()
            .Where(definition => definition.SourceSrid > 0 && definition.TargetSrid > 0 && definition.SourceSrid != definition.TargetSrid)
            .OrderBy(definition => definition.Accuracy > 0d ? definition.Accuracy : double.MaxValue)
            .ThenBy(definition => string.IsNullOrWhiteSpace(definition.ParameterFileName) ? 0 : 1)
            .ThenBy(definition => string.IsNullOrWhiteSpace(definition.MethodName) ? 1 : 0)
            .ThenBy(definition => definition.OperationCode);
    }
}
