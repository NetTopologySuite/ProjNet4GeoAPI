// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Linq;
using ProjNet.CoordinateSystems;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Verifies native WKT2 coordinate-system parsing against real EPSG export examples.
/// </summary>
public class CoordinateSystemWktReaderWkt2Tests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly Lazy<IReadOnlyDictionary<int, string>> CatalogDefinitions = new(() =>
        new ManagedCoordinateSystemDefinitionProvider()
            .GetDefinitions()
            .GroupBy(item => item.Srid)
            .ToDictionary(group => group.Key, group => group.Last().Wkt));

    /// <summary>
    /// Provides WKT2 geodetic CRS examples copied from <c>spec\epsg\EPSG-v12_053-WKT.Zip</c>.
    /// </summary>
    /// <returns>SRID/WKT pairs that should parse successfully.</returns>
    public static IEnumerable<TheoryDataRow<int, string>> SupportedWkt2Rows()
    {
        return
        [
            new TheoryDataRow<int, string>(4230, """GEOGCRS["ED50",DATUM["European Datum 1950",ELLIPSOID["International 1924",6378388,297,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7022]],ID["EPSG",6230]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4230]]"""),
            new TheoryDataRow<int, string>(4267, """GEOGCRS["NAD27",DATUM["North American Datum 1927",ELLIPSOID["Clarke 1866",6378206.4,294.978698213898,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7008]],ID["EPSG",6267]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4267]]"""),
            new TheoryDataRow<int, string>(4277, """GEOGCRS["OSGB36",DATUM["Ordnance Survey of Great Britain 1936",ELLIPSOID["Airy 1830",6377563.396,299.3249646,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7001]],ID["EPSG",6277]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4277]]"""),
            new TheoryDataRow<int, string>(4312, """GEOGCRS["MGI",DATUM["Militar-Geographische Institut",ELLIPSOID["Bessel 1841",6377397.155,299.1528128,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7004]],ID["EPSG",6312]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4312]]"""),
            new TheoryDataRow<int, string>(4314, """GEOGCRS["DHDN",DATUM["Deutsches Hauptdreiecksnetz",ELLIPSOID["Bessel 1841",6377397.155,299.1528128,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7004]],ID["EPSG",6314]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4314]]"""),
            new TheoryDataRow<int, string>(4322, """GEOGCRS["WGS 72",DYNAMIC[FRAMEEPOCH[1972.0]],DATUM["World Geodetic System 1972",ELLIPSOID["WGS 72",6378135,298.26,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7043]],ID["EPSG",6322]],CS[ellipsoidal,2,ID["EPSG",6422]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]],ID["EPSG",4322]]"""),
            new TheoryDataRow<int, string>(10176, """GEODCRS["IGS20",DYNAMIC[FRAMEEPOCH[2015.0]],DATUM["IGS20",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ID["EPSG",1333]],CS[Cartesian,3,ID["EPSG",6500]],AXIS["Geocentric X (X)",geocentricX,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Y (Y)",geocentricY,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Z (Z)",geocentricZ,LENGTHUNIT["metre",1,ID["EPSG",9001]]],ID["EPSG",10176]]"""),
            new TheoryDataRow<int, string>(10412, """GEODCRS["NAD83(CSRS)v8",DATUM["North American Datum of 1983 (CSRS) version 8",ELLIPSOID["GRS 1980",6378137,298.257222101,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7019]],ANCHOREPOCH[2010],ID["EPSG",1365]],CS[Cartesian,3,ID["EPSG",6500]],AXIS["Geocentric X (X)",geocentricX,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Y (Y)",geocentricY,LENGTHUNIT["metre",1,ID["EPSG",9001]]],AXIS["Geocentric Z (Z)",geocentricZ,LENGTHUNIT["metre",1,ID["EPSG",9001]]],DEFININGTRANSFORMATION["ITRF2020 to NAD83(CSRS)v8 (1)",ID["EPSG",10415]],ID["EPSG",10412]]"""),
        ];
    }

    /// <summary>
    /// Verifies supported WKT2 CRS parse to the same semantic model as the committed catalog reference.
    /// </summary>
    /// <param name="srid">Expected EPSG SRID.</param>
    /// <param name="wkt">WKT2 CRS from the EPSG export.</param>
    [Theory]
    [MemberData(nameof(SupportedWkt2Rows))]
    public void CreateFromWkt_ParsesSupportedWkt2CrsEquivalentToCatalogReference(int srid, string wkt)
    {
        CoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem(CoordinateSystemFactory, wkt);
        CoordinateSystem reference = CoordinateSystemTestHelpers.RequireCoordinateSystem(
            CoordinateSystemFactory,
            GetCatalogWkt(srid));

        Assert.IsType(reference.GetType(), parsed);
        Assert.True(parsed.EqualParams(reference), $"WKT2 parse mismatch for EPSG:{srid}.");
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(srid, parsed.AuthorityCode);
    }

    /// <summary>
    /// Verifies explicit prime-meridian units and non-degree angular units survive native WKT2 parsing.
    /// </summary>
    [Fact]
    public void CreateFromWkt_ParsesPrimeMeridianWithExplicitAngularUnit()
    {
        const string wkt = """GEOGCRS["NTF (Paris)",DATUM["Nouvelle Triangulation Francaise (Paris)",ELLIPSOID["Clarke 1880 (IGN)",6378249.2,293.466021293627,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7011]],ID["EPSG",6807]],PRIMEM["Paris",0.040792344,ANGLEUNIT["radian",1,ID["EPSG",9101]],ID["EPSG",8903]],CS[ellipsoidal,2,ID["EPSG",6403]],AXIS["Geodetic latitude (Lat)",north],AXIS["Geodetic longitude (Lon)",east],ANGLEUNIT["grad",0.015707963267949,ID["EPSG",9105]],ID["EPSG",4807]]""";

        GeographicCoordinateSystem parsed = CoordinateSystemTestHelpers.RequireCoordinateSystem<GeographicCoordinateSystem>(CoordinateSystemFactory, wkt);

        Assert.Equal("NTF (Paris)", parsed.Name);
        Assert.Equal("EPSG", parsed.Authority);
        Assert.Equal(4807, parsed.AuthorityCode);
        Assert.Equal("grad", parsed.AngularUnit.Name);
        Assert.Equal("EPSG", parsed.AngularUnit.Authority);
        Assert.Equal(9105, parsed.AngularUnit.AuthorityCode);
        Assert.Equal(0.015707963267949d, parsed.AngularUnit.RadiansPerUnit, 15);
        Assert.Equal("Paris", parsed.PrimeMeridian.Name);
        Assert.Equal("EPSG", parsed.PrimeMeridian.Authority);
        Assert.Equal(8903, parsed.PrimeMeridian.AuthorityCode);
        Assert.True(parsed.PrimeMeridian.AngularUnit.EqualParams(AngularUnit.Radian));
        Assert.Equal(0.040792344d, parsed.PrimeMeridian.Longitude);
        Assert.Equal(AxisOrientationEnum.North, parsed.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.East, parsed.GetAxis(1).Orientation);
    }

    /// <summary>
    /// Verifies ensemble-backed WKT2 geographic CRS still report the current unsupported boundary explicitly.
    /// </summary>
    [Fact]
    public void CreateFromWkt_WithDatumEnsemble_ThrowsNotSupportedException()
    {
        const string wkt = """GEOGCRS["WGS 84",ENSEMBLE["World Geodetic System 1984 ensemble",MEMBER["World Geodetic System 1984 (Transit)",ID["EPSG",1166]],MEMBER["World Geodetic System 1984 (G730)",ID["EPSG",1152]],MEMBER["World Geodetic System 1984 (G873)",ID["EPSG",1153]],MEMBER["World Geodetic System 1984 (G1150)",ID["EPSG",1154]],MEMBER["World Geodetic System 1984 (G1674)",ID["EPSG",1155]],MEMBER["World Geodetic System 1984 (G1762)",ID["EPSG",1156]],MEMBER["World Geodetic System 1984 (G2139)",ID["EPSG",1309]],MEMBER["World Geodetic System 1984 (G2296)",ID["EPSG",1383]],ELLIPSOID["WGS 84",6378137,298.257223563,LENGTHUNIT["metre",1,ID["EPSG",9001]],ID["EPSG",7030]],ENSEMBLEACCURACY[2],ID["EPSG",6326]],CS[ellipsoidal,3,ID["EPSG",6423]],AXIS["Geodetic latitude (Lat)",north,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]]],AXIS["Geodetic longitude (Lon)",east,ANGLEUNIT["degree",0.0174532925199433,ID["EPSG",9102]]],AXIS["Ellipsoidal height (h)",up,LENGTHUNIT["metre",1,ID["EPSG",9001]]],ID["EPSG",4979]]""";

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => CoordinateSystemFactory.CreateFromWkt(wkt));

        Assert.Contains("ensembles", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCatalogWkt(int srid)
    {
        Assert.True(CatalogDefinitions.Value.TryGetValue(srid, out string? wkt), $"SRID {srid} not found in managed EPSG catalog.");
        return wkt ?? string.Empty;
    }
}
