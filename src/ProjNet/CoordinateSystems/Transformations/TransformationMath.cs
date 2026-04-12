// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Shared numeric helper methods used by transformation runtime implementations.
/// </summary>
internal static class TransformationMath
{
    /// <summary>
    /// Conversion factor from arc-seconds to radians.
    /// </summary>
    internal const double ArcSecondToRadians = Math.PI / (180d * 3600d);

    /// <summary>
    /// Sentinel value used when an observation epoch was not supplied.
    /// </summary>
    internal const double MissingObservationEpoch = double.MaxValue;

    /// <summary>
    /// Sentinel value used by GTX grids for nodata samples.
    /// </summary>
    internal const float GtxNoDataSentinel = -88.88880f;

    /// <summary>
    /// Default iteration cap for inverse grid or deformation refinement loops.
    /// </summary>
    internal const int MaxInverseIterations = 10;

    /// <summary>
    /// Determines whether a floating-point value is finite.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <returns><see langword="true"/> when the value is neither NaN nor infinity.</returns>
    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Determines whether an observation epoch value is valid.
    /// </summary>
    /// <param name="epoch">Observation epoch to validate.</param>
    /// <param name="missingObservationEpoch">Sentinel value used for missing observation epoch.</param>
    /// <returns><see langword="true"/> when the epoch is finite and not equal to the missing sentinel value.</returns>
    internal static bool IsValidObservationEpoch(double epoch, double missingObservationEpoch)
    {
        return IsFinite(epoch) && epoch != missingObservationEpoch;
    }

    /// <summary>
    /// Normalizes a longitude in degrees to the inclusive range [-180, 180].
    /// </summary>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <returns>Normalized longitude in degrees.</returns>
    internal static double NormalizeLongitudeDegrees(double longitude)
    {
        double normalized = longitude;
        while (normalized < -180d)
        {
            normalized += 360d;
        }

        while (normalized > 180d)
        {
            normalized -= 360d;
        }

        return normalized;
    }
}
