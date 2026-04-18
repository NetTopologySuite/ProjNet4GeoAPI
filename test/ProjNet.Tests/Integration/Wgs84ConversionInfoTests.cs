// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Verifies span-based affine coefficient access on <see cref="Wgs84ConversionInfo"/>.
/// </summary>
public class Wgs84ConversionInfoTests
{
    /// <summary>
    /// Verifies that the default constructor creates an all-zero conversion.
    /// </summary>
    [Fact]
    public void DefaultConstructor_CreatesZeroConversion()
    {
        var info = new Wgs84ConversionInfo();

        Assert.Equal(0d, info.Dx);
        Assert.Equal(0d, info.Dy);
        Assert.Equal(0d, info.Dz);
        Assert.Equal(0d, info.Ex);
        Assert.Equal(0d, info.Ey);
        Assert.Equal(0d, info.Ez);
        Assert.Equal(0d, info.Ppm);
        Assert.Equal(string.Empty, info.AreaOfUse);
    }

    /// <summary>
    /// Verifies that the constructor stores the provided values.
    /// </summary>
    [Fact]
    public void Constructor_SetsAllFields()
    {
        var info = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d, "Europe");

        Assert.Equal(1d, info.Dx);
        Assert.Equal(2d, info.Dy);
        Assert.Equal(3d, info.Dz);
        Assert.Equal(4d, info.Ex);
        Assert.Equal(5d, info.Ey);
        Assert.Equal(6d, info.Ez);
        Assert.Equal(7d, info.Ppm);
        Assert.Equal("Europe", info.AreaOfUse);
    }

    /// <summary>
    /// Verifies that the zero-values property is true only when all seven parameters are zero.
    /// </summary>
    [Fact]
    public void HasZeroValuesOnly_ReflectsAllParameters()
    {
        Assert.True(new Wgs84ConversionInfo().HasZeroValuesOnly);
        Assert.False(new Wgs84ConversionInfo(0d, 0d, 0d, 0d, 0d, 0d, 0.1d).HasZeroValuesOnly);
    }

    /// <summary>
    /// Verifies that each individual Bursa-Wolf parameter makes the zero-values property false.
    /// </summary>
    /// <param name="dx">Test X shift.</param>
    /// <param name="dy">Test Y shift.</param>
    /// <param name="dz">Test Z shift.</param>
    /// <param name="ex">Test X rotation.</param>
    /// <param name="ey">Test Y rotation.</param>
    /// <param name="ez">Test Z rotation.</param>
    /// <param name="ppm">Test ppm scale.</param>
    [Theory]
    [InlineData(1d, 0d, 0d, 0d, 0d, 0d, 0d)]
    [InlineData(0d, 1d, 0d, 0d, 0d, 0d, 0d)]
    [InlineData(0d, 0d, 1d, 0d, 0d, 0d, 0d)]
    [InlineData(0d, 0d, 0d, 1d, 0d, 0d, 0d)]
    [InlineData(0d, 0d, 0d, 0d, 1d, 0d, 0d)]
    [InlineData(0d, 0d, 0d, 0d, 0d, 1d, 0d)]
    public void HasZeroValuesOnly_WhenAnySingleParameterIsNonZero_ReturnsFalse(
        double dx,
        double dy,
        double dz,
        double ex,
        double ey,
        double ez,
        double ppm)
    {
        var info = new Wgs84ConversionInfo(dx, dy, dz, ex, ey, ez, ppm);

        Assert.False(info.HasZeroValuesOnly);
    }

    /// <summary>
    /// Verifies that WKT formats all seven Bursa-Wolf parameters.
    /// </summary>
    [Fact]
    public void WKT_FormatsExpectedValue()
    {
        var info = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d);

        Assert.Equal("TOWGS84[1, 2, 3, 4, 5, 6, 7]", info.WKT);
        Assert.Equal(info.WKT, info.ToString());
    }

    /// <summary>
    /// Verifies that XML contains all expected attributes.
    /// </summary>
    [Fact]
    public void XML_ContainsExpectedStructure()
    {
        var info = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d);
        var xml = XElement.Parse(info.XML);

        Assert.Equal("CS_WGS84ConversionInfo", xml.Name.LocalName);
        Assert.Equal("1", (string?)xml.Attribute("Dx"));
        Assert.Equal("2", (string?)xml.Attribute("Dy"));
        Assert.Equal("3", (string?)xml.Attribute("Dz"));
        Assert.Equal("4", (string?)xml.Attribute("Ex"));
        Assert.Equal("5", (string?)xml.Attribute("Ey"));
        Assert.Equal("6", (string?)xml.Attribute("Ez"));
        Assert.Equal("7", (string?)xml.Attribute("Ppm"));
    }

    /// <summary>
    /// Verifies that <see cref="Wgs84ConversionInfo.ToXml"/> matches the XML property for non-zero values.
    /// </summary>
    [Fact]
    public void ToXml_WithNonZeroValues_MatchesXmlProperty()
    {
        var info = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d);
        XElement element = info.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(info.XML), element));
    }

    /// <summary>
    /// Verifies that <see cref="Wgs84ConversionInfo.ToWktNode()"/> matches the WKT property.
    /// </summary>
    [Fact]
    public void ToWktNode_MatchesWkt()
    {
        var info = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(info.ToWktNode());

        Assert.Equal("TOWGS84", node.Keyword);
        Assert.Equal(info.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that span-based affine transform output matches the array-based API.
    /// </summary>
    [Fact]
    public void WriteAffineTransformMatchesArrayBasedResult()
    {
        var info = new Wgs84ConversionInfo(570.8, 85.7, 462.8, 4.998, 1.587, 5.261, 3.56);

        Span<double> destination = stackalloc double[7];
        info.WriteAffineTransform(destination);

        double[] expected = info.GetAffineTransform();
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], destination[i], 12);
        }
    }

    /// <summary>
    /// Verifies that too-small destination spans are rejected.
    /// </summary>
    [Fact]
    public void WriteAffineTransformWithSmallDestinationThrows()
    {
        var info = new Wgs84ConversionInfo();
        Span<double> destination = stackalloc double[6];

        ArgumentException exception = default!;
        try
        {
            info.WriteAffineTransform(destination);
        }
        catch (ArgumentException ex)
        {
            exception = ex;
        }

        Assert.NotNull(exception);
        Assert.Equal("destination", exception.ParamName);
    }

    /// <summary>
    /// Verifies that writing affine coefficients updates only the required 7 destination elements.
    /// </summary>
    [Fact]
    public void WriteAffineTransformWithLargerDestinationPreservesTrailingValues()
    {
        var info = new Wgs84ConversionInfo(1.0, 2.0, 3.0, 0.1, 0.2, 0.3, 0.4);
        Span<double> destination = stackalloc double[9];
        destination[0] = -1.0;
        destination[1] = -1.0;
        destination[2] = -1.0;
        destination[3] = -1.0;
        destination[4] = -1.0;
        destination[5] = -1.0;
        destination[6] = -1.0;
        destination[7] = 1234.5;
        destination[8] = -9876.5;

        info.WriteAffineTransform(destination);
        double[] expected = info.GetAffineTransform();

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], destination[i], 12);
        }

        Assert.Equal(1234.5, destination[7], 12);
        Assert.Equal(-9876.5, destination[8], 12);
    }

    /// <summary>
    /// Verifies that the array-based affine transform returns the expected coefficients.
    /// </summary>
    [Fact]
    public void GetAffineTransform_ReturnsExpectedCoefficients()
    {
        var info = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d);

        double[] affine = info.GetAffineTransform();

        Assert.Equal(7, affine.Length);
        Assert.Equal(1.000007d, affine[0], 12);
        Assert.Equal(4d * 4.84813681109535993589914102357e-6, affine[1], 15);
        Assert.Equal(5d * 4.84813681109535993589914102357e-6, affine[2], 15);
        Assert.Equal(6d * 4.84813681109535993589914102357e-6, affine[3], 15);
        Assert.Equal(1d, affine[4], 12);
        Assert.Equal(2d, affine[5], 12);
        Assert.Equal(3d, affine[6], 12);
    }

    /// <summary>
    /// Verifies that equal conversions compare equal and share a hash code.
    /// </summary>
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var first = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d, "A");
        var second = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d, "B");

        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that different parameter values compare unequal.
    /// </summary>
    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var first = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 7d);
        var second = new Wgs84ConversionInfo(1d, 2d, 3d, 4d, 5d, 6d, 8d);

        Assert.False(first.Equals(second));
        Assert.False(first.Equals((object?)second));
    }

    /// <summary>
    /// Verifies that null and other object types compare unequal.
    /// </summary>
    [Fact]
    public void Equals_NullOrDifferentType_ReturnsFalse()
    {
        var info = new Wgs84ConversionInfo();

        Assert.False(EqualsNullable(info, null));
        Assert.False(info.Equals("not conversion info"));
    }

    private static bool EqualsNullable(Wgs84ConversionInfo left, Wgs84ConversionInfo? right)
    {
        return left.Equals(right);
    }
}
