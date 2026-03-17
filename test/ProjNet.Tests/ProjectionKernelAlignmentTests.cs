using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

namespace ProjNET.Tests
{
    public class ProjectionKernelAlignmentTests
    {
        private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
        private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

        [Theory]
        [InlineData("Mercator (variant A)")]
        [InlineData("Mercator (variant B)")]
        [InlineData("Web_Mercator")]
        public void SupportsMercatorVariantAliases(string projectionName)
        {
            var source = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
                $"PROJCS[\"Alias Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1],AUTHORITY[\"EPSG\",\"3857\"]]");

            var target = GeographicCoordinateSystem.WGS84;
            var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            var result = transform.MathTransform.Transform(new[] { 1000d, 2000d });

            Assert.NotNull(transform);
            Assert.NotNull(result);
            Assert.True(result.Length >= 2);
        }

        [Theory]
        [InlineData("Transverse_Mercator_South_Oriented")]
        [InlineData("Gauss_Kruger")]
        public void SupportsTransverseMercatorAliases(string projectionName)
        {
            var source = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
                $"PROJCS[\"Alias TM\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1],AUTHORITY[\"EPSG\",\"32632\"]]");

            var target = GeographicCoordinateSystem.WGS84;
            var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            var result = transform.MathTransform.Transform(new[] { 500000d, 4649776.22482d });

            Assert.NotNull(transform);
            Assert.NotNull(result);
            Assert.True(result.Length >= 2);
        }

        [Theory]
        [InlineData("Lambert_Conformal_Conic_1SP")]
        [InlineData("Lambert_Conformal_Conic_2SP_Belgium")]
        public void SupportsLambertConformalAliases(string projectionName)
        {
            var source = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
                $"PROJCS[\"Alias LCC\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",40],PARAMETER[\"central_meridian\",-100],PARAMETER[\"standard_parallel_1\",33],PARAMETER[\"standard_parallel_2\",45],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");

            var target = GeographicCoordinateSystem.WGS84;
            var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);
            var result = transform.MathTransform.Transform(new[] { 100000d, 100000d });

            Assert.NotNull(transform);
            Assert.NotNull(result);
            Assert.True(result.Length >= 2);
        }
    }
}
