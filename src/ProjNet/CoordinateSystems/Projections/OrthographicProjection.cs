using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjNet.CoordinateSystems.Projections
{
    [Serializable]
    internal class OrthographicProjection : MapProjection
    {
        private enum Mode
        {
            N_POLE = 0,
            S_POLE = 1,
            EQUIT = 2,
            OBLIQ = 3
        }

        private readonly double _sinph0;
        private readonly double _cosph0;
        private readonly double _nu0;
        private readonly double _y_shift;
        private readonly double _y_scale;
        private readonly Mode _mode;
        /// <summary>
        /// Initializes the OrthographicProjection object with the specified parameters to project points. 
        /// </summary>
        /// <param name="parameters">ParameterList with the required parameters.</param>
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
        public OrthographicProjection(IEnumerable<ProjectionParameter> parameters) : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes the OrthographicProjection object with the specified parameters to project points. 
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="inverse">Null indicates the projection is forward (degrees to meters).</param>
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
        public OrthographicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse) : base(parameters, inverse)
        {
            Name = "Orthographic";

            sincos(phi0, out _sinph0, out _cosph0);

            if( Math.Abs(Math.Abs(phi0) - HALF_PI) <= EPS10 )
            {
                _mode = phi0 < 0.0 ? Mode.S_POLE : Mode.N_POLE;
            }
            else if ( Math.Abs(phi0) > EPS10)
            {
                _mode = Mode.OBLIQ;
            }
            else
            {
                _mode = Mode.EQUIT;
            }

            if( _es > 0 )
            {
                _nu0 = _semiMajor / Math.Sqrt(1.0 - _es * _sinph0 * _sinph0);
                _y_shift = _es * _nu0 / _semiMajor * _sinph0 * _cosph0;
                _y_scale = 1.0 / Math.Sqrt(1.0 - _es * _cosph0 * _cosph0);
            }
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override MathTransform Inverse()
        {
            if (_inverse == null)
            {
                _inverse = new OrthographicProjection(_Parameters.ToProjectionParameter(), this);
            }

            return _inverse;
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians.
        /// </summary>
        /// <param name="x">The x-ordinate in meters when entering, longitude in radians ater exit</param>
        /// <param name="y">The y-ordinate in meters when entering, latitude in radians after exit</param>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            if( _es == 0.0 )
            {
                OrthoSInverse(ref x, ref y);
            }
            else
            {
                OrthoEInverse(ref x, ref y);
            }
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians for spherical orthographic projections.
        /// </summary>
        /// <param name="x">The x-ordinate in meters when entering, longitude in radians ater exit</param>
        /// <param name="y">The y-ordinate in meters when entering, latitude in radians after exit</param>
        private void OrthoSInverse(ref double x, ref double y)
        {
            //Using the algorithm in Map projections: A working manual, by John Snyder pg 150
            double rho = hypot(x, y);
            if( rho > _semiMajor)
            {
                if( (rho - _semiMajor) > EPS10)
                {
                    throw new ArgumentOutOfRangeException($"Point ({x:F3}, {y:F3}) is outside of the projection boundary");
                }

                rho = _semiMajor;
            }
            double sinc = rho / _semiMajor;

            double cosc = Math.Sqrt(1.0 - sinc * sinc); // in this range OK

            double phi;
            double lam;
            if (Math.Abs(rho) <= EPS10)
            {
                phi = lat_origin;
                lam = lon_origin;
            }
            else
            {
                switch (_mode)
                {
                    case Mode.N_POLE:
                        phi = Math.Asin(cosc);
                        lam = lon_origin + Math.Atan2(x, -y);
                        break;
                    case Mode.S_POLE:
                        phi = -Math.Asin(cosc);
                        lam = lon_origin + Math.Atan2(x, y);
                        break;
                    case Mode.EQUIT:
                        if (Math.Abs(y) >= _semiMajor)
                        {
                            phi = y < 0.0 ? -HALF_PI : HALF_PI;
                        }
                        else
                        {
                            phi = Math.Asin(y/_semiMajor);
                        }
                        lam = lon_origin + Math.Atan2(x / _semiMajor, cosc);
                        break;
                    case Mode.OBLIQ:
                        phi = Math.Asin(cosc * _sinph0 + (y * _cosph0 / _semiMajor));
                        lam = lon_origin + Math.Atan2(x * sinc, rho * _cosph0 * cosc - y * _sinph0 * sinc);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(_mode));
                }
            }

            //Return values in passed in parameters
            x = lam;
            y = phi;
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians for ellipsoidal orthographic projections.
        /// </summary>
        /// <param name="x">The x-ordinate in meters when entering, longitude in radians ater exit</param>
        /// <param name="y">The y-ordinate in meters when entering, latitude in radians after exit</param>
        private void OrthoEInverse(ref double x, ref double y)
        {
            Func<double, double> SQ = (a) => a * a;

            double x_scaled = x / _semiMajor;
            double y_scaled = y / _semiMajor;
            double phi;
            double lam;
            if (_mode == Mode.N_POLE || _mode == Mode.S_POLE)
            {
                // Polar case. Forward case equations can be simplified as:
                // x = nu * cosphi * sinlam
                // y = nu * -cosphi * coslam * sign(phi0)
                // ==> lam = atan2(x, -y * sign(phi0))
                // ==> (x/a)^2 + (y/a)^2 = nu^2 * cosphi^2
                //                rh^2 = cosphi^2 / (1 - es * sinphi^2)
                // ==>  cosphi^2 = rh^2 * (1 - es) / (1 - es * rh^2)
                lam = Math.Atan2(x, -y * sign(lat_origin));

                double rh2 = SQ(x_scaled) + SQ(y_scaled);
                if (rh2 >= 1.0 - 1e-15)
                {
                    if ((rh2 - 1.0) > EPS10)
                    {
                        throw new ArgumentOutOfRangeException($"Point ({x_scaled:F3}, {y_scaled:F3}) is outside of the projection boundary");
                    }
                    phi = 0.0;
                }
                else
                {
                    phi = Math.Acos(Math.Sqrt(rh2 * (1 - _es) / (1 - _es * rh2))) * sign(lat_origin);
                }
            }
            else if (_mode == Mode.EQUIT)
            {
                // Equatorial case. Forward case equations can be simplified as:
                // x = nu * cosphi * sinlam
                // y  = nu * sinphi * (1 - P->es)
                // (x/a)^2 * (1 - es * sinphi^2) = (1 - sinphi^2) * sinlam^2
                // (y/a)^2 / ((1 - es)^2 + (y/a)^2 * es) = sinphi^2

                // Equation of the ellipse
                if( SQ(x_scaled) + SQ(y_scaled * (_semiMajor / _semiMinor)) > 1 + 1e-11 )
                {
                    throw new ArgumentOutOfRangeException($"Point ({x:F3}, {y:F3}) is outside of the projection boundary");
                }

                double sinphi2 = SQ(y_scaled) / (SQ(1 - _es) + SQ(y_scaled)*_es);
                if (sinphi2 > 1 - 1e-11)
                {
                    phi = HALF_PI * sign(y_scaled);
                    lam = 0.0;
                }
                else
                {
                    phi = Math.Asin(Math.Sqrt(sinphi2)) * sign(y_scaled);
                    double sinlam = x_scaled * Math.Sqrt((1 - _es * sinphi2) / (1 - sinphi2));
                    if (Math.Abs(sinlam) - 1 > -1e-15)
                    {
                        lam = HALF_PI * sign(x_scaled);
                    }
                    else
                    {
                        lam = Math.Asin(sinlam);
                    }
                }
            }
            else
            {
                // Using Q->sinph0 * sinphi + Q->cosph0 * cosphi * coslam == 0 (visibity
                // condition of the forward case) in the forward equations, and a lot of
                // substitution games...
                double x_recentered = x;
                double y_recentered = (y - _y_shift) / _y_scale;
                if( SQ(x_scaled) + SQ(y_scaled) > 1 + 1e-11)
                {
                    throw new ArgumentOutOfRangeException($"Point ({x_scaled:F3}, {y_scaled:F3}) is outside of the projection boundary");
                }

                // From EPSG guidance note 7.2, March 2020, §3.3.5 Orthographic

                // It suggests as initial guess:
                // lp.lam = 0;
                // lp.phi = P->phi0;
                // But for poles, this will not converge well. Better use:
                OrthoSInverse(ref x_recentered, ref y_recentered);
                phi = y_recentered;
                lam = x_recentered - lon_origin;

                for ( int i = 0; i < 20; ++i )
                {
                    sincos(phi, out double sinphi, out double cosphi);
                    sincos(lam, out double sinlam, out double coslam);
                    double one_minus_es_sinphi2 = 1.0 - _es * sinphi * sinphi;
                    double nu = _semiMajor / Math.Sqrt(one_minus_es_sinphi2);
                    double rho = (1.0 - _es) * nu / one_minus_es_sinphi2;

                    double x_new = nu * cosphi * sinlam;
                    double y_new = nu * (sinphi * _cosph0 - cosphi * _sinph0 * coslam) +
                        _es * (_nu0 * _sinph0 - nu * sinphi) * _cosph0;
                    double J11 = -rho * sinphi * sinlam;
                    double J12 = nu * cosphi * coslam;
                    double J21 = rho * (cosphi * _cosph0 + sinphi * _sinph0 * coslam);
                    double J22 = nu * _sinph0 * _cosph0 * sinlam;
                    double D = J11 * J22 - J12 * J21;
                    double dx = x - x_new;
                    double dy = y - y_new;
                    double dphi = (J22 * dx - J12 * dy) / D;
                    double dlam = (-J21 * dx + J11 * dy) / D;

                    phi += dphi;
                    if( phi > HALF_PI)
                    {
                        phi = HALF_PI;
                    }
                    else if (phi < -HALF_PI)
                    {
                        phi = -HALF_PI;
                    }

                    lam += dlam;
                    if( Math.Abs(dphi) < 1e-12 && Math.Abs(dlam) < 1e-12 )
                    {
                        break;
                    }
                }
            }

            //Return values
            x = lam + lon_origin;
            y = phi;
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters
        /// </summary>
        /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            if (_es == 0.0)
            {
                OrthoSForward(ref lon, ref lat);
            }
            else
            {
                OrthoEForward(ref lon, ref lat);
            }
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters for spherical orthographic projections
        /// </summary>
        /// <param name="lam">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="phi">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        private void OrthoSForward(ref double lam, ref double phi)
        {
            double x = HUGE_VAL;
            double y = HUGE_VAL;

            double cosphi = Math.Cos(phi);
            double coslam = Math.Cos(lam - lon_origin);
            double sinphi;
            switch (_mode)
            {
                case Mode.EQUIT:
                    if (cosphi * coslam < -EPS10)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }
                    y = _semiMajor * Math.Sin(phi);
                    break;
                case Mode.OBLIQ:
                    sinphi = Math.Sin(phi);

                    // Is the point visible from the projection plane ?
                    // From https://lists.osgeo.org/pipermail/proj/2020-September/009831.html
                    // this is the dot product of the normal of the ellipsoid at the center of
                    // the projection and at the point considered for projection.
                    // [cos(phi)*cos(lambda), cos(phi)*sin(lambda), sin(phi)]
                    // Also from Snyder's Map Projection - A working manual, equation (5-3), page 149
                    if (_sinph0 * sinphi + _cosph0 * cosphi * coslam < -EPS10)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }
                    y = _semiMajor * ( _cosph0 * sinphi - _sinph0 * cosphi * coslam );
                    break;
                case Mode.N_POLE:
                    coslam = -coslam;
                    if (Math.Abs(phi - phi0) - EPS10 > HALF_PI)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }
                    y = _semiMajor * cosphi * coslam;
                    break;
                case Mode.S_POLE:
                    if (Math.Abs(phi - phi0) - EPS10 > HALF_PI)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }
                    y = _semiMajor * cosphi * coslam;
                    break;
            }

            x = _semiMajor * cosphi * Math.Sin(lam - lon_origin);

            // Set the variables to return
            lam = x;
            phi = y;
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters for ellipsoidal orthographic projections
        /// </summary>
        /// <param name="lam">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="phi">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        private void OrthoEForward(ref double lam, ref double phi)
        {
            // From EPSG guidance note 7.2, March 2020, §3.3.5 Orthographic
            sincos(phi, out double sinphi, out double cosphi);
            sincos(lam - lon_origin, out double sinlam, out double coslam);

            // Is the point visible from the projection plane ?
            // Same condition as in spherical case
            if( _sinph0 * sinphi + _cosph0 * cosphi * coslam < - EPS10 )
            {
                throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
            }

            double nu = _semiMajor / Math.Sqrt(1.0 - _es * sinphi * sinphi);
            double x = nu * cosphi * sinlam;
            double y = nu * (sinphi * _cosph0 - cosphi * _sinph0 * coslam) +
                _es * (_nu0 * _sinph0 - nu * sinphi) * _cosph0;

            lam = x;
            phi = y;
        }
    }
}
