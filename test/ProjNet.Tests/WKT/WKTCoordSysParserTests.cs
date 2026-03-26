// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests.WKT;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class WKTCoordSysParserTests
{
    private readonly CoordinateSystemFactory coordinateSystemFactory = new CoordinateSystemFactory();

    /// <summary>
    /// Parses a coordinate system WKT.
    /// </summary>
    /// <remarks><code>
    /// PROJCS["NAD83(HARN) / Texas Central (ftUS)",
    ///     GEOGCS[
    ///         "NAD83(HARN)",
    ///         DATUM[
    ///             "NAD83_High_Accuracy_Regional_Network",
    ///             SPHEROID[
    ///                 "GRS 1980",
    ///                 6378137,
    ///                 298.257222101,
    ///                 AUTHORITY["EPSG","7019"]
    ///             ],
    ///             TOWGS84[725,685,536,0,0,0,0],
    ///             AUTHORITY["EPSG","6152"]
    ///         ],
    ///         PRIMEM[
    ///             "Greenwich",
    ///             0,
    ///             AUTHORITY["EPSG","8901"]
    ///         ],
    ///         UNIT[
    ///             "degree",
    ///             0.01745329251994328,
    ///             AUTHORITY["EPSG","9122"]
    ///         ],
    ///         AUTHORITY["EPSG","4152"]
    ///     ],
    ///     PROJECTION["Lambert_Conformal_Conic_2SP"],
    ///     PARAMETER["standard_parallel_1",31.88333333333333],
    ///     PARAMETER["standard_parallel_2",30.11666666666667],
    ///     PARAMETER["latitude_of_origin",29.66666666666667],
    ///     PARAMETER["central_meridian",-100.3333333333333],
    ///     PARAMETER["false_easting",2296583.333],
    ///     PARAMETER["false_northing",9842500.000000002],
    ///     UNIT[
    ///         "US survey foot",
    ///         0.3048006096012192,
    ///         AUTHORITY["EPSG","9003"]
    ///     ],
    ///     AUTHORITY["EPSG","2918"]
    /// ]
    /// </code></remarks>
    [Xunit.Fact]
    public void TestProjectedCoordinateSystemEPSG2918()
    {
        const string wkt = "PROJCS[\"NAD83(HARN) / Texas Central (ftUS)\", " +
                                    "GEOGCS[\"NAD83(HARN)\", " +
                                             "DATUM[\"NAD83_High_Accuracy_Regional_Network\", " +
                                                     "SPHEROID[\"GRS 1980\", 6378137, 298.257222101, AUTHORITY[\"EPSG\", \"7019\"]], " +
                                                     "TOWGS84[725, 685, 536, 0, 0, 0, 0], " +
                                                     "AUTHORITY[\"EPSG\", \"6152\"]], " +
                                             "PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], " +
                                             "UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]], " +
                                             "AUTHORITY[\"EPSG\", \"4152\"]], " +
                                    "PROJECTION[\"Lambert_Conformal_Conic_2SP\"], " +
                                    "PARAMETER[\"standard_parallel_1\", 31.883333333333], " +
                                    "PARAMETER[\"standard_parallel_2\", 30.1166666667], " +
                                    "PARAMETER[\"latitude_of_origin\", 29.6666666667], " +
                                    "PARAMETER[\"central_meridian\", -100.333333333333], " +
                                    "PARAMETER[\"false_easting\", 2296583.333], " +
                                    "PARAMETER[\"false_northing\", 9842500], " +
                                    "UNIT[\"US survey foot\", 0.304800609601219, AUTHORITY[\"EPSG\", \"9003\"]], " +
                                    "AUTHORITY[\"EPSG\", \"2918\"]]";

        ProjectedCoordinateSystem pcs = null;
        Assert.Null(Record.Exception(() => pcs = this.coordinateSystemFactory.CreateFromWkt(wkt) as ProjectedCoordinateSystem));

        ProjectedCoordinateSystem pcs2 = null;
        Assert.Null(Record.Exception(() => pcs2 = this.coordinateSystemFactory.CreateFromWkt(wkt.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal)) as ProjectedCoordinateSystem));
        Assert.True(pcs.EqualParams(pcs2));

        Assert.NotNull(pcs);
        CheckInfo(pcs, "NAD83(HARN) / Texas Central (ftUS)", "EPSG", 2918);

        var gcs = pcs.GeographicCoordinateSystem;
        CheckInfo(gcs, "NAD83(HARN)", "EPSG", 4152);
        CheckDatum(gcs.HorizontalDatum, "NAD83_High_Accuracy_Regional_Network", "EPSG", 6152);
        CheckEllipsoid(gcs.HorizontalDatum.Ellipsoid, "GRS 1980", 6378137, 298.257222101, "EPSG", 7019);
        Assert.Equal(new Wgs84ConversionInfo(725, 685, 536, 0, 0, 0, 0), pcs.GeographicCoordinateSystem.HorizontalDatum.Wgs84Parameters);
        this.CheckPrimem(gcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901);
        CheckUnit(gcs.AngularUnit, "degree", 0.0174532925199433, "EPSG", 9122);

        CheckProjection(pcs.Projection, "Lambert_Conformal_Conic_2SP", new[]
        {
            Tuple.Create("standard_parallel_1", 31.883333333333),
            Tuple.Create("standard_parallel_2", 30.1166666667),
            Tuple.Create("latitude_of_origin", 29.6666666667),
            Tuple.Create("central_meridian", -100.333333333333),
            Tuple.Create("false_easting", 2296583.333),
            Tuple.Create("false_northing", 9842500d),
        });

        CheckUnit(pcs.LinearUnit, "US survey foot", 0.304800609601219, "EPSG", 9003);
    }

    /// <summary>
    /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
    /// and tries to parse them.
    /// </summary>
    [Xunit.Fact]
    public void ParseAllWKTs()
    {
        int parseCount = 0;
        foreach (var wkt in SRIDReader.GetSrids())
        {
            var cs1 = this.coordinateSystemFactory.CreateFromWkt(wkt.Wkt);
            Assert.NotNull(cs1);
            var cs2 = this.coordinateSystemFactory.CreateFromWkt(wkt.Wkt.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal));
            Assert.True(cs1.EqualParams(cs2));
            parseCount++;
        }

        Assert.True(parseCount > 2671, "Not all WKT was parsed");
    }

    /// <summary>
    /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
    /// and tries to create a transformation with them.
    /// </summary>
    [Xunit.Fact]
    public void TestCreateCoordinateTransformationForWktInCsv()
    {
        // GeographicCoordinateSystem.WGS84
        var fac = new CoordinateSystemFactory();
        int parseCount = 0;
        int failedCss = 0;
        var failedProjections = new HashSet<string>();
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjNET.Tests.SRID.csv"))
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                var ctFactory = new CoordinateTransformationFactory();
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    int split = line.IndexOf(';', StringComparison.Ordinal);
                    if (split > -1)
                    {
                        string wkt = line.Substring(split + 1);
                        var cs = fac.CreateFromWkt(wkt);
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
    /// Test parsing of a <see cref="ProjectedCoordinateSystem"/> from WKT.
    /// </summary>
    [Xunit.Fact]
    public void TestProjectedCoordinateSystemEPSG27700UnitBeforeProjection()
    {
        const string wkt = "PROJCS[\"OSGB 1936 / British National Grid\"," +
             "GEOGCS[\"OSGB 1936\"," +
                      "DATUM[\"OSGB_1936\"," +
                              "SPHEROID[\"Airy 1830\",6377563.396,299.3249646,AUTHORITY[\"EPSG\",\"7001\"]]," +
                              "AUTHORITY[\"EPSG\",\"6277\"]]," +
                      "PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]]," +
                      "UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]]," +
                      "AUTHORITY[\"EPSG\",\"4277\"]]," +
             "PROJECTION[\"Transverse_Mercator\"]," +
             "PARAMETER[\"latitude_of_origin\",49]," +
             "PARAMETER[\"central_meridian\",-2]," +
             "PARAMETER[\"scale_factor\",0.9996012717]," +
             "PARAMETER[\"false_easting\",400000]," +
             "PARAMETER[\"false_northing\",-100000]," +
             "UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]]," +
             "AXIS[\"Easting\",EAST]," +
             "AXIS[\"Northing\",NORTH]," +
             "AUTHORITY[\"EPSG\",\"27700\"]]";

        ProjectedCoordinateSystem pcs = null;
        Assert.Null(Record.Exception(() => pcs = this.coordinateSystemFactory.CreateFromWkt(wkt) as ProjectedCoordinateSystem));

        ProjectedCoordinateSystem pcs2 = null;
        Assert.Null(Record.Exception(() => pcs2 = this.coordinateSystemFactory.CreateFromWkt(wkt.Replace("[", "(", StringComparison.Ordinal).Replace("]", ")", StringComparison.Ordinal)) as ProjectedCoordinateSystem));
        Assert.True(pcs.EqualParams(pcs2));

        CheckInfo(pcs, "OSGB 1936 / British National Grid", "EPSG", 27700);

        var gcs = pcs.GeographicCoordinateSystem;
        CheckInfo(gcs, "OSGB 1936", "EPSG", 4277);
        CheckDatum(gcs.HorizontalDatum, "OSGB_1936", "EPSG", 6277);
        CheckEllipsoid(gcs.HorizontalDatum.Ellipsoid, "Airy 1830", 6377563.396, 299.3249646, "EPSG", 7001);
        this.CheckPrimem(gcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901);
        CheckUnit(gcs.AngularUnit, "degree", 0.0174532925199433, "EPSG", 9122);

        Assert.Equal("Transverse_Mercator", pcs.Projection.ClassName);
        CheckProjection(pcs.Projection, "Transverse_Mercator", new[]
        {
            Tuple.Create("latitude_of_origin", 49d),
            Tuple.Create("central_meridian", -2d),
            Tuple.Create("scale_factor", 0.9996012717),
            Tuple.Create("false_easting", 400000d),
            Tuple.Create("false_northing", -100000d),
        });

        CheckUnit(pcs.LinearUnit, "metre", 1d, "EPSG", 9001);

        string newWkt = pcs.WKT.Replace(", ", ",", StringComparison.Ordinal);
        Assert.Equal(wkt, newWkt);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestParseSrOrg()
    {
        Assert.Null(Record.Exception(() => this.coordinateSystemFactory.CreateFromWkt(
            "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"Popular Visualisation CRS\"," +
            "DATUM[\"Popular_Visualisation_Datum\",SPHEROID[\"Popular Visualisation Sphere\"," +
            "6378137,0,AUTHORITY[\"EPSG\",\"7059\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\"," +
            "\"6055\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\"," +
            "0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4055\"]]," +
            "PROJECTION[\"Mercator_1SP\"]," +
            "PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[" +
            "\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]],AUTHORITY[\"EPSG\",\"3785\"]")));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestProjNetIssues()
    {
        Assert.Null(Record.Exception(() => this.coordinateSystemFactory.CreateFromWkt(
            "PROJCS[\"International_Terrestrial_Reference_Frame_1992Lambert_Conformal_Conic_2SP\"," +
            "GEOGCS[\"GCS_International_Terrestrial_Reference_Frame_1992\"," +
            "DATUM[\"International_Terrestrial_Reference_Frame_1992\"," +
            "SPHEROID[\"GRS_1980\",6378137,298.257222101]," +
            "TOWGS84[0,0,0,0,0,0,0]]," +
            "PRIMEM[\"Greenwich\",0]," +
            "UNIT[\"Degree\",0.0174532925199433]]," +
            "PROJECTION[\"Lambert_Conformal_Conic_2SP\",AUTHORITY[\"EPSG\",\"9802\"]]," +
            "PARAMETER[\"Central_Meridian\",-102]," +
            "PARAMETER[\"Latitude_Of_Origin\",12]," +
            "PARAMETER[\"False_Easting\",2500000]," +
            "PARAMETER[\"False_Northing\",0]," +
            "PARAMETER[\"Standard_Parallel_1\",17.5]," +
            "PARAMETER[\"Standard_Parallel_2\",29.5]," +
            "PARAMETER[\"Scale_Factor\",1]," +
            "UNIT[\"Meter\",1,AUTHORITY[\"EPSG\",\"9001\"]]]")));

        Assert.Null(Record.Exception(() => this.coordinateSystemFactory.CreateFromWkt(
            "PROJCS[\"Google Maps Global Mercator\"," +
            "GEOGCS[\"WGS 84\"," +
            "DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]]," +
            "AUTHORITY[\"EPSG\",\"6326\"]]," +
            "PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]]," +
            "UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]]," +
            "AUTHORITY[\"EPSG\",\"4326\"]]," +
            "PROJECTION[\"Mercator_2SP\"]," +
            "PARAMETER[\"standard_parallel_1\",0]," +
            "PARAMETER[\"latitude_of_origin\",0]," +
            "PARAMETER[\"central_meridian\",0]," +
            "PARAMETER[\"false_easting\",0]," +
            "PARAMETER[\"false_northing\",0]," +
            "UNIT[\"Meter\",1]," +
            "EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs\"]," +
            "AUTHORITY[\"EPSG\",\"900913\"]]")));
    }

    /// <summary>
    /// Test parsing of a <see cref="FittedCoordinateSystem"/> from WKT.
    /// </summary>
    [Xunit.Fact]
    public void TestFittedCoordinateSystemWkt()
    {
        var fac = new CoordinateSystemFactory();
        FittedCoordinateSystem fcs = null;
        string wkt = "FITTED_CS[\"Local coordinate system MNAU (based on Gauss-Krueger)\"," +
                            "PARAM_MT[\"Affine\"," +
                               "PARAMETER[\"num_row\",3],PARAMETER[\"num_col\",3],PARAMETER[\"elt_0_0\", 0.883485346527455],PARAMETER[\"elt_0_1\", -0.468458794848877],PARAMETER[\"elt_0_2\", 3455869.17937689],PARAMETER[\"elt_1_0\", 0.468458794848877],PARAMETER[\"elt_1_1\", 0.883485346527455],PARAMETER[\"elt_1_2\", 5478710.88035753],PARAMETER[\"elt_2_2\", 1]]," +
                            "PROJCS[\"DHDN / Gauss-Kruger zone 3\"," +
                               "GEOGCS[\"DHDN\"," +
                                  "DATUM[\"Deutsches_Hauptdreiecksnetz\"," +
                                     "SPHEROID[\"Bessel 1841\", 6377397.155, 299.1528128, AUTHORITY[\"EPSG\", \"7004\"]]," +
                                     "TOWGS84[612.4, 77, 440.2, -0.054, 0.057, -2.797, 0.525975255930096]," +
                                     "AUTHORITY[\"EPSG\", \"6314\"]]," +
                                   "PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]]," +
                                   "UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]]," +
                                   "AUTHORITY[\"EPSG\", \"4314\"]]," +
                               "PROJECTION[\"Transverse_Mercator\"]," +
                               "PARAMETER[\"latitude_of_origin\", 0]," +
                               "PARAMETER[\"central_meridian\", 9]," +
                               "PARAMETER[\"scale_factor\", 1]," +
                               "PARAMETER[\"false_easting\", 3500000]," +
                               "PARAMETER[\"false_northing\", 0]," +
                               "UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]]," +
                               "AUTHORITY[\"EPSG\", \"31467\"]]" +
                    "]";

        try
        {
            fcs = fac.CreateFromWkt(wkt) as FittedCoordinateSystem;
        }
        catch (Exception ex)
        {
            Assert.Fail("Could not create fitted coordinate system from:\r\n" + wkt + "\r\n" + ex.Message);
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
    /// Test parsing of a <see cref="GeocentricCoordinateSystem"/> from WKT.
    /// </summary>
    [Xunit.Fact]
    public void TestGeocentricCoordinateSystem()
    {
        var fac = new CoordinateSystemFactory();
        GeocentricCoordinateSystem fcs = null;

        const string wkt = "GEOCCS[\"TUREF\", " +
                        "DATUM[\"Turkish_National_Reference_Frame\", " +
                                "SPHEROID[\"GRS 1980\", 6378137, 298.257222101, AUTHORITY[\"EPSG\", \"7019\"]], " +
                                "AUTHORITY[\"EPSG\", \"1057\"]], " +
                        "PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], " +
                        "UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], " +
                        "AXIS[\"Geocentric X\", OTHER], AXIS[\"Geocentric Y\", OTHER], AXIS[\"Geocentric Z\", NORTH], " +
                        "AUTHORITY[\"EPSG\", \"5250\"]]";

        try
        {
            fcs = fac.CreateFromWkt(wkt) as GeocentricCoordinateSystem;
        }
        catch (Exception ex)
        {
            Assert.Fail("Could not create geocentric coordinate system from:\r\n" + wkt + "\r\n" + ex.Message);
        }

        Assert.NotNull(fcs);
        Assert.True(CheckInfo(fcs, "TUREF", "EPSG", 5250L));
        Assert.True(CheckDatum(fcs.HorizontalDatum, "Turkish_National_Reference_Frame", "EPSG", 1057L));
        Assert.True(CheckEllipsoid(fcs.HorizontalDatum.Ellipsoid, "GRS 1980", 6378137, 298.257222101, "EPSG", 7019));
        Assert.True(this.CheckPrimem(fcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901L));
        Assert.True(CheckUnit(fcs.PrimeMeridian.AngularUnit, "degree", null, null, null));
        Assert.True(CheckUnit(fcs.LinearUnit, "metre", 1, "EPSG", 9001L));

        Assert.Equal("EPSG", fcs.Authority);
        Assert.Equal(5250L, fcs.AuthorityCode);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void ParseWktCreatedByCoordinateSystem()
    {
        // Sample WKT from an external source.
        string sampleWKT =
            "PROJCS[\"\", " +
              "GEOGCS[\"\", " +
                "DATUM[\"\", " +
                  "SPHEROID[\"GRS_1980\", 6378137, 298.2572221010042] " +
                "], " +
              "PRIMEM[\"Greenwich\", 0], " +
              "UNIT[\"Degree\", 0.017453292519943295]" +
              "], " +
              "PROJECTION[\"Transverse_Mercator\"], " +
              "PARAMETER[\"False_Easting\", 500000], " +
              "PARAMETER[\"False_Northing\", 0], " +
              "PARAMETER[\"Central_Meridian\", -75], " +
              "PARAMETER[\"Scale_Factor\", 0.9996], " +
              "UNIT[\"Meter\", 1]" +
              "]";

        var csFromSample = (CoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(sampleWKT);
        string wktFromProjNetCS = csFromSample.WKT;
        var parsed = ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wktFromProjNetCS);
        Assert.IsType<ProjectedCoordinateSystem>(parsed);
        var projCS = (ProjectedCoordinateSystem)parsed;
        Assert.NotNull(projCS.LinearUnit);
        Assert.Equal("Meter", projCS.LinearUnit.Name);
        Assert.Equal(1, projCS.LinearUnit.MetersPerUnit);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void ParseProjectedCrsWithWkt2LikeRootAndIdentifiers()
    {
        const string wkt =
            "PROJECTEDCRS[\"WGS 84 / Pseudo-Mercator\",GEODCRS[\"WGS 84\",DATUM[\"WGS_1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,ID[\"EPSG\",\"7030\"]],ID[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,ID[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,ID[\"EPSG\",\"9122\"]],ID[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,ID[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],ID[\"EPSG\",\"3857\"]]";

        var parsed = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        Assert.NotNull(parsed);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(3857L, parsed.AuthorityCode);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void ParseGeodCrsWithEllipsoidAndIdTokens()
    {
        const string wkt =
            "GEODCRS[\"WGS 84\",DATUM[\"WGS_1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,ID[\"EPSG\",\"7030\"]],ID[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,ID[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,ID[\"EPSG\",\"9122\"]],ID[\"EPSG\",\"4326\"]]";

        var parsed = (GeographicCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        Assert.NotNull(parsed);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4326L, parsed.AuthorityCode);
        Assert.NotNull(parsed.HorizontalDatum);
        Assert.NotNull(parsed.HorizontalDatum.Ellipsoid);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void ParseProjectedCrsWithSpacedIdTokens()
    {
        const string wkt =
            "PROJECTEDCRS[\"WGS 84 / Pseudo-Mercator\",GEODETICCRS[\"WGS 84\",DATUM[\"WGS_1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,ID [\"EPSG\",\"7030\"]],ID [\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,ID [\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,ID [\"EPSG\",\"9122\"]],ID [\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,ID [\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],ID [\"EPSG\",\"3857\"]]";

        var parsed = (ProjectedCoordinateSystem)ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(wkt);
        Assert.NotNull(parsed);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(3857L, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies that span-based WKT parsing returns the same coordinate system metadata as string parsing.
    /// </summary>
    [Xunit.Fact]
    public void ParseReadOnlySpanWktMatchesStringParse()
    {
        const string wkt =
            "PROJECTEDCRS[\"WGS 84 / Pseudo-Mercator\",GEODCRS[\"WGS 84\",DATUM[\"WGS_1984\",ELLIPSOID[\"WGS 84\",6378137,298.257223563,ID[\"EPSG\",\"7030\"]],ID[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,ID[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,ID[\"EPSG\",\"9122\"]],ID[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,ID[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],ID[\"EPSG\",\"3857\"]]";

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
    [Xunit.Fact]
    public void ParseReadOnlySpanWhitespaceThrowsArgumentNullException()
    {
        const string whitespaceWkt = "   \t\r\n";

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => ProjNet.IO.CoordinateSystems.CoordinateSystemWktReader.Parse(whitespaceWkt.AsSpan()));

        Assert.Equal("wkt", exception.ParamName);
    }

    private bool CheckPrimem(PrimeMeridian primeMeridian, string name, double? longitude, string authority, long? code)
    {
        Assert.NotNull(primeMeridian);
        Assert.True(CheckInfo(primeMeridian, name, authority, code));
        Assert.Equal(longitude, primeMeridian.Longitude);
        return true;
    }

    private static bool CheckUnit(IUnit unit, string name, double? value, string authority, long? code)
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

    private static bool CheckInfo(IInfo info, string name, string authority = null, long? code = null)
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

    private static void CheckProjection(IProjection projection, string name, Tuple<string, double>[] pp = null, string authority = null, long? code = null)
    {
        Assert.NotNull(projection);
        Assert.Equal(name, projection.ClassName);
        CheckInfo(projection, name, authority, code);

        if (pp is null)
        {
            return;
        }

        Assert.Equal(pp.Length, projection.NumParameters);

        for (int i = 0; i < pp.Length; i++)
        {
            ProjectionParameter par = null;
            Assert.Null(Record.Exception(() => par = projection.GetParameter(pp[i].Item1)));
            Assert.NotNull(par);
            Assert.Equal(pp[i].Item1, par.Name);
            Assert.Equal(pp[i].Item2, par.Value);
        }
    }
}
