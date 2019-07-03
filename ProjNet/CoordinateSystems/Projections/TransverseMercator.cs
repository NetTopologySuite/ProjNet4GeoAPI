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

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

// SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
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
	/// Summary description for MathTransform.
	/// </summary>
	/// <remarks>
	/// <para>Universal (UTM) and Modified (MTM) Transverses Mercator projections. This
	/// is a cylindrical projection, in which the cylinder has been rotated 90°.
	/// Instead of being tangent to the equator (or to an other standard latitude),
	/// it is tangent to a central meridian. Deformation are more important as we
	/// are going further from the central meridian. The Transverse Mercator
	/// projection is appropriate for region witch have a greater extent north-south
	/// than east-west.</para>
	/// 
	/// <para>Reference: John P. Snyder (Map Projections - A Working Manual,
	///            U.S. Geological Survey Professional Paper 1395, 1987)</para>
    /// </remarks>
    [Serializable] 
    internal class TransverseMercator : MapProjection
	{
    // /* 
    //  * Maximum number of iterations for iterative computations.
    //  */
    // private const int MAXIMUM_ITERATIONS = 15;

    // /*
    //  * Relative iteration precision used in the {@code mlfn} method.
    //  * This overrides the value in the {@link MapProjection} class.
    //  */
    // private const double ITERATION_TOLERANCE = 1E-11;

    /*
     * Maximum difference allowed when comparing real numbers.
     */
    private const double EPSILON = 1E-6;

    // /*
    //  * Maximum difference allowed when comparing latitudes.
    //  */
    // private const double EPSILON_LATITUDE = 1E-10;

    /*
     * A derived quantity of eccentricity, computed by <code>e'Â² = (aÂ²-bÂ²)/bÂ² = es/(1-es)</code>
     * where <c>a</c> is the semi-major axis length and <c>b</c> is the semi-minor axis
     * length.
     */
    private readonly double _esp;

    /*
     * Meridian distance at the {@code latitudeOfOrigin}.
     * Used for calculations for the ellipsoid.
     */
    private readonly double _ml0;

    private readonly double _reciprocSemiMajor;

    /* 
     * Constants used for the forward and inverse transform for the elliptical
     * case of the Transverse Mercator.
     */
    private const double FC1= 1.00000000000000000000000,  // 1/1
                         FC2= 0.50000000000000000000000,  // 1/2
                         FC3= 0.16666666666666666666666,  // 1/6
                         FC4= 0.08333333333333333333333,  // 1/12
                         FC5= 0.05000000000000000000000,  // 1/20
                         FC6= 0.03333333333333333333333,  // 1/30
                         FC7= 0.02380952380952380952380,  // 1/42
                         FC8= 0.01785714285714285714285;  // 1/56



        // // Variables common to all subroutines in this code file
        // // -----------------------------------------------------
        // private double esp;		/* eccentricity constants       */
        // private double ml0;		/* small value m			    */

		/// <summary>
		/// Creates an instance of an TransverseMercatorProjection projection object.
		/// </summary>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		public TransverseMercator(IEnumerable<ProjectionParameter> parameters)
			: this(parameters, null)
		{
			
		}
		/// <summary>
		/// Creates an instance of an TransverseMercatorProjection projection object.
		/// </summary>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		/// <param name="inverse">Flag indicating wether is a forward/projection (false) or an inverse projection (true).</param>
		/// <remarks>
		/// <list type="bullet">
		/// <listheader><term>Items</term><description>Descriptions</description></listheader>
		/// <item><term>semi_major</term><description>Semi major radius</description></item>
		/// <item><term>semi_minor</term><description>Semi minor radius</description></item>
		/// <item><term>scale_factor</term><description></description></item>
		/// <item><term>central meridian</term><description></description></item>
		/// <item><term>latitude_origin</term><description></description></item>
		/// <item><term>false_easting</term><description></description></item>
		/// <item><term>false_northing</term><description></description></item>
		/// </list>
		/// </remarks>
		protected TransverseMercator(IEnumerable<ProjectionParameter> parameters, TransverseMercator inverse)
			: base(parameters, inverse)
		{
			Name = "Transverse_Mercator";
			Authority = "EPSG";
			AuthorityCode = 9807;

            _esp = _es / (1.0 - _es);
            _ml0 = mlfn(lat_origin, Math.Sin(lat_origin), Math.Cos(lat_origin));

            /*
			e = Math.Sqrt(_es);
		    ml0 = _semiMajor*mlfn(lat_origin, Math.Sin(lat_origin), Math.Cos(lat_origin));
			esp = _es / (1.0 - _es);
             */

            _reciprocSemiMajor = 1 / _semiMajor;
        }

        /// <summary>
        /// Converts coordinates in radians to projected meters.
        /// </summary>
        /// <param name="lon">The longitude of the point in radians.</param>
        /// <param name="lat">The latitude of the point in radians.</param>
        /// <returns>Point in projected meters</returns>
        protected override void RadiansToMeters(ref double lon, ref double lat)
		{
            double x = lon;
		    x = adjust_lon(x - central_meridian);

            double y = lat;
            double sinphi = Math.Sin(y);
            double cosphi = Math.Cos(y);

            double t = (Math.Abs(cosphi) > EPSILON) ? sinphi / cosphi : 0;
            t *= t;
            double al = cosphi * x;
            double als = al * al;
            al /= Math.Sqrt(1.0 - _es * sinphi * sinphi);
            double n = _esp * cosphi * cosphi;

            /* NOTE: meridinal distance at latitudeOfOrigin is always 0 */
            y = (mlfn(y, sinphi, cosphi) - _ml0 +
                sinphi * al * x *
                FC2 * (1.0 +
                FC4 * als * (5.0 - t + n * (9.0 + 4.0 * n) +
                FC6 * als * (61.0 + t * (t - 58.0) + n * (270.0 - 330.0 * t) +
                FC8 * als * (1385.0 + t * (t * (543.0 - t) - 3111.0))))));

            x = al * (FC1 + FC3 * als * (1.0 - t + n +
                FC5 * als * (5.0 + t * (t - 18.0) + n * (14.0 - 58.0 * t) +
                FC7 * als * (61.0 + t * (t * (179.0 - t) - 479.0)))));

		    lon = scale_factor*_semiMajor*x;
		    lat = scale_factor*_semiMajor*y;
		}

        /// <summary>
        /// Converts coordinates in projected meters to radians.
        /// </summary>
        /// <param name="x">The x-ordinate of the point</param>
        /// <param name="y">The y-ordinate of the point</param>
        /// <returns>Transformed point in decimal degrees</returns>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            x *= _reciprocSemiMajor;
            y *= _reciprocSemiMajor;

            double phi = inv_mlfn(_ml0 + y / scale_factor);

            if (Math.Abs(phi) >= PI / 2)
            {
                y = y < 0.0 ? -(PI / 2) : (PI / 2);
                x = 0.0;
            }
            else
            {
                double sinphi = Math.Sin(phi);
                double cosphi = Math.Cos(phi);
                double t = (Math.Abs(cosphi) > EPSILON) ? sinphi / cosphi : 0.0;
                double n = _esp * cosphi * cosphi;
                double con = 1.0 - _es * sinphi * sinphi;
                double d = x * Math.Sqrt(con) / scale_factor;
                con *= t;
                t *= t;
                double ds = d * d;

                y = phi - (con * ds / (1.0 - _es)) *
                    FC2 * (1.0 - ds *
                    FC4 * (5.0 + t * (3.0 - 9.0 * n) + n * (1.0 - 4 * n) - ds *
                    FC6 * (61.0 + t * (90.0 - 252.0 * n + 45.0 * t) + 46.0 * n - ds *
                    FC8 * (1385.0 + t * (3633.0 + t * (4095.0 + 1574.0 * t))))));

                x = adjust_lon(central_meridian + d * (FC1 - ds * FC3 * (1.0 + 2.0 * t + n -
                    ds * FC5 * (5.0 + t * (28.0 + 24 * t + 8.0 * n) + 6.0 * n -
                    ds * FC7 * (61.0 + t * (662.0 + t * (1320.0 + 720.0 * t)))))) / cosphi);
            }
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override MathTransform Inverse()
		{
			if (_inverse==null)
				_inverse = new TransverseMercator(_Parameters.ToProjectionParameter(), this);
			return _inverse;
		}
	}
}
