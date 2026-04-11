// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using ProjNet.IO.CoordinateSystems;
using ProjNet.IO.Wkt;
using Xunit;

/// <summary>
/// Verifies native PROJJSON coordinate-system writing for the currently supported CRS slices.
/// </summary>
public class ProjJsonWriterTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Srid)
            .ToDictionary(group => group.Key, group => group.Last().Wkt));

    /// <summary>
    /// Provides EPSG geographic CRS examples that should roundtrip through the initial PROJJSON writer slice.
    /// </summary>
    /// <returns>EPSG SRIDs for supported geographic CRS roundtrip coverage.</returns>
    public static IEnumerable<TheoryDataRow<int>> SupportedGeographicWriterRows()
    {
        return
        [
            new TheoryDataRow<int>(4230),
            new TheoryDataRow<int>(4277),
            new TheoryDataRow<int>(4314),
            new TheoryDataRow<int>(4322),
            new TheoryDataRow<int>(4807),
        ];
    }

    /// <summary>
    /// Provides EPSG projected CRS examples that should roundtrip through the projected PROJJSON writer slice.
    /// </summary>
    /// <returns>EPSG SRIDs for supported projected CRS roundtrip coverage.</returns>
    public static IEnumerable<TheoryDataRow<int>> SupportedProjectedWriterRows()
    {
        return
        [
            new TheoryDataRow<int>(27700),
            new TheoryDataRow<int>(31370),
            new TheoryDataRow<int>(2169),
            new TheoryDataRow<int>(23032),
            new TheoryDataRow<int>(31467),
        ];
    }

    /// <summary>
    /// Provides EPSG geocentric, vertical, and compound CRS examples that should roundtrip through the remaining PROJJSON writer slice.
    /// </summary>
    /// <returns>EPSG SRIDs for supported remaining CRS roundtrip coverage.</returns>
    public static IEnumerable<TheoryDataRow<int, Type>> SupportedRemainingWriterRows()
    {
        return
        [
            new TheoryDataRow<int, Type>(4978, typeof(GeocentricCoordinateSystem)),
            new TheoryDataRow<int, Type>(5701, typeof(VerticalCoordinateSystem)),
            new TheoryDataRow<int, Type>(9518, typeof(CompoundCoordinateSystem)),
        ];
    }

    /// <summary>
    /// Provides EPSG CRS examples that should survive a WKT1 -> PROJJSON -> WKT1 cross-format roundtrip.
    /// </summary>
    /// <returns>EPSG SRIDs for supported cross-format writer coverage.</returns>
    public static IEnumerable<TheoryDataRow<int>> SupportedCrossFormatWriterRows()
    {
        return
        [
            new TheoryDataRow<int>(4230),
            new TheoryDataRow<int>(4277),
            new TheoryDataRow<int>(4314),
            new TheoryDataRow<int>(4322),
            new TheoryDataRow<int>(4807),
            new TheoryDataRow<int>(27700),
            new TheoryDataRow<int>(31370),
            new TheoryDataRow<int>(2169),
            new TheoryDataRow<int>(23032),
            new TheoryDataRow<int>(31467),
            new TheoryDataRow<int>(4978),
            new TheoryDataRow<int>(5701),
            new TheoryDataRow<int>(9518),
        ];
    }

    /// <summary>
    /// Verifies the initial PROJJSON writer slice roundtrips supported geographic CRS back to the same semantic model.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    [Theory]
    [MemberData(nameof(SupportedGeographicWriterRows))]
    public void ToJson_RoundtripsSupportedGeographicCrsEquivalentToCatalogReference(int srid)
    {
        GeographicCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        string json = ProjJsonWriter.ToJson(reference);
        GeographicCoordinateSystem reparsed = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.True(reparsed.EqualParams(reference), $"PROJJSON geographic CRS write/read mismatch for EPSG:{srid}.");
        Assert.Equal(reference.Authority, reparsed.Authority);
        Assert.Equal(reference.AuthorityCode, reparsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies the projected PROJJSON writer slice roundtrips supported projected CRS back to the same semantic model.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    [Theory]
    [MemberData(nameof(SupportedProjectedWriterRows))]
    public void ToJson_RoundtripsSupportedProjectedCrsEquivalentToCatalogReference(int srid)
    {
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        string json = ProjJsonWriter.ToJson(reference);
        ProjectedCoordinateSystem reparsed = Assert.IsType<ProjectedCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.True(reparsed.EqualParams(reference), $"PROJJSON projected CRS write/read mismatch for EPSG:{srid}.");
        Assert.Equal(reference.Authority, reparsed.Authority);
        Assert.Equal(reference.AuthorityCode, reparsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies the remaining PROJJSON writer slice roundtrips supported non-projected CRS back to the same semantic model.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="expectedType">Expected coordinate-system runtime type.</param>
    [Theory]
    [MemberData(nameof(SupportedRemainingWriterRows))]
    public void ToJson_RoundtripsSupportedRemainingCrsEquivalentToCatalogReference(int srid, Type expectedType)
    {
        CoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        string json = ProjJsonWriter.ToJson(reference);
        CoordinateSystem reparsed = Assert.IsAssignableFrom<CoordinateSystem>(ProjJsonReader.Parse(json));
        Assert.IsType(expectedType, reparsed);

        Assert.True(reparsed.EqualParams(reference), $"PROJJSON remaining CRS write/read mismatch for EPSG:{srid}.");
        Assert.Equal(reference.Authority, reparsed.Authority);
        Assert.Equal(reference.AuthorityCode, reparsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies supported CRS survive a WKT1 -> PROJJSON -> WKT1 cross-format roundtrip without semantic drift.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    [Theory]
    [MemberData(nameof(SupportedCrossFormatWriterRows))]
    public void ToJson_RoundtripsSupportedCrsAcrossWkt1AndProjJsonWithoutSemanticDrift(int srid)
    {
        CoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        string json = ProjJsonWriter.ToJson(reference);
        CoordinateSystem fromProjJson = Assert.IsAssignableFrom<CoordinateSystem>(ProjJsonReader.Parse(json));
        CoordinateSystem roundTrippedFromWkt = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            CoordinateSystemFactory,
            fromProjJson.WKT);

        Assert.IsType(fromProjJson.GetType(), roundTrippedFromWkt);
        Assert.True(fromProjJson.EqualParams(roundTrippedFromWkt), $"WKT1 cross-format roundtrip mismatch for EPSG:{srid}.");
        Assert.True(reference.EqualParams(roundTrippedFromWkt), $"Reference mismatch after WKT1 -> PROJJSON -> WKT1 roundtrip for EPSG:{srid}.");
        Assert.Equal(reference.Authority, roundTrippedFromWkt.Authority);
        Assert.Equal(reference.AuthorityCode, roundTrippedFromWkt.AuthorityCode);
    }

    /// <summary>
    /// Verifies <see cref="ProjJsonWriter.WriteTo(Utf8JsonWriter, CoordinateSystem)"/> emits the expected PROJJSON object shape for a geographic CRS.
    /// </summary>
    [Fact]
    public void WriteTo_WritesGeographicCrsWithExpectedProjJsonShape()
    {
        GeographicCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(4807));

        string json;
        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream))
            {
                ProjJsonWriter.WriteTo(writer, reference);
                writer.Flush();
            }

            json = Encoding.UTF8.GetString(stream.ToArray());
        }

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("GeographicCRS", root.GetProperty("type").GetString());
        Assert.Equal("NTF (Paris)", root.GetProperty("name").GetString());
        Assert.Equal("Nouvelle Triangulation Francaise (Paris)", root.GetProperty("datum").GetProperty("name").GetString());
        Assert.Equal("Clarke 1880 (IGN)", root.GetProperty("datum").GetProperty("ellipsoid").GetProperty("name").GetString());
        Assert.Equal("Paris", root.GetProperty("prime_meridian").GetProperty("name").GetString());
        Assert.Equal("ellipsoidal", root.GetProperty("coordinate_system").GetProperty("subtype").GetString());
        Assert.Equal(2, root.GetProperty("coordinate_system").GetProperty("axis").GetArrayLength());
        Assert.Equal("EPSG", root.GetProperty("id").GetProperty("authority").GetString());
        Assert.Equal(4807, root.GetProperty("id").GetProperty("code").GetInt32());
    }

    /// <summary>
    /// Verifies <see cref="ProjJsonWriter.WriteTo(Utf8JsonWriter, CoordinateSystem)"/> emits the expected PROJJSON object shape for a projected CRS.
    /// </summary>
    [Fact]
    public void WriteTo_WritesProjectedCrsWithExpectedProjJsonShape()
    {
        ProjectedCoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem<ProjectedCoordinateSystem>(
            CoordinateSystemFactory,
            GetCatalogWkt(27700));

        string json;
        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream))
            {
                ProjJsonWriter.WriteTo(writer, reference);
                writer.Flush();
            }

            json = Encoding.UTF8.GetString(stream.ToArray());
        }

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement parameters = root.GetProperty("conversion").GetProperty("parameters");
        JsonElement scaleFactorParameter = parameters.EnumerateArray()
            .Single(element => string.Equals(element.GetProperty("name").GetString(), "Scale factor at natural origin", StringComparison.Ordinal));

        Assert.Equal("ProjectedCRS", root.GetProperty("type").GetString());
        Assert.Equal("OSGB36 / British National Grid", root.GetProperty("name").GetString());
        Assert.Equal("GeographicCRS", root.GetProperty("base_crs").GetProperty("type").GetString());
        Assert.Equal("Conversion", root.GetProperty("conversion").GetProperty("type").GetString());
        Assert.Equal("Transverse Mercator", root.GetProperty("conversion").GetProperty("method").GetProperty("name").GetString());
        Assert.Equal(5, parameters.GetArrayLength());
        Assert.Equal("ScaleUnit", scaleFactorParameter.GetProperty("unit").GetProperty("type").GetString());
        Assert.Equal("Cartesian", root.GetProperty("coordinate_system").GetProperty("subtype").GetString());
        Assert.Equal("EPSG", root.GetProperty("id").GetProperty("authority").GetString());
        Assert.Equal(27700, root.GetProperty("id").GetProperty("code").GetInt32());
    }

    /// <summary>
    /// Verifies null JSON writers are rejected.
    /// </summary>
    [Fact]
    public void WriteTo_WithNullWriter_ThrowsArgumentNullException()
    {
        GeographicCoordinateSystem reference = GeographicCoordinateSystem.WGS84;

        Assert.Throws<ArgumentNullException>(() => ProjJsonWriter.WriteTo(null!, reference));
    }

    /// <summary>
    /// Verifies null coordinate systems are rejected by <see cref="ProjJsonWriter.WriteTo(Utf8JsonWriter, CoordinateSystem)"/>.
    /// </summary>
    [Fact]
    public void WriteTo_WithNullCoordinateSystem_ThrowsArgumentNullException()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        Assert.Throws<ArgumentNullException>(() => ProjJsonWriter.WriteTo(writer, null!));
    }

    /// <summary>
    /// Verifies null coordinate systems are rejected by <see cref="ProjJsonWriter.ToJson(CoordinateSystem)"/>.
    /// </summary>
    [Fact]
    public void ToJson_WithNullCoordinateSystem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ProjJsonWriter.ToJson(null!));
    }

    /// <summary>
    /// Verifies affine fitted coordinate systems with geographic bases serialize as PROJJSON derived geographic CRS objects.
    /// </summary>
    [Fact]
    public void ToJson_WithDerivedGeographicCoordinateSystem_EmitsDerivedGeographicCrs()
    {
        FittedCoordinateSystem fitted = CreateDerivedGeographicCoordinateSystem();

        string json = ProjJsonWriter.ToJson(fitted);
        FittedCoordinateSystem parsed = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("DerivedGeographicCRS", root.GetProperty("type").GetString());
        Assert.Equal("GeographicCRS", root.GetProperty("base_crs").GetProperty("type").GetString());
        Assert.Equal("Affine parametric transformation", root.GetProperty("conversion").GetProperty("method").GetProperty("name").GetString());
        Assert.Equal("Local latitude", root.GetProperty("coordinate_system").GetProperty("axis")[0].GetProperty("name").GetString());
        Assert.Equal("Local longitude", root.GetProperty("coordinate_system").GetProperty("axis")[1].GetProperty("name").GetString());
        Assert.True(parsed.EqualParams(fitted));
        Assert.Equal("Local latitude", parsed.GetAxis(0).Name);
        Assert.Equal("Local longitude", parsed.GetAxis(1).Name);
    }

    /// <summary>
    /// Verifies affine fitted coordinate systems with projected bases serialize as PROJJSON derived projected CRS objects.
    /// </summary>
    [Fact]
    public void ToJson_WithDerivedProjectedCoordinateSystem_EmitsDerivedProjectedCrs()
    {
        FittedCoordinateSystem fitted = CreateDerivedProjectedCoordinateSystem();

        string json = ProjJsonWriter.ToJson(fitted);
        FittedCoordinateSystem parsed = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("DerivedProjectedCRS", root.GetProperty("type").GetString());
        Assert.Equal("ProjectedCRS", root.GetProperty("base_crs").GetProperty("type").GetString());
        Assert.Equal("Affine parametric transformation", root.GetProperty("conversion").GetProperty("method").GetProperty("name").GetString());
        Assert.Equal("Local easting", root.GetProperty("coordinate_system").GetProperty("axis")[0].GetProperty("name").GetString());
        Assert.Equal("Local northing", root.GetProperty("coordinate_system").GetProperty("axis")[1].GetProperty("name").GetString());
        Assert.True(parsed.EqualParams(fitted));
        Assert.Equal("Local easting", parsed.GetAxis(0).Name);
        Assert.Equal("Local northing", parsed.GetAxis(1).Name);
    }

    /// <summary>
    /// Verifies legacy WKT1 <c>FITTED_CS</c> geographic definitions survive the WKT2 derived-CRS and PROJJSON derived-CRS pipeline without semantic drift.
    /// </summary>
    [Fact]
    public void ToJson_RoundtripsDerivedGeographicCrsAcrossWkt1Wkt2AndProjJson()
    {
        FittedCoordinateSystem original = CreateWkt1CompatibleDerivedGeographicCoordinateSystem();
        string wkt1 = original.WKT;
        FittedCoordinateSystem fromWkt1 = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(CoordinateSystemFactory, wkt1);

        string wkt2 = fromWkt1.ToWktNode(WktVersion.Wkt22019).ToString();
        FittedCoordinateSystem fromWkt2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(CoordinateSystemFactory, wkt2);

        string json = ProjJsonWriter.ToJson(fromWkt2);
        FittedCoordinateSystem fromProjJson = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);

        Assert.StartsWith("FITTED_CS[", wkt1, StringComparison.Ordinal);
        Assert.StartsWith("GEOGCRS[", wkt2, StringComparison.Ordinal);
        Assert.Equal("DerivedGeographicCRS", document.RootElement.GetProperty("type").GetString());
        Assert.True(fromProjJson.EqualParams(fromWkt2));
        AssertFittedCoordinateSystemSemanticsEqual(fromWkt1, fromProjJson);
        AssertDerivedGeographicBaseSemanticsEqual(
            Assert.IsType<GeographicCoordinateSystem>(fromWkt1.BaseCoordinateSystem),
            Assert.IsType<GeographicCoordinateSystem>(fromProjJson.BaseCoordinateSystem));
    }

    /// <summary>
    /// Verifies legacy WKT1 <c>FITTED_CS</c> projected definitions survive the WKT2 derived-CRS and PROJJSON derived-CRS pipeline without semantic drift.
    /// </summary>
    [Fact]
    public void ToJson_RoundtripsDerivedProjectedCrsAcrossWkt1Wkt2AndProjJson()
    {
        FittedCoordinateSystem original = CreateWkt1CompatibleDerivedProjectedCoordinateSystem();
        string wkt1 = original.WKT;
        FittedCoordinateSystem fromWkt1 = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(CoordinateSystemFactory, wkt1);

        string wkt2 = fromWkt1.ToWktNode(WktVersion.Wkt22019).ToString();
        FittedCoordinateSystem fromWkt2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<FittedCoordinateSystem>(CoordinateSystemFactory, wkt2);

        string json = ProjJsonWriter.ToJson(fromWkt2);
        FittedCoordinateSystem fromProjJson = Assert.IsType<FittedCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);

        Assert.StartsWith("FITTED_CS[", wkt1, StringComparison.Ordinal);
        Assert.StartsWith("DERIVEDPROJCRS[", wkt2, StringComparison.Ordinal);
        Assert.Equal("DerivedProjectedCRS", document.RootElement.GetProperty("type").GetString());
        Assert.True(fromProjJson.EqualParams(fromWkt2));
        AssertFittedCoordinateSystemSemanticsEqual(fromWkt1, fromProjJson);
        AssertDerivedProjectedBaseSemanticsEqual(
            Assert.IsType<ProjectedCoordinateSystem>(fromWkt1.BaseCoordinateSystem),
            Assert.IsType<ProjectedCoordinateSystem>(fromProjJson.BaseCoordinateSystem));
    }

    /// <summary>
    /// Verifies geographic CRS with retained WGS84 conversion metadata serialize as PROJJSON <c>BoundCRS</c>.
    /// </summary>
    [Fact]
    public void ToJson_WithGeographicCoordinateSystemUsingBoundMetadata_EmitsBoundCrs()
    {
        GeographicCoordinateSystem geographic = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ED50 test",
            AngularUnit.Degrees,
            HorizontalDatum.ED50,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        Wgs84ConversionInfo expectedParameters = Assert.IsType<Wgs84ConversionInfo>(geographic.HorizontalDatum.Wgs84Parameters);

        string json = ProjJsonWriter.ToJson(geographic);
        BoundCoordinateSystem parsed = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));
        GeographicCoordinateSystem source = Assert.IsType<GeographicCoordinateSystem>(parsed.SourceCoordinateSystem);
        GeographicCoordinateSystem target = Assert.IsType<GeographicCoordinateSystem>(parsed.TargetCoordinateSystem);

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("BoundCRS", root.GetProperty("type").GetString());
        Assert.Equal("GeographicCRS", root.GetProperty("source_crs").GetProperty("type").GetString());
        Assert.Equal("GeographicCRS", root.GetProperty("target_crs").GetProperty("type").GetString());
        Assert.Equal("AbridgedTransformation", root.GetProperty("transformation").GetProperty("type").GetString());
        Assert.Equal(parsed.Transformation.MethodName, root.GetProperty("transformation").GetProperty("method").GetProperty("name").GetString());
        Assert.Equal(geographic.Name, parsed.Name);
        Assert.Equal(geographic.HorizontalDatum.Name, source.HorizontalDatum.Name);
        Assert.Null(source.HorizontalDatum.Wgs84Parameters);
        Assert.Equal(expectedParameters, parsed.Transformation.Wgs84Parameters);
        Assert.Equal("WGS 84", target.Name);
    }

    /// <summary>
    /// Verifies projected CRS whose base datum retains WGS84 conversion metadata serialize as PROJJSON <c>BoundCRS</c>.
    /// </summary>
    [Fact]
    public void ToJson_WithProjectedCoordinateSystemUsingBoundMetadata_EmitsBoundCrs()
    {
        GeographicCoordinateSystem geographic = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ED50 test",
            AngularUnit.Degrees,
            HorizontalDatum.ED50,
            PrimeMeridian.Greenwich,
            new AxisInfo("Geodetic latitude (Lat)", AxisOrientationEnum.North),
            new AxisInfo("Geodetic longitude (Lon)", AxisOrientationEnum.East));
        IProjection projection = CoordinateSystemFactory.CreateProjection(
            "ED50 TM",
            "Transverse_Mercator",
            new List<ProjectionParameter>
            {
                new("latitude_of_origin", 0),
                new("central_meridian", 9),
                new("scale_factor", 0.9996),
                new("false_easting", 500000),
                new("false_northing", 0),
            });
        ProjectedCoordinateSystem projected = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "ED50 / TM test",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));
        Wgs84ConversionInfo expectedParameters = Assert.IsType<Wgs84ConversionInfo>(projected.GeographicCoordinateSystem.HorizontalDatum.Wgs84Parameters);

        string json = ProjJsonWriter.ToJson(projected);
        BoundCoordinateSystem parsed = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));
        ProjectedCoordinateSystem source = Assert.IsType<ProjectedCoordinateSystem>(parsed.SourceCoordinateSystem);
        GeographicCoordinateSystem target = Assert.IsType<GeographicCoordinateSystem>(parsed.TargetCoordinateSystem);

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("BoundCRS", root.GetProperty("type").GetString());
        Assert.Equal("ProjectedCRS", root.GetProperty("source_crs").GetProperty("type").GetString());
        Assert.Equal("GeographicCRS", root.GetProperty("target_crs").GetProperty("type").GetString());
        Assert.Equal(projected.Name, parsed.Name);
        Assert.Equal("Transverse Mercator", source.Projection.ClassName);
        Assert.Equal(expectedParameters, parsed.Transformation.Wgs84Parameters);
        Assert.Equal("WGS 84", target.Name);
    }

    /// <summary>
    /// Verifies vertical CRS with retained bound-grid metadata serialize as PROJJSON <c>BoundCRS</c>.
    /// </summary>
    [Fact]
    public void ToJson_WithVerticalCoordinateSystemUsingBoundGridMetadata_EmitsBoundCrs()
    {
        VerticalCoordinateSystem vertical = CreateBoundVerticalCoordinateSystem();

        string json = ProjJsonWriter.ToJson(vertical);
        BoundCoordinateSystem parsed = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));
        VerticalCoordinateSystem source = Assert.IsType<VerticalCoordinateSystem>(parsed.SourceCoordinateSystem);
        CompoundCoordinateSystem target = Assert.IsType<CompoundCoordinateSystem>(parsed.TargetCoordinateSystem);

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement parameters = root.GetProperty("transformation").GetProperty("parameters");

        Assert.Equal("BoundCRS", root.GetProperty("type").GetString());
        Assert.Equal("VerticalCRS", root.GetProperty("source_crs").GetProperty("type").GetString());
        Assert.Equal("CompoundCRS", root.GetProperty("target_crs").GetProperty("type").GetString());
        Assert.Equal("AbridgedTransformation", root.GetProperty("transformation").GetProperty("type").GetString());
        Assert.Equal("Geographic3D to GravityRelatedHeight (EGM)", root.GetProperty("transformation").GetProperty("method").GetProperty("name").GetString());
        Assert.Equal(1, parameters.GetArrayLength());
        Assert.Equal("egm96_15.gtx", parameters[0].GetProperty("value").GetString());
        Assert.Equal(vertical.Name, parsed.Name);
        Assert.Equal(vertical.VerticalDatum.Name, source.VerticalDatum.Name);
        Assert.Null(source.BoundGridTransformation);
        Assert.Equal("egm96_15.gtx", parsed.Transformation.ParameterFileName);
        Assert.Equal(3, target.Dimension);
        Assert.True(GeographicCoordinateSystem.WGS84.EqualParams(Assert.IsType<GeographicCoordinateSystem>(target.HeadCoordinateSystem)));
        Assert.Equal("Ellipsoidal height datum", Assert.IsType<VerticalCoordinateSystem>(target.TailCoordinateSystem).VerticalDatum.Name);
    }

    /// <summary>
    /// Verifies geographic CRS with retained datum-ensemble metadata serialize as PROJJSON <c>datum_ensemble</c>.
    /// </summary>
    [Fact]
    public void ToJson_WithGeographicCoordinateSystemUsingDatumEnsemble_EmitsDatumEnsemble()
    {
        GeographicCoordinateSystem geographic = CreateEnsembleBackedGeographicCoordinateSystem();
        DatumEnsemble expectedEnsemble = Assert.IsType<DatumEnsemble>(geographic.HorizontalDatum.Ensemble);

        string json = ProjJsonWriter.ToJson(geographic);
        GeographicCoordinateSystem parsed = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("GeographicCRS", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("datum_ensemble", out JsonElement datumEnsemble));
        Assert.False(root.TryGetProperty("datum", out _));
        Assert.Equal(expectedEnsemble.Name, datumEnsemble.GetProperty("name").GetString());
        Assert.Equal(expectedEnsemble.Members.Count, datumEnsemble.GetProperty("members").GetArrayLength());
        AssertDatumEnsembleEqual(expectedEnsemble, Assert.IsType<DatumEnsemble>(parsed.HorizontalDatum.Ensemble));
    }

    /// <summary>
    /// Verifies projected CRS with ensemble-backed base datums emit <c>datum_ensemble</c> on the nested base CRS.
    /// </summary>
    [Fact]
    public void ToJson_WithProjectedCoordinateSystemUsingDatumEnsemble_EmitsDatumEnsembleOnBaseCrs()
    {
        ProjectedCoordinateSystem projected = CreateEnsembleBackedProjectedCoordinateSystem();
        DatumEnsemble expectedEnsemble = Assert.IsType<DatumEnsemble>(projected.GeographicCoordinateSystem.HorizontalDatum.Ensemble);

        string json = ProjJsonWriter.ToJson(projected);
        ProjectedCoordinateSystem parsed = Assert.IsType<ProjectedCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement baseCrs = root.GetProperty("base_crs");

        Assert.Equal("ProjectedCRS", root.GetProperty("type").GetString());
        Assert.True(baseCrs.TryGetProperty("datum_ensemble", out JsonElement datumEnsemble));
        Assert.False(baseCrs.TryGetProperty("datum", out _));
        Assert.Equal(expectedEnsemble.Name, datumEnsemble.GetProperty("name").GetString());
        AssertDatumEnsembleEqual(expectedEnsemble, Assert.IsType<DatumEnsemble>(parsed.GeographicCoordinateSystem.HorizontalDatum.Ensemble));
    }

    /// <summary>
    /// Verifies vertical CRS with retained datum-ensemble metadata serialize as PROJJSON <c>datum_ensemble</c>.
    /// </summary>
    [Fact]
    public void ToJson_WithVerticalCoordinateSystemUsingDatumEnsemble_EmitsDatumEnsemble()
    {
        VerticalCoordinateSystem vertical = CreateEnsembleBackedVerticalCoordinateSystem();
        DatumEnsemble expectedEnsemble = Assert.IsType<DatumEnsemble>(vertical.VerticalDatum.Ensemble);

        string json = ProjJsonWriter.ToJson(vertical);
        VerticalCoordinateSystem parsed = Assert.IsType<VerticalCoordinateSystem>(ProjJsonReader.Parse(json));

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("VerticalCRS", root.GetProperty("type").GetString());
        Assert.True(root.TryGetProperty("datum_ensemble", out JsonElement datumEnsemble));
        Assert.False(root.TryGetProperty("datum", out _));
        Assert.Equal(expectedEnsemble.Name, datumEnsemble.GetProperty("name").GetString());
        AssertDatumEnsembleEqual(expectedEnsemble, Assert.IsType<DatumEnsemble>(parsed.VerticalDatum.Ensemble));
    }

    /// <summary>
    /// Verifies WKT2 horizontal <c>BOUNDCRS</c> definitions survive a PROJJSON BoundCRS roundtrip without semantic drift.
    /// </summary>
    [Fact]
    public void ToJson_RoundtripsHorizontalBoundCrsAcrossWkt2AndProjJson()
    {
        GeographicCoordinateSystem original = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ED50 test",
            AngularUnit.Degrees,
            HorizontalDatum.ED50,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        BoundCoordinateSystem fromWkt2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(CoordinateSystemFactory, wkt);

        string json = ProjJsonWriter.ToJson(fromWkt2);
        BoundCoordinateSystem fromProjJson = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.True(fromProjJson.EqualParams(fromWkt2));
    }

    /// <summary>
    /// Verifies WKT2 vertical <c>BOUNDCRS</c> definitions survive a PROJJSON BoundCRS roundtrip without semantic drift.
    /// </summary>
    [Fact]
    public void ToJson_RoundtripsVerticalBoundCrsAcrossWkt2AndProjJson()
    {
        VerticalCoordinateSystem original = CreateBoundVerticalCoordinateSystem();
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        BoundCoordinateSystem fromWkt2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<BoundCoordinateSystem>(CoordinateSystemFactory, wkt);

        string json = ProjJsonWriter.ToJson(fromWkt2);
        BoundCoordinateSystem fromProjJson = Assert.IsType<BoundCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.True(fromProjJson.EqualParams(fromWkt2));
    }

    /// <summary>
    /// Verifies ensemble-backed geographic CRS survive a WKT2 -> PROJJSON roundtrip without losing ensemble metadata.
    /// </summary>
    [Fact]
    public void ToJson_RoundtripsGeographicDatumEnsembleAcrossWkt2AndProjJson()
    {
        GeographicCoordinateSystem original = CreateEnsembleBackedGeographicCoordinateSystem();
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        GeographicCoordinateSystem fromWkt2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(CoordinateSystemFactory, wkt);

        string json = ProjJsonWriter.ToJson(fromWkt2);
        GeographicCoordinateSystem fromProjJson = Assert.IsType<GeographicCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.True(fromProjJson.EqualParams(fromWkt2));
        AssertDatumEnsembleEqual(
            Assert.IsType<DatumEnsemble>(fromWkt2.HorizontalDatum.Ensemble),
            Assert.IsType<DatumEnsemble>(fromProjJson.HorizontalDatum.Ensemble));
    }

    /// <summary>
    /// Verifies ensemble-backed vertical CRS survive a WKT2 -> PROJJSON roundtrip without losing ensemble metadata.
    /// </summary>
    [Fact]
    public void ToJson_RoundtripsVerticalDatumEnsembleAcrossWkt2AndProjJson()
    {
        VerticalCoordinateSystem original = CreateEnsembleBackedVerticalCoordinateSystem();
        string wkt = original.ToWktNode(WktVersion.Wkt22019).ToString();
        VerticalCoordinateSystem fromWkt2 = CoordinateSystemTestHelpers.RequireCoordinateSystem<VerticalCoordinateSystem>(CoordinateSystemFactory, wkt);

        string json = ProjJsonWriter.ToJson(fromWkt2);
        VerticalCoordinateSystem fromProjJson = Assert.IsType<VerticalCoordinateSystem>(ProjJsonReader.Parse(json));

        Assert.True(fromProjJson.EqualParams(fromWkt2));
        AssertDatumEnsembleEqual(
            Assert.IsType<DatumEnsemble>(fromWkt2.VerticalDatum.Ensemble),
            Assert.IsType<DatumEnsemble>(fromProjJson.VerticalDatum.Ensemble));
    }

    private static string GetCatalogWkt(int srid)
    {
        if (!CatalogDefinitions.Value.TryGetValue(srid, out string? wkt))
        {
            throw new InvalidOperationException($"No catalog definition found for EPSG:{srid}.");
        }

        return wkt;
    }

    private static FittedCoordinateSystem CreateDerivedGeographicCoordinateSystem()
    {
        return CoordinateSystemFactory.CreateFittedCoordinateSystem(
            "Local WGS 84",
            GeographicCoordinateSystem.WGS84,
            new AffineTransform(1, 0, 0.5, 0, 1, 1.5),
            [
                new AxisInfo("Local latitude", AxisOrientationEnum.North),
                new AxisInfo("Local longitude", AxisOrientationEnum.East),
            ]);
    }

    private static FittedCoordinateSystem CreateWkt1CompatibleDerivedGeographicCoordinateSystem()
    {
        GeographicCoordinateSystem baseCoordinateSystem = GeographicCoordinateSystem.WGS84;
        return CoordinateSystemFactory.CreateFittedCoordinateSystem(
            "WGS 84 fitted",
            baseCoordinateSystem,
            new AffineTransform(1, 0, 0.5, 0, 1, 1.5),
            [
                new AxisInfo(baseCoordinateSystem.GetAxis(0).Name, baseCoordinateSystem.GetAxis(0).Orientation),
                new AxisInfo(baseCoordinateSystem.GetAxis(1).Name, baseCoordinateSystem.GetAxis(1).Orientation),
            ]);
    }

    private static FittedCoordinateSystem CreateDerivedProjectedCoordinateSystem()
    {
        var baseCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        return CoordinateSystemFactory.CreateFittedCoordinateSystem(
            "Local projected",
            baseCoordinateSystem,
            new AffineTransform(1, 0, 100, 0, 1, -50),
            [
                new AxisInfo("Local easting", AxisOrientationEnum.East),
                new AxisInfo("Local northing", AxisOrientationEnum.North),
            ]);
    }

    private static FittedCoordinateSystem CreateWkt1CompatibleDerivedProjectedCoordinateSystem()
    {
        var baseCoordinateSystem = ProjectedCoordinateSystem.WGS84_UTM(32, true);
        return CoordinateSystemFactory.CreateFittedCoordinateSystem(
            "UTM 32N fitted",
            baseCoordinateSystem,
            new AffineTransform(1, 0, 100, 0, 1, -50),
            [
                new AxisInfo(baseCoordinateSystem.GetAxis(0).Name, baseCoordinateSystem.GetAxis(0).Orientation),
                new AxisInfo(baseCoordinateSystem.GetAxis(1).Name, baseCoordinateSystem.GetAxis(1).Orientation),
            ]);
    }

    private static void AssertFittedCoordinateSystemSemanticsEqual(FittedCoordinateSystem expected, FittedCoordinateSystem actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.ToBase(), actual.ToBase());
        AssertCoordinateSystemAxisMetadataEqual(expected, actual);
    }

    private static void AssertDerivedGeographicBaseSemanticsEqual(GeographicCoordinateSystem expected, GeographicCoordinateSystem actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.True(actual.HorizontalDatum.EqualParams(expected.HorizontalDatum));
        Assert.True(actual.PrimeMeridian.EqualParams(expected.PrimeMeridian));
        Assert.True(actual.AngularUnit.EqualParams(expected.AngularUnit));
    }

    private static void AssertDerivedProjectedBaseSemanticsEqual(ProjectedCoordinateSystem expected, ProjectedCoordinateSystem actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        AssertCoordinateSystemAxisMetadataEqual(expected, actual);
        Assert.True(actual.HorizontalDatum.EqualParams(expected.HorizontalDatum));
        Assert.True(actual.LinearUnit.EqualParams(expected.LinearUnit));
        Assert.True(actual.Projection.EqualParams(expected.Projection));
        AssertDerivedGeographicBaseSemanticsEqual(expected.GeographicCoordinateSystem, actual.GeographicCoordinateSystem);
    }

    private static void AssertCoordinateSystemAxisMetadataEqual(CoordinateSystem expected, CoordinateSystem actual)
    {
        Assert.Equal(expected.Dimension, actual.Dimension);
        for (int dimension = 0; dimension < expected.Dimension; dimension++)
        {
            Assert.Equal(expected.GetAxis(dimension).Name, actual.GetAxis(dimension).Name);
            Assert.Equal(expected.GetAxis(dimension).Orientation, actual.GetAxis(dimension).Orientation);
            Assert.True(actual.GetUnits(dimension).EqualParams(expected.GetUnits(dimension)));
        }
    }

    private static VerticalCoordinateSystem CreateBoundVerticalCoordinateSystem()
    {
        VerticalCoordinateSystem vertical = CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "EGM96 height",
            CoordinateSystemFactory.CreateVerticalDatum("EGM96 geoid", DatumType.VD_GeoidModelDerived),
            LinearUnit.Metre,
            new AxisInfo("gravity-related height (H)", AxisOrientationEnum.Up));
        CompoundCoordinateSystem hub = CoordinateSystemFactory.CreateCompoundCoordinateSystem(
            "WGS 84 + ellipsoidal height",
            GeographicCoordinateSystem.WGS84,
            CreateEllipsoidalHeightVerticalCoordinateSystem());

        vertical.BoundGridTransformation = new VerticalBoundGridTransformation(
            "Geographic3D to GravityRelatedHeight (EGM)",
            "egm96_15.gtx",
            hub);

        return vertical;
    }

    private static VerticalCoordinateSystem CreateEllipsoidalHeightVerticalCoordinateSystem()
    {
        return CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "Ellipsoidal height",
            CoordinateSystemFactory.CreateVerticalDatum("Ellipsoidal height datum", DatumType.VD_Ellipsoidal),
            LinearUnit.Metre,
            new AxisInfo("Ellipsoidal height", AxisOrientationEnum.Up));
    }

    private static GeographicCoordinateSystem CreateEnsembleBackedGeographicCoordinateSystem()
    {
        DatumEnsemble ensemble = new(
            "World Geodetic System 1984 ensemble",
            [
                new DatumEnsembleMember("World Geodetic System 1984 (Transit)", "EPSG", 1166),
                new DatumEnsembleMember("World Geodetic System 1984 (G730)", "EPSG", 1152),
            ],
            2d,
            HorizontalDatum.WGS84.Ellipsoid,
            "EPSG",
            6326);
        HorizontalDatum datum = CoordinateSystemTestHelpers.CloneHorizontalDatumWithMetadata(
            HorizontalDatum.WGS84,
            "World Geodetic System 1984 ensemble",
            "EPSG",
            6326,
            ensemble);

        GeographicCoordinateSystem geographic = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "WGS 84",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Geodetic latitude", AxisOrientationEnum.North),
            new AxisInfo("Geodetic longitude", AxisOrientationEnum.East));
        return CoordinateSystemTestHelpers.WithAuthority(geographic, "EPSG", 4326);
    }

    private static ProjectedCoordinateSystem CreateEnsembleBackedProjectedCoordinateSystem()
    {
        GeographicCoordinateSystem geographic = CreateEnsembleBackedGeographicCoordinateSystem();
        IProjection projection = CoordinateSystemFactory.CreateProjection(
            "UTM zone 32N",
            "Transverse_Mercator",
            new List<ProjectionParameter>
            {
                new("latitude_of_origin", 0),
                new("central_meridian", 9),
                new("scale_factor", 0.9996),
                new("false_easting", 500000),
                new("false_northing", 0),
            });

        return CoordinateSystemFactory.CreateProjectedCoordinateSystem(
            "WGS 84 / UTM zone 32N",
            geographic,
            projection,
            LinearUnit.Metre,
            new AxisInfo("Easting", AxisOrientationEnum.East),
            new AxisInfo("Northing", AxisOrientationEnum.North));
    }

    private static VerticalCoordinateSystem CreateEnsembleBackedVerticalCoordinateSystem()
    {
        DatumEnsemble ensemble = new(
            "Example vertical ensemble",
            [
                new DatumEnsembleMember("Datum A", "TEST", 1),
                new DatumEnsembleMember("Datum B", "TEST", 2),
            ],
            0.05d,
            null,
            "TEST",
            1001);
        VerticalDatum datum = CoordinateSystemTestHelpers.CloneVerticalDatumWithMetadata(
            CoordinateSystemFactory.CreateVerticalDatum("Example vertical ensemble", DatumType.VD_GeoidModelDerived),
            "TEST",
            1001,
            ensemble);

        return CoordinateSystemFactory.CreateVerticalCoordinateSystem(
            "Example ensemble height",
            datum,
            LinearUnit.Metre,
            new AxisInfo("Gravity-related height", AxisOrientationEnum.Up));
    }

    private static void AssertDatumEnsembleEqual(DatumEnsemble expected, DatumEnsemble actual)
    {
        Assert.True(expected.Equals(actual));
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Authority, actual.Authority);
        Assert.Equal(expected.AuthorityCode, actual.AuthorityCode);
        Assert.Equal(expected.Members.Count, actual.Members.Count);
    }
}
