// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;

/// <summary>
/// Measures <see cref="CoordinateSystemWktReader.Parse(string)"/> throughput for WKT strings of varying complexity.
/// </summary>
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Benchmark entry types are intentionally public for BenchmarkDotNet discovery.")]
[MemoryDiagnoser]
[SimpleJob]
public class WktParsingBenchmarks
{
    private readonly string simpleGeographicWkt =
        "GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925199433]]";

    private readonly string projectedWkt =
        "PROJCS[\"WGS 84 / UTM zone 32N\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1]]";

    private readonly string compoundWkt =
        "COMPD_CS[\"WGS 84 + EGM96 height\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],VERT_CS[\"EGM96 height\",VERT_DATUM[\"EGM96 geoid\",2005],UNIT[\"metre\",1]]]";

    private readonly string geodeticWkt2 =
        """GEOGCRS["ED50",DATUM["European Datum 1950",ELLIPSOID["International 1924",6378388,297,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7022]],ID["EPSG",6230]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4230]]""";

    private readonly string projectedWkt2 =
        """PROJCRS["WGS 84 / UTM zone 32N",BASEGEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],ID["EPSG",4326]],CONVERSION["UTM zone 32N",METHOD["Transverse Mercator",ID["EPSG",9807]],PARAMETER["Latitude of natural origin",0,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8801]],PARAMETER["Longitude of natural origin",9,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",8802]],PARAMETER["Scale factor at natural origin",0.9996,SCALEUNIT["unity",1,ID["EPSG",9201]],ID["EPSG",8805]],PARAMETER["False easting",500000,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8806]],PARAMETER["False northing",0,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",8807]],ID["EPSG",16032]],CS[Cartesian,2,ID["EPSG",4400]],AXIS["Easting (E)",east],AXIS["Northing (N)",north],LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",32632]]""";

    private readonly string boundWkt2 =
        """
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

    /// <summary>
    /// Parses a simple WGS84 geographic coordinate system WKT string.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark(Baseline = true)]
    public IInfo ParseSimpleGeographicCs()
    {
        return CoordinateSystemWktReader.Parse(this.simpleGeographicWkt);
    }

    /// <summary>
    /// Parses a UTM Zone 32N projected coordinate system WKT string with Transverse Mercator projection.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseProjectedCs()
    {
        return CoordinateSystemWktReader.Parse(this.projectedWkt);
    }

    /// <summary>
    /// Parses a compound coordinate system WKT string with both horizontal and vertical components.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseCompoundCs()
    {
        return CoordinateSystemWktReader.Parse(this.compoundWkt);
    }

    /// <summary>
    /// Parses a simple WKT2 geographic coordinate system string.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseGeodeticWkt2()
    {
        return CoordinateSystemWktReader.Parse(this.geodeticWkt2);
    }

    /// <summary>
    /// Parses a projected WKT2 coordinate system string.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseProjectedWkt2()
    {
        return CoordinateSystemWktReader.Parse(this.projectedWkt2);
    }

    /// <summary>
    /// Parses a bound WKT2 coordinate system string with an abridged transformation.
    /// </summary>
    /// <returns>The parsed coordinate system info.</returns>
    [Benchmark]
    public IInfo ParseBoundWkt2()
    {
        return CoordinateSystemWktReader.Parse(this.boundWkt2);
    }
}
