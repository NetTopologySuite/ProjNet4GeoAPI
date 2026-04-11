// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Tests for <see cref="ProjectedCoordinateSystem"/>.
/// </summary>
public class ProjectedCoordinateSystemTests
{
    private static readonly CoordinateSystemFactory Factory = new();

    /// <summary>
    /// Verifies that the internal constructor stores the supplied values.
    /// </summary>
    [Fact]
    public void Constructor_SetsProperties()
    {
        GeographicCoordinateSystem geographicCoordinateSystem = GeographicCoordinateSystem.WGS84;
        Projection projection = CreateProjection();
        List<AxisInfo> axisInfo = CreateDefaultAxisInfo();
        var system = new ProjectedCoordinateSystem(
            HorizontalDatum.WGS84,
            geographicCoordinateSystem,
            LinearUnit.Foot,
            projection,
            axisInfo,
            "Custom projected",
            "EPSG",
            9999,
            "alias",
            "remarks",
            "abbr");

        Assert.Equal("Custom projected", system.Name);
        Assert.Equal("EPSG", system.Authority);
        Assert.Equal(9999, system.AuthorityCode);
        Assert.Equal("alias", system.Alias);
        Assert.Equal("remarks", system.Remarks);
        Assert.Equal("abbr", system.Abbreviation);
        Assert.True(system.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.Same(geographicCoordinateSystem, system.GeographicCoordinateSystem);
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Foot));
        Assert.Same(projection, system.Projection);
        Assert.Same(axisInfo, system.AxisInfo);
    }

    /// <summary>
    /// Verifies that the factory creates a projected coordinate system with the expected defaults.
    /// </summary>
    [Fact]
    public void Factory_CreatesProjectedCoordinateSystem()
    {
        GeographicCoordinateSystem geographicCoordinateSystem = GeographicCoordinateSystem.WGS84;
        Projection projection = CreateProjection();
        AxisInfo axis0 = new("East", AxisOrientationEnum.East);
        AxisInfo axis1 = new("North", AxisOrientationEnum.North);
        ProjectedCoordinateSystem system = Factory.CreateProjectedCoordinateSystem(
            "Factory projected",
            geographicCoordinateSystem,
            projection,
            LinearUnit.Metre,
            axis0,
            axis1);

        Assert.Equal("Factory projected", system.Name);
        Assert.Equal(2, system.Dimension);
        Assert.True(system.HorizontalDatum.EqualParams(geographicCoordinateSystem.HorizontalDatum));
        Assert.Same(geographicCoordinateSystem, system.GeographicCoordinateSystem);
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.Same(projection, system.Projection);
        Assert.Same(axis0, system.GetAxis(0));
        Assert.Same(axis1, system.GetAxis(1));
    }

    /// <summary>
    /// Verifies that the predefined Web Mercator coordinate system exposes the expected metadata.
    /// </summary>
    [Fact]
    public void WebMercator_HasExpectedMetadata()
    {
        ProjectedCoordinateSystem system = ProjectedCoordinateSystem.WebMercator;
        Projection projection = Assert.IsType<Projection>(system.Projection);

        Assert.Equal("WGS 84 / Pseudo-Mercator", system.Name);
        Assert.Equal("EPSG", system.Authority);
        Assert.Equal(3857, system.AuthorityCode);
        Assert.Equal("WGS 84 / Popular Visualisation Pseudo-Mercator", system.Alias);
        Assert.Equal("WebMercator", system.Abbreviation);
        Assert.True(system.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(system.GeographicCoordinateSystem.EqualParams(GeographicCoordinateSystem.WGS84));
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.Equal("Popular Visualisation Pseudo-Mercator", projection.ClassName);
        Assert.Equal(4, projection.NumParameters);
        Assert.Equal(0.0, projection.GetParameter("false_northing")!.Value);
        Assert.Contains("spherical development", system.Remarks, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WGS84 UTM uses the expected north and south metadata and parameter values.
    /// </summary>
    /// <param name="zone">The UTM zone.</param>
    /// <param name="zoneIsNorth">Whether the zone is in the northern hemisphere.</param>
    /// <param name="expectedAuthorityCode">The expected authority code.</param>
    /// <param name="expectedFalseNorthing">The expected false northing parameter.</param>
    /// <param name="expectedName">The expected coordinate system name.</param>
    [Theory]
    [InlineData(32, true, 32632L, 0.0, "WGS 84 / UTM zone 32N")]
    [InlineData(32, false, 32732L, 10000000.0, "WGS 84 / UTM zone 32S")]
    public void WGS84_UTM_HasExpectedMetadata(int zone, bool zoneIsNorth, long expectedAuthorityCode, double expectedFalseNorthing, string expectedName)
    {
        var system = ProjectedCoordinateSystem.WGS84_UTM(zone, zoneIsNorth);
        Projection projection = Assert.IsType<Projection>(system.Projection);

        Assert.Equal(expectedName, system.Name);
        Assert.Equal("EPSG", system.Authority);
        Assert.Equal(expectedAuthorityCode, system.AuthorityCode);
        Assert.True(system.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(system.GeographicCoordinateSystem.EqualParams(GeographicCoordinateSystem.WGS84));
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.Equal("Transverse_Mercator", projection.ClassName);
        Assert.Equal((zone * 6) - 183, projection.GetParameter("central_meridian")!.Value);
        Assert.Equal(0.9996, projection.GetParameter("scale_factor")!.Value, 12);
        Assert.Equal(500000.0, projection.GetParameter("false_easting")!.Value);
        Assert.Equal(expectedFalseNorthing, projection.GetParameter("false_northing")!.Value);
    }

    /// <summary>
    /// Verifies that the projected coordinate system exposes the members supplied at construction time.
    /// </summary>
    [Fact]
    public void ConstructorConfiguredValues_AreExposed()
    {
        GeographicCoordinateSystem geographicCoordinateSystem = CreateParisGeographicCoordinateSystem();
        Projection projection = CreateProjection("Lambert_Conformal_Conic_2SP");
        ProjectedCoordinateSystem system = CreateSystem(
            horizontalDatum: HorizontalDatum.ED50,
            geographicCoordinateSystem: geographicCoordinateSystem,
            linearUnit: LinearUnit.Foot,
            projection: projection);

        Assert.True(system.HorizontalDatum.EqualParams(HorizontalDatum.ED50));
        Assert.Same(geographicCoordinateSystem, system.GeographicCoordinateSystem);
        Assert.True(system.LinearUnit.EqualParams(LinearUnit.Foot));
        Assert.Same(projection, system.Projection);
    }

    /// <summary>
    /// Verifies that <see cref="Info.WithAuthority"/> preserves the projected CRS shape while replacing authority metadata.
    /// </summary>
    [Fact]
    public void WithAuthority_ReturnsProjectedCloneWithUpdatedAuthorityMetadata()
    {
        ProjectedCoordinateSystem original = CreateSystem(authority: "TEST", authorityCode: 7);
        ProjectedCoordinateSystem clone = Assert.IsType<ProjectedCoordinateSystem>(original.WithAuthority("EPSG", 32632));

        Assert.Equal("EPSG", clone.Authority);
        Assert.Equal(32632, clone.AuthorityCode);
        Assert.Equal("TEST", original.Authority);
        Assert.Equal(7, original.AuthorityCode);
        Assert.NotSame(original, clone);
        Assert.NotSame(original.GeographicCoordinateSystem, clone.GeographicCoordinateSystem);
        Assert.True(original.GeographicCoordinateSystem.EqualParams(clone.GeographicCoordinateSystem));
        Assert.NotSame(original.HorizontalDatum, clone.HorizontalDatum);
        Assert.True(original.HorizontalDatum.EqualParams(clone.HorizontalDatum));
    }

    /// <summary>
    /// Verifies that WKT omits axis clauses when the default projected axes are used.
    /// </summary>
    [Fact]
    public void WKT_WithDefaultAxes_OmitsAxisClauses()
    {
        ProjectedCoordinateSystem system = CreateSystem(axisInfo: CreateDefaultAxisInfo());

        Assert.DoesNotContain("AXIS[", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT includes custom axis clauses when the axis definitions differ from the defaults.
    /// </summary>
    [Fact]
    public void WKT_WithCustomAxes_IncludesAxisClauses()
    {
        ProjectedCoordinateSystem system = CreateSystem(axisInfo: CreateCustomAxisInfo());

        Assert.Contains("AXIS[\"East\", EAST]", system.WKT, StringComparison.Ordinal);
        Assert.Contains("AXIS[\"North\", NORTH]", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT includes authority metadata when it is available.
    /// </summary>
    [Fact]
    public void WKT_WithAuthority_IncludesAuthorityClause()
    {
        ProjectedCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 32632);

        Assert.Contains("AUTHORITY[\"EPSG\", \"32632\"]", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT omits authority metadata when the authority code is not positive.
    /// </summary>
    [Fact]
    public void WKT_WithAuthorityNameButNonPositiveCode_OmitsAuthorityClause()
    {
        ProjectedCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 0);

        Assert.DoesNotContain("AUTHORITY[\"EPSG\", \"0\"]", system.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML contains the expected structure when the projection is a <see cref="Projection"/> instance.
    /// </summary>
    [Fact]
    public void XML_WithProjectionInstance_ContainsExpectedStructure()
    {
        ProjectedCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 32632);
        var xml = XElement.Parse(system.XML);
        XElement inner = Assert.IsType<XElement>(xml.Element("CS_ProjectedCoordinateSystem"));

        Assert.Equal("CS_CoordinateSystem", xml.Name.LocalName);
        Assert.Equal("2", (string?)xml.Attribute("Dimension"));
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.Equal(2, inner.Elements("CS_AxisInfo").Count());
        Assert.NotNull(inner.Element("CS_CoordinateSystem"));
        Assert.NotNull(inner.Element("CS_LinearUnit"));
        Assert.NotNull(inner.Element("CS_Projection"));
    }

    /// <summary>
    /// Verifies that XML still emits the projection element when the projection does not use the concrete <see cref="Projection"/> type.
    /// </summary>
    [Fact]
    public void XML_WithNonProjectionInstance_UsesProjectionXml()
    {
        ProjectedCoordinateSystem system = CreateSystem(projection: new FakeProjection());
        var xml = XElement.Parse(system.XML);
        XElement inner = Assert.IsType<XElement>(xml.Element("CS_ProjectedCoordinateSystem"));
        XElement projection = Assert.IsType<XElement>(inner.Element("CS_Projection"));

        Assert.Equal("Fake_Projection", projection.Attribute("Classname")?.Value);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToXml"/> matches the XML property when the projection is a <see cref="Projection"/> instance.
    /// </summary>
    [Fact]
    public void ToXml_WithProjectionInstance_MatchesXmlProperty()
    {
        ProjectedCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 32632);
        XElement xml = system.ToXml();

        Assert.True(XNode.DeepEquals(XElement.Parse(system.XML), xml));
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToWktNode()"/> matches WKT when default axes are used.
    /// </summary>
    [Fact]
    public void ToWktNode_WithDefaultAxes_MatchesWkt()
    {
        ProjectedCoordinateSystem system = CreateSystem(axisInfo: CreateDefaultAxisInfo());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());

        Assert.Equal("PROJCS", node.Keyword);
        Assert.Equal(system.WKT, node.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToWktNode()"/> uses an identifier node when the projection is not a <see cref="Projection"/> instance.
    /// </summary>
    [Fact]
    public void ToWktNode_WithNonProjectionInstance_UsesIdentifierNode()
    {
        FakeProjection projection = new();
        ProjectedCoordinateSystem system = CreateSystem(projection: projection, axisInfo: CreateDefaultAxisInfo());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());
        WktIdentifier projectionNode = Assert.IsType<WktIdentifier>(node.Children[2]);

        Assert.Equal(projection.WKT, projectionNode.Name);
        Assert.Equal(projection.NumParameters + 4, node.Children.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToWktNode()"/> includes an authority node when metadata is present.
    /// </summary>
    [Fact]
    public void ToWktNode_WithAuthority_IncludesAuthorityNode()
    {
        ProjectedCoordinateSystem system = CreateSystem(authority: "EPSG", authorityCode: 32632, axisInfo: CreateDefaultAxisInfo());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());
        WktKeywordNode authorityNode = Assert.IsType<WktKeywordNode>(node.Children[^1]);

        Assert.Equal("AUTHORITY", authorityNode.Keyword);
        Assert.Equal("EPSG", Assert.IsType<WktQuotedString>(authorityNode.Children[0]).Value);
        Assert.Equal("32632", Assert.IsType<WktQuotedString>(authorityNode.Children[1]).Value);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToWktNode()"/> includes custom axis nodes when the axes differ from the defaults.
    /// </summary>
    [Fact]
    public void ToWktNode_WithCustomAxes_IncludesAxisNodes()
    {
        ProjectedCoordinateSystem system = CreateSystem(axisInfo: CreateCustomAxisInfo());
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());
        WktKeywordNode firstAxis = Assert.IsType<WktKeywordNode>(node.Children[^2]);
        WktKeywordNode secondAxis = Assert.IsType<WktKeywordNode>(node.Children[^1]);

        Assert.Equal("AXIS", firstAxis.Keyword);
        Assert.Equal("East", Assert.IsType<WktQuotedString>(firstAxis.Children[0]).Value);
        Assert.Equal("EAST", Assert.IsType<WktIdentifier>(firstAxis.Children[1]).Name);
        Assert.Equal("AXIS", secondAxis.Keyword);
        Assert.Equal("North", Assert.IsType<WktQuotedString>(secondAxis.Children[0]).Value);
        Assert.Equal("NORTH", Assert.IsType<WktIdentifier>(secondAxis.Children[1]).Name);
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToWktNode()"/> includes axis nodes when only the first axis name differs from the defaults.
    /// </summary>
    [Fact]
    public void ToWktNode_WithFirstAxisNameChanged_IncludesAxisNodes()
    {
        ProjectedCoordinateSystem system = CreateSystem(axisInfo:
        [
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Y", AxisOrientationEnum.North),
        ]);
        WktKeywordNode node = Assert.IsType<WktKeywordNode>(system.ToWktNode());
        WktKeywordNode firstAxis = Assert.IsType<WktKeywordNode>(node.Children[^2]);
        WktKeywordNode secondAxis = Assert.IsType<WktKeywordNode>(node.Children[^1]);

        Assert.Equal("Easting", Assert.IsType<WktQuotedString>(firstAxis.Children[0]).Value);
        Assert.Equal("Y", Assert.IsType<WktQuotedString>(secondAxis.Children[0]).Value);
    }

    /// <summary>
    /// Verifies that GetUnits returns the linear unit regardless of the requested dimension.
    /// </summary>
    /// <param name="dimension">The requested dimension index.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    public void GetUnits_ReturnsLinearUnitForAnyDimension(int dimension)
    {
        ProjectedCoordinateSystem system = CreateSystem(linearUnit: LinearUnit.Foot);
        IUnit unit = system.GetUnits(dimension);

        Assert.True(unit.EqualParams(LinearUnit.Foot));
    }

    /// <summary>
    /// Verifies that equal projected coordinate systems compare equal.
    /// </summary>
    [Fact]
    public void EqualParams_SameValues_ReturnsTrue()
    {
        ProjectedCoordinateSystem first = CreateSystem(name: "First");
        ProjectedCoordinateSystem second = CreateSystem(name: "Second");

        Assert.True(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that projected coordinate systems reject invalid axis counts.
    /// </summary>
    [Fact]
    public void Constructor_InvalidAxisCount_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateSystem(axisInfo: [new AxisInfo("Only", AxisOrientationEnum.East)]));
    }

    /// <summary>
    /// Verifies that a different axis orientation causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentAxisOrientation_ReturnsFalse()
    {
        ProjectedCoordinateSystem first = CreateSystem();
        ProjectedCoordinateSystem second = CreateSystem(axisInfo:
        [
            new AxisInfo("X", AxisOrientationEnum.East),
            new AxisInfo("Y", AxisOrientationEnum.South),
        ]);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different geographic coordinate system causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentGeographicCoordinateSystem_ReturnsFalse()
    {
        ProjectedCoordinateSystem first = CreateSystem();
        ProjectedCoordinateSystem second = CreateSystem(geographicCoordinateSystem: CreateParisGeographicCoordinateSystem());

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different horizontal datum causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentHorizontalDatum_ReturnsFalse()
    {
        ProjectedCoordinateSystem first = CreateSystem();
        ProjectedCoordinateSystem second = CreateSystem(horizontalDatum: HorizontalDatum.ED50);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different linear unit causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentLinearUnit_ReturnsFalse()
    {
        ProjectedCoordinateSystem first = CreateSystem();
        ProjectedCoordinateSystem second = CreateSystem(linearUnit: LinearUnit.Foot);

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that a different projection causes equality to fail.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentProjection_ReturnsFalse()
    {
        ProjectedCoordinateSystem first = CreateSystem();
        ProjectedCoordinateSystem second = CreateSystem(projection: CreateProjection(falseNorthing: 1.0));

        Assert.False(first.EqualParams(second));
    }

    /// <summary>
    /// Verifies that different object types compare unequal.
    /// </summary>
    [Fact]
    public void EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(CreateSystem().EqualParams("not a projected coordinate system"));
    }

    private static Projection CreateProjection(string className = "Transverse_Mercator", double falseNorthing = 0.0)
    {
        return new Projection(
            className,
            [
                new ProjectionParameter("latitude_of_origin", 0.0),
                new ProjectionParameter("central_meridian", 9.0),
                new ProjectionParameter("false_northing", falseNorthing),
            ],
            "Projection",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ProjectedCoordinateSystem CreateSystem(
        string name = "Custom projected",
        HorizontalDatum? horizontalDatum = null,
        GeographicCoordinateSystem? geographicCoordinateSystem = null,
        LinearUnit? linearUnit = null,
        IProjection? projection = null,
        List<AxisInfo>? axisInfo = null,
        string authority = "",
        long authorityCode = -1)
    {
        GeographicCoordinateSystem effectiveGeographicCoordinateSystem = geographicCoordinateSystem ?? GeographicCoordinateSystem.WGS84;

        return new ProjectedCoordinateSystem(
            horizontalDatum ?? effectiveGeographicCoordinateSystem.HorizontalDatum,
            effectiveGeographicCoordinateSystem,
            linearUnit ?? LinearUnit.Metre,
            projection ?? CreateProjection(),
            axisInfo ?? CreateDefaultAxisInfo(),
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeographicCoordinateSystem CreateParisGeographicCoordinateSystem()
    {
        return Factory.CreateGeographicCoordinateSystem(
            "Paris geographic",
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Paris,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
    }

    private static List<AxisInfo> CreateDefaultAxisInfo()
    {
        return
        [
            new AxisInfo("X", AxisOrientationEnum.East),
            new AxisInfo("Y", AxisOrientationEnum.North),
        ];
    }

    private static List<AxisInfo> CreateCustomAxisInfo()
    {
        return
        [
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North),
        ];
    }

    private sealed class FakeProjection : IProjection
    {
        private readonly List<ProjectionParameter> parameters =
        [
            new ProjectionParameter("latitude_of_origin", 0.0),
            new ProjectionParameter("central_meridian", 12.0),
        ];

        public string Name => "Fake projection";

        public string Authority => string.Empty;

        public long AuthorityCode => -1;

        public string Alias => string.Empty;

        public string Abbreviation => string.Empty;

        public string Remarks => string.Empty;

        public string WKT => "PROJECTION[\"Fake_Projection\"]";

        public string XML => "<CS_Projection Classname=\"Fake_Projection\" />";

        public int NumParameters => this.parameters.Count;

        public string ClassName => "Fake_Projection";

        public ProjectionParameter GetParameter(int index) => this.parameters[index];

        public ProjectionParameter? GetParameter(string name)
        {
            foreach (ProjectionParameter parameter in this.parameters)
            {
                if (parameter.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return parameter;
                }
            }

            return null;
        }

        public bool EqualParams(object obj)
        {
            if (obj is not FakeProjection other || other.parameters.Count != this.parameters.Count)
            {
                return false;
            }

            for (int i = 0; i < this.parameters.Count; i++)
            {
                if (!string.Equals(this.parameters[i].Name, other.parameters[i].Name, StringComparison.OrdinalIgnoreCase) ||
                    this.parameters[i].Value != other.parameters[i].Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
