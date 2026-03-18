// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Geometries;
using ProjNet.IO.CoordinateSystems;

public class CoordinateTransformTests : CoordinateTransformTestsBase
{
    public CoordinateTransformTests()
    {
        this.Verbose = true;
    }

    [Xunit.Fact]
    public void TestTransformListOfCoordinates()
    {
        var csFact = new CoordinateSystemFactory();
        var ctFact = new CoordinateTransformationFactory();

        var utm35ETRS = csFact.CreateFromWkt(
                "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        var trans = ctFact.CreateFromCoordinateSystems(utm35ETRS, utm33);

        XY[] points =
        {
            new XY(290586.087, 6714000), new XY(290586.392, 6713996.224),
            new XY(290590.133, 6713973.772), new XY(290594.111, 6713957.416),
            new XY(290596.615, 6713943.567), new XY(290596.701, 6713939.485),
        };

        var tpoints = (XY[])points.Clone();
        trans.MathTransform.Transform(tpoints);
        for (int i = 0; i < points.Length; i++)
        {
            double expectedX = points[i].X;
            double expectedY = points[i].Y;
            trans.MathTransform.Transform(ref expectedX, ref expectedY);

            double actualX = tpoints[i].X;
            double actualY = tpoints[i].Y;

            Assert.That(actualX, Is.EqualTo(expectedX).Within(1E-8));
            Assert.That(actualY, Is.EqualTo(expectedY).Within(1E-8));
        }
    }

    [Xunit.Fact]
    public void TestTransformListOfDoubleArray()
    {
        var csFact = new CoordinateSystemFactory();
        var ctFact = new CoordinateTransformationFactory();

        var utm35ETRS = csFact.CreateFromWkt(
                "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);

        var trans = ctFact.CreateFromCoordinateSystems(utm35ETRS, utm33);

        double[][] points =
        {
            new[] {290586.087, 6714000 }, new[] {90586.392, 6713996.224},
            new[] {290590.133, 6713973.772}, new[] {290594.111, 6713957.416},
            new[] {290596.615, 6713943.567}, new[] {290596.701, 6713939.485},
        };

        double[][] tpoints = trans.MathTransform.TransformList(points).ToArray();
        for (int i = 0; i < points.Length; i++)
        {
            double expectedX = points[i][0];
            double expectedY = points[i][1];
            trans.MathTransform.Transform(ref expectedX, ref expectedY);

            double actualX = tpoints[i][0];
            double actualY = tpoints[i][1];

            Assert.That(actualX, Is.EqualTo(expectedX).Within(1E-8));
            Assert.That(actualY, Is.EqualTo(expectedY).Within(1E-8));
        }
    }

    [Xunit.Fact]
    public void TestCentralMeridianParse()
    {
        const string strSouthPole = "PROJCS[\"South_Pole_Lambert_Azimuthal_Equal_Area\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Lambert_Azimuthal_Equal_Area\"],PARAMETER[\"False_Easting\",0],PARAMETER[\"False_Northing\",0],PARAMETER[\"Central_Meridian\",-127],PARAMETER[\"Latitude_Of_Origin\",-90],UNIT[\"Meter\",1]]";

        var pCoordSysFactory = new CoordinateSystemFactory();
        var pSouthPole = pCoordSysFactory.CreateFromWkt(strSouthPole);
        Assert.IsNotNull(pSouthPole);
    }

    [Xunit.Fact]
    public void TestAlbersProjection()
    {
        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1866", 6378206.4, 294.9786982138982, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Clarke 1866", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Clarke 1866", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("central_meridian", -96),
                                 new ProjectionParameter("latitude_of_center", 23),
                                 new ProjectionParameter("standard_parallel_1", 29.5),
                                 new ProjectionParameter("standard_parallel_2", 45.5),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Albers Conical Equal Area", "albers", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Albers Conical Equal Area", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans1 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcs, coordsys);
        var trans2 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(coordsys, gcs);

        double[] pGeo = new double[] { -75, 35 };
        double[] pUtm = trans1.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans2.MathTransform.Transform(pUtm);

        double[] expected = new[] { 1885472.7, 1535925 };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.05), this.TransformationError("Albers", expected, pUtm, false));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Albers", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestAlbersProjectionFeet()
    {
        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1866", 6378206.4, 294.9786982138982, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Clarke 1866", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Clarke 1866", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("central_meridian", -96),
                                 new ProjectionParameter("latitude_of_center", 23),
                                 new ProjectionParameter("standard_parallel_1", 29.5),
                                 new ProjectionParameter("standard_parallel_2", 45.5),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Albers Conical Equal Area", "albers", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Albers Conical Equal Area", gcs, projection, LinearUnit.Foot, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = new double[] { -75, 35 };
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = new[] { 1885472.7 / LinearUnit.Foot.MetersPerUnit, 1535925 / LinearUnit.Foot.MetersPerUnit };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.1), this.TransformationError("Albers", expected, pUtm, false));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Albers", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestMercator_1SP_Projection()
    {
        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Bessel 1840", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Bessel 1840", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_origin", 0),
                                 new ProjectionParameter("central_meridian", 110),
                                 new ProjectionParameter("scale_factor", 0.997),
                                 new ProjectionParameter("false_easting", 3900000),
                                 new ProjectionParameter("false_northing", 900000),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Mercator_1SP", "Mercator_1SP", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Makassar / NEIEZ", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = new double[] { 120, -3 };
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = new[] { 5009726.58, 569150.82 };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.02), this.TransformationError("Mercator_1SP", expected, pUtm, false));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Mercator_1SP", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestMercator_1SP_Projection_Feet()
    {
        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Bessel 1840", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Bessel 1840", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_origin", 0),
                                 new ProjectionParameter("central_meridian", 110),
                                 new ProjectionParameter("scale_factor", 0.997),
                                 new ProjectionParameter("false_easting", 3900000 / LinearUnit.Foot.MetersPerUnit),
                                 new ProjectionParameter("false_northing", 900000 / LinearUnit.Foot.MetersPerUnit),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Mercator_1SP", "Mercator_1SP", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Makassar / NEIEZ", gcs, projection, LinearUnit.Foot, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = new[] { 120d, -3d };
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = new[] { 5009726.58 / LinearUnit.Foot.MetersPerUnit, 569150.82 / LinearUnit.Foot.MetersPerUnit };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.02), this.TransformationError("Mercator_1SP", expected, pUtm, false));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Mercator_1SP", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestMercator_2SP_Projection()
    {
        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Krassowski 1940", 6378245.0, 298.3, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Krassowski 1940", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Krassowski 1940", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_origin", 42),
                                 new ProjectionParameter("central_meridian", 51),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Mercator_2SP", "Mercator_2SP", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Pulkovo 1942 / Mercator Caspian Sea", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = new[] { 53d, 53d };
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = new[] { 165704.29, 5171848.07 };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.02), this.TransformationError("Mercator_2SP", expected, pUtm, false));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Mercator_2SP", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestTransverseMercator_Projection()
    {
        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Airy 1830", 6377563.396, 299.32496, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Airy 1830", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Airy 1830", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_origin", 49),
                                 new ProjectionParameter("central_meridian", -2),
                                 new ProjectionParameter("scale_factor", 0.9996012717 /* 0.9996*/),
                                 new ProjectionParameter("false_easting", 400000),
                                 new ProjectionParameter("false_northing", -100000),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Transverse Mercator", "Transverse_Mercator", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("OSGB 1936 / British National Grid", gcs, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = new[] { 0.5, 50.5 };
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        // "POINT(577393.372775651 69673.621953601)"
        double[] expected = new[] { 577274.98, 69740.49 };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.01), this.TransformationError("TransverseMercator", expected, pUtm));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 1E-6), this.TransformationError("TransverseMercator", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestLambertConicConformal2SP_Projection()
    {
        var ellipsoid = /*Ellipsoid.Clarke1866;*/
            this.CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1866", 20925832.16, 294.97470, LinearUnit.USSurveyFoot);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Clarke 1866", DatumType.HD_Geocentric, ellipsoid, null);
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Clarke 1866", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_origin", 27.833333333),
                                 new ProjectionParameter("central_meridian", -99),
                                 new ProjectionParameter("standard_parallel_1", 28.3833333333),
                                 new ProjectionParameter("standard_parallel_2", 30.2833333333),
                                 new ProjectionParameter("false_easting", 2000000 / LinearUnit.USSurveyFoot.MetersPerUnit),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Lambert Conic Conformal (2SP)", "lambert_conformal_conic_2sp", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("NAD27 / Texas South Central", gcs, projection, LinearUnit.USSurveyFoot, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, coordsys);

        double[] pGeo = new[] { -96, 28.5 };
        double[] pUtm = trans.MathTransform.Transform(pGeo);
        double[] pGeo2 = trans.MathTransform.Inverse().Transform(pUtm);

        double[] expected = new[] { 2963503.91 / LinearUnit.USSurveyFoot.MetersPerUnit, 254759.80 / LinearUnit.USSurveyFoot.MetersPerUnit };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.05), this.TransformationError("LambertConicConformal2SP", expected, pUtm));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("LambertConicConformal2SP", pGeo, pGeo2, true));
    }

    private ICoordinateTransformation CreateGeo2Laea(double centralMeridian, double latitudeOfOrigin)
    {
        var wgs84 = GeographicCoordinateSystem.WGS84;

        var coordsys = this.CoordinateSystemFactory.CreateFromWkt(string.Empty +
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
                    "UNIT[\"Meter\",1.0]]");

        return this.CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84, coordsys);
    }

    [Xunit.Fact]
    [Repeat(1000)]
    public void TestLambertAzimuthalEqualArea_Projection_round_trip_on_origin()
    {
        double centralMeridian = this.Random.Next(-180, +180);
        double latitudeOfOrigin = this.Random.Next(-90, +90);

        var trans = this.CreateGeo2Laea(centralMeridian, latitudeOfOrigin);

        var forward = trans.MathTransform;
        var reverse = forward.Inverse();

        double[] pGeo = new[] { centralMeridian, latitudeOfOrigin };

        double[] pLaea = forward.Transform(pGeo);

        double[] pGeo2 = reverse.Transform(pLaea);

        double[] expectedPLaea = new double[2] { 0, 0 };

        Assert.IsTrue(this.ToleranceLessThan(pLaea, expectedPLaea, 0.05), this.TransformationError("Lambert_Azimuthal_Equal_Area", expectedPLaea, pLaea));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Lambert_Azimuthal_Equal_Area", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    [Repeat(1000)]
    public void TestLambertAzimuthalEqualArea_Projection_round_trip_on_arbitrary_point()
    {
        int GetRandomSign()
        {
            return this.Random.Next() % 2 == 0 ? -1 : +1;
        }

        double centralMeridian = this.Random.Next(-150, +150);
        double latitudeOfOrigin = this.Random.Next(-70, +70);

        var trans = this.CreateGeo2Laea(centralMeridian, latitudeOfOrigin);

        var forward = trans.MathTransform;
        var reverse = forward.Inverse();

        double lat = latitudeOfOrigin + ((0.01 + this.Random.NextDouble()) * GetRandomSign());
        double lon = centralMeridian + ((0.01 + this.Random.NextDouble()) * GetRandomSign());

        double[] pGeo = new[] { lon, lat };

        double[] pLaea = forward.Transform(pGeo);

        double[] pGeo2 = reverse.Transform(pLaea);

        Assert.NotZero(pLaea[0]);
        Assert.NotZero(pLaea[1]);
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.0000001), this.TransformationError("Lambert_Azimuthal_Equal_Area", pGeo, pGeo2, true));
    }

    [Xunit.Fact]
    public void TestGeocentric()
    {
        var gcs = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("ETRF89 Geographic", AngularUnit.Degrees, HorizontalDatum.ETRF89, PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));
        var gcenCs = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("ETRF89 Geocentric", HorizontalDatum.ETRF89, LinearUnit.Metre, PrimeMeridian.Greenwich);
        var ct = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, gcenCs);
        double[] pExpected = new[] { 2 + (7.0 / 60) + (46.38 / 3600), 53 + (48.0 / 60) + (33.82 / 3600) }; // Point.FromDMS(2, 7, 46.38, 53, 48, 33.82);
        double[] pExpected3D = new[] { pExpected[0], pExpected[1], 73.0 };
        double[] p0 = new[] { 3771793.97, 140253.34, 5124304.35 };
        double[] p1 = ct.MathTransform.Transform(pExpected3D);
        double[] p2 = ct.MathTransform.Inverse().Transform(p1);
        Assert.IsTrue(this.ToleranceLessThan(p1, p0, 0.01));
        Assert.IsTrue(this.ToleranceLessThan(p2, pExpected, 0.00001));
    }

    [Xunit.Fact]
    public void TestDatumTransform()
    {
        // Define datums, set parameters
        var wgs72 = HorizontalDatum.WGS72;
        wgs72.Wgs84Parameters = new Wgs84ConversionInfo(0, 0, 4.5, 0, 0, 0.554, 0.219);
        var ed50 = HorizontalDatum.ED50;
        ed50.Wgs84Parameters = new Wgs84ConversionInfo(-81.0703, -89.3603, -115.7526,
                                                       -0.48488, -0.02436, -0.41321,
                                                       -0.540645); // Parameters for Denmark

        // Define geographic coordinate systems
        var gcsWGS72 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("WGS72 Geographic", AngularUnit.Degrees, wgs72, PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var gcsWGS84 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("WGS84 Geographic", AngularUnit.Degrees, HorizontalDatum.WGS84, PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var gcsED50 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("ED50 Geographic", AngularUnit.Degrees, ed50, PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        // Define geocentric coordinate systems
        var gcenCsWGS72 = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("WGS72 Geocentric", wgs72, LinearUnit.Metre, PrimeMeridian.Greenwich);
        var gcenCsWGS84 = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        var gcenCsED50 = this.CoordinateSystemFactory.CreateGeocentricCoordinateSystem("ED50 Geocentric", ed50, LinearUnit.Metre, PrimeMeridian.Greenwich);

        // Define projections
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_origin", 0),
                                 new ProjectionParameter("central_meridian", 9),
                                 new ProjectionParameter("scale_factor", 0.9996),
                                 new ProjectionParameter("false_easting", 500000),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Transverse Mercator", "Transverse_Mercator", parameters);
        var utmED50 = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("ED50 UTM Zone 32N", gcsED50, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));
        var utmWGS84 = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("WGS84 UTM Zone 32N", gcsWGS84, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        ////Set up coordinate transformations
        // var ctForw = _coordinateTransformationFactory.CreateFromCoordinateSystems(gcsWGS72, gcenCsWGS72); //Geographic->Geocentric (WGS72)
        // var ctWGS84_Gcen2Geo = _coordinateTransformationFactory.CreateFromCoordinateSystems(gcenCsWGS84, gcsWGS84);  //Geocentric->Geographic (WGS84)
        // var ctWGS84_Geo2UTM = _coordinateTransformationFactory.CreateFromCoordinateSystems(gcsWGS84, utmWGS84);  //UTM ->Geographic (WGS84)
        // var ctED50_UTM2Geo = _coordinateTransformationFactory.CreateFromCoordinateSystems(utmED50, gcsED50);  //UTM ->Geographic (ED50)
        // var ctED50_Geo2Gcen = _coordinateTransformationFactory.CreateFromCoordinateSystems(gcsED50, gcenCsED50); //Geographic->Geocentric (ED50)

        // Test datum-shift from WGS72 to WGS84
        // Point3D pGeoCenWGS72 = ctForw.MathTransform.Transform(pLongLatWGS72) as Point3D;
        double[] pGeoCenWGS72 = new[] { 3657660.66, 255768.55, 5201382.11 };
        var geocen_ed50_2_Wgs84 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(gcenCsWGS72, gcenCsWGS84);
        double[] pGeoCenWGS84 = geocen_ed50_2_Wgs84.MathTransform.Transform(pGeoCenWGS72);

        // Point3D pGeoCenWGS84 = wgs72.Wgs84Parameters.Apply(pGeoCenWGS72);
        double[] pExpected = new[] { 3657660.78, 255778.43, 5201387.75 };
        Assert.IsTrue(this.ToleranceLessThan(pExpected, pGeoCenWGS84, 0.01), this.TransformationError("Datum WGS72->WGS84", pExpected, pGeoCenWGS84));

        // and inverse
        double[] pGeoCenWGS72calc = geocen_ed50_2_Wgs84.MathTransform.Inverse().Transform(pGeoCenWGS84);
        Assert.IsTrue(this.ToleranceLessThan(pGeoCenWGS72, pGeoCenWGS72calc, 0.001), this.TransformationError("Datum WGS84->WGS72", pGeoCenWGS72, pGeoCenWGS72calc));

        var utm_ed50_2_Wgs84 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utmED50, utmWGS84);
        double[] pUTMED50 = new double[] { 600000, 6100000 };
        double[] pUTMWGS84 = utm_ed50_2_Wgs84.MathTransform.Transform(pUTMED50);
        pExpected = new[] { 599928.6, 6099790.2 };
        Assert.IsTrue(this.ToleranceLessThan(pExpected, pUTMWGS84, 0.1), this.TransformationError("Datum ED50->WGS84", pExpected, pUTMWGS84));

        // and inverse
        double[] pUTMED50calc = utm_ed50_2_Wgs84.MathTransform.Inverse().Transform(pUTMWGS84);
        Assert.IsTrue(this.ToleranceLessThan(pUTMED50, pUTMED50calc, 0.01), this.TransformationError("Datum WGS84->ED50", pUTMED50, pUTMED50calc));

        // Perform reverse
        var utm_Wgs84_2_Ed50 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utmWGS84, utmED50);
        pUTMED50 = utm_Wgs84_2_Ed50.MathTransform.Transform(pUTMWGS84);
        pExpected = new double[] { 600000, 6100000 };
        Assert.IsTrue(this.ToleranceLessThan(pExpected, pUTMED50, 0.1), this.TransformationError("Datum", pExpected, pUTMED50));

        // and inverse
        double[] pUTMWGS84calc = utm_Wgs84_2_Ed50.MathTransform.Inverse().Transform(pUTMED50);
        Assert.IsTrue(this.ToleranceLessThan(pUTMWGS84, pUTMWGS84calc, 0.1), this.TransformationError("Datum", pUTMWGS84, pUTMWGS84calc));

        // Assert.IsTrue(Math.Abs((pUTMWGS84 as Point3D).Z - 36.35) < 0.5);
        // Point pExpected = Point.FromDMS(2, 7, 46.38, 53, 48, 33.82);
        // ED50_to_WGS84_Denmark: datum.Wgs84Parameters = new Wgs84ConversionInfo(-89.5, -93.8, 127.6, 0, 0, 4.5, 1.2);
    }

    [Xunit.Fact]
    public void TestKrovak_Greenwich_Projection()
    {
        // test case for epsg 5514 (102067)
        var gcsWGS84 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("WGS84 Geographic", AngularUnit.Degrees, HorizontalDatum.WGS84, PrimeMeridian.Greenwich,
             new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Bessel 1840", DatumType.HD_Geocentric, ellipsoid, null);
        datum.Wgs84Parameters = new Wgs84ConversionInfo(570.8, 85.7, 462.8, 4.998, 1.587, 5.261, 3.56);

        var gcsKrovak = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Bessel 1840", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_center", 49.5),
                                 new ProjectionParameter("longitude_of_center", 24.83333333333333),
                                 new ProjectionParameter("azimuth", 30.28813972222222),
                                 new ProjectionParameter("pseudo_standard_parallel_1", 78.5),
                                 new ProjectionParameter("scale_factor", 0.9999),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Krovak", "Krovak", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Krovak", gcsKrovak, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);
        var trans2 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);

        // test case 1
        double[] pGeo = new[] { 12d, 48d };
        double[] expected = new[] { -953116.2548718402, -1245513.5788112187 };

        double[] pUtm = trans.MathTransform.Transform(pGeo);

        // can't inverse trans - Inverse() of ConcateratedTransform makes shallow copy and call Invert on each ICoordinateTransformation.MathTransform - this changes original transformation!
        double[] pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2, true));

        // test case 2
        pGeo = new double[] { 18, 49 };
        expected = new double[] { -499143.4909304862, -1192340.009253714 };

        pUtm = trans.MathTransform.Transform(pGeo);
        pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2));
    }

    [Xunit.Fact]
    public void TestKrovak_Ferro_Projection()
    {
        // test case for epsg 2065 (prime meridian at Ferro)
        var gcsWGS84 = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("WGS84 Geographic", AngularUnit.Degrees, HorizontalDatum.WGS84, PrimeMeridian.Greenwich,
            new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var ellipsoid = this.CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1840", 6377397.155, 299.15281, LinearUnit.Metre);

        var datum = this.CoordinateSystemFactory.CreateHorizontalDatum("Bessel 1840", DatumType.HD_Geocentric, ellipsoid, null);
        datum.Wgs84Parameters = new Wgs84ConversionInfo(570.8, 85.7, 462.8, 4.998, 1.587, 5.261, 3.56);

        var gcsKrovak = this.CoordinateSystemFactory.CreateGeographicCoordinateSystem("Bessel 1840", AngularUnit.Degrees, datum,
            PrimeMeridian.Greenwich, new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        gcsKrovak.PrimeMeridian = PrimeMeridian.Ferro;

        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("latitude_of_center", 49.5),
                                 new ProjectionParameter("longitude_of_center", 42.5),
                                 new ProjectionParameter("azimuth", 30.28813972222222),
                                 new ProjectionParameter("pseudo_standard_parallel_1", 78.5),
                                 new ProjectionParameter("scale_factor", 0.9999),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Krovak", "Krovak", parameters);

        var coordsys = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Krovak", gcsKrovak, projection, LinearUnit.Metre, new AxisInfo("East", AxisOrientationEnum.East), new AxisInfo("North", AxisOrientationEnum.North));

        var trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);
        var trans2 = new CoordinateTransformationFactory().CreateFromCoordinateSystems(gcsWGS84, coordsys);

        // test case 1
        double[] pGeo = new[] { 12d, 48d };
        double[] expected = new[] { -953116.2548718402, -1245513.5788112187 };

        double[] pUtm = trans.MathTransform.Transform(pGeo);

        // can't inverse trans - Inverse() of ConcateratedTransform makes shallow copy and call Invert on each ICoordinateTransformation.MathTransform - this changes original transformation!
        double[] pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2, true));

        // test case 2
        pGeo = new double[] { 18, 49 };
        expected = new double[] { -499143.4909304862, -1192340.009253714 };

        pUtm = trans.MathTransform.Transform(pGeo);
        pGeo2 = trans2.MathTransform.Inverse().Transform(pUtm);

        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.2), this.TransformationError("Krovak", expected, pUtm));
        Assert.IsTrue(this.ToleranceLessThan(pGeo, pGeo2, 0.001), this.TransformationError("Krovak", pGeo, pGeo2));
    }

    [Xunit.Fact]
    public void TestObliqueStereographicProjection()
    {
        // test data from http://www.spatialreference.org/ref/epsg/2171/
        double[] coord2171 = new double[] { 4615496.325851, 5605702.221723 };
        double[] coord4326 = new double[] { 20.78002815042, 50.25299100927 };

        string wkt4326 = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
        string wkt2171 = "PROJCS[\"Pulkovo 1942(58) / Poland zone I\",GEOGCS[\"Pulkovo 1942(58)\",DATUM[\"Pulkovo_1942_58\",SPHEROID[\"Krassowsky 1940\",6378245,298.3,AUTHORITY[\"EPSG\",\"7024\"]],TOWGS84[33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84],AUTHORITY[\"EPSG\",\"6179\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4179\"]],PROJECTION[\"Oblique_Stereographic\"],PARAMETER[\"latitude_of_origin\",50.625],PARAMETER[\"central_meridian\",21.08333333333333],PARAMETER[\"scale_factor\",0.9998],PARAMETER[\"false_easting\",4637000],PARAMETER[\"false_northing\",5647000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"2171\"]]";

        var cs1 = this.CoordinateSystemFactory.CreateFromWkt(wkt4326);
        var cs2 = this.CoordinateSystemFactory.CreateFromWkt(wkt2171);

        var ctf = new CoordinateTransformationFactory();
        var ict = ctf.CreateFromCoordinateSystems(cs2, cs1);

        double[] transformedCoord4326 = ict.MathTransform.Transform(coord2171);

        Assert.AreEqual(coord4326[0], transformedCoord4326[0], 0.01);
        Assert.AreEqual(coord4326[1], transformedCoord4326[1], 0.01);

        var ict2 = ctf.CreateFromCoordinateSystems(cs1, cs2);
        double[] transformedCoord2171 = ict2.MathTransform.Transform(coord4326);

        Assert.AreEqual(coord2171[0], transformedCoord2171[0], 1);
        Assert.AreEqual(coord2171[1], transformedCoord2171[1], 1);
    }

    [Xunit.Fact]
    public void TestUniversalPolarStereographicProjection()
    {
        // test data from http://epsg.io/transform
        double[] coord4326 = new double[] { 15.00, 73.00 };
        double[] coord32661 = new double[] { 2491967.01029204, 163954.12194234435 };

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

        var cs1 = this.CoordinateSystemFactory.CreateFromWkt(wkt4326);
        var cs2 = this.CoordinateSystemFactory.CreateFromWkt(wkt32661);
        var ctf = new CoordinateTransformationFactory();

        var ict = ctf.CreateFromCoordinateSystems(cs2, cs1);
        var ict2 = ctf.CreateFromCoordinateSystems(cs1, cs2);
        double[] transformedCoord4326 = ict.MathTransform.Transform(coord32661);
        double[] transformedCoord32661 = ict2.MathTransform.Transform(coord4326);

        Assert.AreEqual(coord4326[0], transformedCoord4326[0], 0.01);
        Assert.AreEqual(coord4326[1], transformedCoord4326[1], 0.01);
        Assert.AreEqual(coord32661[0], transformedCoord32661[0], 1);
        Assert.AreEqual(coord32661[1], transformedCoord32661[1], 1);
    }

    [Xunit.Fact]
    public void TestAustralianAntarcticPolarStereographicProjection()
    {
        // test data from http://epsg.io/transform
        double[] coord4326 = new double[] { 15.00, -73.00 };
        double[] coord3032 = new double[] { 4476201.247377692, 7066975.373300694 };

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

        var cs1 = this.CoordinateSystemFactory.CreateFromWkt(wkt4326);
        var cs2 = this.CoordinateSystemFactory.CreateFromWkt(wkt3032);
        var ctf = new CoordinateTransformationFactory();

        var ict = ctf.CreateFromCoordinateSystems(cs2, cs1);
        var ict2 = ctf.CreateFromCoordinateSystems(cs1, cs2);
        double[] transformedCoord4326 = ict.MathTransform.Transform(coord3032);
        double[] transformedCoord3032 = ict2.MathTransform.Transform(coord4326);

        Assert.AreEqual(coord4326[0], transformedCoord4326[0], 0.01);
        Assert.AreEqual(coord4326[1], transformedCoord4326[1], 0.01);
        Assert.AreEqual(coord3032[0], transformedCoord3032[0], 1);
        Assert.AreEqual(coord3032[1], transformedCoord3032[1], 1);
    }

    [Xunit.Fact]
    public void TestUnitTransforms()
    {
        var nadUTM = SRIDReader.GetCSbyID(2868); // UTM Arizona Central State Plane using Feet as units
        var wgs84GCS = SRIDReader.GetCSbyID(4326); // GCS WGS84
        var trans = new CoordinateTransformationFactory().CreateFromCoordinateSystems(wgs84GCS, nadUTM);

        double[] p0 = new[] { -111.89, 34.165 };

        // var expected = new[] { 708066.19058, 1151461.51413 };
        double[] expected = new[] { 708066.19057935325, 1151426.4460563776 };

        double[] p1 = trans.MathTransform.Transform(p0);
        double[] p2 = trans.MathTransform.Inverse().Transform(p1);

        Assert.IsTrue(this.ToleranceLessThan(p1, expected, 0.013), this.TransformationError("Unit", expected, p1));

        // WARNING: This accuracy is too poor!
        Assert.IsTrue(this.ToleranceLessThan(p0, p2, 0.0001), this.TransformationError("Unit", expected, p1, true));
    }

    [Xunit.Fact(DisplayName = "Accuracy very poor!")]
    public void TestPolyconicTransforms()
    {
        var wgs84GCS = SRIDReader.GetCSbyID(4326); // GCS WGS84
        string wkt =

              // "PROJCS[\"SAD69 / Brazil Polyconic (deprecated)\",GEOGCS[\"SAD69\",DATUM[\"South_American_Datum_1969\",SPHEROID[\"GRS 1967\",6378160,298.247167427,AUTHORITY[\"EPSG\",\"7036\"]],TOWGS84[-57,1,-41,0,0,0,0],AUTHORITY[\"EPSG\",\"6291\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9108\"]],AUTHORITY[\"EPSG\",\"4291\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Polyconic\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-54],PARAMETER[\"false_easting\",5000000],PARAMETER[\"false_northing\",10000000],AUTHORITY[\"EPSG\",\"29100\"],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";
              // "PROJCS[\"SAD69 / Brazil Polyconic\",GEOGCS[\"SAD69\",DATUM[\"South_American_Datum_1969\",SPHEROID[\"GRS 1967 Modified\",6378160,298.25,AUTHORITY[\"EPSG\",\"7050\"]],TOWGS84[-57,1,-41,0,0,0,0],AUTHORITY[\"EPSG\",\"6618\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4618\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],PROJECTION[\"Polyconic\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",-54],PARAMETER[\"false_easting\",5000000],PARAMETER[\"false_northing\",10000000],AUTHORITY[\"EPSG\",\"29101\"],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";
              "PROJCS[\"SAD69 / Brazil Polyconic\",GEOGCS[\"SAD69\",DATUM[\"South_American_Datum_1969\",SPHEROID[\"GRS 1967 (SAD69)\", 6378160, 298.25, AUTHORITY[\"EPSG\", \"7050\"]],AUTHORITY[\"EPSG\", \"6618\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]],UNIT[\"degree\", 0.01745329251994328, AUTHORITY[\"EPSG\", \"9122\"]], AUTHORITY[\"EPSG\", \"4618\"]], PROJECTION[\"Polyconic\"],PARAMETER[\"latitude_of_origin\", 0], PARAMETER[\"central_meridian\", -54],PARAMETER[\"false_easting\", 5000000], PARAMETER[\"false_northing\", 10000000],UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]], AXIS[\"X\", EAST], AXIS[\"Y\", NORTH],AUTHORITY[\"EPSG\", \"29101\"]]";
        var sad69 = this.CoordinateSystemFactory.CreateFromWkt(wkt);

        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84GCS, sad69);
        double[] p0 = new[] { -50.085, -14.32 };
        double[] expected = new[] { 5422386.5795, 8412674.8723 };

        // "POINT(5422386.57956145 8412722.92229278)"
        double[] p1 = trans.MathTransform.Transform(p0);
        trans.MathTransform.Invert();
        double[] p2 = trans.MathTransform.Transform(p1);

        Assert.IsTrue(this.ToleranceLessThan(p1, expected, 50), this.TransformationError("Polyconic", expected, p1));
        Assert.IsTrue(this.ToleranceLessThan(p0, p2, 0.0001), this.TransformationError("Polyconic", expected, p1, true));
    }

    [Xunit.Fact]
    public void TestCassiniSoldner()
    {
        var csSource = GeographicCoordinateSystem.WGS84;
        var csTarget = this.CoordinateSystemFactory.CreateFromWkt(
            "PROJCS[\"DHDN / Soldner Berlin\",GEOGCS[\"DHDN\",DATUM[\"Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],TOWGS84[598.1,73.7,418.2,0.202,0.045,-2.455,6.7],AUTHORITY[\"EPSG\",\"6314\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4314\"]],PROJECTION[\"Cassini_Soldner\"],PARAMETER[\"latitude_of_origin\",52.41864827777778],PARAMETER[\"central_meridian\",13.62720366666667],PARAMETER[\"false_easting\",40000],PARAMETER[\"false_northing\",10000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"x\",NORTH],AXIS[\"y\",EAST],AUTHORITY[\"EPSG\",\"3068\"]]");

        this.Test("CassiniSoldner", csSource, csTarget,
             new[] { 13.408055555556, 52.518611111111 },
             new[] { 25244.540, 21300.969 }, 0.3, 1.0E-5);

        /*
        var ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(csSource, csTarget);
        var pgeo = new[] {13.408055555556, 52.518611111111};
        var pcs = ct.MathTransform.Transform(pgeo);

        //Evaluated using DotSpatial.Projections
        var pcsExpected = new[] {25244.540, 21300.969};

        Assert.IsTrue(ToleranceLessThan(pcsExpected, pcs, 0.3), TransformationError("CassiniSoldner", pcsExpected, pcs));
        var pgeo2 = ct.MathTransform.Inverse().Transform(pcs);
        Assert.IsTrue(ToleranceLessThan(pgeo, pgeo2, 1.0E-5), TransformationError("CassiniSoldner", pgeo, pgeo2));
         */
    }

    [Xunit.Fact]
    public void TestHotineObliqueMercator()
    {
        var csSource = GeographicCoordinateSystem.WGS84;
        var csTarget = this.CoordinateSystemFactory.CreateFromWkt(
           "PROJCS[\"NAD83(NSRS2007) / Alaska zone 1\",GEOGCS[\"NAD83(NSRS2007)\",DATUM[\"NAD83_National_Spatial_Reference_System_2007\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6759\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4759\"]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"latitude_of_center\",57],PARAMETER[\"longitude_of_center\",-133.6666666666667],PARAMETER[\"azimuth\",323.1301023611111],PARAMETER[\"rectified_grid_angle\",323.1301023611111],PARAMETER[\"scale_factor\",0.9999],PARAMETER[\"false_easting\",5000000],PARAMETER[\"false_northing\",-5000000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH],AUTHORITY[\"EPSG\",\"3468\"]]");

        // 61.216667°, -149.883333°
        // "POINT(4136805.82642057 -4424019.78560519)"
        this.Test("HotineObliqueMercator", csSource, csTarget,
             new[] { -149.883333, 61.216667 },
             new[] { 4136805.826, -4424019.786 }, 0.01, 1.0E-5);
    }

    [Xunit.Fact]
    public void TestTransformListOnConcatenatedDoTransform()
    {
        var utm35ETRS =
          this.CoordinateSystemFactory.CreateFromWkt(
              "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utm35ETRS, utm33);

        var coords = new XY[]
        {
            new XY(290586.087, 6714000),
            new XY(290586.392, 6713996.224),
            new XY(290590.133, 6713973.772),
        };

        trans.MathTransform.Transform(coords);
        Assert.AreNotEqual(290586.087, coords[0].X);
        Assert.AreNotEqual(6714000, coords[0].Y);
    }

    [Xunit.Fact]
    public void TestTransformListOnConcatenatedDoTransformDoubleArr()
    {
        var utm35ETRS =
          this.CoordinateSystemFactory.CreateFromWkt(
              "PROJCS[\"ETRS89 / ETRS-TM35\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",27],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]");

        var utm33 = ProjectedCoordinateSystem.WGS84_UTM(33, true);
        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(utm35ETRS, utm33);

        var coords = new List<double[]>
        {
            new double[]{290586.087, 6714000},
            new double[]{290586.392, 6713996.224},
            new double[]{290590.133, 6713973.772},
        };

        var transformedCoords = trans.MathTransform.TransformList(coords);
        Assert.AreNotEqual(290586.087, transformedCoords[0][0]);
        Assert.AreNotEqual(6714000, transformedCoords[0][1]);
    }

    /// <summary>
    /// Test transformation for affine transformation.
    /// </summary>
    [Xunit.Fact]
    public void AffineTransformationTest()
    {
        // Local coordinate system MNAU (Kraftwerk Mäuserich) (based on Gauß-Krüger using affine transformation)
        // affine transform
        // 1) Offset: X=-3454886,640m Y=-5479481,278m;
        // 2)Rotation: 332,0657, Rotation point  X=3456926,640m Y=5481071,278m;
        // 3) Scale: 1.0

        // TODO MathTransformFactory fac = new MathTransformFactory ();
        double[,] matrix = new double[,]
        {
            {0.883485346527455, -0.468458794848877, 3455869.17937689},
                                          {0.468458794848877, 0.883485346527455, 5478710.88035753},
                                          {0.0, 0.0, 1},
        };
        var mt = new AffineTransform(matrix);

        Assert.IsNotNull(mt);

        Assert.AreEqual(2, mt.DimSource);
        Assert.AreEqual(2, mt.DimTarget);

        // Transformation example (MNAU -> GK)
        // Start point (MNAU) X=2040,000m Y=1590,000m]
        // Target point (GK): X=3456926,640m Y=5481071,278m;
        double[] outPt = mt.Transform(new double[] { 2040.0, 1590.0 });

        Assert.AreEqual(2, outPt.Length);
        Assert.AreEqual(3456926.640, outPt[0], 0.00000001);
        Assert.AreEqual(5481071.278, outPt[1], 0.00000001);
    }

    /// <summary>
    /// Test inverse transformation for affine transformation.
    /// </summary>
    [Xunit.Fact]
    public void InverseAffineTransformationTest()
    {
        // Local coordinate system MNAU (Kraftwerk Mäuserich) (based on Gauß-Krüger using affine transformation)
        // affine transform
        // 1) Offset: X=-3454886,640m Y=-5479481,278m;
        // 2)Rotation: 332,0657, Rotation point  X=3456926,640m Y=5481071,278m;
        // 3) Scale: 1.0

        // TODO MathTransformFactory fac = new MathTransformFactory ();
        double[,] matrix = new double[,]
        {
            {0.883485346527455, -0.468458794848877, 3455869.17937689},
                                          {0.468458794848877, 0.883485346527455, 5478710.88035753},
                                          {0.0, 0.0, 1},
        };
        var mt = new AffineTransform(matrix);

        Assert.IsNotNull(mt);

        Assert.AreEqual(2, mt.DimSource);
        Assert.AreEqual(2, mt.DimTarget);

        // Transformation example (MNAU -> GK)
        // Start point (MNAU) X=2040,000m Y=1590,000m]
        // Target point (GK): X=3456926,640m Y=5481071,278m;

        // check source transform
        double[] outPt = mt.Transform(new double[] { 2040.0, 1590.0 });

        Assert.AreEqual(2, outPt.Length);
        Assert.AreEqual(3456926.640, outPt[0], 0.00000001);
        Assert.AreEqual(5481071.278, outPt[1], 0.00000001);

        var invMt = mt.Inverse();

        double[] inPt = invMt.Transform(new double[] { 3456926.640, 5481071.278 });

        Assert.AreEqual(2, inPt.Length);
        Assert.AreEqual(2040.0, inPt[0], 0.00000001);
        Assert.AreEqual(1590.0, inPt[1], 0.00000001);

        // check source transform - once more
        double[] outPt2 = mt.Transform(new double[] { 2040.0, 1590.0 });

        Assert.AreEqual(2, outPt2.Length);
        Assert.AreEqual(3456926.640, outPt2[0], 0.00000001);
        Assert.AreEqual(5481071.278, outPt2[1], 0.00000001);
    }

    /// <summary>
    /// Coordinate transformation test for fitted coordinate system - test CS - local coordinate system MNAU.
    /// </summary>
    [Xunit.Fact]
    public void TestTransformOnFittedCoordinateSystem()
    {
        // Local coordinate system MNAU (Kraftwerk Mäuserich) (based on Gauß-Krüger using affine transformation)
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
        var fcs = fac.CreateFromWkt(ft_wkt) as FittedCoordinateSystem;

        // ICoordinateSystem gkcs = fac.CreateFromWkt (gk_wkt);

        // Transformation example (MNAU -> GK)
        // Start point (MNAU) X=2040,000m Y=1590,000m]
        // Target point (GK): X=3456926,640m Y=5481071,278m;
        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(fcs, fcs.BaseCoordinateSystem);

        var coords = new List<double[]>
        {
            new double[]{2040.0, 1590.0},
        };

        var transformedCoords = trans.MathTransform.TransformList(coords);
        Assert.AreEqual(3456926.640, transformedCoords[0][0], 0.00000001);
        Assert.AreEqual(5481071.278, transformedCoords[0][1], 0.00000001);
    }

    /// <summary>
    /// test for epsg 21780 projection (different prime meridian).
    /// </summary>
    [Xunit.Fact]
    public void Test_EPSG_21780_PrimeMeredianTransformation()
    {
        string wkt4326 = "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";
        string wkt21780 = "PROJCS[\"Bern 1898 (Bern) / LV03C\",GEOGCS[\"Bern 1898 (Bern)\",DATUM[\"CH1903_Bern\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],AUTHORITY[\"EPSG\",\"6801\"]],PRIMEM[\"Bern\",7.439583333333333,AUTHORITY[\"EPSG\",\"8907\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4801\"]],PROJECTION[\"Hotine_Oblique_Mercator\"],PARAMETER[\"latitude_of_center\",46.95240555555556],PARAMETER[\"longitude_of_center\",0],PARAMETER[\"azimuth\",90],PARAMETER[\"rectified_grid_angle\",90],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"21780\"]]";

        // test data from http://spatialreference.org/ref/epsg/21780/
        double[] sourceCoord = new double[] { 160443.329034, 23582.55586 };
        double[] expectedTargetCoord = new double[] { 9.5553588867188, 47.145080566406 };

        var cs1 = CoordinateSystemWktReader.Parse(wkt21780) as CoordinateSystem;
        var cs2 = CoordinateSystemWktReader.Parse(wkt4326) as CoordinateSystem;
        var ctf = new CoordinateTransformationFactory();
        var ict = ctf.CreateFromCoordinateSystems(cs1, cs2);

        double[] transformedCoord = ict.MathTransform.Transform(sourceCoord);

        Assert.IsTrue(transformedCoord.Length >= 2);
        Assert.AreEqual(expectedTargetCoord[0], transformedCoord[0], 0.001);
        Assert.AreEqual(expectedTargetCoord[1], transformedCoord[1], 0.001);

        // and back
        var ictb = ctf.CreateFromCoordinateSystems(cs2, cs1);
        transformedCoord = ictb.MathTransform.Transform(transformedCoord);

        Assert.IsTrue(transformedCoord.Length >= 2);
        Assert.AreEqual(sourceCoord[0], transformedCoord[0], 0.1);
        Assert.AreEqual(sourceCoord[1], transformedCoord[1], 0.1);
    }

    // https://github.com/NetTopologySuite/ProjNet4GeoAPI/issues/48
    [Xunit.Fact]
    public void Test_EPSG_2056_HotineObliqueMercatorAzimuthCenter_Switzerland()
    {
        var csSrc = GeographicCoordinateSystem.WGS84;
        var csTgt = SRIDReader.GetCSbyID(2056); // CH1903+ / LV95
        var transformer = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csSrc, csTgt);
        double x = 9.619803;
        double y = 47.408735;

        transformer.MathTransform.Transform(ref x, ref y);

        // https://epsg.io/transform#s_srs=4326&t_srs=2056&x=9.6198031&y=47.4087350
        Assert.That(x, Is.EqualTo(2764607.79).Within(0.1));
        Assert.That(y, Is.EqualTo(1253167.89).Within(0.1));
    }

    [Xunit.Fact]
    public void TestEllipsoidalOrthographicTransform()
    {
        // Check equatorial projection
        var csWgs84 = GeographicCoordinateSystem.WGS84;
        var parameters = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("central_meridian", 0),
                                 new ProjectionParameter("latitude_of_origin", 0),
                                 new ProjectionParameter("scale_factor", 1),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection = this.CoordinateSystemFactory.CreateProjection("Orthographic", "Orthographic", parameters);
        var orthographicSystem = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Orthographic centered", csWgs84, projection, LinearUnit.Metre, new AxisInfo("X", AxisOrientationEnum.East), new AxisInfo("Y", AxisOrientationEnum.North));
        var trans = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csWgs84, orthographicSystem);

        // Check origin remains in the same place
        double[] origin = new[] { 0.0, 0.0 };
        double[] transformedOrigin = trans.MathTransform.Transform(origin);
        double[] inverseTransformedOrigin = trans.MathTransform.Inverse().Transform(transformedOrigin);
        Assert.That(this.ToleranceLessThan(origin, transformedOrigin, 0.00001), this.TransformationError("Orthographic", origin, transformedOrigin));
        Assert.That(this.ToleranceLessThan(origin, inverseTransformedOrigin, 0.00001), this.TransformationError("Orthograhpic", origin, inverseTransformedOrigin, true));

        // Check projection works as expected away from origin
        double[] testEastWgs = new[] { 0.001, 0.0 };
        double[] expectedXOrtho = new[] { 111, 0.0 }; // We should expect that .001 degrees is equal to 111 meters at origin
        double[] transEastWgs = trans.MathTransform.Transform(testEastWgs);
        double[] invTransEastWgs = trans.MathTransform.Inverse().Transform(transEastWgs);
        Assert.That(this.ToleranceLessThan(expectedXOrtho, transEastWgs, 1.0), this.TransformationError("Orthographic", expectedXOrtho, transEastWgs));
        Assert.That(this.ToleranceLessThan(testEastWgs, invTransEastWgs, 1.0), this.TransformationError("Orthographic", testEastWgs, invTransEastWgs, true));

        // Check from guidance 7.2
        var parameters2 = new List<ProjectionParameter>(5)
                             {
                                 new ProjectionParameter("central_meridian", 5.0),
                                 new ProjectionParameter("latitude_of_origin", 55.0),
                                 new ProjectionParameter("scale_factor", 1),
                                 new ProjectionParameter("false_easting", 0),
                                 new ProjectionParameter("false_northing", 0),
                             };
        var projection2 = this.CoordinateSystemFactory.CreateProjection("Orthographic", "Orthographic", parameters2);
        var orthoSystem2 = this.CoordinateSystemFactory.CreateProjectedCoordinateSystem("Orthographic", csWgs84, projection2, LinearUnit.Metre, new AxisInfo("X", AxisOrientationEnum.East), new AxisInfo("Y", AxisOrientationEnum.North));
        var trans2 = this.CoordinateTransformationFactory.CreateFromCoordinateSystems(csWgs84, orthoSystem2);
        double[] test2 = new[] { 2.1295499950867, 53.809394412498 };
        double[] expected2 = new[] { -189011.711, -128640.567 };
        double[] transTest2 = trans2.MathTransform.Transform(test2);
        double[] invTransTest2 = trans2.MathTransform.Inverse().Transform(transTest2);
        Assert.That(this.ToleranceLessThan(expected2, transTest2, 1.0), this.TransformationError("Orthographic", expected2, transTest2));
        Assert.That(this.ToleranceLessThan(test2, invTransTest2, 1.0), this.TransformationError("Orthographic", test2, invTransTest2, true));

        // Check that the algorithm correctly identifies a point that cannot be seen
        var @delegate = new TestDelegate(() => trans.MathTransform.Transform(new[] { 180.0, 0.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(@delegate);

        var @delegate2 = new TestDelegate(() => trans2.MathTransform.Transform(new[] { 180.0, 0.0 }));
        Assert.Throws<ArgumentOutOfRangeException>(@delegate2);
    }

    [Xunit.Fact]
    public static void TestMercatorAuxilarySphereTransformation()
    {
        string sourceWkt = "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere\",GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137.0,298.257223563]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Mercator_Auxiliary_Sphere\"],PARAMETER[\"False_Easting\",0.0],PARAMETER[\"False_Northing\",0.0],PARAMETER[\"Central_Meridian\",0.0],PARAMETER[\"Standard_Parallel_1\",0.0],PARAMETER[\"Auxiliary_Sphere_Type\",0.0],UNIT[\"Meter\",1.0]]";
        var sourceCoordinateSystem = GetCoordinateSystem(sourceWkt);
        Assert.NotNull(sourceCoordinateSystem);

        string targetWkt = "PROJCS[\"TX83-NCF\",GEOGCS[\"LL83\",DATUM[\"NAD83\",SPHEROID[\"GRS1980\",6378137.000,298.25722210]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Lambert_Conformal_Conic_2SP\"],PARAMETER[\"false_easting\",1968500.000],PARAMETER[\"false_northing\",6561666.667],PARAMETER[\"central_meridian\",-98.50000000000000],PARAMETER[\"latitude_of_origin\",31.66666666666666],PARAMETER[\"standard_parallel_1\",33.96666666666667],PARAMETER[\"standard_parallel_2\",32.13333333333333],UNIT[\"Foot_US\",0.30480060960122]]";
        var targetCoordinateSystem = GetCoordinateSystem(targetWkt);
        Assert.NotNull(targetCoordinateSystem);

        var transformation = GetTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        Assert.NotNull(transformation);

        var tranformedPoint = transformation.MathTransform.Transform(-10775704.511, 3865240.329);
        Assert.NotNull(tranformedPoint);

        Assert.AreEqual(2491034.95, tranformedPoint.x, 0.1);
        Assert.AreEqual(6968468.98, tranformedPoint.y, 0.1);
    }

    [Xunit.Fact]
    public void TestPopularVisualizationPseudoMercatorProjectionRegistry()
    {
        string sourceWkt = "GEOGCS[\"GCS_WGS_1984\", DATUM[\"D_WGS_1984\", SPHEROID[\"WGS_1984\",6378137.0,298.257223563]], PRIMEM[\"Greenwich\",0.0], UNIT[\"Degree\",0.0174532925199433]]";
        string targetWkt = "PROJCS[\"WGS84.PseudoMercator\",GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722356]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Popular Visualisation Pseudo Mercator\"],PARAMETER[\"false_easting\",0.000],PARAMETER[\"false_northing\",0.000],PARAMETER[\"central_meridian\",0.00000000000000],UNIT[\"Meter\",1.00000000000000]]";

        var sourceCoordinateSystem = GetCoordinateSystem(sourceWkt);
        Assert.NotNull(sourceCoordinateSystem);

        var targetCoordinateSystem = GetCoordinateSystem(targetWkt);
        Assert.NotNull(targetCoordinateSystem);

        var transformation = GetTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        Assert.NotNull(transformation);
    }

    [Xunit.Fact]
    public void TestLamberTangentialConformalConicProjectionRegistryAndTransformation()
    {
        string sourceWkt = "PROJCS[\"WORLD-LM-TAN\",GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722356]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Lambert Tangential Conformal Conic Projection\"],PARAMETER[\"false_easting\",0.000],PARAMETER[\"false_northing\",0.000],PARAMETER[\"scale_factor\",1.000000000000],PARAMETER[\"central_meridian\",0.00000000000000],PARAMETER[\"latitude_of_origin\",1.00000000000000],UNIT[\"Meter\",1.00000000000000]]";
        string targetWkt = "PROJCS[\"WGS84.PseudoMercator\",GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722356]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Popular Visualisation Pseudo Mercator\"],PARAMETER[\"false_easting\",0.000],PARAMETER[\"false_northing\",0.000],PARAMETER[\"central_meridian\",0.00000000000000],UNIT[\"Meter\",1.00000000000000]]";

        var sourceCoordinateSystem = GetCoordinateSystem(sourceWkt);
        Assert.NotNull(sourceCoordinateSystem);

        var targetCoordinateSystem = GetCoordinateSystem(targetWkt);
        Assert.NotNull(targetCoordinateSystem);

        var transformation = GetTransformation(sourceCoordinateSystem, targetCoordinateSystem);
        Assert.NotNull(transformation);

        // Test the transformation with a known points. Tested with AutoCAD map 3D
        double[] pGeo = new[] { 4101119.6855, -229063.8661 }; // Nairobi, Kenya
        double[] pUtm = transformation.MathTransform.Transform(pGeo);

        double[] expected = new[] { 4098998.6422, -142387.5532 };
        Assert.IsTrue(this.ToleranceLessThan(pUtm, expected, 0.05), this.TransformationError("LambertConicConformal2SP", expected, pUtm));
    }

    internal static CoordinateSystem GetCoordinateSystem(string wkt)
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        return coordinateSystemFactory.CreateFromWkt(wkt);
    }

    internal static ICoordinateTransformation GetTransformation(CoordinateSystem sourceCoordinateSystem, CoordinateSystem targetCoordinateSystem)
    {
        var coordinateSystemFactory = new CoordinateSystemFactory();
        var coordinateService = new ProjNet.CoordinateSystemServices(coordinateSystemFactory, new CoordinateTransformationFactory());
        return coordinateService.CreateTransformation(sourceCoordinateSystem, targetCoordinateSystem);
    }
}
