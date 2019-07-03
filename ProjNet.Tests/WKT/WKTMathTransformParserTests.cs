using System;
using NUnit.Framework;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.CoordinateSystems;

namespace ProjNET.Tests.WKT
{
    [TestFixture]
    public class WKTMathTransformParserTests
    {
        /// <summary>
        /// Test parsing of affine math transform from WKT
        /// </summary>
        [Test]
        public void ParseAffineTransformWkt ()
        {
            //TODO MathTransformFactory fac = new MathTransformFactory ();
            MathTransform mt = null;
            string wkt = "PARAM_MT[\"Affine\"," +
                            "PARAMETER[\"num_row\",3]," +
                            "PARAMETER[\"num_col\",3]," +
                            "PARAMETER[\"elt_0_0\", 0.883485346527455]," +
                            "PARAMETER[\"elt_0_1\", -0.468458794848877]," +
                            "PARAMETER[\"elt_0_2\", 3455869.17937689]," +
                            "PARAMETER[\"elt_1_0\", 0.468458794848877]," +
                            "PARAMETER[\"elt_1_1\", 0.883485346527455]," +
                            "PARAMETER[\"elt_1_2\", 5478710.88035753]," +
                            "PARAMETER[\"elt_2_2\", 1]]";

            try
            {
                //TODO replace with MathTransformFactory implementation
                mt = MathTransformWktReader.Parse (wkt);
            }
            catch (Exception ex)
            {
                Assert.Fail ("Could not create affine math transformation from:\r\n" + wkt + "\r\n" + ex.Message);
            }

            Assert.IsNotNull (mt);
            Assert.IsNotNull (mt as AffineTransform);

            Assert.AreEqual (2, mt.DimSource);
            Assert.AreEqual (2, mt.DimTarget);

            //test simple transform
            double[] outPt = mt.Transform (new double[] { 0.0, 0.0 });

            Assert.AreEqual (2, outPt.Length);
            Assert.AreEqual (3455869.17937689, outPt[0], 0.00000001);
            Assert.AreEqual (5478710.88035753, outPt[1], 0.00000001);
        }

        /// <summary>
        /// MathTransformWktReader parses real number with exponent incorrectly
        /// </summary>
        [TestCase("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 6.12303176911189E-17]]")]
        [TestCase("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 5.235E4]]")]
        [TestCase ("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 5.235E+4]]")]
        public void TestMathTransformWktReaderExponencialNumberParsingIssue(string wkt)
        {
            //string wkt = "PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 6.12303176911189E-17]]";
            MathTransform mt = null;

            try
            {
                //TODO replace with MathTransformFactory implementation
                mt = MathTransformWktReader.Parse (wkt);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail ("Failed to parse WKT of affine math transformation from:\r\n" + wkt + "\r\n" + ex.Message);
            }
            catch (Exception e)
            {
                Assert.Fail ("Could not create affine math transformation from:\r\n" + wkt + "\r\n" + e.Message);
            }

            Assert.IsNotNull (mt);
            Assert.IsNotNull (mt as AffineTransform);
        }
    }
}
