// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
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
    /// Provides real unsupported <c>BOUNDCRS</c> fixtures extracted from the checked-in PROJ test suite.
    /// </summary>
    /// <returns>Fixture source labels and WKT2 strings that should remain explicitly unsupported.</returns>
    public static IEnumerable<TheoryDataRow<string, string>> UnsupportedProjBoundCrsRows()
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
    /// Verifies ensemble-backed WKT2 geographic CRS still report the current unsupported boundary explicitly.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithDatumEnsemble_ThrowsNotSupportedException()
    {
        const string wkt = """GEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],CS[ellipsoidal,3,ID["EPSG",6423]],AXIS["Geodetic latitude (Lat)",north,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]]],AXIS["Geodetic longitude (Lon)",east,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]]],AXIS["Ellipsoidal height (h)",up,LENGTHUNIT["metre",1,ID["EPSG",9001]]],ID["EPSG",4979]]""";

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.Contains("ensembles", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies ensemble-backed projected WKT2 CRS still report the current unsupported boundary explicitly.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithEnsembleBasedProjCrs_ThrowsNotSupportedException()
    {
        const string wkt = """PROJCRS["WGS 84 / UTM zone 32N",BASEGEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],ID["EPSG",4326]],CONVERSION["UTM zone 32N",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",500000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",16032]],CS[Cartesian,2,ID["EPSG",4400]],AXIS["Easting (E)",east],AXIS["Northing (N)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",32632]]""";

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.Contains("ensembles", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies projected <c>BOUNDCRS</c> examples map onto the existing source-CRS plus WGS84-parameters model.
    /// </summary>
    /// <param name="fixtureSource">Source file and line for the extracted fixture.</param>
    /// <param name="wkt">BOUNDCRS WKT2 example from the PROJ test corpus.</param>
    [Theory]
    [MemberData(nameof(SupportedProjBoundCrsRows))]
    public void CreateFromWkt_WithSupportedProjBoundCrsFixture_ParsesSourceCrsWithWgs84Parameters(string fixtureSource, string wkt)
    {
        ProjectedCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(CoordinateSystemFactory, wkt);
        Wgs84ConversionInfo parameters = Assert.IsType<Wgs84ConversionInfo>(parsed.GeographicCoordinateSystem.HorizontalDatum.Wgs84Parameters);

        Assert.Equal("NAD83 / California zone 3 (ftUS)", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(2227, parsed.AuthorityCode);
        Assert.Equal("NAD83", parsed.GeographicCoordinateSystem.Name);
        Assert.Equal("North American Datum 1983", parsed.GeographicCoordinateSystem.HorizontalDatum.Name);
        Assert.Equal(new Wgs84ConversionInfo(0, 0, 0, 0, 0, 0, 0), parameters);
        Assert.Same(parsed.HorizontalDatum, parsed.GeographicCoordinateSystem.HorizontalDatum);
        Assert.Equal("Lambert Conic Conformal (2SP)", parsed.Projection.ClassName);
        Assert.Equal(6561666.667d, parsed.Projection.GetParameter("false_easting")?.Value);
        Assert.Equal(1640416.667d, parsed.Projection.GetParameter("false_northing")?.Value);
        Assert.Contains("TOWGS84[0, 0, 0, 0, 0, 0, 0]", parsed.WKT, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(fixtureSource));
    }

    /// <summary>
    /// Verifies unsupported real <c>BOUNDCRS</c> examples from the checked-in PROJ tests remain surfaced as an explicit boundary.
    /// </summary>
    /// <param name="fixtureSource">Source file and line for the extracted fixture.</param>
    /// <param name="wkt">BOUNDCRS WKT2 example from the PROJ test corpus.</param>
    [Theory]
    [MemberData(nameof(UnsupportedProjBoundCrsRows))]
    public void CreateFromWkt_WithUnsupportedProjBoundCrsFixture_ThrowsNotSupportedException(string fixtureSource, string wkt)
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.Contains("BOUNDCRS", exception.Message, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(fixtureSource));
    }

    private static string GetCatalogWkt(int srid)
    {
        Assert.True(CatalogDefinitions.Value.TryGetValue(srid, out string? wkt), $"SRID {srid} not found in managed EPSG catalog.");
        return wkt ?? string.Empty;
    }
}
