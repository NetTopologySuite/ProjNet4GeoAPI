// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="Projection"/>.
/// </summary>
public class ProjectionTests
{
    /// <summary>
    /// Verifies that the constructor stores metadata, class name, and parameters.
    /// </summary>
    [Fact]
    public void Constructor_SetsPropertiesAndParameters()
    {
        List<ProjectionParameter> parameters = CreateParameters();
        var projection = new Projection(
            "Transverse_Mercator",
            parameters,
            "UTM zone 32N",
            "EPSG",
            9807,
            "alias",
            "remarks",
            "abbr");

        Assert.Equal("Transverse_Mercator", projection.ClassName);
        Assert.Equal("UTM zone 32N", projection.Name);
        Assert.Equal("EPSG", projection.Authority);
        Assert.Equal(9807, projection.AuthorityCode);
        Assert.Equal("alias", projection.Alias);
        Assert.Equal("remarks", projection.Remarks);
        Assert.Equal("abbr", projection.Abbreviation);
        Assert.Equal(3, projection.NumParameters);
        Assert.Same(parameters[0], projection.GetParameter(0));
    }

    /// <summary>
    /// Verifies that replacing the parameter list updates lookup and parameter count.
    /// </summary>
    [Fact]
    public void Parameters_Setter_ReplacesCollection()
    {
        Projection projection = CreateProjection();
        List<ProjectionParameter> replacement =
        [
            new ProjectionParameter("false_easting", 500000.0),
        ];

        projection.Parameters = replacement;

        Assert.Equal(1, projection.NumParameters);
        Assert.Same(replacement[0], projection.GetParameter(0));
        Assert.Null(projection.GetParameter("scale_factor"));
    }

    /// <summary>
    /// Verifies that WKT omits the authority clause when authority metadata is absent.
    /// </summary>
    [Fact]
    public void WKT_WithoutAuthority_FormatsExpectedValue()
    {
        Projection projection = CreateProjection();

        Assert.Equal("PROJECTION[\"Transverse_Mercator\"]", projection.WKT);
    }

    /// <summary>
    /// Verifies that WKT omits the authority clause when the authority code is not positive.
    /// </summary>
    [Fact]
    public void WKT_WithAuthorityNameButNonPositiveCode_OmitsAuthorityClause()
    {
        Projection projection = CreateProjection(authority: "EPSG", authorityCode: 0);

        Assert.Equal("PROJECTION[\"Transverse_Mercator\"]", projection.WKT);
    }

    /// <summary>
    /// Verifies that WKT includes the authority clause when metadata is present.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_FormatsExpectedValue()
    {
        Projection projection = CreateProjection(authority: "EPSG", authorityCode: 9807);

        Assert.Equal("PROJECTION[\"Transverse_Mercator\", AUTHORITY[\"EPSG\", \"9807\"]]", projection.WKT);
    }

    /// <summary>
    /// Verifies that XML contains the expected metadata and parameter elements.
    /// </summary>
    [Fact]
    public void XML_ContainsInfoAndParameterElements()
    {
        Projection projection = CreateProjection(authority: "EPSG", authorityCode: 9807);
        var xml = XElement.Parse(projection.XML);

        Assert.Equal("CS_Projection", xml.Name.LocalName);
        Assert.Equal("Transverse_Mercator", (string?)xml.Attribute("Classname"));

        XElement info = Assert.IsType<XElement>(xml.Element("CS_Info"));
        Assert.Equal("9807", (string?)info.Attribute("AuthorityCode"));
        Assert.Equal("EPSG", (string?)info.Attribute("Authority"));
        Assert.Equal("UTM zone 32N", (string?)info.Attribute("Name"));
        Assert.Equal("abbr", (string?)info.Attribute("Abbreviation"));
        Assert.Equal(3, new List<XElement>(xml.Elements("CS_ProjectionParameter")).Count);
    }

    /// <summary>
    /// Verifies that <see cref="Projection.ToXml"/> matches the XML property.
    /// </summary>
    [Fact]
    public void ToXml_MatchesXmlProperty()
    {
        Projection projection = CreateProjection(authority: "EPSG", authorityCode: 9807);
        XElement xml = projection.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(projection.XML), xml));
    }

    /// <summary>
    /// Verifies that <see cref="Projection.ToWktNode"/> returns a projection node without authority when metadata is absent.
    /// </summary>
    [Fact]
    public void ToWktNode_WithoutAuthority_ReturnsKeywordNode()
    {
        Projection projection = CreateProjection();
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(projection.ToWktNode());

        Assert.Equal("PROJECTION", node.Keyword);
        Assert.Single(node.Children);
        Assert.Equal("Transverse_Mercator", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Projection.ToWktNode"/> omits authority when the code is not positive.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthorityNameButNonPositiveCode_OmitsAuthorityNode()
    {
        Projection projection = CreateProjection(authority: "EPSG", authorityCode: 0);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(projection.ToWktNode());

        Assert.Single(node.Children);
        Assert.Equal("Transverse_Mercator", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Projection.ToWktNode"/> includes authority metadata when present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        Projection projection = CreateProjection(authority: "EPSG", authorityCode: 9807);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(projection.ToWktNode());
        WktKeywordNode authorityNode;

        Assert.Equal(2, node.Children.Count);
        Assert.Equal("Transverse_Mercator", Assert.IsType<WktQuotedString>(node.Children[0]).Value);
        authorityNode = Assert.IsType<WktKeywordNode>(node.Children[1]);
        Assert.Equal("AUTHORITY", authorityNode.Keyword);
        Assert.Equal("EPSG", Assert.IsType<WktQuotedString>(authorityNode.Children[0]).Value);
        Assert.Equal("9807", Assert.IsType<WktQuotedString>(authorityNode.Children[1]).Value);
    }

    /// <summary>
    /// Verifies that indexed lookup returns the requested parameter.
    /// </summary>
    [Fact]
    public void GetParameter_ByIndex_ReturnsParameter()
    {
        Projection projection = CreateProjection();
        ProjectionParameter parameter = projection.GetParameter(1);

        Assert.Equal("central_meridian", parameter.Name);
        Assert.Equal(9.0, parameter.Value);
    }

    /// <summary>
    /// Verifies that named lookup is case insensitive.
    /// </summary>
    [Fact]
    public void GetParameter_ByName_IsCaseInsensitive()
    {
        Projection projection = CreateProjection();
        ProjectionParameter? parameter = projection.GetParameter("CENTRAL_MERIDIAN");

        Assert.NotNull(parameter);
        Assert.Equal(9.0, parameter.Value);
    }

    /// <summary>
    /// Verifies that missing named parameters return null.
    /// </summary>
    [Fact]
    public void GetParameter_ByName_WhenMissing_ReturnsNull()
    {
        Projection projection = CreateProjection();

        Assert.Null(projection.GetParameter("false_northing"));
    }

    /// <summary>
    /// Verifies that projections compare equal when parameter names and values match in a different order.
    /// </summary>
    [Fact]
    public void EqualParams_SameParametersInDifferentOrder_ReturnsTrue()
    {
        Projection first = CreateProjection(name: "First", authority: "EPSG", authorityCode: 9807);
        Projection second = CreateProjection(
            className: "Transverse_Mercator",
            name: "Second",
            authority: "IGNF",
            authorityCode: 1,
            parameters:
            [
                new ProjectionParameter("scale_factor", 0.9996),
                new ProjectionParameter("central_meridian", 9.0),
                new ProjectionParameter("latitude_of_origin", 0.0),
            ]);

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that projections with different parameter counts compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentParameterCount_ReturnsFalse()
    {
        Projection first = CreateProjection();
        Projection second = CreateProjection(parameters:
        [
            new ProjectionParameter("latitude_of_origin", 0.0),
            new ProjectionParameter("central_meridian", 9.0),
        ]);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that projections with different parameter names compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentParameterName_ReturnsFalse()
    {
        Projection first = CreateProjection();
        Projection second = CreateProjection(parameters:
        [
            new ProjectionParameter("latitude_of_origin", 0.0),
            new ProjectionParameter("longitude_of_center", 9.0),
            new ProjectionParameter("scale_factor", 0.9996),
        ]);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that projections with different parameter values compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentParameterValue_ReturnsFalse()
    {
        Projection first = CreateProjection();
        Projection second = CreateProjection(parameters:
        [
            new ProjectionParameter("latitude_of_origin", 0.0),
            new ProjectionParameter("central_meridian", 10.0),
            new ProjectionParameter("scale_factor", 0.9996),
        ]);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that projections compare unequal to different object types.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(CreateProjection().EqualParams("not a projection"));
    }

    private static Projection CreateProjection(
        string className = "Transverse_Mercator",
        string name = "UTM zone 32N",
        string authority = "",
        long authorityCode = -1,
        List<ProjectionParameter>? parameters = null)
    {
        return new Projection(
            className,
            parameters ?? CreateParameters(),
            name,
            authority,
            authorityCode,
            "alias",
            "remarks",
            "abbr");
    }

    private static List<ProjectionParameter> CreateParameters()
    {
        return
        [
            new ProjectionParameter("latitude_of_origin", 0.0),
            new ProjectionParameter("central_meridian", 9.0),
            new ProjectionParameter("scale_factor", 0.9996),
        ];
    }
}
