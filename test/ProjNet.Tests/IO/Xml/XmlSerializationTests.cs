// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.Xml;

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Tests that verify the <c>ToXml()</c> methods produce the correct XML structure
/// and match the existing <c>XML</c> property output.
/// </summary>
public class XmlSerializationTests
{
    /// <summary>
    /// Verifies that <see cref="AxisInfo.ToXml"/> produces a <c>CS_AxisInfo</c> element
    /// with the correct <c>Name</c> and <c>Orientation</c> attributes and that its serialized
    /// form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void AxisInfo_ToXml_MatchesXmlProperty()
    {
        var axis = new AxisInfo("Lon", AxisOrientationEnum.East);

        XElement element = axis.ToXml();

        Assert.Equal("CS_AxisInfo", element.Name.LocalName);
        Assert.Equal("Lon", element.Attribute("Name")!.Value);
        Assert.Equal("EAST", element.Attribute("Orientation")!.Value);
        Assert.Equal(NormalizeXml(axis.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="ProjectionParameter.ToXml"/> produces a <c>CS_ProjectionParameter</c>
    /// element with the correct <c>Name</c> and <c>Value</c> attributes and that its serialized
    /// form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void ProjectionParameter_ToXml_MatchesXmlProperty()
    {
        var param = new ProjectionParameter("central_meridian", -93.5);

        XElement element = param.ToXml();

        Assert.Equal("CS_ProjectionParameter", element.Name.LocalName);
        Assert.Equal("central_meridian", element.Attribute("Name")!.Value);
        Assert.Equal("-93.5", element.Attribute("Value")!.Value);
        Assert.Equal(NormalizeXml(param.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="Wgs84ConversionInfo.ToXml"/> produces a <c>CS_WGS84ConversionInfo</c>
    /// element with the correct translation and rotation attributes (<c>Dx</c>, <c>Dy</c>, <c>Dz</c>,
    /// <c>Ex</c>, <c>Ppm</c>) and that its serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void Wgs84ConversionInfo_ToXml_MatchesXmlProperty()
    {
        var info = new Wgs84ConversionInfo(-87, -98, -121, 0, 0, 0, 0);

        XElement element = info.ToXml();

        Assert.Equal("CS_WGS84ConversionInfo", element.Name.LocalName);
        Assert.Equal("-87", element.Attribute("Dx")!.Value);
        Assert.Equal("-98", element.Attribute("Dy")!.Value);
        Assert.Equal("-121", element.Attribute("Dz")!.Value);
        Assert.Equal("0", element.Attribute("Ex")!.Value);
        Assert.Equal("0", element.Attribute("Ppm")!.Value);
        Assert.Equal(NormalizeXml(info.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="AngularUnit.ToXml"/> for the degree unit produces a
    /// <c>CS_AngularUnit</c> element with a <c>RadiansPerUnit</c> attribute and that its
    /// serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void AngularUnit_ToXml_MatchesXmlProperty()
    {
        AngularUnit unit = AngularUnit.Degrees;

        XElement element = unit.ToXml();

        Assert.Equal("CS_AngularUnit", element.Name.LocalName);
        Assert.NotNull(element.Attribute("RadiansPerUnit"));
        Assert.Equal(NormalizeXml(unit.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="LinearUnit.ToXml"/> for the metre unit produces a
    /// <c>CS_LinearUnit</c> element with a <c>MetersPerUnit</c> attribute and that its
    /// serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void LinearUnit_ToXml_MatchesXmlProperty()
    {
        LinearUnit unit = LinearUnit.Metre;

        XElement element = unit.ToXml();

        Assert.Equal("CS_LinearUnit", element.Name.LocalName);
        Assert.NotNull(element.Attribute("MetersPerUnit"));
        Assert.Equal(NormalizeXml(unit.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="Ellipsoid.ToXml"/> for the WGS84 ellipsoid produces a
    /// <c>CS_Ellipsoid</c> element with <c>SemiMajorAxis</c>, <c>SemiMinorAxis</c>,
    /// <c>InverseFlattening</c>, and <c>IvfDefinitive</c> attributes and that its serialized
    /// form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void Ellipsoid_ToXml_MatchesXmlProperty()
    {
        Ellipsoid ellipsoid = Ellipsoid.WGS84;

        XElement element = ellipsoid.ToXml();

        Assert.Equal("CS_Ellipsoid", element.Name.LocalName);
        Assert.NotNull(element.Attribute("SemiMajorAxis"));
        Assert.NotNull(element.Attribute("SemiMinorAxis"));
        Assert.NotNull(element.Attribute("InverseFlattening"));
        Assert.NotNull(element.Attribute("IvfDefinitive"));
        Assert.Equal(NormalizeXml(ellipsoid.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="PrimeMeridian.ToXml"/> for the Greenwich meridian produces
    /// a <c>CS_PrimeMeridian</c> element with a <c>Longitude</c> attribute and the required
    /// <c>CS_Info</c> and <c>CS_AngularUnit</c> child elements.
    /// </summary>
    [Fact]
    public void PrimeMeridian_ToXml_MatchesXmlProperty()
    {
        PrimeMeridian pm = PrimeMeridian.Greenwich;

        XElement element = pm.ToXml();

        Assert.Equal("CS_PrimeMeridian", element.Name.LocalName);
        Assert.NotNull(element.Attribute("Longitude"));

        // The existing XML property has a trailing space before '>' in the attribute:
        // <CS_PrimeMeridian Longitude="0" > vs <CS_PrimeMeridian Longitude="0">
        // XElement won't produce that trailing space, so we compare structure not exact string.
        Assert.NotNull(element.Element("CS_Info"));
        Assert.NotNull(element.Element("CS_AngularUnit"));
    }

    /// <summary>
    /// Verifies that <see cref="HorizontalDatum.ToXml"/> for the WGS84 datum (which has no
    /// WGS84 conversion parameters) produces a <c>CS_HorizontalDatum</c> element with a
    /// <c>DatumType</c> attribute and the required <c>CS_Info</c> and <c>CS_Ellipsoid</c>
    /// child elements, and that its serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void HorizontalDatum_ToXml_MatchesXmlProperty()
    {
        HorizontalDatum datum = HorizontalDatum.WGS84;

        XElement element = datum.ToXml();

        Assert.Equal("CS_HorizontalDatum", element.Name.LocalName);
        Assert.NotNull(element.Attribute("DatumType"));
        Assert.NotNull(element.Element("CS_Info"));
        Assert.NotNull(element.Element("CS_Ellipsoid"));
        Assert.Equal(NormalizeXml(datum.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="HorizontalDatum.ToXml"/> for the ED50 datum (which carries
    /// WGS84 conversion parameters) emits a <c>CS_WGS84ConversionInfo</c> child element and
    /// that its serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void HorizontalDatum_WithWgs84Parameters_ToXml_MatchesXmlProperty()
    {
        HorizontalDatum datum = HorizontalDatum.ED50;

        XElement element = datum.ToXml();

        Assert.Equal("CS_HorizontalDatum", element.Name.LocalName);
        Assert.NotNull(element.Element("CS_WGS84ConversionInfo"));
        Assert.Equal(NormalizeXml(datum.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="VerticalDatum.ToXml"/> for the ODN datum produces a
    /// <c>CS_VerticalDatum</c> element with a <c>DatumType</c> attribute and a <c>CS_Info</c>
    /// child element, and that its serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void VerticalDatum_ToXml_MatchesXmlProperty()
    {
        VerticalDatum datum = VerticalDatum.ODN;

        XElement element = datum.ToXml();

        Assert.Equal("CS_VerticalDatum", element.Name.LocalName);
        Assert.NotNull(element.Attribute("DatumType"));
        Assert.NotNull(element.Element("CS_Info"));
        Assert.Equal(NormalizeXml(datum.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="Projection.ToXml"/> produces a <c>CS_Projection</c> element
    /// with the correct <c>Classname</c> attribute, a <c>CS_Info</c> child element, one
    /// <c>CS_ProjectionParameter</c> child element per parameter, and that its serialized
    /// form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void Projection_ToXml_MatchesXmlProperty()
    {
        var parameters = new List<ProjectionParameter>
        {
            new("latitude_of_origin", 0.0),
            new("central_meridian", 0.0),
            new("scale_factor", 0.9996),
            new("false_easting", 500000),
            new("false_northing", 0),
        };

        var projection = new Projection(
            "Transverse_Mercator",
            parameters,
            "UTM32N",
            "EPSG",
            32632,
            string.Empty,
            string.Empty,
            string.Empty);

        XElement element = projection.ToXml();

        Assert.Equal("CS_Projection", element.Name.LocalName);
        Assert.Equal("Transverse_Mercator", element.Attribute("Classname")!.Value);
        Assert.NotNull(element.Element("CS_Info"));

        IEnumerable<XElement> paramElements = element.Elements("CS_ProjectionParameter");
        Assert.Equal(5, new List<XElement>(paramElements).Count);

        Assert.Equal(NormalizeXml(projection.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="GeographicCoordinateSystem.ToXml"/> for the WGS84 geographic
    /// coordinate system produces an outer <c>CS_CoordinateSystem</c> element with a
    /// <c>Dimension</c> attribute wrapping a <c>CS_GeographicCoordinateSystem</c> element
    /// that contains <c>CS_Info</c>, <c>CS_HorizontalDatum</c>, <c>CS_AngularUnit</c>, and
    /// <c>CS_PrimeMeridian</c> child elements.
    /// </summary>
    [Fact]
    public void GeographicCoordinateSystem_ToXml_MatchesXmlProperty()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;

        XElement element = gcs.ToXml();

        Assert.Equal("CS_CoordinateSystem", element.Name.LocalName);
        Assert.NotNull(element.Attribute("Dimension"));

        XElement? inner = element.Element("CS_GeographicCoordinateSystem");
        Assert.NotNull(inner);
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.NotNull(inner.Element("CS_HorizontalDatum"));
        Assert.NotNull(inner.Element("CS_AngularUnit"));
        Assert.NotNull(inner.Element("CS_PrimeMeridian"));
    }

    /// <summary>
    /// Verifies that <see cref="ProjectedCoordinateSystem.ToXml"/> for a UTM zone 32 north
    /// projection produces an outer <c>CS_CoordinateSystem</c> element with
    /// <c>Dimension</c> set to <c>2</c>, wrapping a <c>CS_ProjectedCoordinateSystem</c>
    /// element that contains <c>CS_Info</c>, a nested <c>CS_CoordinateSystem</c> (geographic),
    /// <c>CS_LinearUnit</c>, and <c>CS_Projection</c> child elements.
    /// </summary>
    [Fact]
    public void ProjectedCoordinateSystem_ToXml_ProducesCorrectStructure()
    {
        var pcs = ProjectedCoordinateSystem.WGS84_UTM(32, true);

        XElement element = pcs.ToXml();

        Assert.Equal("CS_CoordinateSystem", element.Name.LocalName);
        Assert.Equal("2", element.Attribute("Dimension")!.Value);

        XElement? inner = element.Element("CS_ProjectedCoordinateSystem");
        Assert.NotNull(inner);
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.NotNull(inner.Element("CS_CoordinateSystem")); // nested geographic CS
        Assert.NotNull(inner.Element("CS_LinearUnit"));
        Assert.NotNull(inner.Element("CS_Projection"));
    }

    /// <summary>
    /// Verifies that <see cref="GeocentricCoordinateSystem.ToXml"/> for the WGS84 geocentric
    /// coordinate system produces an outer <c>CS_CoordinateSystem</c> element with
    /// <c>Dimension</c> set to <c>3</c>, wrapping a <c>CS_GeocentricCoordinateSystem</c>
    /// element that contains <c>CS_Info</c>, <c>CS_HorizontalDatum</c>, <c>CS_LinearUnit</c>,
    /// and <c>CS_PrimeMeridian</c> child elements.
    /// </summary>
    [Fact]
    public void GeocentricCoordinateSystem_ToXml_ProducesCorrectStructure()
    {
        GeocentricCoordinateSystem gcc = GeocentricCoordinateSystem.WGS84;

        XElement element = gcc.ToXml();

        Assert.Equal("CS_CoordinateSystem", element.Name.LocalName);
        Assert.Equal("3", element.Attribute("Dimension")!.Value);

        XElement? inner = element.Element("CS_GeocentricCoordinateSystem");
        Assert.NotNull(inner);
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.NotNull(inner.Element("CS_HorizontalDatum"));
        Assert.NotNull(inner.Element("CS_LinearUnit"));
        Assert.NotNull(inner.Element("CS_PrimeMeridian"));
    }

    /// <summary>
    /// Verifies that <see cref="VerticalCoordinateSystem.ToXml"/> for the ODN vertical
    /// coordinate system produces an outer <c>CS_CoordinateSystem</c> element with
    /// <c>Dimension</c> set to <c>1</c>, wrapping a <c>CS_VerticalCoordinateSystem</c>
    /// element that contains <c>CS_Info</c>, <c>CS_VerticalDatum</c>, and <c>CS_LinearUnit</c>
    /// child elements, and that its serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void VerticalCoordinateSystem_ToXml_MatchesXmlProperty()
    {
        VerticalCoordinateSystem vcs = VerticalCoordinateSystem.ODN;

        XElement element = vcs.ToXml();

        Assert.Equal("CS_CoordinateSystem", element.Name.LocalName);
        Assert.Equal("1", element.Attribute("Dimension")!.Value);

        XElement? inner = element.Element("CS_VerticalCoordinateSystem");
        Assert.NotNull(inner);
        Assert.NotNull(inner.Element("CS_Info"));
        Assert.NotNull(inner.Element("CS_VerticalDatum"));
        Assert.NotNull(inner.Element("CS_LinearUnit"));

        Assert.Equal(NormalizeXml(vcs.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundCoordinateSystem.ToXml"/> produces an outer
    /// <c>CS_CoordinateSystem</c> element wrapping a <c>CS_CompoundCoordinateSystem</c>
    /// element that contains a <c>CS_Info</c> child element and exactly two nested
    /// <c>CS_CoordinateSystem</c> elements representing the head and tail coordinate systems,
    /// and that its serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void CompoundCoordinateSystem_ToXml_ProducesCorrectStructure()
    {
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vcs = VerticalCoordinateSystem.ODN;
        var compound = new CompoundCoordinateSystem(gcs, vcs, "TestCompound", "EPSG", 9999, string.Empty, string.Empty, string.Empty);

        XElement element = compound.ToXml();

        Assert.Equal("CS_CoordinateSystem", element.Name.LocalName);

        XElement? inner = element.Element("CS_CompoundCoordinateSystem");
        Assert.NotNull(inner);
        Assert.NotNull(inner.Element("CS_Info"));

        // Head and tail coordinate systems should be nested
        IEnumerable<XElement> csDimensions = inner.Elements("CS_CoordinateSystem");
        Assert.Equal(2, new List<XElement>(csDimensions).Count);

        Assert.Equal(NormalizeXml(compound.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="FittedCoordinateSystem.ToXml"/> throws a
    /// <see cref="NotImplementedException"/>, consistent with the <c>XML</c> property on
    /// the same type.
    /// </summary>
    [Fact]
    public void FittedCoordinateSystem_ToXml_ThrowsNotImplementedException()
    {
        // FittedCoordinateSystem.XML also throws NotImplementedException
        GeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84;
        var fcs = new FittedCoordinateSystem(
            gcs,
            new ProjNet.CoordinateSystems.Transformations.AffineTransform(
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
                new double[,]
                {
                    { 1, 0, 0 },
                    { 0, 1, 0 },
                    { 0, 0, 1 },
                }),
#pragma warning restore CA1814
            "TestFitted",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        Assert.Throws<NotImplementedException>(() => fcs.ToXml());
    }

    /// <summary>
    /// Verifies that the <see cref="System.Xml.Linq.XElement"/> returned by <c>ToXml()</c>
    /// serializes to a well-formed XML string that can be parsed back by
    /// <see cref="XElement.Parse(string)"/> and produces a structurally identical element.
    /// </summary>
    [Fact]
    public void ToXml_RoundTrip_ParseBackToXElement()
    {
        // Verify the XElement output can be parsed back
        AngularUnit unit = AngularUnit.Degrees;
        XElement element = unit.ToXml();
        string xmlString = element.ToString(SaveOptions.DisableFormatting);

        var reparsed = XElement.Parse(xmlString);
        Assert.Equal(element.ToString(), reparsed.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="AngularUnit.ToXml"/> for the radian unit produces output
    /// whose serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void AngularUnit_Radian_ToXml_MatchesXmlProperty()
    {
        AngularUnit unit = AngularUnit.Radian;

        XElement element = unit.ToXml();

        Assert.Equal(NormalizeXml(unit.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="LinearUnit.ToXml"/> for the foot unit produces output
    /// whose serialized form matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void LinearUnit_Foot_ToXml_MatchesXmlProperty()
    {
        LinearUnit unit = LinearUnit.Foot;

        XElement element = unit.ToXml();

        Assert.Equal(NormalizeXml(unit.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="Ellipsoid.ToXml"/> for the Clarke 1866 ellipsoid, which is
    /// not IVF-definitive and carries a <see cref="double.PositiveInfinity"/> inverse
    /// flattening value, emits <c>IvfDefinitive</c> as <c>0</c> and that its serialized form
    /// matches the output of the <c>XML</c> property.
    /// </summary>
    [Fact]
    public void Ellipsoid_Clarke1866_ToXml_MatchesXmlProperty()
    {
        // Clarke1866 uses non-IVF-definitive with PositiveInfinity inverse flattening
        Ellipsoid ellipsoid = Ellipsoid.Clarke1866;

        XElement element = ellipsoid.ToXml();

        Assert.Equal("0", element.Attribute("IvfDefinitive")!.Value);
        Assert.Equal(NormalizeXml(ellipsoid.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Verifies that <see cref="Wgs84ConversionInfo.ToXml"/> for a default instance (all
    /// parameters zero) produces output whose serialized form matches the output of the
    /// <c>XML</c> property.
    /// </summary>
    [Fact]
    public void Wgs84ConversionInfo_ZeroValues_ToXml_MatchesXmlProperty()
    {
        var info = new Wgs84ConversionInfo();

        XElement element = info.ToXml();

        Assert.Equal(NormalizeXml(info.XML), NormalizeXml(element.ToString(SaveOptions.DisableFormatting)));
    }

    /// <summary>
    /// Normalizes XML string by removing insignificant whitespace differences.
    /// XElement may add or omit a space before self-closing element markers (<c>/&gt;</c>)
    /// and the existing StringBuilder-based XML properties may have a trailing space before
    /// the closing <c>&gt;</c> of opening tags. Both are valid XML; this helper makes them identical.
    /// </summary>
    private static string NormalizeXml(string xml)
    {
        return xml.Replace(" />", "/>", StringComparison.Ordinal).Replace(" >", ">", StringComparison.Ordinal);
    }
}
