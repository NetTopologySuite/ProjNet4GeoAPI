// Copyright 2008
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

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

/*
 *  Copyright (C) 2002 Urban Science Applications, Inc. 
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems.Projections
{

	/// <summary>
	/// Implemetns the Krovak Projection.
	/// </summary>
	/// <remarks>
	/// <para>The normal case of the Lambert Conformal conic is for the axis of the cone 
	/// to be coincident with the minor axis of the ellipsoid, that is the axis of the cone 
	/// is normal to the ellipsoid at a pole. For the Oblique Conformal Conic the axis 
	/// of the cone is normal to the ellipsoid at a defined location and its extension 
	/// cuts the minor axis at a defined angle. This projection is used in the Czech Republic 
	/// and Slovakia under the name "Krovak" projection.</para>
    /// </remarks>
    [Serializable] 
    internal class KrovakProjection : MapProjection
	{
		/**
		 * Maximum number of iterations for iterative computations.
		 */
		private const int MaximumIterations = 15;
	    
		/**
		 * When to stop the iteration.
		 */
		private const double IterationTolerance = 1E-11;

		/**
		 * Azimuth of the centre line passing through the centre of the projection.
		 * This is equals to the co-latitude of the cone axis at point of intersection
		 * with the ellipsoid.
		 */
		private readonly double _azimuth;

		/**
		 * Latitude of pseudo standard parallel.
		 */
		private readonly double _pseudoStandardParallel;

		/**
		 * Useful variables calculated from parameters defined by user.
		 */
		private readonly double _sinAzim, _cosAzim, _n, _tanS2, _alfa, _hae, _k1, _ka, _ro0, _rop;

        private readonly double _reciprocSemiMajor;

		/**
		 * Useful constant - 45° in radians.
		 */
		private const double S45 = 0.785398163397448;

		#region Constructors

		/// <summary>
		/// Creates an instance of an LambertConformalConic2SPProjection projection object.
		/// </summary>
		/// <remarks>
		/// <para>The parameters this projection expects are listed below.</para>
		/// <list type="table">
		/// <listheader><term>Items</term><description>Descriptions</description></listheader>
		/// <item><term>latitude_of_false_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>longitude_of_false_origin</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>latitude_of_1st_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>latitude_of_2nd_standard_parallel</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>easting_at_false_origin</term><description>The easting value assigned to the false origin.</description></item>
		/// <item><term>northing_at_false_origin</term><description>The northing value assigned to the false origin.</description></item>
		/// </list>
		/// </remarks>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
        public KrovakProjection(IEnumerable<ProjectionParameter> parameters) 
            : this(parameters,null)
		{
		}
	
		/// <summary>
		/// Creates an instance of an Krovak projection object.
		/// </summary>
		/// <remarks>
		/// <para>The parameters this projection expects are listed below.</para>
		/// <list type="table">
		/// <listheader><term>Parameter</term><description>Description</description></listheader>
		/// <item><term>latitude_of_origin</term><description>The latitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>central_meridian</term><description>The longitude of the point which is not the natural origin and at which grid coordinate values false easting and false northing are defined.</description></item>
		/// <item><term>standard_parallel_1</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is nearest the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>standard_parallel_2</term><description>For a conic projection with two standard parallels, this is the latitude of intersection of the cone with the ellipsoid that is furthest from the pole.  Scale is true along this parallel.</description></item>
		/// <item><term>false_easting</term><description>The easting value assigned to the false origin.</description></item>
		/// <item><term>false_northing</term><description>The northing value assigned to the false origin.</description></item>
		/// </list>
		/// </remarks>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		/// <param name="inverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
		protected KrovakProjection(IEnumerable<ProjectionParameter> parameters, KrovakProjection inverse)
			: base(parameters, inverse)
		{
			Name = "Krovak";

            Authority = "EPSG";
			AuthorityCode = 9819;

	        //PROJCS["S-JTSK (Ferro) / Krovak",
	        //GEOGCS["S-JTSK (Ferro)",
	        //    DATUM["D_S_JTSK_Ferro",
	        //        SPHEROID["Bessel 1841",6377397.155,299.1528128]],
	        //    PRIMEM["Ferro",-17.66666666666667],
	        //    UNIT["degree",0.0174532925199433]],
	        //PROJECTION["Krovak"],
	        //PARAMETER["latitude_of_center",49.5],
	        //PARAMETER["longitude_of_center",42.5],
	        //PARAMETER["azimuth",30.28813972222222],
	        //PARAMETER["pseudo_standard_parallel_1",78.5],
	        //PARAMETER["scale_factor",0.9999],
	        //PARAMETER["false_easting",0],
	        //PARAMETER["false_northing",0],
	        //UNIT["metre",1]]

			//Check for missing parameters
            _azimuth = DegreesToRadians(_Parameters.GetParameterValue("azimuth"));
			_pseudoStandardParallel = DegreesToRadians(_Parameters.GetParameterValue("pseudo_standard_parallel_1"));
            
            // Calculates useful constants.
			_sinAzim = Math.Sin(_azimuth);
			_cosAzim = Math.Cos(_azimuth);
			_n       = Math.Sin(_pseudoStandardParallel);
			_tanS2   = Math.Tan(_pseudoStandardParallel / 2 + S45);

            double sinLat = Math.Sin(lat_origin);
            double cosLat = Math.Cos(lat_origin);
            double cosL2 = cosLat * cosLat;
            _alfa = Math.Sqrt(1 + ((_es * (cosL2 * cosL2)) / (1 - _es))); // parameter B
			_hae    = _alfa * _e / 2;
            double u0 = Math.Asin(sinLat / _alfa);

            double esl = _e * sinLat;
            double g = Math.Pow((1 - esl) / (1 + esl), (_alfa * _e) / 2);
            _k1 = Math.Pow(Math.Tan(lat_origin / 2 + S45), _alfa) * g / Math.Tan(u0 / 2 + S45);
			_ka  = Math.Pow(1 / _k1, -1 / _alfa);

            double radius = Math.Sqrt(1 - _es) / (1 - (_es * (sinLat * sinLat)));

			_ro0 = scale_factor * radius / Math.Tan(_pseudoStandardParallel);
			_rop = _ro0 * Math.Pow(_tanS2, _n);

            _reciprocSemiMajor = 1 / _semiMajor;
        }
        #endregion

        /// <summary>
        /// Converts coordinates in radians to projected meters.
        /// </summary>
        /// <param name="lon"></param>
        /// <param name="lat"></param>
        protected override void RadiansToMeters(ref double lon, ref double lat)
		{
            double lambda = lon - central_meridian;
            double phi = lat;

            double esp = _e * Math.Sin(phi);
            double gfi = Math.Pow(((1.0 - esp) / (1.0 + esp)), _hae);
            double u = 2 * (Math.Atan(Math.Pow(Math.Tan(phi / 2 + S45), _alfa) / _k1 * gfi) - S45);
            double deltav = -lambda * _alfa;
            double cosU = Math.Cos(u);
            double s = Math.Asin((_cosAzim * Math.Sin(u)) + (_sinAzim * cosU * Math.Cos(deltav)));
            double d = Math.Asin(cosU * Math.Sin(deltav) / Math.Cos(s));
            double eps = _n * d;
            double ro = _rop / Math.Pow(Math.Tan(s / 2 + S45), _n);

            /* x and y are reverted  */
            lat = -(ro * Math.Cos(eps)) * _semiMajor;
            lon = -(ro * Math.Sin(eps)) * _semiMajor;
		}

        /// <summary>
        /// Converts coordinates in projected meters to radians.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            x *= _reciprocSemiMajor;
            y *= _reciprocSemiMajor;

            // x -> southing, y -> westing
            double ro = Math.Sqrt(x * x + y * y);
            double eps = Math.Atan2(-x, -y);
            double d = eps / _n;
            double s = 2 * (Math.Atan(Math.Pow(_ro0 / ro, 1 / _n) * _tanS2) - S45);
            double cs = Math.Cos(s);
            double u = Math.Asin((_cosAzim * Math.Sin(s)) - (_sinAzim * cs * Math.Cos(d)));
            double kau = _ka * Math.Pow(Math.Tan((u / 2.0) + S45), 1 / _alfa);
            double deltav = Math.Asin((cs * Math.Sin(d)) / Math.Cos(u));
            double lambda = -deltav / _alfa;
            double phi = 0d;

            // iteration calculation
            for (int iter = MaximumIterations;;)
            {
                double fi1 = phi;
                double esf = _e * Math.Sin(fi1);
                phi = 2.0 * (Math.Atan(kau * Math.Pow((1.0 + esf) / (1.0 - esf), _e / 2.0)) - S45);
                if (Math.Abs(fi1 - phi) <= IterationTolerance)
                {
                    break;
                }

                if (--iter < 0)
                {
                    break;
                    //throw new ProjectionException(Errors.format(ErrorKeys.NO_CONVERGENCE));
                }
            }

            x = lambda + central_meridian;
            y = phi;
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override MathTransform Inverse()
		{
			if (_inverse == null)
			{
				_inverse = new KrovakProjection(_Parameters.ToProjectionParameter(), this);
			}

			return _inverse;
		}
	}
}
