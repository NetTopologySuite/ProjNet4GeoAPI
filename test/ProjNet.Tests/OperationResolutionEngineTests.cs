using System;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using Xunit;

namespace ProjNET.Tests
{
    public class OperationResolutionEngineTests
    {
        private readonly CoordinateTransformationFactory _coordinateTransformationFactory = new CoordinateTransformationFactory();
        private readonly CoordinateSystemFactory _coordinateSystemFactory = new CoordinateSystemFactory();

        [Fact]
        public void CreateFromCoordinateSystems_WithSameProjectedCoordinateSystem_UsesIdentityTransform()
        {
            var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
            var transformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(source, source);
            var output = transformation.MathTransform.Transform(new[] { 500000d, 4649776.22482d });

            Assert.True(transformation.MathTransform.Identity());
            Assert.Equal(500000d, output[0], 12);
            Assert.Equal(4649776.22482d, output[1], 12);
        }

        [Fact]
        public void CreateFromCoordinateSystems_WithEquivalentGeographicCoordinateSystems_UsesIdentityTransform()
        {
            var source = GeographicCoordinateSystem.WGS84;
            var target = (GeographicCoordinateSystem)_coordinateSystemFactory.CreateFromWkt(source.WKT);
            var transformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            var output = transformation.MathTransform.Transform(new[] { 13.1234d, 52.9876d });

            Assert.True(transformation.MathTransform.Identity());
            Assert.Equal(13.1234d, output[0], 12);
            Assert.Equal(52.9876d, output[1], 12);
        }

        [Fact]
        public void CreateFromCoordinateSystems_WithProjectedPairHavingDirectMetadata_PrefersMetadataCandidate()
        {
            var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
            var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
            source.Authority = "EPSG";
            source.AuthorityCode = 28992;
            target.Authority = "EPSG";
            target.AuthorityCode = 23031;

            var metadataTransformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            var metadataOutput = metadataTransformation.MathTransform.Transform(new[] { 500000d, 4649776.22482d });

            var fallbackSource = ProjectedCoordinateSystem.WGS84_UTM(32, true);
            var fallbackTarget = ProjectedCoordinateSystem.WGS84_UTM(33, true);
            fallbackSource.Authority = string.Empty;
            fallbackSource.AuthorityCode = -1;
            fallbackTarget.Authority = string.Empty;
            fallbackTarget.AuthorityCode = -1;
            var fallbackTransformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(fallbackSource, fallbackTarget);
            var fallbackOutput = fallbackTransformation.MathTransform.Transform(new[] { 500000d, 4649776.22482d });

            Assert.Equal("EPSG", metadataTransformation.Authority);
            Assert.Equal(1044, metadataTransformation.AuthorityCode);
            Assert.Equal(fallbackOutput[0], metadataOutput[0], 9);
            Assert.Equal(fallbackOutput[1], metadataOutput[1], 9);

            var concatenated = Assert.IsType<ConcatenatedTransform>(metadataTransformation.MathTransform);
            Assert.Equal(2, concatenated.CoordinateTransformationList.Count);
            Assert.DoesNotContain(concatenated.CoordinateTransformationList, ContainsGeographicOrGeocentricCoordinateSystem);
        }

        [Fact]
        public void CreateFromCoordinateSystems_WithProjectedFallbackPair_UsesDirectProj2ProjCorePath()
        {
            var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
            var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
            source.Authority = string.Empty;
            source.AuthorityCode = -1;
            target.Authority = string.Empty;
            target.AuthorityCode = -1;

            var transformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

            var concatenated = Assert.IsType<ConcatenatedTransform>(transformation.MathTransform);
            Assert.Equal(2, concatenated.CoordinateTransformationList.Count);
            Assert.DoesNotContain(concatenated.CoordinateTransformationList, ContainsGeographicOrGeocentricCoordinateSystem);
        }

        [Fact]
        public void CreateFromCoordinateSystems_WithProjectedPairWithoutEpsgAuthority_UsesLegacyFallback()
        {
            var source = ProjectedCoordinateSystem.WGS84_UTM(32, true);
            var target = ProjectedCoordinateSystem.WGS84_UTM(33, true);
            source.Authority = string.Empty;
            source.AuthorityCode = -1;
            target.Authority = string.Empty;
            target.AuthorityCode = -1;

            var transformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

            Assert.Equal(string.Empty, transformation.Authority);
            Assert.Equal(-1, transformation.AuthorityCode);
        }

        [Fact]
        public void CreateFromCoordinateSystems_WithGridOnlyDirectOperations_ThrowsDeterministicDataUnavailable()
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
                var exception = Assert.Throws<InvalidOperationException>(() => _coordinateTransformationFactory.CreateFromCoordinateSystems(source, target));
                Assert.StartsWith("DataUnavailable:", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PROJNET_GRID_REQUIRED", originalRequiredMode);
            }
        }

        [Fact]
        public void CreateFromCoordinateSystems_WithMixedGridAndNonGridDirectOperations_FallsBackToAvailableMetadataOperation()
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

            var transformation = _coordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

            Assert.Equal("EPSG", transformation.Authority);
            Assert.DoesNotContain("Grid:", transformation.Remarks ?? string.Empty);
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
    }
}
