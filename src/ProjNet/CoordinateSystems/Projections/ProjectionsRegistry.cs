// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Registry that maps projection names and aliases to their corresponding <see cref="MapProjection"/> implementation types.
/// </summary>
public class ProjectionsRegistry
{
    private static readonly Dictionary<string, Type> TypeRegistry = new();
    private static readonly Dictionary<string, Type> ConstructorRegistry = new();

    private static readonly object RegistryLock = new();

    /// <summary>
    /// Initializes static members of the <see cref="ProjectionsRegistry"/> class.
    /// </summary>
    static ProjectionsRegistry()
    {
        Register("mercator", typeof(Mercator));
        Register("merc", typeof(Mercator));
        Register("mercator_1sp", typeof(Mercator));
        Register("mercator_2sp", typeof(Mercator));
        Register("mercator_(variant_a)", typeof(Mercator));
        Register("mercator_(variant_b)", typeof(Mercator));
        Register("mercator_auxiliary_sphere", typeof(MercatorAuxiliarySphere));
        Register("pseudo_mercator", typeof(PseudoMercator));
        Register("popular_visualisation_pseudo_mercator", typeof(PseudoMercator));
        Register("google_mercator", typeof(PseudoMercator));
        Register("web_mercator", typeof(PseudoMercator));
        Register("webmerc", typeof(PseudoMercator));
        Register("miller_cylindrical", typeof(MillerCylindricalProjection));
        Register("miller", typeof(MillerCylindricalProjection));
        Register("mill", typeof(MillerCylindricalProjection));
        Register("equidistant_cylindrical", typeof(EquidistantCylindricalProjection));
        Register("equirectangular", typeof(EquidistantCylindricalProjection));
        Register("plate_carree", typeof(EquidistantCylindricalProjection));
        Register("eqc", typeof(EquidistantCylindricalProjection));
        Register("latlong", typeof(LatLongProjection));
        Register("latlon", typeof(LatLongProjection));
        Register("lonlat", typeof(LatLongProjection));
        Register("longlat", typeof(LatLongProjection));
        Register("calcofi", typeof(CalCoFiProjection));
        Register("cal_coop_ocean_fish_invest_lines_stations", typeof(CalCoFiProjection));
        Register("tobmerc", typeof(ToblerMercatorProjection));
        Register("tobler_mercator", typeof(ToblerMercatorProjection));
        Register("col_urban", typeof(ColombiaUrbanProjection));
        Register("colombia_urban", typeof(ColombiaUrbanProjection));
        Register("transverse_cylindrical_equal_area", typeof(TransverseCylindricalEqualAreaProjection));
        Register("tcea", typeof(TransverseCylindricalEqualAreaProjection));
        Register("cylindrical_equal_area", typeof(CylindricalEqualAreaProjection));
        Register("lambert_cylindrical_equal_area", typeof(CylindricalEqualAreaProjection));
        Register("equal_area_cylindrical", typeof(CylindricalEqualAreaProjection));
        Register("cea", typeof(CylindricalEqualAreaProjection));
        Register("loximuthal", typeof(LoximuthalProjection));
        Register("loxim", typeof(LoximuthalProjection));
        Register("patterson", typeof(PattersonProjection));

        Register("transverse_mercator", typeof(TransverseMercator));
        Register("tmerc", typeof(TransverseMercator));
        Register("transverse_mercator_south_oriented", typeof(TransverseMercator));
        Register("gauss_kruger", typeof(TransverseMercator));
        Register("utm", typeof(TransverseMercator));
        Register("etmerc", typeof(TransverseMercator));
        Register("extended_transverse_mercator", typeof(TransverseMercator));
        Register("swiss_oblique_mercator", typeof(SwissObliqueMercatorProjection));
        Register("somerc", typeof(SwissObliqueMercatorProjection));

        Register("albers", typeof(AlbersProjection));
        Register("aea", typeof(AlbersProjection));
        Register("albers_conic_equal_area", typeof(AlbersProjection));
        Register("leac", typeof(LambertEqualAreaConicProjection));

        Register("krovak", typeof(KrovakProjection));
        Register("mod_krovak", typeof(KrovakProjection));

        Register("polyconic", typeof(PolyconicProjection));
        Register("poly", typeof(PolyconicProjection));

        Register("lambert_conformal_conic", typeof(LambertConformalConic2SP));
        Register("lcc", typeof(LambertConformalConic2SP));
        Register("lambert_conformal_conic_1sp", typeof(LambertConformalConic2SP));
        Register("lambert_conformal_conic_2sp", typeof(LambertConformalConic2SP));
        Register("lambert_conformal_conic_2sp_belgium", typeof(LambertConformalConic2SP));
        Register("lambert_conic_conformal_(1sp)", typeof(LambertConformalConic2SP));
        Register("lambert_conic_conformal_(2sp)", typeof(LambertConformalConic2SP));
        Register("lambert_tangential_conformal_conic_projection", typeof(LambertConformalConic2SP));
        Register("lcca", typeof(LambertConformalConicAlternativeProjection));
        Register("lambert_conformal_conic_alternative", typeof(LambertConformalConicAlternativeProjection));
        Register("equidistant_conic", typeof(EquidistantConicProjection));
        Register("equidistant_conic_(spherical)", typeof(EquidistantConicProjection));
        Register("eqdc", typeof(EquidistantConicProjection));
        Register("euler", typeof(EulerProjection));
        Register("murd1", typeof(Murdoch1Projection));
        Register("murd2", typeof(Murdoch2Projection));
        Register("murd3", typeof(Murdoch3Projection));
        Register("tissot", typeof(TissotProjection));
        Register("vitk1", typeof(Vitkovsky1Projection));
        Register("imw_p", typeof(InternationalMapWorldPolyconicProjection));
        Register("international_map_of_the_world_polyconic", typeof(InternationalMapWorldPolyconicProjection));
        Register("bonne", typeof(BonneProjection));
        Register("perspective_conic", typeof(PconicProjection));
        Register("pconic", typeof(PconicProjection));
        Register("ccon", typeof(CentralConicProjection));
        Register("central_conic", typeof(CentralConicProjection));

        Register("lambert_azimuthal_equal_area", typeof(LambertAzimuthalEqualAreaProjection));
        Register("laea", typeof(LambertAzimuthalEqualAreaProjection));

        Register("cass", typeof(CassiniSoldnerProjection));
        Register("cassini_soldner", typeof(CassiniSoldnerProjection));
        Register("omerc", typeof(ObliqueMercatorProjection));
        Register("hotine_oblique_mercator", typeof(HotineObliqueMercatorProjection));
        Register("hotine_oblique_mercator_azimuth_center", typeof(HotineObliqueMercatorProjection));
        Register("oblique_mercator", typeof(ObliqueMercatorProjection));
        Register("sterea", typeof(ObliqueStereographicProjection));
        Register("oblique_stereographic", typeof(ObliqueStereographicProjection));
        Register("ortho", typeof(OrthographicProjection));
        Register("orthographic", typeof(OrthographicProjection));
        Register("ocea", typeof(ObliqueCylindricalEqualAreaProjection));
        Register("oblique_cylindrical_equal_area", typeof(ObliqueCylindricalEqualAreaProjection));
        Register("oea", typeof(OblatedEqualAreaProjection));
        Register("oblated_equal_area", typeof(OblatedEqualAreaProjection));
        Register("near_sided_perspective", typeof(NearSidedPerspectiveProjection));
        Register("nsper", typeof(NearSidedPerspectiveProjection));
        Register("tilted_perspective", typeof(NearSidedPerspectiveProjection));
        Register("tpers", typeof(NearSidedPerspectiveProjection));
        Register("laborde", typeof(LabordeProjection));
        Register("labrd", typeof(LabordeProjection));
        Register("gauss_schreiber_transverse_mercator", typeof(GaussSchreiberTransverseMercatorProjection));
        Register("gauss_laborde_reunion", typeof(GaussSchreiberTransverseMercatorProjection));
        Register("gstmerc", typeof(GaussSchreiberTransverseMercatorProjection));
        Register("geostationary_satellite", typeof(GeostationarySatelliteProjection));
        Register("geos", typeof(GeostationarySatelliteProjection));
        Register("new_zealand_map_grid", typeof(NewZealandMapGridProjection));
        Register("nzmg", typeof(NewZealandMapGridProjection));
        Register("stere", typeof(PolarStereographicProjection));
        Register("polar_stereographic", typeof(PolarStereographicProjection));
        Register("ups", typeof(UpsProjection));

        Register("equal_earth", typeof(EqualEarthProjection));
        Register("eqearth", typeof(EqualEarthProjection));
        Register("eck1", typeof(Eckert1Projection));
        Register("eckert_i", typeof(Eckert1Projection));
        Register("eck2", typeof(Eckert2Projection));
        Register("eckert_ii", typeof(Eckert2Projection));
        Register("eck3", typeof(Eckert3Projection));
        Register("eckert_iii", typeof(Eckert3Projection));
        Register("eck4", typeof(Eckert4Projection));
        Register("eckert_iv", typeof(Eckert4Projection));
        Register("eck5", typeof(Eckert5Projection));
        Register("eckert_v", typeof(Eckert5Projection));
        Register("putp2", typeof(PutninsP2Projection));
        Register("putnins_p2", typeof(PutninsP2Projection));
        Register("putp1", typeof(PutninsP1Projection));
        Register("putnins_p1", typeof(PutninsP1Projection));
        Register("putp3", typeof(PutninsP3Projection));
        Register("putnins_p3", typeof(PutninsP3Projection));
        Register("putp3p", typeof(PutninsP3PrimeProjection));
        Register("putnins_p3p", typeof(PutninsP3PrimeProjection));
        Register("putp4p", typeof(PutninsP4PProjection));
        Register("putnins_p4p", typeof(PutninsP4PProjection));
        Register("weren", typeof(WerenskioldProjection));
        Register("werenskiold_i", typeof(WerenskioldProjection));
        Register("putp5", typeof(PutninsP5Projection));
        Register("putnins_p5", typeof(PutninsP5Projection));
        Register("putp5p", typeof(PutninsP5PrimeProjection));
        Register("putnins_p5p", typeof(PutninsP5PrimeProjection));
        Register("putp6", typeof(PutninsP6Projection));
        Register("putnins_p6", typeof(PutninsP6Projection));
        Register("putp6p", typeof(PutninsP6PrimeProjection));
        Register("putnins_p6p", typeof(PutninsP6PrimeProjection));
        Register("kav7", typeof(Kavrayskiy7Projection));
        Register("kavrayskiy_vii", typeof(Kavrayskiy7Projection));
        Register("wag2", typeof(Wagner2Projection));
        Register("wagner_ii", typeof(Wagner2Projection));
        Register("wag3", typeof(Wagner3Projection));
        Register("wagner_iii", typeof(Wagner3Projection));
        Register("wag4", typeof(Wagner4Projection));
        Register("wagner_iv", typeof(Wagner4Projection));
        Register("wag5", typeof(Wagner5Projection));
        Register("wagner_v", typeof(Wagner5Projection));
        Register("wag6", typeof(Wagner6Projection));
        Register("wagner_vi", typeof(Wagner6Projection));
        Register("wag1", typeof(Wagner1Projection));
        Register("wagner_i", typeof(Wagner1Projection));
        Register("wag7", typeof(Wagner7Projection));
        Register("wagner_vii", typeof(Wagner7Projection));
        Register("cc", typeof(CentralCylindricalProjection));
        Register("central_cylindrical", typeof(CentralCylindricalProjection));
        Register("gall", typeof(GallProjection));
        Register("gall_stereographic", typeof(GallProjection));
        Register("gn_sinu", typeof(GeneralSinusoidalProjection));
        Register("general_sinusoidal", typeof(GeneralSinusoidalProjection));
        Register("eck6", typeof(Eckert6Projection));
        Register("eckert_vi", typeof(Eckert6Projection));
        Register("kav5", typeof(Kavrayskiy5Projection));
        Register("kavrayskiy_v", typeof(Kavrayskiy5Projection));
        Register("qua_aut", typeof(QuarticAuthalicProjection));
        Register("quartic_authalic", typeof(QuarticAuthalicProjection));
        Register("fouc", typeof(FoucautProjection));
        Register("foucaut", typeof(FoucautProjection));
        Register("mbt_s", typeof(McBrydeThomasFlatPolarSineProjection));
        Register("mcbryde_thomas_flat_polar_sine", typeof(McBrydeThomasFlatPolarSineProjection));
        Register("mbtfps", typeof(McBrydeThomasFlatPolarSinusoidalProjection));
        Register("mcbryde_thomas_flat_polar_sinusoidal", typeof(McBrydeThomasFlatPolarSinusoidalProjection));
        Register("mbtfpp", typeof(McBrydeThomasFlatPolarParabolicProjection));
        Register("mbt_fpp", typeof(McBrydeThomasFlatPolarParabolicProjection));
        Register("mcbryde_thomas_flat_polar_parabolic", typeof(McBrydeThomasFlatPolarParabolicProjection));
        Register("mbtfpq", typeof(McBrydeThomasFlatPolarQuarticProjection));
        Register("mcbryde_thomas_flat_polar_quartic", typeof(McBrydeThomasFlatPolarQuarticProjection));
        Register("mbt_fps", typeof(McBrydeThomasFlatPoleSineProjection));
        Register("mcbryde_thomas_flat_pole_sine", typeof(McBrydeThomasFlatPoleSineProjection));
        Register("crast", typeof(CrasterProjection));
        Register("craster_parabolic", typeof(CrasterProjection));
        Register("fahey", typeof(FaheyProjection));
        Register("collg", typeof(CollignonProjection));
        Register("collignon", typeof(CollignonProjection));
        Register("boggs", typeof(BoggsProjection));
        Register("boggs_eumorphic", typeof(BoggsProjection));
        Register("airy", typeof(AiryProjection));
        Register("bipc", typeof(BipolarConicProjection));
        Register("bipolar_conic", typeof(BipolarConicProjection));
        Register("chamb", typeof(ChamberlinTrimetricProjection));
        Register("chamberlin_trimetric", typeof(ChamberlinTrimetricProjection));
        Register("hatano", typeof(HatanoProjection));
        Register("hatano_asymmetrical_equal_area", typeof(HatanoProjection));
        Register("nell", typeof(NellProjection));
        Register("nell_h", typeof(NellHammerProjection));
        Register("nell_hammer", typeof(NellHammerProjection));
        Register("nicol", typeof(NicolosiProjection));
        Register("nicolosi_globular", typeof(NicolosiProjection));
        Register("urm5", typeof(UrmaevVProjection));
        Register("urmaev_v", typeof(UrmaevVProjection));
        Register("urmfps", typeof(UrmaevFlatPolarSinusoidalProjection));
        Register("urmaev_flat_polar_sinusoidal", typeof(UrmaevFlatPolarSinusoidalProjection));
        Register("times", typeof(TimesProjection));
        Register("times_projection", typeof(TimesProjection));
        Register("rpoly", typeof(RectangularPolyconicProjection));
        Register("rectangular_polyconic", typeof(RectangularPolyconicProjection));
        Register("tpeqd", typeof(TwoPointEquidistantProjection));
        Register("two_point_equidistant", typeof(TwoPointEquidistantProjection));
        Register("august", typeof(AugustProjection));
        Register("august_epicycloidal", typeof(AugustProjection));
        Register("bacon", typeof(BaconProjection));
        Register("bacon_globular", typeof(BaconProjection));
        Register("apian", typeof(ApianProjection));
        Register("apian_globular_i", typeof(ApianProjection));
        Register("ortel", typeof(OrteliusProjection));
        Register("ortelius_oval", typeof(OrteliusProjection));
        Register("comill", typeof(CompactMillerProjection));
        Register("compact_miller", typeof(CompactMillerProjection));
        Register("denoy", typeof(DenoyerProjection));
        Register("denoyer_semi_elliptical", typeof(DenoyerProjection));
        Register("fouc_s", typeof(FoucautSinusoidalProjection));
        Register("foucaut_sinusoidal", typeof(FoucautSinusoidalProjection));
        Register("gins8", typeof(Ginsburg8Projection));
        Register("ginsburg_viii", typeof(Ginsburg8Projection));
        Register("lagrng", typeof(LagrangeProjection));
        Register("lagrange", typeof(LagrangeProjection));
        Register("larr", typeof(LarriveeProjection));
        Register("larrivee", typeof(LarriveeProjection));
        Register("lask", typeof(LaskowskiProjection));
        Register("laskowski", typeof(LaskowskiProjection));
        Register("tcc", typeof(TransverseCentralCylindricalProjection));
        Register("transverse_central_cylindrical", typeof(TransverseCentralCylindricalProjection));
        Register("aitoff", typeof(AitoffProjection));
        Register("vandg", typeof(VanDerGrintenProjection));
        Register("vandergrinten", typeof(VanDerGrintenProjection));
        Register("van_der_grinten", typeof(VanDerGrintenProjection));
        Register("van_der_grinten_i", typeof(VanDerGrintenProjection));
        Register("vandg2", typeof(VanDerGrinten2Projection));
        Register("van_der_grinten_ii", typeof(VanDerGrinten2Projection));
        Register("vandg3", typeof(VanDerGrinten3Projection));
        Register("van_der_grinten_iii", typeof(VanDerGrinten3Projection));
        Register("vandg4", typeof(VanDerGrinten4Projection));
        Register("van_der_grinten_iv", typeof(VanDerGrinten4Projection));
        Register("wink1", typeof(Winkel1Projection));
        Register("winkel_i", typeof(Winkel1Projection));
        Register("wink2", typeof(Winkel2Projection));
        Register("winkel_ii", typeof(Winkel2Projection));
        Register("wintri", typeof(WinkelTripelProjection));
        Register("winkel_tripel", typeof(WinkelTripelProjection));
        Register("hammer", typeof(HammerProjection));
        Register("sinu", typeof(SinusoidalProjection));
        Register("sinusoidal", typeof(SinusoidalProjection));
        Register("goode", typeof(GoodeProjection));
        Register("goode_homolosine", typeof(GoodeProjection));
        Register("igh", typeof(IghProjection));
        Register("interrupted_goode_homolosine", typeof(IghProjection));
        Register("imoll", typeof(InterruptedMollweideProjection));
        Register("interrupted_mollweide", typeof(InterruptedMollweideProjection));
        Register("imoll_o", typeof(InterruptedMollweideOceanicProjection));
        Register("interrupted_mollweide_oceanic_view", typeof(InterruptedMollweideOceanicProjection));
        Register("igh_o", typeof(InterruptedGoodeHomolosineOceanicProjection));
        Register("interrupted_goode_homolosine_oceanic_view", typeof(InterruptedGoodeHomolosineOceanicProjection));
        Register("bertin1953", typeof(Bertin1953Projection));
        Register("bertin_1953", typeof(Bertin1953Projection));
        Register("healpix", typeof(HealpixProjection));
        Register("rhealpix", typeof(HealpixProjection));
        Register("s2", typeof(S2Projection));
        Register("s2_projection", typeof(S2Projection));
        Register("sch", typeof(SchMathTransform));
        Register("spherical_cross_track_height", typeof(SchMathTransform));
        Register("som", typeof(SpaceObliqueMercatorProjection));
        Register("space_oblique_mercator", typeof(SpaceObliqueMercatorProjection));
        Register("misrsom", typeof(SpaceObliqueMercatorProjection));
        Register("lsat", typeof(SpaceObliqueMercatorProjection));
        Register("qsc", typeof(QuadrilateralizedSphericalCubeProjection));
        Register("quadrilateralized_spherical_cube", typeof(QuadrilateralizedSphericalCubeProjection));
        Register("rouss", typeof(RoussilheStereographicProjection));
        Register("roussilhe_stereographic", typeof(RoussilheStereographicProjection));
        Register("mil_os", typeof(MillerOblatedStereographicProjection));
        Register("miller_oblated_stereographic", typeof(MillerOblatedStereographicProjection));
        Register("lee_os", typeof(LeeOblatedStereographicProjection));
        Register("lee_oblated_stereographic", typeof(LeeOblatedStereographicProjection));
        Register("gs48", typeof(ModifiedStereographic48USProjection));
        Register("modified_stereographic_48_us", typeof(ModifiedStereographic48USProjection));
        Register("alsk", typeof(ModifiedStereographicAlaskaProjection));
        Register("modified_stereographic_alaska", typeof(ModifiedStereographicAlaskaProjection));
        Register("gs50", typeof(ModifiedStereographic50USProjection));
        Register("modified_stereographic_50_us", typeof(ModifiedStereographic50USProjection));
        Register("guyou", typeof(GuyouProjection));
        Register("peirce_q", typeof(PeirceQuincuncialProjection));
        Register("peirce_quincuncial", typeof(PeirceQuincuncialProjection));
        Register("adams_hemi", typeof(AdamsHemisphereInSquareProjection));
        Register("adams_hemisphere_in_a_square", typeof(AdamsHemisphereInSquareProjection));
        Register("adams_ws1", typeof(AdamsWorldInSquareIProjection));
        Register("adams_world_in_a_square_i", typeof(AdamsWorldInSquareIProjection));
        Register("adams_ws2", typeof(AdamsWorldInSquareIIProjection));
        Register("adams_world_in_a_square_ii", typeof(AdamsWorldInSquareIIProjection));
        Register("spilhaus", typeof(SpilhausProjection));
        Register("airocean", typeof(AiroceanProjection));
        Register("isea", typeof(IseaProjection));
        Register("icosahedral_snyder_equal_area", typeof(IseaProjection));

        Register("natural_earth", typeof(NaturalEarthProjection));
        Register("natearth", typeof(NaturalEarthProjection));

        Register("natural_earth_2", typeof(NaturalEarth2Projection));
        Register("natural_earth2", typeof(NaturalEarth2Projection));
        Register("natearth2", typeof(NaturalEarth2Projection));

        Register("robinson", typeof(RobinsonProjection));
        Register("robin", typeof(RobinsonProjection));

        Register("mollweide", typeof(MollweideProjection));
        Register("moll", typeof(MollweideProjection));

        Register("azimuthal_equidistant", typeof(AzimuthalEquidistantProjection));
        Register("aeqd", typeof(AzimuthalEquidistantProjection));

        Register("gnomonic", typeof(GnomonicProjection));
        Register("gnom", typeof(GnomonicProjection));
    }

    /// <summary>
    /// Registers a projection type under the given name.
    /// </summary>
    /// <param name="name">The projection name or alias (case-insensitive).</param>
    /// <param name="type">The <see cref="MathTransform"/>-derived type that implements the projection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="type"/> does not derive from <see cref="MathTransform"/>, lacks a required
    /// constructor, or a different type is already registered under <paramref name="name"/>.
    /// </exception>
    public static void Register(string name, Type type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ArgumentGuard.ThrowIfNull(name, nameof(name));
        }

        ArgumentGuard.ThrowIfNull(type, nameof(type));

        if (!typeof(MathTransform).IsAssignableFrom(type))
        {
            ArgumentGuard.ThrowArgument("The provided type does not implement 'GeoAPI.CoordinateSystems.Transformations.IMathTransform'!", nameof(type));
        }

        var ci = CheckConstructor(type);
        if (ci is null)
        {
            ArgumentGuard.ThrowArgument("The provided type is lacking a suitable constructor", nameof(type));
        }

        string key = ProjectionNameToRegistryKey(name);
        lock (RegistryLock)
        {
            if (TypeRegistry.TryGetValue(key, out var registeredType))
            {
                if (ReferenceEquals(type, registeredType))
                {
                    return;
                }

                ArgumentGuard.ThrowArgument("A different projection type has been registered with this name", nameof(name));
            }

            TypeRegistry.Add(key, type);
            ConstructorRegistry.Add(key, ci);
        }
    }

    /// <summary>
    /// Registers an alternative name for an already-registered projection type.
    /// </summary>
    /// <param name="aliasName">The new alias to register.</param>
    /// <param name="existingName">The name of the already-registered projection.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aliasName"/> or <paramref name="existingName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="existingName"/> is not a registered projection name.</exception>
    public static void RegisterAlias(string aliasName, string existingName)
    {
        ArgumentGuard.ThrowIfNull(aliasName, nameof(aliasName));

        ArgumentGuard.ThrowIfNull(existingName, nameof(existingName));

        lock (RegistryLock)
        {
            if (!TypeRegistry.TryGetValue(ProjectionNameToRegistryKey(existingName), out var existingProjectionType))
            {
                ArgumentGuard.ThrowArgument($"{existingName} is not a registered projection type");
            }

            Register(aliasName, existingProjectionType);
        }
    }

    /// <summary>
    /// Creates a projection transform instance for the provided projection class name.
    /// </summary>
    /// <param name="className">Projection class name or alias.</param>
    /// <param name="parameters">Projection parameters passed to the constructor.</param>
    /// <returns>Constructed projection transform.</returns>
    internal static MathTransform CreateProjection(string className, IEnumerable<ProjectionParameter> parameters)
    {
        string key = ProjectionNameToRegistryKey(className);

        Type projectionType;
        Type ci;

        lock (RegistryLock)
        {
            if (!TypeRegistry.TryGetValue(key, out projectionType))
            {
                throw new NotSupportedException($"Projection {className} is not supported.");
            }

            ci = ConstructorRegistry[key];
        }

        if (!ci.IsInstanceOfType(parameters))
        {
            parameters = new List<ProjectionParameter>(parameters);
        }

        var res = (MathTransform)Activator.CreateInstance(projectionType, parameters);
        if (res is MapProjection mapProjection && !string.Equals(mapProjection.Name, className, StringComparison.OrdinalIgnoreCase))
        {
            mapProjection.Alias = mapProjection.Name;
            mapProjection.Name = className;
        }

        return res;
    }

    private static string ProjectionNameToRegistryKey(string name)
    {
        return name.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
    }

    private static Type? CheckConstructor(Type type)
    {
        // find a constructor that accepts exactly one parameter that's an
        // instance of List<ProjectionParameter>, and then return the exact
        // parameter type so that we can create instances of this type with
        // minimal copying in the future, when possible.
        foreach (var c in type.GetConstructors())
        {
            var parameters = c.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(typeof(List<ProjectionParameter>)))
            {
                return parameters[0].ParameterType;
            }
        }

        return null;
    }
}
