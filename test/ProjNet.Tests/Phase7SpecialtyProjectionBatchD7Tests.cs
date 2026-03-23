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
/// Validates M7 batch D7 specialty projection (<c>spilhaus</c>).
/// </summary>
public class Phase7SpecialtyProjectionBatchD7Tests
{
    private const string Wgs84 = "SPHEROID[\"WGS 84\",6378137,298.257223563]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("spilhaus")]
    [InlineData("Spilhaus")]
    public void SupportsBatchD7AliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, -49.56371678d, 66.94970198d, 40.17823482d, 45d, 1d));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(130.4d, -16.2d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ/GIE forward vectors for default and parameterized Spilhaus.
    /// </summary>
    [Theory]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 1d, 130.4d, -16.2d, 3733410.0118d, -9320.8573d, 5000d)]
    [InlineData(-49.56371678d, 10.1d, 40.17823482d, 45d, 1d, 130.4d, -16.2d, 4343770.7991d, -3701935.6242d, 5000d)]
    [InlineData(30.1d, 66.94970198d, 40.17823482d, 45d, 1d, 130.4d, -16.2d, 3637341.2895d, -2571368.8666d, 5000d)]
    [InlineData(-49.56371678d, 66.94970198d, 9.1d, 45d, 1d, 130.4d, -16.2d, 3061806.4542d, -1678791.7428d, 5000d)]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 40.1d, 1d, 130.4d, -16.2d, 3720561.6630d, 309609.60362d, 5000d)]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 0.9d, 130.4d, -16.2d, 3360069.0106d, -8388.7716d, 5000d)]
    public void MatchesProjBuiltinsForwardVectors(
        double lat0,
        double lon0,
        double azi,
        double rot,
        double k0,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("spilhaus", lat0, lon0, azi, rot, k0));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies inverse vectors for representative Spilhaus cases.
    /// </summary>
    [Theory]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 1d, 3733410.0118d, -9320.8573d, 130.4d, -16.2d, 0.01d)]
    [InlineData(-49.56371678d, 10.1d, 40.17823482d, 45d, 1d, 4343770.7991d, -3701935.6242d, 130.4d, -16.2d, 0.01d)]
    [InlineData(30.1d, 66.94970198d, 40.17823482d, 45d, 1d, 3637341.2895d, -2571368.8666d, 130.4d, -16.2d, 0.01d)]
    public void MatchesProjBuiltinsInverseVectors(
        double lat0,
        double lon0,
        double azi,
        double rot,
        double k0,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("spilhaus", lat0, lon0, azi, rot, k0));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for representative global points.
    /// </summary>
    [Theory]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 1d, -20.1d, 74.1d, 0.05d)]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 1d, -170d, -80d, 0.05d)]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 1d, 173d, 70d, 0.05d)]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 40.1d, 1d, 130.4d, -16.2d, 0.05d)]
    [InlineData(-49.56371678d, 66.94970198d, 40.17823482d, 45d, 0.9d, 130.4d, -16.2d, 0.05d)]
    public void SupportsBatchD7Roundtrip(double lat0, double lon0, double azi, double rot, double k0, double longitude, double latitude, double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt("spilhaus", lat0, lon0, azi, rot, k0));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, tolerance);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, tolerance);
    }

    private static string BuildProjectedWkt(string projectionName, double lat0, double lon0, double azi, double rot, double k0)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-D7-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",{2}],PARAMETER[\"central_meridian\",{3}],PARAMETER[\"scale_factor\",{4}],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],PARAMETER[\"azi\",{5}],PARAMETER[\"rot\",{6}],UNIT[\"metre\",1]]",
            projectionName,
            Wgs84,
            lat0.ToString("R", CultureInfo.InvariantCulture),
            lon0.ToString("R", CultureInfo.InvariantCulture),
            k0.ToString("R", CultureInfo.InvariantCulture),
            azi.ToString("R", CultureInfo.InvariantCulture),
            rot.ToString("R", CultureInfo.InvariantCulture));
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}

