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
    using ProjNet.CoordinateSystems.Transformations;
    using System;
    using System.Collections.Generic;

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
        /// <param name="name"></param>
        /// <param name="type"></param>
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
                if (TypeRegistry.ContainsKey(key))
                {
                    var rt = TypeRegistry[key];
                    if (ReferenceEquals(type, rt))
                    {
                        return;
                    }

                    throw new ArgumentException("A different projection type has been registered with this name", "name");
                }

                TypeRegistry.Add(key, type);
                ConstructorRegistry.Add(key, ci);
            }
        }

        private static string ProjectionNameToRegistryKey(string name)
        {
            return name.ToLowerInvariant().Replace(' ', '_').Replace("-", "_");
        }

        /// <summary>
        /// Register an alias for an existing Map.
        /// </summary>
        /// <param name="aliasName"></param>
        /// <param name="existingName"></param>
        public static void RegisterAlias(string aliasName, string existingName)
        {
            lock (RegistryLock)
            {
                if (!TypeRegistry.TryGetValue(ProjectionNameToRegistryKey(existingName), out var existingProjectionType))
                {
                    throw new ArgumentException($"{existingName} is not a registered projection type");
                }

                Register(aliasName, existingProjectionType);
            }
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
    }
}

