// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="AxisInfo"/>.
/// </summary>
public class AxisInfoTests
{
    /// <summary>
    /// Verifies that the constructor assigns name and orientation.
    /// </summary>
    [Fact]
    public void Constructor_SetsNameAndOrientation()
    {
        var axis = new AxisInfo("Longitude", AxisOrientationEnum.East);

        Assert.Equal("Longitude", axis.Name);
        Assert.Equal(AxisOrientationEnum.East, axis.Orientation);
    }

    /// <summary>
    /// Verifies that both properties can be updated after construction.
    /// </summary>
    [Fact]
    public void Properties_CanBeUpdated()
    {
        var axis = new AxisInfo("X", AxisOrientationEnum.Other)
        {
            Name = "Latitude",
            Orientation = AxisOrientationEnum.North,
        };

        Assert.Equal("Latitude", axis.Name);
        Assert.Equal(AxisOrientationEnum.North, axis.Orientation);
    }

    /// <summary>
    /// Verifies that WKT uses the expected keyword, axis name, and upper-cased orientation.
    /// </summary>
    [Fact]
    public void WKT_FormatsExpectedValue()
    {
        var axis = new AxisInfo("Latitude", AxisOrientationEnum.North);

        Assert.Equal("AXIS[\"Latitude\", NORTH]", axis.WKT);
    }

    /// <summary>
    /// Verifies that XML uses the expected element name and attributes.
    /// </summary>
    [Fact]
    public void XML_FormatsExpectedValue()
    {
        var axis = new AxisInfo("Longitude", AxisOrientationEnum.East);

        Assert.Equal("<CS_AxisInfo Name=\"Longitude\" Orientation=\"EAST\"/>", axis.XML);
    }

    /// <summary>
    /// Verifies that <see cref="AxisInfo.ToXml"/> returns the expected XML element.
    /// </summary>
    [Fact]
    public void ToXml_ReturnsExpectedElement()
    {
        var axis = new AxisInfo("Height", AxisOrientationEnum.Up);

        XElement xml = axis.ToXml();

        Assert.Equal("CS_AxisInfo", xml.Name.LocalName);
        Assert.Equal("Height", (string?)xml.Attribute("Name"));
        Assert.Equal("UP", (string?)xml.Attribute("Orientation"));
    }

    /// <summary>
    /// Verifies that <see cref="AxisInfo.ToWktNode"/> returns the expected node structure.
    /// </summary>
    [Fact]
    public void ToWktNode_ReturnsExpectedKeywordNode()
    {
        var axis = new AxisInfo("Longitude", AxisOrientationEnum.East);

        WktKeywordNode node = Assert.IsType<WktKeywordNode>(axis.ToWktNode());

        Assert.Equal("AXIS", node.Keyword);
        Assert.Equal(2, node.Children.Count);

        WktQuotedString nameNode = Assert.IsType<WktQuotedString>(node.Children[0]);
        Assert.Equal("Longitude", nameNode.Value);

        WktIdentifier orientationNode = Assert.IsType<WktIdentifier>(node.Children[1]);
        Assert.Equal("EAST", orientationNode.Name);
    }
}
