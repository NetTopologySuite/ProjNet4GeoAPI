// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates TCEA projection aliases and roundtrip behavior.
/// </summary>
public class TransverseCylindricalEqualAreaProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies that TCEA aliases resolve from WKT and produce usable transforms.
    /// </summary>
    /// <param name="projectionName">Projection alias to validate.</param>
    [Theory]
    [InlineData("tcea")]
    [InlineData("Transverse_Cylindrical_Equal_Area")]
    public void SupportsTceaAliasesFromWkt(string projectionName)
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, 0d, 0d, 1d));
        ICoordinateTransformation transform = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);
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
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(projectionName, latitudeOfOrigin, centralMeridian, scaleFactor));

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, projected);
        ICoordinateTransformation inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, GeographicCoordinateSystem.WGS84);

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
            $"PROJCS[\"Projection-{projectionName}\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",{latitudeText}],PARAMETER[\"central_meridian\",{meridianText}],PARAMETER[\"scale_factor\",{scaleText}],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
