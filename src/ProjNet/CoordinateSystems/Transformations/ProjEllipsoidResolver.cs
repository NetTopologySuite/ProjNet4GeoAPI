// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
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
    private const double Airy1830SemiMajorAxis = 6377563.396d;
    private const double Airy1830InverseFlattening = 299.3249646d;
    private const double ModifiedAirySemiMajorAxis = 6377340.189d;
    private const double ModifiedAiryInverseFlattening = 299.3249646d;
    private const double BesselSemiMajorAxis = 6377397.155d;
    private const double BesselSemiMinorAxis = 6356078.962818189d;
    private const double Sixth = 1d / 6d;
    private const double Ra4 = 17d / 360d;
    private const double Ra6 = 67d / 3024d;
    private const double Rv4 = 5d / 72d;
    private const double Rv6 = 55d / 1296d;
    private static readonly string[] SpherificationKeys = ["R_A", "R_V", "R_a", "R_g", "R_h", "R_lat_a", "R_lat_g", "R_C"];

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
            || token.Equals("nad83", StringComparison.OrdinalIgnoreCase)
            || token.Equals("ggrs87", StringComparison.OrdinalIgnoreCase))
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

        if (token.Equals("intl", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nzgd49", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.International1924.SemiMajorAxis;
            semiMinor = Ellipsoid.International1924.SemiMinorAxis;
            return true;
        }

        if (token.Equals("airy", StringComparison.OrdinalIgnoreCase)
            || token.Equals("osgb36", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Airy1830SemiMajorAxis;
            semiMinor = ComputeSemiMinorAxis(Airy1830SemiMajorAxis, Airy1830InverseFlattening);
            return true;
        }

        if (token.Equals("mod_airy", StringComparison.OrdinalIgnoreCase)
            || token.Equals("ire65", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = ModifiedAirySemiMajorAxis;
            semiMinor = ComputeSemiMinorAxis(ModifiedAirySemiMajorAxis, ModifiedAiryInverseFlattening);
            return true;
        }

        if (allowBessel
            && (token.Equals("bessel", StringComparison.OrdinalIgnoreCase)
                || token.Equals("potsdam", StringComparison.OrdinalIgnoreCase)))
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
        return TryApplyExplicitShapeParameters(args, ref semiMajor, ref semiMinor, out errorMessage)
            && TryApplySpherification(args, ref semiMajor, ref semiMinor, out errorMessage);
    }

    /// <summary>
    /// Applies an explicit <c>+a</c> size override while preserving the currently resolved ellipsoid shape.
    /// </summary>
    /// <param name="args">The PROJ argument dictionary.</param>
    /// <param name="semiMajor">The resolved semi-major axis in metres.</param>
    /// <param name="semiMinor">The resolved semi-minor axis in metres.</param>
    /// <param name="errorMessage">An error message when the override token is invalid.</param>
    /// <returns><see langword="true"/> when the override set is valid.</returns>
    internal static bool TryApplySemiMajorOverride(
        IReadOnlyDictionary<string, string> args,
        ref double semiMajor,
        ref double semiMinor,
        out string? errorMessage)
    {
        errorMessage = null;
        if (!TryGetNonEmptyToken(args, "a", out string majorToken))
        {
            return true;
        }

        if (!SpanParseUtility.TryParseFiniteDouble(majorToken, out double explicitSemiMajor) || explicitSemiMajor <= 0d)
        {
            errorMessage = "Ellipsoid +a override must be finite and positive.";
            return false;
        }

        if (semiMajor <= 0d || semiMinor <= 0d)
        {
            errorMessage = "Ellipsoid +a override requires a previously resolved positive ellipsoid shape.";
            return false;
        }

        double scale = explicitSemiMajor / semiMajor;
        semiMajor = explicitSemiMajor;
        semiMinor *= scale;
        return TryValidateResolvedAxes(semiMajor, semiMinor, "+a", out errorMessage);
    }

    private static double ComputeSemiMinorAxis(double semiMajor, double inverseFlattening)
    {
        return (1d - (1d / inverseFlattening)) * semiMajor;
    }

    private static bool TryApplyExplicitShapeParameters(
        IReadOnlyDictionary<string, string> args,
        ref double semiMajor,
        ref double semiMinor,
        out string? errorMessage)
    {
        errorMessage = null;

        if (TryGetNonEmptyToken(args, "rf", out string inverseFlatteningToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening) || inverseFlattening <= 0d)
            {
                errorMessage = "Ellipsoid +rf override must be finite and positive.";
                return false;
            }

            semiMinor = ComputeSemiMinorAxis(semiMajor, inverseFlattening);
            return TryValidateResolvedAxes(semiMajor, semiMinor, "+rf", out errorMessage);
        }

        if (TryGetNonEmptyToken(args, "f", out string flatteningToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(flatteningToken, out double flattening) || flattening < 0d || flattening >= 1d)
            {
                errorMessage = "Ellipsoid +f override must be finite and satisfy 0 <= f < 1.";
                return false;
            }

            semiMinor = (1d - flattening) * semiMajor;
            return TryValidateResolvedAxes(semiMajor, semiMinor, "+f", out errorMessage);
        }

        if (TryGetNonEmptyToken(args, "es", out string eccentricitySquaredToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(eccentricitySquaredToken, out double eccentricitySquared) || eccentricitySquared < 0d || eccentricitySquared >= 1d)
            {
                errorMessage = "Ellipsoid +es override must be finite and satisfy 0 <= es < 1.";
                return false;
            }

            semiMinor = semiMajor * Math.Sqrt(1d - eccentricitySquared);
            return TryValidateResolvedAxes(semiMajor, semiMinor, "+es", out errorMessage);
        }

        if (TryGetNonEmptyToken(args, "e", out string eccentricityToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(eccentricityToken, out double eccentricity) || eccentricity < 0d || eccentricity >= 1d)
            {
                errorMessage = "Ellipsoid +e override must be finite and satisfy 0 <= e < 1.";
                return false;
            }

            semiMinor = semiMajor * Math.Sqrt(1d - (eccentricity * eccentricity));
            return TryValidateResolvedAxes(semiMajor, semiMinor, "+e", out errorMessage);
        }

        if (TryGetNonEmptyToken(args, "b", out string minorToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(minorToken, out double explicitMinor) || explicitMinor <= 0d)
            {
                errorMessage = "Ellipsoid +b override must be finite and positive.";
                return false;
            }

            semiMinor = explicitMinor;
            return TryValidateResolvedAxes(semiMajor, semiMinor, "+b", out errorMessage);
        }

        return true;
    }

    private static bool TryApplySpherification(
        IReadOnlyDictionary<string, string> args,
        ref double semiMajor,
        ref double semiMinor,
        out string? errorMessage)
    {
        errorMessage = null;
        if (!TryFindSpherificationOverride(args, out string? spherificationKey, out string? spherificationValue))
        {
            return true;
        }

        if (!TryComputeEccentricitySquared(semiMajor, semiMinor, out double eccentricitySquared))
        {
            errorMessage = $"Ellipsoid +{spherificationKey} override produced an invalid eccentricity.";
            return false;
        }

        switch (spherificationKey)
        {
            case "R_A":
                semiMajor *= 1d - (eccentricitySquared * (Sixth + (eccentricitySquared * (Ra4 + (eccentricitySquared * Ra6)))));
                break;

            case "R_V":
                semiMajor *= 1d - (eccentricitySquared * (Sixth + (eccentricitySquared * (Rv4 + (eccentricitySquared * Rv6)))));
                break;

            case "R_a":
                semiMajor = (semiMajor + semiMinor) / 2d;
                break;

            case "R_g":
                semiMajor = Math.Sqrt(semiMajor * semiMinor);
                break;

            case "R_h":
                if ((semiMajor + semiMinor) == 0d)
                {
                    errorMessage = "Ellipsoid +R_h override requires a + b to be non-zero.";
                    return false;
                }

                semiMajor = (2d * semiMajor * semiMinor) / (semiMajor + semiMinor);
                break;

            case "R_lat_a":
            case "R_lat_g":
                if (!TryParseSpherificationLatitudeDegrees(spherificationKey, spherificationValue, out double latitudeDegrees, out errorMessage))
                {
                    return false;
                }

                if (!TryComputeLatitudeSpherificationRadius(
                    spherificationKey,
                    semiMajor,
                    eccentricitySquared,
                    latitudeDegrees,
                    out semiMajor,
                    out errorMessage))
                {
                    return false;
                }

                break;

            case "R_C":
                if (!TryGetConformalSphereLatitudeDegrees(args, out double conformalLatitudeDegrees, out errorMessage))
                {
                    return false;
                }

                if (!TryComputeConformalSphereRadius(
                    semiMajor,
                    eccentricitySquared,
                    conformalLatitudeDegrees,
                    out semiMajor,
                    out errorMessage))
                {
                    return false;
                }

                break;

            default:
                errorMessage = $"Unsupported ellipsoid spherification override '+{spherificationKey}'.";
                return false;
        }

        if (!TryValidateResolvedRadius(semiMajor, $"+{spherificationKey}", out errorMessage))
        {
            return false;
        }

        semiMinor = semiMajor;
        return true;
    }

    private static bool TryFindSpherificationOverride(
        IReadOnlyDictionary<string, string> args,
        out string? key,
        out string? value)
    {
        key = null;
        value = null;
        for (int i = 0; i < SpherificationKeys.Length; i++)
        {
            string expectedKey = SpherificationKeys[i];
            foreach (KeyValuePair<string, string> arg in args)
            {
                if (arg.Key.Equals(expectedKey, StringComparison.Ordinal))
                {
                    key = expectedKey;
                    value = arg.Value;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetConformalSphereLatitudeDegrees(
        IReadOnlyDictionary<string, string> args,
        out double latitudeDegrees,
        out string? errorMessage)
    {
        errorMessage = null;
        latitudeDegrees = 0d;
        if (!TryGetNonEmptyToken(args, "lat_0", out string latitudeToken))
        {
            return true;
        }

        if (!TryParseAngleDegreesToken(latitudeToken, out double parsedLatitudeDegrees))
        {
            errorMessage = "Ellipsoid +R_C override requires +lat_0 to be a finite angular value.";
            return false;
        }

        if (Math.Abs(parsedLatitudeDegrees) > 90d)
        {
            errorMessage = "Ellipsoid +R_C override requires +lat_0 to satisfy |lat_0| <= 90°.";
            return false;
        }

        if (TryGetNonEmptyToken(args, "proj", out string projectionToken)
            && projectionToken.Equals("merc", StringComparison.OrdinalIgnoreCase))
        {
            // PROJ validates +lat_0 for merc +R_C, but the conformal sphere radius
            // still behaves like the equatorial case unless +lat_ts changes k0.
            return true;
        }

        latitudeDegrees = parsedLatitudeDegrees;
        return true;
    }

    private static bool TryParseSpherificationLatitudeDegrees(
        string key,
        string? token,
        out double latitudeDegrees,
        out string? errorMessage)
    {
        errorMessage = null;
        latitudeDegrees = 0d;
        if (!TryParseAngleDegreesToken(token ?? string.Empty, out latitudeDegrees))
        {
            errorMessage = $"Ellipsoid +{key} override latitude must be a finite angular value.";
            return false;
        }

        if (Math.Abs(latitudeDegrees) > 90d)
        {
            errorMessage = $"Ellipsoid +{key} override latitude must satisfy |lat| <= 90°.";
            return false;
        }

        return true;
    }

    private static bool TryComputeLatitudeSpherificationRadius(
        string key,
        double semiMajor,
        double eccentricitySquared,
        double latitudeDegrees,
        out double radius,
        out string? errorMessage)
    {
        errorMessage = null;
        double latitudeRadians = latitudeDegrees * Math.PI / 180d;
        double t = 1d - (eccentricitySquared * Math.Sin(latitudeRadians) * Math.Sin(latitudeRadians));
        if (t == 0d)
        {
            radius = 0d;
            errorMessage = $"Ellipsoid +{key} override produced a singular radius at the specified latitude.";
            return false;
        }

        if (key.Equals("R_lat_a", StringComparison.Ordinal))
        {
            radius = semiMajor * ((1d - eccentricitySquared + t) / (2d * t * Math.Sqrt(t)));
            return true;
        }

        radius = semiMajor * (Math.Sqrt(1d - eccentricitySquared) / t);
        return true;
    }

    private static bool TryComputeConformalSphereRadius(
        double semiMajor,
        double eccentricitySquared,
        double latitudeDegrees,
        out double radius,
        out string? errorMessage)
    {
        errorMessage = null;
        double latitudeRadians = latitudeDegrees * Math.PI / 180d;
        double t = 1d - (eccentricitySquared * Math.Sin(latitudeRadians) * Math.Sin(latitudeRadians));
        if (t == 0d)
        {
            radius = 0d;
            errorMessage = "Ellipsoid +R_C override produced a singular radius at the specified latitude.";
            return false;
        }

        radius = semiMajor * (Math.Sqrt(1d - eccentricitySquared) / t);
        return true;
    }

    private static bool TryValidateResolvedAxes(double semiMajor, double semiMinor, string parameterName, out string? errorMessage)
    {
        if (!TryValidateResolvedRadius(semiMajor, parameterName, out errorMessage))
        {
            return false;
        }

        if (!TryValidateResolvedRadius(semiMinor, parameterName, out errorMessage))
        {
            errorMessage = $"Ellipsoid {parameterName} override must resolve to a finite positive semi-minor axis.";
            return false;
        }

        if (!TryComputeEccentricitySquared(semiMajor, semiMinor, out _))
        {
            errorMessage = $"Ellipsoid {parameterName} override produced an invalid eccentricity.";
            return false;
        }

        return true;
    }

    private static bool TryValidateResolvedRadius(double radius, string parameterName, out string? errorMessage)
    {
        errorMessage = null;
        if (radius <= 0d || double.IsNaN(radius) || double.IsInfinity(radius))
        {
            errorMessage = $"Ellipsoid {parameterName} override must resolve to a finite positive radius.";
            return false;
        }

        return true;
    }

    private static bool TryComputeEccentricitySquared(double semiMajor, double semiMinor, out double eccentricitySquared)
    {
        eccentricitySquared = double.NaN;
        if (semiMajor <= 0d || semiMinor <= 0d || double.IsNaN(semiMajor) || double.IsNaN(semiMinor) || double.IsInfinity(semiMajor) || double.IsInfinity(semiMinor))
        {
            return false;
        }

        double axisRatio = semiMinor / semiMajor;
        eccentricitySquared = 1d - (axisRatio * axisRatio);
        return !double.IsNaN(eccentricitySquared)
            && !double.IsInfinity(eccentricitySquared)
            && eccentricitySquared >= 0d;
    }

    private static bool TryGetNonEmptyToken(IReadOnlyDictionary<string, string> args, string key, out string token)
    {
        if (args.TryGetValue(key, out string? rawToken) && !string.IsNullOrWhiteSpace(rawToken))
        {
            token = rawToken;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private static bool TryParseAngleDegreesToken(string token, out double value)
    {
        value = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        bool radiansSuffix = normalized.Length > 0 && (normalized[^1] == 'r' || normalized[^1] == 'R');
        if (radiansSuffix)
        {
            normalized = normalized[..^1];
        }

        if (TryParseNumericToken(normalized, out value))
        {
            if (radiansSuffix)
            {
                value *= 180d / Math.PI;
            }

            return true;
        }

        return TryParseDmsToken(normalized, out value);
    }

    private static bool TryParseNumericToken(string token, out double value)
    {
        value = 0d;
        if (!SpanParseUtility.TryParseFiniteDouble(token, out value))
        {
            int slashIndex = IndexOfOrdinal(token, '/');
            if (slashIndex <= 0 || slashIndex >= token.Length - 1)
            {
                return false;
            }

            string numeratorToken = token[..slashIndex].Trim();
            string denominatorToken = token[(slashIndex + 1)..].Trim();
            if (!SpanParseUtility.TryParseFiniteDouble(numeratorToken, out double numerator)
                || !SpanParseUtility.TryParseFiniteDouble(denominatorToken, out double denominator)
                || denominator == 0d)
            {
                return false;
            }

            value = numerator / denominator;
        }

        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool TryParseDmsToken(string token, out double value)
    {
        value = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string text = token.Trim()
            .Replace('°', 'd')
            .Replace('º', 'd');
        int sign = 1;

        char last = text[text.Length - 1];
        if (last == 'W' || last == 'w' || last == 'S' || last == 's')
        {
            sign = -1;
            text = text[..^1];
        }
        else if (last == 'E' || last == 'e' || last == 'N' || last == 'n')
        {
            text = text[..^1];
        }

        if (text.Length > 0 && text[0] == '-')
        {
            sign *= -1;
            text = text[1..];
        }
        else if (text.Length > 0 && text[0] == '+')
        {
            text = text[1..];
        }

        int dIndex = IndexOfOrdinal(text, 'd');
        if (dIndex < 0)
        {
            dIndex = IndexOfOrdinal(text, 'D');
        }

        int mIndex = IndexOfOrdinal(text, '\'');
        if (dIndex <= 0 || mIndex <= dIndex)
        {
            return false;
        }

        string degreesToken = text[..dIndex];
        string minutesToken = text.Substring(dIndex + 1, mIndex - dIndex - 1);
        if (!SpanParseUtility.TryParseFiniteDouble(degreesToken, out double degrees)
            || !SpanParseUtility.TryParseFiniteDouble(minutesToken, out double minutes))
        {
            return false;
        }

        double seconds = 0d;
        int secondsMarker = IndexOfOrdinal(text, '"');
        if (secondsMarker > mIndex + 1)
        {
            string secondsToken = text.Substring(mIndex + 1, secondsMarker - mIndex - 1);
            if (!SpanParseUtility.TryParseFiniteDouble(secondsToken, out seconds))
            {
                return false;
            }
        }

        value = sign * (degrees + (minutes / 60d) + (seconds / 3600d));
        return true;
    }

    private static int IndexOfOrdinal(string text, char value)
    {
#if NETSTANDARD2_0
        return text.IndexOf(value);
#else
        return text.IndexOf(value, StringComparison.Ordinal);
#endif
    }
}
