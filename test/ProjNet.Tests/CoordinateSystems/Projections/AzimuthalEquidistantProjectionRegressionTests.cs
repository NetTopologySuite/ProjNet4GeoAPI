// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Regression tests for Azimuthal Equidistant projection parity with PROJ reference vectors.
/// </summary>
public class AzimuthalEquidistantProjectionRegressionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    /// <summary>
    /// Verifies north-pole spherical forward projection against PROJ.
    /// </summary>
    [Fact]
    public void AzimuthalEquidistantPolarSphericalMatchesProjReference()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(
                projectionName: "aeqd",
                spheroidClause: "SPHEROID[\"Sphere\",6371000,0]",
                latitudeOfOrigin: 90d,
                centralMeridian: 0d));

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);
        double[] projectedPoint = forward.MathTransform.Transform([30d, 60d]);

        Assert.InRange(Math.Abs(projectedPoint[0] - 1667923.8996683809d), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - (-2888928.9373840508d)), 0d, 1e-3d);
    }

    /// <summary>
    /// Verifies oblique ellipsoidal forward projection against PROJ.
    /// </summary>
    [Fact]
    public void AzimuthalEquidistantEllipsoidalObliqueMatchesProjReference()
    {
        ProjectedCoordinateSystem projected = ProjNet.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            BuildProjectedWkt(
                projectionName: "aeqd",
                spheroidClause: "SPHEROID[\"WGS 84\",6378137,298.257223563]",
                latitudeOfOrigin: 48d,
                centralMeridian: 10d));

        ICoordinateTransformation forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(
            projected.GeographicCoordinateSystem,
            projected);
        double[] projectedPoint = forward.MathTransform.Transform([16.7139129117067757d, 52.2393942647647999d]);

        Assert.InRange(Math.Abs(projectedPoint[0] - 458432.3283705170d), 0d, 1e-3d);
        Assert.InRange(Math.Abs(projectedPoint[1] - 491980.7580910911d), 0d, 1e-3d);
    }

    private static string BuildProjectedWkt(string projectionName, string spheroidClause, double latitudeOfOrigin, double centralMeridian)
    {
        return FormattableString.Invariant(
            $"PROJCS[\"Regression-{projectionName}\",GEOGCS[\"Regression-Geog\",DATUM[\"Regression-Datum\",{spheroidClause}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{projectionName}\"],PARAMETER[\"latitude_of_origin\",{latitudeOfOrigin}],PARAMETER[\"central_meridian\",{centralMeridian}],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]");
    }
}
