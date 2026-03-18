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

namespace ProjNET.Tests
{
    using ProjNet;
    using ProjNet.CoordinateSystems;
    using ProjNet.CoordinateSystems.Transformations;
    using Xunit;

    public class VerificationSuiteTests
    {
        [Theory]
        [InlineData(0d, 0d, 0d, 0d)]
        [InlineData(10d, 10d, 1113194.90793274d, 1118889.97485796d)]
        [InlineData(-75d, 35d, -8348961.80949552d, 4163881.14406429d)]
        public void Wgs84ToWebMercator_MatchesReferencePoints(double lon, double lat, double expectedX, double expectedY)
        {
            var services = CreateCanonicalServices();
            var transform = services.CreateTransformation(4326, 3857);
            double[] result = transform.MathTransform.Transform(new[] { lon, lat });

            AssertCoordinate(expectedX, expectedY, result[0], result[1], 1e-6);
        }

        [Theory]
        [InlineData(0d, 0d, 0d, 0d)]
        [InlineData(1113194.90793274d, 1118889.97485796d, 10d, 10d)]
        [InlineData(-8348961.80949552d, 4163881.14406429d, -75d, 35d)]
        public void WebMercatorToWgs84_MatchesReferencePoints(double x, double y, double expectedLon, double expectedLat)
        {
            var services = CreateCanonicalServices();
            var transform = services.CreateTransformation(3857, 4326);
            double[] result = transform.MathTransform.Transform(new[] { x, y });

            AssertCoordinate(expectedLon, expectedLat, result[0], result[1], 1e-9);
        }

        [Fact]
        public void LegacyCoordinateSystemServicesLookups_RemainConsistent()
        {
            var services = CreateCanonicalServices();

            var bySrid = services.GetCoordinateSystem(4326);
            var byAuthority = services.GetCoordinateSystem("EPSG", 4326);
            bool found = services.TryGetCoordinateSystem("EPSG", 4326, out var byTryGet);
            int? srid = services.GetSRID("EPSG", 4326);

            Assert.NotNull(bySrid);
            Assert.NotNull(byAuthority);
            Assert.True(found);
            Assert.NotNull(byTryGet);
            Assert.Equal(4326, srid);
            Assert.Same(bySrid, byAuthority);
            Assert.Same(bySrid, byTryGet);
        }

        private static CoordinateSystemServices CreateCanonicalServices()
        {
            return new CoordinateSystemServices(new[]
            {
                new System.Collections.Generic.KeyValuePair<int, string>(4326, GeographicCoordinateSystem.WGS84.WKT),
                new System.Collections.Generic.KeyValuePair<int, string>(3857, ProjectedCoordinateSystem.WebMercator.WKT),
            });
        }

        private static void AssertCoordinate(double expectedX, double expectedY, double actualX, double actualY, double tolerance)
        {
            Assert.InRange(actualX, expectedX - tolerance, expectedX + tolerance);
            Assert.InRange(actualY, expectedY - tolerance, expectedY + tolerance);
        }
    }
}
