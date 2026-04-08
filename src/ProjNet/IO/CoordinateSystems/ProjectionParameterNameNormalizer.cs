// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.CoordinateSystems;

using System;
using ProjNet;

/// <summary>
/// Normalizes WKT2 and PROJJSON projection parameter names to the internal parameter aliases.
/// </summary>
internal static class ProjectionParameterNameNormalizer
{
    /// <summary>
    /// Normalizes a projection parameter name.
    /// </summary>
    /// <param name="parameterName">The external parameter name to normalize.</param>
    /// <returns>The normalized parameter name.</returns>
    internal static string Normalize(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        string normalized = parameterName
            .ToUpperInvariant()
            .Trim();
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "(", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ")", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "-", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "/", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, " ", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ".", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "__", "_");

        return normalized switch
        {
            "LONGITUDE_OF_NATURAL_ORIGIN" => "central_meridian",
            "LONGITUDE_OF_FALSE_ORIGIN" => "central_meridian",
            "LONGITUDE_OF_PROJECTION_CENTRE" => "central_meridian",
            "LONGITUDE_OF_ORIGIN" => "central_meridian",
            "LATITUDE_OF_NATURAL_ORIGIN" => "latitude_of_origin",
            "LATITUDE_OF_FALSE_ORIGIN" => "latitude_of_origin",
            "LATITUDE_OF_PROJECTION_CENTRE" => "latitude_of_origin",
            "LATITUDE_OF_ORIGIN" => "latitude_of_origin",
            "LATITUDE_OF_1ST_STANDARD_PARALLEL" => "standard_parallel_1",
            "LATITUDE_OF_2ND_STANDARD_PARALLEL" => "standard_parallel_2",
            "LATITUDE_OF_PSEUDO_STANDARD_PARALLEL" => "standard_parallel_1",
            "EASTING_AT_FALSE_ORIGIN" => "false_easting",
            "EASTING_AT_PROJECTION_CENTRE" => "false_easting",
            "EASTING_AT_NATURAL_ORIGIN" => "false_easting",
            "NORTHING_AT_FALSE_ORIGIN" => "false_northing",
            "NORTHING_AT_PROJECTION_CENTRE" => "false_northing",
            "NORTHING_AT_NATURAL_ORIGIN" => "false_northing",
            "SCALE_FACTOR_AT_NATURAL_ORIGIN" => "scale_factor",
            "SCALE_FACTOR_AT_PROJECTION_CENTRE" => "scale_factor",
            "SCALE_FACTOR_ON_INITIAL_LINE" => "scale_factor",
            "AZIMUTH_OF_INITIAL_LINE" => "azimuth",
            "ANGLE_FROM_RECTIFIED_TO_SKEW_GRID" => "rectified_grid_angle",
            _ => normalized,
        };
    }
}
