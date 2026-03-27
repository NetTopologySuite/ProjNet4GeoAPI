using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems;

namespace ProjNet.CoordinateSystems.Wkt2
{
    /// <summary>
    /// Conversion helpers between the WKT2 model types and the existing ProjNet coordinate system model.
    /// </summary>
    public static class Wkt2Conversions
    {
        /// <summary>
        /// Converts a WKT2 projected CRS model to a ProjNet <see cref="ProjectedCoordinateSystem"/>.
        /// </summary>
        /// <param name="crs">The WKT2 projected CRS.</param>
        /// <returns>A ProjNet projected coordinate system.</returns>
        public static ProjectedCoordinateSystem ToProjNetProjectedCoordinateSystem(this Wkt2ProjCrs crs)
        {
            if (crs == null) throw new ArgumentNullException(nameof(crs));

            var baseGcs = crs.BaseCrs.ToProjNetGeographicCoordinateSystem();

            // Projection method mapping is best-effort; WKT2 method names vary.
            string method = MapProjectionMethodName(crs.Conversion.MethodName);

            var parameters = new List<ProjectionParameter>();
            foreach (var p in crs.Conversion.Parameters)
                parameters.Add(new ProjectionParameter(p.Name, p.Value));

            var projection = new Projection(method, parameters, crs.Conversion.Name, string.Empty, -1, string.Empty, string.Empty, string.Empty);

            var linearUnit = LinearUnit.Metre;
            if (crs.CoordinateSystem != null && crs.CoordinateSystem.Unit != null)
            {
                // ProjNet `LinearUnit` expects meters per unit.
                // WKT2 LENGTHUNIT factor is in meters per unit.
                linearUnit = new LinearUnit(crs.CoordinateSystem.Unit.ConversionFactor, crs.CoordinateSystem.Unit.Name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
            }

            var axes = new List<AxisInfo>(2)
            {
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North)
            };

            // Best-effort for IDs
            string authority = crs.Id != null ? crs.Id.Authority : string.Empty;
            long authorityCode = -1;
            if (crs.Id != null)
                long.TryParse(crs.Id.Code, out authorityCode);

            return new ProjectedCoordinateSystem(baseGcs.HorizontalDatum, baseGcs, linearUnit, projection, axes,
                crs.Name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Converts a ProjNet <see cref="ProjectedCoordinateSystem"/> to a WKT2 projected CRS model.
        /// </summary>
        /// <param name="pcs">The ProjNet projected coordinate system.</param>
        /// <returns>A WKT2 projected CRS model.</returns>
        public static Wkt2ProjCrs FromProjNetProjectedCoordinateSystem(this ProjectedCoordinateSystem pcs)
        {
            if (pcs == null) throw new ArgumentNullException(nameof(pcs));
            if (pcs.GeographicCoordinateSystem == null)
                throw new ArgumentException("ProjectedCoordinateSystem.GeographicCoordinateSystem cannot be null.", nameof(pcs));
            if (pcs.Projection == null)
                throw new ArgumentException("ProjectedCoordinateSystem.Projection cannot be null.", nameof(pcs));

            var baseCrs = pcs.GeographicCoordinateSystem.FromProjNetGeographicCoordinateSystem();

            var conversion = new Wkt2Conversion(pcs.Projection.Name, MapProjNetToWkt2MethodName(pcs.Projection.ClassName));
            for (int i = 0; i < pcs.Projection.NumParameters; i++)
            {
                var p = pcs.Projection.GetParameter(i);
                conversion.Parameters.Add(new Wkt2Parameter(p.Name, p.Value));
            }

            var unit = new Wkt2Unit("LENGTHUNIT", pcs.LinearUnit.Name, pcs.LinearUnit.MetersPerUnit);
            var cs = new Wkt2CoordinateSystem("cartesian", 2) { Unit = unit };
            cs.Axes.Add(new Wkt2Axis("easting", "east") { Order = 1 });
            cs.Axes.Add(new Wkt2Axis("northing", "north") { Order = 2 });

            var crs = new Wkt2ProjCrs("PROJCRS", pcs.Name, baseCrs, conversion, cs);
            if (!string.IsNullOrWhiteSpace(pcs.Authority) && pcs.AuthorityCode > 0)
                crs.Id = new Wkt2Id(pcs.Authority, pcs.AuthorityCode.ToString());

            return crs;
        }

        /// <summary>
        /// Converts a WKT2 geographic CRS model to a ProjNet <see cref="GeographicCoordinateSystem"/>.
        /// </summary>
        /// <param name="crs">The WKT2 geographic CRS.</param>
        /// <returns>A ProjNet geographic coordinate system.</returns>
        public static GeographicCoordinateSystem ToProjNetGeographicCoordinateSystem(this Wkt2GeogCrs crs)
        {
            if (crs == null) throw new ArgumentNullException(nameof(crs));

            // Normalize WKT2 -> ProjNet conventions:
            // - ProjNet GCS is horizontal (2D)
            // - ProjNet expects Lon/East then Lat/North axis order
            // - Per-axis units are not supported; use CS unit (ANGLEUNIT) if available

            var ellipsoid = new Ellipsoid(
                crs.Datum.Ellipsoid.SemiMajorAxis,
                0.0,
                crs.Datum.Ellipsoid.InverseFlattening,
                true,
                LinearUnit.Metre,
                crs.Datum.Ellipsoid.Name,
                string.Empty,
                -1,
                string.Empty,
                string.Empty,
                string.Empty);

            var datum = new HorizontalDatum(
                ellipsoid,
                null,
                DatumType.HD_Geocentric,
                crs.Datum.Name,
                string.Empty,
                -1,
                string.Empty,
                string.Empty,
                string.Empty);

            var angUnit = AngularUnit.Degrees;
            if (crs.CoordinateSystem != null && crs.CoordinateSystem.Unit != null)
            {
                angUnit = new AngularUnit(
                    crs.CoordinateSystem.Unit.ConversionFactor,
                    crs.CoordinateSystem.Unit.Name,
                    string.Empty,
                    -1,
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }

            PrimeMeridian pm;
            if (crs.PrimeMeridian != null)
            {
                pm = new PrimeMeridian(crs.PrimeMeridian.Longitude, angUnit, crs.PrimeMeridian.Name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
            }
            else
            {
                pm = new PrimeMeridian(0.0, angUnit, "Greenwich", string.Empty, -1, string.Empty, string.Empty, string.Empty);
            }

            var axes = new List<AxisInfo>(2)
            {
                new AxisInfo("Lon", AxisOrientationEnum.East),
                new AxisInfo("Lat", AxisOrientationEnum.North)
            };

            // Best-effort for IDs
            string authority = crs.Id != null ? crs.Id.Authority : string.Empty;
            long authorityCode = -1;
            if (crs.Id != null)
                long.TryParse(crs.Id.Code, out authorityCode);

            return new GeographicCoordinateSystem(
                angUnit,
                datum,
                pm,
                axes,
                crs.Name,
                authority,
                authorityCode,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        /// <summary>
        /// Converts a ProjNet <see cref="GeographicCoordinateSystem"/> to a WKT2 geographic CRS model.
        /// </summary>
        /// <param name="gcs">The ProjNet geographic coordinate system.</param>
        /// <returns>A WKT2 geographic CRS model.</returns>
        public static Wkt2GeogCrs FromProjNetGeographicCoordinateSystem(this GeographicCoordinateSystem gcs)
        {
            if (gcs == null) throw new ArgumentNullException(nameof(gcs));
            if (gcs.HorizontalDatum?.Ellipsoid == null)
                throw new ArgumentException("GeographicCoordinateSystem.HorizontalDatum.Ellipsoid cannot be null.", nameof(gcs));

            var unit = new Wkt2Unit("ANGLEUNIT", gcs.AngularUnit.Name, gcs.AngularUnit.RadiansPerUnit);

            var cs = new Wkt2CoordinateSystem("ellipsoidal", 2)
            {
                Unit = unit
            };
            cs.Axes.Add(new Wkt2Axis("longitude", "east") { Order = 1 });
            cs.Axes.Add(new Wkt2Axis("latitude", "north") { Order = 2 });

            var ellipsoid = new Wkt2Ellipsoid(gcs.HorizontalDatum.Ellipsoid.Name, gcs.HorizontalDatum.Ellipsoid.SemiMajorAxis, gcs.HorizontalDatum.Ellipsoid.InverseFlattening)
            {
                LengthUnit = new Wkt2Unit("LENGTHUNIT", "metre", 1.0)
            };

            var datum = new Wkt2GeodeticDatum("DATUM", gcs.HorizontalDatum.Name, ellipsoid);

            var crs = new Wkt2GeogCrs("GEOGCRS", gcs.Name, datum, cs)
            {
                PrimeMeridian = new Wkt2PrimeMeridian(gcs.PrimeMeridian.Name, gcs.PrimeMeridian.Longitude) { AngleUnit = unit }
            };

            if (!string.IsNullOrWhiteSpace(gcs.Authority) && gcs.AuthorityCode > 0)
                crs.Id = new Wkt2Id(gcs.Authority, gcs.AuthorityCode.ToString());

            return crs;
        }

        private static readonly Dictionary<string, string> Wkt2ToProjNetMethodMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Transverse Mercator variants
            { "Transverse Mercator", "Transverse_Mercator" },
            { "Transverse Mercator (South Orientated)", "Transverse_Mercator" },
            { "Gauss-Kruger", "Transverse_Mercator" },

            // Mercator variants
            { "Mercator", "Mercator_1SP" },
            { "Mercator (variant A)", "Mercator_1SP" },
            { "Mercator (1SP)", "Mercator_1SP" },
            { "Mercator (variant B)", "Mercator_2SP" },
            { "Mercator (2SP)", "Mercator_2SP" },
            { "Mercator Auxiliary Sphere", "Mercator_Auxiliary_Sphere" },
            { "Popular Visualisation Pseudo Mercator", "Popular_Visualisation_Pseudo_Mercator" },

            // Lambert variants
            { "Lambert Conic Conformal (2SP)", "lambert_conformal_conic_2sp" },
            { "Lambert Conic Conformal (1SP)", "Lambert_Conformal_Conic" },
            { "Lambert Azimuthal Equal Area", "Lambert_Azimuthal_Equal_Area" },

            // Albers
            { "Albers Equal Area", "Albers_Conic_Equal_Area" },
            { "Albers", "Albers" },

            // Stereographic variants
            { "Oblique Stereographic", "Oblique_Stereographic" },
            { "Polar Stereographic (variant A)", "Polar_Stereographic" },
            { "Polar Stereographic (variant B)", "Polar_Stereographic" },
            { "Polar Stereographic", "Polar_Stereographic" },

            // Other projections
            { "Hotine Oblique Mercator (variant A)", "Hotine_Oblique_Mercator" },
            { "Hotine Oblique Mercator (variant B)", "Hotine_Oblique_Mercator" },
            { "Hotine Oblique Mercator", "Hotine_Oblique_Mercator" },
            { "Oblique Mercator", "Oblique_Mercator" },
            { "Cassini-Soldner", "Cassini_Soldner" },
            { "Krovak", "Krovak" },
            { "Krovak (North Orientated)", "Krovak" },
            { "American Polyconic", "Polyconic" },
            { "Polyconic", "Polyconic" },
            { "Orthographic", "Orthographic" },
        };

        internal static string MapProjectionMethodName(string wkt2Method)
        {
            if (string.IsNullOrWhiteSpace(wkt2Method))
                return "";

            string m = wkt2Method.Trim();
            return Wkt2ToProjNetMethodMap.TryGetValue(m, out string projNetName)
                ? projNetName
                : m;
        }

        private static readonly Dictionary<string, string> ProjNetToWkt2MethodMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Transverse_Mercator", "Transverse Mercator" },
            { "Mercator_1SP", "Mercator (variant A)" },
            { "Mercator_2SP", "Mercator (variant B)" },
            { "Mercator", "Mercator (variant A)" },
            { "Mercator_Auxiliary_Sphere", "Mercator Auxiliary Sphere" },
            { "Popular_Visualisation_Pseudo_Mercator", "Popular Visualisation Pseudo Mercator" },
            { "Pseudo_Mercator", "Popular Visualisation Pseudo Mercator" },
            { "Google_Mercator", "Popular Visualisation Pseudo Mercator" },
            { "lambert_conformal_conic_2sp", "Lambert Conic Conformal (2SP)" },
            { "Lambert_Conformal_Conic", "Lambert Conic Conformal (1SP)" },
            { "Lambert_Conic_Conformal_(2SP)", "Lambert Conic Conformal (2SP)" },
            { "Lambert_Azimuthal_Equal_Area", "Lambert Azimuthal Equal Area" },
            { "Albers_Conic_Equal_Area", "Albers Equal Area" },
            { "Albers", "Albers Equal Area" },
            { "Oblique_Stereographic", "Oblique Stereographic" },
            { "Polar_Stereographic", "Polar Stereographic (variant A)" },
            { "Hotine_Oblique_Mercator", "Hotine Oblique Mercator (variant A)" },
            { "Hotine_Oblique_Mercator_Azimuth_Center", "Hotine Oblique Mercator (variant A)" },
            { "Oblique_Mercator", "Oblique Mercator" },
            { "Cassini_Soldner", "Cassini-Soldner" },
            { "Krovak", "Krovak" },
            { "Polyconic", "American Polyconic" },
            { "Orthographic", "Orthographic" },
            { "Gauss_Kruger", "Transverse Mercator" },
        };

        internal static string MapProjNetToWkt2MethodName(string projNetMethod)
        {
            if (string.IsNullOrWhiteSpace(projNetMethod))
                return "";

            string m = projNetMethod.Trim();
            return ProjNetToWkt2MethodMap.TryGetValue(m, out string wkt2Name)
                ? wkt2Name
                : m;
        }
    }
}
