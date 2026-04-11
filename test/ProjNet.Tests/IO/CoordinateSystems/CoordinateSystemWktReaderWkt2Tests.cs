// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Verifies native WKT2 coordinate-system parsing against real EPSG export examples and PROJ test fixtures.
/// </summary>
public class CoordinateSystemWktReaderWkt2Tests
{
    private const string ProjectedProjBoundCrs = """
        BOUNDCRS[
            SOURCECRS[
                PROJCRS["NAD83 / California zone 3 (ftUS)",
                    BASEGEODCRS["NAD83",
                        DATUM["North American Datum 1983",
                            ELLIPSOID["GRS 1980",6378137,298.257222101,
                                LENGTHUNIT["metre",1]]],
                        PRIMEM["Greenwich",0,
                            ANGLEUNIT["degree",0.0174532925199433]]],
                    CONVERSION["SPCS83 California zone 3 (US Survey feet)",
                        METHOD["Lambert Conic Conformal (2SP)",
                            ID["EPSG",9802]],
                        PARAMETER["Latitude of false origin",36.5,
                            ANGLEUNIT["degree",0.0174532925199433],
                            ID["EPSG",8821]],
                        PARAMETER["Longitude of false origin",-120.5,
                            ANGLEUNIT["degree",0.0174532925199433],
                            ID["EPSG",8822]],
                        PARAMETER["Latitude of 1st standard parallel",38.4333333333333,
                            ANGLEUNIT["degree",0.0174532925199433],
                            ID["EPSG",8823]],
                        PARAMETER["Latitude of 2nd standard parallel",37.0666666666667,
                            ANGLEUNIT["degree",0.0174532925199433],
                            ID["EPSG",8824]],
                        PARAMETER["Easting at false origin",6561666.667,
                            LENGTHUNIT["US survey foot",0.304800609601219],
                            ID["EPSG",8826]],
                        PARAMETER["Northing at false origin",1640416.667,
                            LENGTHUNIT["US survey foot",0.304800609601219],
                            ID["EPSG",8827]]],
                    CS[Cartesian,2],
                        AXIS["easting (X)",east,
                            ORDER[1],
                            LENGTHUNIT["US survey foot",0.304800609601219]],
                        AXIS["northing (Y)",north,
                            ORDER[2],
                            LENGTHUNIT["US survey foot",0.304800609601219]],
                    SCOPE["unknown"],
                    AREA["USA - California - SPCS - 3"],
                    BBOX[36.73,-123.02,38.71,-117.83],
                    ID["EPSG",2227]]],
            TARGETCRS[
                GEODCRS["WGS 84",
                    DATUM["World Geodetic System 1984",
                        ELLIPSOID["WGS 84",6378137,298.257223563,
                            LENGTHUNIT["metre",1]]],
                    PRIMEM["Greenwich",0,
                        ANGLEUNIT["degree",0.0174532925199433]],
                    CS[ellipsoidal,2],
                        AXIS["latitude",north,
                            ORDER[1],
                            ANGLEUNIT["degree",0.0174532925199433]],
                        AXIS["longitude",east,
                            ORDER[2],
                            ANGLEUNIT["degree",0.0174532925199433]],
                    ID["EPSG",4326]]],
            ABRIDGEDTRANSFORMATION["NAD83 to WGS 84 (1)",
                METHOD["Geocentric translations (geog2D domain)",
                    ID["EPSG",9603]],
                PARAMETER["X-axis translation",0,
                    ID["EPSG",8605]],
                PARAMETER["Y-axis translation",0,
                    ID["EPSG",8606]],
                PARAMETER["Z-axis translation",0,
                    ID["EPSG",8607]],
                SCOPE["unknown"],
                AREA["North America - Canada and USA (CONUS, Alaska mainland)"],
                BBOX[23.81,-172.54,86.46,-47.74],
                ID["EPSG",1188]]]
        """;

    private const string VerticalProjBoundCrs = """
        BOUNDCRS[
            SOURCECRS[
                VERTCRS["EGM96 height",
                    VDATUM["EGM96 geoid"],
                    CS[vertical,1],
                        AXIS["gravity-related height (H)",up,
                            LENGTHUNIT["metre",1]],
                    USAGE[
                        SCOPE["Geodesy."],
                        AREA["World."],
                        BBOX[-90,-180,90,180]],
                    ID["EPSG",5773]]],
            TARGETCRS[
                GEOGCRS["WGS 84",
                    DATUM["World Geodetic System 1984",
                        ELLIPSOID["WGS 84",6378137,298.257223563,
                            LENGTHUNIT["metre",1]]],
                    PRIMEM["Greenwich",0,
                        ANGLEUNIT["degree",0.0174532925199433]],
                    CS[ellipsoidal,3],
                        AXIS["latitude",north,
                            ORDER[1],
                            ANGLEUNIT["degree",0.0174532925199433]],
                        AXIS["longitude",east,
                            ORDER[2],
                            ANGLEUNIT["degree",0.0174532925199433]],
                        AXIS["ellipsoidal height",up,
                            ORDER[3],
                            LENGTHUNIT["metre",1]],
                    ID["EPSG",4979]]],
            ABRIDGEDTRANSFORMATION["WGS 84 to EGM96 height (1)",
                METHOD["Geographic3D to GravityRelatedHeight (EGM)",
                    ID["EPSG",9661]],
                PARAMETERFILE["Geoid (height correction) model file","us_nga_egm96_15.tif"]]]
        """;

    private const string EllipsoidalHeightBoundCrs = """
        BOUNDCRS[
            SOURCECRS[
                GEOGCRS["TWD97",DATUM["Taiwan Datum 1997",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1]]],PRIMEM["Greenwich",0,ANGLEUNIT["degree",0.0174532925199433]],CS[ellipsoidal,3],AXIS["geodetic latitude (Lat)",north,ORDER[1],ANGLEUNIT["degree",0.0174532925199433]],AXIS["geodetic longitude (Lon)",east,ORDER[2],ANGLEUNIT["degree",0.0174532925199433]],AXIS["ellipsoidal height (h)",up,ORDER[3],LENGTHUNIT["metre",1]],USAGE[SCOPE["unknown"],AREA["Taiwan"],BBOX[17.36,114.32,26.96,123.61]],ID["EPSG",3823]]],
            TARGETCRS[
                GEOGCRS["WGS 84",DATUM["World Geodetic System 1984",ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1]]],PRIMEM["Greenwich",0,ANGLEUNIT["degree",0.0174532925199433]],CS[ellipsoidal,2],AXIS["latitude",north,ORDER[1],ANGLEUNIT["degree",0.0174532925199433]],AXIS["longitude",east,ORDER[2],ANGLEUNIT["degree",0.0174532925199433]],ID["EPSG",4326]]],
            ABRIDGEDTRANSFORMATION["TWD97 to WGS 84 (1)",VERSION["OGP-Twn"],METHOD["Geocentric translations (geog2D domain)",ID["EPSG",9603]],PARAMETER["X-axis translation",0,ID["EPSG",8605]],PARAMETER["Y-axis translation",0,ID["EPSG",8606]],PARAMETER["Z-axis translation",0,ID["EPSG",8607]],USAGE[SCOPE["unknown"],AREA["Taiwan"],BBOX[17.36,114.32,26.96,123.61]],ID["DERIVED_FROM(EPSG)",3830]]]
        """;

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Srid)
            .ToDictionary(group => group.Key, group => group.Last().Wkt));

    /// <summary>
    /// Provides WKT2 geodetic CRS examples copied from <c>spec\epsg\EPSG-v12_053-WKT.Zip</c>.
    /// </summary>
    /// <returns>SRID/WKT pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedWkt2Rows()
    {
        return
        [
            new TheoryDataRow<int, string>(4230, """GEOGCRS["ED50",DATUM["European Datum 1950",ELLIPSOID["International 1924",6378388,297,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7022]],ID["EPSG",6230]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4230]]"""),
            new TheoryDataRow<int, string>(4267, """GEOGCRS["NAD27",DATUM["North American Datum 1927",ELLIPSOID["Clarke 1866",6378206.4,294.978698213898,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7008]],ID["EPSG",6267]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4267]]"""),
            new TheoryDataRow<int, string>(4277, """GEOGCRS["OSGB36",DATUM["Ordnance Survey of Great Britain 1936",ELLIPSOID["Airy 1830",6377563.396,299.3249646,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7001]],ID["EPSG",6277]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4277]]"""),
            new TheoryDataRow<int, string>(4312, """GEOGCRS["MGI",DATUM["Militar-Geographische Institut",ELLIPSOID["Bessel 1841",6377397.155,299.1528128,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7004]],ID["EPSG",6312]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4312]]"""),
            new TheoryDataRow<int, string>(4314, """GEOGCRS["DHDN",DATUM["Deutsches Hauptdreiecksnetz",ELLIPSOID["Bessel 1841",6377397.155,299.1528128,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7004]],ID["EPSG",6314]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4314]]"""),
            new TheoryDataRow<int, string>(4322, """GEOGCRS["WGS 72",DYNAMIC[FRAMEEPOCH[1972.0]],DATUM["World Geodetic System 1972",ELLIPSOID["WGS 72",6378135,298.26,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7043]],ID["EPSG",6322]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4322]]"""),
            new TheoryDataRow<int, string>(10176, """GEODCRS["IGS20",DYNAMIC[FRAMEEPOCH[2015.0]],DATUM["IGS20",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ID["EPSG",1333]],CS[Cartesian,3,ID["EPSG",6500]],AXIS["Geocentric X (X)",geocentricX,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Y (Y)",geocentricY,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Z (Z)",geocentricZ,LENGTHUNIT["metre",1,ID["EPSG",9001]]],ID["EPSG",10176]]"""),
            new TheoryDataRow<int, string>(10412, """GEODCRS["NAD83(CSRS)v8",DATUM["North American Datum of 1983 (CSRS) version 8",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ANCHOREPOCH[2010],ID["EPSG",1365]],CS[Cartesian,3,ID["EPSG",6500]],AXIS["Geocentric X (X)",geocentricX,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Y (Y)",geocentricY,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Z (Z)",geocentricZ,LENGTHUNIT["metre",1,ID["EPSG",9001]]],DEFININGTRANSFORMATION["ITRF2020 to NAD83(CSRS)v8 (1)",ID["EPSG",10415]],ID["EPSG",10412]]"""),
        ];
    }

    /// <summary>
    /// Provides representative top-level ellipsoidal 3D WKT2 geographic CRS examples from the PostGIS failure set.
    /// </summary>
    /// <returns>SRID/WKT pairs that should now parse successfully as operational compounds.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedWkt2Ellipsoidal3dRows()
    {
        return
        [
            new TheoryDataRow<int, string>(4329, """GEOGCRS["WGS 84 (3D)",DATUM["World Geodetic System 1984",ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1]]],PRIMEM["Greenwich",0,ANGLEUNIT["degree",0.0174532925199433]],CS[ellipsoidal,3],AXIS["geodetic latitude (Lat)",north,ORDER[1],ANGLEUNIT["degree minute second hemisphere",0.0174532925199433]],AXIS["geodetic longitude (Long)",east,ORDER[2],ANGLEUNIT["degree minute second hemisphere",0.0174532925199433]],AXIS["ellipsoidal height (h)",up,ORDER[3],LENGTHUNIT["metre",1]],USAGE[SCOPE["unknown"],AREA["World (by country)"],BBOX[-90,-180,90,180]],ID["EPSG",4329]]"""),
            new TheoryDataRow<int, string>(4979, """GEOGCRS["WGS 84",DATUM["World Geodetic System 1984",ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1]]],PRIMEM["Greenwich",0,ANGLEUNIT["degree",0.0174532925199433]],CS[ellipsoidal,3],AXIS["geodetic latitude (Lat)",north,ORDER[1],ANGLEUNIT["degree",0.0174532925199433]],AXIS["geodetic longitude (Lon)",east,ORDER[2],ANGLEUNIT["degree",0.0174532925199433]],AXIS["ellipsoidal height (h)",up,ORDER[3],LENGTHUNIT["metre",1]],USAGE[SCOPE["unknown"],AREA["World (by country)"],BBOX[-90,-180,90,180]],ID["EPSG",4979]]"""),
        ];
    }

    /// <summary>
    /// Provides datum-backed WKT2 projected CRS examples copied from <c>spec\epsg\EPSG-v12_053-WKT.Zip</c>.
    /// </summary>
    /// <returns>SRID/WKT pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedWkt2ProjectedRows()
    {
        return
        [
            new TheoryDataRow<int, string>(27700, """PROJCRS["OSGB36 / British National Grid",BASEGEOGCRS["OSGB36",DATUM["Ordnance Survey of Great Britain 1936",ELLIPSOID["Airy 1830",6377563.396,299.3249646,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7001]],ID["EPSG",6277]],ID["EPSG",4277]],CONVERSION["British National Grid",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",49,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",-2,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996012717,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",400000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",-100000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",19916]],CS[Cartesian,2,ID["EPSG",4499]],AXIS["Easting (E)",east],AXIS["Northing (N)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",27700]]"""),
            new TheoryDataRow<int, string>(31370, """PROJCRS["BD72 / Belgian Lambert 72",BASEGEOGCRS["BD72",DATUM["Reseau National Belge 1972",ELLIPSOID["International 1924",6378388,297,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7022]],ID["EPSG",6313]],ID["EPSG",4313]],CONVERSION["Belgian Lambert 72",METHOD["Lambert Conic Conformal (2SP)",ID["EPSG",9802]],PARAMETER["Latitude of false origin",90,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8821]],PARAMETER["Longitude of false origin",4.36748666666694,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8822]],PARAMETER["Latitude of 1st standard parallel",51.1666672333336,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8823]],PARAMETER["Latitude of 2nd standard parallel",49.8333339000003,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8824]],PARAMETER["Easting at false origin",150000.013,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8826]],PARAMETER["Northing at false origin",5400088.438,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8827]],ID["EPSG",19961]],CS[Cartesian,2,ID["EPSG",4499]],AXIS["Easting (X)",east],AXIS["Northing (Y)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",31370]]"""),
            new TheoryDataRow<int, string>(2169, """PROJCRS["LUREF / Luxembourg TM",BASEGEOGCRS["LUREF",DATUM["Luxembourg Reference Frame",ELLIPSOID["International 1924",6378388,297,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7022]],ID["EPSG",6181]],ID["EPSG",4181]],CONVERSION["Luxembourg TM",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",49.8333333333336,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",6.16666666666694,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",1,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",80000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",100000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",19966]],CS[Cartesian,2,ID["EPSG",4530]],AXIS["Northing (X)",north],AXIS["Easting (Y)",east],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",2169]]"""),
            new TheoryDataRow<int, string>(23032, """PROJCRS["ED50 / UTM zone 32N",BASEGEOGCRS["ED50",DATUM["European Datum 1950",ELLIPSOID["International 1924",6378388,297,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7022]],ID["EPSG",6230]],ID["EPSG",4230]],CONVERSION["UTM zone 32N",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",500000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",16032]],CS[Cartesian,2,ID["EPSG",4400]],AXIS["Easting (E)",east],AXIS["Northing (N)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",23032]]"""),
            new TheoryDataRow<int, string>(31467, """PROJCRS["DHDN / 3-degree Gauss-Kruger zone 3",BASEGEOGCRS["DHDN",DATUM["Deutsches Hauptdreiecksnetz",ELLIPSOID["Bessel 1841",6377397.155,299.1528128,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7004]],ID["EPSG",6314]],ID["EPSG",4314]],CONVERSION["3-degree Gauss-Kruger zone 3",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",1,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",3500000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",16263]],CS[Cartesian,2,ID["EPSG",4530]],AXIS["Northing (X)",north],AXIS["Easting (Y)",east],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",31467]]"""),
        ];
    }

    /// <summary>
    /// Provides WKT2 vertical CRS examples copied from <c>spec\epsg\EPSG-v12_053-WKT.Zip</c>.
    /// </summary>
    /// <returns>SRID/WKT pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedWkt2VerticalRows()
    {
        return
        [
            new TheoryDataRow<int, string>(10150, """VERTCRS["MSL UK & Ireland VORF08 depth",VDATUM["Mean Sea Level UK & Ireland VORF08",ID["EPSG",1330]],CS[vertical,1,ID["EPSG",6498]],AXIS["Depth (D)",down],LENGTHUNIT["metre",1,ID["EPSG",9001]],GEOIDMODEL["ETRS89 to MSL UK & Ireland VORF08 depth (1)",ID["EPSG",10154]],ID["EPSG",10150]]"""),
            new TheoryDataRow<int, string>(10190, """VERTCRS["NGA 2022 height",VDATUM["Nivellement General de l'Algerie 2022",ID["EPSG",1354]],CS[vertical,1,ID["EPSG",6499]],AXIS["Gravity-related height (H)",up],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",10190]]"""),
            new TheoryDataRow<int, string>(10352, """VERTCRS["Formentera height",VDATUM["Formentera",ID["EPSG",1362]],CS[vertical,1,ID["EPSG",6499]],AXIS["Gravity-related height (H)",up],LENGTHUNIT["metre",1,ID["EPSG",9001]],GEOIDMODEL["ETRS89-ESP [REGENTE] to Formentera height (1)",ID["EPSG",10358]],ID["EPSG",10352]]"""),
        ];
    }

    /// <summary>
    /// Provides datum-backed WKT2 compound CRS examples copied from <c>spec\epsg\EPSG-v12_053-WKT.Zip</c>.
    /// </summary>
    /// <returns>SRID/WKT pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedWkt2CompoundRows()
    {
        return
        [
            new TheoryDataRow<int, string>(10162, """COMPOUNDCRS["JGD2011 / Japan Plane Rectangular CS I + JGD2011 (vertical) height",PROJCRS["JGD2011 / Japan Plane Rectangular CS I",BASEGEOGCRS["JGD2011",DATUM["Japanese Geodetic Datum 2011",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ID["EPSG",1128]],ID["EPSG",6668]],CONVERSION["Japan Plane Rectangular CS zone I",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",33,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",129.5,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9999,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",17801]],CS[Cartesian,2,ID["EPSG",4530]],AXIS["Northing (X)",north],AXIS["Easting (Y)",east],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",6669]],VERTCRS["JGD2011 (vertical) height",VDATUM["Japanese Geodetic Datum 2011 (vertical)",ID["EPSG",1131]],CS[vertical,1,ID["EPSG",6499]],AXIS["Gravity-related height (H)",up],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",6695]],ID["EPSG",10162]]"""),
            new TheoryDataRow<int, string>(10163, """COMPOUNDCRS["JGD2011 / Japan Plane Rectangular CS II + JGD2011 (vertical) height",PROJCRS["JGD2011 / Japan Plane Rectangular CS II",BASEGEOGCRS["JGD2011",DATUM["Japanese Geodetic Datum 2011",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ID["EPSG",1128]],ID["EPSG",6668]],CONVERSION["Japan Plane Rectangular CS zone II",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",33,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",131,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9999,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",17802]],CS[Cartesian,2,ID["EPSG",4530]],AXIS["Northing (X)",north],AXIS["Easting (Y)",east],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",6670]],VERTCRS["JGD2011 (vertical) height",VDATUM["Japanese Geodetic Datum 2011 (vertical)",ID["EPSG",1131]],CS[vertical,1,ID["EPSG",6499]],AXIS["Gravity-related height (H)",up],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",6695]],ID["EPSG",10163]]"""),
            new TheoryDataRow<int, string>(10164, """COMPOUNDCRS["JGD2011 / Japan Plane Rectangular CS III + JGD2011 (vertical) height",PROJCRS["JGD2011 / Japan Plane Rectangular CS III",BASEGEOGCRS["JGD2011",DATUM["Japanese Geodetic Datum 2011",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ID["EPSG",1128]],ID["EPSG",6668]],CONVERSION["Japan Plane Rectangular CS zone III",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",36,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",132.166666666667,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9999,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",17803]],CS[Cartesian,2,ID["EPSG",4530]],AXIS["Northing (X)",north],AXIS["Easting (Y)",east],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",6671]],VERTCRS["JGD2011 (vertical) height",VDATUM["Japanese Geodetic Datum 2011 (vertical)",ID["EPSG",1131]],CS[vertical,1,ID["EPSG",6499]],AXIS["Gravity-related height (H)",up],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",6695]],ID["EPSG",10164]]"""),
        ];
    }

    /// <summary>
    /// Provides representative supported engineering CRS examples.
    /// </summary>
    /// <returns>WKT2 engineering CRS strings that should parse successfully and roundtrip semantically.</returns>
    public static IEnumerable<TheoryDataRow<string>> SupportedEngineeringWkt2Rows()
    {
        return
        [
            new TheoryDataRow<string>("""ENGCRS["Engineering example",EDATUM["Local engineering datum",ID["TEST",1001]],CS[Cartesian,2],AXIS["x",east],AXIS["y",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["TEST",5800]]"""),
        ];
    }

    /// <summary>
    /// Provides representative supported temporal CRS examples.
    /// </summary>
    /// <returns>WKT2 temporal CRS strings that should parse successfully and roundtrip semantically.</returns>
    public static IEnumerable<TheoryDataRow<string>> SupportedTemporalWkt2Rows()
    {
        return
        [
            new TheoryDataRow<string>("""TIMECRS["Temporal example",TDATUM["Unix epoch",TIMEORIGIN["1970-01-01T00:00:00Z"],ID["TEST",1040]],CS[temporal,1],AXIS["time",north],TIMEUNIT["second",1,ID["EPSG",1040]],ID["TEST",1041]]"""),
        ];
    }

    /// <summary>
    /// Provides representative supported parametric CRS examples.
    /// </summary>
    /// <returns>WKT2 parametric CRS strings that should parse successfully and roundtrip semantically.</returns>
    public static IEnumerable<TheoryDataRow<string>> SupportedParametricWkt2Rows()
    {
        return
        [
            new TheoryDataRow<string>("""PARAMETRICCRS["Reservoir pressure",PDATUM["Reservoir datum",ID["TEST",2001]],CS[parametric,1],AXIS["pressure",up],PARAMETRICUNIT["bar",100000,ID["TEST",2002]],ID["TEST",2003]]"""),
        ];
    }

    /// <summary>
    /// Provides representative unsupported top-level WKT2 roots that are intentionally outside the current native or normalized reader surface.
    /// </summary>
    /// <returns>Keyword/WKT pairs that should still be rejected explicitly.</returns>
    public static IEnumerable<TheoryDataRow<string, string, string>> UnsupportedTopLevelWkt2Rows()
    {
        return
        [
            new TheoryDataRow<string, string, string>("COORDINATEMETADATA", """COORDINATEMETADATA["Metadata example"]""", "COORDINATEMETADATA"),
            new TheoryDataRow<string, string, string>("COORDINATEOPERATION", """COORDINATEOPERATION["Operation example"]""", "COORDINATEOPERATION"),
        ];
    }

    /// <summary>
    /// Provides real supported <c>BOUNDCRS</c> fixtures extracted from the checked-in PROJ test suite.
    /// </summary>
    /// <returns>Fixture source labels and WKT2 strings that should parse onto the current source-CRS model.</returns>
    public static IEnumerable<TheoryDataRow<string, string>> SupportedProjBoundCrsRows()
    {
        return
        [
            new TheoryDataRow<string, string>(@"spec\PROJ\test\unit\test_operationfactory.cpp:3815", ProjectedProjBoundCrs),
        ];
    }

    /// <summary>
    /// Provides real supported vertical <c>BOUNDCRS</c> fixtures extracted from the checked-in PROJ test suite.
    /// </summary>
    /// <returns>Fixture source labels and WKT2 strings that should parse onto the current source-CRS model.</returns>
    public static IEnumerable<TheoryDataRow<string, string>> SupportedVerticalBoundCrsRows()
    {
        return
        [
            new TheoryDataRow<string, string>(@"spec\PROJ\test\unit\test_operationfactory.cpp:9132", VerticalProjBoundCrs),
        ];
    }

    /// <summary>
    /// Verifies supported WKT2 CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="wkt">WKT2 CRS from the EPSG export.</param>
    [Theory]
    [MemberData(nameof(SupportedWkt2Rows))]
    public void CreateFromWkt_ParsesSupportedWkt2CrsEquivalentToCatalogReference(int srid, string wkt)
    {
        CoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem(CoordinateSystemFactory, wkt);
        CoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.IsType(reference.GetType(), parsed);
        Assert.True(parsed.EqualParams(reference), $"WKT2 parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported WKT2 projected CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="wkt">WKT2 projected CRS from the EPSG export.</param>
    [Theory]
    [MemberData(nameof(SupportedWkt2ProjectedRows))]
    public void CreateFromWkt_ParsesSupportedWkt2ProjectedCrsEquivalentToCatalogReference(int srid, string wkt)
    {
        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.True(parsed.EqualParams(reference), $"WKT2 projected CRS parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported WKT2 vertical CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="wkt">WKT2 vertical CRS from the EPSG export.</param>
    [Theory]
    [MemberData(nameof(SupportedWkt2VerticalRows))]
    public void CreateFromWkt_ParsesSupportedWkt2VerticalCrsEquivalentToCatalogReference(int srid, string wkt)
    {
        VerticalCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(CoordinateSystemFactory, wkt);
        VerticalCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.True(parsed.EqualParams(reference), $"WKT2 vertical CRS parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported WKT2 compound CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="wkt">WKT2 compound CRS from the EPSG export.</param>
    [Theory]
    [MemberData(nameof(SupportedWkt2CompoundRows))]
    public void CreateFromWkt_ParsesSupportedWkt2CompoundCrsEquivalentToCatalogReference(int srid, string wkt)
    {
        CompoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(CoordinateSystemFactory, wkt);
        CompoundCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.True(parsed.EqualParams(reference), $"WKT2 compound CRS parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies top-level ellipsoidal 3D WKT2 geographic CRS now parse through the existing operational compound representation.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="wkt">WKT2 geographic CRS from the PostGIS failure set.</param>
    [Theory]
    [MemberData(nameof(SupportedWkt2Ellipsoidal3dRows))]
    public void CreateFromWkt_ParsesTopLevelEllipsoidal3dGeographicCrsAsOperationalCompound(int srid, string wkt)
    {
        CompoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem horizontal = Assert.IsType<GeographicCoordinateSystem>(parsed.HeadCoordinateSystem);
        VerticalCoordinateSystem vertical = Assert.IsType<VerticalCoordinateSystem>(parsed.TailCoordinateSystem);

        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
        Assert.Equal(3, parsed.Dimension);
        Assert.Equal(AxisOrientationEnum.North, parsed.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.East, parsed.GetAxis(1).Orientation);
        Assert.Equal(AxisOrientationEnum.Up, parsed.GetAxis(2).Orientation);
        Assert.Equal("World Geodetic System 1984", horizontal.HorizontalDatum.Name);
        Assert.True(horizontal.HorizontalDatum.Ellipsoid.EqualParams(Ellipsoid.WGS84));
        Assert.Equal(DatumType.VD_Ellipsoidal, vertical.VerticalDatum.DatumType);
        Assert.Equal("metre", vertical.LinearUnit.Name);
    }

    /// <summary>
    /// Verifies direct optional metadata blocks are tolerated on projected WKT2 nodes.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesProjectedCrsWithDirectOptionalMetadataBlocksEquivalentToCatalogReference()
    {
        const string wkt = """PROJCRS["OSGB36 / British National Grid",BASEGEOGCRS["OSGB36",DATUM["Ordnance Survey of Great Britain 1936",REMARK["datum remark"],ELLIPSOID["Airy 1830",6377563.396,299.3249646,LENGTHUNIT["metre",1,REMARK["ellipsoid unit remark"],ID["EPSG",9001]],REMARK["ellipsoid remark"],ID["EPSG",7001]],ID["EPSG",6277]],ID["EPSG",4277]],CONVERSION["British National Grid",REMARK["conversion remark"],METHOD["Transverse Mercator",REMARK["method remark"],ID["EPSG",9807]],PARAMETER["Latitude of natural origin",49,ANGLEUNIT["degree",0.0174532925199433,REMARK["angle remark"],ID["EPSG",9102]],REMARK["latitude parameter remark"],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",-2,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996012717,SCALEUNIT["unity",1,REMARK["scale unit remark"],ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",400000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",-100000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",19916]],CS[Cartesian,2,ID["EPSG",4499]],AXIS["Easting (E)",east,ORDER[1]],AXIS["Northing (N)",north,ORDER[2]],LENGTHUNIT["metre",1,REMARK["root unit remark"],ID["EPSG",9001]],REMARK["projected root remark"],SCOPE["Engineering survey, topographic mapping."],AREA["United Kingdom (UK) - offshore to boundary of UKCS within 49°45'N to 61°N and 9°W to 2°E; onshore Great Britain (England, Wales and Scotland). Isle of Man onshore."],BBOX[49.75,-9.01,61.01,2.01],ID["EPSG",27700]]""";

        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(27700));

        Assert.True(parsed.EqualParams(reference));
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(27700, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies <c>USAGE</c>-wrapped optional metadata blocks are tolerated on vertical WKT2 nodes.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesVerticalCrsWithUsageMetadataEquivalentToCatalogReference()
    {
        const string wkt = """VERTCRS["NGA 2022 height",REMARK["vertical root remark"],VDATUM["Nivellement General de l'Algerie 2022",REMARK["vertical datum remark"],ID["EPSG",1354]],CS[vertical,1,ID["EPSG",6499]],AXIS["Gravity-related height (H)",up,ORDER[1]],LENGTHUNIT["metre",1,REMARK["vertical unit remark"],ID["EPSG",9001]],USAGE[SCOPE["Geodesy."],AREA["Algeria - onshore."],BBOX[18.97,-8.67,37.09,11.99]],ID["EPSG",10190]]""";

        VerticalCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(CoordinateSystemFactory, wkt);
        VerticalCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(10190));

        Assert.True(parsed.EqualParams(reference));
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(10190, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies affine derived geographic WKT2 definitions parse onto <see cref="FittedCoordinateSystem"/> and retain the fitted axis metadata.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithDerivedGeographicCrs_ParsesFittedCoordinateSystemAndRetainsAxisMetadata()
    {
        const string wkt = """GEOGCRS["Local WGS 84",BASEGEOGCRS["WGS 84",DATUM["World Geodetic System 1984",ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1]],ID["EPSG",6326]],ID["EPSG",4326]],DERIVINGCONVERSION["unnamed",METHOD["Affine parametric transformation"],PARAMETER["A0",0.5,ANGLEUNIT["degree",0.0174532925199433]],PARAMETER["A1",1,SCALEUNIT["unity",1]],PARAMETER["A2",0,SCALEUNIT["unity",1]],PARAMETER["B0",1.5,ANGLEUNIT["degree",0.0174532925199433]],PARAMETER["B1",0,SCALEUNIT["unity",1]],PARAMETER["B2",1,SCALEUNIT["unity",1]]],CS[ellipsoidal,2],AXIS["Local latitude",north,ORDER[1]],AXIS["Local longitude",east,ORDER[2]],ANGLEUNIT["degree",0.0174532925199433],ID["TEST",2001]]""";

        FittedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem baseCoordinateSystem = Assert.IsType<GeographicCoordinateSystem>(parsed.BaseCoordinateSystem);

        Assert.Equal("Local WGS 84", parsed.Name);
        Assert.Equal("TEST", parsed.Authority);
        Assert.Equal(2001, parsed.AuthorityCode);
        Assert.Equal("WGS 84", baseCoordinateSystem.Name);
        Assert.Equal("Local latitude", parsed.GetAxis(0).Name);
        Assert.Equal(AxisOrientationEnum.North, parsed.GetAxis(0).Orientation);
        Assert.Equal("Local longitude", parsed.GetAxis(1).Name);
        Assert.Equal(AxisOrientationEnum.East, parsed.GetAxis(1).Orientation);
        Assert.StartsWith("PARAM_MT[\"Affine\"", parsed.ToBase(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies affine derived projected WKT2 definitions parse onto <see cref="FittedCoordinateSystem"/> and retain the base projected CRS.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithDerivedProjectedCrs_ParsesFittedCoordinateSystemAndRetainsBaseProjectedCrs()
    {
        const string wkt = """DERIVEDPROJCRS["Local projected",BASEPROJCRS["WGS 84 / UTM zone 32N",BASEGEOGCRS["WGS 84",DATUM["World Geodetic System 1984",ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1]],ID["EPSG",6326]],ID["EPSG",4326]],CONVERSION["UTM zone 32N",METHOD["Transverse Mercator"],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433]],PARAMETER["Scale factor at natural origin",0.9996,SCALEUNIT["unity",1]],PARAMETER["False easting",500000,LENGTHUNIT["metre",1]],PARAMETER["False northing",0,LENGTHUNIT["metre",1]]],CS[Cartesian,2],AXIS["Easting",east,ORDER[1]],AXIS["Northing",north,ORDER[2]],LENGTHUNIT["metre",1],ID["EPSG",32632]],DERIVINGCONVERSION["unnamed",METHOD["Affine parametric transformation"],PARAMETER["A0",100,LENGTHUNIT["metre",1]],PARAMETER["A1",1,SCALEUNIT["unity",1]],PARAMETER["A2",0,SCALEUNIT["unity",1]],PARAMETER["B0",-50,LENGTHUNIT["metre",1]],PARAMETER["B1",0,SCALEUNIT["unity",1]],PARAMETER["B2",1,SCALEUNIT["unity",1]]],CS[Cartesian,2],AXIS["Local easting",east,ORDER[1]],AXIS["Local northing",north,ORDER[2]],LENGTHUNIT["metre",1],ID["TEST",3001]]""";

        FittedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(CoordinateSystemFactory, wkt);
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
    /// Verifies legacy hybrid <c>GEODCRS</c> definitions without <c>CS[...]</c> still fall back to the older normalization path.
    /// </summary>
    [Fact]
    public void CreateFromWkt_FallsBackForLegacyHybridGeodCrsWithoutCoordinateSystemBlock()
    {
        const string wkt = """GEODCRS["WGS 84",DATUM["WGS_1984",ELLIPSOID["WGS 84",6378137,298.257223563,ID["EPSG","7030"]],ID["EPSG","6326"]],PRIMEM["Greenwich",0,ID["EPSG","8901"]],UNIT["degree",0.0174532925199433,ID["EPSG","9122"]],ID["EPSG","4326"]]""";

        GeographicCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(CoordinateSystemFactory, wkt);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4326, parsed.AuthorityCode);
        Assert.Equal("EPSG", parsed.HorizontalDatum.Authority);
        Assert.Equal(6326, parsed.HorizontalDatum.AuthorityCode);
        Assert.Equal("EPSG", parsed.HorizontalDatum.Ellipsoid.Authority);
        Assert.Equal(7030, parsed.HorizontalDatum.Ellipsoid.AuthorityCode);
        Assert.Equal("EPSG", parsed.PrimeMeridian.Authority);
        Assert.Equal(8901, parsed.PrimeMeridian.AuthorityCode);
        Assert.Equal("EPSG", parsed.AngularUnit.Authority);
        Assert.Equal(9122, parsed.AngularUnit.AuthorityCode);
    }

    /// <summary>
    /// Verifies explicit prime-meridian units and non-degree angular units survive native WKT2 parsing.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesPrimeMeridianWithExplicitAngularUnit()
    {
        const string wkt = """GEOGCRS["NTF (Paris)",DATUM["Nouvelle Triangulation Francaise (Paris)",ELLIPSOID["Clarke 1880 (IGN)",6378249.2,293.466021293627,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7011]],ID["EPSG",6807]],PRIMEM["Paris",0.040792344,ANGLEUNIT["radian",1,ID["EPSG",9101]],ID["EPSG",8903]],CS[ellipsoidal,2,ID["EPSG",6403]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["grad",0.015707963267949,ID["EPSG",9105]],ID["EPSG",4807]]""";

        GeographicCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(CoordinateSystemFactory, wkt);

        Assert.Equal("NTF (Paris)", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4807, parsed.AuthorityCode);
        Assert.Equal("grad", parsed.AngularUnit.Name);
        Assert.Equal("EPSG", parsed.AngularUnit.Authority);
        Assert.Equal(9105, parsed.AngularUnit.AuthorityCode);
        Assert.Equal(0.015707963267949d, parsed.AngularUnit.RadiansPerUnit, 15);
        Assert.Equal("Paris", parsed.PrimeMeridian.Name);
        Assert.Equal("EPSG", parsed.PrimeMeridian.Authority);
        Assert.Equal(8903, parsed.PrimeMeridian.AuthorityCode);
        Assert.True(parsed.PrimeMeridian.AngularUnit.EqualParams(AngularUnit.Radian));
        Assert.Equal(0.040792344d, parsed.PrimeMeridian.Longitude);
        Assert.Equal(AxisOrientationEnum.North, parsed.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.East, parsed.GetAxis(1).Orientation);
    }

    /// <summary>
    /// Verifies projected WKT2 parsing preserves base-CRS angular units and explicit prime-meridian units.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesProjectedCrsBasePrimeMeridianAndAngularUnit()
    {
        const string wkt = """PROJCRS["NTF (Paris) / Lambert Nord France",BASEGEOGCRS["NTF (Paris)",DATUM["Nouvelle Triangulation Francaise (Paris)",ELLIPSOID["Clarke 1880 (IGN)",6378249.2,293.466021293627,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7011]],ID["EPSG",6807]],PRIMEM["Paris",0.040792344,ANGLEUNIT["radian",1,ID["EPSG",9101]],ID["EPSG",8903]],ID["EPSG",4807]],CONVERSION["Lambert Nord France",METHOD["Lambert Conic Conformal (1SP)",ID["EPSG",9801]],PARAMETER["Latitude of natural origin",55,ANGLEUNIT["grad",0.015707963267949,ID["EPSG",9105]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",0,ANGLEUNIT["grad",0.015707963267949,ID["EPSG",9105]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.999877341,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",600000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",200000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",18091]],CS[Cartesian,2,ID["EPSG",4499]],AXIS["Easting (X)",east],AXIS["Northing (Y)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",27561]]""";

        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);

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
    /// Verifies ensemble-backed WKT2 geographic CRS now retain ensemble metadata on the parsed datum.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithDatumEnsemble_ParsesGeographicCrsAndRetainsEnsembleMetadata()
    {
        const string wkt = """GEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],CS[ellipsoidal,3,ID["EPSG",6423]],AXIS["Geodetic latitude (Lat)",north,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]]],AXIS["Geodetic longitude (Lon)",east,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]]],AXIS["Ellipsoidal height (h)",up,LENGTHUNIT["metre",1,ID["EPSG",9001]]],ID["EPSG",4979]]""";

        CompoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(CoordinateSystemFactory, wkt);
        GeographicCoordinateSystem horizontal = Assert.IsType<GeographicCoordinateSystem>(parsed.HeadCoordinateSystem);
        DatumEnsemble ensemble = Assert.IsType<DatumEnsemble>(horizontal.HorizontalDatum.Ensemble);

        Assert.Equal("World Geodetic System 1984 ensemble", horizontal.HorizontalDatum.Name);
        Assert.Equal("EPSG", horizontal.HorizontalDatum.Authority);
        Assert.Equal(6326, horizontal.HorizontalDatum.AuthorityCode);
        Assert.Equal(8, ensemble.Members.Count);
        Assert.Equal(2d, ensemble.Accuracy);
        Assert.NotNull(ensemble.Ellipsoid);
        Assert.Equal("WGS 84", Assert.IsType<Ellipsoid>(ensemble.Ellipsoid).Name);
    }

    /// <summary>
    /// Verifies ensemble-backed projected WKT2 CRS now retain ensemble metadata on the base datum.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithEnsembleBasedProjCrs_ParsesProjectedCoordinateSystemAndRetainsBaseEnsembleMetadata()
    {
        const string wkt = """PROJCRS["WGS 84 / UTM zone 32N",BASEGEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],ID["EPSG",4326]],CONVERSION["UTM zone 32N",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",500000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",16032]],CS[Cartesian,2,ID["EPSG",4400]],AXIS["Easting (E)",east],AXIS["Northing (N)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",32632]]""";

        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        DatumEnsemble ensemble = Assert.IsType<DatumEnsemble>(parsed.GeographicCoordinateSystem.HorizontalDatum.Ensemble);

        Assert.Equal("World Geodetic System 1984 ensemble", parsed.GeographicCoordinateSystem.HorizontalDatum.Name);
        Assert.Equal("EPSG", parsed.GeographicCoordinateSystem.HorizontalDatum.Authority);
        Assert.Equal(6326, parsed.GeographicCoordinateSystem.HorizontalDatum.AuthorityCode);
        Assert.Equal(8, ensemble.Members.Count);
        Assert.Equal(2d, ensemble.Accuracy);
        Assert.NotNull(ensemble.Ellipsoid);
    }

    /// <summary>
    /// Verifies vertical ensemble-backed WKT2 CRS retain ensemble metadata on the parsed datum.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithVerticalDatumEnsemble_ParsesVerticalCoordinateSystemAndRetainsEnsembleMetadata()
    {
        const string wkt = """VERTCRS["Example ensemble height",ENSEMBLE["Example vertical ensemble",MEMBER["Datum A",ID["TEST",1]],MEMBER["Datum B",ID["TEST",2]],ENSEMBLEACCURACY[0.05],ID["TEST",1001]],CS[vertical,1],AXIS["Gravity-related height (H)",up],LENGTHUNIT["metre",1],ID["TEST",2001]]""";

        VerticalCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(CoordinateSystemFactory, wkt);
        DatumEnsemble ensemble = Assert.IsType<DatumEnsemble>(parsed.VerticalDatum.Ensemble);

        Assert.Equal("Example vertical ensemble", parsed.VerticalDatum.Name);
        Assert.Equal("TEST", parsed.VerticalDatum.Authority);
        Assert.Equal(1001, parsed.VerticalDatum.AuthorityCode);
        Assert.Equal(2, ensemble.Members.Count);
        Assert.Equal(0.05d, ensemble.Accuracy);
        Assert.Null(ensemble.Ellipsoid);
    }

    /// <summary>
    /// Verifies supported engineering CRS examples parse and roundtrip through the engineering model.
    /// </summary>
    /// <param name="wkt">The engineering CRS WKT2 input.</param>
    [Theory]
    [MemberData(nameof(SupportedEngineeringWkt2Rows))]
    public void CreateFromWkt_WithSupportedEngineeringCrs_ParsesEngineeringCoordinateSystem(string wkt)
    {
        EngineeringCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<EngineeringCoordinateSystem>(CoordinateSystemFactory, wkt);
        EngineeringCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<EngineeringCoordinateSystem>(
            CoordinateSystemFactory,
            parsed.ToWktNode(WktVersion.Wkt22019).ToString());

        Assert.Equal("Engineering example", parsed.Name);
        Assert.Equal("Local engineering datum", parsed.EngineeringDatum.Name);
        Assert.Equal("Cartesian", parsed.CoordinateSystemType);
        Assert.True(parsed.EqualParams(roundTripped));
    }

    /// <summary>
    /// Verifies supported temporal CRS examples parse and roundtrip through the temporal model.
    /// </summary>
    /// <param name="wkt">The temporal CRS WKT2 input.</param>
    [Theory]
    [MemberData(nameof(SupportedTemporalWkt2Rows))]
    public void CreateFromWkt_WithSupportedTemporalCrs_ParsesTemporalCoordinateSystem(string wkt)
    {
        TemporalCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<TemporalCoordinateSystem>(CoordinateSystemFactory, wkt);
        TemporalCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<TemporalCoordinateSystem>(
            CoordinateSystemFactory,
            parsed.ToWktNode(WktVersion.Wkt22019).ToString());

        Assert.Equal("Temporal example", parsed.Name);
        Assert.Equal("Unix epoch", parsed.TemporalDatum.Name);
        Assert.Equal("1970-01-01T00:00:00Z", parsed.TemporalDatum.TimeOrigin);
        Assert.True(parsed.EqualParams(roundTripped));
    }

    /// <summary>
    /// Verifies supported parametric CRS examples parse and roundtrip through the parametric model.
    /// </summary>
    /// <param name="wkt">The parametric CRS WKT2 input.</param>
    [Theory]
    [MemberData(nameof(SupportedParametricWkt2Rows))]
    public void CreateFromWkt_WithSupportedParametricCrs_ParsesParametricCoordinateSystem(string wkt)
    {
        ParametricCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ParametricCoordinateSystem>(CoordinateSystemFactory, wkt);
        ParametricCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<ParametricCoordinateSystem>(
            CoordinateSystemFactory,
            parsed.ToWktNode(WktVersion.Wkt22019).ToString());

        Assert.Equal("Reservoir pressure", parsed.Name);
        Assert.Equal("Reservoir datum", parsed.ParametricDatum.Name);
        Assert.True(parsed.EqualParams(roundTripped));
    }

    /// <summary>
    /// Verifies the new CRS readers reject missing datum blocks explicitly.
    /// </summary>
    /// <param name="wkt">The malformed WKT2 input.</param>
    /// <param name="expectedMessageFragment">The expected diagnostic fragment.</param>
    [Theory]
    [InlineData("""ENGCRS["Broken engineering",CS[Cartesian,2],AXIS["x",east],AXIS["y",north],LENGTHUNIT["metre",1]]""", "EDATUM")]
    [InlineData("""TIMECRS["Broken temporal",CS[temporal,1],AXIS["time",north],TIMEUNIT["second",1]]""", "TDATUM")]
    [InlineData("""PARAMETRICCRS["Broken parametric",CS[parametric,1],AXIS["pressure",up],PARAMETRICUNIT["bar",100000]]""", "PDATUM")]
    public void CreateFromWkt_WithMissingDatumBlock_ThrowsArgumentException(string wkt, string expectedMessageFragment)
    {
        Exception exception = Assert.ThrowsAny<Exception>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.Contains(expectedMessageFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies temporal and parametric CRS reject invalid coordinate-system types.
    /// </summary>
    /// <param name="wkt">The malformed WKT2 input.</param>
    /// <param name="expectedMessageFragment">The expected diagnostic fragment.</param>
    [Theory]
    [InlineData("""TIMECRS["Temporal example",TDATUM["Unix epoch",TIMEORIGIN["1970-01-01T00:00:00Z"]],CS[Cartesian,1],AXIS["time",north],TIMEUNIT["second",1]]""", "coordinate system type")]
    [InlineData("""PARAMETRICCRS["Reservoir pressure",PDATUM["Reservoir datum"],CS[Cartesian,1],AXIS["pressure",up],PARAMETRICUNIT["bar",100000]]""", "coordinate system type")]
    public void CreateFromWkt_WithInvalidTemporalOrParametricCsType_ThrowsNotSupportedException(string wkt, string expectedMessageFragment)
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.Contains(expectedMessageFragment, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies higher-dimensional engineering CRS definitions are retained.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithThreeDimensionalEngineeringCrs_ParsesEngineeringCoordinateSystem()
    {
        const string wkt = """ENGCRS["Engineering 3D",EDATUM["Local engineering datum"],CS[Cartesian,3],AXIS["x",east],AXIS["y",north],AXIS["z",up],LENGTHUNIT["metre",1]]""";

        EngineeringCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<EngineeringCoordinateSystem>(CoordinateSystemFactory, wkt);

        Assert.Equal(3, parsed.Dimension);
        Assert.True(parsed.GetUnits(2).EqualParams(LinearUnit.Metre));
        Assert.Equal(AxisOrientationEnum.Up, parsed.GetAxis(2).Orientation);
    }

    /// <summary>
    /// Verifies representative unsupported WKT2 root keywords remain rejected instead of silently normalizing to an unrelated WKT1 path.
    /// </summary>
    /// <param name="keyword">The unsupported top-level WKT2 keyword.</param>
    /// <param name="wkt">The representative WKT2 input.</param>
    /// <param name="expectedMessageFragment">The keyword fragment currently surfaced by the reader path.</param>
    [Theory]
    [MemberData(nameof(UnsupportedTopLevelWkt2Rows))]
    public void CreateFromWkt_WithUnsupportedTopLevelWkt2Keyword_ThrowsArgumentException(string keyword, string wkt, string expectedMessageFragment)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.True(
            exception.Message.Contains(expectedMessageFragment, StringComparison.OrdinalIgnoreCase),
            $"Expected the current reader boundary for {keyword} to surface '{expectedMessageFragment}', but got '{exception.Message}'.");
    }

    /// <summary>
    /// Verifies projected <c>BOUNDCRS</c> examples now retain first-class bound metadata.
    /// </summary>
    /// <param name="fixtureSource">Source file and line for the extracted fixture.</param>
    /// <param name="wkt">BOUNDCRS WKT2 example from the PROJ test corpus.</param>
    [Theory]
    [MemberData(nameof(SupportedProjBoundCrsRows))]
    public void CreateFromWkt_WithSupportedProjBoundCrsFixture_ParsesBoundCoordinateSystem(string fixtureSource, string wkt)
    {
        BoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(CoordinateSystemFactory, wkt);
        ProjectedCoordinateSystem source = Assert.IsType<ProjectedCoordinateSystem>(parsed.SourceCoordinateSystem);
        GeographicCoordinateSystem target = Assert.IsType<GeographicCoordinateSystem>(parsed.TargetCoordinateSystem);
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(parsed.Transformation.Wgs84Parameters);

        Assert.Equal("NAD83 / California zone 3 (ftUS)", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(2227, parsed.AuthorityCode);
        Assert.Equal("NAD83", source.GeographicCoordinateSystem.Name);
        Assert.Equal("North American Datum 1983", source.GeographicCoordinateSystem.HorizontalDatum.Name);
        Assert.Null(source.GeographicCoordinateSystem.HorizontalDatum.Wgs84Parameters);
        Assert.Equal(new Wgs84ConversionInfo(0, 0, 0, 0, 0, 0, 0), parameters);
        Assert.Equal("Lambert Conic Conformal (2SP)", source.Projection.ClassName);
        Assert.Equal(6561666.667d, source.Projection.GetParameter("false_easting")?.Value);
        Assert.Equal(1640416.667d, source.Projection.GetParameter("false_northing")?.Value);
        Assert.Equal("WGS 84", target.Name);
        Assert.True(target.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(target.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich));
        Assert.True(target.AngularUnit.EqualParams(AngularUnit.Degrees));
        Assert.Equal(AxisOrientationEnum.North, target.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.East, target.GetAxis(1).Orientation);
        Assert.True(parsed.Transformation.UsesWgs84Parameters);
        Assert.False(string.IsNullOrWhiteSpace(fixtureSource));
    }

    /// <summary>
    /// Verifies horizontal <c>BOUNDCRS</c> examples with an ellipsoidal 3D source retain a bound wrapper instead of flattening onto the source CRS.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithEllipsoidal3dSourceBoundCrs_ParsesBoundCoordinateSystem()
    {
        BoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(CoordinateSystemFactory, EllipsoidalHeightBoundCrs);
        CompoundCoordinateSystem source = Assert.IsType<CompoundCoordinateSystem>(parsed.SourceCoordinateSystem);
        GeographicCoordinateSystem horizontal = Assert.IsType<GeographicCoordinateSystem>(source.HeadCoordinateSystem);
        VerticalCoordinateSystem vertical = Assert.IsType<VerticalCoordinateSystem>(source.TailCoordinateSystem);
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(parsed.Transformation.Wgs84Parameters);

        Assert.Equal("TWD97", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(3823, parsed.AuthorityCode);
        Assert.Equal(AxisOrientationEnum.North, source.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.East, source.GetAxis(1).Orientation);
        Assert.Equal(AxisOrientationEnum.Up, source.GetAxis(2).Orientation);
        Assert.Equal("Taiwan Datum 1997", horizontal.HorizontalDatum.Name);
        Assert.True(horizontal.HorizontalDatum.Ellipsoid.EqualParams(Ellipsoid.GRS80));
        Assert.Null(horizontal.HorizontalDatum.Wgs84Parameters);
        Assert.Equal(new Wgs84ConversionInfo(0, 0, 0, 0, 0, 0, 0), parameters);
        Assert.Equal(DatumType.VD_Ellipsoidal, vertical.VerticalDatum.DatumType);
        Assert.Equal("ellipsoidal height (h)", vertical.Name);
        Assert.True(parsed.Transformation.UsesWgs84Parameters);
    }

    /// <summary>
    /// Verifies vertical <c>BOUNDCRS</c> examples from the checked-in PROJ tests parse as first-class bound coordinate systems.
    /// </summary>
    /// <param name="fixtureSource">Source file and line for the extracted fixture.</param>
    /// <param name="wkt">BOUNDCRS WKT2 example from the PROJ test corpus.</param>
    [Theory]
    [MemberData(nameof(SupportedVerticalBoundCrsRows))]
    public void CreateFromWkt_WithSupportedVerticalBoundCrsFixture_ParsesBoundCoordinateSystem(string fixtureSource, string wkt)
    {
        BoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(CoordinateSystemFactory, wkt);
        VerticalCoordinateSystem source = Assert.IsType<VerticalCoordinateSystem>(parsed.SourceCoordinateSystem);
        CompoundCoordinateSystem target = Assert.IsType<CompoundCoordinateSystem>(parsed.TargetCoordinateSystem);

        Assert.Equal("EGM96 height", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(5773, parsed.AuthorityCode);
        Assert.Equal("EGM96 geoid", source.VerticalDatum.Name);
        Assert.Equal(DatumType.VD_GeoidModelDerived, source.VerticalDatum.DatumType);
        Assert.Equal("metre", source.LinearUnit.Name);
        Assert.Null(source.BoundGridTransformation);
        Assert.True(parsed.Transformation.UsesParameterFile);
        Assert.Equal("us_nga_egm96_15.tif", parsed.Transformation.ParameterFileName);
        Assert.Equal(3, target.Dimension);
        Assert.False(string.IsNullOrWhiteSpace(fixtureSource));
    }

    /// <summary>
    /// Verifies equivalent <c>PARAMETERFILE</c> paths on repeated vertical <c>BOUNDCRS</c> wrappers do not report a false conflict.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithEquivalentVerticalBoundParameterFilePaths_DoesNotReportConflict()
    {
        string nested = CreateNestedVerticalBoundCrs(@"grids\us_nga_egm96_15.tif", "GRIDS/us_nga_egm96_15.tif");

        BoundCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(CoordinateSystemFactory, nested);
        BoundCoordinateSystem inner = Assert.IsType<BoundCoordinateSystem>(parsed.SourceCoordinateSystem);

        Assert.Equal("EGM96 height", parsed.Name);
        Assert.Equal(@"grids\us_nga_egm96_15.tif", inner.Transformation.ParameterFileName);
        Assert.Equal("GRIDS/us_nga_egm96_15.tif", parsed.Transformation.ParameterFileName);
    }

    private static string GetCatalogWkt(int srid)
    {
        Assert.True(CatalogDefinitions.Value.TryGetValue(srid, out string? wkt), $"SRID {srid} not found in managed EPSG catalog.");
        return wkt ?? string.Empty;
    }

    private static string CreateNestedVerticalBoundCrs(string innerParameterFileName, string outerParameterFileName)
    {
        string inner = VerticalProjBoundCrs.Replace("us_nga_egm96_15.tif", innerParameterFileName, StringComparison.Ordinal);
        return $$"""
            BOUNDCRS[
                SOURCECRS[
                    {{inner}}],
                TARGETCRS[
                    GEOGCRS["WGS 84",
                        DATUM["World Geodetic System 1984",
                            ELLIPSOID["WGS 84",6378137,298.257223563,
                                LENGTHUNIT["metre",1]]],
                        PRIMEM["Greenwich",0,
                            ANGLEUNIT["degree",0.0174532925199433]],
                        CS[ellipsoidal,3],
                            AXIS["latitude",north,
                                ORDER[1],
                                ANGLEUNIT["degree",0.0174532925199433]],
                            AXIS["longitude",east,
                                ORDER[2],
                                ANGLEUNIT["degree",0.0174532925199433]],
                            AXIS["ellipsoidal height",up,
                                ORDER[3],
                                LENGTHUNIT["metre",1]],
                        ID["EPSG",4979]]],
                ABRIDGEDTRANSFORMATION["WGS 84 to EGM96 height (1)",
                    METHOD["Geographic3D to GravityRelatedHeight (EGM)",
                        ID["EPSG",9661]],
                    PARAMETERFILE["Geoid (height correction) model file","{{outerParameterFileName}}"]]]
            """;
    }
}
