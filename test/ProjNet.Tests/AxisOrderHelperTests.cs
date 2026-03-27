// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class AxisOrderHelperTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithNullSourceReturnsFalse()
    {
        CoordinateSystem source = null!;
        CoordinateSystem target = GeographicCoordinateSystem.WGS84;

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform as object);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithOneDimensionalSystemsReturnsFalse()
    {
        var source = CreateVerticalCoordinateSystem("Vertical source", AxisOrientationEnum.Up);
        var target = CreateVerticalCoordinateSystem("Vertical target", AxisOrientationEnum.Down);

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform as object);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithUnsupportedTargetOrientationReturnsFalse()
    {
        var source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var target = CoordinateSystemFactory.CreateGeocentricCoordinateSystem(
            "Target geocentric",
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich);

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform as object);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithMissingSourceRoleReturnsFalse()
    {
        var sourceHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source duplicate horizontal",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.East));

        var targetHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target normal horizontal",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var vertical = CreateVerticalCoordinateSystem("Vertical", AxisOrientationEnum.Up);
        var source = CoordinateSystemFactory.CreateCompoundCoordinateSystem("Source", sourceHorizontal, vertical);
        var target = CoordinateSystemFactory.CreateCompoundCoordinateSystem("Target", targetHorizontal, vertical);

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform as object);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithUpToDownTargetCreatesVerticalSignFlip()
    {
        var sourceHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var targetHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var source = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "Source ENU",
            sourceHorizontal,
            CreateVerticalCoordinateSystem("Source up", AxisOrientationEnum.Up));

        var target = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "Target END",
            targetHorizontal,
            CreateVerticalCoordinateSystem("Target down", AxisOrientationEnum.Down));

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.True(ok);
        double[] transformed = Assert.IsAssignableFrom<MathTransform>(transform).Transform([10d, 20d, 30d]);

        Assert.Equal(10d, transformed[0], 12);
        Assert.Equal(20d, transformed[1], 12);
        Assert.Equal(-30d, transformed[2], 12);
    }

    private static VerticalCoordinateSystem CreateVerticalCoordinateSystem(string name, AxisOrientationEnum orientation)
    {
        VerticalDatum datum = CoordinateSystemFactory.CreateVerticalDatum(name + " datum", DatumType.VD_Other);
        return CoordinateSystemFactory.CreateVerticalCoordinateSystem(name, datum, LinearUnit.Metre, new AxisInfo("V", orientation));
    }
}

