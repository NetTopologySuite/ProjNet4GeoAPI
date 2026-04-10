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
    internal const int Wgs84EllipsoidCode = 7030;
    internal const int Wgs84DatumCode = 6326;
    internal const int Wgs84GeographicSrid = 4326;
    internal const int Wgs84GeocentricSrid = 4978;
    internal const int WebMercatorSrid = 3857;

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

    internal static Ellipsoid GetWgs84Ellipsoid()
    {
        if (EpsgCoordinateSystemFactory.TryResolveEllipsoid(Wgs84EllipsoidCode, out Ellipsoid? ellipsoid))
        {
            return ellipsoid;
        }

        throw CreateMissingCatalogException($"ellipsoid {Wgs84EllipsoidCode}");
    }

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
