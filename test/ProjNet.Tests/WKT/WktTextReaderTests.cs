using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using NUnit.Framework;
using NUnit.Framework.Internal.Commands;
using Pidgin;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt.Core;
using ProjNet.IO.Wkt.Tree;
using ProjNET.Tests;
using ProjNet.Wkt;

namespace ProjNet.Tests.Wkt;

public class WktTextReaderTests
{

    [Test]
    public void TestGeogCsParser()
    {
        // Arrange
        string parseText01 = "GEOGCS[\"NAD83(HARN)\", \n" +
                             "DATUM[\"NAD83_High_Accuracy_Regional_Network\", \n" +
                             "SPHEROID[\"GRS 1980\", 6378137, 298.257222101, AUTHORITY[\"EPSG\", \"7019\"]], \n" +
                             "TOWGS84[725, 685, 536, 0, 0, 0, 0], AUTHORITY[\"EPSG\", \"6152\"]], \n" +
                             "PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], \n" +
                             "UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]], \n" +
                             "AUTHORITY[\"EPSG\", \"4152\"]]";

        // Act
        var presult = builder.Parse(parseText01);
        var cs = (GeographicCoordinateSystem) presult.Value;

        // Assert
        Assert.That(cs.Name, Is.EqualTo("NAD83(HARN)"));
    }


    [Test]
    public void TestCoordinateSystem_EPSG_2918()
    {
        string parseText01 =
            "PROJCS[\"NAD83(HARN) / Texas Central (ftUS)\", \n" +
            "GEOGCS[\"NAD83(HARN)\", \n" +
            "DATUM[\"NAD83_High_Accuracy_Regional_Network\", \n" +
            "SPHEROID[\"GRS 1980\", 6378137, 298.257222101, AUTHORITY[\"EPSG\", \"7019\"]], \n" +
            "TOWGS84[725, 685, 536, 0, 0, 0, 0], AUTHORITY[\"EPSG\", \"6152\"]], \n" +
            "PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], \n" +
            "UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]], \n" +
            "AUTHORITY[\"EPSG\", \"4152\"]], \n" +
            "PROJECTION[\"Lambert_Conformal_Conic_2SP\"], \n" +
            "PARAMETER[\"standard_parallel_1\", 31.883333333333], \n" +
            "PARAMETER[\"standard_parallel_2\", 30.1166666667], \n" +
            "PARAMETER[\"latitude_of_origin\", 29.6666666667], \n" +
            "PARAMETER[\"central_meridian\", -100.333333333333], \n" +
            "PARAMETER[\"false_easting\", 2296583.333], \n" +
            "PARAMETER[\"false_northing\", 9842500], \n" +
            "UNIT[\"US survey foot\", 0.304800609601219, AUTHORITY[\"EPSG\", \"9003\"]], \n" +
            "AUTHORITY[\"EPSG\", \"2918\"]]";

        var presult = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(presult);
        Assert.That(presult.Success, Is.True);

        var result = (ProjectedCoordinateSystem)presult.Value;
        Assert.NotNull(result);
        Assert.That(result.Name, Is.EqualTo("NAD83(HARN) / Texas Central (ftUS)"));

        Assert.That(result.LinearUnit.Name, Is.EqualTo("US survey foot"));
        Assert.That(result.LinearUnit.MetersPerUnit, Is.EqualTo(0.304800609601219));
        Assert.That(result.LinearUnit.Authority, Is.EqualTo("EPSG"));
        Assert.That(result.LinearUnit.AuthorityCode, Is.EqualTo(9003));

        var parameter = result.Projection.GetParameter("central_meridian");
        Assert.That(parameter.Name, Is.EqualTo("central_meridian"));
        Assert.That(parameter.Value, Is.EqualTo(-100.333333333333));

        Assert.That(result.Authority, Is.EqualTo("EPSG"));
        Assert.That(result.AuthorityCode, Is.EqualTo(2918));
    }


    [Test]
    public void TestCoordinateSystem_EPSG_3067()
    {
        string parseText01 =
            "PROJCS[\"ETRS89 / TM35FIN(E,N)\"," +
            "   GEOGCS[\"ETRS89\"," +
            "       DATUM[\"European_Terrestrial_Reference_System_1989\"," +
            "           SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]]," +
            "           TOWGS84[0,0,0,0,0,0,0]," +
            "           AUTHORITY[\"EPSG\",\"6258\"]]," +
            "       PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]]," +
            "       UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]]," +
            "       AUTHORITY[\"EPSG\",\"4258\"]]," +
            "   PROJECTION[\"Transverse_Mercator\"]," +
            "   PARAMETER[\"latitude_of_origin\",0]," +
            "   PARAMETER[\"central_meridian\",27]," +
            "   PARAMETER[\"scale_factor\",0.9996]," +
            "   PARAMETER[\"false_easting\",500000]," +
            "   PARAMETER[\"false_northing\",0]," +
            "   UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]]," +
            "   AXIS[\"Easting\",EAST]," +
            "   AXIS[\"Northing\",NORTH]," +
            "   AUTHORITY[\"EPSG\",\"3067\"]]";

        var presult = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(presult);
        Assert.That(presult.Success, Is.True);
    }


    [Test]
    public void TestCoordinateSystem_EPSG_3822()
    {
        string parseText01 =
            "GEOCCS[\"TWD97\",\n" +
            "   DATUM[\"Taiwan_Datum_1997\",\n" +
            "       SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],\n" +
            "       AUTHORITY[\"EPSG\",\"1026\"]],\n" +
            "   PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],\n" +
            "   UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],\n" +
            "   AXIS[\"Geocentric X\",OTHER],\n" +
            "   AXIS[\"Geocentric Y\",OTHER],\n" +
            "   AXIS[\"Geocentric Z\",NORTH],\n" +
            "   AUTHORITY[\"EPSG\",\"3822\"]]";

        var result = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(result);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void TestCoordinateSystem_EPSG_8351()
    {
        string parseText01 =
            "GEOGCS[\"S-JTSK [JTSK03]\",DATUM[\"System_of_the_Unified_Trigonometrical_Cadastral_Network_JTSK03\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],TOWGS84[485.021,169.465,483.839,7.786342,4.397554,4.102655,0],AUTHORITY[\"EPSG\",\"1201\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AXIS[\"Latitude\",NORTH],AXIS[\"Longitude\",EAST],AUTHORITY[\"EPSG\",\"8351\"]]";

        var result = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(result);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void TestCoordinateSystem_EPSG_32161()
    {
        string parseText01 =
            "PROJCS[\"NAD83 / Puerto Rico & Virgin Is.\",\n" +
            "   GEOGCS[\"NAD83\",\n" +
            "       DATUM[\"North_American_Datum_1983\",\n" +
            "           SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],\n" +
            "           TOWGS84[0,0,0,0,0,0,0],\n" +
            "           AUTHORITY[\"EPSG\",\"6269\"]],\n" +
            "       PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],\n" +
            "       UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],\n" +
            "       AUTHORITY[\"EPSG\",\"4269\"]],\n" +
            "   PROJECTION[\"Lambert_Conformal_Conic_2SP\"],\n" +
            "   PARAMETER[\"standard_parallel_1\",18.43333333333333],\n" +
            "   PARAMETER[\"standard_parallel_2\",18.03333333333333],\n" +
            "   PARAMETER[\"latitude_of_origin\",17.83333333333333],\n" +
            "   PARAMETER[\"central_meridian\",-66.43333333333334],\n" +
            "   PARAMETER[\"false_easting\",200000],\n" +
            "   PARAMETER[\"false_northing\",200000],\n" +
            "   UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],\n" +
            "   AXIS[\"X\",EAST],\n" +
            "   AXIS[\"Y\",NORTH],\n" +
            "   AUTHORITY[\"EPSG\",\"32161\"]]";

        var result = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(result);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void TestCoordinateSystem_EPSG_102100()
    {
        string parseText01 =
            "PROJCS[\"WGS_1984_Web_Mercator_Auxiliary_Sphere (deprecated)\",\n" +
            "   GEOGCS[\"WGS 84\",\n" +
            "       DATUM[\"WGS_1984\",\n" +
            "           SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],\n" +
            "           AUTHORITY[\"EPSG\",\"6326\"]],\n" +
            "   PRIMEM[\"Greenwich\",0],\n" +
            "   UNIT[\"Degree\",0.0174532925199433]],\n" +
            "   PROJECTION[\"Mercator_1SP\"],\n" +
            "   PARAMETER[\"central_meridian\",0],\n" +
            "PARAMETER[\"scale_factor\",1],\n" +
            "PARAMETER[\"false_easting\",0],\n" +
            "PARAMETER[\"false_northing\",0],\n" +
            "UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],\n" +
            "AXIS[\"Easting\",EAST],\n" +
            "AXIS[\"Northing\",NORTH],\n" +
            "EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +k=1 +units=m +nadgrids=@null +wktext +no_defs\"],\n" +
            "AUTHORITY[\"ESRI\",\"102100\"]]";

        var result = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(result);
    }

    [Test]
    public void TestCoordinateSystem_ESRI_4305()
    {
        string parseText01 = "GEOGCS[\"GCS_Voirol_Unifie_1960 (deprecated)\",\n" +
                             "  DATUM[\"D_Voirol_Unifie_1960\",\n" +
                             "      SPHEROID[\"Clarke 1880 (RGS)\",6378249.145,293.465,AUTHORITY[\"EPSG\",\"7012\"]],\n" +
                             "      AUTHORITY[\"ESRI\",\"106011\"]],\n" +
                             "  PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],\n" +
                             "  UNIT[\"grad\",0.0157079632679489,AUTHORITY[\"EPSG\",\"9105\"]],\n" +
                             "  AXIS[\"Latitude\",NORTH],\n" +
                             "  AXIS[\"Longitude\",EAST],\n" +
                             "  AUTHORITY[\"ESRI\",\"4305\"]]";

        // Act
        var result = builder.Parse(parseText01);

        // Assert
        Assert.NotNull(result);
    }

    /*
    [Test]
    public void ParseAllWKTs()
    {
        int parseCount = 0;
        foreach (var wkt in SRIDReader.GetSrids())
        {
            // Parse in from CSV...
            using var sr = new StringReader(wkt.Wkt);
            using var reader = new WktToProjTextReader(sr);
            var result01 = reader.ReadToEnd();
            Assert.That(result01.Success, Is.True);

            var cs01 = result01.Value;
            Assert.IsNotNull(cs01, "Could not parse WKT: " + wkt.Wkt);

            // Generate WKT string from WktObject...
            var formatter = new DefaultWktOutputFormatter(
                newline: Environment.NewLine,
                leftDelimiter: '(', rightDelimiter: ')',
                indent: "\t", extraWhitespace: " ");
            string wkt01 = cs01.ToString(formatter);

            Assert.IsFalse(string.IsNullOrWhiteSpace(wkt01));

            // Reparse the formatted WKT for extra testing...
            using var sr02 = new StringReader(wkt01);
            using var reader02 = new WktToProjTextReader(sr02);
            var result02 = reader02.ReadToEnd();

            WktCoordinateSystem cs02 = null;
            if (!result02.Success)
            {
                Console.WriteLine($"Original: {wkt.Wkt}");
                Console.WriteLine($"Parsed 1: {wkt01}");
                Assert.Fail(result02.Error.RenderErrorMessage());
            }

            cs02 = result02.GetValueOrDefault();

            // Comparing whole tree using IEquatable.Equals(...)
            if (!cs01.Equals(cs02))
            {
                Console.Error.WriteLine($"Original: {wkt.Wkt}");
                Console.Error.WriteLine($"Parsed 1: {wkt01}");
                Console.Error.WriteLine($"Parsed 2: {cs02}");
                Assert.Fail("Error comparing cs01 and cs02. See beneath...");
            }

            parseCount++;
        }
        Assert.That(parseCount, Is.GreaterThan(2671), "Not all WKT was parsed");
    }
    */


    /// <summary>
    /// Test parsing of a <see cref="ProjectedCoordinateSystem"/> from WKT
    /// </summary>
    [Test]
    public void TestCoordinateSystem_EPSG_27700_UnitBeforeProjection()
    {
        Assert.Ignore("Partial match isn't supported yet in new parser!");
        const string wkt = "PROJCS[\"OSGB 1936 / British National Grid\",\n" +
                           "    GEOGCS[\"OSGB 1936\",\n" +
                           "        DATUM[\"OSGB_1936\",\n" +
                           "            SPHEROID[\"Airy 1830\",6377563.396,299.3249646,AUTHORITY[\"EPSG\",\"7001\"]],\n" +
                           "            AUTHORITY[\"EPSG\",\"6277\"]],\n" +
                           "        PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],\n" +
                           "        UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],\n" +
                           "        AUTHORITY[\"EPSG\",\"4277\"]],\n" +
                           "        PROJECTION[\"Transverse_Mercator\"],\n" +
                           "        PARAMETER[\"latitude_of_origin\",49],\n" +
                           "        PARAMETER[\"central_meridian\",-2],\n" +
                           "        PARAMETER[\"scale_factor\",0.9996012717],\n" +
                           "        PARAMETER[\"false_easting\",400000],\n" +
                           "        PARAMETER[\"false_northing\",-100000],\n" +
                           "        UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],\n" +
                           "        AXIS[\"Easting\",EAST],\n" +
                           "        AXIS[\"Northing\",NORTH],\n" +
                           "        AUTHORITY[\"EPSG\",\"27700\"]]";

        WktProjectedCoordinateSystem pcs = null;
        var result01 = builder.Parse(wkt);
        Assert.IsTrue(result01.Success, !result01.Success ? result01.Error?.RenderErrorMessage() : "Error!");



        Assert.That(pcs.Name, Is.EqualTo("OSGB 1936 / British National Grid"));
        Assert.That(pcs.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(pcs.Authority.Code, Is.EqualTo(27700));

        var gcs = pcs.GeographicCoordinateSystem;
        Assert.That(gcs.Name, Is.EqualTo("OSGB 1936"));
        Assert.That(gcs.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(gcs.Authority.Code, Is.EqualTo(4277));

        //CheckDatum(gcs.HorizontalDatum, "OSGB_1936", "EPSG", 6277);
        Assert.That(gcs.HorizontalDatum.Name, Is.EqualTo("OSGB_1936"));
        Assert.That(gcs.HorizontalDatum.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(gcs.HorizontalDatum.Authority.Code, Is.EqualTo(6277));

        //CheckEllipsoid(gcs.HorizontalDatum.Ellipsoid, "Airy 1830", 6377563.396, 299.3249646, "EPSG", 7001);
        Assert.That(gcs.HorizontalDatum.Spheroid.Name, Is.EqualTo("Airy 1830"));
        Assert.That(gcs.HorizontalDatum.Spheroid.SemiMajorAxis, Is.EqualTo(6377563.396));
        Assert.That(gcs.HorizontalDatum.Spheroid.InverseFlattening, Is.EqualTo(299.3249646));
        Assert.That(gcs.HorizontalDatum.Spheroid.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(gcs.HorizontalDatum.Spheroid.Authority.Code, Is.EqualTo(7001));

        //CheckPrimem(gcs.PrimeMeridian, "Greenwich", 0, "EPSG", 8901);
        Assert.That(gcs.PrimeMeridian.Name, Is.EqualTo("Greenwich"));
        Assert.That(gcs.PrimeMeridian.Longitude, Is.EqualTo(0));
        Assert.That(gcs.PrimeMeridian.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(gcs.PrimeMeridian.Authority.Code, Is.EqualTo(8901));

        //CheckUnit(gcs.AngularUnit, "degree", 0.0174532925199433, "EPSG", 9122);
        Assert.That(gcs.AngularUnit.Name, Is.EqualTo("degree"));
        Assert.That(gcs.AngularUnit.ConversionFactor, Is.EqualTo(0.0174532925199433));
        Assert.That(gcs.AngularUnit.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(gcs.AngularUnit.Authority.Code, Is.EqualTo(9122));

        //Assert.AreEqual("Transverse_Mercator", pcs.Projection.ClassName, "Projection Classname");
        Assert.That(pcs.Projection.Name, Is.EqualTo("Transverse_Mercator"));
        //Assert.AreEqual("Projection Classname", pcs.Projection.Name); // <= Wkt related?

       /*
        CheckProjection(pcs.Projection, "Transverse_Mercator", new[]
        {
            Tuple.Create("latitude_of_origin", 49d),
            Tuple.Create("central_meridian", -2d),
            Tuple.Create("scale_factor", 0.9996012717),
            Tuple.Create("false_easting", 400000d),
            Tuple.Create("false_northing", -100000d)
        });
        */

        //CheckUnit(pcs.LinearUnit, "metre", 1d, "EPSG", 9001);
        Assert.That(pcs.Unit.Name, Is.EqualTo("metre"));
        Assert.That(pcs.Unit.ConversionFactor, Is.EqualTo(1d));
        Assert.That(pcs.Unit.Authority.Name, Is.EqualTo("EPSG"));
        Assert.That(pcs.Unit.Authority.Code, Is.EqualTo(9001));

        //string newWkt = pcs.WKT.Replace(", ", ",");
        //Assert.AreEqual(wkt, newWkt);
    }


    [Test]
    public void TestCoordinateSystem_EPSG_28992()
    {
        // Arrange
        string text = @"PROJCS[""Amersfoort / RD New"", GEOGCS[""Amersfoort"",
    DATUM[""Amersfoort"",SPHEROID[""Bessel 1841"", 6377397.155, 299.1528128, AUTHORITY[""EPSG"",""7004""]],
      TOWGS84[565.2369, 50.0087, 465.658, -0.40685733032239757, -0.3507326765425626, 1.8703473836067956, 4.0812],
      AUTHORITY[""EPSG"",""6289""]],
    PRIMEM[""Greenwich"", 0.0, AUTHORITY[""EPSG"",""8901""]],
    UNIT[""degree"", 0.017453292519943295],
    AXIS[""Geodetic latitude"", NORTH],
    AXIS[""Geodetic longitude"", EAST],
    AUTHORITY[""EPSG"",""4289""]],
  PROJECTION[""Oblique_Stereographic"", AUTHORITY[""EPSG"",""9809""]],
  PARAMETER[""central_meridian"", 5.387638888888891],
  PARAMETER[""latitude_of_origin"", 52.15616055555556],
  PARAMETER[""scale_factor"", 0.9999079],
  PARAMETER[""false_easting"", 155000.0],
  PARAMETER[""false_northing"", 463000.0],
  UNIT[""m"", 1.0],
  AXIS[""Easting"", EAST],
  AXIS[""Northing"", NORTH],
  AUTHORITY[""EPSG"",""28992""]]";

        // Act
        var resultCs = builder.Parse(text);

        // Assert
        Assert.NotNull(resultCs);
        Assert.IsTrue(resultCs.Success);//, resultCs.Error.RenderErrorMessage());
    }


    [Test]
    public void TestParseSrOrg()
    {
        // Arrange
        string text = "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"Popular Visualisation CRS\"," +
                      "DATUM[\"Popular_Visualisation_Datum\",SPHEROID[\"Popular Visualisation Sphere\"," +
                      "6378137,0,AUTHORITY[\"EPSG\",\"7059\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\"," +
                      "\"6055\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\"," +
                      "0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4055\"]]," +
                      "PROJECTION[\"Mercator_1SP\"]," +
                      "PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[" +
                      "\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]],AUTHORITY[\"EPSG\",\"3785\"]"
            ;
        // Act
        var resultCs = builder.Parse(text);

        // Assert
        Assert.NotNull(resultCs);
        Assert.IsTrue(resultCs.Success);
    }


    [Test]
    public void TestProjNetIssues()
    {
        // Arrange
        string text =  "PROJCS[\"International_Terrestrial_Reference_Frame_1992Lambert_Conformal_Conic_2SP\"," +
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
                       "UNIT[\"Meter\",1,AUTHORITY[\"EPSG\",\"9001\"]]]";

        // Act
        var resultCs = builder.Parse(text);

        // Assert
        Assert.NotNull(resultCs);
        Assert.IsTrue(resultCs.Success);


        text = "PROJCS[\"Google Maps Global Mercator\",\n" +
               "GEOGCS[\"WGS 84\",\n" +
               "DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],\n" +
               "AUTHORITY[\"EPSG\",\"6326\"]],\n" +
               "PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],\n" +
               "UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],\n" +
               "AUTHORITY[\"EPSG\",\"4326\"]],\n" +
               "PROJECTION[\"Mercator_2SP\"],\n" +
               "PARAMETER[\"standard_parallel_1\",0],\n" +
               "PARAMETER[\"latitude_of_origin\",0],\n" +
               "PARAMETER[\"central_meridian\",0],\n" +
               "PARAMETER[\"false_easting\",0],\n" +
               "PARAMETER[\"false_northing\",0],\n" +
               "UNIT[\"Meter\",1],\n" +
               "EXTENSION[\"PROJ4\",\"+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs\"],\n" +
               "AUTHORITY[\"EPSG\",\"900913\"]]";

        // Act
        var resultCs2 = builder.Parse(text);

        // Assert
        Assert.NotNull(resultCs2);
        Assert.IsTrue(resultCs2.Success);
    }

    [Test]
    public void TestFittedCoordinateSystemWkt()
    {
        //Assert.Ignore("Is FITTED_CS Support still needed?");

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


        var resultCs = builder.Parse(wkt);


        // Assert
        Assert.NotNull(resultCs);
        Assert.That(resultCs.Success, Is.True);
    }


    /*
    [Test]
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


        using var sr = new StringReader(wkt);
        using var reader = new WktToProjTextReader(sr);

        // Act
        var resultCs = reader.ReadToEnd();

        // Assert
        Assert.NotNull(resultCs);
        Assert.IsTrue(resultCs.Success);


        // Convert to Proj
        var converter = new WktToProjConverter(fac);
        var projCs = converter.Convert(resultCs.Value) as GeocentricCoordinateSystem;

        Assert.NotNull(projCs);
        Assert.AreEqual("TUREF", projCs.Name);
        Assert.AreEqual("EPSG", projCs.Authority);
        Assert.AreEqual(5250, projCs.AuthorityCode);

        Assert.AreEqual("Turkish_National_Reference_Frame", projCs.HorizontalDatum.Name);
        Assert.AreEqual("EPSG", projCs.HorizontalDatum.Authority);
        Assert.AreEqual(1057, projCs.HorizontalDatum.AuthorityCode);

    }
*/


    [Test]
    public void ParseWktCreatedByCoordinateSystem()
    {
        Assert.Ignore("CreateHorizontalDatum contains a check on String.IsNullOrWhitespace for the name and throws....????");
        // Sample WKT from an external source.
        string sampleWkt =
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

        // Act
        var resultCs = builder.Parse(sampleWkt);

        // Assert
        Assert.NotNull(resultCs);
        Assert.IsTrue(resultCs.Success);
    }

    [Test]
    public void TestAxisPresent()
    {
        string parseText = "PROJCS[\"CH1903+ / LV95\",\n" +
                           "    GEOGCS[\"CH1903+\",\n" +
                           "        DATUM[\"CH1903+\",\n" +
                           "            SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],\n" +
                           "            TOWGS84[674.374,15.056,405.346,0,0,0,0],AUTHORITY[\"EPSG\",\"6150\"]],\n" +
                           "            PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],\n" +
                           "            UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],\n" +
                           "            AUTHORITY[\"EPSG\",\"4150\"]],\n" +
                           "        PROJECTION[\"Hotine_Oblique_Mercator_Azimuth_Center\"],\n" +
                           "        PARAMETER[\"latitude_of_center\",46.95240555555556],\n" +
                           "        PARAMETER[\"longitude_of_center\",7.439583333333333],\n" +
                           "        PARAMETER[\"azimuth\",90],\n" +
                           "        PARAMETER[\"rectified_grid_angle\",90],\n" +
                           "        PARAMETER[\"scale_factor\",1],\n" +
                           "        PARAMETER[\"false_easting\",2600000],\n" +
                           "        PARAMETER[\"false_northing\",1200000],\n" +
                           "        UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],\n" +
                           "        AXIS[\"Easting\",EAST],\n" +
                           "        AXIS[\"Northing\",NORTH],\n" +
                           "    AUTHORITY[\"EPSG\",\"2056\"]]";

        // Act
        var resultCs = builder.Parse(parseText);

        // Assert
        Assert.NotNull(resultCs);
        Assert.IsTrue(resultCs.Success);
    }


    readonly private CoordinateSystemFactory _coordinateSystemFactory = new CoordinateSystemFactory();

    /// <summary>
    /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
    /// and tries to parse them.
    /// </summary>
    [Test]
    public void ParseAllWKTs_OldParser()
    {
        int parseCount = 0;
        foreach (var wkt in SRIDReader.GetSrids())
        {
            var cs1 = _coordinateSystemFactory.CreateFromWkt(wkt.Wkt);
            Assert.IsNotNull(cs1, "Could not parse WKT: " + wkt);
            var cs2 = _coordinateSystemFactory.CreateFromWkt(wkt.Wkt.Replace("[", "(").Replace("]", ")"));
            Assert.That(cs1.EqualParams(cs2), Is.True);
            parseCount++;
        }
        Assert.That(parseCount, Is.GreaterThan(2671), "Not all WKT was parsed");
    }


    private readonly WktToProjBuilder builder = new WktToProjBuilder();

    private readonly WktParser.Context ctx = new WktParser.Context {builder = new WktToProjBuilder()};

    [Test]
    public void ParseAllWKTs_PidginParser()
    {
        int parseCount = 0;
        foreach (var wkt in SRIDReader.GetSrids())
        {
            //var cs1 = _coordinateSystemFactory.CreateFromWkt(wkt.Wkt);
            //using var sr = new StringReader(wkt.Wkt);
            var presult = WktParser.SpatialReferenceSystemParser(ctx).Parse(wkt.Wkt);
            Assert.That(presult.Success, Is.True);
            var cs1 = (CoordinateSystem) presult.Value;
            Assert.IsNotNull(cs1, "Could not parse WKT: " + wkt);

            //var presult2 = builder.Parse(wkt.Wkt.Replace("[", "(").Replace("]", ")"));
            var presult2 = WktParser.SpatialReferenceSystemParser(ctx).Parse(wkt.Wkt.Replace("[", "(").Replace("]", ")"));
            Assert.That(presult2.Success, Is.True);
            var cs2 = (CoordinateSystem) presult2.Value;

            Assert.That(cs1.EqualParams(cs2), Is.True);
            parseCount++;
        }
        Assert.That(parseCount, Is.GreaterThan(2671), "Not all WKT was parsed");
    }
}
