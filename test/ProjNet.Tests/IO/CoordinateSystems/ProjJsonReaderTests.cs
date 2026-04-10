// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
using ProjNet.IO.CoordinateSystems;
using Xunit;

/// <summary>
/// Verifies native PROJJSON coordinate-system parsing against EPSG-backed catalog references.
/// </summary>
public class ProjJsonReaderTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Srid)
            .ToDictionary(group => group.Key, group => group.Last().Wkt));

    /// <summary>
    /// Provides PROJJSON geographic CRS examples aligned with real EPSG catalog entries.
    /// </summary>
    /// <returns>SRID/PROJJSON pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedGeographicRows()
    {
        object degreeUnit = "degree";
        object gradUnit = AngularUnitObject("grad", 0.015707963267949d, 9105);
        object metreUnit = "metre";

        return
        [
            new TheoryDataRow<int, string>(
                4230,
                Serialize(
                    GeographicCrsObject(
                        4230,
                        "ED50",
                        GeodeticDatumObject("European Datum 1950", EllipsoidObject("International 1924", 6378388d, 297d, metreUnit, 7022), 6230),
                        GreenwichPrimeMeridianObject(),
                        degreeUnit,
                        ("Geodetic latitude", "Lat", "north"),
                        ("Geodetic longitude", "Lon", "east")))),
            new TheoryDataRow<int, string>(
                4277,
                Serialize(
                    GeographicCrsObject(
                        4277,
                        "OSGB36",
                        GeodeticDatumObject("Ordnance Survey of Great Britain 1936", EllipsoidObject("Airy 1830", 6377563.396d, 299.3249646d, metreUnit, 7001), 6277),
                        GreenwichPrimeMeridianObject(),
                        degreeUnit,
                        ("Geodetic latitude", "Lat", "north"),
                        ("Geodetic longitude", "Lon", "east")))),
            new TheoryDataRow<int, string>(
                4314,
                Serialize(
                    GeographicCrsObject(
                        4314,
                        "DHDN",
                        GeodeticDatumObject("Deutsches Hauptdreiecksnetz", EllipsoidObject("Bessel 1841", 6377397.155d, 299.1528128d, metreUnit, 7004), 6314),
                        GreenwichPrimeMeridianObject(),
                        degreeUnit,
                        ("Geodetic latitude", "Lat", "north"),
                        ("Geodetic longitude", "Lon", "east")))),
            new TheoryDataRow<int, string>(
                4322,
                Serialize(
                    GeographicCrsObject(
                        4322,
                        "WGS 72",
                        GeodeticDatumObject("World Geodetic System 1972", EllipsoidObject("WGS 72", 6378135d, 298.26d, metreUnit, 7043), 6322),
                        GreenwichPrimeMeridianObject(),
                        degreeUnit,
                        ("Geodetic latitude", "Lat", "north"),
                        ("Geodetic longitude", "Lon", "east")))),
            new TheoryDataRow<int, string>(
                4807,
                Serialize(
                    GeographicCrsObject(
                        4807,
                        "NTF (Paris)",
                        GeodeticDatumObject("Nouvelle Triangulation Francaise (Paris)", EllipsoidObject("Clarke 1880 (IGN)", 6378249.2d, 293.466021293627d, metreUnit, 7011), 6807),
                        PrimeMeridianObject("Paris", 0.040792344d, degreeUnit, 8903, useIds: true, stringCode: true),
                        gradUnit,
                        ("Geodetic latitude (Lat)", "Lat", "north"),
                        ("Geodetic longitude (Lon)", "Lon", "east"),
                        useIds: true))),
        ];
    }

    /// <summary>
    /// Provides PROJJSON projected CRS examples aligned with real EPSG catalog entries.
    /// </summary>
    /// <returns>SRID/PROJJSON pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedProjectedRows()
    {
        object degreeUnit = "degree";
        object metreUnit = "metre";
        object unityUnit = ScaleUnitObject();

        return
        [
            new TheoryDataRow<int, string>(
                27700,
                Serialize(
                    ProjectedCrsObject(
                        27700,
                        "OSGB36 / British National Grid",
                        GeographicCrsObject(
                            4277,
                            "OSGB36",
                            GeodeticDatumObject("Ordnance Survey of Great Britain 1936", EllipsoidObject("Airy 1830", 6377563.396d, 299.3249646d, metreUnit, 7001), 6277),
                            GreenwichPrimeMeridianObject(),
                            degreeUnit,
                            ("Geodetic latitude", "Lat", "north"),
                            ("Geodetic longitude", "Lon", "east")),
                        ConversionObject(
                            "British National Grid",
                            "Transverse Mercator",
                            19916,
                            ProjectionParameterObject("Latitude of natural origin", 49d, degreeUnit, 8801),
                            ProjectionParameterObject("Longitude of natural origin", -2d, degreeUnit, 8802),
                            ProjectionParameterObject("Scale factor at natural origin", 0.9996012717d, unityUnit, 8805),
                            ProjectionParameterObject("False easting", 400000d, metreUnit, 8806),
                            ProjectionParameterObject("False northing", -100000d, metreUnit, 8807)),
                        metreUnit,
                        ("Easting", "E", "east"),
                        ("Northing", "N", "north")))),
            new TheoryDataRow<int, string>(
                31370,
                Serialize(
                    ProjectedCrsObject(
                        31370,
                        "BD72 / Belgian Lambert 72",
                        GeographicCrsObject(
                            4313,
                            "BD72",
                            GeodeticDatumObject("Reseau National Belge 1972", EllipsoidObject("International 1924", 6378388d, 297d, metreUnit, 7022), 6313),
                            GreenwichPrimeMeridianObject(),
                            degreeUnit,
                            ("Geodetic latitude", "Lat", "north"),
                            ("Geodetic longitude", "Lon", "east")),
                        ConversionObject(
                            "Belgian Lambert 72",
                            "Lambert Conic Conformal (2SP)",
                            19961,
                            ProjectionParameterObject("Latitude of false origin", 90d, degreeUnit, 8821),
                            ProjectionParameterObject("Longitude of false origin", 4.36748666666694d, degreeUnit, 8822),
                            ProjectionParameterObject("Latitude of 1st standard parallel", 51.1666672333336d, degreeUnit, 8823),
                            ProjectionParameterObject("Latitude of 2nd standard parallel", 49.8333339000003d, degreeUnit, 8824),
                            ProjectionParameterObject("Easting at false origin", 150000.013d, metreUnit, 8826),
                            ProjectionParameterObject("Northing at false origin", 5400088.438d, metreUnit, 8827)),
                        metreUnit,
                        ("Easting", "X", "east"),
                        ("Northing", "Y", "north")))),
            new TheoryDataRow<int, string>(
                2169,
                Serialize(
                    ProjectedCrsObject(
                        2169,
                        "LUREF / Luxembourg TM",
                        GeographicCrsObject(
                            4181,
                            "LUREF",
                            GeodeticDatumObject("Luxembourg Reference Frame", EllipsoidObject("International 1924", 6378388d, 297d, metreUnit, 7022), 6181),
                            GreenwichPrimeMeridianObject(),
                            degreeUnit,
                            ("Geodetic latitude", "Lat", "north"),
                            ("Geodetic longitude", "Lon", "east")),
                        ConversionObject(
                            "Luxembourg TM",
                            "Transverse Mercator",
                            19966,
                            ProjectionParameterObject("Latitude of natural origin", 49.8333333333336d, degreeUnit, 8801),
                            ProjectionParameterObject("Longitude of natural origin", 6.16666666666694d, degreeUnit, 8802),
                            ProjectionParameterObject("Scale factor at natural origin", 1d, unityUnit, 8805),
                            ProjectionParameterObject("False easting", 80000d, metreUnit, 8806),
                            ProjectionParameterObject("False northing", 100000d, metreUnit, 8807)),
                        metreUnit,
                        ("Northing", "X", "north"),
                        ("Easting", "Y", "east")))),
            new TheoryDataRow<int, string>(
                23032,
                Serialize(
                    ProjectedCrsObject(
                        23032,
                        "ED50 / UTM zone 32N",
                        GeographicCrsObject(
                            4230,
                            "ED50",
                            GeodeticDatumObject("European Datum 1950", EllipsoidObject("International 1924", 6378388d, 297d, metreUnit, 7022), 6230),
                            GreenwichPrimeMeridianObject(),
                            degreeUnit,
                            ("Geodetic latitude", "Lat", "north"),
                            ("Geodetic longitude", "Lon", "east")),
                        ConversionObject(
                            "UTM zone 32N",
                            "Transverse Mercator",
                            16032,
                            ProjectionParameterObject("Latitude of natural origin", 0d, degreeUnit, 8801),
                            ProjectionParameterObject("Longitude of natural origin", 9d, degreeUnit, 8802),
                            ProjectionParameterObject("Scale factor at natural origin", 0.9996d, unityUnit, 8805),
                            ProjectionParameterObject("False easting", 500000d, metreUnit, 8806),
                            ProjectionParameterObject("False northing", 0d, metreUnit, 8807)),
                        metreUnit,
                        ("Easting", "E", "east"),
                        ("Northing", "N", "north")))),
            new TheoryDataRow<int, string>(
                31467,
                Serialize(
                    ProjectedCrsObject(
                        31467,
                        "DHDN / 3-degree Gauss-Kruger zone 3",
                        GeographicCrsObject(
                            4314,
                            "DHDN",
                            GeodeticDatumObject("Deutsches Hauptdreiecksnetz", EllipsoidObject("Bessel 1841", 6377397.155d, 299.1528128d, metreUnit, 7004), 6314),
                            GreenwichPrimeMeridianObject(),
                            degreeUnit,
                            ("Geodetic latitude", "Lat", "north"),
                            ("Geodetic longitude", "Lon", "east")),
                        ConversionObject(
                            "3-degree Gauss-Kruger zone 3",
                            "Transverse Mercator",
                            16263,
                            ProjectionParameterObject("Latitude of natural origin", 0d, degreeUnit, 8801),
                            ProjectionParameterObject("Longitude of natural origin", 9d, degreeUnit, 8802),
                            ProjectionParameterObject("Scale factor at natural origin", 1d, unityUnit, 8805),
                            ProjectionParameterObject("False easting", 3500000d, metreUnit, 8806),
                            ProjectionParameterObject("False northing", 0d, metreUnit, 8807)),
                        metreUnit,
                        ("Northing", "X", "north"),
                        ("Easting", "Y", "east")))),
        ];
    }

    /// <summary>
    /// Provides PROJJSON geocentric, vertical, and compound CRS examples aligned with real EPSG catalog entries.
    /// </summary>
    /// <returns>SRID/PROJJSON pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string, Type>> SupportedRemainingRows()
    {
        object degreeUnit = "degree";
        object metreUnit = "metre";

        return
        [
            new TheoryDataRow<int, string, Type>(
                4978,
                Serialize(
                    GeocentricCrsObject(
                        4978,
                        "WGS 84",
                        GeodeticDatumObject("World Geodetic System 1984", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, metreUnit, 7030), 6326),
                        GreenwichPrimeMeridianObject(),
                        metreUnit)),
                typeof(GeocentricCoordinateSystem)),
            new TheoryDataRow<int, string, Type>(
                5701,
                Serialize(
                    VerticalCrsObject(
                        5701,
                        "Newlyn",
                        VerticalDatumObject("Ordnance Datum Newlyn", 5101),
                        metreUnit,
                        "Up",
                        "up")),
                typeof(VerticalCoordinateSystem)),
            new TheoryDataRow<int, string, Type>(
                9518,
                Serialize(
                    CompoundCrsObject(
                        9518,
                        "WGS 84 + EGM96 height",
                        GeographicCrsObject(
                            4326,
                            "WGS 84",
                            GeodeticDatumObject("World Geodetic System 1984", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, metreUnit, 7030), 6326),
                            GreenwichPrimeMeridianObject(),
                            degreeUnit,
                            ("Geodetic latitude", "Lat", "north"),
                            ("Geodetic longitude", "Lon", "east")),
                        VerticalCrsObject(
                            5773,
                            "EGM96 height",
                            VerticalDatumObject("EGM96 geoid", -1),
                            metreUnit,
                            "Gravity-related height",
                            "up"))),
                typeof(CompoundCoordinateSystem)),
        ];
    }

    /// <summary>
    /// Provides representative unsupported top-level PROJJSON type values from the milestone-40 coverage matrix.
    /// </summary>
    /// <returns>Type values that should still report the current unsupported boundary explicitly.</returns>
    public static IEnumerable<TheoryDataRow<string>> UnsupportedTopLevelTypeRows()
    {
        return
        [
            new TheoryDataRow<string>("CoordinateMetadata"),
            new TheoryDataRow<string>("EngineeringCRS"),
            new TheoryDataRow<string>("ParametricCRS"),
            new TheoryDataRow<string>("TimeCRS"),
            new TheoryDataRow<string>("DerivedVerticalCRS"),
            new TheoryDataRow<string>("ConcatenatedOperation"),
        ];
    }

    /// <summary>
    /// Verifies supported PROJJSON geographic CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="json">PROJJSON CRS definition.</param>
    [Theory]
    [MemberData(nameof(SupportedGeographicRows))]
    public void Parse_ParsesSupportedGeographicCrsEquivalentToCatalogReference(int srid, string json)
    {
        GeographicCoordinateSystem parsed = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));
        GeographicCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.True(parsed.EqualParams(reference), $"PROJJSON geographic CRS parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported PROJJSON projected CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="json">PROJJSON CRS definition.</param>
    [Theory]
    [MemberData(nameof(SupportedProjectedRows))]
    public void Parse_ParsesSupportedProjectedCrsEquivalentToCatalogReference(int srid, string json)
    {
        ProjectedCoordinateSystem parsed = Assert.IsType<ProjectedCoordinateSystem>(ProjJsonReader.Parse(json));
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.True(parsed.EqualParams(reference), $"PROJJSON projected CRS parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported remaining PROJJSON CRS types parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="json">PROJJSON CRS definition.</param>
    /// <param name="expectedType">Expected coordinate system type.</param>
    [Theory]
    [MemberData(nameof(SupportedRemainingRows))]
    public void Parse_ParsesSupportedRemainingCrsEquivalentToCatalogReference(int srid, string json, Type expectedType)
    {
        CoordinateSystem parsed = Assert.IsType<CoordinateSystem>(ProjJsonReader.Parse(json), exactMatch: false);
        CoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.Equal(expectedType, parsed.GetType());
        Assert.Equal(expectedType, reference.GetType());
        Assert.True(parsed.EqualParams(reference), $"PROJJSON remaining CRS parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported PROJJSON <c>BoundCRS</c> definitions parse into the first-class bound model.
    /// </summary>
    [Fact]
    public void Parse_ParsesSupportedBoundCrs()
    {
        object degreeUnit = "degree";
        string json = Serialize(
            BoundCrsObject(
                4269,
                "NAD83",
                GeographicCrsObject(
                    4269,
                    "NAD83",
                    GeodeticDatumObject("North American Datum 1983", EllipsoidObject("GRS 1980", 6378137d, 298.257222101d, "metre", 7019), 6269),
                    GreenwichPrimeMeridianObject(),
                    degreeUnit,
                    ("Geodetic latitude", "Lat", "north"),
                    ("Geodetic longitude", "Lon", "east")),
                GeographicCrsObject(
                    4326,
                    "WGS 84",
                    GeodeticDatumObject("World Geodetic System 1984", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, "metre", 7030), 6326),
                    GreenwichPrimeMeridianObject(),
                    degreeUnit,
                    ("Geodetic latitude", "Lat", "north"),
                    ("Geodetic longitude", "Lon", "east")),
                AbridgedTransformationObject(
                    "NAD83 to WGS 84 (1)",
                    "Geocentric translations",
                    BoundParameterValueObject("X-axis translation", 0d),
                    BoundParameterValueObject("Y-axis translation", 0d),
                    BoundParameterValueObject("Z-axis translation", 0d))));

        BoundCoordinateSystem parsed = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));
        GeographicCoordinateSystem source = Assert.IsType<GeographicCoordinateSystem>(parsed.SourceCoordinateSystem);
        GeographicCoordinateSystem target = Assert.IsType<GeographicCoordinateSystem>(parsed.TargetCoordinateSystem);
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(parsed.Transformation.Wgs84Parameters);

        Assert.Equal("NAD83", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4269, parsed.AuthorityCode);
        Assert.Equal("North American Datum 1983", source.HorizontalDatum.Name);
        Assert.Null(source.HorizontalDatum.Wgs84Parameters);
        Assert.Equal("WGS 84", target.Name);
        Assert.True(target.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(target.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich));
        Assert.True(target.AngularUnit.EqualParams(AngularUnit.Degrees));
        Assert.Equal(AxisOrientationEnum.North, target.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.East, target.GetAxis(1).Orientation);
        Assert.Equal("Geocentric translations", parsed.Transformation.MethodName);
        Assert.Equal(new Wgs84ConversionInfo(0, 0, 0, 0, 0, 0, 0), parameters);
    }

    /// <summary>
    /// Verifies supported PROJJSON vertical <c>BoundCRS</c> definitions parse into the first-class bound model.
    /// </summary>
    [Fact]
    public void Parse_ParsesSupportedVerticalBoundCrs()
    {
        string json = Serialize(
            BoundCrsObject(
                3855,
                "EGM2008 height",
                VerticalCrsObject(
                    3855,
                    "EGM2008 height",
                    VerticalDatumObject("EGM2008 geoid", 1027),
                    "metre",
                    "Gravity-related height",
                    "up"),
                CompoundCrsObject(
                    9518,
                    "WGS 84 + ODN height",
                    GeographicCrsObject(
                        4326,
                        "WGS 84",
                        GeodeticDatumObject("World Geodetic System 1984", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, "metre", 7030), 6326),
                        GreenwichPrimeMeridianObject(),
                        "degree",
                        ("Geodetic latitude", "Lat", "north"),
                        ("Geodetic longitude", "Lon", "east")),
                    VerticalCrsObject(
                        5701,
                        "ODN height",
                        VerticalDatumObject("Ordnance Datum Newlyn", 5101),
                        "metre",
                        "Gravity-related height",
                        "up")),
                AbridgedTransformationObject(
                    "WGS 84 to EGM2008 height",
                    "Geographic3D to GravityRelatedHeight (EGM)",
                    BoundParameterValueObject("Geoid (height correction) model file", "egm96_15.gtx"))));

        BoundCoordinateSystem parsed = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));
        VerticalCoordinateSystem source = Assert.IsType<VerticalCoordinateSystem>(parsed.SourceCoordinateSystem);
        CompoundCoordinateSystem target = Assert.IsType<CompoundCoordinateSystem>(parsed.TargetCoordinateSystem);

        Assert.Equal("EGM2008 height", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(3855, parsed.AuthorityCode);
        Assert.Equal("EGM2008 geoid", source.VerticalDatum.Name);
        Assert.Null(source.BoundGridTransformation);
        Assert.Equal("Geographic3D to GravityRelatedHeight (EGM)", parsed.Transformation.MethodName);
        Assert.Equal("egm96_15.gtx", parsed.Transformation.ParameterFileName);
        Assert.Equal(3, target.Dimension);
    }

    /// <summary>
    /// Verifies projected PROJJSON parsing preserves base CRS prime meridian and angular unit semantics.
    /// </summary>
    [Fact]
    public void Parse_ParsesProjectedCrsBasePrimeMeridianAndAngularUnit()
    {
        object metreUnit = "metre";
        object gradUnit = AngularUnitObject("grad", 0.015707963267949d, 9105);
        string json = Serialize(
            ProjectedCrsObject(
                27561,
                "NTF (Paris) / Lambert Nord France",
                GeographicCrsObject(
                    4807,
                    "NTF (Paris)",
                    GeodeticDatumObject("Nouvelle Triangulation Francaise (Paris)", EllipsoidObject("Clarke 1880 (IGN)", 6378249.2d, 293.466021293627d, metreUnit, 7011), 6807),
                    PrimeMeridianObject("Paris", 0.040792344d, AngularUnitObject("radian", 1d, 9101), 8903),
                    gradUnit,
                    ("Geodetic latitude", "Lat", "north"),
                    ("Geodetic longitude", "Lon", "east")),
                ConversionObject(
                    "Lambert Nord France",
                    "Lambert Conic Conformal (1SP)",
                    18091,
                    ProjectionParameterObject("Latitude of natural origin", 55d, gradUnit, 8801),
                    ProjectionParameterObject("Longitude of natural origin", 0d, gradUnit, 8802),
                    ProjectionParameterObject("Scale factor at natural origin", 0.999877341d, ScaleUnitObject(), 8805),
                    ProjectionParameterObject("False easting", 600000d, metreUnit, 8806),
                    ProjectionParameterObject("False northing", 200000d, metreUnit, 8807)),
                metreUnit,
                ("Easting", "X", "east"),
                ("Northing", "Y", "north")));

        ProjectedCoordinateSystem parsed = Assert.IsType<ProjectedCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.Equal("NTF (Paris) / Lambert Nord France", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(27561, parsed.AuthorityCode);
        Assert.Equal("grad", parsed.GeographicCoordinateSystem.AngularUnit.Name);
        Assert.Equal("EPSG", parsed.GeographicCoordinateSystem.AngularUnit.Authority);
        Assert.Equal(9105, parsed.GeographicCoordinateSystem.AngularUnit.AuthorityCode);
        Assert.Equal("Paris", parsed.GeographicCoordinateSystem.PrimeMeridian.Name);
        Assert.True(parsed.GeographicCoordinateSystem.PrimeMeridian.AngularUnit.EqualParams(AngularUnit.Radian));
        Assert.Equal(0.040792344d, parsed.GeographicCoordinateSystem.PrimeMeridian.Longitude);
        Assert.Equal("Lambert Conic Conformal (1SP)", parsed.Projection.ClassName);
        Assert.Equal("EPSG", parsed.Projection.Authority);
        Assert.Equal(18091, parsed.Projection.AuthorityCode);
        Assert.Equal(55d, parsed.Projection.GetParameter("latitude_of_origin")?.Value);
        Assert.Equal(0d, parsed.Projection.GetParameter("central_meridian")?.Value);
        Assert.Equal(0.999877341d, parsed.Projection.GetParameter("scale_factor")?.Value);
        Assert.Equal(600000d, parsed.Projection.GetParameter("false_easting")?.Value);
        Assert.Equal(200000d, parsed.Projection.GetParameter("false_northing")?.Value);
    }

    /// <summary>
    /// Verifies PROJJSON identifiers can be sourced from an <c>ids</c> array and prefer the EPSG identifier.
    /// </summary>
    [Fact]
    public void Parse_WithIdsArray_PrefersEpsgIdentifier()
    {
        object[] ids =
        [
            IdObject("IGNF", "NTFP"),
            IdObject("EPSG", 4807),
        ];

        string json = Serialize(
            Obj(
                ("type", "GeographicCRS"),
                ("name", "NTF (Paris)"),
                ("datum", GeodeticDatumObject("Nouvelle Triangulation Francaise (Paris)", EllipsoidObject("Clarke 1880 (IGN)", 6378249.2d, 293.466021293627d, "metre", 7011), 6807)),
                ("prime_meridian", PrimeMeridianObject("Paris", 0.040792344d, AngularUnitObject("radian", 1d, 9101), 8903, useIds: true, stringCode: true)),
                ("coordinate_system", CoordinateSystemObject("ellipsoidal", AxisObject("Geodetic latitude", "Lat", "north", "degree"), AxisObject("Geodetic longitude", "Lon", "east", "degree"))),
                ("ids", ids)));

        GeographicCoordinateSystem parsed = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4807, parsed.AuthorityCode);
        Assert.Equal("EPSG", parsed.PrimeMeridian.Authority);
        Assert.Equal(8903, parsed.PrimeMeridian.AuthorityCode);
    }

    /// <summary>
    /// Verifies optional usage metadata blocks are tolerated on PROJJSON CRS objects.
    /// </summary>
    [Fact]
    public void Parse_ParsesProjectedCrsWithUsageMetadataEquivalentToCatalogReference()
    {
        Dictionary<string, object?> baseCrs = GeographicCrsObject(
            4277,
            "OSGB36",
            GeodeticDatumObject("Ordnance Survey of Great Britain 1936", EllipsoidObject("Airy 1830", 6377563.396d, 299.3249646d, "metre", 7001), 6277),
            GreenwichPrimeMeridianObject(),
            "degree",
            ("Geodetic latitude", "Lat", "north"),
            ("Geodetic longitude", "Lon", "east"));

        Dictionary<string, object?> conversion = ConversionObject(
            "British National Grid",
            "Transverse Mercator",
            19916,
            ProjectionParameterObject("Latitude of natural origin", 49d, "degree", 8801),
            ProjectionParameterObject("Longitude of natural origin", -2d, "degree", 8802),
            ProjectionParameterObject("Scale factor at natural origin", 0.9996012717d, ScaleUnitObject(), 8805),
            ProjectionParameterObject("False easting", 400000d, "metre", 8806),
            ProjectionParameterObject("False northing", -100000d, "metre", 8807));

        object[] usages =
        [
            Obj(
                ("scope", "Topographic mapping."),
                ("area", "United Kingdom."),
                ("bbox", Obj(("south_latitude", 49.75d), ("west_longitude", -9.01d), ("north_latitude", 61.01d), ("east_longitude", 2.01d)))),
        ];

        string json = Serialize(
            Obj(
                ("type", "ProjectedCRS"),
                ("name", "OSGB36 / British National Grid"),
                ("base_crs", baseCrs),
                ("conversion", conversion),
                ("coordinate_system", CoordinateSystemObject("Cartesian", AxisObject("Easting", "E", "east", "metre"), AxisObject("Northing", "N", "north", "metre"))),
                ("scope", "Engineering survey, topographic mapping."),
                ("area", "United Kingdom."),
                ("bbox", Obj(("south_latitude", 49.75d), ("west_longitude", -9.01d), ("north_latitude", 61.01d), ("east_longitude", 2.01d))),
                ("remarks", "metadata remark"),
                ("usages", usages),
                ("id", IdObject("EPSG", 27700))));

        ProjectedCoordinateSystem parsed = Assert.IsType<ProjectedCoordinateSystem>(ProjJsonReader.Parse(json));
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(27700));

        Assert.True(parsed.EqualParams(reference));
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(27700, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies geographic PROJJSON <c>datum_ensemble</c> objects retain ensemble metadata on the parsed datum.
    /// </summary>
    [Fact]
    public void Parse_WithGeographicDatumEnsemble_RetainsEnsembleMetadata()
    {
        object[] members =
        [
            DatumEnsembleMemberObject("World Geodetic System 1984 (Transit)", "EPSG", 1166),
            DatumEnsembleMemberObject("World Geodetic System 1984 (G730)", "EPSG", 1152),
        ];

        string json = Serialize(
            Obj(
                ("type", "GeographicCRS"),
                ("name", "WGS 84"),
                ("datum_ensemble", DatumEnsembleObject("World Geodetic System 1984 ensemble", members, "2", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, "metre", 7030), IdObject("EPSG", 6326))),
                ("coordinate_system", CoordinateSystemObject("ellipsoidal", AxisObject("Geodetic latitude", "Lat", "north", "degree"), AxisObject("Geodetic longitude", "Lon", "east", "degree"))),
                ("id", IdObject("EPSG", 4326))));

        GeographicCoordinateSystem parsed = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));
        DatumEnsemble ensemble = Assert.IsType<DatumEnsemble>(parsed.HorizontalDatum.Ensemble);

        Assert.Equal("World Geodetic System 1984 ensemble", parsed.HorizontalDatum.Name);
        Assert.Equal("EPSG", parsed.HorizontalDatum.Authority);
        Assert.Equal(6326, parsed.HorizontalDatum.AuthorityCode);
        Assert.Equal(2, ensemble.Members.Count);
        Assert.Equal(2d, ensemble.Accuracy);
        Assert.NotNull(ensemble.Ellipsoid);
        Assert.True(parsed.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
    }

    /// <summary>
    /// Verifies ETRS89-style PROJJSON <c>datum_ensemble</c> objects retain their identifier and ellipsoid metadata.
    /// </summary>
    [Fact]
    public void Parse_WithEtrs89DatumEnsemble_RetainsIdentifierAndEllipsoid()
    {
        object[] members =
        [
            DatumEnsembleMemberObject("European Terrestrial Reference Frame 1989", "EPSG", 1178),
            DatumEnsembleMemberObject("European Terrestrial Reference Frame 1990", "EPSG", 1179),
        ];

        string json = Serialize(
            Obj(
                ("type", "GeographicCRS"),
                ("name", "ETRS89"),
                ("datum_ensemble", DatumEnsembleObject("European Terrestrial Reference System 1989 ensemble", members, "0.1", EllipsoidObject("GRS 1980", 6378137d, 298.257222101d, "metre", 7019), IdObject("EPSG", 6258))),
                ("coordinate_system", CoordinateSystemObject("ellipsoidal", AxisObject("Geodetic latitude", "Lat", "north", "degree"), AxisObject("Geodetic longitude", "Lon", "east", "degree"))),
                ("id", IdObject("EPSG", 4258))));

        GeographicCoordinateSystem parsed = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));
        DatumEnsemble ensemble = Assert.IsType<DatumEnsemble>(parsed.HorizontalDatum.Ensemble);

        Assert.Equal("European Terrestrial Reference System 1989 ensemble", parsed.HorizontalDatum.Name);
        Assert.Equal(6258, parsed.HorizontalDatum.AuthorityCode);
        Assert.Equal(0.1d, ensemble.Accuracy);
        Assert.NotNull(ensemble.Ellipsoid);
        Assert.Equal("GRS 1980", Assert.IsType<Ellipsoid>(ensemble.Ellipsoid).Name);
    }

    /// <summary>
    /// Verifies vertical PROJJSON <c>datum_ensemble</c> objects retain ensemble metadata without requiring an ellipsoid.
    /// </summary>
    [Fact]
    public void Parse_WithVerticalDatumEnsemble_RetainsEnsembleMetadata()
    {
        object[] members =
        [
            DatumEnsembleMemberObject("Datum A", "TEST", 1),
            DatumEnsembleMemberObject("Datum B", "TEST", 2),
        ];

        string json = Serialize(
            Obj(
                ("type", "VerticalCRS"),
                ("name", "Example ensemble height"),
                ("datum_ensemble", DatumEnsembleObject("Example vertical ensemble", members, "0.05", null, IdObject("TEST", 1001))),
                ("coordinate_system", CoordinateSystemObject("vertical", AxisObject("Gravity-related height", "H", "up", "metre"))),
                ("id", IdObject("TEST", 2001))));

        VerticalCoordinateSystem parsed = Assert.IsType<VerticalCoordinateSystem>(ProjJsonReader.Parse(json));
        DatumEnsemble ensemble = Assert.IsType<DatumEnsemble>(parsed.VerticalDatum.Ensemble);

        Assert.Equal("Example vertical ensemble", parsed.VerticalDatum.Name);
        Assert.Equal("TEST", parsed.VerticalDatum.Authority);
        Assert.Equal(1001, parsed.VerticalDatum.AuthorityCode);
        Assert.Equal(2, ensemble.Members.Count);
        Assert.Equal(0.05d, ensemble.Accuracy);
        Assert.Null(ensemble.Ellipsoid);
    }

    /// <summary>
    /// Verifies derived geographic PROJJSON objects map onto <see cref="FittedCoordinateSystem"/> with preserved axis metadata.
    /// </summary>
    [Fact]
    public void Parse_WithDerivedGeographicCrs_ReturnsFittedCoordinateSystem()
    {
        string json = Serialize(
            DerivedGeographicCrsObject(
                2001,
                "Local WGS 84",
                GeographicCrsObject(
                    4326,
                    "WGS 84",
                    GeodeticDatumObject("World Geodetic System 1984", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, "metre", 7030), 6326),
                    GreenwichPrimeMeridianObject(),
                    "degree",
                    ("Geodetic latitude", "Lat", "north"),
                    ("Geodetic longitude", "Lon", "east")),
                AffineConversionObject("unnamed", "degree", 0.5d, 1d, 0d, 1.5d, 0d, 1d),
                "degree",
                ("Local latitude", "Lat", "north"),
                ("Local longitude", "Lon", "east")));

        FittedCoordinateSystem parsed = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(json));
        GeographicCoordinateSystem baseCoordinateSystem = Assert.IsType<GeographicCoordinateSystem>(parsed.BaseCoordinateSystem);

        Assert.Equal("Local WGS 84", parsed.Name);
        Assert.Equal("TEST", parsed.Authority);
        Assert.Equal(2001, parsed.AuthorityCode);
        Assert.Equal("WGS 84", baseCoordinateSystem.Name);
        Assert.Equal("Local latitude", parsed.GetAxis(0).Name);
        Assert.Equal("Local longitude", parsed.GetAxis(1).Name);
        Assert.StartsWith("PARAM_MT[\"Affine\"", parsed.ToBase(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies derived projected PROJJSON objects map onto <see cref="FittedCoordinateSystem"/> with a projected base CRS.
    /// </summary>
    [Fact]
    public void Parse_WithDerivedProjectedCrs_ReturnsFittedCoordinateSystem()
    {
        string json = Serialize(
            DerivedProjectedCrsObject(
                3001,
                "Local projected",
                ProjectedCrsObject(
                    32632,
                    "WGS 84 / UTM zone 32N",
                    GeographicCrsObject(
                        4326,
                        "WGS 84",
                        GeodeticDatumObject("World Geodetic System 1984", EllipsoidObject("WGS 84", 6378137d, 298.257223563d, "metre", 7030), 6326),
                        GreenwichPrimeMeridianObject(),
                        "degree",
                        ("Geodetic latitude", "Lat", "north"),
                        ("Geodetic longitude", "Lon", "east")),
                    ConversionObject(
                        "UTM zone 32N",
                        "Transverse Mercator",
                        16032,
                        ProjectionParameterObject("Latitude of natural origin", 0d, "degree", 8801),
                        ProjectionParameterObject("Longitude of natural origin", 9d, "degree", 8802),
                        ProjectionParameterObject("Scale factor at natural origin", 0.9996d, ScaleUnitObject(), 8805),
                        ProjectionParameterObject("False easting", 500000d, "metre", 8806),
                        ProjectionParameterObject("False northing", 0d, "metre", 8807)),
                    "metre",
                    ("Easting", "E", "east"),
                    ("Northing", "N", "north")),
                AffineConversionObject("unnamed", "metre", 100d, 1d, 0d, -50d, 0d, 1d),
                "metre",
                ("Local easting", "X", "east"),
                ("Local northing", "Y", "north")));

        FittedCoordinateSystem parsed = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(json));
        ProjectedCoordinateSystem baseCoordinateSystem = Assert.IsType<ProjectedCoordinateSystem>(parsed.BaseCoordinateSystem);

        Assert.Equal("Local projected", parsed.Name);
        Assert.Equal("TEST", parsed.Authority);
        Assert.Equal(3001, parsed.AuthorityCode);
        Assert.Equal("WGS 84 / UTM zone 32N", baseCoordinateSystem.Name);
        Assert.Equal("Local easting", parsed.GetAxis(0).Name);
        Assert.Equal("Local northing", parsed.GetAxis(1).Name);
        Assert.StartsWith("PARAM_MT[\"Affine\"", parsed.ToBase(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies representative unsupported top-level PROJJSON types remain explicit reader boundaries.
    /// </summary>
    /// <param name="type">The unsupported PROJJSON <c>type</c> value.</param>
    [Theory]
    [MemberData(nameof(UnsupportedTopLevelTypeRows))]
    public void Parse_WithUnsupportedTopLevelType_ThrowsNotSupportedException(string type)
    {
        string json = Serialize(Obj(("type", type), ("name", "Unsupported test object")));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => ProjJsonReader.Parse(json));

        Assert.Contains(type, exception.Message, StringComparison.Ordinal);
    }

    private static string GetCatalogWkt(int srid)
    {
        if (!CatalogDefinitions.Value.TryGetValue(srid, out string? wkt))
        {
            throw new InvalidOperationException($"No catalog definition found for EPSG:{srid}.");
        }

        return wkt;
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value);

    private static Dictionary<string, object?> GeographicCrsObject(
        int srid,
        string name,
        Dictionary<string, object?> datum,
        Dictionary<string, object?> primeMeridian,
        object angularUnit,
        (string Name, string Abbreviation, string Direction) axis1,
        (string Name, string Abbreviation, string Direction) axis2,
        bool useIds = false)
    {
        Dictionary<string, object?> coordinateSystem = CoordinateSystemObject(
            "ellipsoidal",
            AxisObject(axis1.Name, axis1.Abbreviation, axis1.Direction, angularUnit),
            AxisObject(axis2.Name, axis2.Abbreviation, axis2.Direction, angularUnit));

        if (useIds)
        {
            object[] ids =
            [
                IdObject("ESRI", $"GCS_{name.Replace(" ", "_", StringComparison.Ordinal)}"),
                IdObject("EPSG", srid),
            ];

            return Obj(
                ("type", "GeographicCRS"),
                ("name", name),
                ("datum", datum),
                ("prime_meridian", primeMeridian),
                ("coordinate_system", coordinateSystem),
                ("ids", ids));
        }

        return Obj(
            ("type", "GeographicCRS"),
            ("name", name),
            ("datum", datum),
            ("prime_meridian", primeMeridian),
            ("coordinate_system", coordinateSystem),
            ("id", IdObject("EPSG", srid)));
    }

    private static Dictionary<string, object?> ProjectedCrsObject(
        int srid,
        string name,
        Dictionary<string, object?> baseCrs,
        Dictionary<string, object?> conversion,
        object linearUnit,
        (string Name, string Abbreviation, string Direction) axis1,
        (string Name, string Abbreviation, string Direction) axis2)
    {
        Dictionary<string, object?> coordinateSystem = CoordinateSystemObject(
            "Cartesian",
            AxisObject(axis1.Name, axis1.Abbreviation, axis1.Direction, linearUnit),
            AxisObject(axis2.Name, axis2.Abbreviation, axis2.Direction, linearUnit));

        return Obj(
            ("type", "ProjectedCRS"),
            ("name", name),
            ("base_crs", baseCrs),
            ("conversion", conversion),
            ("coordinate_system", coordinateSystem),
            ("id", IdObject("EPSG", srid)));
    }

    private static Dictionary<string, object?> DerivedGeographicCrsObject(
        int code,
        string name,
        Dictionary<string, object?> baseCrs,
        Dictionary<string, object?> conversion,
        object angularUnit,
        (string Name, string Abbreviation, string Direction) axis1,
        (string Name, string Abbreviation, string Direction) axis2)
    {
        Dictionary<string, object?> coordinateSystem = CoordinateSystemObject(
            "ellipsoidal",
            AxisObject(axis1.Name, axis1.Abbreviation, axis1.Direction, angularUnit),
            AxisObject(axis2.Name, axis2.Abbreviation, axis2.Direction, angularUnit));

        return Obj(
            ("type", "DerivedGeographicCRS"),
            ("name", name),
            ("base_crs", baseCrs),
            ("conversion", conversion),
            ("coordinate_system", coordinateSystem),
            ("id", IdObject("TEST", code)));
    }

    private static Dictionary<string, object?> DerivedProjectedCrsObject(
        int code,
        string name,
        Dictionary<string, object?> baseCrs,
        Dictionary<string, object?> conversion,
        object linearUnit,
        (string Name, string Abbreviation, string Direction) axis1,
        (string Name, string Abbreviation, string Direction) axis2)
    {
        Dictionary<string, object?> coordinateSystem = CoordinateSystemObject(
            "Cartesian",
            AxisObject(axis1.Name, axis1.Abbreviation, axis1.Direction, linearUnit),
            AxisObject(axis2.Name, axis2.Abbreviation, axis2.Direction, linearUnit));

        return Obj(
            ("type", "DerivedProjectedCRS"),
            ("name", name),
            ("base_crs", baseCrs),
            ("conversion", conversion),
            ("coordinate_system", coordinateSystem),
            ("id", IdObject("TEST", code)));
    }

    private static Dictionary<string, object?> GeocentricCrsObject(
        int srid,
        string name,
        Dictionary<string, object?> datum,
        Dictionary<string, object?> primeMeridian,
        object linearUnit)
    {
        Dictionary<string, object?> coordinateSystem = CoordinateSystemObject(
            "Cartesian",
            AxisObject("Geocentric X", "X", "geocentricX", linearUnit),
            AxisObject("Geocentric Y", "Y", "geocentricY", linearUnit),
            AxisObject("Geocentric Z", "Z", "geocentricZ", linearUnit));

        return Obj(
            ("type", "GeodeticCRS"),
            ("name", name),
            ("datum", datum),
            ("prime_meridian", primeMeridian),
            ("coordinate_system", coordinateSystem),
            ("id", IdObject("EPSG", srid)));
    }

    private static Dictionary<string, object?> VerticalCrsObject(
        int srid,
        string name,
        Dictionary<string, object?> datum,
        object linearUnit,
        string axisName,
        string axisDirection)
    {
        return Obj(
            ("type", "VerticalCRS"),
            ("name", name),
            ("datum", datum),
            ("coordinate_system", CoordinateSystemObject("vertical", AxisObject(axisName, "H", axisDirection, linearUnit))),
            ("id", IdObject("EPSG", srid)));
    }

    private static Dictionary<string, object?> DatumEnsembleObject(
        string name,
        object[] members,
        string accuracy,
        Dictionary<string, object?>? ellipsoid,
        Dictionary<string, object?>? id)
    {
        Dictionary<string, object?> datumEnsemble = Obj(
            ("type", "DatumEnsemble"),
            ("name", name),
            ("members", members),
            ("accuracy", accuracy));

        if (ellipsoid is not null)
        {
            datumEnsemble["ellipsoid"] = ellipsoid;
        }

        if (id is not null)
        {
            datumEnsemble["id"] = id;
        }

        return datumEnsemble;
    }

    private static Dictionary<string, object?> DatumEnsembleMemberObject(string name, string authority, int code)
    {
        return Obj(("name", name), ("id", IdObject(authority, code)));
    }

    private static Dictionary<string, object?> BoundCrsObject(
        int srid,
        string name,
        Dictionary<string, object?> sourceCrs,
        Dictionary<string, object?> targetCrs,
        Dictionary<string, object?> transformation)
    {
        return Obj(
            ("type", "BoundCRS"),
            ("name", name),
            ("source_crs", sourceCrs),
            ("target_crs", targetCrs),
            ("transformation", transformation),
            ("id", IdObject("EPSG", srid)));
    }

    private static Dictionary<string, object?> CompoundCrsObject(int srid, string name, params Dictionary<string, object?>[] components)
    {
        return Obj(
            ("type", "CompoundCRS"),
            ("name", name),
            ("components", components.Cast<object>().ToArray()),
            ("id", IdObject("EPSG", srid)));
    }

    private static Dictionary<string, object?> GeodeticDatumObject(string name, Dictionary<string, object?> ellipsoid, int code)
    {
        return Obj(
            ("type", "GeodeticReferenceFrame"),
            ("name", name),
            ("ellipsoid", ellipsoid),
            ("id", IdObject("EPSG", code)));
    }

    private static Dictionary<string, object?> VerticalDatumObject(string name, int code)
    {
        return Obj(
            ("type", "VerticalReferenceFrame"),
            ("name", name),
            code > 0 ? ("id", IdObject("EPSG", code)) : ("id", null));
    }

    private static Dictionary<string, object?> EllipsoidObject(string name, double semiMajorAxis, double inverseFlattening, object unit, int code)
    {
        return Obj(
            ("type", "Ellipsoid"),
            ("name", name),
            ("semi_major_axis", semiMajorAxis),
            ("inverse_flattening", inverseFlattening),
            ("unit", unit),
            ("id", IdObject("EPSG", code)));
    }

    private static Dictionary<string, object?> PrimeMeridianObject(string name, double longitude, object unit, int code, bool useIds = false, bool stringCode = false)
    {
        object codeValue = stringCode ? code.ToString(CultureInfo.InvariantCulture) : code;
        return useIds
            ? Obj(
                ("name", name),
                ("longitude", longitude),
                ("unit", unit),
                ("ids", new object[] { IdObject("IGNF", name.ToUpperInvariant()), IdObject("EPSG", codeValue) }))
            : Obj(
                ("name", name),
                ("longitude", longitude),
                ("unit", unit),
                ("id", IdObject("EPSG", codeValue)));
    }

    private static Dictionary<string, object?> GreenwichPrimeMeridianObject() => PrimeMeridianObject("Greenwich", 0d, "degree", 8901);

    private static Dictionary<string, object?> CoordinateSystemObject(string subtype, params Dictionary<string, object?>[] axes)
    {
        return Obj(
            ("subtype", subtype),
            ("axis", axes.Cast<object>().ToArray()));
    }

    private static Dictionary<string, object?> AxisObject(string name, string abbreviation, string direction, object unit)
    {
        return Obj(
            ("name", name),
            ("abbreviation", abbreviation),
            ("direction", direction),
            ("unit", unit));
    }

    private static Dictionary<string, object?> ConversionObject(string name, string methodName, int conversionCode, params Dictionary<string, object?>[] parameters)
    {
        return Obj(
            ("type", "Conversion"),
            ("name", name),
            ("method", Obj(("name", methodName))),
            ("parameters", parameters.Cast<object>().ToArray()),
            ("id", IdObject("EPSG", conversionCode)));
    }

    private static Dictionary<string, object?> AffineConversionObject(string name, object translationUnit, double a0, double a1, double a2, double b0, double b1, double b2)
    {
        return ConversionObject(
            name,
            "Affine parametric transformation",
            9624,
            ProjectionParameterObject("A0", a0, translationUnit, 8623),
            ProjectionParameterObject("A1", a1, ScaleUnitObject(), 8624),
            ProjectionParameterObject("A2", a2, ScaleUnitObject(), 8625),
            ProjectionParameterObject("B0", b0, translationUnit, 8639),
            ProjectionParameterObject("B1", b1, ScaleUnitObject(), 8640),
            ProjectionParameterObject("B2", b2, ScaleUnitObject(), 8641));
    }

    private static Dictionary<string, object?> AbridgedTransformationObject(string name, string methodName, params Dictionary<string, object?>[] parameters)
    {
        return Obj(
            ("type", "AbridgedTransformation"),
            ("name", name),
            ("method", Obj(("name", methodName))),
            ("parameters", parameters.Cast<object>().ToArray()));
    }

    private static Dictionary<string, object?> ProjectionParameterObject(string name, double value, object unit, int code)
    {
        return Obj(
            ("name", name),
            ("value", value),
            ("unit", unit),
            ("id", IdObject("EPSG", code)));
    }

    private static Dictionary<string, object?> BoundParameterValueObject(string name, object value)
    {
        return Obj(
            ("name", name),
            ("value", value));
    }

    private static Dictionary<string, object?> AngularUnitObject(string name, double conversionFactor, int code)
    {
        return Obj(
            ("type", "AngularUnit"),
            ("name", name),
            ("conversion_factor", conversionFactor),
            ("id", IdObject("EPSG", code)));
    }

    private static Dictionary<string, object?> ScaleUnitObject()
    {
        return Obj(
            ("type", "ScaleUnit"),
            ("name", "unity"),
            ("conversion_factor", 1d),
            ("id", IdObject("EPSG", 9201)));
    }

    private static Dictionary<string, object?> IdObject(string authority, object code)
    {
        return Obj(
            ("authority", authority),
            ("code", code));
    }

    private static Dictionary<string, object?> Obj(params (string Key, object? Value)[] members)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, object? value) in members)
        {
            if (value is not null)
            {
                result[key] = value;
            }
        }

        return result;
    }
}
