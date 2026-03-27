// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNET.Tests;

using System;
using System.Globalization;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates current projection group projections (<c>airy</c>, <c>chamb</c>, <c>bipc</c>).
/// </summary>
public class AiryChamberlinBipolarProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for covered projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("airy")]
    [InlineData("bipc")]
    [InlineData("Bipolar_Conic")]
    [InlineData("chamb")]
    [InlineData("Chamberlin_Trimetric")]
    public void SupportsAliasesFromWkt(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, GetDefaultProfile(projectionName)));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for covered projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="sphereRadius">Sphere radius meters for the fixture profile.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    /// <param name="extraParameters">Optional WKT parameter segment.</param>
    [Theory]
    [InlineData("airy", 6400000d, 2d, 1d, 189109.886908621d, 94583.752387504d, null)]
    [InlineData("chamb", 6400000d, 2d, 1d, -27864.779586801d, -223364.324593274d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]")]
    [InlineData("bipc", 6400000d, 2d, 1d, 2460565.740974965d, -14598319.989330800d, null)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double sphereRadius,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        string? extraParameters)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, new ProjectionProfile(sphereRadius, extraParameters)));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable covered projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="sphereRadius">Sphere radius meters for the fixture profile.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    [Theory]
    [InlineData("bipc", 6400000d, 200d, 100d, -73.038693105d, 17.248116270d)]
    [InlineData("bipc", 6400000d, -200d, -100d, -73.034503807d, 17.246835092d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double sphereRadius,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, new ProjectionProfile(sphereRadius, null)));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies Airy and Chamberlin remain forward-only in this test set.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    [Theory]
    [InlineData("airy")]
    [InlineData("chamb")]
    public void ForwardOnlyProjectionsDoNotSupportInverse(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt(projectionName, GetDefaultProfile(projectionName)));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(200d, 100d)));
    }

    /// <summary>
    /// Verifies bipolar conic roundtrip stability.
    /// </summary>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData(2d, 1d)]
    [InlineData(-2d, -1d)]
    public void SupportsBipcRoundtrip(double longitude, double latitude)
    {
        var projected = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, BuildProjectedWkt("bipc", new ProjectionProfile(6400000d, null)));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);

        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-9);
    }

    /// <summary>
    /// Verifies Airy forward behavior for polar aspects and <c>no_cut</c> builtins cases.
    /// </summary>
    [Fact]
    public void AiryPolarAndNoCutCasesMatchBuiltins()
    {
        var northPole = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, 
            BuildProjectedWkt(
                "airy",
                new ProjectionProfile(
                    1d,
                    ",PARAMETER[\"latitude_of_origin\",90]")));
        var northForward = CoordinateTransformationFactory.CreateFromCoordinateSystems(northPole.GeographicCoordinateSystem, northPole);

        double[] northZero = northForward.MathTransform.Transform(CreatePoint(0d, 0d));
        double[] northAtPole = northForward.MathTransform.Transform(CreatePoint(0d, 90d));

        Assert.InRange(Math.Abs(northZero[0] - 0d), 0d, 1e-6);
        Assert.InRange(Math.Abs(northZero[1] - (-1.3863d)), 0d, 1e-4);
        Assert.InRange(Math.Abs(northAtPole[0] - 0d), 0d, 1e-6);
        Assert.InRange(Math.Abs(northAtPole[1] - 0d), 0d, 1e-6);
        Assert.Throws<ArgumentException>(() => northForward.MathTransform.Transform(CreatePoint(0d, -90d)));

        var noCut = ProjNET.Tests.CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, 
            BuildProjectedWkt(
                "airy",
                new ProjectionProfile(
                    1d,
                    ",PARAMETER[\"latitude_of_origin\",-90],PARAMETER[\"no_cut\",1]")));
        var noCutForward = CoordinateTransformationFactory.CreateFromCoordinateSystems(noCut.GeographicCoordinateSystem, noCut);
        double[] noCutProjected = noCutForward.MathTransform.Transform(CreatePoint(0d, 10d));

        Assert.InRange(Math.Abs(noCutProjected[0] - 0d), 0d, 1e-6);
        Assert.InRange(Math.Abs(noCutProjected[1] - 1.5677d), 0d, 1e-4);
    }

    private static string BuildProjectedWkt(string projectionName, ProjectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        ProjectionProfile effectiveProfile = profile ?? new ProjectionProfile(6400000d, null);
        bool hasLatitudeOfOrigin = effectiveProfile.ExtraParameters?.IndexOf("latitude_of_origin", StringComparison.OrdinalIgnoreCase) >= 0;
        string latitudeOfOriginParameter = hasLatitudeOfOrigin ? string.Empty : ",PARAMETER[\"latitude_of_origin\",0]";
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Projection-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",{1},0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"]{2},PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{3},UNIT[\"metre\",1]]",
            projectionName,
            effectiveProfile.SphereRadius.ToString("R", CultureInfo.InvariantCulture),
            latitudeOfOriginParameter,
            effectiveProfile.ExtraParameters ?? string.Empty);
    }

    private static ProjectionProfile GetDefaultProfile(string projectionName)
    {
        if (projectionName.Equals("chamb", StringComparison.OrdinalIgnoreCase) ||
            projectionName.Equals("chamberlin_trimetric", StringComparison.OrdinalIgnoreCase))
        {
            return new ProjectionProfile(6400000d, ",PARAMETER[\"lat_1\",0.5],PARAMETER[\"lat_2\",2]");
        }

        return new ProjectionProfile(6400000d, null);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];

    private sealed class ProjectionProfile
    {
        public ProjectionProfile(double sphereRadius, string? extraParameters)
        {
            this.SphereRadius = sphereRadius;
            this.ExtraParameters = extraParameters;
        }

        public double SphereRadius { get; }

        public string ExtraParameters { get; }
    }
}
