// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Verifies that the built-in static coordinate-system accessors keep their expected metadata.
/// </summary>
public class StaticAccessorConsistencyTests
{
    private static readonly AngularUnitExpectation[] AngularUnitExpectations =
    [
        new(nameof(AngularUnit.Degrees), new InfoExpectation("degree", "EPSG", 9102, "deg", string.Empty, "=pi/180 radians"), 0.017453292519943295769236907684886d),
        new(nameof(AngularUnit.Radian), new InfoExpectation("radian", "EPSG", 9101, "rad", string.Empty, "SI standard unit"), 1d),
        new(nameof(AngularUnit.Grad), new InfoExpectation("grad", "EPSG", 9105, "gr", string.Empty, "=pi/200 radians"), 0.015707963267948966192313216916398d),
        new(nameof(AngularUnit.Gon), new InfoExpectation("gon", "EPSG", 9106, "g", string.Empty, "=pi/200 radians"), 0.015707963267948966192313216916398d),
    ];

    private static readonly LinearUnitExpectation[] LinearUnitExpectations =
    [
        new(nameof(LinearUnit.Metre), new InfoExpectation("metre", "EPSG", 9001, "m", string.Empty, "Also known as International metre. SI standard unit"), 1d),
        new(nameof(LinearUnit.Foot), new InfoExpectation("foot", "EPSG", 9002, "ft", string.Empty, string.Empty), 0.3048d),
        new(nameof(LinearUnit.USSurveyFoot), new InfoExpectation("US survey foot", "EPSG", 9003, "American foot", "ftUS", "Used in USA"), 0.304800609601219d),
        new(nameof(LinearUnit.NauticalMile), new InfoExpectation("nautical mile", "EPSG", 9030, "NM", string.Empty, string.Empty), 1852d),
        new(nameof(LinearUnit.ClarkesFoot), new InfoExpectation("Clarke's foot", "EPSG", 9005, "Clarke's foot", string.Empty, "Assumes Clarke's 1865 ratio"), 0.3047972654d),
    ];

    private static readonly PrimeMeridianExpectation[] PrimeMeridianExpectations =
    [
        new(nameof(PrimeMeridian.Greenwich), new InfoExpectation("Greenwich", "EPSG", 8901, string.Empty, string.Empty, string.Empty), 0d),
        new(nameof(PrimeMeridian.Lisbon), new InfoExpectation("Lisbon", "EPSG", 8902, string.Empty, string.Empty, string.Empty), -9.0754862d),
        new(nameof(PrimeMeridian.Paris), new InfoExpectation("Paris", "EPSG", 8903, string.Empty, string.Empty, "Value adopted by IGN (Paris) in 1936"), 2.5969213d),
        new(nameof(PrimeMeridian.Bogota), new InfoExpectation("Bogota", "EPSG", 8904, string.Empty, string.Empty, string.Empty), -74.04513d),
        new(nameof(PrimeMeridian.Madrid), new InfoExpectation("Madrid", "EPSG", 8905, string.Empty, string.Empty, string.Empty), -3.411658d),
        new(nameof(PrimeMeridian.Rome), new InfoExpectation("Rome", "EPSG", 8906, string.Empty, string.Empty, string.Empty), 12.27084d),
        new(nameof(PrimeMeridian.Bern), new InfoExpectation("Bern", "EPSG", 8907, string.Empty, string.Empty, "1895 value"), 7.26225d),
        new(nameof(PrimeMeridian.Jakarta), new InfoExpectation("Jakarta", "EPSG", 8908, string.Empty, string.Empty, string.Empty), 106.482779d),
        new(nameof(PrimeMeridian.Ferro), new InfoExpectation("Ferro", "EPSG", 8909, string.Empty, string.Empty, "Used in Austria and former Czechoslovakia"), -17.66666666666667d),
        new(nameof(PrimeMeridian.Brussels), new InfoExpectation("Brussels", "EPSG", 8910, string.Empty, string.Empty, string.Empty), 4.220471d),
        new(nameof(PrimeMeridian.Stockholm), new InfoExpectation("Stockholm", "EPSG", 8911, string.Empty, string.Empty, string.Empty), 18.03298d),
        new(nameof(PrimeMeridian.Athens), new InfoExpectation("Athens", "EPSG", 8912, string.Empty, string.Empty, "Used in Greece for older mapping based on Hatt projection"), 23.4258815d),
        new(nameof(PrimeMeridian.Oslo), new InfoExpectation("Oslo", "EPSG", 8913, string.Empty, string.Empty, "Formerly known as Kristiania or Christiania"), 10.43225d),
    ];

    private static readonly EllipsoidExpectation[] EllipsoidExpectations =
    [
        new(nameof(Ellipsoid.Airy1830), new InfoExpectation("Airy 1830", "EPSG", 7001, string.Empty, string.Empty, string.Empty), 6377563.396d, 299.3249646d, true, LinearUnit.Metre),
        new(nameof(Ellipsoid.Bessel1841), new InfoExpectation("Bessel 1841", "EPSG", 7004, string.Empty, string.Empty, string.Empty), 6377397.155d, 299.1528128d, true, LinearUnit.Metre),
        new(nameof(Ellipsoid.WGS84), new InfoExpectation("WGS 84", "EPSG", 7030, "WGS84", string.Empty, "Inverse flattening derived from four defining parameters"), 6378137d, 298.257223563d, true, LinearUnit.Metre),
        new(nameof(Ellipsoid.WGS72), new InfoExpectation("WGS 72", "EPSG", 7043, "WGS 72", string.Empty, string.Empty), 6378135d, 298.26d, true, LinearUnit.Metre),
        new(nameof(Ellipsoid.GRS80), new InfoExpectation("GRS 1980", "EPSG", 7019, "International 1979", string.Empty, "Adopted by IUGG 1979 Canberra"), 6378137d, 298.257222101d, true, LinearUnit.Metre),
        new(nameof(Ellipsoid.International1924), new InfoExpectation("International 1924", "EPSG", 7022, "Hayford 1909", string.Empty, "Described as a=6378388 m"), 6378388d, 297d, true, LinearUnit.Metre),
        new(nameof(Ellipsoid.Clarke1880), new InfoExpectation("Clarke 1880", "EPSG", 7034, "Clarke 1880", string.Empty, "Clarke gave a and b"), 20926202d, 297d, true, LinearUnit.ClarkesFoot),
        new(nameof(Ellipsoid.Clarke1866), new InfoExpectation("Clarke 1866", "EPSG", 7008, "Clarke 1866", string.Empty, "Original definition a=20926062"), 6378206.4d, double.PositiveInfinity, false, LinearUnit.Metre),
        new(nameof(Ellipsoid.Sphere), new InfoExpectation("GRS 1980 Authalic Sphere", "EPSG", 7048, "Sphere", string.Empty, "Authalic sphere derived from GRS 1980 ellipsoid"), 6370997d, double.PositiveInfinity, false, LinearUnit.Metre),
    ];

    private static readonly HorizontalDatumExpectation[] HorizontalDatumExpectations =
    [
        new(nameof(HorizontalDatum.WGS84), new InfoExpectation("World Geodetic System 1984", "EPSG", 6326, string.Empty, string.Empty, "Since 1997, WGS 84 has been maintained within 10cm"), DatumType.HD_Geocentric, Ellipsoid.WGS84, null),
        new(nameof(HorizontalDatum.WGS72), new InfoExpectation("World Geodetic System 1972", "EPSG", 6322, string.Empty, string.Empty, "Used by GPS before 1987"), DatumType.HD_Geocentric, Ellipsoid.WGS72, new Wgs84ConversionInfo(0d, 0d, 4.5d, 0d, 0d, 0.554d, 0.219d)),
        new(nameof(HorizontalDatum.ETRF89), new InfoExpectation("European Terrestrial Reference System 1989", "EPSG", 6258, "ETRF89", string.Empty, "The distinction in usage between ETRF89 and ETRS89 is confused"), DatumType.HD_Geocentric, Ellipsoid.GRS80, new Wgs84ConversionInfo()),
        new(nameof(HorizontalDatum.ED50), new InfoExpectation("European Datum 1950", "EPSG", 6230, "ED50", string.Empty, string.Empty), DatumType.HD_Geocentric, Ellipsoid.International1924, new Wgs84ConversionInfo(-87d, -98d, -121d, 0d, 0d, 0d, 0d)),
    ];

    private static readonly VerticalDatumExpectation[] VerticalDatumExpectations =
    [
        new(nameof(VerticalDatum.ODN), new InfoExpectation("Ordnance Datum Newlyn", "EPSG", 5101, string.Empty, string.Empty, string.Empty), DatumType.VD_GeoidModelDerived),
    ];

    /// <summary>
    /// Returns the angular-unit expectations for theory-based tests.
    /// </summary>
    /// <returns>The angular-unit expectation rows.</returns>
    public static IEnumerable<object[]> GetAngularUnitExpectations()
    {
        return AngularUnitExpectations.Select(static expectation => new object[]
        {
            expectation.PropertyName,
            expectation.Info.Name,
            expectation.Info.Authority,
            expectation.Info.AuthorityCode,
            expectation.Info.Alias,
            expectation.Info.Abbreviation,
            expectation.Info.RemarksFragment,
            expectation.RadiansPerUnit,
        });
    }

    /// <summary>
    /// Returns the linear-unit expectations for theory-based tests.
    /// </summary>
    /// <returns>The linear-unit expectation rows.</returns>
    public static IEnumerable<object[]> GetLinearUnitExpectations()
    {
        return LinearUnitExpectations.Select(static expectation => new object[]
        {
            expectation.PropertyName,
            expectation.Info.Name,
            expectation.Info.Authority,
            expectation.Info.AuthorityCode,
            expectation.Info.Alias,
            expectation.Info.Abbreviation,
            expectation.Info.RemarksFragment,
            expectation.MetersPerUnit,
        });
    }

    /// <summary>
    /// Returns the prime-meridian expectations for theory-based tests.
    /// </summary>
    /// <returns>The prime-meridian expectation rows.</returns>
    public static IEnumerable<object[]> GetPrimeMeridianExpectations()
    {
        return PrimeMeridianExpectations.Select(static expectation => new object[]
        {
            expectation.PropertyName,
            expectation.Info.Name,
            expectation.Info.Authority,
            expectation.Info.AuthorityCode,
            expectation.Info.Alias,
            expectation.Info.Abbreviation,
            expectation.Info.RemarksFragment,
            expectation.Longitude,
        });
    }

    /// <summary>
    /// Returns the ellipsoid expectations for theory-based tests.
    /// </summary>
    /// <returns>The ellipsoid expectation rows.</returns>
    public static IEnumerable<object[]> GetEllipsoidExpectations()
    {
        return EllipsoidExpectations.Select(static expectation => new object[]
        {
            expectation.PropertyName,
            expectation.Info.Name,
            expectation.Info.Authority,
            expectation.Info.AuthorityCode,
            expectation.Info.Alias,
            expectation.Info.Abbreviation,
            expectation.Info.RemarksFragment,
            expectation.SemiMajorAxis,
            expectation.InverseFlattening,
            expectation.IsIvfDefinitive,
            GetLinearUnitAccessorName(expectation.AxisUnit),
        });
    }

    /// <summary>
    /// Returns the horizontal-datum expectations for theory-based tests.
    /// </summary>
    /// <returns>The horizontal-datum expectation rows.</returns>
    public static IEnumerable<object[]> GetHorizontalDatumExpectations()
    {
        return HorizontalDatumExpectations.Select(static expectation => new object[]
        {
            expectation.PropertyName,
            expectation.Info.Name,
            expectation.Info.Authority,
            expectation.Info.AuthorityCode,
            expectation.Info.Alias,
            expectation.Info.Abbreviation,
            expectation.Info.RemarksFragment,
            expectation.DatumType,
            GetEllipsoidAccessorName(expectation.Ellipsoid),
            expectation.ExpectedWgs84Parameters is not null,
            expectation.ExpectedWgs84Parameters?.Dx ?? 0d,
            expectation.ExpectedWgs84Parameters?.Dy ?? 0d,
            expectation.ExpectedWgs84Parameters?.Dz ?? 0d,
            expectation.ExpectedWgs84Parameters?.Ex ?? 0d,
            expectation.ExpectedWgs84Parameters?.Ey ?? 0d,
            expectation.ExpectedWgs84Parameters?.Ez ?? 0d,
            expectation.ExpectedWgs84Parameters?.Ppm ?? 0d,
        });
    }

    /// <summary>
    /// Returns the vertical-datum expectations for theory-based tests.
    /// </summary>
    /// <returns>The vertical-datum expectation rows.</returns>
    public static IEnumerable<object[]> GetVerticalDatumExpectations()
    {
        return VerticalDatumExpectations.Select(static expectation => new object[]
        {
            expectation.PropertyName,
            expectation.Info.Name,
            expectation.Info.Authority,
            expectation.Info.AuthorityCode,
            expectation.Info.Alias,
            expectation.Info.Abbreviation,
            expectation.Info.RemarksFragment,
            expectation.DatumType,
        });
    }

    /// <summary>
    /// Verifies that all selected public static property accessors are tracked by this test suite.
    /// </summary>
    [Fact]
    public void PublicStaticAccessorProperties_AreCoveredByExpectationTables()
    {
        Dictionary<Type, string[]> coveredPropertyNames = new()
        {
            [typeof(AngularUnit)] = AngularUnitExpectations.Select(static expectation => expectation.PropertyName).ToArray(),
            [typeof(LinearUnit)] = LinearUnitExpectations.Select(static expectation => expectation.PropertyName).ToArray(),
            [typeof(PrimeMeridian)] = PrimeMeridianExpectations.Select(static expectation => expectation.PropertyName).ToArray(),
            [typeof(Ellipsoid)] = EllipsoidExpectations.Select(static expectation => expectation.PropertyName).ToArray(),
            [typeof(HorizontalDatum)] = HorizontalDatumExpectations.Select(static expectation => expectation.PropertyName).ToArray(),
            [typeof(VerticalDatum)] = VerticalDatumExpectations.Select(static expectation => expectation.PropertyName).ToArray(),
            [typeof(GeographicCoordinateSystem)] = [nameof(GeographicCoordinateSystem.WGS84)],
            [typeof(GeocentricCoordinateSystem)] = [nameof(GeocentricCoordinateSystem.WGS84)],
            [typeof(ProjectedCoordinateSystem)] = [nameof(ProjectedCoordinateSystem.WebMercator)],
            [typeof(VerticalCoordinateSystem)] = [nameof(VerticalCoordinateSystem.ODN)],
        };

        foreach ((Type type, string[] expectedPropertyNames) in coveredPropertyNames)
        {
            string[] actualPropertyNames = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(static property => property.PropertyType == property.DeclaringType)
                .Select(static property => property.Name)
                .OrderBy(static propertyName => propertyName, StringComparer.Ordinal)
                .ToArray();
            string[] expected = expectedPropertyNames
                .OrderBy(static propertyName => propertyName, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected, actualPropertyNames);
        }
    }

    /// <summary>
    /// Verifies that built-in angular-unit accessors expose the expected metadata.
    /// </summary>
    /// <param name="propertyName">The static property name.</param>
    /// <param name="expectedName">The expected display name.</param>
    /// <param name="expectedAuthority">The expected authority.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedAlias">The expected alias.</param>
    /// <param name="expectedAbbreviation">The expected abbreviation.</param>
    /// <param name="expectedRemarksFragment">The expected remarks content or the empty string.</param>
    /// <param name="expectedRadiansPerUnit">The expected radians-per-unit factor.</param>
    [Theory]
    [MemberData(nameof(GetAngularUnitExpectations))]
    public void AngularUnitStatics_HaveExpectedMetadata(
        string propertyName,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode,
        string expectedAlias,
        string expectedAbbreviation,
        string expectedRemarksFragment,
        double expectedRadiansPerUnit)
    {
        AngularUnit unit = GetStaticPropertyValue<AngularUnit>(typeof(AngularUnit), propertyName);

        AssertInfoMetadata(unit, new InfoExpectation(expectedName, expectedAuthority, expectedAuthorityCode, expectedAlias, expectedAbbreviation, expectedRemarksFragment));
        Assert.Equal(expectedRadiansPerUnit, unit.RadiansPerUnit, 15);
    }

    /// <summary>
    /// Verifies that built-in linear-unit accessors expose the expected metadata.
    /// </summary>
    /// <param name="propertyName">The static property name.</param>
    /// <param name="expectedName">The expected display name.</param>
    /// <param name="expectedAuthority">The expected authority.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedAlias">The expected alias.</param>
    /// <param name="expectedAbbreviation">The expected abbreviation.</param>
    /// <param name="expectedRemarksFragment">The expected remarks content or the empty string.</param>
    /// <param name="expectedMetersPerUnit">The expected meters-per-unit factor.</param>
    [Theory]
    [MemberData(nameof(GetLinearUnitExpectations))]
    public void LinearUnitStatics_HaveExpectedMetadata(
        string propertyName,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode,
        string expectedAlias,
        string expectedAbbreviation,
        string expectedRemarksFragment,
        double expectedMetersPerUnit)
    {
        LinearUnit unit = GetStaticPropertyValue<LinearUnit>(typeof(LinearUnit), propertyName);

        AssertInfoMetadata(unit, new InfoExpectation(expectedName, expectedAuthority, expectedAuthorityCode, expectedAlias, expectedAbbreviation, expectedRemarksFragment));
        Assert.Equal(expectedMetersPerUnit, unit.MetersPerUnit, 15);
    }

    /// <summary>
    /// Verifies that built-in prime-meridian accessors expose the expected metadata.
    /// </summary>
    /// <param name="propertyName">The static property name.</param>
    /// <param name="expectedName">The expected display name.</param>
    /// <param name="expectedAuthority">The expected authority.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedAlias">The expected alias.</param>
    /// <param name="expectedAbbreviation">The expected abbreviation.</param>
    /// <param name="expectedRemarksFragment">The expected remarks content or the empty string.</param>
    /// <param name="expectedLongitude">The expected longitude in degrees.</param>
    [Theory]
    [MemberData(nameof(GetPrimeMeridianExpectations))]
    public void PrimeMeridianStatics_HaveExpectedMetadata(
        string propertyName,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode,
        string expectedAlias,
        string expectedAbbreviation,
        string expectedRemarksFragment,
        double expectedLongitude)
    {
        PrimeMeridian meridian = GetStaticPropertyValue<PrimeMeridian>(typeof(PrimeMeridian), propertyName);

        AssertInfoMetadata(meridian, new InfoExpectation(expectedName, expectedAuthority, expectedAuthorityCode, expectedAlias, expectedAbbreviation, expectedRemarksFragment));
        Assert.Equal(expectedLongitude, meridian.Longitude, 12);
        Assert.True(meridian.AngularUnit.EqualParams(AngularUnit.Degrees));
    }

    /// <summary>
    /// Verifies that built-in ellipsoid accessors expose the expected metadata.
    /// </summary>
    /// <param name="propertyName">The static property name.</param>
    /// <param name="expectedName">The expected display name.</param>
    /// <param name="expectedAuthority">The expected authority.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedAlias">The expected alias.</param>
    /// <param name="expectedAbbreviation">The expected abbreviation.</param>
    /// <param name="expectedRemarksFragment">The expected remarks content or the empty string.</param>
    /// <param name="expectedSemiMajorAxis">The expected semi-major axis.</param>
    /// <param name="expectedInverseFlattening">The expected inverse flattening.</param>
    /// <param name="expectedIsIvfDefinitive">The expected IVF-definitive flag.</param>
    /// <param name="expectedAxisUnitName">The expected axis-unit accessor name.</param>
    [Theory]
    [MemberData(nameof(GetEllipsoidExpectations))]
    public void EllipsoidStatics_HaveExpectedMetadata(
        string propertyName,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode,
        string expectedAlias,
        string expectedAbbreviation,
        string expectedRemarksFragment,
        double expectedSemiMajorAxis,
        double expectedInverseFlattening,
        bool expectedIsIvfDefinitive,
        string expectedAxisUnitName)
    {
        Ellipsoid ellipsoid = GetStaticPropertyValue<Ellipsoid>(typeof(Ellipsoid), propertyName);
        LinearUnit expectedAxisUnit = GetStaticPropertyValue<LinearUnit>(typeof(LinearUnit), expectedAxisUnitName);

        AssertInfoMetadata(ellipsoid, new InfoExpectation(expectedName, expectedAuthority, expectedAuthorityCode, expectedAlias, expectedAbbreviation, expectedRemarksFragment));
        Assert.Equal(expectedSemiMajorAxis, ellipsoid.SemiMajorAxis, 12);
        Assert.Equal(expectedInverseFlattening, ellipsoid.InverseFlattening, 12);
        Assert.Equal(expectedIsIvfDefinitive, ellipsoid.IsIvfDefinitive);
        Assert.True(ellipsoid.AxisUnit.EqualParams(expectedAxisUnit));
    }

    /// <summary>
    /// Verifies that built-in horizontal-datum accessors expose the expected metadata.
    /// </summary>
    /// <param name="propertyName">The static property name.</param>
    /// <param name="expectedName">The expected display name.</param>
    /// <param name="expectedAuthority">The expected authority.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedAlias">The expected alias.</param>
    /// <param name="expectedAbbreviation">The expected abbreviation.</param>
    /// <param name="expectedRemarksFragment">The expected remarks content or the empty string.</param>
    /// <param name="expectedDatumType">The expected datum type.</param>
    /// <param name="expectedEllipsoidName">The expected ellipsoid accessor name.</param>
    /// <param name="hasExpectedWgs84Parameters"><see langword="true"/> when Bursa-Wolf parameters are expected.</param>
    /// <param name="expectedDx">The expected X translation.</param>
    /// <param name="expectedDy">The expected Y translation.</param>
    /// <param name="expectedDz">The expected Z translation.</param>
    /// <param name="expectedEx">The expected X rotation.</param>
    /// <param name="expectedEy">The expected Y rotation.</param>
    /// <param name="expectedEz">The expected Z rotation.</param>
    /// <param name="expectedPpm">The expected ppm scale term.</param>
    [Theory]
    [MemberData(nameof(GetHorizontalDatumExpectations))]
    public void HorizontalDatumStatics_HaveExpectedMetadata(
        string propertyName,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode,
        string expectedAlias,
        string expectedAbbreviation,
        string expectedRemarksFragment,
        DatumType expectedDatumType,
        string expectedEllipsoidName,
        bool hasExpectedWgs84Parameters,
        double expectedDx,
        double expectedDy,
        double expectedDz,
        double expectedEx,
        double expectedEy,
        double expectedEz,
        double expectedPpm)
    {
        HorizontalDatum datum = GetStaticPropertyValue<HorizontalDatum>(typeof(HorizontalDatum), propertyName);
        Ellipsoid expectedEllipsoid = GetStaticPropertyValue<Ellipsoid>(typeof(Ellipsoid), expectedEllipsoidName);
        Wgs84ConversionInfo? expectedParameters = hasExpectedWgs84Parameters
            ? new Wgs84ConversionInfo(expectedDx, expectedDy, expectedDz, expectedEx, expectedEy, expectedEz, expectedPpm)
            : null;

        AssertInfoMetadata(datum, new InfoExpectation(expectedName, expectedAuthority, expectedAuthorityCode, expectedAlias, expectedAbbreviation, expectedRemarksFragment));
        Assert.Equal(expectedDatumType, datum.DatumType);
        Assert.True(datum.Ellipsoid.EqualParams(expectedEllipsoid));
        AssertWgs84Parameters(datum.Wgs84Parameters, expectedParameters);
    }

    /// <summary>
    /// Verifies that the predefined vertical datum keeps its expected metadata.
    /// </summary>
    /// <param name="propertyName">The static property name.</param>
    /// <param name="expectedName">The expected display name.</param>
    /// <param name="expectedAuthority">The expected authority.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedAlias">The expected alias.</param>
    /// <param name="expectedAbbreviation">The expected abbreviation.</param>
    /// <param name="expectedRemarksFragment">The expected remarks content or the empty string.</param>
    /// <param name="expectedDatumType">The expected datum type.</param>
    [Theory]
    [MemberData(nameof(GetVerticalDatumExpectations))]
    public void VerticalDatumStatics_HaveExpectedMetadata(
        string propertyName,
        string expectedName,
        string expectedAuthority,
        long expectedAuthorityCode,
        string expectedAlias,
        string expectedAbbreviation,
        string expectedRemarksFragment,
        DatumType expectedDatumType)
    {
        VerticalDatum datum = GetStaticPropertyValue<VerticalDatum>(typeof(VerticalDatum), propertyName);

        AssertInfoMetadata(datum, new InfoExpectation(expectedName, expectedAuthority, expectedAuthorityCode, expectedAlias, expectedAbbreviation, expectedRemarksFragment));
        Assert.Equal(expectedDatumType, datum.DatumType);
    }

    /// <summary>
    /// Verifies that the predefined geographic WGS84 accessor keeps its expected metadata and axis order.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystemWgs84_HasExpectedMetadata()
    {
        GeographicCoordinateSystem coordinateSystem = GeographicCoordinateSystem.WGS84;

        AssertInfoMetadata(coordinateSystem, new InfoExpectation("WGS 84", "EPSG", 4326, string.Empty, string.Empty, string.Empty));
        Assert.True(coordinateSystem.AngularUnit.EqualParams(AngularUnit.Degrees));
        Assert.True(coordinateSystem.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(coordinateSystem.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich));
        AssertAxis(coordinateSystem.GetAxis(0), "Lon", AxisOrientationEnum.East);
        AssertAxis(coordinateSystem.GetAxis(1), "Lat", AxisOrientationEnum.North);
    }

    /// <summary>
    /// Verifies that the predefined geocentric WGS84 accessor keeps its expected metadata and axis order.
    /// </summary>
    [Fact]
    public void GeocentricCoordinateSystemWgs84_HasExpectedMetadata()
    {
        GeocentricCoordinateSystem coordinateSystem = GeocentricCoordinateSystem.WGS84;

        AssertInfoMetadata(coordinateSystem, new InfoExpectation("WGS 84", "EPSG", 4978, string.Empty, string.Empty, string.Empty));
        Assert.True(coordinateSystem.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(coordinateSystem.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.True(coordinateSystem.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich));
        AssertAxis(coordinateSystem.GetAxis(0), "Geocentric X (X)", AxisOrientationEnum.Other);
        AssertAxis(coordinateSystem.GetAxis(1), "Geocentric Y (Y)", AxisOrientationEnum.East);
        AssertAxis(coordinateSystem.GetAxis(2), "Geocentric Z (Z)", AxisOrientationEnum.North);
    }

    /// <summary>
    /// Verifies that the predefined Web Mercator accessor keeps its expected metadata and normalization.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystemWebMercator_HasExpectedMetadata()
    {
        ProjectedCoordinateSystem coordinateSystem = ProjectedCoordinateSystem.WebMercator;
        Projection projection = Assert.IsType<Projection>(coordinateSystem.Projection);

        AssertInfoMetadata(
            coordinateSystem,
            new InfoExpectation(
                "WGS 84 / Pseudo-Mercator",
                "EPSG",
                3857,
                "WGS 84 / Popular Visualisation Pseudo-Mercator",
                "WebMercator",
                "spherical development of ellipsoidal coordinates"));
        Assert.True(coordinateSystem.GeographicCoordinateSystem.EqualParams(GeographicCoordinateSystem.WGS84));
        Assert.True(coordinateSystem.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(coordinateSystem.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.Equal("Popular Visualisation Pseudo-Mercator", projection.ClassName);
        AssertAxis(coordinateSystem.GetAxis(0), "East", AxisOrientationEnum.East);
        AssertAxis(coordinateSystem.GetAxis(1), "North", AxisOrientationEnum.North);
    }

    /// <summary>
    /// Verifies that the predefined WGS84 UTM helper keeps its expected metadata for both hemispheres.
    /// </summary>
    /// <param name="zone">The UTM zone.</param>
    /// <param name="zoneIsNorth"><see langword="true"/> for the northern hemisphere; otherwise <see langword="false"/>.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedName">The expected coordinate-system name.</param>
    [Theory]
    [InlineData(32, true, 32632L, "WGS 84 / UTM zone 32N")]
    [InlineData(32, false, 32732L, "WGS 84 / UTM zone 32S")]
    public void ProjectedCoordinateSystemWgs84Utm_HasExpectedMetadata(int zone, bool zoneIsNorth, long expectedAuthorityCode, string expectedName)
    {
        var coordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(zone, zoneIsNorth);
        Projection projection = Assert.IsType<Projection>(coordinateSystem.Projection);

        AssertInfoMetadata(
            coordinateSystem,
            new InfoExpectation(
                expectedName,
                "EPSG",
                expectedAuthorityCode,
                string.Empty,
                string.Empty,
                "Large and medium scale topographic mapping and engineering survey"));
        Assert.True(coordinateSystem.GeographicCoordinateSystem.EqualParams(GeographicCoordinateSystem.WGS84));
        Assert.True(coordinateSystem.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(coordinateSystem.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.Equal("Transverse_Mercator", projection.ClassName);
        AssertAxis(coordinateSystem.GetAxis(0), "East", AxisOrientationEnum.East);
        AssertAxis(coordinateSystem.GetAxis(1), "North", AxisOrientationEnum.North);
    }

    /// <summary>
    /// Verifies that the predefined ODN vertical coordinate system keeps its expected metadata.
    /// </summary>
    [Fact]
    public void VerticalCoordinateSystemOdn_HasExpectedMetadata()
    {
        VerticalCoordinateSystem coordinateSystem = VerticalCoordinateSystem.ODN;

        AssertInfoMetadata(coordinateSystem, new InfoExpectation("Newlyn", "EPSG", 5701, string.Empty, "ODN", string.Empty));
        Assert.True(coordinateSystem.VerticalDatum.EqualParams(VerticalDatum.ODN));
        Assert.True(coordinateSystem.LinearUnit.EqualParams(LinearUnit.Metre));
        AssertAxis(coordinateSystem.GetAxis(0), "Up", AxisOrientationEnum.Up);
    }

    private static T GetStaticPropertyValue<T>(Type declaringType, string propertyName)
    {
        PropertyInfo property = Assert.IsType<PropertyInfo>(
            declaringType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static),
            exactMatch: false);
        return Assert.IsType<T>(property.GetValue(null));
    }

    private static void AssertInfoMetadata(Info info, InfoExpectation expectation)
    {
        Assert.Equal(expectation.Name, info.Name);
        Assert.Equal(expectation.Authority, info.Authority);
        Assert.Equal(expectation.AuthorityCode, info.AuthorityCode);
        Assert.Equal(expectation.Alias, info.Alias);
        Assert.Equal(expectation.Abbreviation, info.Abbreviation);
        AssertRemarks(info.Remarks, expectation.RemarksFragment);
    }

    private static void AssertRemarks(string actual, string expectedFragment)
    {
        if (expectedFragment.Length == 0)
        {
            Assert.Equal(string.Empty, actual);
            return;
        }

        Assert.Contains(expectedFragment, actual, StringComparison.Ordinal);
    }

    private static void AssertAxis(AxisInfo axis, string expectedName, AxisOrientationEnum expectedOrientation)
    {
        Assert.Equal(expectedName, axis.Name);
        Assert.Equal(expectedOrientation, axis.Orientation);
    }

    private static void AssertWgs84Parameters(Wgs84ConversionInfo? actual, Wgs84ConversionInfo? expected)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(actual);
        Assert.Equal(expected.Dx, parameters.Dx, 12);
        Assert.Equal(expected.Dy, parameters.Dy, 12);
        Assert.Equal(expected.Dz, parameters.Dz, 12);
        Assert.Equal(expected.Ex, parameters.Ex, 12);
        Assert.Equal(expected.Ey, parameters.Ey, 12);
        Assert.Equal(expected.Ez, parameters.Ez, 12);
        Assert.Equal(expected.Ppm, parameters.Ppm, 12);
    }

    private static string GetEllipsoidAccessorName(Ellipsoid ellipsoid)
    {
        if (ellipsoid.EqualParams(Ellipsoid.WGS84))
        {
            return nameof(Ellipsoid.WGS84);
        }

        if (ellipsoid.EqualParams(Ellipsoid.WGS72))
        {
            return nameof(Ellipsoid.WGS72);
        }

        if (ellipsoid.EqualParams(Ellipsoid.GRS80))
        {
            return nameof(Ellipsoid.GRS80);
        }

        if (ellipsoid.EqualParams(Ellipsoid.Airy1830))
        {
            return nameof(Ellipsoid.Airy1830);
        }

        if (ellipsoid.EqualParams(Ellipsoid.Bessel1841))
        {
            return nameof(Ellipsoid.Bessel1841);
        }

        if (ellipsoid.EqualParams(Ellipsoid.International1924))
        {
            return nameof(Ellipsoid.International1924);
        }

        throw new InvalidOperationException($"No ellipsoid accessor mapping was configured for '{ellipsoid.Name}'.");
    }

    private static string GetLinearUnitAccessorName(LinearUnit unit)
    {
        if (unit.EqualParams(LinearUnit.Metre))
        {
            return nameof(LinearUnit.Metre);
        }

        if (unit.EqualParams(LinearUnit.ClarkesFoot))
        {
            return nameof(LinearUnit.ClarkesFoot);
        }

        throw new InvalidOperationException($"No linear-unit accessor mapping was configured for '{unit.Name}'.");
    }

    /// <summary>
    /// Expected metadata for an <see cref="Info"/>-derived static accessor.
    /// </summary>
    /// <param name="Name">The expected display name.</param>
    /// <param name="Authority">The expected authority.</param>
    /// <param name="AuthorityCode">The expected authority code.</param>
    /// <param name="Alias">The expected alias.</param>
    /// <param name="Abbreviation">The expected abbreviation.</param>
    /// <param name="RemarksFragment">The expected remarks content or the empty string.</param>
    private sealed record InfoExpectation(
        string Name,
        string Authority,
        long AuthorityCode,
        string Alias,
        string Abbreviation,
        string RemarksFragment);

    /// <summary>
    /// Expected metadata for an angular-unit static accessor.
    /// </summary>
    /// <param name="PropertyName">The static property name.</param>
    /// <param name="Info">The shared info metadata expectation.</param>
    /// <param name="RadiansPerUnit">The expected radians-per-unit factor.</param>
    private sealed record AngularUnitExpectation(string PropertyName, InfoExpectation Info, double RadiansPerUnit);

    /// <summary>
    /// Expected metadata for a linear-unit static accessor.
    /// </summary>
    /// <param name="PropertyName">The static property name.</param>
    /// <param name="Info">The shared info metadata expectation.</param>
    /// <param name="MetersPerUnit">The expected meters-per-unit factor.</param>
    private sealed record LinearUnitExpectation(string PropertyName, InfoExpectation Info, double MetersPerUnit);

    /// <summary>
    /// Expected metadata for a prime-meridian static accessor.
    /// </summary>
    /// <param name="PropertyName">The static property name.</param>
    /// <param name="Info">The shared info metadata expectation.</param>
    /// <param name="Longitude">The expected longitude.</param>
    private sealed record PrimeMeridianExpectation(string PropertyName, InfoExpectation Info, double Longitude);

    /// <summary>
    /// Expected metadata for an ellipsoid static accessor.
    /// </summary>
    /// <param name="PropertyName">The static property name.</param>
    /// <param name="Info">The shared info metadata expectation.</param>
    /// <param name="SemiMajorAxis">The expected semi-major axis.</param>
    /// <param name="InverseFlattening">The expected inverse flattening.</param>
    /// <param name="IsIvfDefinitive">The expected IVF-definitive flag.</param>
    /// <param name="AxisUnit">The expected axis unit.</param>
    private sealed record EllipsoidExpectation(
        string PropertyName,
        InfoExpectation Info,
        double SemiMajorAxis,
        double InverseFlattening,
        bool IsIvfDefinitive,
        LinearUnit AxisUnit);

    /// <summary>
    /// Expected metadata for a horizontal-datum static accessor.
    /// </summary>
    /// <param name="PropertyName">The static property name.</param>
    /// <param name="Info">The shared info metadata expectation.</param>
    /// <param name="DatumType">The expected datum type.</param>
    /// <param name="Ellipsoid">The expected ellipsoid.</param>
    /// <param name="ExpectedWgs84Parameters">The expected Bursa-Wolf parameters, or <see langword="null"/>.</param>
    private sealed record HorizontalDatumExpectation(
        string PropertyName,
        InfoExpectation Info,
        DatumType DatumType,
        Ellipsoid Ellipsoid,
        Wgs84ConversionInfo? ExpectedWgs84Parameters);

    /// <summary>
    /// Expected metadata for a vertical-datum static accessor.
    /// </summary>
    /// <param name="PropertyName">The static property name.</param>
    /// <param name="Info">The shared info metadata expectation.</param>
    /// <param name="DatumType">The expected datum type.</param>
    private sealed record VerticalDatumExpectation(
        string PropertyName,
        InfoExpectation Info,
        DatumType DatumType);
}
