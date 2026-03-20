// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests;

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
/// Represents the documented type.
/// </summary>
public class OperationResolutionEngineTests
{
    private static readonly double[] GeographicSamplePoint = { 13.1234d, 52.9876d };
    private static readonly double[] UtmSamplePoint = { 500000d, 4649776.22482d };

    private readonly CoordinateTransformationFactory coordinateTransformationFactory = new CoordinateTransformationFactory();
    private readonly CoordinateSystemFactory coordinateSystemFactory = new CoordinateSystemFactory();

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithSameProjectedCoordinateSystemUsesIdentityTransform()
    {
        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, source);
        double[] output = transformation.MathTransform.Transform(UtmSamplePoint);

        Assert.True(transformation.MathTransform.Identity());
        Assert.Equal(500000d, output[0], 12);
        Assert.Equal(4649776.22482d, output[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithEquivalentGeographicCoordinateSystemsUsesIdentityTransform()
    {
        var source = GeographicCoordinateSystem.WGS84;
        var target = (GeographicCoordinateSystem)this.coordinateSystemFactory.CreateFromWkt(source.WKT);
        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] output = transformation.MathTransform.Transform(GeographicSamplePoint);

        Assert.True(transformation.MathTransform.Identity());
        Assert.Equal(13.1234d, output[0], 12);
        Assert.Equal(52.9876d, output[1], 12);
    }

    /// <summary>
    /// Performs the documented operation.
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

        var metadataTransformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
        double[] metadataOutput = metadataTransformation.MathTransform.Transform(UtmSamplePoint);

        var fallbackSource = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var fallbackTarget = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        fallbackSource.Authority = string.Empty;
        fallbackSource.AuthorityCode = -1;
        fallbackTarget.Authority = string.Empty;
        fallbackTarget.AuthorityCode = -1;
        var fallbackTransformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(fallbackSource, fallbackTarget);
        double[] fallbackOutput = fallbackTransformation.MathTransform.Transform(UtmSamplePoint);

        Assert.Equal("EPSG", metadataTransformation.Authority);
        Assert.Equal(1044, metadataTransformation.AuthorityCode);
        Assert.Equal(fallbackOutput[0], metadataOutput[0], 9);
        Assert.Equal(fallbackOutput[1], metadataOutput[1], 9);

        var concatenated = Assert.IsType<ConcatenatedTransform>(metadataTransformation.MathTransform);
        Assert.Equal(2, concatenated.CoordinateTransformationList.Count);
        Assert.DoesNotContain(concatenated.CoordinateTransformationList, ContainsGeographicOrGeocentricCoordinateSystem);
    }

    /// <summary>
    /// Performs the documented operation.
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

        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        var concatenated = Assert.IsType<ConcatenatedTransform>(transformation.MathTransform);
        Assert.Equal(2, concatenated.CoordinateTransformationList.Count);
        Assert.DoesNotContain(concatenated.CoordinateTransformationList, ContainsGeographicOrGeocentricCoordinateSystem);
    }

    /// <summary>
    /// Performs the documented operation.
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

        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        Assert.Equal(string.Empty, transformation.Authority);
        Assert.Equal(-1, transformation.AuthorityCode);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithGridOnlyDirectOperationsThrowsDeterministicDataUnavailable()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var gridOnlyPair = provider.GetDefinitions()
            .Where(definition => definition.SourceSrid > 0 && definition.TargetSrid > 0 && definition.SourceSrid != definition.TargetSrid)
            .GroupBy(definition => new { definition.SourceSrid, definition.TargetSrid })
            .Select(group => group.ToList())
            .FirstOrDefault(group => group.All(definition => !string.IsNullOrWhiteSpace(definition.ParameterFileName)));

        Assert.NotNull(gridOnlyPair);

        var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        source.Authority = "EPSG";
        source.AuthorityCode = gridOnlyPair[0].SourceSrid;
        target.Authority = "EPSG";
        target.AuthorityCode = gridOnlyPair[0].TargetSrid;

        string originalRequiredMode = Environment.GetEnvironmentVariable("PROJNET_GRID_REQUIRED");
        try
        {
            Environment.SetEnvironmentVariable("PROJNET_GRID_REQUIRED", "true");
            var exception = Assert.Throws<InvalidOperationException>(() => this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target));
            Assert.StartsWith("DataUnavailable:", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PROJNET_GRID_REQUIRED", originalRequiredMode);
        }
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithMixedGridAndNonGridDirectOperationsFallsBackToAvailableMetadataOperation()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var mixedPair = provider.GetDefinitions()
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

        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        Assert.Equal("EPSG", transformation.Authority);
        Assert.DoesNotContain("Grid:", transformation.Remarks ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithSupportedGeographicEpsgOperationUsesExplicitDatumTransform()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var services = new CoordinateSystemServices();
        var operation = GetRankedOperations(provider)
            .First(definition =>
                IsExplicitMethodSupported(definition.MethodName)
                && string.IsNullOrWhiteSpace(definition.ParameterFileName)
                && services.GetCoordinateSystem(definition.SourceSrid) is GeographicCoordinateSystem
                && services.GetCoordinateSystem(definition.TargetSrid) is GeographicCoordinateSystem);

        var source = (GeographicCoordinateSystem)services.GetCoordinateSystem(operation.SourceSrid);
        var target = (GeographicCoordinateSystem)services.GetCoordinateSystem(operation.TargetSrid);

        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

        Assert.Equal("EPSG", transformation.Authority);
        Assert.True(IsExplicitMethodSupported(provider.GetDefinitions().First(definition => definition.OperationCode == transformation.AuthorityCode).MethodName));
        Assert.True(ContainsDatumTransform(transformation.MathTransform));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void CreateFromCoordinateSystemsWithProjectedPairUsingSupportedBaseGeographicOperationUsesExplicitDatumTransform()
    {
        var provider = new ManagedCoordinateOperationDefinitionProvider();
        var services = new CoordinateSystemServices();

        var candidates = (
            from operation in GetRankedOperations(provider)
            where IsExplicitMethodSupported(operation.MethodName)
                && string.IsNullOrWhiteSpace(operation.ParameterFileName)
            from sourceProjected in EpsgGeneratedCatalog.ProjectedCrs.Where(record => record.BaseSrid == operation.SourceSrid).Take(1)
            from targetProjected in EpsgGeneratedCatalog.ProjectedCrs.Where(record => record.BaseSrid == operation.TargetSrid).Take(1)
            select new
            {
                operation,
                SourceProjectedSrid = sourceProjected.Srid,
                TargetProjectedSrid = targetProjected.Srid,
            }).Take(200);

        foreach (var candidate in candidates)
        {
            var sourceTemplate = (ProjectedCoordinateSystem)services.GetCoordinateSystem(candidate.SourceProjectedSrid);
            var targetTemplate = (ProjectedCoordinateSystem)services.GetCoordinateSystem(candidate.TargetProjectedSrid);
            var source = (ProjectedCoordinateSystem)this.coordinateSystemFactory.CreateFromWkt(sourceTemplate.WKT);
            var target = (ProjectedCoordinateSystem)this.coordinateSystemFactory.CreateFromWkt(targetTemplate.WKT);

            source.Authority = string.Empty;
            source.AuthorityCode = -1;
            target.Authority = string.Empty;
            target.AuthorityCode = -1;
            source.GeographicCoordinateSystem.Authority = "EPSG";
            source.GeographicCoordinateSystem.AuthorityCode = candidate.operation.SourceSrid;
            target.GeographicCoordinateSystem.Authority = "EPSG";
            target.GeographicCoordinateSystem.AuthorityCode = candidate.operation.TargetSrid;

            var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
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
    /// Performs the documented operation.
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

        var sourceFitted = this.coordinateSystemFactory.CreateFittedCoordinateSystem(
            "source-fitted",
            sourceBase,
            sourceToBase,
            new List<AxisInfo>());
        var targetFitted = this.coordinateSystemFactory.CreateFittedCoordinateSystem(
            "target-fitted",
            targetBase,
            targetToBase,
            new List<AxisInfo>());

        var transformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(sourceFitted, targetFitted);
        var baseTransformation = this.coordinateTransformationFactory.CreateFromCoordinateSystems(sourceBase, targetBase);

        double[] input = new[] { 500000d, 4649776.22482d };
        double[] transformed = transformation.MathTransform.Transform(input);

        double[] baseInput = sourceToBase.Transform(input);
        double[] baseOutput = baseTransformation.MathTransform.Transform(baseInput);
        double[] expected = targetToBase.Inverse().Transform(baseOutput);

        Assert.Equal(expected[0], transformed[0], 9);
        Assert.Equal(expected[1], transformed[1], 9);
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
            foreach (var item in nested.CoordinateTransformationList)
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

        if (mathTransform is ConcatenatedTransform concatenated)
        {
            return concatenated.CoordinateTransformationList.Any(ContainsDatumTransform);
        }

        return false;
    }

    private static bool ContainsDatumTransform(ICoordinateTransformationCore transformation)
    {
        if (transformation is CoordinateTransformation coordinateTransformation)
        {
            return ContainsDatumTransform(coordinateTransformation.MathTransform);
        }

        if (transformation is ConcatenatedTransform concatenated)
        {
            return concatenated.CoordinateTransformationList.Any(ContainsDatumTransform);
        }

        return false;
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
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
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
