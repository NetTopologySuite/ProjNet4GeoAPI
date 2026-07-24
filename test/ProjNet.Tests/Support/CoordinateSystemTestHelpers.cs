// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.Data;
using Xunit;

/// <summary>
/// Provides shared coordinate-system and WKT parsing helpers for tests.
/// </summary>
internal static class CoordinateSystemTestHelpers
{
    /// <summary>
    /// Creates a fresh coordinate-system factory for tests that need an explicit instance.
    /// </summary>
    /// <returns>A new coordinate-system factory.</returns>
    internal static CoordinateSystemFactory CreateCoordinateSystemFactory()
        => new();

    /// <summary>
    /// Creates a fresh coordinate-transformation factory for tests that need an explicit instance.
    /// </summary>
    /// <returns>A new coordinate-transformation factory.</returns>
    internal static CoordinateTransformationFactory CreateCoordinateTransformationFactory()
        => new();

    /// <summary>
    /// Creates a coordinate-system service with the default test factories.
    /// </summary>
    /// <returns>A new coordinate-system service.</returns>
    internal static CoordinateSystemServices CreateCoordinateSystemServices()
        => new(CreateCoordinateSystemFactory(), CreateCoordinateTransformationFactory());

    /// <summary>
    /// Creates a coordinate-system service with the default test factories and the supplied definitions.
    /// </summary>
    /// <param name="definitions">Coordinate-system definitions to load.</param>
    /// <returns>A new coordinate-system service.</returns>
    internal static CoordinateSystemServices CreateCoordinateSystemServices(IEnumerable<CoordinateSystemDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return new CoordinateSystemServices(CreateCoordinateSystemFactory(), CreateCoordinateTransformationFactory(), definitions);
    }

    /// <summary>
    /// Parses a coordinate system from WKT with a fresh factory and asserts that parsing succeeded.
    /// </summary>
    /// <param name="wkt">Well-known text to parse.</param>
    /// <returns>The parsed coordinate system.</returns>
    internal static CoordinateSystem RequireCoordinateSystem(string wkt)
        => RequireCoordinateSystem(CreateCoordinateSystemFactory(), wkt);

    /// <summary>
    /// Parses a coordinate system from WKT with a fresh factory and asserts that it matches the requested type.
    /// </summary>
    /// <typeparam name="TCoordinateSystem">Expected coordinate system type.</typeparam>
    /// <param name="wkt">Well-known text to parse.</param>
    /// <returns>The parsed coordinate system cast to <typeparamref name="TCoordinateSystem"/>.</returns>
    internal static TCoordinateSystem RequireCoordinateSystem<TCoordinateSystem>(string wkt)
        where TCoordinateSystem : CoordinateSystem
        => RequireCoordinateSystem<TCoordinateSystem>(CreateCoordinateSystemFactory(), wkt);

    /// <summary>
    /// Parses a coordinate system from WKT and asserts that parsing succeeded.
    /// </summary>
    /// <param name="factory">Coordinate system factory.</param>
    /// <param name="wkt">Well-known text to parse.</param>
    /// <returns>The parsed coordinate system.</returns>
    internal static CoordinateSystem RequireCoordinateSystem(CoordinateSystemFactory factory, string wkt)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(wkt);

        CoordinateSystem? coordinateSystem = factory.CreateFromWkt(wkt);
        return Assert.IsType<CoordinateSystem>(coordinateSystem, exactMatch: false);
    }

    /// <summary>
    /// Parses a coordinate system from WKT and asserts that it matches the requested type.
    /// </summary>
    /// <typeparam name="TCoordinateSystem">Expected coordinate system type.</typeparam>
    /// <param name="factory">Coordinate system factory.</param>
    /// <param name="wkt">Well-known text to parse.</param>
    /// <returns>The parsed coordinate system cast to <typeparamref name="TCoordinateSystem"/>.</returns>
    internal static TCoordinateSystem RequireCoordinateSystem<TCoordinateSystem>(CoordinateSystemFactory factory, string wkt)
        where TCoordinateSystem : CoordinateSystem
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(wkt);

        CoordinateSystem? coordinateSystem = factory.CreateFromWkt(wkt);
        return Assert.IsType<TCoordinateSystem>(coordinateSystem);
    }

    /// <summary>
    /// Clones a geographic coordinate system while replacing its authority metadata.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned geographic coordinate system with the requested authority metadata.</returns>
    internal static GeographicCoordinateSystem WithAuthority(GeographicCoordinateSystem coordinateSystem, string authority, long authorityCode)
        => coordinateSystem.WithAuthority(authority, authorityCode);

    /// <summary>
    /// Clones a projected coordinate system while replacing its authority metadata.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned projected coordinate system with the requested authority metadata.</returns>
    internal static ProjectedCoordinateSystem WithAuthority(ProjectedCoordinateSystem coordinateSystem, string authority, long authorityCode)
        => coordinateSystem.WithAuthority(authority, authorityCode);

    /// <summary>
    /// Clones a geocentric coordinate system while replacing its authority metadata.
    /// </summary>
    /// <param name="coordinateSystem">Coordinate system to clone.</param>
    /// <param name="authority">Replacement authority.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <returns>A cloned geocentric coordinate system with the requested authority metadata.</returns>
    internal static GeocentricCoordinateSystem WithAuthority(GeocentricCoordinateSystem coordinateSystem, string authority, long authorityCode)
        => coordinateSystem.WithAuthority(authority, authorityCode);

    /// <summary>
    /// Clones a projected coordinate system while replacing the authority metadata on its base geographic coordinate system.
    /// </summary>
    /// <param name="coordinateSystem">Projected coordinate system to clone.</param>
    /// <param name="authority">Replacement base-geographic authority.</param>
    /// <param name="authorityCode">Replacement base-geographic authority code.</param>
    /// <returns>A cloned projected coordinate system with updated base geographic authority metadata.</returns>
    internal static ProjectedCoordinateSystem WithBaseGeographicAuthority(ProjectedCoordinateSystem coordinateSystem, string authority, long authorityCode)
    {
        GeographicCoordinateSystem geographicCoordinateSystem = WithAuthority(coordinateSystem.GeographicCoordinateSystem, authority, authorityCode);
        return CloneProjectedCoordinateSystem(coordinateSystem, geographicCoordinateSystem: geographicCoordinateSystem);
    }

    /// <summary>
    /// Clones a horizontal datum while replacing its primary metadata fields.
    /// </summary>
    /// <param name="datum">Datum to clone.</param>
    /// <param name="name">Replacement name.</param>
    /// <param name="authority">Replacement authority.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to preserve the source ensemble.</param>
    /// <returns>A cloned horizontal datum with the requested metadata.</returns>
    internal static HorizontalDatum CloneHorizontalDatumWithMetadata(HorizontalDatum datum, string name, string authority, long authorityCode, DatumEnsemble? ensemble = null)
    {
        ArgumentNullException.ThrowIfNull(datum);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(authority);

        HorizontalDatum clone = datum.WithName(name);
        clone = clone.WithAuthority(authority, authorityCode);
        return ensemble is null
            ? clone
            : clone.WithEnsemble(ensemble);
    }

    /// <summary>
    /// Clones a vertical datum while replacing its authority metadata.
    /// </summary>
    /// <param name="datum">Datum to clone.</param>
    /// <param name="authority">Replacement authority.</param>
    /// <param name="authorityCode">Replacement authority code.</param>
    /// <param name="ensemble">Replacement ensemble metadata, or <see langword="null"/> to preserve the source ensemble.</param>
    /// <returns>A cloned vertical datum with the requested authority metadata.</returns>
    internal static VerticalDatum CloneVerticalDatumWithMetadata(VerticalDatum datum, string authority, long authorityCode, DatumEnsemble? ensemble = null)
    {
        ArgumentNullException.ThrowIfNull(datum);
        ArgumentNullException.ThrowIfNull(authority);

        VerticalDatum clone = datum.WithAuthority(authority, authorityCode);
        return ensemble is null
            ? clone
            : clone.WithEnsemble(ensemble);
    }

    private static GeographicCoordinateSystem CloneGeographicCoordinateSystem(
        GeographicCoordinateSystem coordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        HorizontalDatum? horizontalDatum = null)
    {
        ArgumentNullException.ThrowIfNull(coordinateSystem);

        var clone = new GeographicCoordinateSystem(
            coordinateSystem.AngularUnit,
            horizontalDatum ?? coordinateSystem.HorizontalDatum,
            coordinateSystem.PrimeMeridian,
            CloneAxisInfo(coordinateSystem.AxisInfo),
            coordinateSystem.Name,
            authority ?? coordinateSystem.Authority,
            authorityCode ?? coordinateSystem.AuthorityCode,
            coordinateSystem.Alias,
            coordinateSystem.Abbreviation,
            coordinateSystem.Remarks,
            coordinateSystem.DefaultEnvelope,
            CloneWgs84ConversionInfoList(coordinateSystem.WGS84ConversionInfo));

        return clone;
    }

    private static ProjectedCoordinateSystem CloneProjectedCoordinateSystem(
        ProjectedCoordinateSystem coordinateSystem,
        string? authority = null,
        long? authorityCode = null,
        GeographicCoordinateSystem? geographicCoordinateSystem = null)
    {
        ArgumentNullException.ThrowIfNull(coordinateSystem);

        var clone = new ProjectedCoordinateSystem(
            (geographicCoordinateSystem ?? coordinateSystem.GeographicCoordinateSystem).HorizontalDatum,
            geographicCoordinateSystem ?? coordinateSystem.GeographicCoordinateSystem,
            coordinateSystem.LinearUnit,
            coordinateSystem.Projection,
            CloneAxisInfo(coordinateSystem.AxisInfo),
            coordinateSystem.Name,
            authority ?? coordinateSystem.Authority,
            authorityCode ?? coordinateSystem.AuthorityCode,
            coordinateSystem.Alias,
            coordinateSystem.Remarks,
            coordinateSystem.Abbreviation,
            coordinateSystem.DefaultEnvelope);

        return clone;
    }

    private static GeocentricCoordinateSystem CloneGeocentricCoordinateSystem(
        GeocentricCoordinateSystem coordinateSystem,
        string? authority = null,
        long? authorityCode = null)
    {
        ArgumentNullException.ThrowIfNull(coordinateSystem);

        var clone = new GeocentricCoordinateSystem(
            coordinateSystem.HorizontalDatum,
            coordinateSystem.LinearUnit,
            coordinateSystem.PrimeMeridian,
            CloneAxisInfo(coordinateSystem.AxisInfo),
            coordinateSystem.Name,
            authority ?? coordinateSystem.Authority,
            authorityCode ?? coordinateSystem.AuthorityCode,
            coordinateSystem.Alias,
            coordinateSystem.Remarks,
            coordinateSystem.Abbreviation,
            coordinateSystem.DefaultEnvelope);

        return clone;
    }

    private static List<AxisInfo> CloneAxisInfo(List<AxisInfo> axisInfo)
    {
        var clone = new List<AxisInfo>(axisInfo.Count);
        foreach (AxisInfo axis in axisInfo)
        {
            clone.Add(new AxisInfo(axis));
        }

        return clone;
    }

    private static List<Wgs84ConversionInfo> CloneWgs84ConversionInfoList(List<Wgs84ConversionInfo> conversions)
    {
        var clone = new List<Wgs84ConversionInfo>(conversions.Count);
        foreach (Wgs84ConversionInfo conversion in conversions)
        {
            clone.Add(CloneWgs84ConversionInfo(conversion));
        }

        return clone;
    }

    private static Wgs84ConversionInfo CloneWgs84ConversionInfo(Wgs84ConversionInfo conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);
        return new Wgs84ConversionInfo(
            conversion.Dx,
            conversion.Dy,
            conversion.Dz,
            conversion.Ex,
            conversion.Ey,
            conversion.Ez,
            conversion.Ppm,
            conversion.AreaOfUse);
    }
}
