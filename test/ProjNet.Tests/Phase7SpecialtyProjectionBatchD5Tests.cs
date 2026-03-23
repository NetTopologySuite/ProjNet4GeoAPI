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
/// Validates M7 batch D5 specialty projections.
/// </summary>
public class Phase7SpecialtyProjectionBatchD5Tests
{
    private const string Sphere6400000 = "SPHEROID[\"Sphere\",6400000,0]";

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for batch D5 projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("putp1")]
    [InlineData("Putnins_P1")]
    [InlineData("putp3p")]
    [InlineData("Putnins_P3P")]
    [InlineData("putp5p")]
    [InlineData("Putnins_P5P")]
    [InlineData("putp6p")]
    [InlineData("Putnins_P6P")]
    [InlineData("kav7")]
    [InlineData("Kavrayskiy_VII")]
    [InlineData("wag4")]
    [InlineData("Wagner_IV")]
    [InlineData("wag5")]
    [InlineData("Wagner_V")]
    [InlineData("wag6")]
    [InlineData("Wagner_VI")]
    [InlineData("weren")]
    [InlineData("Werenskiold_I")]
    [InlineData("vandg2")]
    [InlineData("Van_Der_Grinten_II")]
    [InlineData("vandg3")]
    [InlineData("Van_Der_Grinten_III")]
    [InlineData("vandg4")]
    [InlineData("Van_Der_Grinten_IV")]
    public void SupportsBatchD5AliasesFromWkt(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for batch D5 projections.
    /// </summary>
    [Theory]
    [InlineData("putp1", 2d, 1d, 211642.762754160d, 105831.180787330d, 1e-6d)]
    [InlineData("putp3p", 2d, 1d, 178238.118539985d, 89124.560786088d, 1e-6d)]
    [InlineData("putp5p", 2d, 1d, 226388.175248756d, 113204.568558475d, 1e-6d)]
    [InlineData("putp6p", 2d, 1d, 198034.195132195d, 125989.475461323d, 1e-6d)]
    [InlineData("kav7", 2d, 1d, 193462.974943729d, 111701.072127637d, 1e-6d)]
    [InlineData("wag4", 2d, 1d, 192801.218662384d, 129416.216394803d, 1e-6d)]
    [InlineData("wag5", 2d, 1d, 203227.051925325d, 138651.631442713d, 1e-6d)]
    [InlineData("wag6", 2d, 1d, 223391.801323985d, 111701.072127637d, 1e-6d)]
    [InlineData("weren", 2d, 1d, 223378.515757634d, 146214.093042288d, 1e-6d)]
    [InlineData("vandg2", 2d, 1d, 223395.247850437d, 111718.491037226d, 1e-4d)]
    [InlineData("vandg3", 2d, 1d, 223395.249552831d, 111704.519904421d, 1e-6d)]
    [InlineData("vandg4", 2d, 1d, 223374.577294355d, 111701.195484154d, 1e-6d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, tolerance);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable batch D5 projections.
    /// </summary>
    [Theory]
    [InlineData("putp1", 200d, 100d, 0.001889802d, 0.000944901d, 2e-9d)]
    [InlineData("putp3p", 200d, 100d, 0.002244050d, 0.001122025d, 2e-9d)]
    [InlineData("putp5p", 200d, 100d, 0.001766713d, 0.000883357d, 2e-9d)]
    [InlineData("putp6p", 200d, 100d, 0.002019551d, 0.000793716d, 2e-9d)]
    [InlineData("kav7", 200d, 100d, 0.002067483d, 0.000895247d, 2e-9d)]
    [InlineData("wag4", 200d, 100d, 0.002074503d, 0.000772683d, 2e-9d)]
    [InlineData("wag5", 200d, 100d, 0.001968072d, 0.000721216d, 2e-9d)]
    [InlineData("wag6", 200d, 100d, 0.001790493d, 0.000895247d, 2e-9d)]
    [InlineData("weren", 200d, 100d, 0.001790493d, 0.000683918d, 2e-9d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude,
        double tolerance)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, tolerance);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, tolerance);
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable batch D5 projections.
    /// </summary>
    [Theory]
    [InlineData("putp1", 2d, 1d)]
    [InlineData("putp3p", 2d, 1d)]
    [InlineData("putp5p", 2d, 1d)]
    [InlineData("putp6p", 2d, 1d)]
    [InlineData("kav7", 2d, 1d)]
    [InlineData("wag4", 2d, 1d)]
    [InlineData("wag5", 2d, 1d)]
    [InlineData("wag6", 2d, 1d)]
    [InlineData("weren", 2d, 1d)]
    public void SupportsBatchD5Roundtrip(string projectionName, double longitude, double latitude)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-7d);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-7d);
    }

    /// <summary>
    /// Verifies van der Grinten II/III/IV remain forward-only.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    [Theory]
    [InlineData("vandg2")]
    [InlineData("vandg3")]
    [InlineData("vandg4")]
    public void VanDerGrintenVariantsDoNotSupportInverse(string projectionName)
    {
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(200d, 100d)));
    }

    private static string BuildProjectedWkt(string projectionName)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase7-D5-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            Sphere6400000);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}

