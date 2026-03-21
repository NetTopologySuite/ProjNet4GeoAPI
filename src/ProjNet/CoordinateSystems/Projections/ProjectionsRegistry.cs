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

namespace ProjNet.CoordinateSystems.Projections
{
    using System;
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems.Transformations;

    /// <summary>
    /// Registry class for all known <see cref="MapProjection"/>s.
    /// </summary>
    public class ProjectionsRegistry
    {
        private static readonly Dictionary<string, Type> TypeRegistry = new Dictionary<string, Type>();
        private static readonly Dictionary<string, Type> ConstructorRegistry = new Dictionary<string, Type>();

        private static readonly object RegistryLock = new object();

        /// <summary>
        /// Initializes static members of the <see cref="ProjectionsRegistry"/> class.
        /// Static constructor.
        /// </summary>
        static ProjectionsRegistry()
        {
            Register("mercator", typeof(Mercator));
            Register("mercator_1sp", typeof(Mercator));
            Register("mercator_2sp", typeof(Mercator));
            Register("mercator_(variant_a)", typeof(Mercator));
            Register("mercator_(variant_b)", typeof(Mercator));
            Register("mercator_auxiliary_sphere", typeof(MercatorAuxiliarySphere));
            Register("pseudo_mercator", typeof(PseudoMercator));
            Register("popular_visualisation_pseudo_mercator", typeof(PseudoMercator));
            Register("google_mercator", typeof(PseudoMercator));
            Register("web_mercator", typeof(PseudoMercator));
            Register("miller_cylindrical", typeof(MillerCylindricalProjection));
            Register("miller", typeof(MillerCylindricalProjection));
            Register("mill", typeof(MillerCylindricalProjection));
            Register("equidistant_cylindrical", typeof(EquidistantCylindricalProjection));
            Register("equirectangular", typeof(EquidistantCylindricalProjection));
            Register("plate_carree", typeof(EquidistantCylindricalProjection));
            Register("eqc", typeof(EquidistantCylindricalProjection));
            Register("cylindrical_equal_area", typeof(CylindricalEqualAreaProjection));
            Register("lambert_cylindrical_equal_area", typeof(CylindricalEqualAreaProjection));
            Register("equal_area_cylindrical", typeof(CylindricalEqualAreaProjection));
            Register("cea", typeof(CylindricalEqualAreaProjection));
            Register("loximuthal", typeof(LoximuthalProjection));
            Register("loxim", typeof(LoximuthalProjection));
            Register("patterson", typeof(PattersonProjection));

            Register("transverse_mercator", typeof(TransverseMercator));
            Register("transverse_mercator_south_oriented", typeof(TransverseMercator));
            Register("gauss_kruger", typeof(TransverseMercator));
            Register("utm", typeof(TransverseMercator));
            Register("etmerc", typeof(TransverseMercator));
            Register("extended_transverse_mercator", typeof(TransverseMercator));

            Register("albers", typeof(AlbersProjection));
            Register("albers_conic_equal_area", typeof(AlbersProjection));

            Register("krovak", typeof(KrovakProjection));

            Register("polyconic", typeof(PolyconicProjection));

            Register("lambert_conformal_conic", typeof(LambertConformalConic2SP));
            Register("lambert_conformal_conic_1sp", typeof(LambertConformalConic2SP));
            Register("lambert_conformal_conic_2sp", typeof(LambertConformalConic2SP));
            Register("lambert_conformal_conic_2sp_belgium", typeof(LambertConformalConic2SP));
            Register("lambert_conic_conformal_(1sp)", typeof(LambertConformalConic2SP));
            Register("lambert_conic_conformal_(2sp)", typeof(LambertConformalConic2SP));
            Register("lambert_tangential_conformal_conic_projection", typeof(LambertConformalConic2SP));
            Register("equidistant_conic", typeof(EquidistantConicProjection));
            Register("equidistant_conic_(spherical)", typeof(EquidistantConicProjection));
            Register("eqdc", typeof(EquidistantConicProjection));
            Register("bonne", typeof(BonneProjection));
            Register("perspective_conic", typeof(PconicProjection));
            Register("pconic", typeof(PconicProjection));

            Register("lambert_azimuthal_equal_area", typeof(LambertAzimuthalEqualAreaProjection));

            Register("cassini_soldner", typeof(CassiniSoldnerProjection));
            Register("hotine_oblique_mercator", typeof(HotineObliqueMercatorProjection));
            Register("hotine_oblique_mercator_azimuth_center", typeof(HotineObliqueMercatorProjection));
            Register("oblique_mercator", typeof(ObliqueMercatorProjection));
            Register("oblique_stereographic", typeof(ObliqueStereographicProjection));
            Register("orthographic", typeof(OrthographicProjection));
            Register("polar_stereographic", typeof(PolarStereographicProjection));

            Register("equal_earth", typeof(EqualEarthProjection));
            Register("eqearth", typeof(EqualEarthProjection));
            Register("hammer", typeof(HammerProjection));
            Register("sinu", typeof(SinusoidalProjection));
            Register("sinusoidal", typeof(SinusoidalProjection));
            Register("goode", typeof(GoodeProjection));
            Register("goode_homolosine", typeof(GoodeProjection));
            Register("igh", typeof(IghProjection));
            Register("interrupted_goode_homolosine", typeof(IghProjection));
            Register("healpix", typeof(HealpixProjection));

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
        /// Method to register a new Map.
        /// </summary>
        /// <param name="name">The name parameter.</param>
        /// <param name="type">The type parameter.</param>
        public static void Register(string name, Type type)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(MathTransform).IsAssignableFrom(type))
            {
                throw new ArgumentException("The provided type does not implement 'GeoAPI.CoordinateSystems.Transformations.IMathTransform'!", nameof(type));
            }

            var ci = CheckConstructor(type);
            if (ci == null)
            {
                throw new ArgumentException("The provided type is lacking a suitable constructor", nameof(type));
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

                    throw new ArgumentException("A different projection type has been registered with this name", nameof(name));
                }

                TypeRegistry.Add(key, type);
                ConstructorRegistry.Add(key, ci);
            }
        }

        /// <summary>
        /// Register an alias for an existing Map.
        /// </summary>
        /// <param name="aliasName">The aliasName parameter.</param>
        /// <param name="existingName">The existingName parameter.</param>
        public static void RegisterAlias(string aliasName, string existingName)
        {
            if (aliasName is null)
            {
                throw new ArgumentNullException(nameof(aliasName));
            }

            if (existingName is null)
            {
                throw new ArgumentNullException(nameof(existingName));
            }

            lock (RegistryLock)
            {
                if (!TypeRegistry.TryGetValue(ProjectionNameToRegistryKey(existingName), out var existingProjectionType))
                {
                    throw new ArgumentException($"{existingName} is not a registered projection type");
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

            var res = (MapProjection)Activator.CreateInstance(projectionType, parameters);
            if (!res.Name.Equals(className, StringComparison.InvariantCultureIgnoreCase))
            {
                res.Alias = res.Name;
                res.Name = className;
            }

            return res;
        }

        private static string ProjectionNameToRegistryKey(string name)
        {
            return name.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        }

        private static Type CheckConstructor(Type type)
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
}
