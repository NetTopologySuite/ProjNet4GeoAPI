// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="Parameter"/> and <see cref="ProjectionParameter"/>.
/// </summary>
public class ParameterTests
{
    /// <summary>
    /// Verifies that the constructor assigns the name and value.
    /// </summary>
    [Fact]
    public void Parameter_Constructor_SetsNameAndValue()
    {
        var parameter = new Parameter("scale_factor", 1.0);

        Assert.Equal("scale_factor", parameter.Name);
        Assert.Equal(1.0, parameter.Value);
    }

    /// <summary>
    /// Verifies that the name can be updated after construction.
    /// </summary>
    [Fact]
    public void Parameter_Name_CanBeUpdated()
    {
        var parameter = new Parameter("scale_factor", 1.0)
        {
            Name = "central_meridian",
        };

        Assert.Equal("central_meridian", parameter.Name);
    }

    /// <summary>
    /// Verifies that the value can be updated after construction.
    /// </summary>
    [Fact]
    public void Parameter_Value_CanBeUpdated()
    {
        var parameter = new Parameter("scale_factor", 1.0)
        {
            Value = 0.9996,
        };

        Assert.Equal(0.9996, parameter.Value);
    }

    /// <summary>
    /// Verifies that the constructor assigns the name and value.
    /// </summary>
    [Fact]
    public void ProjectionParameter_Constructor_SetsNameAndValue()
    {
        var parameter = new ProjectionParameter("central_meridian", 15.0);

        Assert.Equal("central_meridian", parameter.Name);
        Assert.Equal(15.0, parameter.Value);
    }

    /// <summary>
    /// Verifies that the name and value can be updated after construction.
    /// </summary>
    [Fact]
    public void ProjectionParameter_Properties_CanBeUpdated()
    {
        var parameter = new ProjectionParameter("scale_factor", 1.0)
        {
            Name = "latitude_of_origin",
            Value = 52.5,
        };

        Assert.Equal("latitude_of_origin", parameter.Name);
        Assert.Equal(52.5, parameter.Value);
    }

    /// <summary>
    /// Verifies that WKT uses invariant formatting and includes name and value.
    /// </summary>
    [Fact]
    public void ProjectionParameter_WKT_FormatsExpectedValue()
    {
        var parameter = new ProjectionParameter("scale_factor", 0.9996);

        Assert.Equal("PARAMETER[\"scale_factor\", 0.9996]", parameter.WKT);
    }

    /// <summary>
    /// Verifies that XML uses the expected element name and attributes.
    /// </summary>
    [Fact]
    public void ProjectionParameter_XML_FormatsExpectedValue()
    {
        var parameter = new ProjectionParameter("central_meridian", 15.5);

        Assert.Equal("<CS_ProjectionParameter Name=\"central_meridian\" Value=\"15.5\"/>", parameter.XML);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectionParameter.ToXml"/> returns the expected element.
    /// </summary>
    [Fact]
    public void ProjectionParameter_ToXml_ReturnsExpectedElement()
    {
        var parameter = new ProjectionParameter("false_easting", 500000.0);

        XElement xml = parameter.ToXml();

        Assert.Equal("CS_ProjectionParameter", xml.Name.LocalName);
        Assert.Equal("false_easting", (string?)xml.Attribute("Name"));
        Assert.Equal("500000", (string?)xml.Attribute("Value"));
    }

    /// <summary>
    /// Verifies that <see cref="ProjectionParameter.ToWktNode()"/> returns the expected node structure.
    /// </summary>
    [Fact]
    public void ProjectionParameter_ToWktNode_ReturnsKeywordNode()
    {
        var parameter = new ProjectionParameter("central_meridian", 15.0);

        WktKeywordNode node = Assert.IsType<WktKeywordNode>(parameter.ToWktNode());

        Assert.Equal("PARAMETER", node.Keyword);
        Assert.Equal(2, node.Children.Count);

        WktQuotedString nameNode = Assert.IsType<WktQuotedString>(node.Children[0]);
        Assert.Equal("central_meridian", nameNode.Value);

        WktNumber valueNode = Assert.IsType<WktNumber>(node.Children[1]);
        Assert.Equal(15.0, valueNode.Value, 12);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectionParameter.ToString"/> returns a readable diagnostic string.
    /// </summary>
    [Fact]
    public void ProjectionParameter_ToString_ReturnsReadableValue()
    {
        var parameter = new ProjectionParameter("scale_factor", 0.9996);

        string text = parameter.ToString();

        Assert.StartsWith("ProjectionParameter 'scale_factor': ", text, StringComparison.Ordinal);
        Assert.Contains("9996", text, StringComparison.Ordinal);
    }
}
