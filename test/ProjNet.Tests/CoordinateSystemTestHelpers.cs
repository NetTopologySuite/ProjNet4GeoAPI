// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems;
using Xunit;

/// <summary>
/// Provides nullable-safe WKT parsing helpers for projection and transform tests.
/// </summary>
internal static class CoordinateSystemTestHelpers
{
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
}
