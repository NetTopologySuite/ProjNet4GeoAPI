// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Tests for <see cref="Info"/>.
/// </summary>
public class InfoTests
{
    /// <summary>
    /// Verifies that the constructor stores the supplied metadata and <see cref="Info.ToString"/> returns WKT.
    /// </summary>
    [Fact]
    public void Constructor_SetsMetadataAndToStringReturnsWkt()
    {
        var info = new TestInfo(
            "WGS 84",
            "EPSG",
            4326,
            "alias",
            "abbr",
            "remarks",
            "GEOGCS[\"WGS 84\"]",
            "<Info />");

        Assert.Equal("WGS 84", info.Name);
        Assert.Equal("EPSG", info.Authority);
        Assert.Equal(4326, info.AuthorityCode);
        Assert.Equal("alias", info.Alias);
        Assert.Equal("abbr", info.Abbreviation);
        Assert.Equal("remarks", info.Remarks);
        Assert.Equal("GEOGCS[\"WGS 84\"]", info.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Info.WithAuthority"/> returns a new instance with updated authority metadata.
    /// </summary>
    [Fact]
    public void WithAuthority_ReturnsCloneWithUpdatedAuthorityMetadata()
    {
        LinearUnit original = LinearUnit.Metre;
        LinearUnit clone = original.WithAuthority("TEST", 1234);

        Assert.Equal("TEST", clone.Authority);
        Assert.Equal(1234, clone.AuthorityCode);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal("EPSG", original.Authority);
        Assert.Equal(9001, original.AuthorityCode);
        Assert.NotSame(original, clone);
    }

    /// <summary>
    /// Verifies that <see cref="Info.WithName"/> returns a new instance with updated naming metadata.
    /// </summary>
    [Fact]
    public void WithName_ReturnsCloneWithUpdatedName()
    {
        LinearUnit original = LinearUnit.Metre;
        LinearUnit clone = original.WithName("Meter");

        Assert.Equal("Meter", clone.Name);
        Assert.Equal("metre", original.Name);
        Assert.Equal(original.Authority, clone.Authority);
        Assert.Equal(original.AuthorityCode, clone.AuthorityCode);
        Assert.NotSame(original, clone);
    }

    /// <summary>
    /// Verifies that the typed <c>WithAuthority</c> overloads on unit-like types and ellipsoid/prime-meridian types return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedWithAuthority_ReturnsConcreteClonesWithoutCasts()
    {
        AngularUnit angularClone = AngularUnit.Degrees.WithAuthority("TEST", 1001);
        LinearUnit linearClone = LinearUnit.Metre.WithAuthority("TEST", 1002);
        ParametricUnit parametricClone = new ParametricUnit(0.1d, "pressure", "EPSG", 1024, string.Empty, string.Empty, string.Empty).WithAuthority("TEST", 1003);
        TimeUnit timeClone = new TimeUnit(1d, "second", "EPSG", 1040, string.Empty, string.Empty, string.Empty).WithAuthority("TEST", 1004);
        Ellipsoid ellipsoidClone = Ellipsoid.WGS84.WithAuthority("TEST", 1005);
        PrimeMeridian primeMeridianClone = PrimeMeridian.Greenwich.WithAuthority("TEST", 1006);

        Assert.Equal("TEST", angularClone.Authority);
        Assert.Equal(1002, linearClone.AuthorityCode);
        Assert.Equal("TEST", parametricClone.Authority);
        Assert.Equal(1004, timeClone.AuthorityCode);
        Assert.Equal("TEST", ellipsoidClone.Authority);
        Assert.Equal(1006, primeMeridianClone.AuthorityCode);
    }

    /// <summary>
    /// Verifies that the typed <c>WithName</c> overloads on unit-like types and ellipsoid/prime-meridian types return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedWithName_ReturnsConcreteClonesWithoutCasts()
    {
        AngularUnit angularClone = AngularUnit.Degrees.WithName("Degree");
        LinearUnit linearClone = LinearUnit.Metre.WithName("Meter");
        ParametricUnit parametricClone = new ParametricUnit(0.1d, "pressure", "EPSG", 1024, string.Empty, string.Empty, string.Empty).WithName("Pressure unit");
        TimeUnit timeClone = new TimeUnit(1d, "second", "EPSG", 1040, string.Empty, string.Empty, string.Empty).WithName("Second unit");
        Ellipsoid ellipsoidClone = Ellipsoid.WGS84.WithName("Custom WGS 84");
        PrimeMeridian primeMeridianClone = PrimeMeridian.Greenwich.WithName("Custom Greenwich");

        Assert.Equal("Degree", angularClone.Name);
        Assert.Equal("Meter", linearClone.Name);
        Assert.Equal("Pressure unit", parametricClone.Name);
        Assert.Equal("Second unit", timeClone.Name);
        Assert.Equal("Custom WGS 84", ellipsoidClone.Name);
        Assert.Equal("Custom Greenwich", primeMeridianClone.Name);
    }

    /// <summary>
    /// Verifies that the typed datum <c>WithAuthority</c> overloads return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedDatumWithAuthority_ReturnsConcreteClonesWithoutCasts()
    {
        HorizontalDatum horizontalClone = HorizontalDatum.WGS84.WithAuthority("TEST", 2001);
        VerticalDatum verticalClone = VerticalDatum.ODN.WithAuthority("TEST", 2002);
        EngineeringDatum engineeringClone = new EngineeringDatum("Engineering datum", "EPSG", 9300, string.Empty, string.Empty, string.Empty).WithAuthority("TEST", 2003);
        ParametricDatum parametricClone = new ParametricDatum("Parametric datum", "EPSG", 9301, string.Empty, string.Empty, string.Empty).WithAuthority("TEST", 2004);
        TemporalDatum temporalClone = new TemporalDatum("2024-01-01T00:00:00Z", "Temporal datum", "EPSG", 9302, string.Empty, string.Empty, string.Empty).WithAuthority("TEST", 2005);

        Assert.Equal("TEST", horizontalClone.Authority);
        Assert.Equal(2002, verticalClone.AuthorityCode);
        Assert.Equal("TEST", engineeringClone.Authority);
        Assert.Equal(2004, parametricClone.AuthorityCode);
        Assert.Equal("TEST", temporalClone.Authority);
    }

    /// <summary>
    /// Verifies that the typed datum <c>WithName</c> overloads return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedDatumWithName_ReturnsConcreteClonesWithoutCasts()
    {
        HorizontalDatum horizontalClone = HorizontalDatum.WGS84.WithName("Horizontal datum");
        VerticalDatum verticalClone = VerticalDatum.ODN.WithName("Vertical datum");
        EngineeringDatum engineeringClone = new EngineeringDatum("Engineering datum", "EPSG", 9300, string.Empty, string.Empty, string.Empty).WithName("Engineering datum clone");
        ParametricDatum parametricClone = new ParametricDatum("Parametric datum", "EPSG", 9301, string.Empty, string.Empty, string.Empty).WithName("Parametric datum clone");
        TemporalDatum temporalClone = new TemporalDatum("2024-01-01T00:00:00Z", "Temporal datum", "EPSG", 9302, string.Empty, string.Empty, string.Empty).WithName("Temporal datum clone");

        Assert.Equal("Horizontal datum", horizontalClone.Name);
        Assert.Equal("Vertical datum", verticalClone.Name);
        Assert.Equal("Engineering datum clone", engineeringClone.Name);
        Assert.Equal("Parametric datum clone", parametricClone.Name);
        Assert.Equal("Temporal datum clone", temporalClone.Name);
    }

    /// <summary>
    /// Verifies that the typed datum <c>WithEnsemble</c> overloads return concrete clones without casts when ensembles are supported.
    /// </summary>
    [Fact]
    public void TypedDatumWithEnsemble_ReturnsConcreteClonesForSupportedDatums()
    {
        DatumEnsemble horizontalEnsemble = CreateTestEnsemble("Horizontal ensemble", HorizontalDatum.WGS84.Ellipsoid);
        DatumEnsemble verticalEnsemble = CreateTestEnsemble("Vertical ensemble");

        HorizontalDatum horizontalClone = HorizontalDatum.WGS84.WithEnsemble(horizontalEnsemble);
        VerticalDatum verticalClone = VerticalDatum.ODN.WithEnsemble(verticalEnsemble);

        Assert.Equal("Horizontal ensemble", Assert.IsType<DatumEnsemble>(horizontalClone.Ensemble).Name);
        Assert.Equal("Vertical ensemble", Assert.IsType<DatumEnsemble>(verticalClone.Ensemble).Name);
    }

    /// <summary>
    /// Verifies that the typed datum <c>WithEnsemble</c> overloads keep unsupported datum types typed and reject non-null ensemble metadata.
    /// </summary>
    [Fact]
    public void TypedDatumWithEnsemble_OnUnsupportedDatumsRejectsNonNullMetadata()
    {
        DatumEnsemble ensemble = CreateTestEnsemble("Unsupported ensemble");

        var engineeringDatum = new EngineeringDatum("Engineering datum", "EPSG", 9300, string.Empty, string.Empty, string.Empty);
        var parametricDatum = new ParametricDatum("Parametric datum", "EPSG", 9301, string.Empty, string.Empty, string.Empty);
        var temporalDatum = new TemporalDatum("2024-01-01T00:00:00Z", "Temporal datum", "EPSG", 9302, string.Empty, string.Empty, string.Empty);

        EngineeringDatum engineeringClone = engineeringDatum.WithEnsemble(null);
        ParametricDatum parametricClone = parametricDatum.WithEnsemble(null);
        TemporalDatum temporalClone = temporalDatum.WithEnsemble(null);

        Assert.NotSame(engineeringDatum, engineeringClone);
        Assert.NotSame(parametricDatum, parametricClone);
        Assert.NotSame(temporalDatum, temporalClone);

        Assert.Throws<NotSupportedException>(() => engineeringDatum.WithEnsemble(ensemble));
        Assert.Throws<NotSupportedException>(() => parametricDatum.WithEnsemble(ensemble));
        Assert.Throws<NotSupportedException>(() => temporalDatum.WithEnsemble(ensemble));
    }

    /// <summary>
    /// Verifies that the typed coordinate-system <c>WithAuthority</c> overloads return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedCoordinateSystemWithAuthority_ReturnsConcreteClonesWithoutCasts()
    {
        GeographicCoordinateSystem geographicClone = GeographicCoordinateSystem.WGS84.WithAuthority("TEST", 3001);
        ProjectedCoordinateSystem projectedClone = ProjectedCoordinateSystem.WebMercator.WithAuthority("TEST", 3002);
        GeocentricCoordinateSystem geocentricClone = GeocentricCoordinateSystem.WGS84.WithAuthority("TEST", 3003);
        VerticalCoordinateSystem verticalClone = VerticalCoordinateSystem.ODN.WithAuthority("TEST", 3004);
        CompoundCoordinateSystem compoundClone = CreateTestCompoundCoordinateSystem().WithAuthority("TEST", 3005);
        BoundCoordinateSystem boundClone = CreateTestBoundCoordinateSystem().WithAuthority("TEST", 3006);
        FittedCoordinateSystem fittedClone = CreateTestFittedCoordinateSystem().WithAuthority("TEST", 3007);
        EngineeringCoordinateSystem engineeringClone = CreateTestEngineeringCoordinateSystem().WithAuthority("TEST", 3008);
        ParametricCoordinateSystem parametricClone = CreateTestParametricCoordinateSystem().WithAuthority("TEST", 3009);
        TemporalCoordinateSystem temporalClone = CreateTestTemporalCoordinateSystem().WithAuthority("TEST", 3010);

        Assert.Equal("TEST", geographicClone.Authority);
        Assert.Equal(3002, projectedClone.AuthorityCode);
        Assert.Equal("TEST", geocentricClone.Authority);
        Assert.Equal(3004, verticalClone.AuthorityCode);
        Assert.Equal("TEST", compoundClone.Authority);
        Assert.Equal(3006, boundClone.AuthorityCode);
        Assert.Equal("TEST", fittedClone.Authority);
        Assert.Equal(3008, engineeringClone.AuthorityCode);
        Assert.Equal("TEST", parametricClone.Authority);
        Assert.Equal(3010, temporalClone.AuthorityCode);
    }

    /// <summary>
    /// Verifies that the typed coordinate-system <c>WithName</c> overloads return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedCoordinateSystemWithName_ReturnsConcreteClonesWithoutCasts()
    {
        GeographicCoordinateSystem geographicClone = GeographicCoordinateSystem.WGS84.WithName("Geographic clone");
        ProjectedCoordinateSystem projectedClone = ProjectedCoordinateSystem.WebMercator.WithName("Projected clone");
        GeocentricCoordinateSystem geocentricClone = GeocentricCoordinateSystem.WGS84.WithName("Geocentric clone");
        VerticalCoordinateSystem verticalClone = VerticalCoordinateSystem.ODN.WithName("Vertical clone");
        CompoundCoordinateSystem compoundClone = CreateTestCompoundCoordinateSystem().WithName("Compound clone");
        BoundCoordinateSystem boundClone = CreateTestBoundCoordinateSystem().WithName("Bound clone");
        FittedCoordinateSystem fittedClone = CreateTestFittedCoordinateSystem().WithName("Fitted clone");
        EngineeringCoordinateSystem engineeringClone = CreateTestEngineeringCoordinateSystem().WithName("Engineering clone");
        ParametricCoordinateSystem parametricClone = CreateTestParametricCoordinateSystem().WithName("Parametric clone");
        TemporalCoordinateSystem temporalClone = CreateTestTemporalCoordinateSystem().WithName("Temporal clone");

        Assert.Equal("Geographic clone", geographicClone.Name);
        Assert.Equal("Projected clone", projectedClone.Name);
        Assert.Equal("Geocentric clone", geocentricClone.Name);
        Assert.Equal("Vertical clone", verticalClone.Name);
        Assert.Equal("Compound clone", compoundClone.Name);
        Assert.Equal("Bound clone", boundClone.Name);
        Assert.Equal("Fitted clone", fittedClone.Name);
        Assert.Equal("Engineering clone", engineeringClone.Name);
        Assert.Equal("Parametric clone", parametricClone.Name);
        Assert.Equal("Temporal clone", temporalClone.Name);
    }

    /// <summary>
    /// Verifies that the typed operation-model <c>WithAuthority</c> overloads return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedOperationWithAuthority_ReturnsConcreteClonesWithoutCasts()
    {
        Projection projectionClone = CreateTestProjection().WithAuthority("TEST", 4001);
        CoordinateOperation coordinateOperationClone = CreateTestCoordinateOperation().WithAuthority("TEST", 4002);
        ConcatenatedOperation concatenatedOperationClone = CreateTestConcatenatedOperation().WithAuthority("TEST", 4003);

        Assert.Equal("TEST", projectionClone.Authority);
        Assert.Equal(4002, coordinateOperationClone.AuthorityCode);
        Assert.Equal("TEST", concatenatedOperationClone.Authority);
    }

    /// <summary>
    /// Verifies that the typed operation-model <c>WithName</c> overloads return concrete clones without casts.
    /// </summary>
    [Fact]
    public void TypedOperationWithName_ReturnsConcreteClonesWithoutCasts()
    {
        Projection projectionClone = CreateTestProjection().WithName("Projection clone");
        CoordinateOperation coordinateOperationClone = CreateTestCoordinateOperation().WithName("Coordinate operation clone");
        ConcatenatedOperation concatenatedOperationClone = CreateTestConcatenatedOperation().WithName("Concatenated operation clone");

        Assert.Equal("Projection clone", projectionClone.Name);
        Assert.Equal("Coordinate operation clone", coordinateOperationClone.Name);
        Assert.Equal("Concatenated operation clone", concatenatedOperationClone.Name);
    }

    /// <summary>
    /// Verifies that base-typed <see cref="Info.WithAuthority"/> callers still use the generic fallback dispatch.
    /// </summary>
    [Fact]
    public void InfoWithAuthority_OnBaseTypedReference_UsesFallbackDispatch()
    {
        Info info = CreateTestBoundCoordinateSystem();

        Info clone = info.WithAuthority("TEST", 5001);

        BoundCoordinateSystem typedClone = Assert.IsType<BoundCoordinateSystem>(clone);
        Assert.Equal("TEST", typedClone.Authority);
        Assert.Equal(5001, typedClone.AuthorityCode);
    }

    /// <summary>
    /// Verifies that base-typed <see cref="Info.WithName"/> callers still use the generic fallback dispatch.
    /// </summary>
    [Fact]
    public void InfoWithName_OnBaseTypedReference_UsesFallbackDispatch()
    {
        Info info = CreateTestConcatenatedOperation();

        Info clone = info.WithName("Fallback clone");

        Assert.Equal("Fallback clone", Assert.IsType<ConcatenatedOperation>(clone).Name);
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXml"/> includes the supported metadata attributes in the expected order.
    /// </summary>
    [Fact]
    public void InfoXml_WithMetadata_IncludesExpectedAttributes()
    {
        var info = new TestInfo(
            "WGS 84",
            "EPSG",
            4326,
            "alias",
            "abbr",
            "remarks",
            "WKT",
            "XML");

        Assert.Equal(
            "<CS_Info AuthorityCode=\"4326\" Abbreviation=\"abbr\" Authority=\"EPSG\" Name=\"WGS 84\"/>",
            info.InfoXml);
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXml"/> omits optional attributes when the values are blank or not positive.
    /// </summary>
    [Fact]
    public void InfoXml_WithBlankMetadata_OmitsOptionalAttributes()
    {
        var info = new TestInfo(
            " ",
            "\t",
            0,
            "alias",
            string.Empty,
            "remarks",
            "WKT",
            "XML");

        Assert.Equal("<CS_Info/>", info.InfoXml);
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXmlElement"/> includes the expected attributes and values.
    /// </summary>
    [Fact]
    public void InfoXmlElement_WithMetadata_IncludesExpectedAttributes()
    {
        var info = new TestInfo(
            "WGS 84",
            "EPSG",
            4326,
            "alias",
            "abbr",
            "remarks",
            "WKT",
            "XML");

        XElement xml = info.InfoXmlElement;

        Assert.Equal("CS_Info", xml.Name.LocalName);
        Assert.Equal("4326", (string?)xml.Attribute("AuthorityCode"));
        Assert.Equal("abbr", (string?)xml.Attribute("Abbreviation"));
        Assert.Equal("EPSG", (string?)xml.Attribute("Authority"));
        Assert.Equal("WGS 84", (string?)xml.Attribute("Name"));
    }

    /// <summary>
    /// Verifies that <see cref="Info.InfoXmlElement"/> omits optional attributes when the values are blank or not positive.
    /// </summary>
    [Fact]
    public void InfoXmlElement_WithBlankMetadata_OmitsOptionalAttributes()
    {
        var info = new TestInfo(
            " ",
            "\t",
            -1,
            "alias",
            string.Empty,
            "remarks",
            "WKT",
            "XML");

        XElement xml = info.InfoXmlElement;

        Assert.Empty(xml.Attributes());
    }

    private static DatumEnsemble CreateTestEnsemble(string name, Ellipsoid? ellipsoid = null)
    {
        return new DatumEnsemble(
            name,
            [
                new DatumEnsembleMember("Member A"),
                new DatumEnsembleMember("Member B"),
            ],
            0.25d,
            ellipsoid,
            "TEST",
            1);
    }

    private static CompoundCoordinateSystem CreateTestCompoundCoordinateSystem()
    {
        return new CompoundCoordinateSystem(
            GeographicCoordinateSystem.WGS84,
            VerticalCoordinateSystem.ODN,
            "Custom compound",
            "EPSG",
            9900,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static BoundCoordinateSystem CreateTestBoundCoordinateSystem()
    {
        return new BoundCoordinateSystem(
            CreateTestGeographicCoordinateSystem("Source"),
            GeographicCoordinateSystem.WGS84,
            new BoundTransformation("Geocentric translations", new Wgs84ConversionInfo(1, 2, 3, 0, 0, 0, 0)),
            "Bound source",
            "EPSG",
            9901,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static FittedCoordinateSystem CreateTestFittedCoordinateSystem()
    {
        return new FittedCoordinateSystem(
            CreateTestGeographicCoordinateSystem("Base geographic"),
            new AffineTransform(1, 0, 10, 0, 1, 20),
            "Custom fitted",
            "EPSG",
            9902,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static EngineeringCoordinateSystem CreateTestEngineeringCoordinateSystem()
    {
        return new EngineeringCoordinateSystem(
            new EngineeringDatum("Local plant", "EPSG", 1098, string.Empty, string.Empty, string.Empty),
            "Cartesian",
            [new AxisInfo("x", AxisOrientationEnum.East), new AxisInfo("y", AxisOrientationEnum.North)],
            [LinearUnit.Metre, LinearUnit.Metre],
            "Plant grid",
            "EPSG",
            5800,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ParametricCoordinateSystem CreateTestParametricCoordinateSystem()
    {
        return new ParametricCoordinateSystem(
            new ParametricUnit(0.1d, "pressure", "EPSG", 0, string.Empty, string.Empty, string.Empty),
            new ParametricDatum("Reservoir datum", "EPSG", 0, string.Empty, string.Empty, string.Empty),
            new AxisInfo("pressure", AxisOrientationEnum.Up),
            "Reservoir pressure",
            "EPSG",
            0,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static TemporalCoordinateSystem CreateTestTemporalCoordinateSystem()
    {
        return new TemporalCoordinateSystem(
            new TimeUnit(1d, "second", "EPSG", 1040, string.Empty, string.Empty, string.Empty),
            new TemporalDatum("1950-01-01T00:00:00Z", "Unix epoch", "EPSG", 1040, string.Empty, string.Empty, string.Empty),
            new AxisInfo("time", AxisOrientationEnum.Other),
            "Temporal axis",
            "EPSG",
            1041,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static Projection CreateTestProjection()
    {
        return new Projection(
            "Transverse_Mercator",
            [new ProjectionParameter("latitude_of_origin", 0d)],
            "Transverse Mercator",
            "EPSG",
            9807,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CoordinateOperation CreateTestCoordinateOperation(string name = "Test operation")
    {
        return new CoordinateOperation(
            "Axis order reversal",
            [new Parameter("Order", 1d)],
            CreateTestGeographicCoordinateSystem("Operation source"),
            CreateTestGeographicCoordinateSystem("Operation target"),
            name,
            "EPSG",
            9603,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ConcatenatedOperation CreateTestConcatenatedOperation()
    {
        return new ConcatenatedOperation(
            [CreateTestCoordinateOperation("Step 1"), CreateTestCoordinateOperation("Step 2")],
            CreateTestGeographicCoordinateSystem("Concatenated source"),
            CreateTestGeographicCoordinateSystem("Concatenated target"),
            "Test concatenated operation",
            "EPSG",
            9610,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static GeographicCoordinateSystem CreateTestGeographicCoordinateSystem(string name)
    {
        return new GeographicCoordinateSystem(
            AngularUnit.Degrees,
            HorizontalDatum.WGS84,
            PrimeMeridian.Greenwich,
            [new AxisInfo("Lon", AxisOrientationEnum.East), new AxisInfo("Lat", AxisOrientationEnum.North)],
            name,
            "EPSG",
            4326,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private sealed class TestInfo : Info
    {
        private readonly string wkt;
        private readonly string xml;

        internal TestInfo(
            string name,
            string authority,
            long code,
            string alias,
            string abbreviation,
            string remarks,
            string wkt,
            string xml)
            : base(name, authority, code, alias, abbreviation, remarks)
        {
            this.wkt = wkt;
            this.xml = xml;
        }

        public override string WKT => this.wkt;

        public override string XML => this.xml;

        public override bool EqualParams(object obj) => ReferenceEquals(this, obj);
    }
}
