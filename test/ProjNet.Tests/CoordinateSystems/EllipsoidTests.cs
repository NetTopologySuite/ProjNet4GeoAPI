// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="Ellipsoid"/>.
/// </summary>
public class EllipsoidTests
{
    /// <summary>
    /// Verifies that the built-in ellipsoid factories expose the expected metadata.
    /// </summary>
    /// <param name="key">Well-known ellipsoid key.</param>
    /// <param name="expectedName">Expected ellipsoid name.</param>
    /// <param name="expectedAuthorityCode">Expected authority code.</param>
    /// <param name="expectedIvfDefinitive">Expected IVF definitiveness.</param>
    /// <param name="usesClarkesFoot">Whether the axis unit should be Clarke's foot.</param>
    [Theory]
    [InlineData("WGS84", "WGS 84", 7030L, true, false)]
    [InlineData("WGS72", "WGS 72", 7043L, true, false)]
    [InlineData("GRS80", "GRS 1980", 7019L, true, false)]
    [InlineData("International1924", "International 1924", 7022L, true, false)]
    [InlineData("Clarke1880", "Clarke 1880", 7034L, true, true)]
    [InlineData("Clarke1866", "Clarke 1866", 7008L, false, false)]
    [InlineData("Sphere", "GRS 1980 Authalic Sphere", 7048L, false, false)]
    public void KnownEllipsoids_ExposeExpectedMetadata(
        string key,
        string expectedName,
        long expectedAuthorityCode,
        bool expectedIvfDefinitive,
        bool usesClarkesFoot)
    {
        Ellipsoid ellipsoid = GetKnownEllipsoid(key);
        LinearUnit expectedUnit = usesClarkesFoot ? LinearUnit.ClarkesFoot : LinearUnit.Metre;

        Assert.Equal(expectedName, ellipsoid.Name);
        Assert.Equal("EPSG", ellipsoid.Authority);
        Assert.Equal(expectedAuthorityCode, ellipsoid.AuthorityCode);
        Assert.Equal(expectedIvfDefinitive, ellipsoid.IsIvfDefinitive);
        Assert.True(ellipsoid.AxisUnit.EqualParams(expectedUnit));
    }

    /// <summary>
    /// Verifies that IVF-definitive ellipsoids with zero inverse flattening use the semi-major axis as semi-minor axis.
    /// </summary>
    [Fact]
    public void Constructor_WithZeroInverseFlatteningAndIvfDefinitive_UsesSemiMajorAxisAsSemiMinor()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, 0d, true, LinearUnit.Metre, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.Equal(10d, ellipsoid.SemiMinorAxis);
    }

    /// <summary>
    /// Verifies that IVF-definitive ellipsoids compute the semi-minor axis from inverse flattening.
    /// </summary>
    [Fact]
    public void Constructor_WithFiniteInverseFlatteningAndIvfDefinitive_ComputesSemiMinorAxis()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, 2d, true, LinearUnit.Metre, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.Equal(5d, ellipsoid.SemiMinorAxis);
    }

    /// <summary>
    /// Verifies that non-IVF-definitive ellipsoids preserve the supplied semi-minor axis.
    /// </summary>
    [Fact]
    public void Constructor_WithoutIvfDefinitive_PreservesSemiMinorAxis()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, double.PositiveInfinity, false, LinearUnit.Metre, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.Equal(7d, ellipsoid.SemiMinorAxis);
    }

    /// <summary>
    /// Verifies that WKT omits the authority clause when authority information is unavailable.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthority_OmitsAuthorityClause()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.Equal("SPHEROID[\"Custom\", 10, 2]", ellipsoid.WKT);
    }

    /// <summary>
    /// Verifies that WKT includes the authority clause when authority information is available.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_IncludesAuthorityClause()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;

        Assert.Equal("SPHEROID[\"WGS 84\", 6378137, 298.257223563, AUTHORITY[\"EPSG\", \"7030\"]]", ellipsoid.WKT);
    }

    /// <summary>
    /// Verifies that XML contains the expected attributes and child elements.
    /// </summary>
    [Fact]
    public void XML_ContainsExpectedStructure()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.ClarkesFoot, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var xml = XElement.Parse(ellipsoid.XML);

        Assert.Equal("CS_Ellipsoid", xml.Name.LocalName);
        Assert.Equal("10", (string?)xml.Attribute("SemiMajorAxis"));
        Assert.Equal("7", (string?)xml.Attribute("SemiMinorAxis"));
        Assert.Equal("2", (string?)xml.Attribute("InverseFlattening"));
        Assert.Equal("0", (string?)xml.Attribute("IvfDefinitive"));
        Assert.NotNull(xml.Element("CS_Info"));
        Assert.NotNull(xml.Element("CS_LinearUnit"));
    }

    /// <summary>
    /// Verifies that IVF-definitive XML uses a flag value of <c>1</c>.
    /// </summary>
    [Fact]
    public void XML_WithIvfDefinitive_ContainsOneFlag()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        var xml = XElement.Parse(ellipsoid.XML);

        Assert.Equal("1", (string?)xml.Attribute("IvfDefinitive"));
        Assert.NotNull(xml.Element("CS_Info"));
        Assert.NotNull(xml.Element("CS_LinearUnit"));
    }

    /// <summary>
    /// Verifies that <see cref="Ellipsoid.ToXml"/> matches the XML property for IVF-definitive ellipsoids with authority information.
    /// </summary>
    [Fact]
    public void ToXml_WithAuthorityAndIvfDefinitive_MatchesXmlProperty()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        XElement element = ellipsoid.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(ellipsoid.XML), element));
    }

    /// <summary>
    /// Verifies that <see cref="Ellipsoid.ToXml"/> matches the XML property for non-IVF-definitive ellipsoids without authority information.
    /// </summary>
    [Fact]
    public void ToXml_WithoutAuthority_MatchesXmlProperty()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.ClarkesFoot, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        XElement element = ellipsoid.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(ellipsoid.XML), element));
    }

    /// <summary>
    /// Verifies that the WKT node includes authority information when available.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(ellipsoid.ToWktNode());

        Assert.Equal("SPHEROID", node.Keyword);
        Assert.Equal(4, node.Children.Count);
        Assert.IsType<WktKeywordNode>(node.Children[3]);
    }

    /// <summary>
    /// Verifies that the WKT node omits authority information when it is unavailable.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthority_OmitsAuthorityNode()
    {
        var ellipsoid = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "Custom", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(ellipsoid.ToWktNode());

        Assert.Equal(3, node.Children.Count);
    }

    /// <summary>
    /// Verifies that equality ignores metadata when the geometric parameters match.
    /// </summary>
    [Fact]
    public void EqualParams_SameParametersDifferentMetadata_ReturnsTrue()
    {
        var first = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "First", "EPSG", 1, "a1", "abbr1", "r1");
        var second = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "Second", "OTHER", 2, "a2", "abbr2", "r2");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different inverse flattening value breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentInverseFlattening_ReturnsFalse()
    {
        var first = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new Ellipsoid(10d, 7d, 3d, false, LinearUnit.Metre, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different IVF definitiveness flag breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentIvfDefinitive_ReturnsFalse()
    {
        var first = new Ellipsoid(10d, 10d, 0d, true, LinearUnit.Metre, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new Ellipsoid(10d, 10d, 0d, false, LinearUnit.Metre, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different semi-major axis breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentSemiMajorAxis_ReturnsFalse()
    {
        var first = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new Ellipsoid(11d, 7d, 2d, false, LinearUnit.Metre, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different semi-minor axis breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentSemiMinorAxis_ReturnsFalse()
    {
        var first = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new Ellipsoid(10d, 8d, 2d, false, LinearUnit.Metre, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different axis unit breaks equality.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentAxisUnit_ReturnsFalse()
    {
        var first = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Metre, "A", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        var second = new Ellipsoid(10d, 7d, 2d, false, LinearUnit.Foot, "B", string.Empty, -1, string.Empty, string.Empty, string.Empty);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that non-ellipsoid objects compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;

        Assert.False(ellipsoid.EqualParams("not an ellipsoid"));
    }

    private static Ellipsoid GetKnownEllipsoid(string key)
    {
        return key switch
        {
            "WGS84" => Ellipsoid.WGS84,
            "WGS72" => Ellipsoid.WGS72,
            "GRS80" => Ellipsoid.GRS80,
            "International1924" => Ellipsoid.International1924,
            "Clarke1880" => Ellipsoid.Clarke1880,
            "Clarke1866" => Ellipsoid.Clarke1866,
            "Sphere" => Ellipsoid.Sphere,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
    }
}
