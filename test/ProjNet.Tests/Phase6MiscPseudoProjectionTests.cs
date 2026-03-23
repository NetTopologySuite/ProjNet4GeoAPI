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
/// Validates M6 wave 4 pseudo-cylindrical projections.
/// </summary>
public class Phase6MiscPseudoProjectionTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    /// <summary>
    /// Verifies aliases resolve from WKT for wave 4 projections.
    /// </summary>
    /// <param name="projectionName">Projection alias.</param>
    [Theory]
    [InlineData("gall")]
    [InlineData("Gall_Stereographic")]
    [InlineData("crast")]
    [InlineData("Craster_Parabolic")]
    [InlineData("fahey")]
    [InlineData("collg")]
    [InlineData("Collignon")]
    [InlineData("boggs")]
    [InlineData("Boggs_Eumorphic")]
    [InlineData("hatano")]
    [InlineData("Hatano_Asymmetrical_Equal_Area")]
    [InlineData("nell")]
    [InlineData("nell_h")]
    [InlineData("Nell_Hammer")]
    [InlineData("nicol")]
    [InlineData("Nicolosi_Globular")]
    [InlineData("times")]
    [InlineData("Times_Projection")]
    public void SupportsWave4AliasesFromWkt(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, false));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(2d, 1d));

        Assert.NotNull(projected);
        Assert.NotNull(forward);
        Assert.NotNull(projectedPoint);
        Assert.True(projectedPoint.Length >= 2);
    }

    /// <summary>
    /// Verifies PROJ builtins forward vectors for wave 4 projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    /// <param name="expectedX">Expected x meters.</param>
    /// <param name="expectedY">Expected y meters.</param>
    [Theory]
    [InlineData("gall", 2d, 1d, 157969.171134520d, 95345.249178386d)]
    [InlineData("crast", 2d, 1d, 218280.142056781d, 114306.045604280d)]
    [InlineData("fahey", 2d, 1d, 182993.344649124d, 101603.193569884d)]
    [InlineData("collg", 2d, 1d, 249872.921577930d, 99423.174788460d)]
    [InlineData("boggs", 2d, 1d, 211949.700808182d, 117720.998305411d)]
    [InlineData("hatano", 2d, 1d, 189878.878946528d, 131409.802440626d)]
    [InlineData("nell", 2d, 1d, 223385.132504696d, 111698.236447187d)]
    [InlineData("nell_h", 2d, 1d, 223385.131640953d, 111698.236533562d)]
    [InlineData("nicol", 2d, 1d, 223374.561814140d, 111732.553988545d)]
    [InlineData("times", 25d, -10d, 2065971.530107881d, -951526.064849459d)]
    public void MatchesProjBuiltinsForwardVectors(
        string projectionName,
        double longitude,
        double latitude,
        double expectedX,
        double expectedY)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        bool useSphereEllps = projectionName.Equals("times", StringComparison.OrdinalIgnoreCase);
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, useSphereEllps));
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected.GeographicCoordinateSystem, projected);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));

        Assert.InRange(Math.Abs(projectedPoint[0] - expectedX), 0d, 1e-7);
        Assert.InRange(Math.Abs(projectedPoint[1] - expectedY), 0d, 1e-7);
    }

    /// <summary>
    /// Verifies PROJ builtins inverse vectors for inverse-capable wave 4 projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="x">Input x meters.</param>
    /// <param name="y">Input y meters.</param>
    /// <param name="expectedLongitude">Expected longitude degrees.</param>
    /// <param name="expectedLatitude">Expected latitude degrees.</param>
    [Theory]
    [InlineData("gall", 200d, 100d, 0.002532140d, 0.001048847d)]
    [InlineData("crast", 200d, 100d, 0.001832259d, 0.000874839d)]
    [InlineData("fahey", 200d, 100d, 0.002185789d, 0.000984246d)]
    [InlineData("collg", 200d, 100d, 0.001586797d, 0.001010173d)]
    [InlineData("hatano", 200d, 100d, 0.002106462d, 0.000760957d)]
    [InlineData("nell", 200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData("nell_h", 200d, 100d, 0.001790493d, 0.000895247d)]
    [InlineData("times", 2065971.530107881d, -951526.064849459d, 25d, -10d)]
    public void MatchesProjBuiltinsInverseVectors(
        string projectionName,
        double x,
        double y,
        double expectedLongitude,
        double expectedLatitude)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        bool useSphereEllps = projectionName.Equals("times", StringComparison.OrdinalIgnoreCase);
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, useSphereEllps));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);
        double[] geographicPoint = inverse.MathTransform.Transform(CreatePoint(x, y));

        Assert.InRange(Math.Abs(geographicPoint[0] - expectedLongitude), 0d, 2e-9);
        Assert.InRange(Math.Abs(geographicPoint[1] - expectedLatitude), 0d, 2e-9);
    }

    /// <summary>
    /// Verifies Boggs and Nicolosi remain forward-only in this wave.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    [Theory]
    [InlineData("boggs")]
    [InlineData("nicol")]
    public void ForwardOnlyProjectionsDoNotSupportInverse(string projectionName)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, false));
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, projected.GeographicCoordinateSystem);

        Assert.Throws<InvalidOperationException>(() => inverse.MathTransform.Transform(CreatePoint(200d, 100d)));
    }

    /// <summary>
    /// Verifies roundtrip stability for inverse-capable wave 4 projections.
    /// </summary>
    /// <param name="projectionName">Projection code.</param>
    /// <param name="longitude">Input longitude degrees.</param>
    /// <param name="latitude">Input latitude degrees.</param>
    [Theory]
    [InlineData("gall", 2d, 1d)]
    [InlineData("crast", -2d, -1d)]
    [InlineData("fahey", 2d, -1d)]
    [InlineData("collg", -2d, 1d)]
    [InlineData("hatano", 2d, 1d)]
    [InlineData("nell", -2d, -1d)]
    [InlineData("nell_h", 2d, -1d)]
    [InlineData("times", -35d, 20d)]
    public void SupportsWave4Roundtrip(string projectionName, double longitude, double latitude)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        bool useSphereEllps = projectionName.Equals("times", StringComparison.OrdinalIgnoreCase);
        var projected = (ProjectedCoordinateSystem)CoordinateSystemFactory.CreateFromWkt(BuildProjectedWkt(projectionName, useSphereEllps));
        var geographic = projected.GeographicCoordinateSystem;
        var forward = CoordinateTransformationFactory.CreateFromCoordinateSystems(geographic, projected);
        var inverse = CoordinateTransformationFactory.CreateFromCoordinateSystems(projected, geographic);
        double[] projectedPoint = forward.MathTransform.Transform(CreatePoint(longitude, latitude));
        double[] roundtrip = inverse.MathTransform.Transform(projectedPoint);

        Assert.InRange(Math.Abs(roundtrip[0] - longitude), 0d, 1e-9);
        Assert.InRange(Math.Abs(roundtrip[1] - latitude), 0d, 1e-9);
    }

    private static string BuildProjectedWkt(string projectionName, bool useSphereEllps)
    {
        ArgumentNullException.ThrowIfNull(projectionName);

        string spheroidClause = useSphereEllps
            ? "SPHEROID[\"Sphere\",6370997,0]"
            : "SPHEROID[\"Sphere\",6400000,0]";

        return string.Format(
            CultureInfo.InvariantCulture,
            "PROJCS[\"Phase6-{0}\",GEOGCS[\"GIE\",DATUM[\"GIE_Datum\",{1}],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"{0}\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]",
            projectionName,
            spheroidClause);
    }

    private static double[] CreatePoint(double x, double y) => [x, y];
}
