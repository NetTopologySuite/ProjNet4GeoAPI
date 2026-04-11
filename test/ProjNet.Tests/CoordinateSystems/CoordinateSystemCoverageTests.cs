// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Coverage tests for coordinate system classes with low coverage:
/// <see cref="GeocentricCoordinateSystem"/>, <see cref="CompoundCoordinateSystem"/>,
/// <see cref="FittedCoordinateSystem"/>, <see cref="PrimeMeridian"/>,
/// <see cref="Info"/>, <see cref="ParameterInfo"/>, and <see cref="CoordinateSystemServices"/>.
/// </summary>
public class CoordinateSystemCoverageTests
{
    private static readonly CoordinateSystemFactory Factory = new();

    // ========================================================================
    // GeocentricCoordinateSystem
    // ========================================================================

    /// <summary>
    /// Verifies that the factory creates a geocentric system with 3 dimensions.
    /// </summary>
    [Fact]
    public void GeocentricCS_Factory_DimensionIsThree()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.Equal(3, gcs.Dimension);
    }

    /// <summary>
    /// Verifies that the factory assigns the specified name.
    /// </summary>
    [Fact]
    public void GeocentricCS_Factory_NameIsAssigned()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "TestGeocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.Equal("TestGeocentric", gcs.Name);
    }

    /// <summary>
    /// Verifies that properties are accessible after factory creation.
    /// </summary>
    [Fact]
    public void GeocentricCS_Factory_PropertiesAccessible()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.True(gcs.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
        Assert.True(gcs.LinearUnit.EqualParams(LinearUnit.Metre));
        Assert.True(gcs.PrimeMeridian.EqualParams(PrimeMeridian.Greenwich));
    }

    /// <summary>
    /// Verifies that WKT output starts with GEOCCS and contains the name.
    /// </summary>
    [Fact]
    public void GeocentricCS_WKT_ContainsGeoccsAndName()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.StartsWith("GEOCCS[\"WGS84 Geocentric\"", gcs.WKT, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that WKT output contains datum, unit, and prime meridian.
    /// </summary>
    [Fact]
    public void GeocentricCS_WKT_ContainsSubComponents()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        string wkt = gcs.WKT;

        Assert.Contains("DATUM[", wkt, StringComparison.Ordinal);
        Assert.Contains("PRIMEM[", wkt, StringComparison.Ordinal);
        Assert.Contains("UNIT[", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML output contains the expected element tags.
    /// </summary>
    [Fact]
    public void GeocentricCS_XML_ContainsExpectedElements()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        string xml = gcs.XML;

        Assert.Contains("CS_GeocentricCoordinateSystem", xml, StringComparison.Ordinal);
        Assert.Contains("CS_HorizontalDatum", xml, StringComparison.Ordinal);
        Assert.Contains("CS_LinearUnit", xml, StringComparison.Ordinal);
        Assert.Contains("CS_PrimeMeridian", xml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML output contains the dimension attribute set to 3.
    /// </summary>
    [Fact]
    public void GeocentricCS_XML_ContainsDimension()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.Contains("Dimension=\"3\"", gcs.XML, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that GetUnits returns the linear unit for all three dimensions.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GeocentricCS_GetUnits_ReturnsLinearUnit(int dimension)
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        IUnit unit = gcs.GetUnits(dimension);

        Assert.IsType<LinearUnit>(unit);
        Assert.True(gcs.LinearUnit.EqualParams(unit));
    }

    /// <summary>
    /// Verifies that EqualParams returns true for an equivalent system.
    /// </summary>
    [Fact]
    public void GeocentricCS_EqualParams_EquivalentSystems_ReturnsTrue()
    {
        GeocentricCoordinateSystem a = Factory.CreateGeocentricCoordinateSystem(
            "GCS1", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        GeocentricCoordinateSystem b = Factory.CreateGeocentricCoordinateSystem(
            "GCS2", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different linear unit.
    /// </summary>
    [Fact]
    public void GeocentricCS_EqualParams_DifferentUnit_ReturnsFalse()
    {
        GeocentricCoordinateSystem a = Factory.CreateGeocentricCoordinateSystem(
            "GCS1", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        GeocentricCoordinateSystem b = Factory.CreateGeocentricCoordinateSystem(
            "GCS2", HorizontalDatum.WGS84, LinearUnit.Foot, PrimeMeridian.Greenwich);

        Assert.False(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void GeocentricCS_EqualParams_DifferentType_ReturnsFalse()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.False(gcs.EqualParams("not a CS"));
    }

    /// <summary>
    /// Verifies that the static WGS84 property returns a valid geocentric system.
    /// </summary>
    [Fact]
    public void GeocentricCS_WGS84_StaticProperty_IsValid()
    {
        GeocentricCoordinateSystem wgs84 = GeocentricCoordinateSystem.WGS84;

        Assert.NotNull(wgs84);
        Assert.Equal(3, wgs84.Dimension);
        Assert.True(wgs84.HorizontalDatum.EqualParams(HorizontalDatum.WGS84));
    }

    /// <summary>
    /// Verifies WKT round-trip for a geocentric coordinate system.
    /// </summary>
    [Fact]
    public void GeocentricCS_WKT_RoundTrip()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        string wkt = gcs.WKT;

        var parsed = Factory.CreateFromWkt(wkt) as GeocentricCoordinateSystem;

        Assert.NotNull(parsed);
        Assert.True(gcs.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that ToString returns WKT.
    /// </summary>
    [Fact]
    public void GeocentricCS_ToString_ReturnsWKT()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        Assert.Equal(gcs.WKT, gcs.ToString());
    }

    /// <summary>
    /// Verifies that property setters update the values.
    /// </summary>
    [Fact]
    public void GeocentricCS_PropertySetters_UpdateValues()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);

        gcs.LinearUnit = LinearUnit.Foot;
        gcs.PrimeMeridian = PrimeMeridian.Paris;

        Assert.True(gcs.LinearUnit.EqualParams(LinearUnit.Foot));
        Assert.True(gcs.PrimeMeridian.EqualParams(PrimeMeridian.Paris));
    }

    /// <summary>
    /// Verifies that DefaultEnvelope can be set and retrieved.
    /// </summary>
    [Fact]
    public void GeocentricCS_DefaultEnvelope_CanBeSetAndRetrieved()
    {
        GeocentricCoordinateSystem gcs = Factory.CreateGeocentricCoordinateSystem(
            "WGS84 Geocentric", HorizontalDatum.WGS84, LinearUnit.Metre, PrimeMeridian.Greenwich);
        double[] envelope = new double[] { -180, -90, 180, 90 };

        gcs.DefaultEnvelope = envelope;

        Assert.Equal(envelope, gcs.DefaultEnvelope);
    }

    // ========================================================================
    // CompoundCoordinateSystem
    // ========================================================================

    /// <summary>
    /// Verifies that a compound system has the combined dimension of head and tail.
    /// </summary>
    [Fact]
    public void CompoundCS_Dimension_IsSumOfComponents()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);

        Assert.Equal(geoCs.Dimension + vertCs.Dimension, compound.Dimension);
    }

    /// <summary>
    /// Verifies that HeadCoordinateSystem returns the first component.
    /// </summary>
    [Fact]
    public void CompoundCS_HeadCoordinateSystem_ReturnsFirst()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);

        Assert.Same(geoCs, compound.HeadCoordinateSystem);
    }

    /// <summary>
    /// Verifies that TailCoordinateSystem returns the second component.
    /// </summary>
    [Fact]
    public void CompoundCS_TailCoordinateSystem_ReturnsSecond()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);

        Assert.Same(vertCs, compound.TailCoordinateSystem);
    }

    /// <summary>
    /// Verifies that WKT output starts with COMPD_CS and contains both sub-systems.
    /// </summary>
    [Fact]
    public void CompoundCS_WKT_ContainsCompdCsAndSubSystems()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);
        string wkt = compound.WKT;

        Assert.StartsWith("COMPD_CS[\"WGS84 + ODN\"", wkt, StringComparison.Ordinal);
        Assert.Contains("GEOGCS[", wkt, StringComparison.Ordinal);
        Assert.Contains("VERT_CS[", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML output contains expected compound system elements.
    /// </summary>
    [Fact]
    public void CompoundCS_XML_ContainsExpectedElements()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);
        string xml = compound.XML;

        Assert.Contains("CS_CompoundCoordinateSystem", xml, StringComparison.Ordinal);
        Assert.Contains("CS_CoordinateSystem", xml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that GetUnits delegates to head CS for head dimensions.
    /// </summary>
    [Fact]
    public void CompoundCS_GetUnits_HeadDimension_ReturnsHeadUnit()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);

        IUnit unit = compound.GetUnits(0);
        Assert.IsType<AngularUnit>(unit);
    }

    /// <summary>
    /// Verifies that GetUnits delegates to tail CS for tail dimensions.
    /// </summary>
    [Fact]
    public void CompoundCS_GetUnits_TailDimension_ReturnsTailUnit()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);

        IUnit unit = compound.GetUnits(geoCs.Dimension);
        Assert.IsType<LinearUnit>(unit);
    }

    /// <summary>
    /// Verifies that EqualParams returns true for equivalent compound systems.
    /// </summary>
    [Fact]
    public void CompoundCS_EqualParams_EquivalentSystems_ReturnsTrue()
    {
        CompoundCoordinateSystem a = Factory.CreateCompoundCoordinateSystem(
            "CS1", GeographicCoordinateSystem.WGS84, VerticalCoordinateSystem.ODN);
        CompoundCoordinateSystem b = Factory.CreateCompoundCoordinateSystem(
            "CS2", GeographicCoordinateSystem.WGS84, VerticalCoordinateSystem.ODN);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for different tail systems.
    /// </summary>
    [Fact]
    public void CompoundCS_EqualParams_DifferentTail_ReturnsFalse()
    {
        VerticalCoordinateSystem tailA = VerticalCoordinateSystem.ODN;
        var tailB = new VerticalCoordinateSystem(
            LinearUnit.Foot,
            VerticalDatum.ODN,
            new AxisInfo("Up", AxisOrientationEnum.Up),
            "Different",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        CompoundCoordinateSystem a = Factory.CreateCompoundCoordinateSystem(
            "CS1", GeographicCoordinateSystem.WGS84, tailA);
        CompoundCoordinateSystem b = Factory.CreateCompoundCoordinateSystem(
            "CS2", GeographicCoordinateSystem.WGS84, tailB);

        Assert.False(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void CompoundCS_EqualParams_DifferentType_ReturnsFalse()
    {
        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84+ODN", GeographicCoordinateSystem.WGS84, VerticalCoordinateSystem.ODN);

        Assert.False(compound.EqualParams("not a CS"));
    }

    /// <summary>
    /// Verifies that property setters update head and tail.
    /// </summary>
    [Fact]
    public void CompoundCS_PropertySetters_UpdateValues()
    {
        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84+ODN", GeographicCoordinateSystem.WGS84, VerticalCoordinateSystem.ODN);

        var newTail = new VerticalCoordinateSystem(
            LinearUnit.Foot,
            VerticalDatum.ODN,
            new AxisInfo("Up", AxisOrientationEnum.Up),
            "Foot",
            string.Empty,
            -1,
            string.Empty,
            string.Empty,
            string.Empty);

        compound.TailCoordinateSystem = newTail;

        Assert.Same(newTail, compound.TailCoordinateSystem);
    }

    /// <summary>
    /// Verifies WKT round-trip for a compound coordinate system.
    /// </summary>
    [Fact]
    public void CompoundCS_WKT_RoundTrip()
    {
        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", GeographicCoordinateSystem.WGS84, VerticalCoordinateSystem.ODN);
        string wkt = compound.WKT;

        var parsed = Factory.CreateFromWkt(wkt) as CompoundCoordinateSystem;

        Assert.NotNull(parsed);
        Assert.True(compound.EqualParams(parsed));
    }

    /// <summary>
    /// Verifies that GetAxis returns axes from both head and tail systems.
    /// </summary>
    [Fact]
    public void CompoundCS_GetAxis_ReturnsAxesFromBothSystems()
    {
        GeographicCoordinateSystem geoCs = GeographicCoordinateSystem.WGS84;
        VerticalCoordinateSystem vertCs = VerticalCoordinateSystem.ODN;

        CompoundCoordinateSystem compound = Factory.CreateCompoundCoordinateSystem(
            "WGS84 + ODN", geoCs, vertCs);

        for (int i = 0; i < compound.Dimension; i++)
        {
            AxisInfo axis = compound.GetAxis(i);
            Assert.NotNull(axis.Name);
        }
    }

    // ========================================================================
    // FittedCoordinateSystem
    // ========================================================================

    /// <summary>
    /// Verifies that a fitted CS can be created via factory with a WKT transform string.
    /// </summary>
    [Fact]
    public void FittedCS_Factory_CreatesValidSystem()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        Assert.NotNull(fitted);
        Assert.Equal("Fitted WGS84", fitted.Name);
    }

    /// <summary>
    /// Verifies that BaseCoordinateSystem returns the underlying system.
    /// </summary>
    [Fact]
    public void FittedCS_BaseCoordinateSystem_ReturnsBase()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        Assert.True(baseCs.EqualParams(fitted.BaseCoordinateSystem));
    }

    /// <summary>
    /// Verifies that ToBaseTransform is accessible and not null.
    /// </summary>
    [Fact]
    public void FittedCS_ToBaseTransform_IsNotNull()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        Assert.NotNull(fitted.ToBaseTransform);
    }

    /// <summary>
    /// Verifies that ToBase returns a non-empty WKT string.
    /// </summary>
    [Fact]
    public void FittedCS_ToBase_ReturnsTransformWkt()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        string toBase = fitted.ToBase();

        Assert.NotNull(toBase);
        Assert.NotEmpty(toBase);
    }

    /// <summary>
    /// Verifies that WKT starts with FITTED_CS and contains base CS.
    /// </summary>
    [Fact]
    public void FittedCS_WKT_ContainsFittedCsAndBase()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);
        string wkt = fitted.WKT;

        Assert.StartsWith("FITTED_CS[\"Fitted WGS84\"", wkt, StringComparison.Ordinal);
        Assert.Contains("GEOGCS[", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML throws NotImplementedException.
    /// </summary>
    [Fact]
    public void FittedCS_XML_ThrowsNotImplementedException()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        Assert.Throws<NotImplementedException>(() => fitted.XML);
    }

    /// <summary>
    /// Verifies that GetUnits delegates to the base coordinate system.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FittedCS_GetUnits_DelegatesToBase(int dimension)
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        IUnit unit = fitted.GetUnits(dimension);

        Assert.IsType<AngularUnit>(unit);
    }

    /// <summary>
    /// Verifies that EqualParams returns true for equivalent fitted systems.
    /// </summary>
    [Fact]
    public void FittedCS_EqualParams_EquivalentSystems_ReturnsTrue()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        string toBaseWkt = "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]";
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem a = Factory.CreateFittedCoordinateSystem("F1", baseCs, toBaseWkt, axes);
        FittedCoordinateSystem b = Factory.CreateFittedCoordinateSystem("F2", baseCs, toBaseWkt, axes);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void FittedCS_EqualParams_DifferentType_ReturnsFalse()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        FittedCoordinateSystem fitted = Factory.CreateFittedCoordinateSystem(
            "Fitted WGS84",
            baseCs,
            "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]",
            axes);

        Assert.False(fitted.EqualParams("not a CS"));
    }

    /// <summary>
    /// Verifies that a fitted CS created via MathTransform overload works correctly.
    /// </summary>
    [Fact]
    public void FittedCS_Factory_WithMathTransform_CreatesValidSystem()
    {
        GeographicCoordinateSystem baseCs = GeographicCoordinateSystem.WGS84;
        var axes = new List<AxisInfo>
        {
            new("Lon", AxisOrientationEnum.East),
            new("Lat", AxisOrientationEnum.North),
        };

        string toBaseWkt = "PARAM_MT[\"Affine\", PARAMETER[\"num_row\", 3], PARAMETER[\"num_col\", 3], PARAMETER[\"elt_0_0\", 1], PARAMETER[\"elt_0_1\", 0], PARAMETER[\"elt_0_2\", 0], PARAMETER[\"elt_1_0\", 0], PARAMETER[\"elt_1_1\", 1], PARAMETER[\"elt_1_2\", 0], PARAMETER[\"elt_2_0\", 0], PARAMETER[\"elt_2_1\", 0], PARAMETER[\"elt_2_2\", 1]]";
        FittedCoordinateSystem fittedViaWkt = Factory.CreateFittedCoordinateSystem("F1", baseCs, toBaseWkt, axes);
        MathTransform transform = fittedViaWkt.ToBaseTransform;

        FittedCoordinateSystem fittedViaMt = Factory.CreateFittedCoordinateSystem("F2", baseCs, transform, axes);

        Assert.NotNull(fittedViaMt);
        Assert.True(fittedViaMt.BaseCoordinateSystem.EqualParams(baseCs));
    }

    // ========================================================================
    // PrimeMeridian
    // ========================================================================

    /// <summary>
    /// Verifies longitude values for all predefined prime meridians.
    /// </summary>
    [Theory]
    [InlineData("Greenwich", 0.0)]
    [InlineData("Lisbon", -9.0754862)]
    [InlineData("Paris", 2.5969213)]
    [InlineData("Bogota", -74.04513)]
    [InlineData("Madrid", -3.411658)]
    [InlineData("Rome", 12.27084)]
    [InlineData("Bern", 7.26225)]
    [InlineData("Jakarta", 106.482779)]
    [InlineData("Ferro", -17.66666666666667)]
    [InlineData("Brussels", 4.220471)]
    [InlineData("Stockholm", 18.03298)]
    [InlineData("Athens", 23.4258815)]
    [InlineData("Oslo", 10.43225)]
    public void PrimeMeridian_StaticInstances_HaveCorrectLongitude(string name, double expectedLongitude)
    {
        PrimeMeridian pm = name switch
        {
            "Greenwich" => PrimeMeridian.Greenwich,
            "Lisbon" => PrimeMeridian.Lisbon,
            "Paris" => PrimeMeridian.Paris,
            "Bogota" => PrimeMeridian.Bogota,
            "Madrid" => PrimeMeridian.Madrid,
            "Rome" => PrimeMeridian.Rome,
            "Bern" => PrimeMeridian.Bern,
            "Jakarta" => PrimeMeridian.Jakarta,
            "Ferro" => PrimeMeridian.Ferro,
            "Brussels" => PrimeMeridian.Brussels,
            "Stockholm" => PrimeMeridian.Stockholm,
            "Athens" => PrimeMeridian.Athens,
            "Oslo" => PrimeMeridian.Oslo,
            _ => throw new ArgumentException($"Unknown meridian: {name}"),
        };

        Assert.Equal(expectedLongitude, pm.Longitude, 10);
    }

    /// <summary>
    /// Verifies that all predefined meridians have EPSG authority.
    /// </summary>
    [Theory]
    [InlineData("Greenwich", 8901)]
    [InlineData("Lisbon", 8902)]
    [InlineData("Paris", 8903)]
    [InlineData("Bogota", 8904)]
    [InlineData("Madrid", 8905)]
    [InlineData("Rome", 8906)]
    [InlineData("Bern", 8907)]
    [InlineData("Jakarta", 8908)]
    [InlineData("Ferro", 8909)]
    [InlineData("Brussels", 8910)]
    [InlineData("Stockholm", 8911)]
    [InlineData("Athens", 8912)]
    [InlineData("Oslo", 8913)]
    public void PrimeMeridian_StaticInstances_HaveCorrectEpsgCode(string name, long expectedCode)
    {
        PrimeMeridian pm = name switch
        {
            "Greenwich" => PrimeMeridian.Greenwich,
            "Lisbon" => PrimeMeridian.Lisbon,
            "Paris" => PrimeMeridian.Paris,
            "Bogota" => PrimeMeridian.Bogota,
            "Madrid" => PrimeMeridian.Madrid,
            "Rome" => PrimeMeridian.Rome,
            "Bern" => PrimeMeridian.Bern,
            "Jakarta" => PrimeMeridian.Jakarta,
            "Ferro" => PrimeMeridian.Ferro,
            "Brussels" => PrimeMeridian.Brussels,
            "Stockholm" => PrimeMeridian.Stockholm,
            "Athens" => PrimeMeridian.Athens,
            "Oslo" => PrimeMeridian.Oslo,
            _ => throw new ArgumentException($"Unknown meridian: {name}"),
        };

        Assert.Equal("EPSG", pm.Authority);
        Assert.Equal(expectedCode, pm.AuthorityCode);
    }

    /// <summary>
    /// Verifies that WKT starts with PRIMEM and contains the meridian name and longitude.
    /// </summary>
    [Fact]
    public void PrimeMeridian_WKT_ContainsPrimemAndName()
    {
        string wkt = PrimeMeridian.Greenwich.WKT;

        Assert.StartsWith("PRIMEM[\"Greenwich\"", wkt, StringComparison.Ordinal);
        Assert.Contains("AUTHORITY[\"EPSG\", \"8901\"]", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that XML output contains the expected elements and longitude attribute.
    /// </summary>
    [Fact]
    public void PrimeMeridian_XML_ContainsExpectedElements()
    {
        string xml = PrimeMeridian.Greenwich.XML;

        Assert.Contains("CS_PrimeMeridian", xml, StringComparison.Ordinal);
        Assert.Contains("Longitude=\"0\"", xml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a factory-created prime meridian has correct values.
    /// </summary>
    [Fact]
    public void PrimeMeridian_Factory_CreatesWithCorrectValues()
    {
        PrimeMeridian pm = Factory.CreatePrimeMeridian(
            "CustomMeridian", AngularUnit.Degrees, 45.0);

        Assert.Equal("CustomMeridian", pm.Name);
        Assert.Equal(45.0, pm.Longitude);
        Assert.True(pm.AngularUnit.EqualParams(AngularUnit.Degrees));
    }

    /// <summary>
    /// Verifies that EqualParams returns true for meridians with same longitude and unit.
    /// </summary>
    [Fact]
    public void PrimeMeridian_EqualParams_SameValues_ReturnsTrue()
    {
        PrimeMeridian a = Factory.CreatePrimeMeridian("PM1", AngularUnit.Degrees, 10.0);
        PrimeMeridian b = Factory.CreatePrimeMeridian("PM2", AngularUnit.Degrees, 10.0);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for different longitudes.
    /// </summary>
    [Fact]
    public void PrimeMeridian_EqualParams_DifferentLongitude_ReturnsFalse()
    {
        PrimeMeridian a = Factory.CreatePrimeMeridian("PM1", AngularUnit.Degrees, 10.0);
        PrimeMeridian b = Factory.CreatePrimeMeridian("PM2", AngularUnit.Degrees, 20.0);

        Assert.False(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for different angular units.
    /// </summary>
    [Fact]
    public void PrimeMeridian_EqualParams_DifferentUnit_ReturnsFalse()
    {
        PrimeMeridian a = Factory.CreatePrimeMeridian("PM1", AngularUnit.Degrees, 10.0);
        PrimeMeridian b = Factory.CreatePrimeMeridian("PM2", AngularUnit.Radian, 10.0);

        Assert.False(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams returns false for a different type.
    /// </summary>
    [Fact]
    public void PrimeMeridian_EqualParams_DifferentType_ReturnsFalse()
    {
        Assert.False(PrimeMeridian.Greenwich.EqualParams("not a PM"));
    }

    /// <summary>
    /// Verifies WKT round-trip for Paris prime meridian.
    /// </summary>
    [Fact]
    public void PrimeMeridian_Paris_WKT_ContainsLongitude()
    {
        string wkt = PrimeMeridian.Paris.WKT;

        Assert.Contains("Paris", wkt, StringComparison.Ordinal);
        Assert.Contains("2.5969213", wkt, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies XML output for non-Greenwich meridians.
    /// </summary>
    [Fact]
    public void PrimeMeridian_NonGreenwich_XML_ContainsLongitude()
    {
        string xml = PrimeMeridian.Rome.XML;

        Assert.Contains("CS_PrimeMeridian", xml, StringComparison.Ordinal);
        Assert.Contains("12.27084", xml, StringComparison.Ordinal);
    }

    // ========================================================================
    // Info base class (tested via concrete types)
    // ========================================================================

    /// <summary>
    /// Verifies that all Info properties are accessible on a concrete type.
    /// </summary>
    [Fact]
    public void Info_Properties_AccessibleOnConcreteType()
    {
        PrimeMeridian pm = PrimeMeridian.Greenwich;

        Assert.Equal("Greenwich", pm.Name);
        Assert.Equal("EPSG", pm.Authority);
        Assert.Equal(8901, pm.AuthorityCode);
    }

    /// <summary>
    /// Verifies that ToString returns WKT.
    /// </summary>
    [Fact]
    public void Info_ToString_ReturnsWKT()
    {
        PrimeMeridian pm = PrimeMeridian.Greenwich;

        Assert.Equal(pm.WKT, pm.ToString());
    }

    /// <summary>
    /// Verifies that EqualParams ignores Name differences (name is excluded from comparison).
    /// </summary>
    [Fact]
    public void Info_EqualParams_IgnoresName()
    {
        PrimeMeridian a = Factory.CreatePrimeMeridian("Name1", AngularUnit.Degrees, 0.0);
        PrimeMeridian b = Factory.CreatePrimeMeridian("Name2", AngularUnit.Degrees, 0.0);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams ignores Authority and AuthorityCode differences.
    /// </summary>
    [Fact]
    public void Info_EqualParams_IgnoresAuthority()
    {
        var a = new LinearUnit(1.0, "metre", "EPSG", 9001, string.Empty, string.Empty, string.Empty);
        var b = new LinearUnit(1.0, "meter", "OTHER", 1, string.Empty, string.Empty, string.Empty);

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams ignores Remarks differences.
    /// </summary>
    [Fact]
    public void Info_EqualParams_IgnoresRemarks()
    {
        var a = new LinearUnit(1.0, "metre", "EPSG", 9001, string.Empty, string.Empty, "Remark A");
        var b = new LinearUnit(1.0, "meter", "EPSG", 9001, string.Empty, string.Empty, "Remark B");

        Assert.True(a.EqualParams(b));
    }

    /// <summary>
    /// Verifies that EqualParams ignores Alias and Abbreviation differences.
    /// </summary>
    [Fact]
    public void Info_EqualParams_IgnoresAliasAndAbbreviation()
    {
        var a = new LinearUnit(1.0, "metre", "EPSG", 9001, "m", "alias1", string.Empty);
        var b = new LinearUnit(1.0, "meter", "EPSG", 9001, "mt", "alias2", string.Empty);

        Assert.True(a.EqualParams(b));
    }

    // ========================================================================
    // ParameterInfo (tested via Projection)
    // ========================================================================

    /// <summary>
    /// Verifies that NumParameters returns the parameter count of a projection.
    /// </summary>
    [Fact]
    public void ParameterInfo_NumParameters_ReturnsCorrectCount()
    {
        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 0),
            new ProjectionParameter("central_meridian", 0),
            new ProjectionParameter("scale_factor", 1.0),
        };

        IProjection projection = Factory.CreateProjection("test", "transverse_mercator", parameters);

        Assert.Equal(3, projection.NumParameters);
    }

    /// <summary>
    /// Verifies that GetParameter returns the correct parameter by index.
    /// </summary>
    [Fact]
    public void ParameterInfo_GetParameter_ByIndex_ReturnsCorrectValue()
    {
        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("scale_factor", 0.9996),
            new ProjectionParameter("central_meridian", -93.0),
        };

        IProjection projection = Factory.CreateProjection("test", "transverse_mercator", parameters);

        ProjectionParameter param = projection.GetParameter(0);
        Assert.Equal("scale_factor", param.Name);
        Assert.Equal(0.9996, param.Value);
    }

    /// <summary>
    /// Verifies that GetParameter by name returns the correct parameter.
    /// </summary>
    [Fact]
    public void ParameterInfo_GetParameter_ByName_ReturnsCorrectValue()
    {
        var parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("scale_factor", 0.9996),
            new ProjectionParameter("central_meridian", -93.0),
        };

        IProjection projection = Factory.CreateProjection("test", "transverse_mercator", parameters);

        ProjectionParameter param = projection.GetParameter("central_meridian")!;
        Assert.Equal(-93.0, param.Value);
    }

    // ========================================================================
    // CoordinateSystemServices — edge cases
    // ========================================================================

    /// <summary>
    /// Verifies that the default constructor initializes with known coordinate systems.
    /// </summary>
    [Fact]
    public void Services_DefaultConstructor_ContainsWGS84()
    {
        var services = new CoordinateSystemServices();

        CoordinateSystem? cs = services.GetCoordinateSystem(4326);

        Assert.NotNull(cs);
    }

    /// <summary>
    /// Verifies that GetCoordinateSystem returns null for an unknown SRID.
    /// </summary>
    [Fact]
    public void Services_GetCoordinateSystem_UnknownSrid_ReturnsNull()
    {
        var services = new CoordinateSystemServices();

        CoordinateSystem? cs = services.GetCoordinateSystem(999999);

        Assert.Null(cs);
    }

    /// <summary>
    /// Verifies that TryGetCoordinateSystem returns false for an unknown SRID.
    /// </summary>
    [Fact]
    public void Services_TryGetCoordinateSystem_UnknownSrid_ReturnsFalse()
    {
        var services = new CoordinateSystemServices();

        bool found = services.TryGetCoordinateSystem(999999, out CoordinateSystem? cs);

        Assert.False(found);
        Assert.Null(cs);
    }

    /// <summary>
    /// Verifies that TryGetCoordinateSystem returns true for a known SRID.
    /// </summary>
    [Fact]
    public void Services_TryGetCoordinateSystem_KnownSrid_ReturnsTrue()
    {
        var services = new CoordinateSystemServices();

        bool found = services.TryGetCoordinateSystem(4326, out CoordinateSystem? cs);

        Assert.True(found);
        Assert.NotNull(cs);
    }

    /// <summary>
    /// Verifies that GetCoordinateSystem by authority/code returns the correct system.
    /// </summary>
    [Fact]
    public void Services_GetCoordinateSystem_ByAuthorityCode_ReturnsSystem()
    {
        var services = new CoordinateSystemServices();

        CoordinateSystem? cs = services.GetCoordinateSystem("EPSG", 4326);

        Assert.NotNull(cs);
    }

    /// <summary>
    /// Verifies that TryGetCoordinateSystem by authority/code returns false for unknown.
    /// </summary>
    [Fact]
    public void Services_TryGetCoordinateSystem_ByAuthorityCode_UnknownCode_ReturnsFalse()
    {
        var services = new CoordinateSystemServices();

        bool found = services.TryGetCoordinateSystem("EPSG", 999999, out CoordinateSystem? cs);

        Assert.False(found);
        Assert.Null(cs);
    }

    /// <summary>
    /// Verifies that GetAvailableSridValues returns a sorted non-empty array.
    /// </summary>
    [Fact]
    public void Services_GetAvailableSridValues_ReturnsNonEmptySortedArray()
    {
        var services = new CoordinateSystemServices();

        int[] srids = services.GetAvailableSridValues();

        Assert.NotEmpty(srids);

        // Verify sorted
        for (int i = 1; i < srids.Length; i++)
        {
            Assert.True(srids[i] >= srids[i - 1], "SRID values should be sorted");
        }
    }

    /// <summary>
    /// Verifies that GetSRID returns the correct SRID for a known authority/code.
    /// </summary>
    [Fact]
    public void Services_GetSRID_KnownSystem_ReturnsSrid()
    {
        var services = new CoordinateSystemServices();

        int? srid = services.GetSRID("EPSG", 4326);

        Assert.NotNull(srid);
        Assert.Equal(4326, srid.Value);
    }

    /// <summary>
    /// Verifies that GetSRID returns null for an unknown authority/code.
    /// </summary>
    [Fact]
    public void Services_GetSRID_UnknownSystem_ReturnsNull()
    {
        var services = new CoordinateSystemServices();

        int? srid = services.GetSRID("UNKNOWN", 99999);

        Assert.Null(srid);
    }

    /// <summary>
    /// Verifies that RemoveCoordinateSystem throws NotSupportedException.
    /// </summary>
    [Fact]
    public void Services_RemoveCoordinateSystem_ThrowsNotSupportedException()
    {
        var services = new CoordinateSystemServices();

        Assert.Throws<NotSupportedException>(() => services.RemoveCoordinateSystem(4326));
    }

    /// <summary>
    /// Verifies that CreateTransformation returns a transformation between two known systems.
    /// </summary>
    [Fact]
    public void Services_CreateTransformation_BetweenKnownSystems_ReturnsTransformation()
    {
        var services = new CoordinateSystemServices();

        ICoordinateTransformation? transformation = services.CreateTransformation(4326, 3857);

        Assert.NotNull(transformation);
    }

    /// <summary>
    /// Verifies that CreateTransformation returns null for an unknown source SRID.
    /// </summary>
    [Fact]
    public void Services_CreateTransformation_UnknownSrid_ReturnsNull()
    {
        var services = new CoordinateSystemServices();

        ICoordinateTransformation? transformation = services.CreateTransformation(999999, 4326);

        Assert.Null(transformation);
    }

    /// <summary>
    /// Verifies that CreateTransformation with null source returns null.
    /// </summary>
    [Fact]
    public void Services_CreateTransformation_NullSources_ReturnsNull()
    {
        var services = new CoordinateSystemServices();

        ICoordinateTransformation? transformation = services.CreateTransformation(null, null);

        Assert.Null(transformation);
    }

    /// <summary>
    /// Verifies that GetAvailableSridValues yields entries.
    /// </summary>
    [Fact]
    public void Services_GetAvailableSridValues_YieldsEntries()
    {
        var services = new CoordinateSystemServices();

        int[] srids = services.GetAvailableSridValues();

        Assert.NotEmpty(srids);
        Assert.Contains(4326, srids);
    }

    /// <summary>
    /// Verifies that constructing with explicit factories works.
    /// </summary>
    [Fact]
    public void Services_ConstructorWithFactories_IsUsable()
    {
        var csFactory = new CoordinateSystemFactory();
        var ctFactory = new CoordinateTransformationFactory();

        var services = new CoordinateSystemServices(csFactory, ctFactory);

        Assert.NotNull(services);
        Assert.NotEmpty(services.GetAvailableSridValues());
    }

    /// <summary>
    /// Verifies that constructing with definitions initializes systems.
    /// </summary>
    [Fact]
    public void Services_ConstructorWithDefinitions_InitializesSystems()
    {
        string wkt = GeographicCoordinateSystem.WGS84.WKT;
        CoordinateSystemDefinition[] definitions = new[]
        {
            new CoordinateSystemDefinition(4326, wkt),
        };

        var services = new CoordinateSystemServices(definitions);

        CoordinateSystem? cs = services.GetCoordinateSystem(4326);
        Assert.NotNull(cs);
    }
}
