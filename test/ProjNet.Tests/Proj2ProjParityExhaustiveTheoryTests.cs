namespace ProjNET.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using ProjNet.CoordinateSystems;
    using ProjNet.CoordinateSystems.Transformations;
    using Xunit;

    public class Proj2ProjParityExhaustiveTheoryTests
    {
        [Theory]
        [Trait("Category", "ExhaustiveValidation")]
        [MemberData(nameof(GetParityCases))]
        public void CreateFromCoordinateSystems_WithDirectProjectedPair_MatchesExhaustiveProjReference(Proj2ProjParityTheoryTests.Proj2ProjCase testCase)
        {
            if (!IsExhaustiveLaneEnabled())
            {
                Assert.Skip("Set PROJNET_RUN_EXHAUSTIVE=1 to run exhaustive parity cases.");
            }

            var coordinateSystemFactory = new CoordinateSystemFactory();
            var transformationFactory = new CoordinateTransformationFactory();

            var source = (CoordinateSystem)coordinateSystemFactory.CreateFromWkt(testCase.SourceWkt);
            var target = (CoordinateSystem)coordinateSystemFactory.CreateFromWkt(testCase.TargetWkt);
            source.Authority = "EPSG";
            source.AuthorityCode = testCase.SourceSrid;
            target.Authority = "EPSG";
            target.AuthorityCode = testCase.TargetSrid;

            var transformation = transformationFactory.CreateFromCoordinateSystems(source, target);
            double[] output = transformation.MathTransform.Transform(new[] { testCase.InputX, testCase.InputY });
            double deltaX = Math.Abs(output[0] - testCase.ExpectedX);
            double deltaY = Math.Abs(output[1] - testCase.ExpectedY);

            Assert.Equal("EPSG", transformation.Authority);
            Assert.Equal(testCase.OperationCode, transformation.AuthorityCode);
            Assert.InRange(deltaX, 0d, testCase.ToleranceMeters);
            Assert.InRange(deltaY, 0d, testCase.ToleranceMeters);
        }

        public static IEnumerable<object[]> GetParityCases()
        {
            string fixturePath = Path.Combine(AppContext.BaseDirectory, "Generated", "proj2proj-direct-parity-exhaustive-fixture.json");
            Assert.True(File.Exists(fixturePath), "Fixture file not found: " + fixturePath);

            string json = File.ReadAllText(fixturePath);
            var fixture = JsonSerializer.Deserialize<Proj2ProjParityTheoryTests.Proj2ProjFixture>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            Assert.NotNull(fixture);
            Assert.NotNull(fixture.Cases);
            Assert.NotEmpty(fixture.Cases);

            foreach (var item in fixture.Cases)
            {
                yield return new object[] { item };
            }
        }

        private static bool IsExhaustiveLaneEnabled()
        {
            string value = Environment.GetEnvironmentVariable("PROJNET_RUN_EXHAUSTIVE");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
