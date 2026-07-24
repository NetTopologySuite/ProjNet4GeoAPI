// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Verifies that public WGS84 convenience accessors stay semantically aligned with the generated EPSG catalog.
/// </summary>
public class Wgs84CatalogConsistencyTests
{
    private static readonly CoordinateSystemFactory Factory = new();
    private static readonly MethodInfo ResolveCatalogCoordinateSystemMethod = typeof(CoordinateSystem).Assembly
        .GetType("ProjNet.Data.Generated.EpsgCoordinateSystemFactory", throwOnError: true)!
        .GetMethod("TryResolveCoordinateSystem", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("The generated EPSG coordinate-system resolver could not be located.");

    /// <summary>
    /// Verifies that the public WGS84 geocentric accessor matches the EPSG catalog entry.
    /// </summary>
    [Fact]
    public void GeocentricWgs84_StaticMatchesCatalogLookup()
    {
        GeocentricCoordinateSystem catalog = ResolveCatalogCoordinateSystem<GeocentricCoordinateSystem>(4978);
        GeocentricCoordinateSystem runtime = GeocentricCoordinateSystem.WGS84;

        Assert.True(runtime.EqualParams(catalog));
        Assert.Equal(catalog.Authority, runtime.Authority);
        Assert.Equal(catalog.AuthorityCode, runtime.AuthorityCode);
    }

    /// <summary>
    /// Verifies that the public WGS84 geographic accessor matches the EPSG catalog entry after legacy lon/lat normalization.
    /// </summary>
    [Fact]
    public void GeographicWgs84_StaticMatchesLegacyNormalizedCatalogLookup()
    {
        GeographicCoordinateSystem catalog = ResolveCatalogCoordinateSystem<GeographicCoordinateSystem>(4326);
        GeographicCoordinateSystem runtime = GeographicCoordinateSystem.WGS84;
        GeographicCoordinateSystem normalizedCatalog = Factory.CreateGeographicCoordinateSystem(
            catalog.Name,
            catalog.AngularUnit,
            catalog.HorizontalDatum,
            catalog.PrimeMeridian,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));

        Assert.True(runtime.EqualParams(normalizedCatalog));
        Assert.Equal(catalog.Authority, runtime.Authority);
        Assert.Equal(catalog.AuthorityCode, runtime.AuthorityCode);
    }

    /// <summary>
    /// Verifies that the normalized public WGS84 geographic accessor reuses the same immutable runtime instance.
    /// </summary>
    [Fact]
    public void GeographicWgs84_StaticReturnsSameNormalizedInstance()
    {
        GeographicCoordinateSystem first = GeographicCoordinateSystem.WGS84;
        GeographicCoordinateSystem second = GeographicCoordinateSystem.WGS84;

        Assert.Same(first, second);
        Assert.Equal(AxisOrientationEnum.East, first.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.North, first.GetAxis(1).Orientation);
    }

    /// <summary>
    /// Verifies that the public Web Mercator accessor matches the EPSG catalog entry after legacy base-CRS normalization.
    /// </summary>
    [Fact]
    public void WebMercator_StaticMatchesLegacyNormalizedCatalogLookup()
    {
        ProjectedCoordinateSystem catalog = ResolveCatalogCoordinateSystem<ProjectedCoordinateSystem>(3857);
        ProjectedCoordinateSystem runtime = ProjectedCoordinateSystem.WebMercator;

        AssertProjectedCoordinateSystemMatchesNormalizedCatalog(runtime, catalog);
    }

    /// <summary>
    /// Verifies that the public Web Mercator accessor reuses the same immutable normalized runtime instance.
    /// </summary>
    [Fact]
    public void WebMercator_StaticReturnsSameNormalizedInstance()
    {
        ProjectedCoordinateSystem first = ProjectedCoordinateSystem.WebMercator;
        ProjectedCoordinateSystem second = ProjectedCoordinateSystem.WebMercator;

        Assert.Same(first, second);
        Assert.Same(GeographicCoordinateSystem.WGS84, first.GeographicCoordinateSystem);
        Assert.Equal(AxisOrientationEnum.East, first.GetAxis(0).Orientation);
        Assert.Equal(AxisOrientationEnum.North, first.GetAxis(1).Orientation);
    }

    /// <summary>
    /// Verifies that the public WGS84 UTM accessor matches EPSG catalog entries after legacy base-CRS normalization.
    /// </summary>
    /// <param name="zone">The UTM zone.</param>
    /// <param name="zoneIsNorth"><see langword="true"/> for the northern hemisphere; otherwise <see langword="false"/>.</param>
    [Theory]
    [InlineData(32, true)]
    [InlineData(32, false)]
    public void Wgs84Utm_StaticMatchesLegacyNormalizedCatalogLookup(int zone, bool zoneIsNorth)
    {
        int srid = 32600 + zone + (zoneIsNorth ? 0 : 100);
        ProjectedCoordinateSystem catalog = ResolveCatalogCoordinateSystem<ProjectedCoordinateSystem>(srid);
        var runtime = ProjectedCoordinateSystem.WGS84_UTM(zone, zoneIsNorth);

        AssertProjectedCoordinateSystemMatchesNormalizedCatalog(runtime, catalog);
    }

    private static void AssertProjectedCoordinateSystemMatchesNormalizedCatalog(ProjectedCoordinateSystem runtime, ProjectedCoordinateSystem catalog)
    {
        GeographicCoordinateSystem normalizedCatalogGeographic = Factory.CreateGeographicCoordinateSystem(
            catalog.GeographicCoordinateSystem.Name,
            catalog.GeographicCoordinateSystem.AngularUnit,
            catalog.GeographicCoordinateSystem.HorizontalDatum,
            catalog.GeographicCoordinateSystem.PrimeMeridian,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        IProjection normalizedCatalogProjection = Factory.CreateProjection(
            catalog.Projection.Name,
            catalog.Projection.ClassName,
            CopyProjectionParameters(catalog.Projection));
        ProjectedCoordinateSystem normalizedCatalog = Factory.CreateProjectedCoordinateSystem(
            catalog.Name,
            normalizedCatalogGeographic,
            normalizedCatalogProjection,
            catalog.LinearUnit,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North));

        Assert.True(runtime.EqualParams(normalizedCatalog));
        Assert.Same(GeographicCoordinateSystem.WGS84, runtime.GeographicCoordinateSystem);
        Assert.Equal(catalog.Authority, runtime.Authority);
        Assert.Equal(catalog.AuthorityCode, runtime.AuthorityCode);
    }

    private static TCoordinateSystem ResolveCatalogCoordinateSystem<TCoordinateSystem>(int srid)
        where TCoordinateSystem : CoordinateSystem
    {
        object?[] arguments = [srid, null];
        bool resolved = (bool)(ResolveCatalogCoordinateSystemMethod.Invoke(null, arguments) ?? false);
        Assert.True(resolved);

        return Assert.IsType<TCoordinateSystem>(arguments[1]);
    }

    private static List<ProjectionParameter> CopyProjectionParameters(IProjection projection)
    {
        var parameters = new List<ProjectionParameter>(projection.NumParameters);
        for (int i = 0; i < projection.NumParameters; i++)
        {
            ProjectionParameter parameter = projection.GetParameter(i);
            parameters.Add(new ProjectionParameter(parameter.Name, parameter.Value));
        }

        return parameters;
    }
}
