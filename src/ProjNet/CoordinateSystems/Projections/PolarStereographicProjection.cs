// Copyright 2015
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
    /// Implements the Polar Stereographic Projection.
    /// </summary>
    [Serializable]
    internal class PolarStereographicProjection : MapProjection
    {
        private readonly double _globalScale;
        private readonly double _reciprocGlobalScale;

        private static int MAXIMUM_ITERATIONS = 15;
        private static double ITERATION_TOLERANCE = 1E-14;
        private static double EPS15 = 1E-15;
        private static double M_HALFPI = 0.5 * Math.PI;
        private double phits, akm1;
        private bool N_POLE;


        /// <summary>
        /// Initializes the PolarStereographicProjection object with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>central_meridian</term><description>The longitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the longitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>latitude_of_origin</term><description>The latitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the latitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>scale_factor</term><description>The factor by which the map grid is reduced or enlarged during the projection process, defined by its value at the natural origin.</description></item>
        /// <item><term>false_easting</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Easting, FE, is the easting value assigned to the abscissa (east).</description></item>
        /// <item><term>false_northing</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Northing, FN, is the northing value assigned to the ordinate.</description></item>
        /// </list>
        /// </remarks>
        public PolarStereographicProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes the PolarStereographicProjection object with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="inverse">Inverse projection</param>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>central_meridian</term><description>The longitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the longitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>latitude_of_origin</term><description>The latitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the latitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>scale_factor</term><description>The factor by which the map grid is reduced or enlarged during the projection process, defined by its value at the natural origin.</description></item>
        /// <item><term>false_easting</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Easting, FE, is the easting value assigned to the abscissa (east).</description></item>
        /// <item><term>false_northing</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Northing, FN, is the northing value assigned to the ordinate.</description></item>
        /// </list>
        /// </remarks>
        public PolarStereographicProjection(IEnumerable<ProjectionParameter> parameters, PolarStereographicProjection inverse)
            : base(parameters, inverse)
        {
            Name = "Polar_Stereographic";

            _globalScale = scale_factor * _semiMajor;
            _reciprocGlobalScale = 1.0 / _globalScale;

            if (_e == 0.0) throw new Exception("Polar Stereographics: only ellipsoidal formulation");
            N_POLE = (lat_origin > 0.0); // N or S hemisphere
            phits = Math.Abs(lat_origin);

            if (Math.Abs(phits - M_HALFPI) < EPS10)
            {
                double one_p_e = 1.0 + _e;
                double one_m_e = 1.0 - _e;
                double pow_p = Math.Pow(one_p_e, one_p_e);
                double pow_m = Math.Pow(one_m_e, one_m_e);
                akm1 = 2.0 / Math.Sqrt(pow_p * pow_m);
            }
            else
            {
                double sinphits = Math.Sin(phits);
                double cosphits = Math.Cos(phits);
                akm1 = cosphits / tsfn(cosphits, sinphits, _e);

                double t = _e * sinphits;
                akm1 /= Math.Sqrt(1.0 - t * t);
            }
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            x *= _reciprocGlobalScale;
            y *= _reciprocGlobalScale;

            if (N_POLE) y = -y;
            double rho = Math.Sqrt(x * x + y * y);
            double tp = -rho / akm1;
            double phi_l = M_HALFPI - 2.0 * Math.Atan(tp);
            double halfe = -0.5 * _e;

            double lp_phi = 0.0;
            for (int iter = MAXIMUM_ITERATIONS; ;)
            {
                double sinphi = _e * Math.Sin(phi_l);
                double one_p_sinphi = 1.0 + sinphi;
                double one_m_sinphi = 1.0 - sinphi;
                lp_phi = 2.0 * Math.Atan(tp * Math.Pow(one_p_sinphi / one_m_sinphi, halfe))  + M_HALFPI;
                if (Math.Abs(phi_l - lp_phi) < ITERATION_TOLERANCE)
                {
                    break;
                }

                phi_l = lp_phi;
                if (--iter < 0)
                {
                    throw new Exception("Polar Stereographics doesn't converge");
                }

            }

            if (!N_POLE) lp_phi = -lp_phi;
            double lp_lam = (x == 0.0 && y == 0.0) ? 0.0 : Math.Atan2(x, y);

            x = lp_lam + central_meridian;
            y = lp_phi;
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters
        /// </summary>
        /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lp_lam = lon - central_meridian;
            double lp_phi = lat;

            double coslam = Math.Cos(lp_lam);
            double sinlam = Math.Sin(lp_lam);

            if (!N_POLE)
            {
                lp_phi = -lp_phi;
                coslam = -coslam;
            }

            double sinphi = Math.Sin(lp_phi);
            double cosphi = Math.Cos(lp_phi);

            double x = (Math.Abs(lp_phi - M_HALFPI) < EPS15) ? 0.0 : akm1 * tsfn(cosphi, sinphi, _e);
            lon = x * sinlam * _globalScale;
            lat = -x * coslam * _globalScale;
        }


        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override MathTransform Inverse()
        {
            if (_inverse == null)
            {
                _inverse = new PolarStereographicProjection(_Parameters.ToProjectionParameter(), this);
            }

            return _inverse;
        }

        private double tsfn(double cosphi, double sinphi, double e)
        {
            double t = (sinphi > 0.0) ? cosphi / (1.0 + sinphi) : (1.0 - sinphi) / cosphi;
            return Math.Exp(e * Atanh(e * sinphi)) * t;
        }


        /// <summary>
        /// Atanh - Inverse of Math.Tanh
        /// </summary>
        /// <remarks>The Math.Atanh is not available for netstandard2.0.</remarks>
        /// <param name="x"></param>
        private static double Atanh(double x)
        {
            return Math.Log((1 + x) / (1 - x)) * 0.5;
        }
    }
}
