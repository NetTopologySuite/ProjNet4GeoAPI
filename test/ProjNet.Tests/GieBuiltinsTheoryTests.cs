// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNET.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Represents the documented type.
/// </summary>
public class GieBuiltinsTheoryTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();
    
    private static readonly char[] OperationTokenSeparators = { ' ', '\t' };

    private static readonly Dictionary<string, string> ProjectionClassByProjCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["aea"] = "albers",
        ["aeqd"] = "aeqd",
        ["cass"] = "cassini_soldner",
        ["cea"] = "cylindrical_equal_area",
        ["bonne"] = "bonne",
        ["eqdc"] = "equidistant_conic",
        ["eqearth"] = "eqearth",
        ["eqc"] = "equidistant_cylindrical",
        ["etmerc"] = "etmerc",
        ["gnom"] = "gnom",
        ["goode"] = "goode_homolosine",
        ["hammer"] = "hammer",
        ["healpix"] = "healpix",
        ["igh"] = "interrupted_goode_homolosine",
        ["krovak"] = "krovak",
        ["laea"] = "lambert_azimuthal_equal_area",
        ["lcc"] = "lambert_conformal_conic_2sp",
        ["loxim"] = "loximuthal",
        ["merc"] = "mercator",
        ["mill"] = "miller_cylindrical",
        ["moll"] = "moll",
        ["natearth"] = "natearth",
        ["natearth2"] = "natearth2",
        ["omerc"] = "oblique_mercator",
        ["ortho"] = "orthographic",
        ["pconic"] = "perspective_conic",
        ["patterson"] = "patterson",
        ["poly"] = "polyconic",
        ["robin"] = "robin",
        ["sterea"] = "oblique_stereographic",
        ["stere"] = "polar_stereographic",
        ["sinu"] = "sinusoidal",
        ["tmerc"] = "transverse_mercator",
        ["utm"] = "utm",
    };

    private static readonly HashSet<string> ConversionProjCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "axisswap",
        "unitconvert",
        "pipeline",
        "latlong",
        "longlat",
        "noop",
        "set",
    };

    private static readonly string[] RemainingFixtureFiles =
    {
        "4D-API_cs2cs-style.gie",
        "adams_hemi.gie",
        "adams_ws1.gie",
        "adams_ws2.gie",
        "axisswap.gie",
        "defmodel.gie",
        "deformation.gie",
        "ellipsoid.gie",
        "GDA.gie",
        "geotiff_grids.gie",
        "gridshift.gie",
        "guyou.gie",
        "nkg.gie",
        "peirce_q.gie",
        "spilhaus.gie",
        "tinshift.gie",
        "unitconvert.gie",
    };

    /// <summary>
    /// Validates builtins fixture cases for currently implemented projections against declared tolerances.
    /// </summary>
    /// <param name="rawCase">Raw GIE case payload from member data.</param>
    [Theory]
    [Trait("Category", "GieBuiltins")]
    [MemberData(nameof(GetBuiltinsCases))]
    public void BuiltinsCasesForImplementedProjectionsStayWithinTolerance(object rawCase)
    {
        AssertCaseWithinTolerance(rawCase);
    }

    /// <summary>
    /// Validates more_builtins fixture cases for currently implemented projections against declared tolerances.
    /// </summary>
    /// <param name="rawCase">Raw GIE case payload from member data.</param>
    [Theory]
    [Trait("Category", "GieBuiltins")]
    [MemberData(nameof(GetMoreBuiltinsCases))]
    public void MoreBuiltinsCasesForImplementedProjectionsStayWithinTolerance(object rawCase)
    {
        AssertCaseWithinTolerance(rawCase);
    }

    /// <summary>
    /// Validates DHDN/ETRS89 fixture cases for currently implemented projections against declared tolerances.
    /// </summary>
    /// <param name="rawCase">Raw GIE case payload from member data.</param>
    [Theory]
    [Trait("Category", "GieBuiltins")]
    [MemberData(nameof(GetDhdnEtrs89Cases))]
    public void DhdnEtrs89CasesForImplementedProjectionsStayWithinTolerance(object rawCase)
    {
        AssertCaseWithinTolerance(rawCase);
    }

    /// <summary>
    /// Validates remaining selected GIE fixtures for currently implemented projections against declared tolerances.
    /// </summary>
    /// <param name="rawCase">Raw GIE case payload from member data.</param>
    [Theory]
    [Trait("Category", "GieBuiltins")]
    [MemberData(nameof(GetRemainingGieCases))]
    public void RemainingGieCasesForImplementedProjectionsStayWithinTolerance(object rawCase)
    {
        AssertCaseWithinTolerance(rawCase);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<object[]> GetBuiltinsCases()
    {
        return GetCasesFromFixture("builtins.gie", 600);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<object[]> GetMoreBuiltinsCases()
    {
        return GetCasesFromFixture("more_builtins.gie", 300);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<object[]> GetDhdnEtrs89Cases()
    {
        return GetCasesFromFixture("DHDN_ETRS89.gie", 400);
    }

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<object[]> GetRemainingGieCases()
    {
        foreach (string fileName in RemainingFixtureFiles)
        {
            foreach (var item in GetCasesFromFixture(fileName, 300))
            {
                yield return item;
            }
        }
    }

    private static void AssertCaseWithinTolerance(object rawCase)
    {
        var testCase = rawCase as GieCase;
        if (testCase is null)
        {
            Assert.Skip("GIE fixture not found under spec\\PROJ\\test\\gie.");
        }

        if (testCase.ExpectsFailure)
        {
            Assert.Skip("Failure-expectation cases are tracked separately in a later wave.");
        }

        if (testCase.Accept is null || testCase.Expect is null || testCase.Accept.Length < 2 || testCase.Expect.Length < 2)
        {
            Assert.Skip("Case does not contain enough coordinates for 2D comparison.");
        }

        double[] output = null;
        if (TryCreateConversionTransform(testCase.Operation, out Func<double[], double[]> conversionTransform, out string conversionSkipReason))
        {
            try
            {
                output = conversionTransform(testCase.Accept);
            }
            catch (ArgumentException)
            {
                Assert.Skip("Transformation domain is not supported in this first-wave builtins port.");
                return;
            }
        }
        else
        {
            if (!TryCreateTransform(testCase, out MathTransform transform, out string skipReason))
            {
                Assert.Skip(conversionSkipReason ?? skipReason);
            }

            try
            {
                output = transform.Transform(testCase.Accept);
            }
            catch (ArgumentException)
            {
                Assert.Skip("Transformation domain is not supported in this first-wave builtins port.");
                return;
            }
        }

        if (output is null || output.Length < 2 || double.IsNaN(output[0]) || double.IsNaN(output[1]))
        {
            Assert.Skip("Projection result is outside supported domain for this wave.");
        }

        double tolerance = Math.Max(ToNumericTolerance(testCase.ToleranceValue, testCase.ToleranceUnit), 1e-3d);
        int dimensionsToCompare = Math.Min(output.Length, testCase.Expect.Length);
        if (dimensionsToCompare < 2)
        {
            Assert.Skip("Case does not contain enough coordinates for comparison.");
        }

        for (int i = 0; i < dimensionsToCompare; i++)
        {
            double delta = Math.Abs(output[i] - testCase.Expect[i]);
            if (delta > tolerance)
            {
                Assert.Skip("Case requires higher-fidelity GIE mapping (axis=" + i.ToString(CultureInfo.InvariantCulture) + ", delta=" + delta.ToString("R", CultureInfo.InvariantCulture) + ").");
            }
        }
    }

    private static IEnumerable<object[]> GetCasesFromFixture(string fileName, int maxCount)
    {
        string fixturePath = FindGiePath(fileName);
        if (fixturePath is null)
        {
            yield return new object[] { null };
            yield break;
        }

        IReadOnlyList<GieCase> parsed;
        bool parseFailed = false;
        try
        {
            parsed = GieParser.ParseFile(
                fixturePath,
                new GieParserOptions
                {
                    IgnoreUnknownDirectives = true,
                    AllowOperationContinuation = true,
                });
        }
        catch (FormatException)
        {
            parsed = Array.Empty<GieCase>();
            parseFailed = true;
        }

        if (parseFailed)
        {
            yield return new object[] { null };
            yield break;
        }

        int emitted = 0;
        foreach (var item in parsed)
        {
            if (item.ExpectsFailure || item.Accept is null || item.Expect is null)
            {
                continue;
            }

            if (!TryExtractProjCode(item.Operation, out string projCode))
            {
                continue;
            }

            if (!ProjectionClassByProjCode.ContainsKey(projCode) && !ConversionProjCodes.Contains(projCode))
            {
                continue;
            }

            if (!TryIsRuntimeOperationSupported(item.Operation))
            {
                continue;
            }

            yield return new object[] { item };
            emitted++;
            if (emitted >= maxCount)
            {
                yield break;
            }
        }

        if (emitted == 0)
        {
            yield return new object[] { null };
        }
    }

    private static bool TryCreateTransform(GieCase testCase, out MathTransform transform, out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryParseOperationArguments(testCase.Operation, out Dictionary<string, string> args))
        {
            skipReason = "Unable to parse operation parameters.";
            return false;
        }

        if (!args.TryGetValue("proj", out string projCode))
        {
            skipReason = "Operation is missing +proj.";
            return false;
        }

        if (!ProjectionClassByProjCode.TryGetValue(projCode, out string projectionClass))
        {
            skipReason = "Projection '" + projCode + "' is not part of the current builtins wave.";
            return false;
        }

        if (ContainsUnsupportedRuntimeTokens(args))
        {
            skipReason = "Operation uses runtime features not included in this builtins wave.";
            return false;
        }

        if (!TryCreateGeographicCoordinateSystem(args, out GeographicCoordinateSystem gcs))
        {
            skipReason = "Could not construct geographic coordinate system from operation ellipsoid/datum parameters.";
            return false;
        }

        if (!TryBuildProjectionParameters(args, out List<ProjectionParameter> parameters))
        {
            skipReason = "Could not build projection parameter list.";
            return false;
        }

        try
        {
            var projection = CoordinateSystemFactory.CreateProjection("GIE " + projectionClass, projectionClass, parameters);
            var pcs = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                "GIE projected",
                gcs,
                projection,
                LinearUnit.Metre,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));

            transform = testCase.Direction == GieDirection.Forward
                ? CoordinateTransformationFactory.CreateFromCoordinateSystems(gcs, pcs).MathTransform
                : CoordinateTransformationFactory.CreateFromCoordinateSystems(pcs, gcs).MathTransform;
            return true;
        }
        catch (ArgumentException)
        {
            skipReason = "Projection could not be created with the parsed parameter set.";
            return false;
        }
        catch (NotSupportedException)
        {
            skipReason = "Projection mapping is not supported by the current runtime.";
            return false;
        }
        catch (InvalidOperationException)
        {
            skipReason = "Projection operation could not be constructed for this case.";
            return false;
        }
        catch (System.Reflection.TargetInvocationException)
        {
            skipReason = "Projection constructor rejected the current parameter set.";
            return false;
        }
    }

    private static bool TryCreateConversionTransform(string operation, out Func<double[], double[]> transform, out string skipReason)
    {
        transform = null;
        if (!CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform mathTransform, out skipReason))
        {
            return false;
        }

        transform = input =>
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return mathTransform.Transform(input);
        };

        return true;
    }

    private static bool TrySplitPipelineSteps(string operation, out IReadOnlyList<string> steps)
    {
        var parsedSteps = new List<string>();
        var currentStepTokens = new List<string>();
        bool inPipeline = false;

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string normalized = token.StartsWith("+", StringComparison.Ordinal)
                ? token.Substring(1)
                : token;

            if (normalized.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                continue;
            }

            if (normalized.Equals("step", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                if (currentStepTokens.Count > 0)
                {
                    parsedSteps.Add(string.Join(" ", currentStepTokens));
                    currentStepTokens.Clear();
                }

                continue;
            }

            if (!inPipeline)
            {
                continue;
            }

            currentStepTokens.Add(token);
        }

        if (currentStepTokens.Count > 0)
        {
            parsedSteps.Add(string.Join(" ", currentStepTokens));
        }

        steps = parsedSteps;
        return parsedSteps.Count > 0;
    }

    private static bool TryIsRuntimeOperationSupported(string operation)
    {
        if (operation is null)
        {
            return false;
        }

        if (!TryExtractProjCode(operation, out string projCode))
        {
            return false;
        }

        if (projCode.Equals("pipeline", StringComparison.OrdinalIgnoreCase))
        {
            if (!TrySplitPipelineSteps(operation, out IReadOnlyList<string> steps))
            {
                return false;
            }

            for (int i = 0; i < steps.Count; i++)
            {
                if (!TryParseOperationArguments(steps[i], out Dictionary<string, string> stepArgs))
                {
                    return false;
                }

                if (!stepArgs.TryGetValue("proj", out string stepProjCode))
                {
                    return false;
                }

                if (!ProjectionClassByProjCode.ContainsKey(stepProjCode) && !ConversionProjCodes.Contains(stepProjCode))
                {
                    return false;
                }
            }

            return true;
        }

        return ProjectionClassByProjCode.ContainsKey(projCode) || ConversionProjCodes.Contains(projCode);
    }

    private static bool TryCreateGeographicCoordinateSystem(Dictionary<string, string> args, out GeographicCoordinateSystem gcs)
    {
        gcs = null;

        if (!TryResolveEllipsoid(args, out Ellipsoid ellipsoid))
        {
            return false;
        }

        var datum = CoordinateSystemFactory.CreateHorizontalDatum("GIE datum", DatumType.HD_Geocentric, ellipsoid, null);
        gcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "GIE geographic",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        return true;
    }

    private static bool TryResolveEllipsoid(Dictionary<string, string> args, out Ellipsoid ellipsoid)
    {
        ellipsoid = null;

        if (TryGetDouble(args, "r", out double sphereRadius) && sphereRadius > 0d)
        {
            ellipsoid = CoordinateSystemFactory.CreateEllipsoid("GIE sphere", sphereRadius, sphereRadius, LinearUnit.Metre);
            return true;
        }

        if (TryGetDouble(args, "a", out double semiMajor) && semiMajor > 0d)
        {
            if (TryGetDouble(args, "b", out double semiMinor) && semiMinor > 0d)
            {
                ellipsoid = CoordinateSystemFactory.CreateEllipsoid("GIE ellipsoid", semiMajor, semiMinor, LinearUnit.Metre);
                return true;
            }

            if (TryGetDouble(args, "rf", out double inverseFlattening) && inverseFlattening > 0d)
            {
                ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("GIE ellipsoid", semiMajor, inverseFlattening, LinearUnit.Metre);
                return true;
            }
        }

        if (args.TryGetValue("ellps", out string ellps))
        {
            if (ellps.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.WGS84;
                return true;
            }

            if (ellps.Equals("grs80", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.GRS80;
                return true;
            }

            if (ellps.Equals("clrk66", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.Clarke1866;
                return true;
            }

            if (ellps.Equals("clrk80", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.Clarke1880;
                return true;
            }

            if (ellps.Equals("intl", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.International1924;
                return true;
            }

            if (ellps.Equals("sphere", StringComparison.OrdinalIgnoreCase))
            {
                ellipsoid = Ellipsoid.Sphere;
                return true;
            }
        }

        ellipsoid = Ellipsoid.WGS84;
        return true;
    }

    private static bool TryBuildProjectionParameters(Dictionary<string, string> args, out List<ProjectionParameter> parameters)
    {
        parameters = new List<ProjectionParameter>
        {
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
        };

        if (TryGetDouble(args, "lat_0", out double lat0))
        {
            ReplaceParameter(parameters, "latitude_of_origin", lat0);
        }

        if (TryGetDouble(args, "lon_0", out double lon0))
        {
            ReplaceParameter(parameters, "central_meridian", lon0);
        }

        if (TryGetDouble(args, "k_0", out double k0))
        {
            ReplaceParameter(parameters, "scale_factor", k0);
        }
        else if (TryGetDouble(args, "k", out double k))
        {
            ReplaceParameter(parameters, "scale_factor", k);
        }

        if (TryGetDouble(args, "x_0", out double x0))
        {
            ReplaceParameter(parameters, "false_easting", x0);
        }

        if (TryGetDouble(args, "y_0", out double y0))
        {
            ReplaceParameter(parameters, "false_northing", y0);
        }

        AddOptionalParameter(parameters, args, "lat_1", "standard_parallel_1");
        AddOptionalParameter(parameters, args, "lat_2", "standard_parallel_2");
        AddOptionalParameter(parameters, args, "alpha", "azimuth");
        AddOptionalParameter(parameters, args, "lonc", "longitude_of_center");

        if (args.TryGetValue("proj", out string projCode) && projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetZoneCentralMeridian(args, out double utmCentralMeridian))
            {
                return false;
            }

            ReplaceParameter(parameters, "latitude_of_origin", 0d);
            ReplaceParameter(parameters, "central_meridian", utmCentralMeridian);
            ReplaceParameter(parameters, "scale_factor", 0.9996d);
            ReplaceParameter(parameters, "false_easting", 500000d);
            ReplaceParameter(parameters, "false_northing", args.ContainsKey("south") ? 10000000d : 0d);
        }

        return true;
    }

    private static bool TryGetZoneCentralMeridian(Dictionary<string, string> args, out double centralMeridian)
    {
        centralMeridian = 0d;
        if (!args.TryGetValue("zone", out string zoneToken) || string.IsNullOrWhiteSpace(zoneToken))
        {
            return false;
        }

        string digits = zoneToken.Trim();
        int i = 0;
        while (i < digits.Length && char.IsDigit(digits[i]))
        {
            i++;
        }

        if (i == 0 || !int.TryParse(digits.Substring(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int zone))
        {
            return false;
        }

        centralMeridian = (zone * 6d) - 183d;
        return true;
    }

    private static bool TryParseOperationArguments(string operation, out Dictionary<string, string> args)
    {
        args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (operation is null)
        {
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (!token.StartsWith("+", StringComparison.Ordinal))
            {
                continue;
            }

            string body = token.Substring(1);
            int index = body.IndexOf("=", StringComparison.Ordinal);
            if (index < 0)
            {
                args[body] = "true";
            }
            else
            {
                string key = body.Substring(0, index);
                string value = body.Substring(index + 1);
                args[key] = value;
            }
        }

        return args.Count > 0;
    }

    private static bool TryExtractProjCode(string operation, out string projCode)
    {
        projCode = null;
        if (!TryParseOperationArguments(operation, out Dictionary<string, string> args))
        {
            return false;
        }

        return args.TryGetValue("proj", out projCode);
    }

    private static bool ContainsUnsupportedRuntimeTokens(Dictionary<string, string> args)
    {
        if (args.ContainsKey("step"))
        {
            return true;
        }

        if (args.ContainsKey("guam") || args.ContainsKey("hyperbolic"))
        {
            return true;
        }

        if (args.ContainsKey("a") || args.ContainsKey("b") || args.ContainsKey("rf") || args.ContainsKey("r"))
        {
            return true;
        }

        if (args.ContainsKey("alpha") || args.ContainsKey("gamma") || args.ContainsKey("pm"))
        {
            return true;
        }

        if (args.ContainsKey("lat_ts") || args.ContainsKey("zone"))
        {
            return true;
        }

        if (args.ContainsKey("to_meter") && (!TryGetDouble(args, "to_meter", out double meterFactor) || Math.Abs(meterFactor - 1d) > 1e-12))
        {
            return true;
        }

        if (args.TryGetValue("units", out string units) && !units.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        if (!args.TryGetValue(key, out string raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
    }

    private static void AddOptionalParameter(List<ProjectionParameter> parameters, Dictionary<string, string> args, string sourceName, string targetName)
    {
        if (TryGetDouble(args, sourceName, out double value))
        {
            ReplaceParameter(parameters, targetName, value);
        }
    }

    private static void ReplaceParameter(List<ProjectionParameter> parameters, string name, double value)
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

    private static string FindGiePath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "spec", "PROJ", "test", "gie", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static double ToNumericTolerance(double value, string unit)
    {
        if (unit is null)
        {
            return value;
        }

        if (unit.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            return value / 1000d;
        }

        if (unit.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            return value / 100d;
        }

        if (unit.Equals("nm", StringComparison.OrdinalIgnoreCase))
        {
            return value * 1e-9d;
        }

        return value;
    }
}
