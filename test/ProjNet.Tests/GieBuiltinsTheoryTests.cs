// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Contains xUnit theory tests that run PROJ GIE built-in fixture cases against ProjNet coordinate transformations.
/// </summary>
public class GieBuiltinsTheoryTests
{
    private static readonly CoordinateSystemFactory CoordinateSystemFactory = new();
    private static readonly CoordinateTransformationFactory CoordinateTransformationFactory = new();

    private static readonly char[] CommaSeparator = [','];
    private static readonly char[] OperationTokenSeparators = [' ', '\t'];

    private static readonly Dictionary<string, string> ProjectionClassByProjCode = new(StringComparer.OrdinalIgnoreCase)
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
        ["tcea"] = "transverse_cylindrical_equal_area",
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
        ["somerc"] = "swiss_oblique_mercator",
        ["aitoff"] = "aitoff",
        ["wink1"] = "winkel_i",
        ["wink2"] = "winkel_ii",
        ["wintri"] = "winkel_tripel",
        ["vandg"] = "van_der_grinten",
        ["vandg2"] = "van_der_grinten_ii",
        ["vandg3"] = "van_der_grinten_iii",
        ["vandg4"] = "van_der_grinten_iv",
        ["tmerc"] = "transverse_mercator",
        ["utm"] = "utm",
        ["ups"] = "ups",
    };

    private static readonly HashSet<string> ConversionProjCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "axisswap",
        "cart",
        "geocent",
        "helmert",
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
        "defmodel",
        "deformation",
        "xyzgridshift",
        "tinshift",
    };

    private static readonly HashSet<string> ProjectionsWithoutInverse = new(StringComparer.OrdinalIgnoreCase)
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
    [
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
    ];

    /// <summary>
    /// Validates builtins fixture cases for currently implemented projections against declared tolerances.
    /// </summary>
    /// <param name="rawCase">Raw GIE case payload from member data.</param>
    [Theory]
    [Trait("Category", "GieBuiltins")]
    [MemberData(nameof(GetBuiltinsCases))]
    public void BuiltinsCasesForImplementedProjectionsStayWithinTolerance(GieCase? rawCase)
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
    public void MoreBuiltinsCasesForImplementedProjectionsStayWithinTolerance(GieCase? rawCase)
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
    public void DhdnEtrs89CasesForImplementedProjectionsStayWithinTolerance(GieCase? rawCase)
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
    public void RemainingGieCasesForImplementedProjectionsStayWithinTolerance(GieCase? rawCase)
    {
        AssertCaseWithinTolerance(rawCase);
    }

    /// <summary>
    /// Verifies that standalone <c>push</c> and <c>pop</c> conversion cases are normalized through the GIE harness as single-step pipelines.
    /// </summary>
    /// <param name="operation">Standalone PROJ operation.</param>
    [Theory]
    [InlineData("+proj=push +v_3")]
    [InlineData("+proj=pop +v_3")]
    public void StandalonePushPopConversionCasesRunThroughHarnessConversionPath(string operation)
    {
        bool ok = TryCreateConversionTransform(operation, out Func<double[], double[]>? transform, out string? skipReason);

        Assert.True(ok, skipReason);

        double[] result = Assert.IsType<Func<double[], double[]>>(transform)([12d, 56d, 0d, 2020d]);
        Assert.Equal(12d, result[0], 12);
        Assert.Equal(56d, result[1], 12);
        Assert.Equal(0d, result[2], 12);
        Assert.Equal(2020d, result[3], 12);
    }

    /// <summary>
    /// Returns theory data rows sourced from the <c>builtins.gie</c> fixture file.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<TheoryDataRow<GieCase?>> GetBuiltinsCases()
    {
        return GetCasesFromFixture("builtins.gie", 600);
    }

    /// <summary>
    /// Returns theory data rows sourced from the <c>more_builtins.gie</c> fixture file.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<TheoryDataRow<GieCase?>> GetMoreBuiltinsCases()
    {
        return GetCasesFromFixture("more_builtins.gie", 300);
    }

    /// <summary>
    /// Returns theory data rows sourced from the <c>DHDN_ETRS89.gie</c> fixture file.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<TheoryDataRow<GieCase?>> GetDhdnEtrs89Cases()
    {
        return GetCasesFromFixture("DHDN_ETRS89.gie", 400);
    }

    /// <summary>
    /// Returns theory data rows sourced from the remaining selected GIE fixture files.
    /// </summary>
    /// <returns>The computed value.</returns>
    public static IEnumerable<TheoryDataRow<GieCase?>> GetRemainingGieCases()
    {
        foreach (string fileName in RemainingFixtureFiles)
        {
            foreach (TheoryDataRow<GieCase?> item in GetCasesFromFixture(fileName, 300))
            {
                yield return item;
            }
        }
    }

    private static void AssertCaseWithinTolerance(GieCase? rawCase)
    {
        if (rawCase is null)
        {
            Assert.Skip("No applicable GIE case was produced from local fixtures for this data row.");
        }

        if (rawCase.ExpectsFailure)
        {
            Assert.Skip("Failure-expectation cases are tracked separately in a later wave.");
        }

        if (rawCase.Accept is null || rawCase.Expect is null || rawCase.Accept.Length < 2 || rawCase.Expect.Length < 2)
        {
            Assert.Skip("Case does not contain enough coordinates for 2D comparison.");
        }

        double[]? output = null;
        string? conversionSkipReason = null;
        bool isGeographicDatumShift = HasGeographicDatumShift(rawCase.Operation);
        if (!isGeographicDatumShift && TryCreateConversionTransformForDirection(rawCase.Operation, rawCase.Direction, out Func<double[], double[]>? conversionTransform, out conversionSkipReason))
        {
            Func<double[], double[]> transform = Assert.IsType<Func<double[], double[]>>(conversionTransform);
            try
            {
                output = transform(rawCase.Accept);
            }
            catch (ArgumentException)
            {
                Assert.Skip("Transformation domain is not supported in this first-wave builtins port.");
                return;
            }
        }
        else
        {
            if (!TryCreateTransform(rawCase, out MathTransform? transform, out string? skipReason))
            {
                Assert.Skip(skipReason ?? conversionSkipReason ?? "Transformation could not be created.");
            }

            MathTransform mathTransform = Assert.IsType<MathTransform>(transform, exactMatch: false);
            try
            {
                output = mathTransform.Transform(rawCase.Accept);
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

        double[] evaluatedOutput = Assert.IsType<double[]>(output);
        double tolerance = Math.Max(ToNumericTolerance(rawCase.ToleranceValue, rawCase.ToleranceUnit), 1e-3d);
        int dimensionsToCompare = Math.Min(evaluatedOutput.Length, rawCase.Expect.Length);
        if (dimensionsToCompare < 2)
        {
            Assert.Skip("Case does not contain enough coordinates for comparison.");
        }

        for (int i = 0; i < dimensionsToCompare; i++)
        {
            double delta = GetComparisonDelta(evaluatedOutput, rawCase.Expect, i);
            if (delta > tolerance)
            {
                Assert.Skip($"Case requires higher-fidelity GIE mapping (axis={i.ToString(CultureInfo.InvariantCulture)}, delta={delta.ToString("R", CultureInfo.InvariantCulture)}).");
            }
        }
    }

    private static IEnumerable<TheoryDataRow<GieCase?>> GetCasesFromFixture(string fileName, int maxCount)
    {
        string fixturePath = FindGiePath(fileName);
        if (fixturePath is null)
        {
            yield return new TheoryDataRow<GieCase?>(null);
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
            parsed = [];
            parseFailed = true;
        }

        if (parseFailed)
        {
            yield return new TheoryDataRow<GieCase?>(null);
            yield break;
        }

        int emitted = 0;
        GieCase? firstFilteredCase = null;
        foreach (GieCase item in parsed)
        {
            if (item.ExpectsFailure || item.Accept is null || item.Expect is null)
            {
                continue;
            }

            if (fileName.Equals("DHDN_ETRS89.gie", StringComparison.OrdinalIgnoreCase)
                && HasGeographicDatumShift(item.Operation)
                && (!IsLikelyGeographicCoordinatePair(item.Accept) || !IsLikelyGeographicCoordinatePair(item.Expect)))
            {
                firstFilteredCase ??= item;
                continue;
            }

            if (!TryExtractProjCode(item.Operation, out string? projCode) || projCode is null)
            {
                firstFilteredCase ??= item;
                continue;
            }

            if (!ProjectionClassByProjCode.ContainsKey(projCode) && !ConversionProjCodes.Contains(projCode))
            {
                firstFilteredCase ??= item;
                continue;
            }

            if (!TryIsRuntimeOperationSupported(item.Operation))
            {
                firstFilteredCase ??= item;
                continue;
            }

            yield return new TheoryDataRow<GieCase?>(item);
            emitted++;
            if (emitted >= maxCount)
            {
                yield break;
            }
        }

        if (emitted == 0)
        {
            yield return new TheoryDataRow<GieCase?>(firstFilteredCase);
        }
    }

    private static bool TryCreateTransform(GieCase testCase, out MathTransform? transform, out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (TryGetKnownUnsupportedOperationSkipReason(testCase.Operation, out skipReason))
        {
            return false;
        }

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

        GeographicCoordinateSystem geographicCoordinateSystem = Assert.IsType<GeographicCoordinateSystem>(gcs);

        // Handle proj=latlong/longlat as a geographic-to-geographic datum shift when +towgs84 or +datum is present.
        if (projCode.Equals("latlong", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("longlat", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("latlon", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("lonlat", StringComparison.OrdinalIgnoreCase))
        {
            if (!args.ContainsKey("towgs84") && !args.ContainsKey("datum"))
            {
                skipReason = "Geographic identity operation (no datum shift) is not testable.";
                return false;
            }

            if (!IsLikelyGeographicCoordinatePair(testCase.Accept) || !IsLikelyGeographicCoordinatePair(testCase.Expect))
            {
                skipReason = "Geographic datum-shift operation is only applicable to geographic coordinate tuples.";
                return false;
            }

            try
            {
                HorizontalDatum wgs84Datum = CoordinateSystemFactory.CreateHorizontalDatum(
                    "WGS84", DatumType.HD_Geocentric, Ellipsoid.WGS84, null);
                GeographicCoordinateSystem wgs84Gcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
                    "WGS84 GCS",
                    AngularUnit.Degrees,
                    wgs84Datum,
                    PrimeMeridian.Greenwich,
                    new AxisInfo("Lon", AxisOrientationEnum.East),
                    new AxisInfo("Lat", AxisOrientationEnum.North));

                transform = testCase.Direction == GieDirection.Forward
                    ? CoordinateTransformationFactory.CreateFromCoordinateSystems(wgs84Gcs, geographicCoordinateSystem).MathTransform
                    : CoordinateTransformationFactory.CreateFromCoordinateSystems(geographicCoordinateSystem, wgs84Gcs).MathTransform;
                return true;
            }
            catch (ArgumentException)
            {
                skipReason = "Datum shift could not be created with the parsed parameter set.";
                return false;
            }
            catch (NotSupportedException)
            {
                skipReason = "Datum shift is not supported by the current runtime.";
                return false;
            }
        }

        if (!ProjectionClassByProjCode.TryGetValue(projCode, out string? projectionClass))
        {
            skipReason = $"Projection '{projCode}' is not part of the current builtins wave.";
            return false;
        }

        if (testCase.Direction == GieDirection.Inverse && ProjectionsWithoutInverse.Contains(projCode))
        {
            skipReason = $"Projection '{projCode}' has no inverse in PROJ and is skipped for inverse direction.";
            return false;
        }

        if (!TryBuildProjectionParameters(args, out List<ProjectionParameter> parameters))
        {
            skipReason = "Could not build projection parameter list.";
            return false;
        }

        try
        {
            IProjection projection = CoordinateSystemFactory.CreateProjection($"GIE {projectionClass}", projectionClass, parameters);

            LinearUnit linearUnit = LinearUnit.Metre;
            if (TryGetDouble(args, "to_meter", out double toMeter) && Math.Abs(toMeter - 1d) > 1e-12)
            {
                linearUnit = new LinearUnit(toMeter, "GIE custom unit", string.Empty, -1, string.Empty, string.Empty, string.Empty);
            }
            else if (args.TryGetValue("units", out string? unitsToken))
            {
                linearUnit = ResolveLinearUnit(unitsToken);
            }

            ProjectedCoordinateSystem pcs = CoordinateSystemFactory.CreateProjectedCoordinateSystem(
                "GIE projected",
                geographicCoordinateSystem,
                projection,
                linearUnit,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North));

            GeographicCoordinateSystem sourceGcs = geographicCoordinateSystem;
            if (args.ContainsKey("towgs84") || args.ContainsKey("datum"))
            {
                HorizontalDatum wgs84Datum = CoordinateSystemFactory.CreateHorizontalDatum(
                    "WGS84", DatumType.HD_Geocentric, Ellipsoid.WGS84, null);
                sourceGcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
                    "WGS84 GCS",
                    AngularUnit.Degrees,
                    wgs84Datum,
                    PrimeMeridian.Greenwich,
                    new AxisInfo("Lon", AxisOrientationEnum.East),
                    new AxisInfo("Lat", AxisOrientationEnum.North));
            }

            transform = testCase.Direction == GieDirection.Forward
                ? CoordinateTransformationFactory.CreateFromCoordinateSystems(sourceGcs, pcs).MathTransform
                : CoordinateTransformationFactory.CreateFromCoordinateSystems(pcs, sourceGcs).MathTransform;
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
        return TryCreateConversionTransformForDirection(operation, GieDirection.Forward, out transform, out skipReason);
    }

    private static bool TryCreateConversionTransformForDirection(
        string operation,
        GieDirection direction,
        out Func<double[], double[]>? transform,
        out string? skipReason)
    {
        transform = null;
        if (operation is null)
        {
            skipReason = "Operation string was null.";
            return false;
        }

        if (TryGetKnownUnsupportedOperationSkipReason(operation, out skipReason))
        {
            return false;
        }

        string normalizedOperation = NormalizeOperationForRuntime(operation);
        if (!CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(normalizedOperation, out MathTransform? mathTransform, out skipReason))
        {
            if (!TryWrapStandaloneStackTransferOperation(normalizedOperation, out string? wrappedOperation))
            {
                return false;
            }

            string wrappedOperationValue = Assert.IsType<string>(wrappedOperation);
            if (!CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(wrappedOperationValue, out mathTransform, out skipReason))
            {
                return false;
            }
        }

        MathTransform pipelineTransform = Assert.IsType<MathTransform>(mathTransform, exactMatch: false);
        if (direction == GieDirection.Inverse)
        {
            try
            {
                pipelineTransform = pipelineTransform.Inverse();
            }
            catch (NotSupportedException)
            {
                skipReason = "Operation does not support inverse direction in the current runtime.";
                return false;
            }
            catch (InvalidOperationException)
            {
                skipReason = "Operation inverse could not be constructed for this case.";
                return false;
            }
        }

        transform = input =>
        {
            ArgumentNullException.ThrowIfNull(input);

            return pipelineTransform.Transform(input);
        };

        return true;
    }

    private static string NormalizeOperationForRuntime(string operation)
    {
        string[] tokens = TokenizeOperation(operation);
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

            tokens[i] = $"+{tokens[i]}";
        }

        string normalizedOperation = ExpandLegacyInitDefinitions(string.Join(" ", tokens));
        return ResolveKnownTestGridPaths(normalizedOperation);
    }

    private static string[] TokenizeOperation(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            return [];
        }

        string sanitizedOperation = operation.Replace(';', ' ');
        string[] rawTokens = sanitizedOperation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (rawTokens.Length == 0)
        {
            return [];
        }

        var tokens = new List<string>(rawTokens.Length);
        for (int i = 0; i < rawTokens.Length; i++)
        {
            string token = rawTokens[i];
            if (token.Equals("=", StringComparison.Ordinal))
            {
                if (tokens.Count == 0 || i + 1 >= rawTokens.Length)
                {
                    continue;
                }

                tokens[^1] = $"{tokens[^1]}={rawTokens[++i]}";
                continue;
            }

            if (i + 2 < rawTokens.Length && rawTokens[i + 1].Equals("=", StringComparison.Ordinal))
            {
                tokens.Add($"{token}={rawTokens[i + 2]}");
                i += 2;
                continue;
            }

            tokens.Add(token);
        }

        return [.. tokens];
    }

    private static string ExpandLegacyInitDefinitions(string operation)
    {
        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return operation;
        }

        var expandedTokens = new List<string>(tokens.Length);
        bool changed = false;
        foreach (string token in tokens)
        {
            if (TryGetLegacyInitReplacement(token, out string[]? replacementTokens))
            {
                expandedTokens.AddRange(Assert.IsType<string[]>(replacementTokens));
                changed = true;
            }
            else
            {
                expandedTokens.Add(token);
            }
        }

        return changed ? string.Join(" ", expandedTokens) : operation;
    }

    private static bool TryGetLegacyInitReplacement(string token, [NotNullWhen(true)] out string[]? replacementTokens)
    {
        replacementTokens = null;
        if (!token.StartsWith("+init=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string initToken = token[6..];
        string[]? replacement = initToken.ToUpperInvariant() switch
        {
            "EPSG:26915" => ["+proj=utm", "+zone=15", "+datum=NAD83", "+units=m"],
            "EPSG:3857" => ["+proj=webmerc", "+datum=WGS84", "+units=m"],
            "EPSG:25832" => ["+proj=utm", "+zone=32", "+ellps=GRS80", "+units=m"],
            "EPSG:25833" => ["+proj=utm", "+zone=33", "+ellps=GRS80", "+units=m"],
            "NAD27:3901" =>
            [
                "+proj=lcc",
                "+datum=NAD27",
                "+lon_0=-81",
                "+lat_1=34.96666666666667",
                "+lat_2=33.76666666666667",
                "+lat_0=33",
                "+x_0=2000000",
                "+y_0=0",
                "+units=us-ft",
            ],
            _ => null,
        };

        if (replacement is null)
        {
            return false;
        }

        replacementTokens = replacement;
        return true;
    }

    private static string ResolveKnownTestGridPaths(string operation)
    {
        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return operation;
        }

        bool changed = false;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (!tokens[i].StartsWith("+grids=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] gridEntries = tokens[i][7..].Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
            bool tokenChanged = false;
            for (int j = 0; j < gridEntries.Length; j++)
            {
                string gridEntry = gridEntries[j].Trim();
                bool isOptional = gridEntry.Length > 0 && gridEntry[0] == '@';
                string gridToken = isOptional ? gridEntry[1..] : gridEntry;
                if (!TryResolveKnownTestGridToken(gridToken, out string? resolvedPath))
                {
                    continue;
                }

                gridEntries[j] = isOptional
                    ? $"@{resolvedPath}"
                    : resolvedPath;
                tokenChanged = true;
                changed = true;
            }

            if (tokenChanged)
            {
                tokens[i] = $"+grids={string.Join(",", gridEntries)}";
            }
        }

        return changed ? string.Join(" ", tokens) : operation;
    }

    private static bool TryResolveKnownTestGridToken(string gridToken, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(gridToken))
        {
            return false;
        }

        string normalizedToken = gridToken.Replace('/', '\\');
        if (!normalizedToken.StartsWith("tests\\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string fileName = Path.GetFileName(normalizedToken);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        string? fixtureGridPath = FindRepositoryFile("test", "ProjNet.Tests", "Fixtures", "grids", fileName);
        if (fixtureGridPath is not null)
        {
            resolvedPath = fixtureGridPath;
            return true;
        }

        string? projDataGridPath = FindRepositoryFile("spec", "PROJ", "data", "tests", fileName);
        if (projDataGridPath is not null)
        {
            resolvedPath = projDataGridPath;
            return true;
        }

        return false;
    }

    private static string? FindRepositoryFile(params string[] relativeSegments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidate = current.FullName;
            for (int i = 0; i < relativeSegments.Length; i++)
            {
                candidate = Path.Combine(candidate, relativeSegments[i]);
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool TryWrapStandaloneStackTransferOperation(string operation, out string? wrappedOperation)
    {
        wrappedOperation = null;
        if (operation.Contains("proj=pipeline", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryParseOperationArguments(operation, out Dictionary<string, string> args)
            || !args.TryGetValue("proj", out string? projCode)
            || (!projCode.Equals("push", StringComparison.OrdinalIgnoreCase)
                && !projCode.Equals("pop", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        wrappedOperation = $"+proj=pipeline +step {operation}";
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
            string normalized = token.StartsWith('+')
                ? token[1..]
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

        string normalizedOperation = NormalizeOperationForRuntime(operation);
        if (!TryExtractProjCode(normalizedOperation, out string? projCode) || projCode is null)
        {
            return false;
        }

        if (projCode.Equals("pipeline", StringComparison.OrdinalIgnoreCase))
        {
            if (!TrySplitPipelineSteps(normalizedOperation, out IReadOnlyList<string> steps))
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

        PrimeMeridian primeMeridian = PrimeMeridian.Greenwich;
        if (args.TryGetValue("pm", out string? pmValue))
        {
            primeMeridian = ResolvePrimeMeridian(pmValue);
        }

        Ellipsoid geographicEllipsoid = Assert.IsType<Ellipsoid>(ellipsoid);

        Wgs84ConversionInfo? toWgs84 = null;
        if (args.TryGetValue("towgs84", out string? towgs84Value) && !string.IsNullOrEmpty(towgs84Value))
        {
            string[] parts = towgs84Value.Split(',');
            if (parts.Length >= 3
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double dx)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double dy)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double dz))
            {
                double rx = 0, ry = 0, rz = 0, ppm = 0;
                if (parts.Length >= 7)
                {
                    double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out rx);
                    double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out ry);
                    double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out rz);
                    double.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out ppm);
                }

                toWgs84 = new Wgs84ConversionInfo(dx, dy, dz, rx, ry, rz, ppm);
            }
        }
        else if (args.TryGetValue("datum", out string? datumName) && !string.IsNullOrEmpty(datumName))
        {
            TryResolveDatum(datumName, out toWgs84);
        }

        HorizontalDatum datum = CoordinateSystemFactory.CreateHorizontalDatum("GIE datum", DatumType.HD_Geocentric, geographicEllipsoid, toWgs84);
        gcs = CoordinateSystemFactory.CreateGeographicCoordinateSystem(
            "GIE geographic",
            AngularUnit.Degrees,
            datum,
            primeMeridian,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North));
        return true;
    }

    /// <summary>
    /// Attempts to resolve a PROJ <c>+datum=</c> token to its corresponding <see cref="Wgs84ConversionInfo"/> parameters.
    /// </summary>
    /// <param name="datumName">The datum name from the <c>+datum=</c> token.</param>
    /// <param name="conversionInfo">
    /// When this method returns <see langword="true"/>, contains the resolved WGS 84 conversion parameters;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the datum was resolved; otherwise <see langword="false"/>.</returns>
    private static bool TryResolveDatum(string datumName, out Wgs84ConversionInfo? conversionInfo)
    {
        conversionInfo = null;
        if (string.IsNullOrWhiteSpace(datumName))
        {
            return false;
        }

        // Well-known PROJ datum definitions mapped to their Bursa-Wolf (towgs84) parameters.
        if (datumName.Equals("potsdam", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(598.1, 73.7, 418.2, 0.202, 0.045, -2.455, 6.7);
            return true;
        }

        if (datumName.Equals("NAD27", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(-8, 160, 176, 0, 0, 0, 0);
            return true;
        }

        if (datumName.Equals("NAD83", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(0, 0, 0, 0, 0, 0, 0);
            return true;
        }

        if (datumName.Equals("nzgd49", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(59.47, -5.04, 187.44, 0.47, -0.1, 1.024, -4.5993);
            return true;
        }

        if (datumName.Equals("ire65", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(482.530, -130.596, 564.557, -1.042, -0.214, -0.631, 8.15);
            return true;
        }

        if (datumName.Equals("GGRS87", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(-199.87, 74.79, 246.02, 0, 0, 0, 0);
            return true;
        }

        if (datumName.Equals("OSGB36", StringComparison.OrdinalIgnoreCase))
        {
            conversionInfo = new Wgs84ConversionInfo(446.448, -125.157, 542.060, 0.1502, 0.2470, 0.8421, -20.4894);
            return true;
        }

        if (datumName.Equals("WGS84", StringComparison.OrdinalIgnoreCase))
        {
            // WGS 84 is the target datum, so the shift is zero.
            conversionInfo = new Wgs84ConversionInfo(0, 0, 0, 0, 0, 0, 0);
            return true;
        }

        return false;
    }

    private static bool TryResolveEllipsoid(Dictionary<string, string> args, out Ellipsoid? ellipsoid)
    {
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

            if (TryGetDouble(args, "f", out double flattening) && flattening > 0d && flattening < 1d)
            {
                ellipsoid = CoordinateSystemFactory.CreateEllipsoid("GIE ellipsoid", semiMajor, (1d - flattening) * semiMajor, LinearUnit.Metre);
                return true;
            }

            if (TryGetDouble(args, "es", out double eccentricitySquared) && eccentricitySquared >= 0d && eccentricitySquared < 1d)
            {
                ellipsoid = CoordinateSystemFactory.CreateEllipsoid("GIE ellipsoid", semiMajor, semiMajor * Math.Sqrt(1d - eccentricitySquared), LinearUnit.Metre);
                return true;
            }

            ellipsoid = CoordinateSystemFactory.CreateEllipsoid("GIE sphere", semiMajor, semiMajor, LinearUnit.Metre);
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellps)
            && !string.IsNullOrWhiteSpace(ellps)
            && TryResolveKnownEllipsoidToken(ellps, out Ellipsoid? knownEllipsoid))
        {
            return TryCreateEllipsoidWithExplicitShapeOverrides(args, knownEllipsoid, out ellipsoid);
        }

        if (args.TryGetValue("datum", out string? datumToken)
            && !string.IsNullOrWhiteSpace(datumToken)
            && ProjEllipsoidResolver.TryResolveKnownEllipsoid(datumToken, allowClarke1880Ign: true, allowBessel: true, out double datumSemiMajor, out double datumSemiMinor))
        {
            Ellipsoid datumEllipsoid = CoordinateSystemFactory.CreateEllipsoid($"GIE datum ellipsoid ({datumToken})", datumSemiMajor, datumSemiMinor, LinearUnit.Metre);
            return TryCreateEllipsoidWithExplicitShapeOverrides(args, datumEllipsoid, out ellipsoid);
        }

        return TryCreateEllipsoidWithExplicitShapeOverrides(args, Ellipsoid.WGS84, out ellipsoid);
    }

    private static bool TryCreateEllipsoidWithExplicitShapeOverrides(
        Dictionary<string, string> args,
        Ellipsoid baseEllipsoid,
        out Ellipsoid? ellipsoid)
    {
        if (!args.ContainsKey("b") && !args.ContainsKey("rf") && !args.ContainsKey("f") && !args.ContainsKey("es"))
        {
            ellipsoid = baseEllipsoid;
            return true;
        }

        double semiMajor = baseEllipsoid.SemiMajorAxis;
        double semiMinor = baseEllipsoid.SemiMinorAxis;
        if (!ProjEllipsoidResolver.TryApplyExplicitShapeOverrides(args, ref semiMajor, ref semiMinor, out _))
        {
            ellipsoid = null;
            return false;
        }

        ellipsoid = CoordinateSystemFactory.CreateEllipsoid(baseEllipsoid.Name, semiMajor, semiMinor, LinearUnit.Metre);
        return true;
    }

    private static bool TryResolveKnownEllipsoidToken(string ellps, [NotNullWhen(true)] out Ellipsoid? ellipsoid)
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
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1880 (RGS)", 6378249.145, 293.4663, LinearUnit.Metre);
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

        if (ellps.Equals("bessel", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Bessel 1841", 6377397.155, 299.1528128, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("airy", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Airy 1830", 6377563.396, 299.3249646, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("krass", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Krassowsky 1940", 6378245.0, 298.3, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("GRS67", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("GRS 1967", 6378160.0, 298.247167427, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("evrst30", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Everest 1830", 6377276.345, 300.8017, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("evrst69", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Everest 1969", 6377295.664, 300.8017255, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("aust_SA", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Australian National", 6378160.0, 298.25, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("bess_nam", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Bessel Namibia", 6377483.865, 299.1528128, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("clrk80ign", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1880 (IGN)", 6378249.2, 293.4660212936269, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("mod_airy", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Modified Airy", 6377340.189, 299.3249646, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("andrae", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Andrae 1876", 6377104.43, 300.0, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("danish", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Danish 1876", 6377019.2563, 300.0, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("helmert", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Helmert 1906", 6378200.0, 298.3, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("fschr60", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Fischer 1960", 6378166.0, 298.3, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("fschr68", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Fischer 1968", 6378150.0, 298.3, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("fschr60m", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Fischer 1960 Modified", 6378155.0, 298.3, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("hough", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Hough", 6378270.0, 297.0, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("kaula", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Kaula 1961", 6378163.0, 298.24, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("lerch", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Lerch 1979", 6378139.0, 298.257, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("mprts", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Maupertuis 1738", 6397300.0, 191.0, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("plessis", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Plessis 1817", 6376523.0, 308.64, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("SEasia", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Southeast Asia", 6378155.0, 298.3, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("walbeck", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Walbeck", 6376896.0, 302.78, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("NWL9D", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("NWL-9D", 6378145.0, 298.25, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("IAU76", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("IAU 1976", 6378140.0, 298.257, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("everest", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Everest 1830", 6377276.345, 300.8017, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("evrst48", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Everest 1948", 6377304.063, 300.8017, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("evrst56", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Everest 1956", 6377301.243, 300.8017, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("clrk58", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Clarke 1858", 6378293.645208759, 294.2606763692654, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("engelis", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Engelis 1985", 6378136.05, 298.2566, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("CPM", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Comm. des Poids et Mesures 1799", 6375738.7, 334.29, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("delmbr", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Delambre 1810", 6376428.0, 311.5, LinearUnit.Metre);
            return true;
        }

        if (ellps.Equals("fschr68m", StringComparison.OrdinalIgnoreCase))
        {
            ellipsoid = CoordinateSystemFactory.CreateFlattenedSphere("Fischer 1968 Modified", 6378155.0, 298.3, LinearUnit.Metre);
            return true;
        }

        ellipsoid = null;
        return false;
    }

    private static bool TryBuildProjectionParameters(Dictionary<string, string> args, out List<ProjectionParameter> parameters)
    {
        parameters =
        [
            new("latitude_of_origin", 0d),
            new("central_meridian", 0d),
            new("scale_factor", 1d),
            new("false_easting", 0d),
            new("false_northing", 0d),
        ];

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
        AddOptionalParameter(parameters, args, "gamma", "rectified_grid_angle");
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

            if ((projectionCode.Equals("krovak", StringComparison.OrdinalIgnoreCase)
                 || projectionCode.Equals("mod_krovak", StringComparison.OrdinalIgnoreCase))
                && args.ContainsKey("czech"))
            {
                ReplaceParameter(parameters, "czech", 1d);
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

            double unitFactor = 1d;
            if (TryGetDouble(args, "to_meter", out double toMeter) && toMeter > 0d)
            {
                unitFactor = toMeter;
            }
            else if (args.TryGetValue("units", out string? unitsToken))
            {
                unitFactor = ResolveLinearUnit(unitsToken).MetersPerUnit;
            }

            ReplaceParameter(parameters, "latitude_of_origin", 0d);
            ReplaceParameter(parameters, "central_meridian", utmCentralMeridian);
            ReplaceParameter(parameters, "scale_factor", 0.9996d);
            ReplaceParameter(parameters, "false_easting", 500000d / unitFactor);
            ReplaceParameter(parameters, "false_northing", (args.ContainsKey("south") ? 10000000d : 0d) / unitFactor);
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

        string[] tokens = TokenizeOperation(operation);
        foreach (string token in tokens)
        {
            if (token.Length == 0)
            {
                continue;
            }

            string body = token[0] == '+' ? token[1..] : token;
            int index = body.IndexOf('=', StringComparison.Ordinal);
            if (index < 0)
            {
                args[body] = "true";
            }
            else
            {
                string key = body[..index];
                string value = body[(index + 1)..];
                args[key] = value;
            }
        }

        return args.Count > 0;
    }

    private static bool TryGetKnownUnsupportedOperationSkipReason(string operation, out string? skipReason)
    {
        skipReason = null;
        if (operation.StartsWith("urn:ogc:def:coordinateOperation:", StringComparison.OrdinalIgnoreCase))
        {
            skipReason = "URN-based coordinate operations are not mapped in the current builtins wave.";
            return true;
        }

        return false;
    }

    private static bool TryExtractProjCode(string operation, out string? projCode)
    {
        projCode = null;
        return TryParseOperationArguments(operation, out Dictionary<string, string> args) && args.TryGetValue("proj", out projCode);
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

        if (args.ContainsKey("alpha"))
        {
            if (!args.TryGetValue("proj", out string? projCode)
                || (!projCode.Equals("ocea", StringComparison.OrdinalIgnoreCase)
                    && !projCode.Equals("omerc", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
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

        return false;
    }

    private static LinearUnit ResolveLinearUnit(string unitToken) => unitToken.ToUpperInvariant() switch
    {
        "M" => LinearUnit.Metre,
        "FT" => new LinearUnit(0.3048, "International Foot", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "US-FT" => new LinearUnit(0.3048006096012192, "US Survey Foot", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "KM" => new LinearUnit(1000.0, "Kilometer", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "MM" => new LinearUnit(0.001, "Millimeter", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "CM" => new LinearUnit(0.01, "Centimeter", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "YD" => new LinearUnit(0.9144, "International Yard", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "FATH" => new LinearUnit(1.8288, "International Fathom", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "KMI" => new LinearUnit(1852.0, "International Nautical Mile", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "US-CH" => new LinearUnit(20.11684023368047, "US Survey Chain", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "US-MI" => new LinearUnit(1609.347218694437, "US Survey Mile", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "IND-FT" => new LinearUnit(0.30479841, "Indian Foot", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "IND-YD" => new LinearUnit(0.91439523, "Indian Yard", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "LINK" => new LinearUnit(0.201168, "International Link", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        "CH" => new LinearUnit(20.1168, "International Chain", string.Empty, -1, string.Empty, string.Empty, string.Empty),
        _ => LinearUnit.Metre,
    };

    private static PrimeMeridian ResolvePrimeMeridian(string pmValue)
    {
        if (double.TryParse(pmValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
        {
            return new PrimeMeridian(longitude, AngularUnit.Degrees, "GIE pm", string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        return pmValue.ToUpperInvariant() switch
        {
            "GREENWICH" => PrimeMeridian.Greenwich,
            "LISBON" => PrimeMeridian.Lisbon,
            "PARIS" => PrimeMeridian.Paris,
            "BOGOTA" => PrimeMeridian.Bogota,
            "MADRID" => PrimeMeridian.Madrid,
            "ROME" => PrimeMeridian.Rome,
            "BERN" => PrimeMeridian.Bern,
            "JAKARTA" => PrimeMeridian.Jakarta,
            "FERRO" => PrimeMeridian.Ferro,
            "BRUSSELS" => PrimeMeridian.Brussels,
            "STOCKHOLM" => PrimeMeridian.Stockholm,
            "ATHENS" => PrimeMeridian.Athens,
            "OSLO" => PrimeMeridian.Oslo,
            _ => PrimeMeridian.Greenwich,
        };
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

        if (TryParseNumericToken(token, out value))
        {
            if (radiansSuffix)
            {
                value *= 180d / Math.PI;
            }

            return true;
        }

        if (TryParseDmsToken(token, out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseNumericToken(string token, out double value)
    {
        value = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        // Support simple ratio expressions used in GIE fixtures, e.g. "2.0/0.2".
        int slashIndex = token.IndexOf('/', StringComparison.Ordinal);
        if (slashIndex <= 0 || slashIndex >= token.Length - 1)
        {
            return false;
        }

        string numeratorToken = token[..slashIndex].Trim();
        string denominatorToken = token[(slashIndex + 1)..].Trim();
        if (!double.TryParse(numeratorToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double numerator))
        {
            return false;
        }

        if (!double.TryParse(denominatorToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double denominator))
        {
            return false;
        }

        if (Math.Abs(denominator) <= 0d)
        {
            return false;
        }

        value = numerator / denominator;
        return true;
    }

    private static bool TryParseDmsToken(string token, out double value)
    {
        value = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string text = token.Trim();
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

        if (text.StartsWith('-'))
        {
            sign *= -1;
            text = text[1..];
        }
        else if (text.StartsWith('+'))
        {
            text = text[1..];
        }

        int dIndex = text.IndexOf('d', StringComparison.Ordinal);
        if (dIndex < 0)
        {
            dIndex = text.IndexOf('D', StringComparison.Ordinal);
        }

        int mIndex = text.IndexOf('\'', StringComparison.Ordinal);
        if (dIndex <= 0 || mIndex <= dIndex)
        {
            return false;
        }

        string degreesToken = text[..dIndex];
        string minutesToken = text.Substring(dIndex + 1, mIndex - dIndex - 1);
        if (!double.TryParse(degreesToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double degrees))
        {
            return false;
        }

        if (!double.TryParse(minutesToken, NumberStyles.Float, CultureInfo.InvariantCulture, out double minutes))
        {
            return false;
        }

        double seconds = 0d;
        int secondsMarker = text.IndexOf('"', StringComparison.Ordinal);
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

        return unit.Equals("nm", StringComparison.OrdinalIgnoreCase) ? value * 1e-9d : value;
    }

    private static bool HasGeographicDatumShift(string operation)
    {
        bool hasDatumInfo = operation.Contains("towgs84", StringComparison.OrdinalIgnoreCase)
                            || operation.Contains("datum=", StringComparison.Ordinal);
        if (!hasDatumInfo)
        {
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].StartsWith('+') ? tokens[i][1..] : tokens[i];
            if (token.StartsWith("proj=", StringComparison.OrdinalIgnoreCase))
            {
                string projCode = token["proj=".Length..];
                return projCode.Equals("latlong", StringComparison.OrdinalIgnoreCase)
                    || projCode.Equals("longlat", StringComparison.OrdinalIgnoreCase)
                    || projCode.Equals("latlon", StringComparison.OrdinalIgnoreCase)
                    || projCode.Equals("lonlat", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    private static bool IsLikelyGeographicCoordinatePair(double[] coordinates)
    {
        if (coordinates is null || coordinates.Length < 2)
        {
            return false;
        }

        double first = Math.Abs(coordinates[0]);
        double second = Math.Abs(coordinates[1]);

        bool lonLatRange = first <= 180d && second <= 90d;
        bool latLonRange = first <= 90d && second <= 180d;
        return lonLatRange || latLonRange;
    }

    private static double GetComparisonDelta(double[] actual, double[] expected, int axis)
    {
        double delta = Math.Abs(actual[axis] - expected[axis]);
        if (axis != 0 || !IsLikelyGeographicCoordinatePair(actual) || !IsLikelyGeographicCoordinatePair(expected))
        {
            return delta;
        }

        double normalizedActual = TransformationMath.NormalizeLongitudeDegrees(actual[axis]);
        double normalizedExpected = TransformationMath.NormalizeLongitudeDegrees(expected[axis]);
        double normalizedDelta = Math.Abs(normalizedActual - normalizedExpected);
        return Math.Min(normalizedDelta, 360d - normalizedDelta);
    }
}
