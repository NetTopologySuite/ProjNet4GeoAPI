// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.WKT;

using System;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests for parsing WKT math transform definitions.
/// </summary>
public class WKTMathTransformParserTests
{
    private static readonly double[] Origin2D = [0.0, 0.0];
    private static readonly double[] AffineSamplePoint = [2040.0, 1590.0];

    /// <summary>
    /// Test parsing of affine math transform from WKT.
    /// </summary>
    [Fact]
    public void ParseAffineTransformWkt()
    {
        MathTransform mt = default!;
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
            mt = MathTransformWktReader.Parse(wkt);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Could not create affine math transformation from:\r\n{wkt}\r\n{ex.Message}");
        }

        Assert.NotNull(mt);
        Assert.NotNull(mt as AffineTransform);

        Assert.Equal(2, mt.DimSource);
        Assert.Equal(2, mt.DimTarget);

        // test simple transform
        double[] outPt = mt.Transform(Origin2D);

        Assert.Equal(2, outPt.Length);
        Assert.Equal(3455869.17937689, outPt[0], 0.00000001);
        Assert.Equal(5478710.88035753, outPt[1], 0.00000001);
    }

    /// <summary>
    /// Verifies that affine transforms now emit canonical WKT with the shared separator formatting.
    /// </summary>
    [Fact]
    public void AffineTransformWkt_UsesCanonicalSpacingAndParameterOrder()
    {
        AffineTransform transform = CreateAffineTransform();
        string expected =
            "PARAM_MT[\"Affine\", " +
            "PARAMETER[\"num_row\", 3], " +
            "PARAMETER[\"num_col\", 3], " +
            "PARAMETER[\"elt_0_0\", 0.883485346527455], " +
            "PARAMETER[\"elt_0_1\", -0.468458794848877], " +
            "PARAMETER[\"elt_0_2\", 3455869.17937689], " +
            "PARAMETER[\"elt_1_0\", 0.468458794848877], " +
            "PARAMETER[\"elt_1_1\", 0.883485346527455], " +
            "PARAMETER[\"elt_1_2\", 5478710.88035753], " +
            "PARAMETER[\"elt_2_0\", 0], " +
            "PARAMETER[\"elt_2_1\", 0], " +
            "PARAMETER[\"elt_2_2\", 1]]";

        Assert.Equal(expected, transform.WKT);
    }

    /// <summary>
    /// Verifies that affine transform WKT roundtrips through the parser without losing matrix values.
    /// </summary>
    [Fact]
    public void AffineTransformWkt_RoundTripsThroughParser()
    {
        AffineTransform original = CreateAffineTransform();

        MathTransform parsedTransform = MathTransformWktReader.Parse(original.WKT);
        AffineTransform parsed = Assert.IsType<AffineTransform>(parsedTransform);
        double[] originalResult = original.Transform(AffineSamplePoint);
        double[] parsedResult = parsed.Transform(AffineSamplePoint);

        Assert.Equal(original.WKT, parsed.WKT);
        Assert.Equal(originalResult[0], parsedResult[0], 12);
        Assert.Equal(originalResult[1], parsedResult[1], 12);
    }

    /// <summary>
    /// MathTransformWktReader parses real number with exponent incorrectly.
    /// </summary>
    /// <param name="wkt">The wkt value.</param>
    [Theory]
    [InlineData("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 6.12303176911189E-17]]")]
    [InlineData("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 5.235E4]]")]
    [InlineData("PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 5.235E+4]]")]
    public void TestMathTransformWktReaderExponencialNumberParsingIssue(string wkt)
    {
        // string wkt = "PARAM_MT[\"Affine\",PARAMETER[\"num_row\", 3],PARAMETER[\"num_col\", 3],PARAMETER[\"elt_0_0\", 6.12303176911189E-17]]";
        MathTransform mt = default!;

        try
        {
            mt = MathTransformWktReader.Parse(wkt);
        }
        catch (ArgumentException ex)
        {
            Assert.Fail($"Failed to parse WKT of affine math transformation from:\r\n{wkt}\r\n{ex.Message}");
        }
        catch (Exception e)
        {
            Assert.Fail($"Could not create affine math transformation from:\r\n{wkt}\r\n{e.Message}");
        }

        Assert.NotNull(mt);
        Assert.NotNull(mt as AffineTransform);
    }

    private static AffineTransform CreateAffineTransform()
    {
        return new AffineTransform(
            0.883485346527455,
            -0.468458794848877,
            3455869.17937689,
            0.468458794848877,
            0.883485346527455,
            5478710.88035753);
    }
}
