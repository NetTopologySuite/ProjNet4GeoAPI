// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;
using static ProjNet.Tests.CoordinateSystemTestHelpers;

/// <summary>
/// Tests for coordinate system transformations across various projection types and datum shifts.
/// </summary>
public class CoordinateTransformTests : CoordinateTransformTestsBase
{
    private static readonly double[] AffineTargetPoint = [3456926.640, 5481071.278];
    private static readonly double[] AffineTestPoint = [2040.0, 1590.0];
    private static readonly double[] CassiniSoldnerExpected = [25244.540, 21300.969];
    private static readonly double[] CassiniSoldnerInput = [13.408055555556, 52.518611111111];
    private static readonly double[] TransformListSamplePoint1 = [290586.087, 6714000];
    private static readonly double[] TransformListSamplePoint2 = [290586.392, 6713996.224];
    private static readonly double[] TransformListSamplePoint3 = [290590.133, 6713973.772];
    private static readonly double[] OrthographicHorizonTestPoint = [180.0, 0.0];

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateTransformTests"/> class.
    /// </summary>
    public CoordinateTransformTests()
    {
        this.Verbose = true;
    }

    /// <summary>
    /// Verifies that transforming an array of <see cref="XY"/> coordinates produces the same results as transforming each coordinate individually.
    /// </summary>
    [Fact]
    public void TestTransformListOfCoordinates()
    {
        var csFact = new CoordinateSystemFactory();
        var ctFact = new CoordinateTransformationFactory();

        CoordinateSystem utm35ETRS = RequireCoordinateSystem(
            csFact,
            "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation trans = ctFact.CreateFromCoordinateSystems(utm35ETRS, utm33);

        XY[] points =
        [
            new XY(290586.087, 6714000), new XY(290586.392, 6713996.224),
            new XY(290590.133, 6713973.772), new XY(290594.111, 6713957.416),
            new XY(290596.615, 6713943.567), new XY(290596.701, 6713939.485),
        ];

        var tpoints = (XY[])points.Clone();
        trans.MathTransform.Transform(tpoints);
        for (int i = 0; i < points.Length; i++)
        {
            double expectedX = points[i].X;
            double expectedY = points[i].Y;
            trans.MathTransform.Transform(ref expectedX, ref expectedY);

            double actualX = tpoints[i].X;
            double actualY = tpoints[i].Y;

            Assert.Equal(expectedX, actualX, 8);
            Assert.Equal(expectedY, actualY, 8);
        }
    }

    /// <summary>
    /// Verifies that <c>TransformList</c> for double-array inputs produces the same results as individual point transforms.
    /// </summary>
    [Fact]
    public void TestTransformListOfDoubleArray()
    {
        var csFact = new CoordinateSystemFactory();
        var ctFact = new CoordinateTransformationFactory();

        CoordinateSystem utm35ETRS = RequireCoordinateSystem(
            csFact,
            "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation trans = ctFact.CreateFromCoordinateSystems(utm35ETRS, utm33);

        double[][] points =
        [
            [290586.087, 6714000], [90586.392, 6713996.224],
            [290590.133, 6713973.772], [290594.111, 6713957.416],
            [290596.615, 6713943.567], [290596.701, 6713939.485],
        ];

        double[][] tpoints = [.. trans.MathTransform.TransformList(points)];
        for (int i = 0; i < points.Length; i++)
        {
            double expectedX = points[i][0];
            double expectedY = points[i][1];
            trans.MathTransform.Transform(ref expectedX, ref expectedY);

            double actualX = tpoints[i][0];
            double actualY = tpoints[i][1];

            Assert.Equal(expectedX, actualX, 8);
            Assert.Equal(expectedY, actualY, 8);
        }
    }

    /// <summary>
    /// Verifies that a Lambert Azimuthal Equal Area WKT containing a negative central meridian is parsed without error.
    /// </summary>
    [Fact]
    public void TestCentralMeridianParse()
    {
        const string strSouthPole = "PROJCS[\"South_Pole_Lambert_Azimuthal_Equal_Area\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Lambert_Azimuthal_Equal_Area\"],PARAMETER[\"False_Easting\",0],PARAMETER[\"False_Northing\",0],PARAMETER[\"Central_Meridian\",-127],PARAMETER[\"Latitude_Of_Origin\",-90],UNIT[\"Meter\",1]]";

        var pCoordSysFactory = new CoordinateSystemFactory();
        CoordinateSystem pSouthPole = RequireCoordinateSystem(pCoordSysFactory, strSouthPole);
        Assert.NotNull(pSouthPole);
    }

    /// <summary>
    /// Verifies forward and inverse Albers Conical Equal Area projection using the Clarke 1866 ellipsoid with metre output units.
    /// </summary>
    [Fact]
    public void TestAlbersProjection()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1866", 6378206.4, 294.9786982138982, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Clarke 1866", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Clarke 1866",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("central_meridian", -96),
                                 new("latitude_of_center", 23),
                                 new("standard_parallel_1", 29.5),
                                 new("standard_parallel_2", 45.5),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Albers Conical Equal Area", "albers", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Albers Conical Equal Area", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans1 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcs, coordsys);
        ICoordinateTransformation trans2 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(coordsys, gcs);

        double[] pGeo = [-75, 35];
        double[] pUtm = trans1.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans2.MathTransform.Transform(pUtm);

        double[] expected = [1885472.7, 1535925];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.05), this.TransformationError("Albers", expected, pUtm, false));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Albers", pGeo, pGeo2, true));
    }

    /// <summary>
    /// Verifies forward and inverse Albers Conical Equal Area projection using the Clarke 1866 ellipsoid with feet output units.
    /// </summary>
    [Fact]
    public void TestAlbersProjectionFeet()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1866", 6378206.4, 294.9786982138982, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Clarke 1866", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Clarke 1866",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("central_meridian", -96),
                                 new("latitude_of_center", 23),
                                 new("standard_parallel_1", 29.5),
                                 new("standard_parallel_2", 45.5),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Albers Conical Equal Area", "albers", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Albers Conical Equal Area", gcs, projection, LinearUnit.Foot, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = [-75, 35];
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = [1885472.7 / LinearUnit.Foot.MetersPerUnit, 1535925 / LinearUnit.Foot.MetersPerUnit];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.1), this.TransformationError("Albers", expected, pUtm, false));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Albers", pGeo, pGeo2, true));
    }

    /// <summary>
    /// Verifies forward and inverse Mercator 1SP projection using the Bessel 1840 ellipsoid with metre output units.
    /// </summary>
    [Fact]
    public void TestMercator1SPProjection()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Bessel 1840", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Bessel 1840",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_origin", 0),
                                 new("central_meridian", 110),
                                 new("scale_factor", 0.997),
                                 new("false_easting", 3900000),
                                 new("false_northing", 900000),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Mercator_1SP", "Mercator_1SP", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Makassar / NEIEZ", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = [120, -3];
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = [5009726.58, 569150.82];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.02), this.TransformationError("Mercator_1SP", expected, pUtm, false));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Mercator_1SP", pGeo, pGeo2, true));
    }

    /// <summary>
    /// Verifies forward and inverse Mercator 1SP projection using the Bessel 1840 ellipsoid with feet output units.
    /// </summary>
    [Fact]
    public void TestMercator1SPProjectionFeet()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Bessel 1840", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Bessel 1840",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_origin", 0),
                                 new("central_meridian", 110),
                                 new("scale_factor", 0.997),
                                 new("false_easting", 3900000 / LinearUnit.Foot.MetersPerUnit),
                                 new("false_northing", 900000 / LinearUnit.Foot.MetersPerUnit),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Mercator_1SP", "Mercator_1SP", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Makassar / NEIEZ", gcs, projection, LinearUnit.Foot, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = [120d, -3d];
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = [5009726.58 / LinearUnit.Foot.MetersPerUnit, 569150.82 / LinearUnit.Foot.MetersPerUnit];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.02), this.TransformationError("Mercator_1SP", expected, pUtm, false));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Mercator_1SP", pGeo, pGeo2, true));
    }

    /// <summary>
    /// Verifies forward and inverse Mercator 2SP (Caspian Sea) projection using the Krassowski 1940 ellipsoid.
    /// </summary>
    [Fact]
    public void TestMercator2SPProjection()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Krassowski 1940", 6378245.0, 298.3, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Krassowski 1940", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Krassowski 1940",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_origin", 42),
                                 new("central_meridian", 51),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Mercator_2SP", "Mercator_2SP", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Pulkovo 1942 / Mercator Caspian Sea", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = [53d, 53d];
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = [165704.29, 5171848.07];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.02), this.TransformationError("Mercator_2SP", expected, pUtm, false));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Mercator_2SP", pGeo, pGeo2, true));
    }

    /// <summary>
    /// Verifies forward and inverse Transverse Mercator projection for the OSGB 1936 British National Grid.
    /// </summary>
    [Fact]
    public void TestTransverseMercatorProjection()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Airy 1830", 6377563.396, 299.32496, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Airy 1830", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Airy 1830",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_origin", 49),
                                 new("central_meridian", -2),
                                 new("scale_factor", 0.9996012717), // 0.9996
                                 new("false_easting", 400000),
                                 new("false_northing", -100000),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Transverse Mercator", "Transverse_Mercator", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("OSGB 1936 / British National Grid", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = [0.5, 50.5];
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        // "POINT(577393.372775651 69673.621953601)"
        double[] expected = [577274.98, 69740.49];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.01), this.TransformationError("TransverseMercator", expected, pUtm));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 1E-6), this.TransformationError("TransverseMercator", pGeo, pGeo2, true));
    }

    /// <summary>
    /// Verifies forward and inverse Lambert Conic Conformal 2SP projection for the NAD27 / Texas South Central system.
    /// </summary>
    [Fact]
    public void TestLambertConicConformal2SPProjection()
    {
        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1866", 20925832.16, 294.97470, LinearUnit.USSurveyFoot);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Clarke 1866", DatumType.HD_Geocentric, ellipsoid, null);
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Clarke 1866",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_origin", 27.833333333),
                                 new("central_meridian", -99),
                                 new("standard_parallel_1", 28.3833333333),
                                 new("standard_parallel_2", 30.2833333333),
                                 new("false_easting", 2000000 / LinearUnit.USSurveyFoot.MetersPerUnit),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Lambert Conic Conformal (2SP)", "lambert_conformal_conic_2sp", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("NAD27 / Texas South Central", gcs, projection, LinearUnit.USSurveyFoot, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = [-96, 28.5];
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = [2963503.91 / LinearUnit.USSurveyFoot.MetersPerUnit, 254759.80 / LinearUnit.USSurveyFoot.MetersPerUnit];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.05), this.TransformationError("LambertConicConformal2SP", expected, pUtm));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("LambertConicConformal2SP", pGeo, pGeo2, true));
    }

    private ICoordinateTransformation CreateGeo2Laea(double centralMeridian, double latitudeOfOrigin)
    {
        GeographicCoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;
        string laeaWkt =
            "PROJCS[\"Lambert_Azimuthal_Equal_Area_Custom\"," +
            "GEOGCS[\"GCS_WGS_1984\"," +
            "DATUM[\"D_WGS_1984\"," +
            "SPHEROID[\"WGS_1984\",6378137.0,298.257223563]]," +
            "PRIMEM[\"Greenwich\",0.0]," +
            "UNIT[\"Degree\",0.0174532925199433]]," +
            "PROJECTION[\"Lambert_Azimuthal_Equal_Area\"]," +
            "PARAMETER[\"False_Easting\",0.0]," +
            "PARAMETER[\"False_Northing\",0.0]," +
            $"PARAMETER[\"Central_Meridian\",{centralMeridian}]," +
            $"PARAMETER[\"Latitude_Of_Origin\",{latitudeOfOrigin}]," +
            "UNIT[\"Meter\",1.0]]";

        CoordinateSystem coordsys = RequireCoordinateSystem(this.CoordinateSystemFactory, laeaWkt);

        return this.CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84, coordsys);
    }

    /// <summary>
    /// Verifies that forward and inverse Lambert Azimuthal Equal Area transforms round-trip to the projection origin across 1000 random projection centers.
    /// </summary>
    [Fact]
    public void TestLambertAzimuthalEqualAreaProjectionRoundTripOnOrigin()
    {
        for (int i = 0; i < 1000; i++)
        {
            double centralMeridian = this.Random.Next(-180, +180);
            double latitudeOfOrigin = this.Random.Next(-90, +90);

            ICoordinateTransformation trans = this.CreateGeo2Laea(centralMeridian, latitudeOfOrigin);

            MathTransform forward = trans.MathTransform;
            MathTransform reverse = forward.Inverse();

            double[] pGeo = [centralMeridian, latitudeOfOrigin];

            double[] pLaea = forward.Transform(pGeo);

            double[] pGeo2 = reverse.Transform(pLaea);

            double[] expectedPLaea = [0, 0];

            Assert.True(this.ToleranceLessThan(pLaea, expectedPLaea, 0.05), this.TransformationError("Lambert_Azimuthal_Equal_Area", expectedPLaea, pLaea));
            Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Lambert_Azimuthal_Equal_Area", pGeo, pGeo2, true));
        }
    }

    /// <summary>
    /// Verifies that forward and inverse Lambert Azimuthal Equal Area transforms round-trip correctly for 1000 random off-origin points.
    /// </summary>
    [Fact]
    public void TestLambertAzimuthalEqualAreaProjectionRoundTripOnArbitraryPoint()
    {
        int GetRandomSign()
        {
            return this.Random.Next() % 2 == 0 ? -1 : +1;
        }

        for (int i = 0; i < 1000; i++)
        {
            double centralMeridian = this.Random.Next(-150, +150);
            double latitudeOfOrigin = this.Random.Next(-70, +70);

            ICoordinateTransformation trans = this.CreateGeo2Laea(centralMeridian, latitudeOfOrigin);

            MathTransform forward = trans.MathTransform;
            MathTransform reverse = forward.Inverse();

            double lat = latitudeOfOrigin + ((0.01 + this.Random.NextDouble()) * GetRandomSign());
            double lon = centralMeridian + ((0.01 + this.Random.NextDouble()) * GetRandomSign());

            double[] pGeo = [lon, lat];

            double[] pLaea = forward.Transform(pGeo);

            double[] pGeo2 = reverse.Transform(pLaea);

            Assert.NotEqual(0d, pLaea[0]);
            Assert.NotEqual(0d, pLaea[1]);
            Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Lambert_Azimuthal_Equal_Area", pGeo, pGeo2, true));
        }
    }

    /// <summary>
    /// Verifies the geographic-to-geocentric coordinate transformation and its inverse using the ETRF89 datum.
    /// </summary>
    [Fact]
    public void TestGeocentric()
    {
        GeographicCoordinateSystem gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ETRF89 Geographic",
            AngularUnit.Degrees,
            HorizontalDatum.ETRF89,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));
        GeocentricCoordinateSystem gcenCs = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("ETRF89 Geocentric", HorizontalDatum.ETRF89, LinearUnit.Metre, PrimeMeridian.Greenwich);
        ICoordinateTransformation ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, gcenCs);
        double[] pExpected = [2 + (7.0 / 60) + (46.38 / 3600), 53 + (48.0 / 60) + (33.82 / 3600)]; // Point.FromDMS(2, 7, 46.38, 53, 48, 33.82);
        double[] pExpected3D = [pExpected[0], pExpected[1], 73.0];
        double[] p0 = [3771793.97, 140253.34, 5124304.35];
        double[] p1 = ct.MathTransform.Transform(pExpected3D);
        double[] p2 = ct.MathTransform.Inverse().Transform(p1);
        Assert.True(this.ToleranceLessThan(p1, p0, 0.01));
        Assert.True(this.ToleranceLessThan(p2, pExpected, 0.00001));
    }

    /// <summary>
    /// Verifies datum shift transformations between WGS72, WGS84, and ED50 in both geocentric and projected (UTM) coordinate spaces.
    /// </summary>
    [Fact]
    public void TestDatumTransform()
    {
        // Define datums, set parameters
        HorizontalDatum wgs72 = HorizontalDatum.WGS72;
        HorizontalDatum ed50 = new(
            HorizontalDatum.ED50.Ellipsoid,
            new Wgs84ConversionInfo(
                -81.0703,
                -89.3603,
                -115.7526,
                -0.48488,
                -0.02436,
                -0.41321,
                -0.540645),
            HorizontalDatum.ED50.DatumType,
            HorizontalDatum.ED50.Name,
            HorizontalDatum.ED50.Authority,
            HorizontalDatum.ED50.AuthorityCode,
            HorizontalDatum.ED50.Alias,
            HorizontalDatum.ED50.Remarks,
            HorizontalDatum.ED50.Abbreviation);

        // Define geographic coordinate systems
        _ = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "WGS72 Geographic",
            AngularUnit.Degrees,
            wgs72,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        GeographicCoordinateSystem gcsWGS84 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "WGS84 Geographic",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        GeographicCoordinateSystem gcsED50 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ED50 Geographic",
            AngularUnit.Degrees,
            ed50,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        // Define geocentric coordinate systems
        GeocentricCoordinateSystem gcenCsWGS72 = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("WGS72 Geocentric", wgs72, LinearUnit.Metre, PrimeMeridian.Greenwich);
        GeocentricCoordinateSystem gcenCsWGS84 = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        _ = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("ED50 Geocentric", ed50, LinearUnit.Metre, PrimeMeridian.Greenwich);

        // Define projections
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_origin", 0),
                                 new("central_meridian", 9),
                                 new("scale_factor", 0.9996),
                                 new("false_easting", 500000),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Transverse Mercator", "Transverse_Mercator", parameters);
        ProjectedCoordinateSystem utmED50 = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("ED50 UTM Zone 32N", gcsED50, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));
        ProjectedCoordinateSystem utmWGS84 = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("WGS84 UTM Zone 32N", gcsWGS84, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        // Test datum-shift from WGS72 to WGS84
        double[] pGeoCenWGS72 = [3657660.66, 255768.55, 5201382.11];
        ICoordinateTransformation geocen_ed50_2_Wgs84 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcenCsWGS72, gcenCsWGS84);
        double[] pGeoCenWGS84 = geocen_ed50_2_Wgs84.MathTransform.Transform(pGeoCenWGS72);

        double[] pExpected = [3657660.78, 255778.43, 5201387.75];
        Assert.True(this.ToleranceLessThan(pExpected, pGeoCenWGS84, 0.01), this.TransformationError("Datum WGS72->WGS84", pExpected, pGeoCenWGS84));

        // and inverse
        double[] pGeoCenWGS72calc = geocen_ed50_2_Wgs84.MathTransform.Inverse().Transform(pGeoCenWGS84);
        Assert.True(this.ToleranceLessThan(pGeoCenWGS72, pGeoCenWGS72calc, 0.001), this.TransformationError("Datum WGS84->WGS72", pGeoCenWGS72, pGeoCenWGS72calc));

        ICoordinateTransformation utm_ed50_2_Wgs84 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utmED50, utmWGS84);
        double[] pUTMED50 = [600000, 6100000];
        double[] pUTMWGS84 = utm_ed50_2_Wgs84.MathTransform.Transform(pUTMED50);
        pExpected = [599928.6, 6099790.2];
        Assert.True(this.ToleranceLessThan(pExpected, pUTMWGS84, 0.1), this.TransformationError("Datum ED50->WGS84", pExpected, pUTMWGS84));

        // and inverse
        double[] pUTMED50calc = utm_ed50_2_Wgs84.MathTransform.Inverse().Transform(pUTMWGS84);
        Assert.True(this.ToleranceLessThan(pUTMED50, pUTMED50calc, 0.01), this.TransformationError("Datum WGS84->ED50", pUTMED50, pUTMED50calc));

        // Perform reverse
        ICoordinateTransformation utm_Wgs84_2_Ed50 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utmWGS84, utmED50);
        pUTMED50 = utm_Wgs84_2_Ed50.MathTransform.Transform(pUTMWGS84);
        pExpected = [600000, 6100000];
        Assert.True(this.ToleranceLessThan(pExpected, pUTMED50, 0.1), this.TransformationError("Datum", pExpected, pUTMED50));

        // and inverse
        double[] pUTMWGS84calc = utm_Wgs84_2_Ed50.MathTransform.Inverse().Transform(pUTMED50);
        Assert.True(this.ToleranceLessThan(pUTMWGS84, pUTMWGS84calc, 0.1), this.TransformationError("Datum", pUTMWGS84, pUTMWGS84calc));

        // Assert.True(Math.Abs((pUTMWGS84 as Point3D).Z - 36.35) < 0.5);
        // Point pExpected = Point.FromDMS(2, 7, 46.38, 53, 48, 33.82);
    }

    /// <summary>
    /// Verifies forward and inverse Krovak projection referenced to the Greenwich meridian (EPSG 5514 / 102067).
    /// </summary>
    [Fact]
    public void TestKrovakGreenwichProjection()
    {
        // test case for epsg 5514 (102067)
        GeographicCoordinateSystem gcsWGS84 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "WGS84 Geographic",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum(
            "Bessel 1840",
            DatumType.HD_Geocentric,
            ellipsoid,
            new Wgs84ConversionInfo(570.8, 85.7, 462.8, 4.998, 1.587, 5.261, 3.56));

        GeographicCoordinateSystem gcsKrovak = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Bessel 1840",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_center", 49.5),
                                 new("longitude_of_center", 24.83333333333333),
                                 new("azimuth", 30.28813972222222),
                                 new("pseudo_standard_parallel_1", 78.5),
                                 new("scale_factor", 0.9999),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Krovak", "Krovak", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Krovak", gcsKrovak, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);
        ICoordinateTransformation trans2 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);

        // test case 1
        double[] pGeo = [12d, 48d];
        double[] expected = [-953116.2548718402, -1245513.5788112187];

        double[] pUtm = trans.MathTransform.Transform(pGeo);

        // can't inverse trans - Inverse() of ConcateratedTransform makes shallow copy and call Invert on each ICoordinateTransformation.MathTransform - this changes original transformation!
        double[] pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2, true));

        // test case 2
        pGeo = [18, 49];
        expected = [-499143.4909304862, -1192340.009253714];

        pUtm = trans.MathTransform.Transform(pGeo);
        pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2));
    }

    /// <summary>
    /// Verifies forward and inverse Krovak projection referenced to the Ferro prime meridian (EPSG 2065).
    /// </summary>
    [Fact]
    public void TestKrovakFerroProjection()
    {
        // test case for epsg 2065 (prime meridian at Ferro)
        GeographicCoordinateSystem gcsWGS84 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "WGS84 Geographic",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        Ellipsoid ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        HorizontalDatum datum = this.CoordinateSystemFactory.CreateHorizontalDatum(
            "Bessel 1840",
            DatumType.HD_Geocentric,
            ellipsoid,
            new Wgs84ConversionInfo(570.8, 85.7, 462.8, 4.998, 1.587, 5.261, 3.56));

        GeographicCoordinateSystem gcsKrovak = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "Bessel 1840",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Ferro,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("latitude_of_center", 49.5),
                                 new("longitude_of_center", 42.5),
                                 new("azimuth", 30.28813972222222),
                                 new("pseudo_standard_parallel_1", 78.5),
                                 new("scale_factor", 0.9999),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Krovak", "Krovak", parameters);

        ProjectedCoordinateSystem coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Krovak", gcsKrovak, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ICoordinateTransformation trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);
        ICoordinateTransformation trans2 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);

        // test case 1
        double[] pGeo = [12d, 48d];
        double[] expected = [-953116.2548718402, -1245513.5788112187];

        double[] pUtm = trans.MathTransform.Transform(pGeo);

        // can't inverse trans - Inverse() of ConcateratedTransform makes shallow copy and call Invert on each ICoordinateTransformation.MathTransform - this changes original transformation!
        double[] pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2, true));

        // test case 2
        pGeo = [18, 49];
        expected = [-499143.4909304862, -1192340.009253714];

        pUtm = trans.MathTransform.Transform(pGeo);
        pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.True(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2));
    }

    /// <summary>
    /// Verifies forward and inverse Oblique Stereographic projection for EPSG 2171 (Pulkovo 1942(58) / Poland zone I).
    /// </summary>
    [Fact]
    public void TestObliqueStereographicProjection()
    {
        // test data from http://www.spatialreference.org/ref/epsg/2171/
        double[] coord2171 = [4615496.325851, 5605702.221723];
        double[] coord4326 = [20.78002815042, 50.25299100927];

        string wkt4326 = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
        string wkt2171 = "PROJCS[\"Pulkovo 1942(58) / Poland zone I\",GEOGCS[\"Pulkovo 1942(58)\",DATUM[\"Pulkovo_1942_58\",SPHEROID[\"Krassowsky 1940\",6378245,298.3,AUTHORITY[\"EPSG\",\"7024\"]],TOWGS84[33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84],AUTHORITY[\"EPSG\",\"6179\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4179\"]],PROJECTION[\"Oblique_Stereographic\"],PARAMETER[\"latitude_of_origin\",50.625],PARAMETER[\"central_meridian\",21.08333333333333],PARAMETER[\"scale_factor\",0.9998],PARAMETER[\"false_easting\",4637000],PARAMETER[\"false_northing\",5647000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"2171\"]]";

        CoordinateSystem cs1 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt4326);
        CoordinateSystem cs2 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt2171);

        var ctf = new CoordinateTransformationFactory();
        ICoordinateTransformation ict = ctf.CreateFromCoordinateSystems(cs2, cs1);

        double[] transformedCoord4326 = ict.MathTransform.Transform(coord2171);

        Assert.Equal(coord4326[0], transformedCoord4326[0], 0.01);
        Assert.Equal(coord4326[1], transformedCoord4326[1], 0.01);

        ICoordinateTransformation ict2 = ctf.CreateFromCoordinateSystems(cs1, cs2);
        double[] transformedCoord2171 = ict2.MathTransform.Transform(coord4326);

        Assert.Equal(coord2171[0], transformedCoord2171[0], 1);
        Assert.Equal(coord2171[1], transformedCoord2171[1], 1);
    }

    /// <summary>
    /// Verifies forward and inverse Universal Polar Stereographic (UPS North) projection for EPSG 32661.
    /// </summary>
    [Fact]
    public void TestUniversalPolarStereographicProjection()
    {
        // test data from http://epsg.io/transform
        double[] coord4326 = [15.00, 73.00];
        double[] coord32661 = [2491967.01029204, 163954.12194234435];

        string wkt4326 = string.Empty +
            "GEOGCS[\"WGS 84\"," +
            "DATUM[\"WGS_1984\"," +
            "SPHEROID[\"WGS 84\",6378137,298.257223563," +
            "AUTHORITY[\"EPSG\",\"7030\"]]," +
            "AUTHORITY[\"EPSG\",\"6326\"]]," +
            "PRIMEM[\"Greenwich\",0," +
            "AUTHORITY[\"EPSG\",\"8901\"]]," +
            "UNIT[\"degree\",0.01745329251994328," +
            "AUTHORITY[\"EPSG\",\"9122\"]]," +
            "AUTHORITY[\"EPSG\",\"4326\"]]";

        string wkt32661 = string.Empty +
            "PROJCS[\"WGS 84 / UPS North (N,E)\"," +
            "GEOGCS[\"WGS 84\"," +
            "DATUM[\"WGS_1984\"," +
            "SPHEROID[\"WGS 84\",6378137,298.257223563," +
            "AUTHORITY[\"EPSG\",\"7030\"]]," +
            "AUTHORITY[\"EPSG\",\"6326\"]]," +
            "PRIMEM[\"Greenwich\",0," +
            "AUTHORITY[\"EPSG\",\"8901\"]]," +
            "UNIT[\"degree\",0.0174532925199433," +
            "AUTHORITY[\"EPSG\",\"9122\"]]," +
            "AUTHORITY[\"EPSG\",\"4326\"]]," +
            "PROJECTION[\"Polar_Stereographic\"]," +
            "PARAMETER[\"latitude_of_origin\",90]," +
            "PARAMETER[\"central_meridian\",0]," +
            "PARAMETER[\"scale_factor\",0.994]," +
            "PARAMETER[\"false_easting\",2000000]," +
            "PARAMETER[\"false_northing\",2000000]," +
            "UNIT[\"metre\",1," +
            "AUTHORITY[\"EPSG\",\"9001\"]]," +
            "AUTHORITY[\"EPSG\",\"32661\"]]";

        CoordinateSystem cs1 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt4326);
        CoordinateSystem cs2 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt32661);
        var ctf = new CoordinateTransformationFactory();

        ICoordinateTransformation ict = ctf.CreateFromCoordinateSystems(cs2, cs1);
        ICoordinateTransformation ict2 = ctf.CreateFromCoordinateSystems(cs1, cs2);
        double[] transformedCoord4326 = ict.MathTransform.Transform(coord32661);
        double[] transformedCoord32661 = ict2.MathTransform.Transform(coord4326);

        Assert.Equal(coord4326[0], transformedCoord4326[0], 0.01);
        Assert.Equal(coord4326[1], transformedCoord4326[1], 0.01);
        Assert.Equal(coord32661[0], transformedCoord32661[0], 1);
        Assert.Equal(coord32661[1], transformedCoord32661[1], 1);
    }

    /// <summary>
    /// Verifies forward and inverse Australian Antarctic Polar Stereographic projection for EPSG 3032.
    /// </summary>
    [Fact]
    public void TestAustralianAntarcticPolarStereographicProjection()
    {
        // test data from http://epsg.io/transform
        double[] coord4326 = [15.00, -73.00];
        double[] coord3032 = [4476201.247377692, 7066975.373300694];

        string wkt4326 = string.Empty +
            "GEOGCS[\"WGS 84\"," +
            "DATUM[\"WGS_1984\"," +
            "SPHEROID[\"WGS 84\",6378137,298.257223563," +
            "AUTHORITY[\"EPSG\",\"7030\"]]," +
            "AUTHORITY[\"EPSG\",\"6326\"]]," +
            "PRIMEM[\"Greenwich\",0," +
            "AUTHORITY[\"EPSG\",\"8901\"]]," +
            "UNIT[\"degree\",0.01745329251994328," +
            "AUTHORITY[\"EPSG\",\"9122\"]]," +
            "AUTHORITY[\"EPSG\",\"4326\"]]";

        string wkt3032 = string.Empty +
            "PROJCS[\"WGS 84 / Australian Antarctic Polar Stereographic\"," +
            "GEOGCS[\"WGS 84\"," +
            "DATUM[\"WGS_1984\"," +
            "SPHEROID[\"WGS 84\",6378137,298.257223563," +
            "AUTHORITY[\"EPSG\",\"7030\"]]," +
            "AUTHORITY[\"EPSG\",\"6326\"]]," +
            "PRIMEM[\"Greenwich\",0," +
            "AUTHORITY[\"EPSG\",\"8901\"]]," +
            "UNIT[\"degree\",0.0174532925199433," +
            "AUTHORITY[\"EPSG\",\"9122\"]]," +
            "AUTHORITY[\"EPSG\",\"4326\"]]," +
            "PROJECTION[\"Polar_Stereographic\"]," +
            "PARAMETER[\"latitude_of_origin\",-71]," +
            "PARAMETER[\"central_meridian\",70]," +
            "PARAMETER[\"false_easting\",6000000]," +
            "PARAMETER[\"false_northing\",6000000]," +
            "UNIT[\"metre\",1," +
            "AUTHORITY[\"EPSG\",\"9001\"]]," +
            "AUTHORITY[\"EPSG\",\"3032\"]]";

        CoordinateSystem cs1 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt4326);
        CoordinateSystem cs2 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt3032);
        var ctf = new CoordinateTransformationFactory();

        ICoordinateTransformation ict = ctf.CreateFromCoordinateSystems(cs2, cs1);
        ICoordinateTransformation ict2 = ctf.CreateFromCoordinateSystems(cs1, cs2);
        double[] transformedCoord4326 = ict.MathTransform.Transform(coord3032);
        double[] transformedCoord3032 = ict2.MathTransform.Transform(coord4326);

        Assert.Equal(coord4326[0], transformedCoord4326[0], 0.01);
        Assert.Equal(coord4326[1], transformedCoord4326[1], 0.01);
        Assert.Equal(coord3032[0], transformedCoord3032[0], 1);
        Assert.Equal(coord3032[1], transformedCoord3032[1], 1);
    }

    /// <summary>
    /// Verifies that a feet-based projected system (EPSG 2868, Arizona Central State Plane) transforms correctly from WGS84 geographic coordinates.
    /// </summary>
    [Fact]
    public void TestUnitTransforms()
    {
        CoordinateSystem nadUTM = Assert.IsType<CoordinateSystem>(SRIDReader.GetCSbyID(2868), exactMatch: false); // UTM Arizona Central State Plane using Feet as units
        CoordinateSystem wgs84GCS = Assert.IsType<CoordinateSystem>(SRIDReader.GetCSbyID(4326), exactMatch: false); // GCS WGS84
        ICoordinateTransformation trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(wgs84GCS, nadUTM);

        double[] p0 = [-111.89, 34.165];

        double[] expected = [708066.19057935325, 1151426.4460563776];

        double[] p1 = trans.MathTransform.Transform(p0);
        double[] p2 = trans.MathTransform.Inverse().Transform(p1);

        Assert.True(this.ToleranceLessThan(p1, expected, 0.013), this.TransformationError("Unit", expected, p1));

        // WARNING: This accuracy is too poor!
        Assert.True(this.ToleranceLessThan(p0, p2, 0.0001), this.TransformationError("Unit", expected, p1, true));
    }

    /// <summary>
    /// Verifies the Polyconic projection (SAD69 / Brazil Polyconic, EPSG 29101) forward and inverse transforms.
    /// </summary>
    [Fact(DisplayName = "Accuracy very poor!")]
    public void TestPolyconicTransforms()
    {
        CoordinateSystem wgs84GCS = Assert.IsType<CoordinateSystem>(SRIDReader.GetCSbyID(4326), exactMatch: false); // GCS WGS84
        string wkt =

              // "PROJCS[\"SAD69 / Brazil Polyconic (deprecated)\",GEOGCS[\"SAD69\",DATUM[\"South_American_Datum_1969\",SPHEROID[\"GRS 1967\",6378160,298.247167427,AUTHORITY[\"EPSG\",\"7036\"]],TOWGS84[-57,1,-41,0,0,0,0],AUTHORITY[\"EPSG\",\"6291\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9108\"]],AUTHORITY[\"EPSG\",\"4291\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Polyconic\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-54],PARAMETER[\"false_easting\",5000000],PARAMETER[\"false_northing\",10000000],AUTHORITY[\"EPSG\",\"29100\"],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";
              // "PROJCS[\"SAD69 / Brazil Polyconic\",GEOGCS[\"SAD69\",DATUM[\"South_American_Datum_1969\",SPHEROID[\"GRS 1967 Modified\",6378160,298.25,AUTHORITY[\"EPSG\",\"7050\"]],TOWGS84[-57,1,-41,0,0,0,0],AUTHORITY[\"EPSG\",\"6618\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4618\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Polyconic\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-54],PARAMETER[\"false_easting\",5000000],PARAMETER[\"false_northing\",10000000],AUTHORITY[\"EPSG\",\"29101\"],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";
              "PROJCS[\"SAD69 / Brazil Polyconic\",GEOGCS[\"SAD69\",DATUM[\"South_American_Datum_1969\",SPHEROID[\"GRS 1967 (SAD69)\", 6378160, 298.25, AUTHORITY[\"EPSG\", \"7050\"]],AUTHORITY[\"EPSG\", \"6618\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]],UNIT[\"degree\", 0.01745329251994328, AUTHORITY[\"EPSG\", \"9122\"]], AUTHORITY[\"EPSG\", \"4618\"]], PROJECTION[\"Polyconic\"],PARAMETER[\"latitude_of_origin\", 0], PARAMETER[\"central_meridian\", -54],PARAMETER[\"false_easting\", 5000000], PARAMETER[\"false_northing\", 10000000],UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"X\", EAST], AXIS[\"Y\", NORTH],AUTHORITY[\"EPSG\", \"29101\"]]";
        CoordinateSystem sad69 = RequireCoordinateSystem(this.CoordinateSystemFactory, wkt);

        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84GCS, sad69);
        double[] p0 = [-50.085, -14.32];
        double[] expected = [5422386.5795, 8412674.8723];

        // "POINT(5422386.57956145 8412722.92229278)"
        double[] p1 = trans.MathTransform.Transform(p0);
        trans.MathTransform.Invert();
        double[] p2 = trans.MathTransform.Transform(p1);

        Assert.True(this.ToleranceLessThan(p1, expected, 50), this.TransformationError("Polyconic", expected, p1));
        Assert.True(this.ToleranceLessThan(p0, p2, 0.0001), this.TransformationError("Polyconic", expected, p1, true));
    }

    /// <summary>
    /// Verifies forward and inverse Cassini-Soldner projection for DHDN / Soldner Berlin (EPSG 3068).
    /// </summary>
    [Fact]
    public void TestCassiniSoldner()
    {
        GeographicCoordinateSystem csSource = GeographicCoordinateSystem.WGS84;
        CoordinateSystem csTarget = RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            "PROJCS[\"DHDN / Soldner Berlin\",GEOGCS[\"DHDN\",DATUM[\"Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],TOWGS84[598.1,73.7,418.2,0.202,0.045,-2.455,6.7],AUTHORITY[\"EPSG\",\"6314\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4314\"]],PROJECTION[\"Cassini_Soldner\"],PARAMETER[\"latitude_of_origin\",52.41864827777778],PARAMETER[\"central_meridian\",13.62720366666667],PARAMETER[\"false_easting\",40000],PARAMETER[\"false_northing\",10000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"x\",NORTH],AXIS[\"y\",EAST],AUTHORITY[\"EPSG\",\"3068\"]]");

        this.Test(
            "CassiniSoldner",
            csSource,
            csTarget,
            CassiniSoldnerInput,
            CassiniSoldnerExpected,
            0.3,
            1.0E-5);
    }

    /// <summary>
    /// Verifies forward and inverse Hotine Oblique Mercator projection for NAD83(NSRS2007) / Alaska zone 1 (EPSG 3468).
    /// </summary>
    [Fact]
    public void TestHotineObliqueMercator()
    {
        GeographicCoordinateSystem csSource = GeographicCoordinateSystem.WGS84;
        CoordinateSystem csTarget = RequireCoordinateSystem(
            this.CoordinateSystemFactory,
            "PROJCS[\"NAD83(NSRS2007) / Alaska zone 1\",GEOGCS[\"NAD83(NSRS2007)\",DATUM[\"NAD83_National_Spatial_Reference_System_2007\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6759\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4759\"]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"latitude_of_center\",57],PARAMETER[\"longitude_of_center\",-133.6666666666667],PARAMETER[\"azimuth\",323.1301023611111],PARAMETER[\"rectified_grid_angle\",323.1301023611111],PARAMETER[\"scale_factor\",0.9999],PARAMETER[\"false_easting\",5000000],PARAMETER[\"false_northing\",-5000000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],AUTHORITY[\"EPSG\",\"3468\"]]");

        // 61.216667 deg, -149.883333 deg
        // "POINT(4136805.82642057 -4424019.78560519)"
        this.Test(
            "HotineObliqueMercator",
            csSource,
            csTarget,
            [-149.883333, 61.216667],
            [4136805.826, -4424019.786],
            0.01,
            1.0E-5);
    }

    /// <summary>
    /// Verifies that a concatenated transform correctly modifies an <see cref="XY"/> array in-place.
    /// </summary>
    [Fact]
    public void TestTransformListOnConcatenatedDoTransform()
    {
        CoordinateSystem utm35ETRS =
          RequireCoordinateSystem(
              this.CoordinateSystemFactory,
              "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utm35ETRS, utm33);

        var coords = new XY[]
        {
            new(290586.087, 6714000),
            new(290586.392, 6713996.224),
            new(290590.133, 6713973.772),
        };

        trans.MathTransform.Transform(coords);
        Assert.NotEqual(290586.087, coords[0].X);
        Assert.NotEqual(6714000, coords[0].Y);
    }

    /// <summary>
    /// Verifies that a concatenated transform correctly converts a list of double-array coordinates.
    /// </summary>
    [Fact]
    public void TestTransformListOnConcatenatedDoTransformDoubleArr()
    {
        CoordinateSystem utm35ETRS =
          RequireCoordinateSystem(
              this.CoordinateSystemFactory,
              "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utm35ETRS, utm33);

        var coords = new List<double[]>
        {
            TransformListSamplePoint1,
            TransformListSamplePoint2,
            TransformListSamplePoint3,
        };

        IList<double[]> transformedCoords = trans.MathTransform.TransformList(coords);
        Assert.NotEqual(290586.087, transformedCoords[0][0]);
        Assert.NotEqual(6714000, transformedCoords[0][1]);
    }

    /// <summary>
    /// Test transformation for affine transformation.
    /// </summary>
    [Fact]
    public void AffineTransformationTest()
    {
        // Local coordinate system MNAU (Kraftwerk Maeuserich) (based on Gauss-Krueger using affine transformation)
        // affine transform
        // 1) Offset: X=-3454886,640m Y=-5479481,278m;
        // 2)Rotation: 332,0657, Rotation point  X=3456926,640m Y=5481071,278m;
        // 3) Scale: 1.0
        double[,] matrix = new double[,]
        {
            { 0.883485346527455, -0.468458794848877, 3455869.17937689 },
            { 0.468458794848877, 0.883485346527455, 5478710.88035753 },
            { 0.0, 0.0, 1 },
        };
        var mt = new AffineTransform(matrix);

        Assert.NotNull(mt);

        Assert.Equal(2, mt.DimSource);
        Assert.Equal(2, mt.DimTarget);

        // Transformation example (MNAU -> GK)
        // Start point (MNAU) X=2040,000m Y=1590,000m]
        // Target point (GK): X=3456926,640m Y=5481071,278m;
        double[] outPt = mt.Transform(AffineTestPoint);

        Assert.Equal(2, outPt.Length);
        Assert.Equal(3456926.640, outPt[0], 0.00000001);
        Assert.Equal(5481071.278, outPt[1], 0.00000001);
    }

    /// <summary>
    /// Test inverse transformation for affine transformation.
    /// </summary>
    [Fact]
    public void InverseAffineTransformationTest()
    {
        // Local coordinate system MNAU (Kraftwerk Maeuserich) (based on Gauss-Krueger using affine transformation)
        // affine transform
        // 1) Offset: X=-3454886,640m Y=-5479481,278m;
        // 2)Rotation: 332,0657, Rotation point  X=3456926,640m Y=5481071,278m;
        // 3) Scale: 1.0
        double[,] matrix = new double[,]
        {
            { 0.883485346527455, -0.468458794848877, 3455869.17937689 },
            { 0.468458794848877, 0.883485346527455, 5478710.88035753 },
            { 0.0, 0.0, 1 },
        };
        var mt = new AffineTransform(matrix);

        Assert.NotNull(mt);

        Assert.Equal(2, mt.DimSource);
        Assert.Equal(2, mt.DimTarget);

        // Transformation example (MNAU -> GK)
        // Start point (MNAU) X=2040,000m Y=1590,000m]
        // Target point (GK): X=3456926,640m Y=5481071,278m;

        // check source transform
        double[] outPt = mt.Transform(AffineTestPoint);

        Assert.Equal(2, outPt.Length);
        Assert.Equal(3456926.640, outPt[0], 0.00000001);
        Assert.Equal(5481071.278, outPt[1], 0.00000001);

        MathTransform invMt = mt.Inverse();

        double[] inPt = invMt.Transform(AffineTargetPoint);

        Assert.Equal(2, inPt.Length);
        Assert.Equal(2040.0, inPt[0], 0.00000001);
        Assert.Equal(1590.0, inPt[1], 0.00000001);

        // check source transform - once more
        double[] outPt2 = mt.Transform(AffineTestPoint);

        Assert.Equal(2, outPt2.Length);
        Assert.Equal(3456926.640, outPt2[0], 0.00000001);
        Assert.Equal(5481071.278, outPt2[1], 0.00000001);
    }

    /// <summary>
    /// Coordinate transformation test for fitted coordinate system - test CS - local coordinate system MNAU.
    /// </summary>
    [Fact]
    public void TestTransformOnFittedCoordinateSystem()
    {
        // Local coordinate system MNAU (Kraftwerk Maeuserich) (based on Gauss-Krueger using affine transformation)
        // affine transform
        // 1) Offset: X=-3454886,640m Y=-5479481,278m;
        // 2)Rotation: 332,0657, Rotation point  X=3456926,640m Y=5481071,278m;
        // 3) Scale: 1.0
        string ft_wkt = "FITTED_CS[\"Local coordinate system MNAU (based on Gauss-Krueger)\"," +
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

        // string gk_wkt = "PROJCS[\"DHDN / Gauss-Kruger zone 3\",GEOGCS[\"DHDN\",DATUM[\"Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],AUTHORITY[\"EPSG\",\"6314\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4314\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",3500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"31467\"]]";
        var fac = new CoordinateSystemFactory();
        FittedCoordinateSystem fcs = RequireCoordinateSystem<FittedCoordinateSystem>(fac, ft_wkt);

        // ICoordinateSystem gkcs = fac.CreateFromWkt (gk_wkt);

        // Transformation example (MNAU -> GK)
        // Start point (MNAU) X=2040,000m Y=1590,000m]
        // Target point (GK): X=3456926,640m Y=5481071,278m;
        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(fcs, fcs.BaseCoordinateSystem);

        var coords = new List<double[]>
        {
            AffineTestPoint,
        };

        IList<double[]> transformedCoords = trans.MathTransform.TransformList(coords);
        Assert.Equal(3456926.640, transformedCoords[0][0], 0.00000001);
        Assert.Equal(5481071.278, transformedCoords[0][1], 0.00000001);
    }

    /// <summary>
    /// Verifies WKT2-derived geographic CRS definitions integrate with the fitted runtime path when transforming to the parsed base CRS.
    /// </summary>
    [Fact]
    public void TestTransformFromDerivedGeographicWkt2ToBaseCoordinateSystem()
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        string wkt = CreateDerivedGeographicRuntimeCoordinateSystem().ToWktNode(WktVersion.Wkt22019).ToString();
        FittedCoordinateSystem derived = RequireCoordinateSystem<FittedCoordinateSystem>(coordinateSystemFactory, wkt);
        GeographicCoordinateSystem baseCoordinateSystem = Assert.IsType<GeographicCoordinateSystem>(derived.BaseCoordinateSystem);

        ICoordinateTransformation transformation = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(derived, baseCoordinateSystem);

        double[] localPoint = [12.5, 55.25];
        double[] expected = derived.ToBaseTransform.Transform(localPoint);
        double[] actual = transformation.MathTransform.Transform(localPoint);
        double[] roundTripped = transformation.MathTransform.Inverse().Transform(actual);

        Assert.Equal(expected[0], actual[0], 12);
        Assert.Equal(expected[1], actual[1], 12);
        Assert.Equal(localPoint[0], roundTripped[0], 12);
        Assert.Equal(localPoint[1], roundTripped[1], 12);
    }

    /// <summary>
    /// Verifies WKT2-derived projected CRS definitions compose through the fitted runtime path when transforming to another projected CRS.
    /// </summary>
    [Fact]
    public void TestTransformFromDerivedProjectedWkt2ToDifferentProjectedCoordinateSystem()
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        string wkt = CreateDerivedProjectedRuntimeCoordinateSystem().ToWktNode(WktVersion.Wkt22019).ToString();
        FittedCoordinateSystem derived = RequireCoordinateSystem<FittedCoordinateSystem>(coordinateSystemFactory, wkt);
        ProjectedCoordinateSystem baseCoordinateSystem = Assert.IsType<ProjectedCoordinateSystem>(derived.BaseCoordinateSystem);
        var targetCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        ICoordinateTransformation transformation = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(derived, targetCoordinateSystem);
        ICoordinateTransformation baseTransformation = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(baseCoordinateSystem, targetCoordinateSystem);

        double[] localPoint = [450000d, 6200000d];
        double[] expected = baseTransformation.MathTransform.Transform(derived.ToBaseTransform.Transform(localPoint));
        double[] actual = transformation.MathTransform.Transform(localPoint);

        Assert.Equal(expected[0], actual[0], 8);
        Assert.Equal(expected[1], actual[1], 8);
    }

    /// <summary>
    /// Verifies PROJJSON-derived projected CRS definitions compose through the fitted runtime path when transforming from another projected CRS.
    /// </summary>
    [Fact]
    public void TestTransformFromDifferentProjectedCoordinateSystemToDerivedProjectedProjJson()
    {
        FittedCoordinateSystem derived = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(ProjJsonWriter.ToJson(CreateDerivedProjectedRuntimeCoordinateSystem())));
        var sourceCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        ProjectedCoordinateSystem baseCoordinateSystem = Assert.IsType<ProjectedCoordinateSystem>(derived.BaseCoordinateSystem);

        ICoordinateTransformation transformation = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(sourceCoordinateSystem, derived);
        ICoordinateTransformation sourceToBaseTransformation = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(sourceCoordinateSystem, baseCoordinateSystem);

        double[] sourcePoint = [500000d, 6100000d];
        double[] expected = derived.ToBaseTransform.Inverse().Transform(sourceToBaseTransformation.MathTransform.Transform(sourcePoint));
        double[] actual = transformation.MathTransform.Transform(sourcePoint);

        Assert.Equal(expected[0], actual[0], 8);
        Assert.Equal(expected[1], actual[1], 8);
    }

    /// <summary>
    /// Tests the EPSG 21780 (Bern 1898 (Bern) / LV03C) projection with a non-Greenwich prime meridian.
    /// </summary>
    [Fact]
    public void TestEPSG21780PrimeMeredianTransformation()
    {
        string wkt4326 = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
        string wkt21780 = "PROJCS[\"Bern 1898 (Bern) / LV03C\",GEOGCS[\"Bern 1898 (Bern)\",DATUM[\"CH1903_Bern\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],AUTHORITY[\"EPSG\",\"6801\"]],PRIMEM[\"Bern\",7.439583333333333,AUTHORITY[\"EPSG\",\"8907\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4801\"]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"latitude_of_center\",46.95240555555556],PARAMETER[\"longitude_of_center\",0],PARAMETER[\"azimuth\",90],PARAMETER[\"rectified_grid_angle\",90],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"21780\"]]";

        // test data from http://spatialreference.org/ref/epsg/21780/
        double[] sourceCoord = [160443.329034, 23582.55586];
        double[] expectedTargetCoord = [9.5553588867188, 47.145080566406];

        CoordinateSystem cs1 = Assert.IsType<CoordinateSystem>(CoordinateSystemWktReader.Parse(wkt21780), exactMatch: false);
        CoordinateSystem cs2 = Assert.IsType<CoordinateSystem>(CoordinateSystemWktReader.Parse(wkt4326), exactMatch: false);
        var ctf = new CoordinateTransformationFactory();
        ICoordinateTransformation ict = ctf.CreateFromCoordinateSystems(cs1, cs2);

        double[] transformedCoord = ict.MathTransform.Transform(sourceCoord);

        Assert.True(transformedCoord.Length >= 2);
        Assert.Equal(expectedTargetCoord[0], transformedCoord[0], 0.001);
        Assert.Equal(expectedTargetCoord[1], transformedCoord[1], 0.001);

        // and back
        ICoordinateTransformation ictb = ctf.CreateFromCoordinateSystems(cs2, cs1);
        transformedCoord = ictb.MathTransform.Transform(transformedCoord);

        Assert.True(transformedCoord.Length >= 2);
        Assert.Equal(sourceCoord[0], transformedCoord[0], 0.1);
        Assert.Equal(sourceCoord[1], transformedCoord[1], 0.1);
    }

    // https://github.com/NetTopologySuite/ProjNet4GeoAPI/issues/48

    /// <summary>
    /// Verifies the Hotine Oblique Mercator transformation for EPSG 2056 (CH1903+ / LV95, Switzerland).
    /// </summary>
    [Fact]
    public void TestEPSG2056HotineObliqueMercatorAzimuthCenterSwitzerland()
    {
        GeographicCoordinateSystem csSrc = GeographicCoordinateSystem.WGS84;
        CoordinateSystem csTgt = Assert.IsType<CoordinateSystem>(SRIDReader.GetCSbyID(2056), exactMatch: false); // CH1903+ / LV95
        ICoordinateTransformation transformer = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csSrc, csTgt);
        double x = 9.619803;
        double y = 47.408735;

        transformer.MathTransform.Transform(ref x, ref y);

        // https://epsg.io/transform#s_srs=4326&t_srs=2056&x=9.6198031&y=47.4087350
        Assert.InRange(x, 2764607.79 - 0.1, 2764607.79 + 0.1);
        Assert.InRange(y, 1253167.89 - 0.1, 1253167.89 + 0.1);
    }

    /// <summary>
    /// Verifies forward and inverse ellipsoidal Orthographic projection, including detection of points beyond the visible hemisphere.
    /// </summary>
    [Fact]
    public void TestEllipsoidalOrthographicTransform()
    {
        // Check equatorial projection
        GeographicCoordinateSystem csWgs84 = GeographicCoordinateSystem.WGS84;
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new("central_meridian", 0),
                                 new("latitude_of_origin", 0),
                                 new("scale_factor", 1),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection = this.CoordinateSystemFactory.CreateProjection("Orthographic", "Orthographic", parameters);
        ProjectedCoordinateSystem orthographicSystem = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Orthographic centered", csWgs84, projection, LinearUnit.Metre, new AxisInfo("X", AxisOrientationEnum.East), new AxisInfo("Y", AxisOrientationEnum.North));
        ICoordinateTransformation trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csWgs84, orthographicSystem);

        // Check origin remains in the same place
        double[] origin = [0.0, 0.0];
        double[] transformedOrigin = trans.MathTransform.Transform(origin);
        double[] inverseTransformedOrigin = trans.MathTransform.Inverse().Transform(transformedOrigin);
        Assert.True(this.ToleranceLessThan(origin, transformedOrigin, 0.00001), this.TransformationError("Orthographic", origin, transformedOrigin));
        Assert.True(this.ToleranceLessThan(origin, inverseTransformedOrigin, 0.00001), this.TransformationError("Orthograhpic", origin, inverseTransformedOrigin, true));

        // Check projection works as expected away from origin
        double[] testEastWgs = [0.001, 0.0];
        double[] expectedXOrtho = [111, 0.0]; // We should expect that .001 degrees is equal to 111 meters at origin
        double[] transEastWgs = trans.MathTransform.Transform(testEastWgs);
        double[] invTransEastWgs = trans.MathTransform.Inverse().Transform(transEastWgs);
        Assert.True(this.ToleranceLessThan(expectedXOrtho, transEastWgs, 1.0), this.TransformationError("Orthographic", expectedXOrtho, transEastWgs));
        Assert.True(this.ToleranceLessThan(testEastWgs, invTransEastWgs, 1.0), this.TransformationError("Orthographic", testEastWgs, invTransEastWgs, true));

        // Check from guidance 7.2
        var parameters2 = new List<ProjectionParameter>(5)
                             {
                                 new("central_meridian", 5.0),
                                 new("latitude_of_origin", 55.0),
                                 new("scale_factor", 1),
                                 new("false_easting", 0),
                                 new("false_northing", 0),
                             };
        IProjection projection2 = this.CoordinateSystemFactory.CreateProjection("Orthographic", "Orthographic", parameters2);
        ProjectedCoordinateSystem orthoSystem2 = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Orthographic", csWgs84, projection2, LinearUnit.Metre, new AxisInfo("X", AxisOrientationEnum.East), new AxisInfo("Y", AxisOrientationEnum.North));
        ICoordinateTransformation trans2 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csWgs84, orthoSystem2);
        double[] test2 = [2.1295499950867, 53.809394412498];
        double[] expected2 = [-189011.711, -128640.567];
        double[] transTest2 = trans2.MathTransform.Transform(test2);
        double[] invTransTest2 = trans2.MathTransform.Inverse().Transform(transTest2);
        Assert.True(this.ToleranceLessThan(expected2, transTest2, 1.0), this.TransformationError("Orthographic", expected2, transTest2));
        Assert.True(this.ToleranceLessThan(test2, invTransTest2, 1.0), this.TransformationError("Orthographic", test2, invTransTest2, true));

        // Check that the algorithm correctly identifies a point that cannot be seen
        Action action1 = () => trans.MathTransform.Transform(OrthographicHorizonTestPoint);
        Assert.Throws<ArgumentOutOfRangeException>(action1);

        Action action2 = () => trans2.MathTransform.Transform(OrthographicHorizonTestPoint);
        Assert.Throws<ArgumentOutOfRangeException>(action2);
    }

    /// <summary>
    /// Verifies transformation from WGS 1984 Web Mercator Auxiliary Sphere to a Lambert Conformal Conic state plane system.
    /// </summary>
    [Fact]
    public static void TestMercatorAuxilarySphereTransformation()
    {
        string sourceWkt = "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Mercator_Auxiliary_Sphere\"],PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",0.0],PARAMETER[\"Standard_Parallel_1\",0.0],PARAMETER[\"Auxiliary_Sphere_Type\",0.0],UNIT[\"Meter\",1.0]]";
        CoordinateSystem sourceCoordinateSystem = GetCoordinateSystem(sourceWkt);
        Assert.NotNull(sourceCoordinateSystem);

        string targetWkt = "PROJCS[\"TX83-NCF\",GEOGCS[\"LL83\",DATUM[\"NAD83\",SPHEROID[\"GRS1980\",6378137.000,298.25722210]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Lambert_Conformal_Conic_2SP\"],PARAMETER[\"false_easting\",1968500.000],PARAMETER[\"false_northing\",6561666.667],PARAMETER[\"central_meridian\",-98.50000000000000],PARAMETER[\"latitude_of_origin\",31.66666666666666],PARAMETER[\"standard_parallel_1\",33.96666666666667],PARAMETER[\"standard_parallel_2\",32.13333333333333],UNIT[\"Foot_US\",0.30480060960122]]";
        CoordinateSystem targetCoordinateSystem = GetCoordinateSystem(targetWkt);
        Assert.NotNull(targetCoordinateSystem);

        ICoordinateTransformation transformation = GetTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        Assert.NotNull(transformation);

        (double X, double Y) tranformedPoint = transformation.MathTransform.Transform(-10775704.511, 3865240.329);

        Assert.Equal(2491034.95, tranformedPoint.X, 0.1);
        Assert.Equal(6968468.98, tranformedPoint.Y, 0.1);
    }

    /// <summary>
    /// Verifies that the Popular Visualisation Pseudo Mercator projection is recognized and a transformation can be created.
    /// </summary>
    [Fact]
    public void TestPopularVisualizationPseudoMercatorProjectionRegistry()
    {
        string sourceWkt = "GEOGCS[\"GCS_WGS_1984\", DATUM[\"D_WGS_1984\", SPHEROID[\"WGS_1984\",6378137.0,298.257223563]], PRIMEM[\"Greenwich\",0.0], UNIT[\"Degree\",0.0174532925199433]]";
        string targetWkt = "PROJCS[\"WGS84.PseudoMercator\",GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722356]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Popular Visualisation Pseudo Mercator\"],PARAMETER[\"false_easting\",0.000],PARAMETER[\"false_northing\",0.000],PARAMETER[\"central_meridian\",0.00000000000000],UNIT[\"Meter\",1.00000000000000]]";

        CoordinateSystem sourceCoordinateSystem = GetCoordinateSystem(sourceWkt);
        Assert.NotNull(sourceCoordinateSystem);

        CoordinateSystem targetCoordinateSystem = GetCoordinateSystem(targetWkt);
        Assert.NotNull(targetCoordinateSystem);

        ICoordinateTransformation transformation = GetTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        Assert.NotNull(transformation);
    }

    /// <summary>
    /// Verifies that the Lambert Tangential Conformal Conic projection is registered and that the transformation to Pseudo Mercator is within tolerance.
    /// </summary>
    [Fact]
    public void TestLamberTangentialConformalConicProjectionRegistryAndTransformation()
    {
        string sourceWkt = "PROJCS[\"WORLD-LM-TAN\",GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722356]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Lambert Tangential Conformal Conic Projection\"],PARAMETER[\"false_easting\",0.000],PARAMETER[\"false_northing\",0.000],PARAMETER[\"scale_factor\",1.000000000000],PARAMETER[\"central_meridian\",0.00000000000000],PARAMETER[\"latitude_of_origin\",1.00000000000000],UNIT[\"Meter\",1.00000000000000]]";
        string targetWkt = "PROJCS[\"WGS84.PseudoMercator\",GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722356]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Popular Visualisation Pseudo Mercator\"],PARAMETER[\"false_easting\",0.000],PARAMETER[\"false_northing\",0.000],PARAMETER[\"central_meridian\",0.00000000000000],UNIT[\"Meter\",1.00000000000000]]";

        CoordinateSystem sourceCoordinateSystem = GetCoordinateSystem(sourceWkt);
        Assert.NotNull(sourceCoordinateSystem);

        CoordinateSystem targetCoordinateSystem = GetCoordinateSystem(targetWkt);
        Assert.NotNull(targetCoordinateSystem);

        ICoordinateTransformation transformation = GetTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        Assert.NotNull(transformation);

        // Test the transformation with a known points. Tested with AutoCAD map 3D
        double[] pGeo = [4101119.6855, -229063.8661]; // Nairobi, Kenya
        double[] pUtm = transformation.MathTransform.Transform(pGeo);

        double[] expected = [4098998.6422, -142387.5532];
        Assert.True(this.ToleranceLessThan(pUtm, expected, 0.05), this.TransformationError("LambertConicConformal2SP", expected, pUtm));
    }

    /// <summary>
    /// Creates a coordinate system from WKT for test setup.
    /// </summary>
    /// <param name="wkt">Well-known text representation of the coordinate system.</param>
    /// <returns>Parsed coordinate system instance.</returns>
    internal static CoordinateSystem GetCoordinateSystem(string wkt)
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        return RequireCoordinateSystem(coordinateSystemFactory, wkt);
    }

    /// <summary>
    /// Creates a transformation between source and target coordinate systems for test execution.
    /// </summary>
    /// <param name="sourceCoordinateSystem">Source coordinate system.</param>
    /// <param name="targetCoordinateSystem">Target coordinate system.</param>
    /// <returns>Coordinate transformation instance.</returns>
    internal static ICoordinateTransformation GetTransformation(CoordinateSystem sourceCoordinateSystem, CoordinateSystem targetCoordinateSystem)
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        var coordinateService = new CoordinateSystemServices(coordinateSystemFactory, new CoordinateTransformationFactory());
        ICoordinateTransformation? transformation = coordinateService.CreateTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        return Assert.IsType<ICoordinateTransformation>(transformation, exactMatch: false);
    }

    private static FittedCoordinateSystem CreateDerivedGeographicRuntimeCoordinateSystem()
    {
        GeographicCoordinateSystem baseCoordinateSystem = GeographicCoordinateSystem.WGS84;
        return new CoordinateSystemFactory().CreateFittedCoordinateSystem(
            "Runtime derived geographic",
            baseCoordinateSystem,
            new AffineTransform(1, 0, 0.5, 0, 1, 1.5),
            [
                new AxisInfo(baseCoordinateSystem.GetAxis(0).Name, baseCoordinateSystem.GetAxis(0).Orientation),
                new AxisInfo(baseCoordinateSystem.GetAxis(1).Name, baseCoordinateSystem.GetAxis(1).Orientation),
            ]);
    }

    private static FittedCoordinateSystem CreateDerivedProjectedRuntimeCoordinateSystem()
    {
        var baseCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        return new CoordinateSystemFactory().CreateFittedCoordinateSystem(
            "Runtime derived projected",
            baseCoordinateSystem,
            new AffineTransform(1, 0, 100, 0, 1, -50),
            [
                new AxisInfo(baseCoordinateSystem.GetAxis(0).Name, baseCoordinateSystem.GetAxis(0).Orientation),
                new AxisInfo(baseCoordinateSystem.GetAxis(1).Name, baseCoordinateSystem.GetAxis(1).Orientation),
            ]);
    }
}
