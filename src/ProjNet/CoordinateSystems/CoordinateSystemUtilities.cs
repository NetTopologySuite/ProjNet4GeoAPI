// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Globalization;

/// <summary>
/// Provides shared coordinate-system utility helpers that are independent from specific
/// projection implementations.
/// </summary>
public static class CoordinateSystemUtilities
{
    /// <summary>
    /// Calculates the UTM zone number for the given longitude.
    /// </summary>
    /// <param name="lon">The longitude in decimal degrees.</param>
    /// <returns>The UTM zone number (1-60).</returns>
    public static long CalcUtmZone(double lon)
    {
        return lon >= 180d
            ? 60L
            : (long)(((lon + 180.0) / 6.0) + 1.0);
    }

    /// <summary>
    /// Converts a longitude value in degrees to radians.
    /// </summary>
    /// <param name="x">The value in degrees to convert to radians.</param>
    /// <param name="edge">If true, -180 and +180 are valid, otherwise they are considered out of range.</param>
    /// <returns>The longitude converted to radians.</returns>
    public static double LongitudeToRadians(double x, bool edge)
    {
        if (edge ? (x >= -180 && x <= 180) : (x > -180 && x < 180))
        {
            return DegreesToRadians(x);
        }

        string longitudeMessage = $"{x.ToString(CultureInfo.InvariantCulture)} not a valid longitude in degrees.";
        ArgumentGuard.ThrowArgumentOutOfRange(nameof(x), longitudeMessage);
        return 0d;
    }

    /// <summary>
    /// Converts a latitude value in degrees to radians.
    /// </summary>
    /// <param name="y">The value in degrees to convert to radians.</param>
    /// <param name="edge">If true, -90 and +90 are valid, otherwise they are considered out of range.</param>
    /// <returns>The latitude converted to radians.</returns>
    public static double LatitudeToRadians(double y, bool edge)
    {
        if (edge ? (y >= -90 && y <= 90) : (y > -90 && y < 90))
        {
            return DegreesToRadians(y);
        }

        string latitudeMessage = $"{y.ToString(CultureInfo.InvariantCulture)} not a valid latitude in degrees.";
        ArgumentGuard.ThrowArgumentOutOfRange(nameof(y), latitudeMessage);
        return 0d;
    }

    private static double DegreesToRadians(double degrees) => Math.PI * degrees / 180.0;
}
