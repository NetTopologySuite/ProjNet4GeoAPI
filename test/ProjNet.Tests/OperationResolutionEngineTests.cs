using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
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
    }
}
