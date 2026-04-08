// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems;

using System;
using System.Text;

/// <summary>
/// Provides shared outward projection naming and unit conventions for CRS serialization formats.
/// </summary>
internal static class ProjectionSerializationSupport
{
    /// <summary>
    /// Gets the outward-facing projection method name for serialization.
    /// </summary>
    /// <param name="className">The internal projection classification name.</param>
    /// <returns>The serialization-friendly projection method name.</returns>
    internal static string GetMethodName(string className)
    {
        return NormalizeMethodKey(className) switch
        {
            "TRANSVERSE_MERCATOR" => "Transverse Mercator",
            "LAMBERT_CONFORMAL_CONIC_1SP" => "Lambert Conic Conformal (1SP)",
            "LAMBERT_CONFORMAL_CONIC_2SP" => "Lambert Conic Conformal (2SP)",
            "MERCATOR_1SP" => "Mercator (variant A)",
            "MERCATOR_2SP" => "Mercator (variant B)",
            "CASSINI_SOLDNER" => "Cassini-Soldner",
            "ALBERS_CONIC_EQUAL_AREA" => "Albers Equal Area",
            "OBLIQUE_STEREOGRAPHIC" => "Oblique Stereographic",
            "LAMBERT_AZIMUTHAL_EQUAL_AREA" => "Lambert Azimuthal Equal Area",
            "KROVAK" => "Krovak",
            "POPULAR_VISUALISATION_PSEUDO_MERCATOR" => "Popular Visualisation Pseudo Mercator",
            _ => className,
        };
    }

    /// <summary>
    /// Gets the normalized projection method key used by serialization helpers.
    /// </summary>
    /// <param name="className">The internal projection classification name.</param>
    /// <returns>The normalized method key.</returns>
    internal static string NormalizeMethodKey(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return string.Empty;
        }

        string normalized = NormalizeKey(className, replacePeriods: false);
        return normalized switch
        {
            "LAMBERT_CONIC_CONFORMAL_1SP" => "LAMBERT_CONFORMAL_CONIC_1SP",
            "LAMBERT_CONIC_CONFORMAL_2SP" => "LAMBERT_CONFORMAL_CONIC_2SP",
            "MERCATOR_VARIANT_A" => "MERCATOR_1SP",
            "MERCATOR_VARIANT_B" => "MERCATOR_2SP",
            _ => normalized,
        };
    }

    /// <summary>
    /// Gets the outward-facing projection parameter name for serialization.
    /// </summary>
    /// <param name="methodKey">The normalized projection method key.</param>
    /// <param name="parameterName">The internal projection parameter name.</param>
    /// <returns>The serialization-friendly parameter name.</returns>
    internal static string GetParameterName(string methodKey, string parameterName)
    {
        string parameterKey = NormalizeParameterKey(parameterName);
        return methodKey switch
        {
            "LAMBERT_CONFORMAL_CONIC_2SP" => parameterKey switch
            {
                "LATITUDE_OF_ORIGIN" => "Latitude of false origin",
                "CENTRAL_MERIDIAN" => "Longitude of false origin",
                "STANDARD_PARALLEL_1" => "Latitude of 1st standard parallel",
                "STANDARD_PARALLEL_2" => "Latitude of 2nd standard parallel",
                "FALSE_EASTING" => "Easting at false origin",
                "FALSE_NORTHING" => "Northing at false origin",
                _ => GetDefaultParameterName(parameterKey, parameterName),
            },
            _ => GetDefaultParameterName(parameterKey, parameterName),
        };
    }

    /// <summary>
    /// Determines whether a projection parameter should be emitted with an angular unit.
    /// </summary>
    /// <param name="parameterName">The internal projection parameter name.</param>
    /// <returns><see langword="true"/> when the parameter uses angular units.</returns>
    internal static bool ParameterUsesAngularUnit(string parameterName)
    {
        return NormalizeParameterKey(parameterName) switch
        {
            "LATITUDE_OF_ORIGIN" or
            "LONGITUDE_OF_ORIGIN" or
            "CENTRAL_MERIDIAN" or
            "STANDARD_PARALLEL_1" or
            "STANDARD_PARALLEL_2" or
            "LATITUDE_OF_CENTER" or
            "LONGITUDE_OF_CENTER" or
            "LATITUDE_OF_PROJECTION_CENTER" or
            "LONGITUDE_OF_PROJECTION_CENTER" or
            "AZIMUTH" or
            "RECTIFIED_GRID_ANGLE" => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether a projection parameter should be emitted with a linear unit.
    /// </summary>
    /// <param name="parameterName">The internal projection parameter name.</param>
    /// <returns><see langword="true"/> when the parameter uses linear units.</returns>
    internal static bool ParameterUsesLinearUnit(string parameterName)
    {
        return NormalizeParameterKey(parameterName) switch
        {
            "FALSE_EASTING" or
            "FALSE_NORTHING" or
            "EASTING" or
            "NORTHING" or
            "SEMI_MAJOR" or
            "SEMI_MINOR" => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether a projection parameter should be emitted with a scale unit.
    /// </summary>
    /// <param name="parameterName">The internal projection parameter name.</param>
    /// <returns><see langword="true"/> when the parameter uses a scale unit.</returns>
    internal static bool ParameterUsesScaleUnit(string parameterName)
    {
        return NormalizeParameterKey(parameterName) == "SCALE_FACTOR";
    }

    private static string GetDefaultParameterName(string parameterKey, string parameterName)
    {
        return parameterKey switch
        {
            "LATITUDE_OF_ORIGIN" => "Latitude of natural origin",
            "LONGITUDE_OF_ORIGIN" or "CENTRAL_MERIDIAN" => "Longitude of natural origin",
            "STANDARD_PARALLEL_1" => "Latitude of 1st standard parallel",
            "STANDARD_PARALLEL_2" => "Latitude of 2nd standard parallel",
            "FALSE_EASTING" => "False easting",
            "FALSE_NORTHING" => "False northing",
            "SCALE_FACTOR" => "Scale factor at natural origin",
            "LATITUDE_OF_CENTER" or "LATITUDE_OF_PROJECTION_CENTER" => "Latitude of projection centre",
            "LONGITUDE_OF_CENTER" or "LONGITUDE_OF_PROJECTION_CENTER" => "Longitude of projection centre",
            "AZIMUTH" => "Azimuth of initial line",
            "RECTIFIED_GRID_ANGLE" => "Angle from Rectified to Skew Grid",
            _ => parameterName,
        };
    }

    private static string NormalizeParameterKey(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        return NormalizeKey(parameterName, replacePeriods: true);
    }

    private static string NormalizeKey(string value, bool replacePeriods)
    {
        string normalizedValue = value
            .ToUpperInvariant()
            .Trim();
        var builder = new StringBuilder(normalizedValue.Length);
        bool previousWasUnderscore = false;

        foreach (char character in normalizedValue)
        {
            if (character == '(' || character == ')')
            {
                continue;
            }

            char normalizedCharacter = character switch
            {
                '-' or '/' or ' ' => '_',
                '.' when replacePeriods => '_',
                _ => character,
            };

            if (normalizedCharacter == '_')
            {
                if (previousWasUnderscore)
                {
                    continue;
                }

                previousWasUnderscore = true;
            }
            else
            {
                previousWasUnderscore = false;
            }

            builder.Append(normalizedCharacter);
        }

        return builder.ToString();
    }
}
