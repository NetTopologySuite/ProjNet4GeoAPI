// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.WKT;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using ProjNet.IO.Wkt;
using ProjNet.Tests.IO.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for parsing WKT coordinate system definitions.
/// </summary>
public class WKTCoordSysParserTests
{
    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Srid)
            .ToDictionary(group => group.Key, group => group.Last().Wkt));

    private readonly CoordinateSystemFactory coordinateSystemFactory = new();

    /// <summary>
    /// Tests parsing of the shared EPSG archive fixture for EPSG:2918.
    /// </summary>
    [Fact]
    public void TestProjectedCoordinateSystemEPSG2918()
    {
        string wkt = GetArchiveWkt(2918);

        ProjectedCoordinateSystem pcs = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, wkt);
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            this.coordinateSystemFactory,
            GetManagedWkt(2918));

        ProjectedCoordinateSystem pcs2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, wkt.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal));
        Assert.True(pcs.EqualParams(reference));
        Assert.True(pcs.EqualParams(pcs2));

        CheckInfo(pcs, "NAD83(HARN) / Texas Central (ftUS)", "EPSG", 2918);

        GeographicCoordinateSystem gcs = pcs.GeographicCoordinateSystem;
        CheckInfo(gcs, "NAD83(HARN)", "EPSG", 4152);
        CheckDatum(gcs.HorizontalDatum, "NAD83 (High Accuracy Reference Network)", "EPSG", 6152);
        CheckEllipsoid(gcs.HorizontalDatum.Ellipsoid, "GRS 1980", 6378137, 298.257222101, "EPSG", 7019);
        this.CheckPrimem(gcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901);
        CheckUnit(gcs.AngularUnit, "degree", 0.017453292519943295, "EPSG", 9102);

        Assert.Equal("Lambert Conic Conformal (2SP)", pcs.Projection.ClassName);
        Assert.Equal("SPCS83 Texas Central zone (US survey foot)", pcs.Projection.Name);
        Assert.Equal("EPSG", pcs.Projection.Authority);
        Assert.Equal(15359L, pcs.Projection.AuthorityCode);
        CheckProjectionParameters(
            pcs.Projection,
            [
            Tuple.Create("standard_parallel_1", 31.8833333333336d),
            Tuple.Create("standard_parallel_2", 30.1166666666669d),
            Tuple.Create("latitude_of_origin", 29.6666666666669d),
            Tuple.Create("central_meridian", -100.333333333334d),
            Tuple.Create("false_easting", 2296583.333d),
            Tuple.Create("false_northing", 9842500d),
        ]);

        CheckUnit(pcs.LinearUnit, "US survey foot", 0.304800609601219, "EPSG", 9003);
    }

    /// <summary>
    /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
    /// and tries to parse them.
    /// </summary>
    [Fact]
    public void ParseAllWKTs()
    {
        int parseCount = 0;
        foreach (SRIDReader.WktString wkt in SRIDReader.GetSrids())
        {
            CoordinateSystem? cs1 = this.coordinateSystemFactory.CreateFromWkt(wkt.Wkt);
            Assert.NotNull(cs1);
            CoordinateSystem cs2 = CoordinateSystemTestHelpers.RequireCoordinateSystem(
                this.coordinateSystemFactory,
                wkt.Wkt.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal));
            Assert.True(cs1.EqualParams(cs2));
            parseCount++;
        }

        Assert.True(parseCount > 2671, "Not all WKT was parsed");
    }

    /// <summary>
    /// Verifies that non-coordinate-system WKT returns <see langword="null"/> instead of throwing.
    /// </summary>
    [Fact]
    public void CreateFromWktReturnsNullForNonCoordinateSystemWkt()
    {
        const string wkt = """UNIT["metre",1,AUTHORITY["EPSG","9001"]]""";

        CoordinateSystem? coordinateSystem = this.coordinateSystemFactory.CreateFromWkt(wkt);

        Assert.Null(coordinateSystem);
    }

    /// <summary>
    /// Verifies mixed-case WKT2 keywords are normalized and parsed correctly.
    /// </summary>
    [Fact]
    public void CreateFromWktParsesMixedCaseGeodeticCrsAndEllipsoid()
    {
        const string wkt = """geodeticcrs["WGS 84",DATUM["WGS_1984",Ellipsoid["WGS 84",6378137,298.257223563],id["EPSG","6326"]],PRIMEM["Greenwich",0,id["EPSG","8901"]],UNIT["degree",0.0174532925199433,id["EPSG","9122"]],id["EPSG","4326"]]""";

        GeographicCoordinateSystem coordinateSystem = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(this.coordinateSystemFactory, wkt);

        CheckInfo(coordinateSystem, "WGS 84", "EPSG", 4326);
        CheckDatum(coordinateSystem.HorizontalDatum, "WGS_1984", "EPSG", 6326);
        CheckEllipsoid(coordinateSystem.HorizontalDatum.Ellipsoid, "WGS 84", 6378137, 298.257223563, string.Empty, -1);
    }

    /// <summary>
    /// Verifies SPHEROID parsing succeeds when AUTHORITY is omitted.
    /// </summary>
    [Fact]
    public void CreateFromWktParsesSpheroidWithoutAuthority()
    {
        const string wkt = """GEOGCS["Custom",DATUM["Custom_Datum",SPHEROID["Custom Spheroid",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]]""";

        GeographicCoordinateSystem coordinateSystem = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(this.coordinateSystemFactory, wkt);

        CheckInfo(coordinateSystem, "Custom", string.Empty, -1);
        CheckDatum(coordinateSystem.HorizontalDatum, "Custom_Datum", string.Empty, -1);
        CheckEllipsoid(coordinateSystem.HorizontalDatum.Ellipsoid, "Custom Spheroid", 6378137, 298.257223563, string.Empty, -1);
    }

    /// <summary>
    /// Verifies malformed SPHEROID definitions without AUTHORITY are rejected when bracket types do not match.
    /// </summary>
    [Fact]
    public void ParseSpheroidWithoutAuthorityRejectsMismatchedBrackets()
    {
        const string malformedWkt = """SPHEROID("WGS 84",6378137,298.257223563]""";

        Assert.Throws<WktParseException>(() => ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(malformedWkt));
    }

    /// <summary>
    /// Verifies malformed WKT1 TOWGS84 parameter counts surface as parser failures.
    /// </summary>
    [Fact]
    public void ParseTowgs84WithInvalidValueCountThrowsWktParseException()
    {
        const string malformedWkt =
            """GEOGCS["Custom",DATUM["Custom_Datum",SPHEROID["Custom Spheroid",6378137,298.257223563],TOWGS84[1,2]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]]""";

        Assert.Throws<WktParseException>(() => ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(malformedWkt));
    }

    /// <summary>
    /// Verifies non-numeric AUTHORITY codes are represented as unknown authority code <c>-1</c>.
    /// </summary>
    [Fact]
    public void CreateFromWktUsesMinusOneForNonNumericAuthorityCodes()
    {
        const string wkt =
            """GEOGCS["Custom",DATUM["Custom_Datum",SPHEROID["Custom Spheroid",6378137,298.257223563,AUTHORITY["LOCAL","abc"]],AUTHORITY["LOCAL","abc"]],PRIMEM["Greenwich",0,AUTHORITY["LOCAL","abc"]],UNIT["degree",0.0174532925199433,AUTHORITY["LOCAL","abc"]],AUTHORITY["LOCAL","abc"]]""";

        GeographicCoordinateSystem coordinateSystem = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(this.coordinateSystemFactory, wkt);

        Assert.Equal(-1, coordinateSystem.AuthorityCode);
        Assert.Equal(-1, coordinateSystem.HorizontalDatum.AuthorityCode);
        Assert.Equal(-1, coordinateSystem.HorizontalDatum.Ellipsoid.AuthorityCode);
        Assert.Equal(-1, coordinateSystem.PrimeMeridian.AuthorityCode);
        Assert.Equal(-1, coordinateSystem.AngularUnit.AuthorityCode);
    }

    /// <summary>
    /// Verifies projected coordinate systems without PARAMETER entries parse successfully.
    /// </summary>
    [Fact]
    public void CreateFromWktParsesProjectedCoordinateSystemWithoutParameters()
    {
        const string wkt =
            """PROJCS["Custom",GEOGCS["Custom GCS",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]],PROJECTION["Mercator_1SP"],UNIT["metre",1]]""";

        ProjectedCoordinateSystem coordinateSystem = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, wkt);
        Assert.Equal("Mercator_1SP", coordinateSystem.Projection.ClassName);
        Assert.Equal(0, coordinateSystem.Projection.NumParameters);
    }

    /// <summary>
    /// Verifies WKT2 root keyword aliases are normalized to equivalent WKT1 coordinate system roots.
    /// </summary>
    [Fact]
    public void CreateFromWktParsesWkt2RootKeywordAliases()
    {
        const string projectedWkt =
            """PROJCRS["Custom Projected",GEOGCS["Custom GCS",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]],PROJECTION["Mercator_1SP"],UNIT["metre",1]]""";
        const string verticalWkt =
            """VERTCRS["Custom Height",VERT_DATUM["Custom Vertical Datum",2005],UNIT["metre",1],AXIS["Up",UP]]""";
        const string compoundWkt =
            """COMPOUNDCRS["Compound",GEOGCS["Custom GCS",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["degree",0.0174532925199433]],VERT_CS["Custom Height",VERT_DATUM["Custom Vertical Datum",2005],UNIT["metre",1],AXIS["Up",UP]]]""";

        ProjectedCoordinateSystem projected = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, projectedWkt);
        VerticalCoordinateSystem vertical = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(this.coordinateSystemFactory, verticalWkt);
        CompoundCoordinateSystem compound = CoordinateSystemTestHelpers.RequireCoordinateSystem<CompoundCoordinateSystem>(this.coordinateSystemFactory, compoundWkt);

        Assert.Equal("Custom Projected", projected.Name);
        Assert.Equal("Custom Height", vertical.Name);
        Assert.Equal("Compound", compound.Name);
    }

    /// <summary>
    /// Verifies BOUNDCRS roots are surfaced as explicitly unsupported instead of unrecognized.
    /// </summary>
    [Fact]
    public void CreateFromWktBoundCrsThrowsNotSupported()
    {
        const string wkt = "BoundCrs[]";

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => this.coordinateSystemFactory.CreateFromWkt(wkt));
        Assert.Contains("BOUNDCRS", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
    /// and tries to create a transformation with them.
    /// </summary>
    [Fact]
    public void TestCreateCoordinateTransformationForWktInCsv()
    {
        // GeographicCoordinateSystem.WGS84
        var fac = new CoordinateSystemFactory();
        int parseCount = 0;
        int failedCss = 0;
        var failedProjections = new HashSet<string>();
        using Stream stream = Assert.IsType<Stream>(Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjNET.Tests.SRID.csv"), exactMatch: false);
        using (var sr = new StreamReader(stream, Encoding.UTF8))
        {
            var ctFactory = new CoordinateTransformationFactory();
            while (!sr.EndOfStream)
            {
                string line = Assert.IsType<string>(sr.ReadLine());
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int split = line.IndexOf(';', StringComparison.Ordinal);
                if (split > -1)
                {
                    string wkt = line[(split + 1)..];
                    CoordinateSystem? cs = fac.CreateFromWkt(wkt);
                    if (cs is null)
                    {
                        continue; // We check this in another test.
                    }

                    if (cs is ProjectedCoordinateSystem pcs)
                    {
                        switch (pcs.Projection.ClassName)
                        {
                            // Skip not supported projections
                            case "Oblique_Stereographic":
                            case "Transverse_Mercator_South_Orientated":
                            case "Lambert_Conformal_Conic_1SP":
                            case "Lambert_Azimuthal_Equal_Area":
                            case "Tunisia_Mining_Grid":
                            case "New_Zealand_Map_Grid":
                            case "Polyconic":
                            case "Lambert_Conformal_Conic_2SP_Belgium":
                            case "Polar_Stereographic":
                            case "Hotine_Oblique_Mercator_Azimuth_Center":
                            case "Mercator_1SP":
                            case "Mercator_2SP":
                            case "Cylindrical_Equal_Area":
                            case "Equirectangular":
                            case "Laborde_Oblique_Mercator":
                                continue;
                        }
                    }

                    try
                    {
                        ctFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, cs);
                    }
                    catch (Exception)
                    {
                        if (cs is ProjectedCoordinateSystem ics)
                        {
                            if (!failedProjections.Contains(ics.Projection.ClassName))
                            {
                                failedProjections.Add(ics.Projection.ClassName);
                            }
                        }
                        else
                        {
                            Assert.True(false);
                        }

                        failedCss += 1;

                        // Assert.Fail(
                        //        $"Could not create transformation from:\r\n{wkt}\r\n{ex.Message}\r\nClass name:{ics.Projection.ClassName}");
                        // else
                        //    Assert.Fail($"Could not create transformation from:\r\n{wkt}\r\n{ex.Message}");
                    }

                    parseCount++;
                }
            }
        }

        Assert.True(parseCount >= 2556, "Not all WKT was processed");
        if (failedCss > 0)
        {
            Console.WriteLine($"Failed to create transfroms for {failedCss} coordinate systems");
            foreach (string fp in failedProjections)
            {
                Console.WriteLine($"case \"{fp}\":");
            }
        }
    }

    /// <summary>
    /// Tests parsing of the shared EPSG archive fixture for EPSG:27700.
    /// </summary>
    [Fact]
    public void TestProjectedCoordinateSystemEPSG27700()
    {
        string wkt = GetArchiveWkt(27700);

        ProjectedCoordinateSystem pcs = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, wkt);
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            this.coordinateSystemFactory,
            GetManagedWkt(27700));

        ProjectedCoordinateSystem pcs2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, wkt.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal));
        Assert.True(pcs.EqualParams(reference));
        Assert.True(pcs.EqualParams(pcs2));

        CheckInfo(pcs, "OSGB36 / British National Grid", "EPSG", 27700);

        GeographicCoordinateSystem gcs = pcs.GeographicCoordinateSystem;
        CheckInfo(gcs, "OSGB36", "EPSG", 4277);
        CheckDatum(gcs.HorizontalDatum, "Ordnance Survey of Great Britain 1936", "EPSG", 6277);
        CheckEllipsoid(gcs.HorizontalDatum.Ellipsoid, "Airy 1830", 6377563.396, 299.3249646, "EPSG", 7001);
        this.CheckPrimem(gcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901);
        CheckUnit(gcs.AngularUnit, "degree", 0.017453292519943295, "EPSG", 9102);

        Assert.Equal("Transverse Mercator", pcs.Projection.ClassName);
        Assert.Equal("British National Grid", pcs.Projection.Name);
        Assert.Equal("EPSG", pcs.Projection.Authority);
        Assert.Equal(19916L, pcs.Projection.AuthorityCode);
        CheckProjectionParameters(
            pcs.Projection,
            [
            Tuple.Create("latitude_of_origin", 49d),
            Tuple.Create("central_meridian", -2d),
            Tuple.Create("scale_factor", 0.9996012717),
            Tuple.Create("false_easting", 400000d),
            Tuple.Create("false_northing", -100000d),
        ]);

        CheckUnit(pcs.LinearUnit, "metre", 1d, "EPSG", 9001);

        ProjectedCoordinateSystem roundTripped = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(this.coordinateSystemFactory, pcs.WKT);
        Assert.True(pcs.EqualParams(roundTripped));
    }

    /// <summary>
    /// Verifies that a WGS 84 Pseudo-Mercator WKT definition sourced from spatialreference.org can be parsed without errors.
    /// </summary>
    [Fact]
    public void TestParseSrOrg()
    {
        const string wkt =
            """
            PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["Popular Visualisation CRS",
            DATUM["Popular_Visualisation_Datum",SPHEROID["Popular Visualisation Sphere",
            6378137,0,AUTHORITY["EPSG","7059"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG",
            "6055"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",
            0.01745329251994328,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4055"]],
            PROJECTION["Mercator_1SP"],
            PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER[
            "false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["X",EAST],AXIS["Y",NORTH]],AUTHORITY["EPSG","3785"]]
            """;

        Assert.Null(Record.Exception(() => this.coordinateSystemFactory.CreateFromWkt(wkt)));
    }

    /// <summary>
    /// Verifies that known problematic WKT definitions can be parsed without errors.
    /// </summary>
    [Fact]
    public void TestProjNetIssues()
    {
        const string firstIssueWkt =
            """
            PROJCS["International_Terrestrial_Reference_Frame_1992Lambert_Conformal_Conic_2SP",
            GEOGCS["GCS_International_Terrestrial_Reference_Frame_1992",
            DATUM["International_Terrestrial_Reference_Frame_1992",
            SPHEROID["GRS_1980",6378137,298.257222101],
            TOWGS84[0,0,0,0,0,0,0]],
            PRIMEM["Greenwich",0],
            UNIT["Degree",0.0174532925199433]],
            PROJECTION["Lambert_Conformal_Conic_2SP",AUTHORITY["EPSG","9802"]],
            PARAMETER["Central_Meridian",-102],
            PARAMETER["Latitude_Of_Origin",12],
            PARAMETER["False_Easting",2500000],
            PARAMETER["False_Northing",0],
            PARAMETER["Standard_Parallel_1",17.5],
            PARAMETER["Standard_Parallel_2",29.5],
            PARAMETER["Scale_Factor",1],
            UNIT["Meter",1,AUTHORITY["EPSG","9001"]]]
            """;
        const string secondIssueWkt =
            """
            PROJCS["Google Maps Global Mercator",
            GEOGCS["WGS 84",
            DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],
            AUTHORITY["EPSG","6326"]],
            PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],
            UNIT["degree",0.01745329251994328,AUTHORITY["EPSG","9122"]],
            AUTHORITY["EPSG","4326"]],
            PROJECTION["Mercator_2SP"],
            PARAMETER["standard_parallel_1",0],
            PARAMETER["latitude_of_origin",0],
            PARAMETER["central_meridian",0],
            PARAMETER["false_easting",0],
            PARAMETER["false_northing",0],
            UNIT["Meter",1],
            EXTENSION["PROJ4","+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs"],
            AUTHORITY["EPSG","900913"]]
            """;

        Assert.Null(Record.Exception(() => this.coordinateSystemFactory.CreateFromWkt(firstIssueWkt)));
        Assert.Null(Record.Exception(() => this.coordinateSystemFactory.CreateFromWkt(secondIssueWkt)));
    }

    /// <summary>
    /// Test parsing of a <see cref="FittedCoordinateSystem"/> from WKT.
    /// </summary>
    [Fact]
    public void TestFittedCoordinateSystemWkt()
    {
        var fac = new CoordinateSystemFactory();
        FittedCoordinateSystem fcs = default!;
        const string wkt =
            """
            FITTED_CS["Local coordinate system MNAU (based on Gauss-Krueger)",
                PARAM_MT["Affine",
                    PARAMETER["num_row",3],PARAMETER["num_col",3],PARAMETER["elt_0_0", 0.883485346527455],PARAMETER["elt_0_1", -0.468458794848877],PARAMETER["elt_0_2", 3455869.17937689],PARAMETER["elt_1_0", 0.468458794848877],PARAMETER["elt_1_1", 0.883485346527455],PARAMETER["elt_1_2", 5478710.88035753],PARAMETER["elt_2_2", 1]],
                PROJCS["DHDN / Gauss-Kruger zone 3",
                    GEOGCS["DHDN",
                        DATUM["Deutsches_Hauptdreiecksnetz",
                            SPHEROID["Bessel 1841", 6377397.155, 299.1528128, AUTHORITY["EPSG", "7004"]],
                            TOWGS84[612.4, 77, 440.2, -0.054, 0.057, -2.797, 0.525975255930096],
                            AUTHORITY["EPSG", "6314"]],
                        PRIMEM["Greenwich", 0, AUTHORITY["EPSG", "8901"]],
                        UNIT["degree", 0.0174532925199433, AUTHORITY["EPSG", "9122"]],
                        AUTHORITY["EPSG", "4314"]],
                    PROJECTION["Transverse_Mercator"],
                    PARAMETER["latitude_of_origin", 0],
                    PARAMETER["central_meridian", 9],
                    PARAMETER["scale_factor", 1],
                    PARAMETER["false_easting", 3500000],
                    PARAMETER["false_northing", 0],
                    UNIT["metre", 1, AUTHORITY["EPSG", "9001"]],
                    AUTHORITY["EPSG", "31467"]]
            ]
            """;

        try
        {
            fcs = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(fac, wkt);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Could not create fitted coordinate system from:\r\n{wkt}\r\n{ex.Message}");
        }

        Assert.NotNull(fcs);
        Assert.False(string.IsNullOrEmpty(fcs.ToBase()));
        Assert.NotNull(fcs.BaseCoordinateSystem);

        Assert.Equal("Local coordinate system MNAU (based on Gauss-Krueger)", fcs.Name);

        // Assert.AreEqual ("CUSTOM", fcs.Authority);
        // Assert.AreEqual (123456, fcs.AuthorityCode);
        Assert.Equal("EPSG", fcs.BaseCoordinateSystem.Authority);
        Assert.Equal(31467, fcs.BaseCoordinateSystem.AuthorityCode);
    }

    /// <summary>
    /// Tests parsing of the shared EPSG archive fixture for EPSG:5250.
    /// </summary>
    [Fact]
    public void TestGeocentricCoordinateSystem()
    {
        var fac = new CoordinateSystemFactory();
        GeocentricCoordinateSystem fcs = default!;

        string wkt = GetArchiveWkt(5250);
        GeocentricCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeocentricCoordinateSystem>(
            fac,
            GetManagedWkt(5250));

        try
        {
            fcs = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeocentricCoordinateSystem>(fac, wkt);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Could not create geocentric coordinate system from:\r\n{wkt}\r\n{ex.Message}");
        }

        Assert.NotNull(fcs);
        Assert.True(fcs.EqualParams(reference));
        Assert.True(CheckInfo(fcs, "TUREF", "EPSG", 5250L));
        Assert.True(CheckDatum(fcs.HorizontalDatum, "Turkish National Reference Frame", "EPSG", 1057L));
        Assert.True(CheckEllipsoid(fcs.HorizontalDatum.Ellipsoid, "GRS 1980", 6378137, 298.257222101, "EPSG", 7019));
        Assert.True(this.CheckPrimem(fcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901L));
        Assert.True(CheckUnit(fcs.PrimeMeridian.AngularUnit, "degree", 0.017453292519943295, "EPSG", 9102L));
        Assert.True(CheckUnit(fcs.LinearUnit, "metre", 1, "EPSG", 9001L));

        Assert.Equal("EPSG", fcs.Authority);
        Assert.Equal(5250L, fcs.AuthorityCode);
    }

    /// <summary>
    /// Verifies that WKT produced by a coordinate system object can be round-tripped back into an equivalent projected coordinate system.
    /// </summary>
    [Fact]
    public void ParseWktCreatedByCoordinateSystem()
    {
        // Sample WKT from an external source.
        const string sampleWKT =
            """
            PROJCS["",
              GEOGCS["",
                DATUM["",
                  SPHEROID["GRS_1980", 6378137, 298.2572221010042]
                ],
              PRIMEM["Greenwich", 0],
              UNIT["Degree", 0.017453292519943295]
              ],
              PROJECTION["Transverse_Mercator"],
              PARAMETER["False_Easting", 500000],
              PARAMETER["False_Northing", 0],
              PARAMETER["Central_Meridian", -75],
              PARAMETER["Scale_Factor", 0.9996],
              UNIT["Meter", 1]
              ]
            """;

        var csFromSample = (CoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(sampleWKT);
        string wktFromProjNetCS = csFromSample.WKT;
        IInfo parsed = ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wktFromProjNetCS);
        Assert.IsType<ProjectedCoordinateSystem>(parsed);
        var projCS = (ProjectedCoordinateSystem)parsed;
        Assert.NotNull(projCS.LinearUnit);
        Assert.Equal("Meter", projCS.LinearUnit.Name);
        Assert.Equal(1, projCS.LinearUnit.MetersPerUnit);
    }

    /// <summary>
    /// Verifies that a <c>PROJECTEDCRS</c> WKT using WKT2-style root and <c>ID</c> tokens can be parsed into a projected coordinate system with correct authority metadata.
    /// </summary>
    [Fact]
    public void ParseProjectedCrsWithWkt2LikeRootAndIdentifiers()
    {
        const string wkt =
            """
            PROJECTEDCRS["WGS 84 / Pseudo-Mercator",
                GEODCRS["WGS 84",
                    DATUM["WGS_1984",ELLIPSOID["WGS 84",6378137,298.257223563,ID["EPSG","7030"]],ID["EPSG","6326"]],
                    PRIMEM["Greenwich",0,ID["EPSG","8901"]],
                    UNIT["degree",0.0174532925199433,ID["EPSG","9122"]],
                    ID["EPSG","4326"]],
                PROJECTION["Mercator_1SP"],
                PARAMETER["central_meridian",0],
                PARAMETER["scale_factor",1],
                PARAMETER["false_easting",0],
                PARAMETER["false_northing",0],
                UNIT["metre",1,ID["EPSG","9001"]],
                AXIS["X",EAST],
                AXIS["Y",NORTH],
                ID["EPSG","3857"]]
            """;

        var parsed = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        Assert.NotNull(parsed);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(3857L, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies that a <c>GEODCRS</c> WKT using <c>ELLIPSOID</c> and <c>ID</c> tokens can be parsed into a geographic coordinate system with correct datum and ellipsoid.
    /// </summary>
    [Fact]
    public void ParseGeodCrsWithEllipsoidAndIdTokens()
    {
        const string wkt =
            """
            GEODCRS["WGS 84",
                DATUM["WGS_1984",ELLIPSOID["WGS 84",6378137,298.257223563,ID["EPSG","7030"]],ID["EPSG","6326"]],
                PRIMEM["Greenwich",0,ID["EPSG","8901"]],
                UNIT["degree",0.0174532925199433,ID["EPSG","9122"]],
                ID["EPSG","4326"]]
            """;

        var parsed = (GeographicCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        Assert.NotNull(parsed);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4326L, parsed.AuthorityCode);
        Assert.NotNull(parsed.HorizontalDatum);
        Assert.NotNull(parsed.HorizontalDatum.Ellipsoid);
    }

    /// <summary>
    /// Verifies that a <c>PROJECTEDCRS</c> WKT using the <c>GEODETICCRS</c> keyword and space-separated <c>ID</c> tokens can be parsed with correct authority metadata.
    /// </summary>
    [Fact]
    public void ParseProjectedCrsWithSpacedIdTokens()
    {
        const string wkt =
            """
            PROJECTEDCRS["WGS 84 / Pseudo-Mercator",
                GEODETICCRS["WGS 84",
                    DATUM["WGS_1984",ELLIPSOID["WGS 84",6378137,298.257223563,ID ["EPSG","7030"]],ID ["EPSG","6326"]],
                    PRIMEM["Greenwich",0,ID ["EPSG","8901"]],
                    UNIT["degree",0.0174532925199433,ID ["EPSG","9122"]],
                    ID ["EPSG","4326"]],
                PROJECTION["Mercator_1SP"],
                PARAMETER["central_meridian",0],
                PARAMETER["scale_factor",1],
                PARAMETER["false_easting",0],
                PARAMETER["false_northing",0],
                UNIT["metre",1,ID ["EPSG","9001"]],
                AXIS["X",EAST],
                AXIS["Y",NORTH],
                ID ["EPSG","3857"]]
            """;

        var parsed = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        Assert.NotNull(parsed);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(3857L, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies that span-based WKT parsing returns the same coordinate system metadata as string parsing.
    /// </summary>
    [Fact]
    public void ParseReadOnlySpanWktMatchesStringParse()
    {
        const string wkt =
            """
            PROJECTEDCRS["WGS 84 / Pseudo-Mercator",
                GEODCRS["WGS 84",
                    DATUM["WGS_1984",ELLIPSOID["WGS 84",6378137,298.257223563,ID["EPSG","7030"]],ID["EPSG","6326"]],
                    PRIMEM["Greenwich",0,ID["EPSG","8901"]],
                    UNIT["degree",0.0174532925199433,ID["EPSG","9122"]],
                    ID["EPSG","4326"]],
                PROJECTION["Mercator_1SP"],
                PARAMETER["central_meridian",0],
                PARAMETER["scale_factor",1],
                PARAMETER["false_easting",0],
                PARAMETER["false_northing",0],
                UNIT["metre",1,ID["EPSG","9001"]],
                AXIS["X",EAST],
                AXIS["Y",NORTH],
                ID["EPSG","3857"]]
            """;

        var fromString = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        var fromSpan = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt.AsSpan());

        Assert.NotNull(fromString);
        Assert.NotNull(fromSpan);
        Assert.True(fromString.EqualParams(fromSpan));
        Assert.Equal(fromString.Authority, fromSpan.Authority);
        Assert.Equal(fromString.AuthorityCode, fromSpan.AuthorityCode);
    }

    /// <summary>
    /// Verifies that whitespace-only span input is rejected by the span-based parser overload.
    /// </summary>
    [Fact]
    public void ParseReadOnlySpanWhitespaceThrowsArgumentNullException()
    {
        const string whitespaceWkt = "   \t\r\n";

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(whitespaceWkt.AsSpan()));

        Assert.Equal("wkt", exception.ParamName);
    }

    private static string GetArchiveWkt(int srid)
        => EpsgArchiveWktFixtureSource.GetFixture(srid).Wkt;

    private static string GetManagedWkt(int srid)
    {
        if (!CatalogDefinitions.Value.TryGetValue(srid, out string? wkt))
        {
            throw new InvalidOperationException($"SRID {srid} not found in the managed EPSG catalog.");
        }

        return wkt;
    }

    private bool CheckPrimem(PrimeMeridian primeMeridian, string name, double? longitude, string authority, long? code)
    {
        Assert.NotNull(primeMeridian);
        Assert.True(CheckInfo(primeMeridian, name, authority, code));
        Assert.Equal(longitude, primeMeridian.Longitude);
        return true;
    }

    private static bool CheckUnit(IUnit unit, string name, double? value, string? authority, long? code)
    {
        Assert.NotNull(unit);
        Assert.True(CheckInfo(unit, name, authority, code));
        Assert.True(unit is LinearUnit || unit is AngularUnit);

        if (!value.HasValue)
        {
            return true;
        }

        if (unit is LinearUnit lunit)
        {
            Assert.Equal(value, lunit.MetersPerUnit);
        }
        else if (unit is AngularUnit aunit)
        {
            Assert.Equal(value, aunit.RadiansPerUnit);
        }

        return true;
    }

    private static bool CheckEllipsoid(Ellipsoid ellipsoid, string name, double? semiMajor, double? inverseFlattening, string authority, long? code)
    {
        Assert.NotNull(ellipsoid);
        Assert.True(CheckInfo(ellipsoid, name, authority, code));
        if (semiMajor.HasValue)
        {
            Assert.Equal(semiMajor, ellipsoid.SemiMajorAxis);
        }

        if (inverseFlattening.HasValue)
        {
            Assert.Equal(inverseFlattening, ellipsoid.InverseFlattening);
        }

        return true;
    }

    private static bool CheckDatum(Datum datum, string name, string authority, long? code)
    {
        Assert.NotNull(datum);
        Assert.IsType<HorizontalDatum>(datum);

        Assert.True(CheckInfo(datum, name, authority, code));

        return true;
    }

    private static bool CheckInfo(IInfo info, string name, string? authority = null, long? code = null)
    {
        Assert.NotNull(info);
        if (!string.IsNullOrWhiteSpace(name))
        {
            Assert.Equal(name, info.Name);
        }

        if (!string.IsNullOrWhiteSpace(authority))
        {
            Assert.Equal(authority, info.Authority);
        }

        if (code.HasValue)
        {
            Assert.Equal(code, info.AuthorityCode);
        }

        return true;
    }

    private static void CheckProjection(IProjection projection, string name, Tuple<string, double>[]? pp = null, string? authority = null, long? code = null)
    {
        Assert.NotNull(projection);
        Assert.Equal(name, projection.ClassName);
        CheckInfo(projection, name, authority, code);
        CheckProjectionParameters(projection, pp);
    }

    private static void CheckProjectionParameters(IProjection projection, Tuple<string, double>[]? pp = null)
    {
        if (pp is null)
        {
            return;
        }

        Assert.Equal(pp.Length, projection.NumParameters);

        for (int i = 0; i < pp.Length; i++)
        {
            ProjectionParameter par = Assert.IsType<ProjectionParameter>(projection.GetParameter(pp[i].Item1));
            Assert.Equal(pp[i].Item1, par.Name, ignoreCase: true);
            Assert.Equal(pp[i].Item2, par.Value);
        }
    }
}
