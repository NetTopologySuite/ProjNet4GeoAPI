using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Projections
{
    /// <summary>
    /// Implements the Mercator Auxiliary Sphere projection (Web Mercator).
    /// This projection uses a spherical model with a constant radius.
    /// </summary>
    [Serializable]
    internal class MercatorAuxiliarySphere : MapProjection
    {
        // Scale factor – for the spherical (auxiliary) Mercator this is 1.
        private const double _k0 = 1.0;

        /// <summary>
        /// Initializes the MercatorAuxiliarySphere projection with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of projection parameters.</param>
        public MercatorAuxiliarySphere(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes the MercatorAuxiliarySphere projection with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of projection parameters.</param>
        /// <param name="isInverse">Reference to the inverse projection.</param>
        protected MercatorAuxiliarySphere(IEnumerable<ProjectionParameter> parameters, MercatorAuxiliarySphere isInverse)
            : base(parameters, isInverse)
        {
            Authority = "EPSG";
            Name = "Mercator_Auxiliary_Sphere";
        }

        /// <summary>
        /// Converts geographic coordinates (in radians) to projected coordinates (in meters).
        /// </summary>
        /// <param name="lon">Longitude in radians.</param>
        /// <param name="lat">Latitude in radians.</param>
        /// <remarks>
        /// It is assumed that _semiMajor and central_meridian (as well as other parameters like false_easting/false_northing)
        /// are already set in the base class.
        /// </remarks>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            if (double.IsNaN(lon) || double.IsNaN(lat))
            {
                lon = double.NaN;
                lat = double.NaN;
                return;
            }

            double dLon = lon;
            double dLat = lat;

            if (Math.Abs(Math.Abs(dLat) - HALF_PI) <= EPSLN)
            {
                throw new ArgumentException("Transformation cannot be computed at the poles.");
            }

            // Forward equations for the Spherical (Auxiliary) Mercator Projection:
            // X = semiMajor * k0 * (lon - central_meridian)
            // Y = semiMajor * k0 * ln( tan(PI/4 + lat/2) )
            lon = _semiMajor * _k0 * (dLon - central_meridian);
            lat = _semiMajor * _k0 * Math.Log(Math.Tan((PI * 0.25) + (dLat * 0.5)));
            // Note: false_easting and false_northing can be added here if necessary.
        }

        /// <summary>
        /// Converts projected coordinates (in meters) to geographic coordinates (in radians).
        /// </summary>
        /// <param name="x">X coordinate in meters.</param>
        /// <param name="y">Y coordinate in meters.</param>
        /// <remarks>
        /// Uses the inverse transformation of the Spherical Mercator Projection.
        /// </remarks>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            double dX = x;
            double dY = y;

            // Inverse equations:
            // lon = central_meridian + X / (semiMajor * k0)
            // lat = PI/2 - 2 * atan( exp( -Y / (semiMajor * k0) ) )
            double ts = Math.Exp(-dY / (_semiMajor * _k0));
            double dLat = HALF_PI - (2 * Math.Atan(ts));
            double dLon = central_meridian + (dX / (_semiMajor * _k0));

            x = dLon;
            y = dLat;
            // Note: false_easting/false_northing can be subtracted here if provided in the parameter list.
        }

        /// <summary>
        /// Returns the inverse transformation of this projection.
        /// </summary>
        /// <returns>The inverse projection as MathTransform.</returns>
        public override MathTransform Inverse()
        {
            if (_inverse is null)
            {
                _inverse = new MercatorAuxiliarySphere(_Parameters.ToProjectionParameter(), this);
            }
            return _inverse;
        }
    }
}
