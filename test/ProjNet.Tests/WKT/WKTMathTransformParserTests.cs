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

namespace ProjNET.Tests.WKT
{
    using System;
    using NUnit.Framework;
    using ProjNet.CoordinateSystems.Transformations;
    using ProjNet.IO.CoordinateSystems;

    public class WKTMathTransformParserTests
    {
        /// <summary>
        /// Test parsing of affine math transform from WKT.
        /// </summary>
        [Xunit.Fact]
        public void ParseAffineTransformWkt()
        {
            // TODO MathTransformFactory fac = new MathTransformFactory ();
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
                // TODO replace with MathTransformFactory implementation
                mt = MathTransformWktReader.Parse(wkt);
            }
            catch (Exception ex)
            {
                Assert.Fail("Could not create affine math transformation from:\r\n" + wkt + "\r\n" + ex.Message);
            }

            Assert.IsNotNull(mt);
            Assert.IsNotNull(mt as AffineTransform);

            Assert.AreEqual(2, mt.DimSource);
            Assert.AreEqual(2, mt.DimTarget);

            // test simple transform
            double[] outPt = mt.Transform(new double[] { 0.0, 0.0 });

            Assert.AreEqual(2, outPt.Length);
            Assert.AreEqual(3455869.17937689, outPt[0], 0.00000001);
            Assert.AreEqual(5478710.88035753, outPt[1], 0.00000001);
        }

        /// <summary>
        /// MathTransformWktReader parses real number with exponent incorrectly.
        /// </summary>
        [Xunit.Theory]
        [Xunit.InlineData("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 6.12303176911189E-17]]")]
        [Xunit.InlineData("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 5.235E4]]")]
        [Xunit.InlineData("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 5.235E+4]]")]
        public void TestMathTransformWktReaderExponencialNumberParsingIssue(string wkt)
        {
            // string wkt = "PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 6.12303176911189E-17]]";
            MathTransform mt = null;

            try
            {
                // TODO replace with MathTransformFactory implementation
                mt = MathTransformWktReader.Parse(wkt);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail("Failed to parse WKT of affine math transformation from:\r\n" + wkt + "\r\n" + ex.Message);
            }
            catch (Exception e)
            {
                Assert.Fail("Could not create affine math transformation from:\r\n" + wkt + "\r\n" + e.Message);
            }

            Assert.IsNotNull(mt);
            Assert.IsNotNull(mt as AffineTransform);
        }
    }
}
