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
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates TCEA projection aliases and roundtrip behavior.
/// </summary>
public class Phase5TceaProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies that TCEA aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("tcea")]
    [InlineData("Transverse_Cylindrical_Equal_Area")]
    public void SupportsTceaAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, 0d, 0d, 1d));
        var transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
        double[] result = transform.MathTransform.Transform(CreatePoint(120000d, 210000d));

        Assert.NotNull(projected);
        Assert.NotNull(transform);
        Assert.NotNull(result);
        Assert.True(result.Length >= 2);
    }

    /// <summary>
    /// Verifies forward/inverse roundtrip stability for TCEA aliases.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    /// <param name="longitude">Input longitude.</param>
    /// <param name="latitude">Input latitude.</param>
    /// <param name="latitudeOfOrigin">Projection latitude of origin.</param>
    /// <param name="centralMeridian">Projection central meridian.</param>
    /// <param name="scaleFactor">Projection scale factor.</param>
    /// <param name="tolerance">Maximum absolute roundtrip delta.</param>
    [Theory]
    [InlineData("tcea", 8.2d, 47.3d, 0d, 0d, 1d, 1e-9)]
    [InlineData("tcea", -73.5d, 22.1d, 10d, -30d, 0.9999d, 1e-8)]
    [InlineData("Transverse_Cylindrical_Equal_Area", 45.5d, -12.75d, -5d, 20d, 1.0002d, 1e-8)]
    public void SupportsTceaRoundtrip(
        string projectionName,
        double longitude,
        double latitude,
        double latitudeOfOrigin,
        double centralMeridian,
        double scaleFactor,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(
            BuildProjectedWkt(projectionName, latitudeOfOrigin, centralMeridian, scaleFactor));

        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    private static string BuildProjectedWkt(string projectionName, double latitudeOfOrigin, double centralMeridian, double scaleFactor)
    {
        string latitudeText = latitudeOfOrigin.ToString(CultureInfo.InvariantCulture);
        string meridianText = centralMeridian.ToString(CultureInfo.InvariantCulture);
        string scaleText = scaleFactor.ToString(CultureInfo.InvariantCulture);

        return
            $"PROJCS[\"Phase5-{projectionName}\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",{latitudeText}],PARAMETER[\"central_meridian\",{meridianText}],PARAMETER[\"scale_factor\",{scaleText}],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
