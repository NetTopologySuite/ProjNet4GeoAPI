// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for <see cref="AxisOrderHelper"/>.
/// </summary>
public class AxisOrderHelperTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();

    /// <summary>
    /// Verifies that <see cref="AxisOrderHelper.TryCreateAxisSwapTransform"/> returns <see langword="false"/>
    /// and outputs <see langword="null"/> when the source coordinate system is <see langword="null"/>.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithNullSourceReturnsFalse()
    {
        CoordinateSystem source = null!;
        CoordinateSystem target = GeographicCoordinateSystem.WGS84;

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform);
    }

    /// <summary>
    /// Verifies that <see cref="AxisOrderHelper.TryCreateAxisSwapTransform"/> returns <see langword="false"/>
    /// and outputs <see langword="null"/> when both coordinate systems are one-dimensional (vertical).
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithOneDimensionalSystemsReturnsFalse()
    {
        VerticalCoordinateSystem source = CreateVerticalCoordinateSystem("Vertical source", AxisOrientationEnum.Up);
        VerticalCoordinateSystem target = CreateVerticalCoordinateSystem("Vertical target", AxisOrientationEnum.Down);

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform);
    }

    /// <summary>
    /// Verifies that <see cref="AxisOrderHelper.TryCreateAxisSwapTransform"/> returns <see langword="false"/>
    /// and outputs <see langword="null"/> when the target coordinate system uses an unsupported axis orientation
    /// (geocentric).
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithUnsupportedTargetOrientationReturnsFalse()
    {
        GeographicCoordinateSystem source = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        GeocentricCoordinateSystem target = CoordinateSystemFactory.CreateGeocentricCoordinateSystem(
            "Target geocentric",
            HorizontalDatum.WGS84,
            LinearUnit.Metre,
            PrimeMeridian.Greenwich);

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform);
    }

    /// <summary>
    /// Verifies that <see cref="AxisOrderHelper.TryCreateAxisSwapTransform"/> returns <see langword="false"/>
    /// and outputs <see langword="null"/> when the source coordinate system has a duplicated axis role that
    /// prevents an unambiguous mapping.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithMissingSourceRoleReturnsFalse()
    {
        GeographicCoordinateSystem sourceHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source duplicate horizontal",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.East));

        GeographicCoordinateSystem targetHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target normal horizontal",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        VerticalCoordinateSystem vertical = CreateVerticalCoordinateSystem("Vertical", AxisOrientationEnum.Up);
        CompoundCoordinateSystem source = CoordinateSystemFactory.CreateCompoundCoordinateSystem("Source", sourceHorizontal, vertical);
        CompoundCoordinateSystem target = CoordinateSystemFactory.CreateCompoundCoordinateSystem("Target", targetHorizontal, vertical);

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.False(ok);
        Assert.Null(transform);
    }

    /// <summary>
    /// Verifies that <see cref="AxisOrderHelper.TryCreateAxisSwapTransform"/> returns <see langword="true"/>
    /// and produces a transform that negates the vertical coordinate when the source vertical axis is
    /// <see cref="AxisOrientationEnum.Up"/> and the target is <see cref="AxisOrientationEnum.Down"/>.
    /// </summary>
    [Fact]
    public void TryCreateAxisSwapTransformWithUpToDownTargetCreatesVerticalSignFlip()
    {
        GeographicCoordinateSystem sourceHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Source EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        GeographicCoordinateSystem targetHorizontal = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Target EN",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        CompoundCoordinateSystem source = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "Source ENU",
            sourceHorizontal,
            CreateVerticalCoordinateSystem("Source up", AxisOrientationEnum.Up));

        CompoundCoordinateSystem target = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "Target END",
            targetHorizontal,
            CreateVerticalCoordinateSystem("Target down", AxisOrientationEnum.Down));

        bool ok = AxisOrderHelper.TryCreateAxisSwapTransform(source, target, out MathTransform? transform);

        Assert.True(ok);
        double[] transformed = Assert.IsType<MathTransform>(transform, exactMatch: false).Transform([10d, 20d, 30d]);

        Assert.Equal(10d, transformed[0], 12);
        Assert.Equal(20d, transformed[1], 12);
        Assert.Equal(-30d, transformed[2], 12);
    }

    private static VerticalCoordinateSystem CreateVerticalCoordinateSystem(string name, AxisOrientationEnum orientation)
    {
        VerticalDatum datum = CoordinateSystemFactory.CreateVerticalDatum($"{name} datum", DatumType.VD_Other);
        return CoordinateSystemFactory.CreateVerticalCoordinateSystem(name, datum, LinearUnit.Metre, new AxisInfo("V", orientation));
    }
}
