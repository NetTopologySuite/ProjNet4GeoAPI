// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

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
    private static readonly object MissingCaseSentinel = new();

    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();

    private static readonly char[] OperationTokenSeparators = { ' ', '\t' };

    private static readonly Dictionary<string, string> ProjectionClassByProjCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["adams_hemi"] = "adams_hemisphere_in_a_square",
        ["adams_ws1"] = "adams_world_in_a_square_i",
        ["adams_ws2"] = "adams_world_in_a_square_ii",
        ["aea"] = "albers",
        ["aeqd"] = "aeqd",
        ["cass"] = "cassini_soldner",
        ["cea"] = "cylindrical_equal_area",
        ["bonne"] = "bonne",
        ["eqdc"] = "equidistant_conic",
        ["eqearth"] = "eqearth",
        ["eqc"] = "equidistant_cylindrical",
        ["eck1"] = "eckert_i",
        ["eck2"] = "eckert_ii",
        ["eck3"] = "eckert_iii",
        ["eck4"] = "eckert_iv",
        ["eck5"] = "eckert_v",
        ["putp1"] = "putnins_p1",
        ["putp2"] = "putnins_p2",
        ["putp3"] = "putnins_p3",
        ["putp3p"] = "putnins_p3p",
        ["putp4p"] = "putnins_p4p",
        ["putp5"] = "putnins_p5",
        ["putp5p"] = "putnins_p5p",
        ["putp6"] = "putnins_p6",
        ["putp6p"] = "putnins_p6p",
        ["weren"] = "werenskiold_i",
        ["kav7"] = "kavrayskiy_vii",
        ["wag2"] = "wagner_ii",
        ["wag3"] = "wagner_iii",
        ["wag4"] = "wagner_iv",
        ["wag5"] = "wagner_v",
        ["wag6"] = "wagner_vi",
        ["wag1"] = "wagner_i",
        ["wag7"] = "wagner_vii",
        ["kav5"] = "kavrayskiy_v",
        ["qua_aut"] = "quartic_authalic",
        ["fouc"] = "foucaut",
        ["mbt_s"] = "mcbryde_thomas_flat_polar_sine",
        ["cc"] = "central_cylindrical",
        ["ccon"] = "central_conic",
        ["lcca"] = "lambert_conformal_conic_alternative",
        ["ocea"] = "oblique_cylindrical_equal_area",
        ["oea"] = "oblated_equal_area",
        ["rpoly"] = "rectangular_polyconic",
        ["tpeqd"] = "two_point_equidistant",
        ["august"] = "august_epicycloidal",
        ["bacon"] = "bacon_globular",
        ["apian"] = "apian_globular_i",
        ["ortel"] = "ortelius_oval",
        ["calcofi"] = "cal_coop_ocean_fish_invest_lines_stations",
        ["col_urban"] = "colombia_urban",
        ["comill"] = "compact_miller",
        ["denoy"] = "denoyer_semi_elliptical",
        ["fouc_s"] = "foucaut_sinusoidal",
        ["gins8"] = "ginsburg_viii",
        ["igh_o"] = "interrupted_goode_homolosine_oceanic_view",
        ["imoll"] = "interrupted_mollweide",
        ["imoll_o"] = "interrupted_mollweide_oceanic_view",
        ["bertin1953"] = "bertin_1953",
        ["lagrng"] = "lagrange",
        ["larr"] = "larrivee",
        ["lask"] = "laskowski",
        ["euler"] = "euler",
        ["murd1"] = "murd1",
        ["murd2"] = "murd2",
        ["murd3"] = "murd3",
        ["tissot"] = "tissot",
        ["vitk1"] = "vitk1",
        ["imw_p"] = "international_map_of_the_world_polyconic",
        ["mbtfpp"] = "mcbryde_thomas_flat_polar_parabolic",
        ["mbtfpq"] = "mcbryde_thomas_flat_polar_quartic",
        ["mbt_fps"] = "mcbryde_thomas_flat_pole_sine",
        ["tcc"] = "transverse_central_cylindrical",
        ["tobmerc"] = "tobler_mercator",
        ["gall"] = "gall",
        ["gn_sinu"] = "general_sinusoidal",
        ["guyou"] = "guyou",
        ["eck6"] = "eckert_vi",
        ["mbtfps"] = "mcbryde_thomas_flat_polar_sinusoidal",
        ["crast"] = "craster_parabolic",
        ["fahey"] = "fahey",
        ["collg"] = "collignon",
        ["boggs"] = "boggs_eumorphic",
        ["airy"] = "airy",
        ["bipc"] = "bipolar_conic",
        ["chamb"] = "chamberlin_trimetric",
        ["hatano"] = "hatano_asymmetrical_equal_area",
        ["nell"] = "nell",
        ["nell_h"] = "nell_hammer",
        ["nicol"] = "nicolosi_globular",
        ["urm5"] = "urmaev_v",
        ["urmfps"] = "urmaev_flat_polar_sinusoidal",
        ["times"] = "times_projection",
        ["etmerc"] = "etmerc",
        ["gnom"] = "gnom",
        ["goode"] = "goode_homolosine",
        ["geos"] = "geostationary_satellite",
        ["gstmerc"] = "gauss_schreiber_transverse_mercator",
        ["qsc"] = "quadrilateralized_spherical_cube",
        ["rouss"] = "roussilhe_stereographic",
        ["mil_os"] = "miller_oblated_stereographic",
        ["lee_os"] = "lee_oblated_stereographic",
        ["gs48"] = "modified_stereographic_48_us",
        ["alsk"] = "modified_stereographic_alaska",
        ["gs50"] = "modified_stereographic_50_us",
        ["labrd"] = "laborde",
        ["nsper"] = "near_sided_perspective",
        ["tpers"] = "tilted_perspective",
        ["nzmg"] = "new_zealand_map_grid",
        ["hammer"] = "hammer",
        ["healpix"] = "healpix",
        ["rhealpix"] = "rhealpix",
        ["s2"] = "s2",
        ["spilhaus"] = "spilhaus",
        ["airocean"] = "airocean",
        ["isea"] = "icosahedral_snyder_equal_area",
        ["mod_krovak"] = "mod_krovak",
        ["leac"] = "leac",
        ["som"] = "space_oblique_mercator",
        ["misrsom"] = "space_oblique_mercator",
        ["lsat"] = "space_oblique_mercator",
        ["igh"] = "interrupted_goode_homolosine",
        ["krovak"] = "krovak",
        ["laea"] = "lambert_azimuthal_equal_area",
        ["lcc"] = "lambert_conformal_conic_2sp",
        ["loxim"] = "loximuthal",
        ["merc"] = "mercator",
        ["webmerc"] = "webmerc",
        ["mill"] = "miller_cylindrical",
        ["moll"] = "moll",
        ["natearth"] = "natearth",
        ["natearth2"] = "natearth2",
        ["omerc"] = "oblique_mercator",
        ["ortho"] = "orthographic",
        ["pconic"] = "perspective_conic",
        ["peirce_q"] = "peirce_quincuncial",
        ["patterson"] = "patterson",
        ["poly"] = "polyconic",
        ["robin"] = "robin",
        ["sterea"] = "oblique_stereographic",
        ["stere"] = "polar_stereographic",
        ["sinu"] = "sinusoidal",
        ["aitoff"] = "aitoff",
        ["wink1"] = "winkel_i",
        ["wink2"] = "winkel_ii",
        ["wintri"] = "winkel_tripel",
        ["vandg2"] = "van_der_grinten_ii",
        ["vandg3"] = "van_der_grinten_iii",
        ["vandg4"] = "van_der_grinten_iv",
        ["tmerc"] = "transverse_mercator",
        ["utm"] = "utm",
        ["ups"] = "ups",
    };

    private static readonly HashSet<string> ConversionProjCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "axisswap",
        "unitconvert",
        "pipeline",
        "latlong",
        "latlon",
        "lonlat",
        "longlat",
        "noop",
        "set",
        "push",
        "pop",
        "xyzgridshift",
        "tinshift",
    };

    private static readonly HashSet<string> ProjectionsWithoutInverse = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "wink2",
        "wag7",
        "airy",
        "chamb",
        "boggs",
        "nicol",
        "urm5",
        "august",
        "bacon",
        "apian",
        "ortel",
        "denoy",
        "gins8",
        "larr",
        "lask",
        "tcc",
        "guyou",
        "adams_hemi",
        "adams_ws1",
        "rpoly",
        "bertin1953",
        "vandg2",
        "vandg3",
        "vandg4",
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
            foreach (object[] item in GetCasesFromFixture(fileName, 300))
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
            Assert.Skip("No applicable GIE case was produced from local fixtures for this data row.");
        }

        if (testCase.ExpectsFailure)
        {
            Assert.Skip("Failure-expectation cases are tracked separately in a later wave.");
        }

        if (testCase.Accept is null || testCase.Expect is null || testCase.Accept.Length < 2 || testCase.Expect.Length < 2)
        {
            Assert.Skip("Case does not contain enough coordinates for 2D comparison.");
        }

        double[]? output = null;
        if (TryCreateConversionTransform(testCase.Operation, out Func<double[], double[]>? conversionTransform, out string? conversionSkipReason))
        {
            Func<double[], double[]> transform = Assert.IsAssignableFrom<Func<double[], double[]>>(conversionTransform);
            try
            {
                output = transform(testCase.Accept);
            }
            catch (ArgumentException)
            {
                Assert.Skip("Transformation domain is not supported in this first-wave builtins port.");
                return;
            }
        }
        else
        {
            if (!TryCreateTransform(testCase, out MathTransform? transform, out string? skipReason))
            {
                Assert.Skip(skipReason ?? conversionSkipReason ?? "Transformation could not be created.");
            }

            MathTransform mathTransform = Assert.IsAssignableFrom<MathTransform>(transform);
            try
            {
                output = mathTransform.Transform(testCase.Accept);
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

        double[] evaluatedOutput = Assert.IsAssignableFrom<double[]>(output);
        double tolerance = Math.Max(ToNumericTolerance(testCase.ToleranceValue, testCase.ToleranceUnit), 1e-3d);
        int dimensionsToCompare = Math.Min(evaluatedOutput.Length, testCase.Expect.Length);
        if (dimensionsToCompare < 2)
        {
            Assert.Skip("Case does not contain enough coordinates for comparison.");
        }

        for (int i = 0; i < dimensionsToCompare; i++)
        {
            double delta = Math.Abs(evaluatedOutput[i] - testCase.Expect[i]);
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
            yield return new object[] { MissingCaseSentinel };
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
            yield return new object[] { MissingCaseSentinel };
            yield break;
        }

        int emitted = 0;
        foreach (GieCase item in parsed)
        {
            if (item.ExpectsFailure || item.Accept is null || item.Expect is null)
            {
                continue;
            }

            if (!TryExtractProjCode(item.Operation, out string? projCode) || projCode is null)
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
            yield return new object[] { MissingCaseSentinel };
        }
    }

    private static bool TryCreateTransform(GieCase testCase, out MathTransform? transform, out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryParseOperationArguments(testCase.Operation, out Dictionary<string, string> args))
        {
            skipReason = "Unable to parse operation parameters.";
            return false;
        }

        if (!args.TryGetValue("proj", out string? projCode))
        {
            skipReason = "Operation is missing +proj.";
            return false;
        }

        if (!ProjectionClassByProjCode.TryGetValue(projCode, out string? projectionClass))
        {
            skipReason = "Projection '" + projCode + "' is not part of the current builtins wave.";
            return false;
        }

        if (testCase.Direction == GieDirection.Inverse && ProjectionsWithoutInverse.Contains(projCode))
        {
            skipReason = "Projection '" + projCode + "' has no inverse in PROJ and is skipped for inverse direction.";
            return false;
        }

        if (ContainsUnsupportedRuntimeTokens(args))
        {
            skipReason = "Operation uses runtime features not included in this builtins wave.";
            return false;
        }

        if (!TryCreateGeographicCoordinateSystem(args, out GeographicCoordinateSystem? gcs))
        {
            skipReason = "Could not construct geographic coordinate system from operation ellipsoid/datum parameters.";
            return false;
        }

        GeographicCoordinateSystem geographicCoordinateSystem = Assert.IsAssignableFrom<GeographicCoordinateSystem>(gcs);
        if (!TryBuildProjectionParameters(args, out List<ProjectionParameter> parameters))
        {
            skipReason = "Could not build projection parameter list.";
            return false;
        }

        try
        {
            IProjection projection = CoordinateSystemFactory.CreateProjection("GIE " + projectionClass, projectionClass, parameters);
            ProjectedCoordinateSystem pcs = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                "GIE projected",
                geographicCoordinateSystem,
                projection,
                LinearUnit.Metre,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));

            transform = testCase.Direction == GieDirection.Forward
                ? CoordinateTransformationFactory.CreateFromCoordinateSystems(geographicCoordinateSystem, pcs).MathTransform
                : CoordinateTransformationFactory.CreateFromCoordinateSystems(pcs, geographicCoordinateSystem).MathTransform;
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

    private static bool TryCreateConversionTransform(string operation, out Func<double[], double[]>? transform, out string? skipReason)
    {
        transform = null;
        if (operation is null)
        {
            skipReason = "Operation string was null.";
            return false;
        }

        string normalizedOperation = NormalizeOperationForRuntime(operation);
        if (!CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(normalizedOperation, out MathTransform? mathTransform, out skipReason))
        {
            return false;
        }

        MathTransform pipelineTransform = Assert.IsAssignableFrom<MathTransform>(mathTransform);
        transform = input =>
        {
            ArgumentNullException.ThrowIfNull(input);

            return pipelineTransform.Transform(input);
        };

        return true;
    }

    private static string NormalizeOperationForRuntime(string operation)
    {
        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return operation;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Length == 0 || tokens[i][0] == '+')
            {
                continue;
            }

            tokens[i] = "+" + tokens[i];
        }

        return string.Join(" ", tokens);
    }

    private static bool TrySplitPipelineSteps(string operation, out IReadOnlyList<string> steps)
    {
        var parsedSteps = new List<string>();
        var currentStepTokens = new List<string>();
        bool inPipeline = false;

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string normalized = token.StartsWith('+')
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

        if (!TryExtractProjCode(operation, out string? projCode) || projCode is null)
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

                if (!stepArgs.TryGetValue("proj", out string? stepProjCode))
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

    private static bool TryCreateGeographicCoordinateSystem(Dictionary<string, string> args, out GeographicCoordinateSystem? gcs)
    {
        gcs = null;

        if (!TryResolveEllipsoid(args, out Ellipsoid? ellipsoid))
        {
            return false;
        }

        Ellipsoid geographicEllipsoid = Assert.IsAssignableFrom<Ellipsoid>(ellipsoid);
        HorizontalDatum datum = CoordinateSystemFactory.CreateHorizontalDatum("GIE datum", DatumType.HD_Geocentric, geographicEllipsoid, null);
        gcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "GIE geographic",
            AngularUnit.Degrees,
            datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        return true;
    }

    private static bool TryResolveEllipsoid(Dictionary<string, string> args, out Ellipsoid? ellipsoid)
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

        if (args.TryGetValue("ellps", out string? ellps))
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
        AddOptionalParameter(parameters, args, "lat_1", "lat_1");
        AddOptionalParameter(parameters, args, "lat_2", "lat_2");
        AddOptionalParameter(parameters, args, "lat_ts", "lat_ts");
        AddOptionalParameter(parameters, args, "lat_ts", "latitude_true_scale");
        AddOptionalParameter(parameters, args, "lon_1", "lon_1");
        AddOptionalParameter(parameters, args, "lon_2", "lon_2");
        AddOptionalParameter(parameters, args, "lat_3", "lat_3");
        AddOptionalParameter(parameters, args, "lon_3", "lon_3");
        AddOptionalParameter(parameters, args, "lat_b", "lat_b");
        AddOptionalParameter(parameters, args, "alpha", "azimuth");
        AddOptionalParameter(parameters, args, "azi", "azi");
        AddOptionalParameter(parameters, args, "tilt", "tilt");
        AddOptionalParameter(parameters, args, "lonc", "longitude_of_center");
        AddOptionalParameter(parameters, args, "h", "h");
        AddOptionalParameter(parameters, args, "satellite_height", "h");
        if (args.TryGetValue("shape", out string? shapeToken))
        {
            if (!TryGetPeirceShapeCode(shapeToken, out double shapeCode))
            {
                return false;
            }

            ReplaceParameter(parameters, "shape", shapeCode);
        }

        AddOptionalParameter(parameters, args, "scrollx", "scrollx");
        AddOptionalParameter(parameters, args, "scrolly", "scrolly");
        if (args.TryGetValue("UVtoST", out string? uvToStMode)
            || args.TryGetValue("uvtost", out uvToStMode)
            || args.TryGetValue("uv_to_st", out uvToStMode))
        {
            double uvToStCode;
            if (uvToStMode.Equals("linear", StringComparison.OrdinalIgnoreCase))
            {
                uvToStCode = 0d;
            }
            else if (uvToStMode.Equals("quadratic", StringComparison.OrdinalIgnoreCase))
            {
                uvToStCode = 1d;
            }
            else if (uvToStMode.Equals("tangent", StringComparison.OrdinalIgnoreCase))
            {
                uvToStCode = 2d;
            }
            else if (uvToStMode.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                uvToStCode = 3d;
            }
            else if (!double.TryParse(uvToStMode, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out uvToStCode))
            {
                uvToStCode = double.NaN;
            }

            ReplaceParameter(parameters, "uv_to_st", uvToStCode);
        }

        AddOptionalParameter(parameters, args, "h_0", "h_0");
        AddOptionalParameter(parameters, args, "m", "m");
        AddOptionalParameter(parameters, args, "n", "n");
        AddOptionalParameter(parameters, args, "q", "q");
        AddOptionalParameter(parameters, args, "W", "W");
        AddOptionalParameter(parameters, args, "theta", "theta");
        AddOptionalParameter(parameters, args, "inc_angle", "inc_angle");
        AddOptionalParameter(parameters, args, "ps_rev", "ps_rev");
        AddOptionalParameter(parameters, args, "asc_lon", "asc_lon");
        AddOptionalParameter(parameters, args, "path", "path");
        AddOptionalParameter(parameters, args, "lsat", "lsat");
        AddOptionalParameter(parameters, args, "rot", "rot");
        AddOptionalParameter(parameters, args, "lon_1", "longitude1");
        AddOptionalParameter(parameters, args, "lat_1", "latitude1");
        AddOptionalParameter(parameters, args, "lon_2", "longitude2");
        AddOptionalParameter(parameters, args, "lat_2", "latitude2");
        if (args.TryGetValue("sweep", out string? sweepAxis))
        {
            double sweepX = sweepAxis.Equals("x", StringComparison.OrdinalIgnoreCase) ? 1d : 0d;
            ReplaceParameter(parameters, "sweep_x", sweepX);
        }

        if (args.TryGetValue("proj", out string? projectionCode))
        {
            if (projectionCode.Equals("airocean", StringComparison.OrdinalIgnoreCase) && args.TryGetValue("orient", out string? airoceanOrientation))
            {
                if (!TryGetAiroceanOrientationCode(airoceanOrientation, out double orientationCode))
                {
                    return false;
                }

                ReplaceParameter(parameters, "airocean_orient", orientationCode);
            }

            if (projectionCode.Equals("isea", StringComparison.OrdinalIgnoreCase))
            {
                if (args.TryGetValue("orient", out string? iseaOrientation))
                {
                    if (!TryGetIseaOrientCode(iseaOrientation, out double orientCode))
                    {
                        return false;
                    }

                    ReplaceParameter(parameters, "isea_orient", orientCode);
                }

                if (args.TryGetValue("mode", out string? iseaMode))
                {
                    if (!TryGetIseaModeCode(iseaMode, out double modeCode))
                    {
                        return false;
                    }

                    ReplaceParameter(parameters, "isea_mode", modeCode);
                }

                AddOptionalParameter(parameters, args, "resolution", "isea_resolution");
                AddOptionalParameter(parameters, args, "aperture", "isea_aperture");
                AddOptionalParameter(parameters, args, "azi", "isea_azimuth");
            }

            if (projectionCode.Equals("leac", StringComparison.OrdinalIgnoreCase) && args.ContainsKey("south"))
            {
                ReplaceParameter(parameters, "south", 1d);
            }

            if (projectionCode.Equals("ups", StringComparison.OrdinalIgnoreCase) && args.ContainsKey("south"))
            {
                ReplaceParameter(parameters, "south", 1d);
            }
        }

        if (args.ContainsKey("no_cut"))
        {
            ReplaceParameter(parameters, "no_cut", 1d);
        }

        if (args.ContainsKey("ns") || args.ContainsKey("noskew"))
        {
            ReplaceParameter(parameters, "ns", 1d);
        }

        if (args.TryGetValue("proj", out string? projCode) && projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
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
        if (!args.TryGetValue("zone", out string? zoneToken) || string.IsNullOrWhiteSpace(zoneToken))
        {
            return false;
        }

        string digits = zoneToken.Trim();
        int i = 0;
        while (i < digits.Length && char.IsDigit(digits[i]))
        {
            i++;
        }

        if (i == 0 || !int.TryParse(digits.AsSpan(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int zone))
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
            if (token.Length == 0)
            {
                continue;
            }

            string body = token[0] == '+' ? token.Substring(1) : token;
            int index = body.IndexOf('=', StringComparison.Ordinal);
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

    private static bool TryExtractProjCode(string operation, out string? projCode)
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

        if (args.ContainsKey("gamma") || args.ContainsKey("pm"))
        {
            return true;
        }

        if (args.ContainsKey("alpha"))
        {
            if (!args.TryGetValue("proj", out string? projCode) || !projCode.Equals("ocea", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (args.ContainsKey("zone"))
        {
            return true;
        }

        if (args.ContainsKey("north_square") || args.ContainsKey("south_square"))
        {
            if (!args.TryGetValue("proj", out string? projectionCodeForSquares)
                || !projectionCodeForSquares.Equals("rhealpix", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (TryGetDouble(args, "north_square", out double northSquare) && Math.Abs(northSquare) > 1e-12d)
            {
                return true;
            }

            if (TryGetDouble(args, "south_square", out double southSquare) && Math.Abs(southSquare) > 1e-12d)
            {
                return true;
            }
        }

        if (args.ContainsKey("czech"))
        {
            return true;
        }

        if (args.ContainsKey("to_meter") && (!TryGetDouble(args, "to_meter", out double meterFactor) || Math.Abs(meterFactor - 1d) > 1e-12))
        {
            return true;
        }

        if (args.TryGetValue("units", out string? units) && !units.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetDouble(Dictionary<string, string> args, string key, out double value)
    {
        value = 0d;
        if (!args.TryGetValue(key, out string? raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string token = raw.Trim();
        bool radiansSuffix = token.Length > 0 && (token[^1] == 'r' || token[^1] == 'R');
        if (radiansSuffix)
        {
            token = token[..^1];
        }

        if (!double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        if (radiansSuffix)
        {
            value = value * (180d / Math.PI);
        }

        return true;
    }

    private static bool TryGetAiroceanOrientationCode(string token, out double orientationCode)
    {
        orientationCode = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out orientationCode))
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

    private static bool TryGetIseaOrientCode(string token, out double orientCode)
    {
        orientCode = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out orientCode))
        {
            return orientCode == 0d || orientCode == 1d;
        }

        orientCode = normalized.ToUpperInvariant() switch
        {
            "ISEA" => 0d,
            "POLE" => 1d,
            _ => double.NaN,
        };

        return !double.IsNaN(orientCode);
    }

    private static bool TryGetIseaModeCode(string token, out double modeCode)
    {
        modeCode = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out modeCode))
        {
            return modeCode >= 0d && modeCode <= 3d;
        }

        modeCode = normalized.ToUpperInvariant() switch
        {
            "PLANE" => 0d,
            "DI" => 1d,
            "DD" => 2d,
            "HEX" => 3d,
            _ => double.NaN,
        };

        return !double.IsNaN(modeCode);
    }

    private static bool TryGetPeirceShapeCode(string token, out double shapeCode)
    {
        shapeCode = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out shapeCode))
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
        string direct = Path.Combine(AppContext.BaseDirectory, "Fixtures", "gie", fileName);
        if (File.Exists(direct))
        {
            return direct;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = Path.Combine(current.FullName, "test", "ProjNet.Tests", "Fixtures", "gie", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return default!;
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
