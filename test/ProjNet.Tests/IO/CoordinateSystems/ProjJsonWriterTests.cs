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
    /// Verifies unsupported coordinate-system types stay an explicit boundary until later M38 slices.
    /// </summary>
    [Fact]
    public void ToJson_WithUnsupportedCoordinateSystem_ThrowsNotSupportedException()
    {
        FittedCoordinateSystem fitted = CoordinateSystemFactory.CreateFittedCoordinateSystem(
            "Fitted WGS 84",
            GeographicCoordinateSystem.WGS84,
            new AffineTransform(1, 0, 0, 0, 1, 0),
            []);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => ProjJsonWriter.ToJson(fitted));

        Assert.Contains(nameof(FittedCoordinateSystem), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies geographic CRS with retained WGS84 conversion metadata still report the current <c>BoundCRS</c> writer boundary explicitly.
    /// </summary>
    [Fact]
    public void ToJson_WithGeographicCoordinateSystemUsingBoundMetadata_ThrowsNotSupportedException()
    {
        GeographicCoordinateSystem geographic = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "ED50 test",
            AngularUnit.Degrees,
            HorizontalDatum.ED50,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => ProjJsonWriter.ToJson(geographic));

        Assert.Contains("BoundCRS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies projected CRS whose base datum retains WGS84 conversion metadata still report the current <c>BoundCRS</c> writer boundary explicitly.
    /// </summary>
    [Fact]
    public void ToJson_WithProjectedCoordinateSystemUsingBoundMetadata_ThrowsNotSupportedException()
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

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => ProjJsonWriter.ToJson(projected));

        Assert.Contains("BoundCRS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies vertical CRS with retained bound-grid metadata still report the current <c>BoundCRS</c> writer boundary explicitly.
    /// </summary>
    [Fact]
    public void ToJson_WithVerticalCoordinateSystemUsingBoundGridMetadata_ThrowsNotSupportedException()
    {
        VerticalCoordinateSystem vertical = CreateBoundVerticalCoordinateSystem();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => ProjJsonWriter.ToJson(vertical));

        Assert.Contains("bound-grid", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BoundCRS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCatalogWkt(int srid)
    {
        if (!CatalogDefinitions.Value.TryGetValue(srid, out string? wkt))
        {
            throw new InvalidOperationException($"No catalog definition found for EPSG:{srid}.");
        }

        return wkt;
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
}
