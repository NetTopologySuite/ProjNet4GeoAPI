// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Globalization;
using ProjNet.CoordinateSystems;

/// <summary>
/// Resolves PROJ ellipsoid tokens to runtime semi-major and semi-minor axes.
/// </summary>
internal static class ProjEllipsoidResolver
{
    private const double Clarke1880SemiMajorAxis = 6378249.145d;
    private const double Clarke1880InverseFlattening = 293.4663d;
    private const double Clarke1880IgnSemiMajorAxis = 6378249.2d;
    private const double Clarke1880IgnInverseFlattening = 293.4660212936269d;
    private const double BesselSemiMajorAxis = 6377397.155d;
    private const double BesselSemiMinorAxis = 6356078.962818189d;

    /// <summary>
    /// Tries to resolve a supported PROJ ellipsoid token to metric semi-axis values.
    /// </summary>
    /// <param name="token">The ellipsoid token to resolve.</param>
    /// <param name="allowClarke1880Ign">Whether the <c>clrk80ign</c> token is supported by the caller.</param>
    /// <param name="allowBessel">Whether the <c>bessel</c> token is supported by the caller.</param>
    /// <param name="semiMajor">The resolved semi-major axis in metres.</param>
    /// <param name="semiMinor">The resolved semi-minor axis in metres.</param>
    /// <returns><see langword="true"/> when the token is recognized.</returns>
    internal static bool TryResolveKnownEllipsoid(
        string token,
        bool allowClarke1880Ign,
        bool allowBessel,
        out double semiMajor,
        out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
            semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
            return true;
        }

        if (token.Equals("grs80", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad83", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.GRS80.SemiMajorAxis;
            semiMinor = Ellipsoid.GRS80.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk66", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad27", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1866.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1866.SemiMinorAxis;
            return true;
        }

        // PROJ token tables define clrk80/clrk80ign in metres, unlike the historic
        // public Ellipsoid.Clarke1880 model in this codebase, which preserves foot units.
        if (token.Equals("clrk80", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Clarke1880SemiMajorAxis;
            semiMinor = ComputeSemiMinorAxis(Clarke1880SemiMajorAxis, Clarke1880InverseFlattening);
            return true;
        }

        if (allowClarke1880Ign && token.Equals("clrk80ign", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Clarke1880IgnSemiMajorAxis;
            semiMinor = ComputeSemiMinorAxis(Clarke1880IgnSemiMajorAxis, Clarke1880IgnInverseFlattening);
            return true;
        }

        if (token.Equals("intl", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.International1924.SemiMajorAxis;
            semiMinor = Ellipsoid.International1924.SemiMinorAxis;
            return true;
        }

        if (allowBessel && token.Equals("bessel", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = BesselSemiMajorAxis;
            semiMinor = BesselSemiMinorAxis;
            return true;
        }

        if (token.Equals("sphere", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Sphere.SemiMajorAxis;
            semiMinor = Ellipsoid.Sphere.SemiMinorAxis;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Applies explicit PROJ ellipsoid shape overrides to previously resolved semi-axis values.
    /// </summary>
    /// <param name="args">The PROJ argument dictionary.</param>
    /// <param name="semiMajor">The resolved semi-major axis in metres.</param>
    /// <param name="semiMinor">The resolved semi-minor axis in metres.</param>
    /// <param name="errorMessage">An error message when an override token is invalid.</param>
    /// <returns><see langword="true"/> when the override set is valid.</returns>
    internal static bool TryApplyExplicitShapeOverrides(
        IReadOnlyDictionary<string, string> args,
        ref double semiMajor,
        ref double semiMinor,
        out string? errorMessage)
    {
        errorMessage = null;

        if (args.TryGetValue("b", out string? minorToken) && !string.IsNullOrWhiteSpace(minorToken))
        {
            if (!TryParseFiniteDouble(minorToken, out double explicitMinor) || explicitMinor <= 0d)
            {
                errorMessage = "Ellipsoid +b override must be finite and positive.";
                return false;
            }

            semiMinor = explicitMinor;
            return true;
        }

        if (args.TryGetValue("rf", out string? inverseFlatteningToken) && !string.IsNullOrWhiteSpace(inverseFlatteningToken))
        {
            if (!TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening) || inverseFlattening <= 0d)
            {
                errorMessage = "Ellipsoid +rf override must be finite and positive.";
                return false;
            }

            semiMinor = ComputeSemiMinorAxis(semiMajor, inverseFlattening);
            if (semiMinor <= 0d || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
            {
                errorMessage = "Ellipsoid +rf override must resolve to a finite positive semi-minor axis.";
                return false;
            }

            return true;
        }

        if (args.TryGetValue("f", out string? flatteningToken) && !string.IsNullOrWhiteSpace(flatteningToken))
        {
            if (!TryParseFiniteDouble(flatteningToken, out double flattening) || flattening <= 0d || flattening >= 1d)
            {
                errorMessage = "Ellipsoid +f override must be finite and satisfy 0 < f < 1.";
                return false;
            }

            semiMinor = (1d - flattening) * semiMajor;
            return true;
        }

        if (args.TryGetValue("es", out string? eccentricitySquaredToken) && !string.IsNullOrWhiteSpace(eccentricitySquaredToken))
        {
            if (!TryParseFiniteDouble(eccentricitySquaredToken, out double eccentricitySquared) || eccentricitySquared < 0d || eccentricitySquared >= 1d)
            {
                errorMessage = "Ellipsoid +es override must be finite and satisfy 0 <= es < 1.";
                return false;
            }

            semiMinor = semiMajor * Math.Sqrt(1d - eccentricitySquared);
            return true;
        }

        return true;
    }

    private static double ComputeSemiMinorAxis(double semiMajor, double inverseFlattening)
    {
        return (1d - (1d / inverseFlattening)) * semiMajor;
    }

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }
}
