// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for parsing WKT math transform definitions.
/// </summary>
public class WKTMathTransformParserTests
{
    private static readonly double[] Origin2D = [0.0, 0.0];
    private static readonly double[] AffineSamplePoint = [2040.0, 1590.0];
    private static readonly double[] GeographicSamplePoint = [12.0, 45.0];
    private static readonly double[] IdentitySamplePoint = [1.0, 2.0, 3.0, 4.0];

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
    /// Verifies that map projection WKT roundtrips through the parser without losing the projection behavior.
    /// </summary>
    [Fact]
    public void MapProjectionWkt_RoundTripsThroughParser()
    {
        MapProjection original = CreateMercatorProjection();

        MathTransform parsedTransform = MathTransformWktReader.Parse(original.WKT);
        MapProjection parsed = Assert.IsAssignableFrom<MapProjection>(parsedTransform);
        double[] originalResult = original.Transform(GeographicSamplePoint);
        double[] parsedResult = parsed.Transform(GeographicSamplePoint);

        Assert.Equal(original.WKT, parsed.WKT);
        Assert.Equal(originalResult[0], parsedResult[0], 12);
        Assert.Equal(originalResult[1], parsedResult[1], 12);
    }

    /// <summary>
    /// Verifies that inverse map projection WKT roundtrips through the parser and preserves inverse behavior.
    /// </summary>
    [Fact]
    public void InverseMapProjectionWkt_RoundTripsThroughParser()
    {
        MapProjection forward = CreateMercatorProjection();
        MapProjection original = Assert.IsAssignableFrom<MapProjection>(forward.Inverse());
        double[] projectedSamplePoint = forward.Transform(GeographicSamplePoint);

        MathTransform parsedTransform = MathTransformWktReader.Parse(original.WKT);
        MapProjection parsed = Assert.IsAssignableFrom<MapProjection>(parsedTransform);
        double[] originalResult = original.Transform(projectedSamplePoint);
        double[] parsedResult = parsed.Transform(projectedSamplePoint);

        Assert.Equal(original.WKT, parsed.WKT);
        Assert.Equal(originalResult[0], parsedResult[0], 12);
        Assert.Equal(originalResult[1], parsedResult[1], 12);
    }

    /// <summary>
    /// Verifies that affine transform nodes can be imported through both the string and node readers.
    /// </summary>
    [Fact]
    public void AffineTransformNodeImport_RoundTripsThroughStringAndNodeReaders()
    {
        AffineTransform original = CreateAffineTransform();
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(original.ToWktNode());

        AffineTransform parsedFromString = Assert.IsType<AffineTransform>(MathTransformWktReader.Parse(node.ToString()));
        AffineTransform parsedFromNode = Assert.IsType<AffineTransform>(MathTransformWktReader.ReadMathTransform(node));

        AssertTransformMatchesExpected(original, parsedFromString, AffineSamplePoint);
        AssertTransformMatchesExpected(original, parsedFromNode, AffineSamplePoint);
    }

    /// <summary>
    /// Verifies that identity transform nodes can be imported through both the string and node readers.
    /// </summary>
    [Fact]
    public void IdentityTransformNodeImport_RoundTripsThroughStringAndNodeReaders()
    {
        var original = new IdentityMathTransform(IdentitySamplePoint.Length);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(original.ToWktNode());

        IdentityMathTransform parsedFromString = Assert.IsType<IdentityMathTransform>(MathTransformWktReader.Parse(node.ToString()));
        IdentityMathTransform parsedFromNode = Assert.IsType<IdentityMathTransform>(MathTransformWktReader.ReadMathTransform(node));

        AssertTransformMatchesExpected(original, parsedFromString, IdentitySamplePoint);
        AssertTransformMatchesExpected(original, parsedFromNode, IdentitySamplePoint);
    }

    /// <summary>
    /// Verifies that map projection nodes can be imported through both the string and node readers.
    /// </summary>
    [Fact]
    public void MapProjectionNodeImport_RoundTripsThroughStringAndNodeReaders()
    {
        MapProjection original = CreateMercatorProjection();
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(original.ToWktNode());

        MapProjection parsedFromString = Assert.IsAssignableFrom<MapProjection>(MathTransformWktReader.Parse(node.ToString()));
        MapProjection parsedFromNode = Assert.IsAssignableFrom<MapProjection>(MathTransformWktReader.ReadMathTransform(node));

        AssertTransformMatchesExpected(original, parsedFromString, GeographicSamplePoint);
        AssertTransformMatchesExpected(original, parsedFromNode, GeographicSamplePoint);
    }

    /// <summary>
    /// Verifies that inverse map projection nodes can be imported through both the string and node readers.
    /// </summary>
    [Fact]
    public void InverseMapProjectionNodeImport_RoundTripsThroughStringAndNodeReaders()
    {
        MapProjection forward = CreateMercatorProjection();
        MapProjection original = Assert.IsAssignableFrom<MapProjection>(forward.Inverse());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(original.ToWktNode());
        double[] projectedSamplePoint = forward.Transform(GeographicSamplePoint);

        MapProjection parsedFromString = Assert.IsAssignableFrom<MapProjection>(MathTransformWktReader.Parse(node.ToString()));
        MapProjection parsedFromNode = Assert.IsAssignableFrom<MapProjection>(MathTransformWktReader.ReadInverseMathTransform(node));

        AssertTransformMatchesExpected(original, parsedFromString, projectedSamplePoint);
        AssertTransformMatchesExpected(original, parsedFromNode, projectedSamplePoint);
    }

    /// <summary>
    /// Verifies that nested inverse map projection nodes resolve back to the forward projection for both readers.
    /// </summary>
    [Fact]
    public void NestedInverseMapProjectionNodeImport_RoundTripsThroughStringAndNodeReaders()
    {
        MapProjection original = CreateMercatorProjection();
        WktKeywordNode inverseNode = Assert.IsType<WktKeywordNode>(original.Inverse().ToWktNode());
        var nestedInverseNode = new WktKeywordNode("INVERSE_MT", inverseNode);

        MapProjection parsedFromString = Assert.IsAssignableFrom<MapProjection>(MathTransformWktReader.Parse(nestedInverseNode.ToString()));
        MapProjection parsedFromNode = Assert.IsAssignableFrom<MapProjection>(MathTransformWktReader.ReadInverseMathTransform(nestedInverseNode));

        AssertTransformMatchesExpected(original, parsedFromString, GeographicSamplePoint, assertWkt: false);
        AssertTransformMatchesExpected(original, parsedFromNode, GeographicSamplePoint, assertWkt: false);
        Assert.Equal(parsedFromString.WKT, parsedFromNode.WKT);
    }

    /// <summary>
    /// Verifies that identity transform WKT roundtrips through the parser without changing dimensionality.
    /// </summary>
    [Fact]
    public void IdentityTransformWkt_RoundTripsThroughParser()
    {
        var original = new IdentityMathTransform(IdentitySamplePoint.Length);

        MathTransform parsedTransform = MathTransformWktReader.Parse(original.WKT);
        IdentityMathTransform parsed = Assert.IsType<IdentityMathTransform>(parsedTransform);
        double[] parsedResult = parsed.Transform(IdentitySamplePoint);

        Assert.Equal(original.WKT, parsed.WKT);
        Assert.Equal(IdentitySamplePoint, parsedResult);
    }

    /// <summary>
    /// Verifies unknown top-level math transform keywords surface as parser failures.
    /// </summary>
    [Fact]
    public void ParseWithUnknownRootKeywordThrowsWktParseException()
    {
        const string wkt = """UNKNOWN_MT["Custom"]""";

        WktParseException exception = Assert.Throws<WktParseException>(() => MathTransformWktReader.Parse(wkt));

        Assert.Contains("not recognized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies malformed affine metadata is reported as a parser failure.
    /// </summary>
    [Fact]
    public void ParseAffineTransformWithInvalidRowCountThrowsWktParseException()
    {
        const string wkt = """PARAM_MT["Affine",PARAMETER["num_row",0],PARAMETER["num_col",3]]""";

        WktParseException exception = Assert.Throws<WktParseException>(() => MathTransformWktReader.Parse(wkt));

        Assert.Contains("num_row", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    private static MapProjection CreateMercatorProjection()
    {
        return Assert.IsAssignableFrom<MapProjection>(ProjectionsRegistry.CreateProjection("mercator", CreateMercatorParameters()));
    }

    private static List<ProjectionParameter> CreateMercatorParameters()
    {
        return
        [
            new("semi_major", Ellipsoid.WGS84.SemiMajorAxis),
            new("semi_minor", Ellipsoid.WGS84.SemiMinorAxis),
            new("central_meridian", 0d),
            new("latitude_of_origin", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
            new("unit", 1d),
        ];
    }

    private static void AssertTransformMatchesExpected(MathTransform expected, MathTransform actual, double[] samplePoint, bool assertWkt = true)
    {
        double[] expectedResult = expected.Transform(samplePoint);
        double[] actualResult = actual.Transform(samplePoint);

        Assert.Equal(expected.DimSource, actual.DimSource);
        Assert.Equal(expected.DimTarget, actual.DimTarget);
        if (assertWkt)
        {
            Assert.Equal(expected.WKT, actual.WKT);
        }

        Assert.Equal(expectedResult.Length, actualResult.Length);

        for (int index = 0; index < expectedResult.Length; index++)
        {
            Assert.Equal(expectedResult[index], actualResult[index], 12);
        }
    }
}
