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

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

public class Phase4ProjectionSupportTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    [Theory]
    [InlineData("eqearth")]
    [InlineData("natearth")]
    [InlineData("natearth2")]
    [InlineData("robin")]
    [InlineData("moll")]
    [InlineData("aeqd")]
    [InlineData("gnom")]
    public void SupportsPhase4ProjectionAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(new[] { 1000d, 2000d });

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    [Theory]
    [InlineData("eqearth", 12.5d, 25.25d, 1e-6)]
    [InlineData("natearth", -32.4d, 18.6d, 1e-6)]
    [InlineData("natearth2", 77.2d, -22.8d, 1e-6)]
    [InlineData("robin", 101.5d, 40.1d, 1e-5)]
    [InlineData("moll", -73.8d, 5.5d, 1e-6)]
    [InlineData("aeqd", 12.5d, 25.25d, 1e-6)]
    [InlineData("gnom", 8.2d, 15.4d, 1e-6)]
    public void SupportsPhase4ProjectionRoundtrip(string projectionName, double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(new[] { longitude, latitude });
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(System.Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(System.Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return
            $"PROJCS[\"Phase4-{projectionName}\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }
}

