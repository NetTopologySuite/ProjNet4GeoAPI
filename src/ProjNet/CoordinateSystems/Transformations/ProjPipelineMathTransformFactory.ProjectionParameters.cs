// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Creates runtime math transforms from PROJ-style pipeline operation strings.
/// </summary>
internal static partial class ProjPipelineMathTransformFactory
{
    private static bool TryBuildProjectionStepParameters(
        Dictionary<string, string> args,
        string projCode,
        [NotNullWhen(true)] out List<ProjectionParameter>? parameters,
        out string? skipReason)
    {
        parameters = null;
        if (!TryResolveProjectionEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        if (!TryResolveProjectionUnitFactor(args, out double unitFactor, out skipReason))
        {
            return false;
        }

        List<ProjectionParameter> projectionParameters = CreateDefaultProjectionStepParameters(semiMajor, semiMinor, unitFactor);
        if (!TryApplyDefaultProjectionStepParameters(args, projectionParameters, out skipReason)
            || !TryApplyProjectionScaleFactor(args, projectionParameters, out skipReason))
        {
            return false;
        }

        if (args.ContainsKey("south"))
        {
            SetOrAddProjectionParameter(projectionParameters, "south", 1d);
        }

        if (!TryApplyProjectionSpecificParameters(args, projCode, unitFactor, projectionParameters, out skipReason))
        {
            return false;
        }

        parameters = projectionParameters;
        return true;
    }

    private static List<ProjectionParameter> CreateDefaultProjectionStepParameters(double semiMajor, double semiMinor, double unitFactor)
    {
        return
        [
            new("latitude_of_origin", 0d),
            new("central_meridian", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
            new("semi_major", semiMajor),
            new("semi_minor", semiMinor),
            new("unit", unitFactor),
        ];
    }

    private static bool TryApplyDefaultProjectionStepParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        if (!TryApplyOptionalProjectionParameter(args, "lat_0", "latitude_of_origin", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lon_0", "central_meridian", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "x_0", "false_easting", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "y_0", "false_northing", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_1", "standard_parallel_1", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_2", "standard_parallel_2", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lonc", "longitude_of_center", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_ts", "lat_ts", parameters, out skipReason))
        {
            return false;
        }

        return TryApplyPrimeMeridianOffset(args, parameters, out skipReason);
    }

    private static bool TryApplyProjectionScaleFactor(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (args.TryGetValue("k_0", out string? k0Token) && !string.IsNullOrWhiteSpace(k0Token))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(k0Token, out double k0))
            {
                skipReason = "Invalid value for +k_0.";
                return false;
            }

            SetOrAddProjectionParameter(parameters, "scale_factor", k0);
            return true;
        }

        if (args.TryGetValue("k", out string? kToken) && !string.IsNullOrWhiteSpace(kToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(kToken, out double k))
            {
                skipReason = "Invalid value for +k.";
                return false;
            }

            SetOrAddProjectionParameter(parameters, "scale_factor", k);
        }

        return true;
    }

    private static bool TryApplyProjectionSpecificParameters(
        Dictionary<string, string> args,
        string projCode,
        double unitFactor,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        switch (projCode.ToUpperInvariant())
        {
            case "CASS":
                skipReason = null;
                return TryApplyCassProjectionParameters(args, parameters, out skipReason);
            case "AEQD":
                skipReason = null;
                return TryApplyAeqdProjectionParameters(args, parameters, out skipReason);
            case "AIROCEAN":
                return TryApplyAiroceanProjectionParameters(args, parameters, out skipReason);
            case "PEIRCE_Q":
                return TryApplyPeirceProjectionParameters(args, parameters, out skipReason);
            case "KROVAK":
            case "MOD_KROVAK":
                return TryApplyKrovakProjectionParameters(args, parameters, out skipReason);
            case "SPILHAUS":
                return TryApplySpilhausProjectionParameters(args, parameters, out skipReason);
            case "AIRY":
                return TryApplyAiryProjectionParameters(args, parameters, out skipReason);
            case "URM5":
                return TryApplyUrm5ProjectionParameters(args, parameters, out skipReason);
            case "UTM":
                return TryApplyUtmProjectionParameters(args, unitFactor, parameters, out skipReason);
            default:
                skipReason = null;
                return true;
        }
    }

    private static bool TryApplyCassProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        if (args.ContainsKey("hyperbolic"))
        {
            SetOrAddProjectionParameter(parameters, "hyperbolic", 1d);
        }

        skipReason = null;
        return true;
    }

    private static bool TryApplyAeqdProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        if (args.ContainsKey("guam"))
        {
            SetOrAddProjectionParameter(parameters, "guam", 1d);
        }

        skipReason = null;
        return true;
    }

    private static bool TryApplyAiroceanProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue("orient", out string? orientationToken) || string.IsNullOrWhiteSpace(orientationToken))
        {
            return true;
        }

        if (!TryResolveAiroceanOrientationCode(orientationToken, out double orientationCode))
        {
            skipReason = "Invalid value for +orient on airocean step.";
            return false;
        }

        SetOrAddProjectionParameter(parameters, "airocean_orient", orientationCode);
        return true;
    }

    private static bool TryApplyPeirceProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (args.TryGetValue("shape", out string? shapeToken) && !string.IsNullOrWhiteSpace(shapeToken))
        {
            if (!TryResolvePeirceShapeCode(shapeToken, out double shapeCode))
            {
                skipReason = "Invalid value for +shape on peirce_q step.";
                return false;
            }

            SetOrAddProjectionParameter(parameters, "shape", shapeCode);
        }

        return TryApplyOptionalProjectionParameter(args, "scrollx", "scrollx", parameters, out skipReason)
            && TryApplyOptionalProjectionParameter(args, "scrolly", "scrolly", parameters, out skipReason);
    }

    private static bool TryApplyKrovakProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (args.TryGetValue("lat_1", out string? pseudoStandardParallelToken) && !string.IsNullOrWhiteSpace(pseudoStandardParallelToken))
        {
            if (!SpanParseUtility.TryParseFiniteDouble(pseudoStandardParallelToken, out double pseudoStandardParallel))
            {
                skipReason = "Invalid value for +lat_1 on krovak step.";
                return false;
            }

            SetOrAddProjectionParameter(parameters, "pseudo_standard_parallel_1", pseudoStandardParallel);
        }
        else
        {
            SetOrAddProjectionParameter(parameters, "pseudo_standard_parallel_1", 78.5d);
        }

        if (!TryApplyOptionalProjectionParameter(args, "alpha", "azimuth", parameters, out skipReason))
        {
            return false;
        }

        if (args.ContainsKey("czech"))
        {
            SetOrAddProjectionParameter(parameters, "czech", 1d);
        }

        return true;
    }

    private static bool TryApplySpilhausProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.ContainsKey("lat_0"))
        {
            SetOrAddProjectionParameter(parameters, "latitude_of_origin", -49.56371678d);
        }

        if (!args.ContainsKey("lon_0"))
        {
            SetOrAddProjectionParameter(parameters, "central_meridian", 66.94970198d);
        }

        return TryApplyOptionalProjectionParameter(args, "azi", "azi", parameters, out skipReason)
            && TryApplyOptionalProjectionParameter(args, "rot", "rot", parameters, out skipReason);
    }

    private static bool TryApplyAiryProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        if (!TryApplyOptionalProjectionParameter(args, "lat_b", "lat_b", parameters, out skipReason))
        {
            return false;
        }

        if (args.ContainsKey("no_cut"))
        {
            SetOrAddProjectionParameter(parameters, "no_cut", 1d);
        }

        return true;
    }

    private static bool TryApplyUrm5ProjectionParameters(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        if (!args.TryGetValue("n", out string? nToken) || string.IsNullOrWhiteSpace(nToken))
        {
            skipReason = "urm5 step requires +n parameter.";
            return false;
        }

        if (!SpanParseUtility.TryParseFiniteDouble(nToken, out double n))
        {
            skipReason = "Invalid value for +n.";
            return false;
        }

        SetOrAddProjectionParameter(parameters, "n", n);
        return TryApplyOptionalProjectionParameter(args, "q", "q", parameters, out skipReason)
            && TryApplyOptionalProjectionParameter(args, "alpha", "alpha", parameters, out skipReason);
    }

    private static bool TryApplyUtmProjectionParameters(
        Dictionary<string, string> args,
        double unitFactor,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        if (!TryGetZoneCentralMeridian(args, out double centralMeridian))
        {
            skipReason = "utm step requires a valid +zone parameter.";
            return false;
        }

        SetOrAddProjectionParameter(parameters, "latitude_of_origin", 0d);
        SetOrAddProjectionParameter(parameters, "central_meridian", centralMeridian);
        SetOrAddProjectionParameter(parameters, "scale_factor", 0.9996d);
        SetOrAddProjectionParameter(parameters, "false_easting", 500000d / unitFactor);
        SetOrAddProjectionParameter(parameters, "false_northing", (args.ContainsKey("south") ? 10000000d : 0d) / unitFactor);

        skipReason = null;
        return true;
    }

    private static bool TryResolvePeirceShapeCode(string token, out double shapeCode)
    {
        shapeCode = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (SpanParseUtility.TryParseFiniteDouble(normalized, out shapeCode))
        {
            return true;
        }

        shapeCode = normalized.ToUpperInvariant() switch
        {
            "SQUARE" => 0d,
            "DIAMOND" => 1d,
            "NHEMISPHERE" => 2d,
            "SHEMISPHERE" => 3d,
            "HORIZONTAL" => 4d,
            "VERTICAL" => 5d,
            _ => double.NaN,
        };

        return !double.IsNaN(shapeCode);
    }

    private static bool TryResolveProjectionUnitFactor(
        Dictionary<string, string> args,
        out double unitFactor,
        out string? skipReason)
    {
        unitFactor = 1d;
        skipReason = null;

        if (args.TryGetValue("to_meter", out string? toMeterToken) && !string.IsNullOrWhiteSpace(toMeterToken))
        {
            if (!TryParsePositiveScaleFactor(toMeterToken, out unitFactor))
            {
                skipReason = "Unable to parse +to_meter parameter for projection step.";
                return false;
            }
        }
        else if (args.TryGetValue("units", out string? unitsToken)
            && !string.IsNullOrWhiteSpace(unitsToken)
            && !TryResolveUnitFactor(unitsToken, out unitFactor))
        {
            skipReason = "Unable to parse +units parameter for projection step.";
            return false;
        }

        return true;
    }

    private static bool TryResolveDatumToWgs84Parameters(
        Dictionary<string, string> args,
        out Wgs84ConversionInfo? toWgs84,
        out string? skipReason)
    {
        toWgs84 = null;
        skipReason = null;

        if (args.TryGetValue("towgs84", out string? towgs84Token) && !string.IsNullOrWhiteSpace(towgs84Token))
        {
            string[] values = towgs84Token.Split(CommaSeparator, StringSplitOptions.None);
            if (values.Length != 3 && values.Length != 6 && values.Length != 7)
            {
                skipReason = "Invalid value for +towgs84.";
                return false;
            }

            if (!SpanParseUtility.TryParseFiniteDouble(values[0], out double dx)
                || !SpanParseUtility.TryParseFiniteDouble(values[1], out double dy)
                || !SpanParseUtility.TryParseFiniteDouble(values[2], out double dz))
            {
                skipReason = "Invalid value for +towgs84.";
                return false;
            }

            double rx = 0d;
            double ry = 0d;
            double rz = 0d;
            double ppm = 0d;

            if (values.Length >= 6)
            {
                if (!SpanParseUtility.TryParseFiniteDouble(values[3], out rx)
                    || !SpanParseUtility.TryParseFiniteDouble(values[4], out ry)
                    || !SpanParseUtility.TryParseFiniteDouble(values[5], out rz))
                {
                    skipReason = "Invalid value for +towgs84.";
                    return false;
                }
            }

            if (values.Length == 7 && !SpanParseUtility.TryParseFiniteDouble(values[6], out ppm))
            {
                skipReason = "Invalid value for +towgs84.";
                return false;
            }

            toWgs84 = new Wgs84ConversionInfo(dx, dy, dz, rx, ry, rz, ppm);
            return true;
        }

        if (args.TryGetValue("datum", out string? datumToken) && !string.IsNullOrWhiteSpace(datumToken))
        {
            TryResolveKnownDatumToWgs84Parameters(datumToken, out toWgs84);
        }

        return true;
    }

    private static bool TryResolveKnownDatumToWgs84Parameters(string datumToken, out Wgs84ConversionInfo? toWgs84)
    {
        toWgs84 = null;
        if (string.IsNullOrWhiteSpace(datumToken))
        {
            return false;
        }

        if (datumToken.Equals("potsdam", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo(598.1, 73.7, 418.2, 0.202, 0.045, -2.455, 6.7);
            return true;
        }

        if (datumToken.Equals("NAD27", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo(-8, 160, 176, 0, 0, 0, 0);
            return true;
        }

        if (datumToken.Equals("NAD83", StringComparison.OrdinalIgnoreCase)
            || datumToken.Equals("WGS84", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo();
            return true;
        }

        if (datumToken.Equals("nzgd49", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo(59.47, -5.04, 187.44, 0.47, -0.1, 1.024, -4.5993);
            return true;
        }

        if (datumToken.Equals("ire65", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo(482.530, -130.596, 564.557, -1.042, -0.214, -0.631, 8.15);
            return true;
        }

        if (datumToken.Equals("GGRS87", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo(-199.87, 74.79, 246.02, 0, 0, 0, 0);
            return true;
        }

        if (datumToken.Equals("OSGB36", StringComparison.OrdinalIgnoreCase))
        {
            toWgs84 = new Wgs84ConversionInfo(446.448, -125.157, 542.060, 0.1502, 0.2470, 0.8421, -20.4894);
            return true;
        }

        return false;
    }

    private static bool TryResolveVerticalUnitFactor(
        Dictionary<string, string> args,
        out double unitFactor,
        out string? skipReason)
    {
        unitFactor = 1d;
        skipReason = null;

        if (args.TryGetValue("vto_meter", out string? vtoMeterToken) && !string.IsNullOrWhiteSpace(vtoMeterToken))
        {
            if (!TryParsePositiveScaleFactor(vtoMeterToken, out unitFactor))
            {
                skipReason = "Unable to parse +vto_meter parameter for projection step.";
                return false;
            }
        }
        else if (args.TryGetValue("vunits", out string? vunitsToken)
            && !string.IsNullOrWhiteSpace(vunitsToken)
            && !TryResolveUnitFactor(vunitsToken, out unitFactor))
        {
            skipReason = "Unable to parse +vunits parameter for projection step.";
            return false;
        }

        return true;
    }

    private static bool TryApplyOptionalProjectionParameter(
        Dictionary<string, string> args,
        string sourceKey,
        string targetName,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue(sourceKey, out string? token) || string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        if (!SpanParseUtility.TryParseFiniteDouble(token, out double value))
        {
            skipReason = $"Invalid value for +{sourceKey}.";
            return false;
        }

        SetOrAddProjectionParameter(parameters, targetName, value);
        return true;
    }

    private static void SetOrAddProjectionParameter(
        List<ProjectionParameter> parameters,
        string name,
        double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }

    private static bool TryApplyPrimeMeridianOffset(
        Dictionary<string, string> args,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue("pm", out string? pmToken) || string.IsNullOrWhiteSpace(pmToken))
        {
            return true;
        }

        if (!TryResolvePrimeMeridianLongitudeDegrees(pmToken, out double primeMeridianLongitudeDegrees))
        {
            skipReason = "Invalid or unsupported value for +pm.";
            return false;
        }

        AddProjectionLongitudeOffset(parameters, "central_meridian", primeMeridianLongitudeDegrees);
        AddProjectionLongitudeOffset(parameters, "longitude_of_center", primeMeridianLongitudeDegrees);
        return true;
    }

    private static void AddProjectionLongitudeOffset(
        List<ProjectionParameter> parameters,
        string name,
        double offsetDegrees)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, parameters[i].Value + offsetDegrees);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, offsetDegrees));
    }

    private static bool TryResolveProjectionEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor,
        out string? skipReason)
    {
        skipReason = null;

        if (args.TryGetValue("r", out string? radiusToken)
            && SpanParseUtility.TryParseFiniteDouble(radiusToken, out double radius)
            && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            if (ProjEllipsoidResolver.TryResolveKnownEllipsoid(
                ellps,
                allowClarke1880Ign: true,
                allowBessel: true,
                out semiMajor,
                out semiMinor))
            {
                if (!ProjEllipsoidResolver.TryApplySemiMajorOverride(args, ref semiMajor, ref semiMinor, out skipReason))
                {
                    return false;
                }

                return ProjEllipsoidResolver.TryApplyExplicitShapeOverrides(args, ref semiMajor, ref semiMinor, out skipReason);
            }

            skipReason = "Projection step received unsupported +ellps value.";
            return false;
        }

        if (args.TryGetValue("datum", out string? datum) && !string.IsNullOrWhiteSpace(datum))
        {
            if (ProjEllipsoidResolver.TryResolveKnownEllipsoid(
                datum,
                allowClarke1880Ign: true,
                allowBessel: true,
                out semiMajor,
                out semiMinor))
            {
                if (!ProjEllipsoidResolver.TryApplySemiMajorOverride(args, ref semiMajor, ref semiMinor, out skipReason))
                {
                    return false;
                }

                return ProjEllipsoidResolver.TryApplyExplicitShapeOverrides(args, ref semiMajor, ref semiMinor, out skipReason);
            }

            skipReason = "Projection step received unsupported +datum value.";
            return false;
        }

        if (args.TryGetValue("a", out string? majorToken)
            && SpanParseUtility.TryParseFiniteDouble(majorToken, out double major)
            && major > 0d)
        {
            semiMajor = major;
            semiMinor = major;
            return ProjEllipsoidResolver.TryApplyExplicitShapeOverrides(args, ref semiMajor, ref semiMinor, out skipReason);
        }

        semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
        semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
        return ProjEllipsoidResolver.TryApplyExplicitShapeOverrides(args, ref semiMajor, ref semiMinor, out skipReason);
    }

    private static bool TryGetZoneCentralMeridian(Dictionary<string, string> args, out double centralMeridian)
    {
        centralMeridian = 0d;
        if (!args.TryGetValue("zone", out string? zoneToken) || string.IsNullOrWhiteSpace(zoneToken))
        {
            return false;
        }

        string digits = zoneToken.Trim();
        int zone = 0;
        int index = 0;
        while (index < digits.Length && char.IsDigit(digits[index]))
        {
            int digit = digits[index] - '0';
            if (zone > ((int.MaxValue - digit) / 10))
            {
                return false;
            }

            zone = (zone * 10) + digit;
            index++;
        }

        if (index == 0)
        {
            return false;
        }

        if (zone is < 1 or > 60)
        {
            return false;
        }

        centralMeridian = (zone * 6d) - 183d;
        return true;
    }

    private static bool TryResolveGeocentricScale(
        Dictionary<string, string> args,
        out double scale,
        out string? skipReason)
    {
        scale = 1d;
        skipReason = null;

        if (args.TryGetValue("units", out string? unitsToken)
            && !string.IsNullOrWhiteSpace(unitsToken)
            && !unitsToken.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            skipReason = "geocent/cart currently supports only +units=m.";
            return false;
        }

        if (args.TryGetValue("to_meter", out string? toMeterToken) && !string.IsNullOrWhiteSpace(toMeterToken))
        {
            if (!TryParsePositiveScaleFactor(toMeterToken, out scale))
            {
                skipReason = "Unable to parse +to_meter parameter for geocent/cart.";
                return false;
            }
        }

        return true;
    }

    private static bool TryParsePositiveScaleFactor(string token, out double scale)
    {
        scale = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedScale))
        {
            if (parsedScale <= 0d || double.IsNaN(parsedScale) || double.IsInfinity(parsedScale))
            {
                return false;
            }

            scale = parsedScale;
            return true;
        }

#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        int slashIndex = token.IndexOf('/', StringComparison.Ordinal);
#else
        int slashIndex = token.IndexOf('/');
#endif
        if (slashIndex <= 0 || slashIndex >= token.Length - 1)
        {
            return false;
        }

        string numeratorToken = token[..slashIndex].Trim();
        string denominatorToken = token[(slashIndex + 1)..].Trim();
        if (!double.TryParse(numeratorToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double numerator)
            || !double.TryParse(denominatorToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double denominator))
        {
            return false;
        }

        if (denominator == 0d)
        {
            return false;
        }

        parsedScale = numerator / denominator;
        if (parsedScale <= 0d || double.IsNaN(parsedScale) || double.IsInfinity(parsedScale))
        {
            return false;
        }

        scale = parsedScale;
        return true;
    }

    private static bool TryResolveAiroceanOrientationCode(string token, out double orientationCode)
    {
        orientationCode = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (SpanParseUtility.TryParseFiniteDouble(normalized, out orientationCode))
        {
            return orientationCode == 0d || orientationCode == 1d;
        }

        orientationCode = normalized.ToUpperInvariant() switch
        {
            "VERTICAL" => 0d,
            "HORIZONTAL" => 1d,
            _ => double.NaN,
        };

        return !double.IsNaN(orientationCode);
    }

    private static bool TryResolvePrimeMeridianLongitudeDegrees(string token, out double longitudeDegrees)
    {
        longitudeDegrees = 0d;
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

        if (SpanParseUtility.TryParseFiniteDouble(normalized, out longitudeDegrees))
        {
            if (radiansSuffix)
            {
                longitudeDegrees *= 180d / Math.PI;
            }

            return true;
        }

        if (TryParsePrimeMeridianDmsToken(normalized, out longitudeDegrees))
        {
            return true;
        }

        longitudeDegrees = normalized.ToUpperInvariant() switch
        {
            "GREENWICH" => 0d,
            "LISBON" => -(9d + (7d / 60d) + (54.862d / 3600d)),
            "PARIS" => 2d + (20d / 60d) + (14.025d / 3600d),
            "BOGOTA" => -(74d + (4d / 60d) + (51.3d / 3600d)),
            "MADRID" => -(3d + (41d / 60d) + (16.58d / 3600d)),
            "ROME" => 12d + (27d / 60d) + (8.4d / 3600d),
            "BERN" => 7d + (26d / 60d) + (22.5d / 3600d),
            "JAKARTA" => 106d + (48d / 60d) + (27.79d / 3600d),
            "FERRO" => -(17d + (40d / 60d)),
            "BRUSSELS" => 4d + (22d / 60d) + (4.71d / 3600d),
            "STOCKHOLM" => 18d + (3d / 60d) + (29.8d / 3600d),
            "ATHENS" => 23d + (42d / 60d) + (58.815d / 3600d),
            "OSLO" => 10d + (43d / 60d) + (22.5d / 3600d),
            _ => double.NaN,
        };

        return !double.IsNaN(longitudeDegrees);
    }

    private static bool TryResolveProjPrimeMeridianLongitudeDegrees(string token, out double longitudeDegrees)
    {
        return TryResolvePrimeMeridianLongitudeDegrees(token, out longitudeDegrees);
    }

    private static bool TryParsePrimeMeridianDmsToken(string token, out double value)
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

#pragma warning disable CA1307, CA1865 // netstandard2.0 lacks char+StringComparison overload; char search is ordinal here.
        int dIndex = text.IndexOf('d');
        if (dIndex < 0)
        {
            dIndex = text.IndexOf('D');
        }

        int mIndex = text.IndexOf('\'');
        if (dIndex <= 0 || mIndex <= dIndex)
        {
            return false;
        }

        string degreesToken = text[..dIndex];
        string minutesToken = text.Substring(dIndex + 1, mIndex - dIndex - 1);
        if (!double.TryParse(degreesToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees)
            || !double.TryParse(minutesToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
        {
            return false;
        }

        double seconds = 0d;
        int secondsMarker = text.IndexOf('"');
#pragma warning restore CA1307, CA1865
        if (secondsMarker > mIndex + 1)
        {
            string secondsToken = text.Substring(mIndex + 1, secondsMarker - mIndex - 1);
            if (!double.TryParse(secondsToken, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
            {
                return false;
            }
        }

        value = sign * (degrees + (minutes / 60d) + (seconds / 3600d));
        return true;
    }

    private static bool TryResolveUnitScale(
        IDictionary<string, string> args,
        string inKey,
        string outKey,
        bool treatDegRadAsIdentity,
        out double scale)
    {
        scale = 1d;
        bool hasIn = args.TryGetValue(inKey, out string? inToken) && !string.IsNullOrWhiteSpace(inToken);
        bool hasOut = args.TryGetValue(outKey, out string? outToken) && !string.IsNullOrWhiteSpace(outToken);
        if (!hasIn && !hasOut)
        {
            return true;
        }

        if (hasIn != hasOut)
        {
            return false;
        }

        string inUnitToken = ArgumentGuard.ThrowIfNull(inToken, nameof(inToken));
        string outUnitToken = ArgumentGuard.ThrowIfNull(outToken, nameof(outToken));
        if (treatDegRadAsIdentity
            && ((inUnitToken.Equals("deg", StringComparison.OrdinalIgnoreCase) && outUnitToken.Equals("rad", StringComparison.OrdinalIgnoreCase))
                || (inUnitToken.Equals("rad", StringComparison.OrdinalIgnoreCase) && outUnitToken.Equals("deg", StringComparison.OrdinalIgnoreCase))))
        {
            scale = 1d;
            return true;
        }

        if (!TryResolveUnitFactor(inUnitToken, out double inFactor) || !TryResolveUnitFactor(outUnitToken, out double outFactor))
        {
            return false;
        }

        if (outFactor == 0d)
        {
            return false;
        }

        scale = inFactor / outFactor;
        return true;
    }

    private static bool TryResolveUnitFactor(string token, out double factor)
    {
        factor = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double numeric))
        {
            if (numeric <= 0d || double.IsInfinity(numeric) || double.IsNaN(numeric))
            {
                return false;
            }

            factor = numeric;
            return true;
        }

        if (token.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-3d;
            return true;
        }

        if (token.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-2d;
            return true;
        }

        if (token.Equals("dm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-1d;
            return true;
        }

        if (token.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1d;
            return true;
        }

        if (token.Equals("km", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e3d;
            return true;
        }

        if (token.Equals("ft", StringComparison.OrdinalIgnoreCase))
        {
            factor = 0.3048d;
            return true;
        }

        if (token.Equals("us-ft", StringComparison.OrdinalIgnoreCase))
        {
            factor = TransformationMath.MetresPerUsSurveyFoot;
            return true;
        }

        if (token.Equals("rad", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1d;
            return true;
        }

        if (token.Equals("deg", StringComparison.OrdinalIgnoreCase))
        {
            factor = Math.PI / 180d;
            return true;
        }

        if (token.Equals("grad", StringComparison.OrdinalIgnoreCase))
        {
            factor = Math.PI / 200d;
            return true;
        }

        return false;
    }
}
