// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Tests.IO.CoordinateSystems;
using Xunit;

/// <summary>
/// Regression tests for issues reported in the SharpMap project.
/// </summary>
public class SharpMapIssueRegressionTests : CoordinateTransformTestsBase
{
    private string wkt7151 =
        """
        PROJCS["NAD_1983_Hotine_Oblique_Mercator_Azimuth_Natural_Origin",GEOGCS["GCS_North_American_1983",DATUM["D_North_American_1983",SPHEROID["GRS_1980",6378137.0,298.257222101]],PRIMEM["Greenwich",0.0],UNIT["Degree",0.017453292519943295]],PROJECTION["Hotine_Oblique_Mercator"],PARAMETER["longitude_of_center",-86.0],PARAMETER["latitude_of_center",45.30916666666666],PARAMETER["azimuth",337.25555999999995],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",2546731.496],PARAMETER["false_northing",-4354009.816],PARAMETER["rectified_grid_angle",337.25555999999995],UNIT["m",1.0]]
        """;

    private string wkt2236 =
        """
        PROJCS["NAD83 / Florida East (ftUS)", GEOGCS [ "NAD83", DATUM ["North American Datum 1983 (EPSG ID 6269)", SPHEROID ["GRS 1980 (EPSG ID 7019)", 6378137, 298.257222101]], PRIMEM [ "Greenwich", 0.000000 ], UNIT ["Decimal Degree", 0.01745329251994328]], PROJECTION ["SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)"], PARAMETER ["Latitude_Of_Origin", 24.33333333333333333333333333333333333333], PARAMETER ["Central_Meridian", -80.9999999999999999999999999999999999999], PARAMETER ["Scale_Factor", 0.999941177], PARAMETER ["False_Easting", 656166.6669999999999999999999999999999999], PARAMETER ["False_Northing", 0], UNIT ["U.S. Foot", 0.3048006096012192024384048768097536195072]]
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharpMapIssueRegressionTests"/> class.
    /// </summary>
    public SharpMapIssueRegressionTests()
    {
        this.Verbose = true;
    }

    /// <summary>
    /// Verifies that a NAD83 State Plane (Florida East, US survey feet) to WGS84 coordinate
    /// transformation can be created without error.
    /// </summary>
    [Fact(DisplayName = "NAD83 (State Plane) projection to the WGS84 (Lat/Long), http://sharpmap.codeplex.com/discussions/435794")]
    public void TestNad83ToWGS84()
    {
        CoordinateSystem src = this.RequireCoordinateSystem(this.wkt2236);
        CoordinateSystem tgt = this.RequireCoordinateSystem(this.GetEpsgWkt(4326));

        ProjNet.CoordinateSystems.Projections.ProjectionsRegistry.Register(
            "SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)",
            this.ReflectType("ProjNet.CoordinateSystems.Projections.TransverseMercator"));

        _ = this.AssertTransformationCreated(src, tgt);
    }

    // projection problem with Michigan GeoRef

    /// <summary>
    /// Verifies that a Michigan GeoRef (Hotine Oblique Mercator) to Web Mercator transformation
    /// can be created and applied to a coordinate without error.
    /// </summary>
    [Fact(DisplayName = "projection problem with Michigan GeoRef")]
    public void TestMichiganGeoRefToWebMercator()
    {
        CoordinateSystem src = this.RequireCoordinateSystem(this.wkt7151);
        ProjectedCoordinateSystem tgt = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;

        ICoordinateTransformation transform = this.AssertTransformationCreated(src, tgt);
        double[] ptSrc = [535247.9375, 324548.09375];
        double[] ptTgt = default!;
        Assert.Null(Record.Exception(() => ptTgt = transform.MathTransform.Transform(ptSrc)));
        Assert.NotNull(ptTgt);
    }

    /// <summary>
    /// Verifies that the WKT parser accepts an authority code written as either a quoted string
    /// or an unquoted integer and produces equivalent coordinate systems in both cases.
    /// </summary>
    [Fact(DisplayName = "Parse AUTHORITY with unqouted AuthorityCode")]
    public void TestAuthorityCodeParsing()
    {
        const string wkt1 =
            """
            PROJCS["NAD_1983_BC_Environment_Albers",GEOGCS["GCS_North_American_1983",DATUM["D_North_American_1983",SPHEROID["GRS_1980",6378137.0,298.257222101]],PRIMEM["Greenwich",0.0],UNIT["Degree",0.0174532925199433]],PROJECTION["Albers"],PARAMETER["False_Easting",1000000.0],PARAMETER["False_Northing",0.0],PARAMETER["Central_Meridian",-126.0],PARAMETER["Standard_Parallel_1",50.0],PARAMETER["Standard_Parallel_2",58.5],PARAMETER["Latitude_Of_Origin",45.0],UNIT["Meter",1.0],AUTHORITY["EPSG","3005"]]
            """;
        CoordinateSystem cs1 = this.RequireCoordinateSystem(wkt1);
        const string wkt2 =
            """
            PROJCS["NAD_1983_BC_Environment_Albers",GEOGCS["GCS_North_American_1983",DATUM["D_North_American_1983",SPHEROID["GRS_1980",6378137.0,298.257222101]],PRIMEM["Greenwich",0.0],UNIT["Degree",0.0174532925199433]],PROJECTION["Albers"],PARAMETER["False_Easting",1000000.0],PARAMETER["False_Northing",0.0],PARAMETER["Central_Meridian",-126.0],PARAMETER["Standard_Parallel_1",50.0],PARAMETER["Standard_Parallel_2",58.5],PARAMETER["Latitude_Of_Origin",45.0],UNIT["Meter",1.0],AUTHORITY["EPSG",3005]]
            """;
        CoordinateSystem cs2 = this.RequireCoordinateSystem(wkt2);

        // Assert.Equal(cs1, cs2);
        Assert.True(cs1.EqualParams(cs2));
    }

    /// <summary>
    /// Verifies that coordinate transformations between EPSG 25832 (UTM zone 32N) and
    /// EPSG 3857 (Web Mercator) can be created successfully in both directions.
    /// </summary>
    [Fact]
    public void Test25832To3857()
    {
        CoordinateSystem cs1 = this.RequireCoordinateSystem(this.GetEpsgWkt(25832));
        CoordinateSystem cs2 = this.RequireCoordinateSystem(this.GetEpsgWkt(3857));

        _ = this.AssertTransformationCreated(cs1, cs2);
        _ = this.AssertTransformationCreated(cs2, cs1);
        _ = this.AssertTransformationCreated(cs1, ProjectedCoordinateSystem.WebMercator);
        _ = this.AssertTransformationCreated(ProjectedCoordinateSystem.WebMercator, cs1);
    }

    /// <summary>
    /// Verifies that a Lambert Azimuthal Equal Area (EPSG 3035) transformation produces
    /// accurate forward and inverse results.
    /// </summary>
    [Fact]
    public void TestLaea()
    {
        GeographicCoordinateSystem csSrc = GeographicCoordinateSystem.WGS84;
        CoordinateSystem csTgt = this.RequireCoordinateSystem(this.GetEpsgWkt(3035));

        ICoordinateTransformation ct = this.CreateTransformation(csSrc, csTgt);

        (double resX, double resY) = ct.MathTransform.Transform(16.4, 48.2);
        Assert.InRange(resX, 4796297.431434812 - 1e-2, 4796297.431434812 + 1e-2);
        Assert.InRange(resY, 2807999.1539475969 - 1e-2, 2807999.1539475969 + 1e-2);

        (double origX, double origY) = ct.MathTransform.Inverse().Transform(resX, resY);
        Assert.InRange(origX, 16.4 - 1e-2, 16.4 + 1e-2);
        Assert.InRange(origY, 48.2 - 1e-2, 48.2 + 1e-2);
    }

    private Type ReflectType(string typeName)
    {
        Assembly asm = Assert.IsType<Assembly>(Assembly.GetAssembly(typeof(ProjNet.CoordinateSystems.Projections.MapProjection)), exactMatch: false);
        Type? res = asm.GetType(typeName);
        return Assert.IsType<Type>(res, exactMatch: false);
    }

    private string GetEpsgWkt(int srid)
        => EpsgArchiveWktFixtureSource.GetFixture(srid).Wkt;
}
