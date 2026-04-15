// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Verifies invertibility discovery through <see cref="MathTransform.IsInvertible"/>.
/// </summary>
public class MathTransformInvertibilityTests
{
    /// <summary>
    /// Verifies Mercator projections report inverse support.
    /// </summary>
    [Fact]
    public void MercatorProjectionReportsInvertible()
    {
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(
            ProjectionsRegistry.CreateProjection("mercator", CreateMercatorParameters()));

        Assert.True(projection.IsInvertible);
        Assert.IsAssignableFrom<MathTransform>(projection.Inverse());
    }

    /// <summary>
    /// Verifies that Mercator WKT formatting preserves the expected forward and inverse WKT shape.
    /// </summary>
    [Fact]
    public void MercatorProjectionWkt_MatchesForwardAndInverseReferenceStrings()
    {
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(
            ProjectionsRegistry.CreateProjection("mercator", CreateMercatorParameters()));
        MapProjection inverse = Assert.IsAssignableFrom<MapProjection>(projection.Inverse());

        Assert.Equal(CreateExpectedProjectionWkt(projection), projection.WKT);
        Assert.Equal(CreateExpectedProjectionWkt(inverse), inverse.WKT);
    }

    /// <summary>
    /// Verifies that Mercator XML formatting preserves the expected forward and inverse XML shape.
    /// </summary>
    [Fact]
    public void MercatorProjectionXml_MatchesForwardAndInverseReferenceStrings()
    {
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(
            ProjectionsRegistry.CreateProjection("mercator", CreateMercatorParameters()));
        MapProjection inverse = Assert.IsAssignableFrom<MapProjection>(projection.Inverse());

        Assert.Equal(CreateExpectedProjectionXml(projection), projection.XML);
        Assert.Equal(CreateExpectedProjectionXml(inverse), inverse.XML);
        Assert.True(XNode.DeepEquals(XElement.Parse(CreateExpectedProjectionXml(projection)), projection.ToXml()));
        Assert.True(XNode.DeepEquals(XElement.Parse(CreateExpectedProjectionXml(inverse)), inverse.ToXml()));
    }

    /// <summary>
    /// Verifies forward-only Airy projections report missing inverse support without requiring capability probes by exception.
    /// </summary>
    [Fact]
    public void AiryProjectionReportsNotInvertibleAndInverseThrows()
    {
        MapProjection projection = Assert.IsAssignableFrom<MapProjection>(
            ProjectionsRegistry.CreateProjection("airy", CreateAiryParameters()));

        Assert.False(projection.IsInvertible);
        Assert.Throws<NotSupportedException>(() => projection.Inverse());
    }

    /// <summary>
    /// Verifies non-projection math transforms keep reporting inverse support.
    /// </summary>
    [Fact]
    public void HelmertTransformReportsInvertible()
    {
        MathTransform transform = CreateTransform("+proj=helmert +convention=coordinate_frame +x=0.67678 +y=0.65495 +z=-0.52827 +rx=-0.022742 +ry=0.012667 +rz=0.022704 +s=-0.01070");

        Assert.True(transform.IsInvertible);
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

    private static List<ProjectionParameter> CreateAiryParameters()
    {
        return
        [
            new("semi_major", 6400000d),
            new("semi_minor", 6400000d),
            new("central_meridian", 0d),
            new("latitude_of_origin", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
            new("unit", 1d),
        ];
    }

    private static string CreateExpectedProjectionWkt(MapProjection projection)
    {
        string parameterizedWkt = "PARAM_MT[\"" + projection.Name + "\"" +
            string.Concat(Enumerable.Range(0, projection.NumParameters).Select(i => ", " + projection.GetParameter(i).WKT)) +
            "]";
        return projection.IsInverse
            ? "INVERSE_MT[" + parameterizedWkt + "]"
            : parameterizedWkt;
    }

    private static string CreateExpectedProjectionXml(MapProjection projection)
    {
        string transformElementName = projection.IsInverse
            ? "CT_InverseTransform"
            : "CT_ParameterizedMathTransform";
        return "<CT_MathTransform><" + transformElementName + " Name=\"" + projection.ClassName + "\">" +
            string.Concat(Enumerable.Range(0, projection.NumParameters).Select(i => projection.GetParameter(i).ToXml().ToString(SaveOptions.DisableFormatting))) +
            "</" + transformElementName + "></CT_MathTransform>";
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = ProjPipelineMathTransformFactory.TryCreateMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }
}
