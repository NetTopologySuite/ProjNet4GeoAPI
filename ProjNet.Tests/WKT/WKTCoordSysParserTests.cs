using System;
using System.IO;
using System.Reflection;
using System.Text;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.UnitTests.Converters.WKT
{
    [TestFixture]
    public class WKTCoordSysParserTests
    {
        /// <summary>
        /// Parses a coordinate system WKTs
        /// </summary>
        /// <remarks><code>
        /// PROJCS["NAD83(HARN) / Texas Central (ftUS)",
        /// 	GEOGCS[
        /// 		"NAD83(HARN)",
        /// 		DATUM[
        /// 			"NAD83_High_Accuracy_Regional_Network",
        /// 			SPHEROID[
        /// 				"GRS 1980",
        /// 				6378137,
        /// 				298.257222101,
        /// 				AUTHORITY["EPSG","7019"]
        /// 			],
        ///				TOWGS84[725,685,536,0,0,0,0],
        /// 			AUTHORITY["EPSG","6152"]
        /// 		],
        /// 		PRIMEM[
        /// 			"Greenwich",
        /// 			0,
        /// 			AUTHORITY["EPSG","8901"]
        /// 		],
        /// 		UNIT[
        /// 			"degree",
        /// 			0.01745329251994328,
        /// 			AUTHORITY["EPSG","9122"]
        /// 		],
        /// 		AUTHORITY["EPSG","4152"]
        /// 	],
        /// 	PROJECTION["Lambert_Conformal_Conic_2SP"],
        /// 	PARAMETER["standard_parallel_1",31.88333333333333],
        /// 	PARAMETER["standard_parallel_2",30.11666666666667],
        /// 	PARAMETER["latitude_of_origin",29.66666666666667],
        /// 	PARAMETER["central_meridian",-100.3333333333333],
        /// 	PARAMETER["false_easting",2296583.333],
        /// 	PARAMETER["false_northing",9842500.000000002],
        /// 	UNIT[
        /// 		"US survey foot",
        /// 		0.3048006096012192,
        /// 		AUTHORITY["EPSG","9003"]
        /// 	],
        /// 	AUTHORITY["EPSG","2918"]
        /// ]
        /// </code></remarks>
        [Test]
        public void ParseCoordSys()
        {
            string wkt = "PROJCS[\"NAD83(HARN) / Texas Central (ftUS)\", GEOGCS[\"NAD83(HARN)\", DATUM[\"NAD83_High_Accuracy_Regional_Network\", SPHEROID[\"GRS 1980\", 6378137, 298.257222101, AUTHORITY[\"EPSG\", \"7019\"]], TOWGS84[725, 685, 536, 0, 0, 0, 0], AUTHORITY[\"EPSG\", \"6152\"]], PRIMEM[\"Greenwich\", 0, AUTHORITY[\"EPSG\", \"8901\"]], UNIT[\"degree\", 0.0174532925199433, AUTHORITY[\"EPSG\", \"9122\"]], AUTHORITY[\"EPSG\", \"4152\"]], UNIT[\"US survey foot\", 0.304800609601219, AUTHORITY[\"EPSG\", \"9003\"]], PROJECTION[\"Lambert_Conformal_Conic_2SP\"], PARAMETER[\"standard_parallel_1\", 31.883333333333], PARAMETER[\"standard_parallel_2\", 30.1166666667], PARAMETER[\"latitude_of_origin\", 29.6666666667], PARAMETER[\"central_meridian\", -100.333333333333], PARAMETER[\"false_easting\", 2296583.333], PARAMETER[\"false_northing\", 9842500], AUTHORITY[\"EPSG\", \"2918\"]]";
            CoordinateSystemFactory fac = new CoordinateSystemFactory();
            ProjectedCoordinateSystem pcs = fac.CreateFromWkt(wkt) as ProjectedCoordinateSystem;
            Assert.IsNotNull(pcs, "Could not parse WKT: " + wkt);

            Assert.AreEqual("NAD83(HARN) / Texas Central (ftUS)", pcs.Name);
            Assert.AreEqual("NAD83(HARN)", pcs.GeographicCoordinateSystem.Name);
            Assert.AreEqual("NAD83_High_Accuracy_Regional_Network", pcs.GeographicCoordinateSystem.HorizontalDatum.Name);
            Assert.AreEqual("GRS 1980", pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.Name);
            Assert.AreEqual(6378137, pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.SemiMajorAxis);
            Assert.AreEqual(298.257222101, pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.InverseFlattening);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.Authority);
            Assert.AreEqual(7019, pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.AuthorityCode);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.HorizontalDatum.Authority);
            Assert.AreEqual(6152, pcs.GeographicCoordinateSystem.HorizontalDatum.AuthorityCode);
            Assert.AreEqual(new Wgs84ConversionInfo(725, 685, 536, 0, 0, 0, 0), pcs.GeographicCoordinateSystem.HorizontalDatum.Wgs84Parameters);
            Assert.AreEqual("Greenwich", pcs.GeographicCoordinateSystem.PrimeMeridian.Name);
            Assert.AreEqual(0, pcs.GeographicCoordinateSystem.PrimeMeridian.Longitude);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.PrimeMeridian.Authority);
            Assert.AreEqual(8901, pcs.GeographicCoordinateSystem.PrimeMeridian.AuthorityCode, 8901);
            Assert.AreEqual("degree", pcs.GeographicCoordinateSystem.AngularUnit.Name);
            Assert.AreEqual(0.0174532925199433, pcs.GeographicCoordinateSystem.AngularUnit.RadiansPerUnit);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.AngularUnit.Authority);
            Assert.AreEqual(9122, pcs.GeographicCoordinateSystem.AngularUnit.AuthorityCode);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.Authority);
            Assert.AreEqual(4152, pcs.GeographicCoordinateSystem.AuthorityCode, 4152);
            Assert.AreEqual("Lambert_Conformal_Conic_2SP", pcs.Projection.ClassName, "Projection Classname");

            ProjectionParameter latitude_of_origin = pcs.Projection.GetParameter("latitude_of_origin");
            Assert.IsNotNull(latitude_of_origin);
            Assert.AreEqual(29.6666666667, latitude_of_origin.Value);
            ProjectionParameter central_meridian = pcs.Projection.GetParameter("central_meridian");
            Assert.IsNotNull(central_meridian);
            Assert.AreEqual(-100.333333333333, central_meridian.Value);
            ProjectionParameter standard_parallel_1 = pcs.Projection.GetParameter("standard_parallel_1");
            Assert.IsNotNull(standard_parallel_1);
            Assert.AreEqual(31.883333333333, standard_parallel_1.Value);
            ProjectionParameter standard_parallel_2 = pcs.Projection.GetParameter("standard_parallel_2");
            Assert.IsNotNull(standard_parallel_2);
            Assert.AreEqual(30.1166666667, standard_parallel_2.Value);
            ProjectionParameter false_easting = pcs.Projection.GetParameter("false_easting");
            Assert.IsNotNull(false_easting);
            Assert.AreEqual(2296583.333, false_easting.Value);
            ProjectionParameter false_northing = pcs.Projection.GetParameter("false_northing");
            Assert.IsNotNull(false_northing);
            Assert.AreEqual(9842500, false_northing.Value);

            Assert.AreEqual("US survey foot", pcs.LinearUnit.Name);
            Assert.AreEqual(0.304800609601219, pcs.LinearUnit.MetersPerUnit);
            Assert.AreEqual("EPSG", pcs.LinearUnit.Authority);
            Assert.AreEqual(9003, pcs.LinearUnit.AuthorityCode);
            Assert.AreEqual("EPSG", pcs.Authority);
            Assert.AreEqual(2918, pcs.AuthorityCode);
            Assert.AreEqual(wkt, pcs.WKT);
        }
        /// <summary>
        /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
        /// and tries to parse them.
        /// </summary>
        [Test]
        public void ParseAllWKTs()
        {
            CoordinateSystemFactory fac = new CoordinateSystemFactory();
            int parsecount = 0;
            foreach (SRIDReader.WktString wkt in SRIDReader.GetSrids())
            {
                ICoordinateSystem cs = fac.CreateFromWkt(wkt.Wkt);
                Assert.IsNotNull(cs, "Could not parse WKT: " + wkt);
                parsecount++;
            }
            Assert.AreEqual(parsecount, 2671, "Not all WKT was parsed");
        }

        /// <summary>
        /// This test reads in a file with 2671 pre-defined coordinate systems and projections,
        /// and tries to create a transformation with them.
        /// </summary>
        [Test]
        public void TestTransformAllWKTs()
        {
            //GeographicCoordinateSystem.WGS84
            CoordinateTransformationFactory fact = new CoordinateTransformationFactory();
            CoordinateSystemFactory fac = new CoordinateSystemFactory();
            int parsecount = 0;
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjNET.Tests.SRID.csv");
            using (var sr = new StreamReader(stream, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    int split = line.IndexOf(';');
                    if (split > -1)
                    {
                        string srid = line.Substring(0, split);
                        string wkt = line.Substring(split + 1);
                        ICoordinateSystem cs = fac.CreateFromWkt(wkt);
                        if (cs == null) continue; //We check this in another test.
                        if (cs is IProjectedCoordinateSystem)
                        {
                            switch ((cs as IProjectedCoordinateSystem).Projection.ClassName)
                            {
                                //Skip not supported projections
                                case "Oblique_Stereographic":
                                case "Transverse_Mercator_South_Orientated":
                                //case "Hotine_Oblique_Mercator":
                                case "Lambert_Conformal_Conic_1SP":
                                //case "Krovak":
                                //case "Cassini_Soldner":
                                case "Lambert_Azimuthal_Equal_Area":
                                case "Tunisia_Mining_Grid":
                                case "New_Zealand_Map_Grid":
                                case "Polyconic":
                                case "Lambert_Conformal_Conic_2SP_Belgium":
                                case "Polar_Stereographic":
                                    continue;
                                default: break;
                            }
                        }
                        try
                        {
                            ICoordinateTransformation trans = fact.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, cs);
                        }
                        catch (Exception ex)
                        {
                            if (cs is IProjectedCoordinateSystem)
                                Assert.Fail("Could not create transformation from:\r\n" + wkt + "\r\n" + ex.Message + "\r\nClass name:" + (cs as IProjectedCoordinateSystem).Projection.ClassName);
                            else
                                Assert.Fail("Could not create transformation from:\r\n" + wkt + "\r\n" + ex.Message);
                        }
                        parsecount++;
                    }
                }
            }
            Assert.GreaterOrEqual(parsecount, 2556, "Not all WKT was processed");
        }
        [Test]
        public void TestUnitBeforeProjection()
        {
            CoordinateSystemFactory fac = new CoordinateSystemFactory();
            string wkt = "PROJCS[\"OSGB 1936 / British National Grid\"," +
                 "GEOGCS[\"OSGB 1936\"," +
                 "DATUM[\"OSGB_1936\"," +
                     "SPHEROID[\"Airy 1830\",6377563.396,299.3249646,AUTHORITY[\"EPSG\",\"7001\"]]," +
                     "AUTHORITY[\"EPSG\",\"6277\"]]," +
                     "PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]]," +
                     "UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]]," +
                     "AUTHORITY[\"EPSG\",\"4277\"]]," +
                 "UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]]," +
                 "PROJECTION[\"Transverse_Mercator\"]," +
                 "PARAMETER[\"latitude_of_origin\",49]," +
                 "PARAMETER[\"central_meridian\",-2]," +
                 "PARAMETER[\"scale_factor\",0.9996012717]," +
                 "PARAMETER[\"false_easting\",400000]," +
                 "PARAMETER[\"false_northing\",-100000]," +
                 "AUTHORITY[\"EPSG\",\"27700\"]," +
                 "AXIS[\"Easting\",EAST]," +
                 "AXIS[\"Northing\",NORTH]]";
            ProjectedCoordinateSystem pcs = fac.CreateFromWkt(wkt) as ProjectedCoordinateSystem;

            Assert.IsNotNull(pcs);

            Assert.AreEqual("OSGB 1936 / British National Grid", pcs.Name);
            Assert.AreEqual("OSGB 1936", pcs.GeographicCoordinateSystem.Name);
            Assert.AreEqual("OSGB_1936", pcs.GeographicCoordinateSystem.HorizontalDatum.Name);
            Assert.AreEqual("Airy 1830", pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.Name);
            Assert.AreEqual(6377563.396, pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.SemiMajorAxis);
            Assert.AreEqual(299.3249646, pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.InverseFlattening);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.Authority);
            Assert.AreEqual(7001, pcs.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid.AuthorityCode);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.HorizontalDatum.Authority);
            Assert.AreEqual(6277, pcs.GeographicCoordinateSystem.HorizontalDatum.AuthorityCode);
            Assert.AreEqual("Greenwich", pcs.GeographicCoordinateSystem.PrimeMeridian.Name);
            Assert.AreEqual(0, pcs.GeographicCoordinateSystem.PrimeMeridian.Longitude);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.PrimeMeridian.Authority);
            Assert.AreEqual(8901, pcs.GeographicCoordinateSystem.PrimeMeridian.AuthorityCode, 8901);
            Assert.AreEqual("degree", pcs.GeographicCoordinateSystem.AngularUnit.Name);
            Assert.AreEqual(0.0174532925199433, pcs.GeographicCoordinateSystem.AngularUnit.RadiansPerUnit);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.AngularUnit.Authority);
            Assert.AreEqual(9122, pcs.GeographicCoordinateSystem.AngularUnit.AuthorityCode);
            Assert.AreEqual("EPSG", pcs.GeographicCoordinateSystem.Authority);
            Assert.AreEqual(4277, pcs.GeographicCoordinateSystem.AuthorityCode, 4277);

            Assert.AreEqual("Transverse_Mercator", pcs.Projection.ClassName, "Projection Classname");

            ProjectionParameter latitude_of_origin = pcs.Projection.GetParameter("latitude_of_origin");
            Assert.IsNotNull(latitude_of_origin);
            Assert.AreEqual(49, latitude_of_origin.Value);
            ProjectionParameter central_meridian = pcs.Projection.GetParameter("central_meridian");
            Assert.IsNotNull(central_meridian);
            Assert.AreEqual(-2, central_meridian.Value);
            ProjectionParameter scale_factor = pcs.Projection.GetParameter("scale_factor");
            Assert.IsNotNull(scale_factor);
            Assert.AreEqual(0.9996012717, scale_factor.Value);
            ProjectionParameter false_easting = pcs.Projection.GetParameter("false_easting");
            Assert.IsNotNull(false_easting);
            Assert.AreEqual(400000, false_easting.Value);
            ProjectionParameter false_northing = pcs.Projection.GetParameter("false_northing");
            Assert.IsNotNull(false_northing);
            Assert.AreEqual(-100000, false_northing.Value);

            Assert.AreEqual("metre", pcs.LinearUnit.Name);
            Assert.AreEqual(1, pcs.LinearUnit.MetersPerUnit);
            Assert.AreEqual("EPSG", pcs.LinearUnit.Authority);
            Assert.AreEqual(9001, pcs.LinearUnit.AuthorityCode);
            Assert.AreEqual("EPSG", pcs.Authority);
            Assert.AreEqual(27700, pcs.AuthorityCode);

            string newWkt = pcs.WKT.Replace(", ", ",");
            Assert.AreEqual(wkt, newWkt);

        }

        /// <summary>
        /// Test parsing of IFittedCoordinate system from WKT
        /// </summary>
        [Test]
        public void ParseFittedCoordinateSystemWkt ()
        {
            CoordinateSystemFactory fac = new CoordinateSystemFactory ();
            IFittedCoordinateSystem fcs = null;
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
                                   "UNIT[\"metre\", 1, AUTHORITY[\"EPSG\", \"9001\"]]," + 
                                   "PROJECTION[\"Transverse_Mercator\"]," + 
                                   "PARAMETER[\"latitude_of_origin\", 0]," + 
                                   "PARAMETER[\"central_meridian\", 9]," + 
                                   "PARAMETER[\"scale_factor\", 1]," + 
                                   "PARAMETER[\"false_easting\", 3500000]," +
                                   "PARAMETER[\"false_northing\", 0]," + 
                                   "AUTHORITY[\"EPSG\", \"31467\"]]" + 
                        "]";

            try
            { 
                fcs = fac.CreateFromWkt (wkt) as IFittedCoordinateSystem;
            }
            catch (Exception ex)
            {
                Assert.Fail ("Could not create fitted coordinate system from:\r\n" + wkt + "\r\n" + ex.Message);
            }

            Assert.That(fcs, Is.Not.Null);
            Assert.That(fcs.ToBase(), Is.Not.Null.Or.Empty);
            Assert.That(fcs.BaseCoordinateSystem, Is.Not.Null);

            Assert.AreEqual ("Local coordinate system MNAU (based on Gauss-Krueger)", fcs.Name);
            //Assert.AreEqual ("CUSTOM", fcs.Authority);
            //Assert.AreEqual (123456, fcs.AuthorityCode);

            Assert.AreEqual ("EPSG", fcs.BaseCoordinateSystem.Authority);
            Assert.AreEqual (31467, fcs.BaseCoordinateSystem.AuthorityCode);
        }
    }
}
