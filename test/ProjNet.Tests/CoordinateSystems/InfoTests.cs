// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
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
        LinearUnit clone = Assert.IsType<LinearUnit>(original.WithAuthority("TEST", 1234));

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
        LinearUnit clone = Assert.IsType<LinearUnit>(original.WithName("Meter"));

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
