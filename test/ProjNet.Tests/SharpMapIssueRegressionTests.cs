// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNET.Tests;

using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class SharpMapIssueRegressionTests : CoordinateTransformTestsBase
{
    private string wkt7151 = "PROJCS[\"NAD_1983_Hotine_Oblique_Mercator_Azimuth_Natural_Origin\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"longitude_of_center\",-86.0],PARAMETER[\"latitude_of_center\",45.30916666666666],PARAMETER[\"azimuth\",337.25555999999995],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",2546731.496],PARAMETER[\"false_northing\",-4354009.816],PARAMETER[\"rectified_grid_angle\",337.25555999999995],UNIT[\"m\",1.0]]";
    private string wkt2236 = "PROJCS[\"NAD83 / Florida East (ftUS)\", GEOGCS [ \"NAD83\", DATUM [\"North American Datum 1983 (EPSG ID 6269)\", SPHEROID [\"GRS 1980 (EPSG ID 7019)\", 6378137, 298.257222101]], PRIMEM [ \"Greenwich\", 0.000000 ], UNIT [\"Decimal Degree\", 0.01745329251994328]], PROJECTION [\"SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)\"], PARAMETER [\"Latitude_Of_Origin\", 24.33333333333333333333333333333333333333], PARAMETER [\"Central_Meridian\", -80.9999999999999999999999999999999999999], PARAMETER [\"Scale_Factor\", 0.999941177], PARAMETER [\"False_Easting\", 656166.6669999999999999999999999999999999], PARAMETER [\"False_Northing\", 0], UNIT [\"U.S. Foot\", 0.3048006096012192024384048768097536195072]]";
    private string wkt8307 = "GEOGCS [ \"WGS 84\", DATUM [\"World Geodetic System 1984 (EPSG ID 6326)\", SPHEROID [\"WGS 84 (EPSG ID 7030)\", 6378137, 298.257223563]], PRIMEM [ \"Greenwich\", 0.000000 ], UNIT [\"Decimal Degree\", 0.01745329251994328]]";

    /// <summary>
    /// Initializes a new instance of the <see cref="SharpMapIssueRegressionTests"/> class.
    /// </summary>
    public SharpMapIssueRegressionTests()
    {
        this.Verbose = true;
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "NAD83 (State Plane) projection to the WGS84 (Lat/Long), http://sharpmap.codeplex.com/discussions/435794")]
    public void TestNad83ToWGS84()
    {
        var src = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, this.wkt2236);
        var tgt = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, this.wkt8307);

        ProjNet.CoordinateSystems.Projections.ProjectionsRegistry.Register(
            "SPCS83 Florida East zone (US Survey feet) (EPSG OP 15318)",
            this.ReflectType("ProjNet.CoordinateSystems.Projections.TransverseMercator"));

        ICoordinateTransformation transform = default!;
        Assert.Null(Record.Exception(() => transform = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(src, tgt)));
        Assert.NotNull(transform);
    }

    // projection problem with Michigan GeoRef

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "projection problem with Michigan GeoRef")]
    public void TestMichiganGeoRefToWebMercator()
    {
        var src = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, this.wkt7151);
        var tgt = ProjNet.CoordinateSystems.ProjectedCoordinateSystem.WebMercator;

        ICoordinateTransformation transform = default!;
        Assert.Null(Record.Exception(() => transform = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(src, tgt)));
        Assert.NotNull(transform);
        double[] ptSrc = new[] { 535247.9375, 324548.09375 };
        double[] ptTgt = default!;
        Assert.Null(Record.Exception(() => ptTgt = transform.MathTransform.Transform(ptSrc)));
        Assert.NotNull(ptTgt);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact(DisplayName = "Parse AUTHORITY with unqouted AuthorityCode")]
    public void TestAuthorityCodeParsing()
    {
        const string wkt1 = "PROJCS[\"NAD_1983_BC_Environment_Albers\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Albers\"],PARAMETER[\"False_Easting\",1000000.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",-126.0],PARAMETER[\"Standard_Parallel_1\",50.0],PARAMETER[\"Standard_Parallel_2\",58.5],PARAMETER[\"Latitude_Of_Origin\",45.0],UNIT[\"Meter\",1.0],AUTHORITY[\"EPSG\",\"3005\"]]";
        CoordinateSystem cs1 = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, wkt1), cs2 = null!;
        const string wkt2 = "PROJCS[\"NAD_1983_BC_Environment_Albers\",GEOGCS[\"GCS_North_American_1983\",DATUM[\"D_North_American_1983\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Albers\"],PARAMETER[\"False_Easting\",1000000.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",-126.0],PARAMETER[\"Standard_Parallel_1\",50.0],PARAMETER[\"Standard_Parallel_2\",58.5],PARAMETER[\"Latitude_Of_Origin\",45.0],UNIT[\"Meter\",1.0],AUTHORITY[\"EPSG\",3005]]";
        cs2 = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, wkt2);

        // Assert.Equal(cs1, cs2);
        Assert.True(cs1.EqualParams(cs2));
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void Test25832To3857()
    {
        const string wkt1 = // "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"25832\"]]";
            "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"25832\"]]";

        CoordinateSystem cs1 = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, wkt1), cs2 = null!;
        const string wkt2 = "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",                  SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]],PROJECTION[\"Mercator_1SP\"],PARAMETER[\"latitude_of_origin\", 0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs\"],AUTHORITY[\"EPSG\",\"3857\"]]";
        cs2 = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, wkt2);

        ICoordinateTransformation ct = default!;
        Assert.Null(Record.Exception(() => ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(cs1, cs2)));
        Assert.NotNull(ct);
        Assert.Null(Record.Exception(() => ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(cs2, cs1)));
        Assert.NotNull(ct);
        Assert.Null(Record.Exception(() => ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(cs1, ProjectedCoordinateSystem.WebMercator)));
        Assert.NotNull(ct);
        Assert.Null(Record.Exception(() => ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(ProjectedCoordinateSystem.WebMercator, cs1)));
        Assert.NotNull(ct);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    [Xunit.Fact]
    public void TestLaea()
    {
        const string Epsg3035 =
            @"PROJCS[""ETRS89 / ETRS-LAEA"",GEOGCS[""ETRS89"",DATUM[""European_Terrestrial_Reference_System_1989"",SPHEROID[""GRS 1980"",6378137,298.257222101,AUTHORITY[""EPSG"",""7019""]],AUTHORITY[""EPSG"",""6258""]],PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],UNIT[""degree"",0.01745329251994328,AUTHORITY[""EPSG"",""9122""]],AUTHORITY[""EPSG"",""4258""]],PROJECTION[""Lambert_Azimuthal_Equal_Area""],PARAMETER[""latitude_of_center"",52],PARAMETER[""longitude_of_center"",10],PARAMETER[""false_easting"",4321000],PARAMETER[""false_northing"",3210000],UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],AXIS[""X"",EAST],AXIS[""Y"",NORTH],AUTHORITY[""EPSG"",""3035""]]";

        var csSrc = GeographicCoordinateSystem.WGS84;
        var csTgt = CoordinateSystemTestHelpers.RequireCoordinateSystem(this.CoordinateSystemFactory, Epsg3035);

        var ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csSrc, csTgt);

        (double resX, double resY) = ((MathTransform)ct.MathTransform).Transform(16.4, 48.2);
        Assert.InRange(resX, 4796297.431434812 - 1e-2, 4796297.431434812 + 1e-2);
        Assert.InRange(resY, 2807999.1539475969 - 1e-2, 2807999.1539475969 + 1e-2);

        (double origX, double origY) = ((MathTransform)ct.MathTransform.Inverse()).Transform(resX, resY);
        Assert.InRange(origX, 16.4 - 1e-2, 16.4 + 1e-2);
        Assert.InRange(origY, 48.2 - 1e-2, 48.2 + 1e-2);
    }

    private System.Type ReflectType(string typeName)
    {
        var asm = Assembly.GetAssembly(typeof(ProjNet.CoordinateSystems.Projections.MapProjection));
        var res = asm.GetType(typeName);
        return Assert.IsAssignableFrom<System.Type>(res);
    }
}

