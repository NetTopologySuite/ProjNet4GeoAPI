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
/// Validates Wagner projection support for M6 wave 3 (<c>wag2</c>, <c>wag3</c>, <c>wag7</c>).
/// </summary>
public class Phase6WagProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for Wagner projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("wag2")]
    [InlineData("Wagner_II")]
    [InlineData("wag3")]
    [InlineData("Wagner_III")]
    [InlineData("wag7")]
    [InlineData("Wagner_VII")]
    public void SupportsWagAliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, null));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for Wagner projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="latTs">Optional lat_ts in degrees.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    [Theory]
    [InlineData("wag2", null, 2d, 1d, 206589.888099962d, 120778.040357547d)]
    [InlineData("wag2", null, -2d, -1d, -206589.888099962d, -120778.040357547d)]
    [InlineData("wag3", null, 2d, 1d, 223387.021718166d, 111701.072127637d)]
    [InlineData("wag3", null, -2d, -1d, -223387.021718166d, -111701.072127637d)]
    [InlineData("wag7", null, 2d, 1d, 198601.876957312d, 125637.045714171d)]
    [InlineData("wag7", null, -2d, -1d, -198601.876957312d, -125637.045714171d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double? latTs,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, latTs));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for Wagner II and III.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="latTs">Optional lat_ts in degrees.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    [Theory]
    [InlineData("wag2", null, 200d, 100d, 0.001936024d, 0.000827958d)]
    [InlineData("wag2", null, -200d, -100d, -0.001936024d, -0.000827958d)]
    [InlineData("wag3", null, 200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData("wag3", null, -200d, -100d, -0.001790493d, -0.000895247d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double? latTs,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, latTs));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies Wagner VII inverse is intentionally unavailable.
    /// </summary>
    [Fact]
    public void WagnerViiInverseIsNotSupported()
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("wag7", null));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(200d, 100d)));
    }

    /// <summary>
    /// Verifies roundtrip stability for Wagner II and III.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="latTs">Optional lat_ts in degrees.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData("wag2", null, 2d, 1d)]
    [InlineData("wag2", null, -2d, -1d)]
    [InlineData("wag3", null, 2d, 1d)]
    [InlineData("wag3", null, -2d, -1d)]
    [InlineData("wag3", 10d, 2d, 1d)]
    [InlineData("wag3", 10d, -2d, -1d)]
    public void SupportsWagRoundtrip(string projectionName, double? latTs, double longitude, double latitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, latTs));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-9);
    }

    /// <summary>
    /// Verifies that <c>lat_ts</c> changes Wagner III forward output while remaining numerically stable.
    /// </summary>
    [Fact]
    public void WagnerIiiLatTsChangesForwardResult()
    {
        var projectedDefault = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("wag3", null));
        var projectedLatTs10 = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("wag3", 10d));
        var forwardDefault = CoordinateTransformationFactory.CreateFromCoordinateSystems(projectedDefault.GeographicCoordinateSystem, projectedDefault);
        var forwardLatTs10 = CoordinateTransformationFactory.CreateFromCoordinateSystems(projectedLatTs10.GeographicCoordinateSystem, projectedLatTs10);

        double[] point = CreatePoint(2d, 1d);
        double[] projectedDefaultPoint = forwardDefault.MathTransform.Transform(point);
        double[] projectedLatTs10Point = forwardLatTs10.MathTransform.Transform(point);

        Assert.InRange(Math.Abs(projectedDefaultPoint[1] - projectedLatTs10Point[1]), 0d, 1e-9);
        Assert.True(Math.Abs(projectedDefaultPoint[0] - projectedLatTs10Point[0]) > 1e-6);
    }

    private static string BuildProjectedWkt(string projectionName, double? latTs)
    {
        string latTsParameter = latTs.HasValue
            ? string.Format(CultureInfo.InvariantCulture, ",PARAMETER[\"lat_ts\",{0}]", latTs.Value)
            : string.Empty;

        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase6-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",SPHEROID[\"Sphere\",6400000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0]{1},UNIT[\"metre\",1]]",
            projectionName,
            latTsParameter);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
