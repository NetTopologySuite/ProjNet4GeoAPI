// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Diagnostics.CodeAnalysis;
using ProjNet.Data.Generated;

/// <summary>
/// Provides bootstrap-safe access to the generated EPSG catalog for the public WGS84 static factories.
/// </summary>
/// <remarks>
/// <para>The generated EPSG factory constructs ellipsoids, datums, and coordinate systems directly from
/// generated records and primitive coordinate-system constructors. It does not depend on the public
/// <c>*.WGS84</c>, <see cref="ProjectedCoordinateSystem.WebMercator"/>, or
/// <see cref="ProjectedCoordinateSystem.WGS84_UTM(int, bool)"/> accessors.</para>
/// <para>This helper captures that dependency boundary so the public convenience accessors can resolve
/// through catalog data without introducing a bootstrap cycle back into the generated factory.</para>
/// </remarks>
internal static class Wgs84CatalogBootstrap
{
    /// <summary>
    /// Gets the EPSG ellipsoid code for WGS 84.
    /// </summary>
    internal const int Wgs84EllipsoidCode = 7030;

    /// <summary>
    /// Gets the EPSG datum code for WGS 84.
    /// </summary>
    internal const int Wgs84DatumCode = 6326;

    /// <summary>
    /// Gets the EPSG SRID for the two-dimensional WGS 84 geographic CRS.
    /// </summary>
    internal const int Wgs84GeographicSrid = 4326;

    /// <summary>
    /// Gets the EPSG SRID for the WGS 84 geocentric CRS.
    /// </summary>
    internal const int Wgs84GeocentricSrid = 4978;

    /// <summary>
    /// Gets the EPSG SRID for Web Mercator.
    /// </summary>
    internal const int WebMercatorSrid = 3857;

    /// <summary>
    /// Tries to resolve a coordinate system from the generated EPSG catalog.
    /// </summary>
    /// <typeparam name="TCoordinateSystem">The expected coordinate-system type.</typeparam>
    /// <param name="srid">The EPSG SRID to resolve.</param>
    /// <param name="coordinateSystem">The resolved coordinate system when available.</param>
    /// <returns><see langword="true"/> when the coordinate system could be resolved as <typeparamref name="TCoordinateSystem"/>; otherwise <see langword="false"/>.</returns>
    internal static bool TryGetCoordinateSystem<TCoordinateSystem>(int srid, [NotNullWhen(true)] out TCoordinateSystem? coordinateSystem)
        where TCoordinateSystem : CoordinateSystem
    {
        if (EpsgCoordinateSystemFactory.TryResolveCoordinateSystem(srid, out CoordinateSystem? resolved) &&
            resolved is TCoordinateSystem typed)
        {
            coordinateSystem = typed;
            return true;
        }

        coordinateSystem = null;
        return false;
    }

    /// <summary>
    /// Resolves the WGS 84 ellipsoid from the generated EPSG catalog.
    /// </summary>
    /// <returns>The generated EPSG ellipsoid instance.</returns>
    internal static Ellipsoid GetWgs84Ellipsoid()
    {
        if (EpsgCoordinateSystemFactory.TryResolveEllipsoid(Wgs84EllipsoidCode, out Ellipsoid? ellipsoid))
        {
            return ellipsoid;
        }

        throw CreateMissingCatalogException($"ellipsoid {Wgs84EllipsoidCode}");
    }

    /// <summary>
    /// Resolves the WGS 84 horizontal datum from the generated EPSG catalog.
    /// </summary>
    /// <returns>The generated EPSG horizontal datum instance.</returns>
    internal static HorizontalDatum GetWgs84Datum()
    {
        if (EpsgCoordinateSystemFactory.TryResolveHorizontalDatum(Wgs84DatumCode, out HorizontalDatum? datum))
        {
            return datum;
        }

        throw CreateMissingCatalogException($"horizontal datum {Wgs84DatumCode}");
    }

    private static InvalidOperationException CreateMissingCatalogException(string item)
        => new($"The generated EPSG catalog could not resolve {item} during WGS84 bootstrap.");
}
