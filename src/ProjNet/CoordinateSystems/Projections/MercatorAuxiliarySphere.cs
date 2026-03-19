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
    /// Implements the Mercator Auxiliary Sphere projection (Web Mercator).
    /// This projection uses a spherical model with a constant radius.
    /// </summary>
    [Serializable]
    internal class MercatorAuxiliarySphere : MapProjection
    {
        // Scale factor – for the spherical (auxiliary) Mercator this is 1.
        private const double k0 = 1.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="MercatorAuxiliarySphere"/> class.
        /// Initializes the MercatorAuxiliarySphere projection with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of projection parameters.</param>
        public MercatorAuxiliarySphere(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MercatorAuxiliarySphere"/> class.
        /// Initializes the MercatorAuxiliarySphere projection with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of projection parameters.</param>
        /// <param name="isInverse">Reference to the inverse projection.</param>
        protected MercatorAuxiliarySphere(IEnumerable<ProjectionParameter> parameters, MercatorAuxiliarySphere isInverse)
            : base(parameters, isInverse)
        {
            this.Authority = "EPSG";
            this.Name = "Mercator_Auxiliary_Sphere";
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

            if (Math.Abs(Math.Abs(dLat) - HALFPI) <= EPSLN)
            {
                throw new ArgumentException("Transformation cannot be computed at the poles.");
            }

            // Forward equations for the Spherical (Auxiliary) Mercator Projection:
            // X = semiMajor * k0 * (lon - central_meridian)
            // Y = semiMajor * k0 * ln( tan(PI/4 + lat/2) )
            lon = this.semiMajor * k0 * (dLon - this.centralMeridian);
            lat = this.semiMajor * k0 * Math.Log(Math.Tan((PI * 0.25) + (dLat * 0.5)));

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
            double ts = Math.Exp(-dY / (this.semiMajor * k0));
            double dLat = HALFPI - (2 * Math.Atan(ts));
            double dLon = this.centralMeridian + (dX / (this.semiMajor * k0));

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
            if (this.inverse is null)
            {
                this.inverse = new MercatorAuxiliarySphere(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }
    }
}
