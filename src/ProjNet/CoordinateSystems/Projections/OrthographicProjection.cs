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
    using System.Text;

    /// <summary>
    /// Represents the documented type.
    /// </summary>
    [Serializable]
    internal class OrthographicProjection : MapProjection
    {
        private enum Mode
        {
            N_POLE = 0,
            S_POLE = 1,
            EQUIT = 2,
            OBLIQ = 3,
        }

        private readonly double sinph0;
        private readonly double cosph0;
        private readonly double nu0;
        private readonly double yShift;
        private readonly double yScale;
        private readonly Mode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrthographicProjection"/> class.
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
        /// Initializes a new instance of the <see cref="OrthographicProjection"/> class.
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
            this.Name = "Orthographic";

            Sincos(this.Phi0, out this.sinph0, out this.cosph0);

            if (Math.Abs(Math.Abs(this.Phi0) - HALFPI) <= EPS10)
            {
                this.mode = this.Phi0 < 0.0 ? Mode.S_POLE : Mode.N_POLE;
            }
            else if (Math.Abs(this.Phi0) > EPS10)
            {
                this.mode = Mode.OBLIQ;
            }
            else
            {
                this.mode = Mode.EQUIT;
            }

            if (this.es > 0)
            {
                this.nu0 = this.semiMajor / Math.Sqrt(1.0 - (this.es * this.sinph0 * this.sinph0));
                this.yShift = this.es * this.nu0 / this.semiMajor * this.sinph0 * this.cosph0;
                this.yScale = 1.0 / Math.Sqrt(1.0 - (this.es * this.cosph0 * this.cosph0));
            }
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override MathTransform Inverse()
        {
            if (this.inverse == null)
            {
                this.inverse = new OrthographicProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians.
        /// </summary>
        /// <param name="x">The x-ordinate in meters when entering, longitude in radians ater exit.</param>
        /// <param name="y">The y-ordinate in meters when entering, latitude in radians after exit.</param>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            if (this.es == 0.0)
            {
                this.OrthoSInverse(ref x, ref y);
            }
            else
            {
                this.OrthoEInverse(ref x, ref y);
            }
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians for spherical orthographic projections.
        /// </summary>
        /// <param name="x">The x-ordinate in meters when entering, longitude in radians ater exit.</param>
        /// <param name="y">The y-ordinate in meters when entering, latitude in radians after exit.</param>
        private void OrthoSInverse(ref double x, ref double y)
        {
            // Using the algorithm in Map projections: A working manual, by John Snyder pg 150
            double rho = Hypot(x, y);
            if (rho > this.semiMajor)
            {
                if ((rho - this.semiMajor) > EPS10)
                {
                    throw new ArgumentOutOfRangeException($"Point ({x:F3}, {y:F3}) is outside of the projection boundary");
                }

                rho = this.semiMajor;
            }

            double sinc = rho / this.semiMajor;

            double cosc = Math.Sqrt(1.0 - (sinc * sinc)); // in this range OK

            double phi;
            double lam;
            if (Math.Abs(rho) <= EPS10)
            {
                phi = this.latOrigin;
                lam = this.Lon_origin;
            }
            else
            {
                switch (this.mode)
                {
                    case Mode.N_POLE:
                        phi = Math.Asin(cosc);
                        lam = this.Lon_origin + Math.Atan2(x, -y);
                        break;
                    case Mode.S_POLE:
                        phi = -Math.Asin(cosc);
                        lam = this.Lon_origin + Math.Atan2(x, y);
                        break;
                    case Mode.EQUIT:
                        if (Math.Abs(y) >= this.semiMajor)
                        {
                            phi = y < 0.0 ? -HALFPI : HALFPI;
                        }
                        else
                        {
                            phi = Math.Asin(y / this.semiMajor);
                        }

                        lam = this.Lon_origin + Math.Atan2(x / this.semiMajor, cosc);
                        break;
                    case Mode.OBLIQ:
                        phi = Math.Asin((cosc * this.sinph0) + (y * this.cosph0 / this.semiMajor));
                        lam = this.Lon_origin + Math.Atan2(x * sinc, (rho * this.cosph0 * cosc) - (y * this.sinph0 * sinc));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(this.mode));
                }
            }

            // Return values in passed in parameters
            x = lam;
            y = phi;
        }

        /// <summary>
        /// Converts coordinates in projected meters to radians for ellipsoidal orthographic projections.
        /// </summary>
        /// <param name="x">The x-ordinate in meters when entering, longitude in radians ater exit.</param>
        /// <param name="y">The y-ordinate in meters when entering, latitude in radians after exit.</param>
        private void OrthoEInverse(ref double x, ref double y)
        {
            Func<double, double> sQ = (a) => a * a;

            double x_scaled = x / this.semiMajor;
            double y_scaled = y / this.semiMajor;
            double phi;
            double lam;
            if (this.mode == Mode.N_POLE || this.mode == Mode.S_POLE)
            {
                // Polar case. Forward case equations can be simplified as:
                // x = nu * cosphi * sinlam
                // y = nu * -cosphi * coslam * sign(phi0)
                // ==> lam = atan2(x, -y * sign(phi0))
                // ==> (x/a)^2 + (y/a)^2 = nu^2 * cosphi^2
                //                rh^2 = cosphi^2 / (1 - es * sinphi^2)
                // ==>  cosphi^2 = rh^2 * (1 - es) / (1 - es * rh^2)
                lam = Math.Atan2(x, -y * Sign(this.latOrigin));

                double rh2 = sQ(x_scaled) + sQ(y_scaled);
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
                    phi = Math.Acos(Math.Sqrt(rh2 * (1 - this.es) / (1 - (this.es * rh2)))) * Sign(this.latOrigin);
                }
            }
            else if (this.mode == Mode.EQUIT)
            {
                // Equatorial case. Forward case equations can be simplified as:
                // x = nu * cosphi * sinlam
                // y  = nu * sinphi * (1 - P->es)
                // (x/a)^2 * (1 - es * sinphi^2) = (1 - sinphi^2) * sinlam^2
                // (y/a)^2 / ((1 - es)^2 + (y/a)^2 * es) = sinphi^2

                // Equation of the ellipse
                if (sQ(x_scaled) + sQ(y_scaled * (this.semiMajor / this.semiMinor)) > 1 + 1e-11)
                {
                    throw new ArgumentOutOfRangeException($"Point ({x:F3}, {y:F3}) is outside of the projection boundary");
                }

                double sinphi2 = sQ(y_scaled) / (sQ(1 - this.es) + (sQ(y_scaled) * this.es));
                if (sinphi2 > 1 - 1e-11)
                {
                    phi = HALFPI * Sign(y_scaled);
                    lam = 0.0;
                }
                else
                {
                    phi = Math.Asin(Math.Sqrt(sinphi2)) * Sign(y_scaled);
                    double sinlam = x_scaled * Math.Sqrt((1 - (this.es * sinphi2)) / (1 - sinphi2));
                    if (Math.Abs(sinlam) - 1 > -1e-15)
                    {
                        lam = HALFPI * Sign(x_scaled);
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
                double y_recentered = (y - this.yShift) / this.yScale;
                if (sQ(x_scaled) + sQ(y_scaled) > 1 + 1e-11)
                {
                    throw new ArgumentOutOfRangeException($"Point ({x_scaled:F3}, {y_scaled:F3}) is outside of the projection boundary");
                }

                // From EPSG guidance note 7.2, March 2020, §3.3.5 Orthographic

                // It suggests as initial guess:
                // lp.lam = 0;
                // lp.phi = P->phi0;
                // But for poles, this will not converge well. Better use:
                this.OrthoSInverse(ref x_recentered, ref y_recentered);
                phi = y_recentered;
                lam = x_recentered - this.Lon_origin;

                for (int i = 0; i < 20; ++i)
                {
                    Sincos(phi, out double sinphi, out double cosphi);
                    Sincos(lam, out double sinlam, out double coslam);
                    double one_minus_es_sinphi2 = 1.0 - (this.es * sinphi * sinphi);
                    double nu = this.semiMajor / Math.Sqrt(one_minus_es_sinphi2);
                    double rho = (1.0 - this.es) * nu / one_minus_es_sinphi2;

                    double x_new = nu * cosphi * sinlam;
                    double y_new = (nu * ((sinphi * this.cosph0) - (cosphi * this.sinph0 * coslam))) +
                        (this.es * ((this.nu0 * this.sinph0) - (nu * sinphi)) * this.cosph0);
                    double j11 = -rho * sinphi * sinlam;
                    double j12 = nu * cosphi * coslam;
                    double j21 = rho * ((cosphi * this.cosph0) + (sinphi * this.sinph0 * coslam));
                    double j22 = nu * this.sinph0 * this.cosph0 * sinlam;
                    double d = (j11 * j22) - (j12 * j21);
                    double dx = x - x_new;
                    double dy = y - y_new;
                    double dphi = ((j22 * dx) - (j12 * dy)) / d;
                    double dlam = ((-j21 * dx) + (j11 * dy)) / d;

                    phi += dphi;
                    if (phi > HALFPI)
                    {
                        phi = HALFPI;
                    }
                    else if (phi < -HALFPI)
                    {
                        phi = -HALFPI;
                    }

                    lam += dlam;
                    if (Math.Abs(dphi) < 1e-12 && Math.Abs(dlam) < 1e-12)
                    {
                        break;
                    }
                }
            }

            // Return values
            x = lam + this.Lon_origin;
            y = phi;
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters.
        /// </summary>
        /// <param name="lon">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="lat">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            if (this.es == 0.0)
            {
                this.OrthoSForward(ref lon, ref lat);
            }
            else
            {
                this.OrthoEForward(ref lon, ref lat);
            }
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters for spherical orthographic projections.
        /// </summary>
        /// <param name="lam">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="phi">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        private void OrthoSForward(ref double lam, ref double phi)
        {
            double x = HUGEVAL;
            double y = HUGEVAL;

            double cosphi = Math.Cos(phi);
            double coslam = Math.Cos(lam - this.Lon_origin);
            double sinphi;
            switch (this.mode)
            {
                case Mode.EQUIT:
                    if (cosphi * coslam < -EPS10)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }

                    y = this.semiMajor * Math.Sin(phi);
                    break;
                case Mode.OBLIQ:
                    sinphi = Math.Sin(phi);

                    // Is the point visible from the projection plane ?
                    // From https://lists.osgeo.org/pipermail/proj/2020-September/009831.html
                    // this is the dot product of the normal of the ellipsoid at the center of
                    // the projection and at the point considered for projection.
                    // [cos(phi)*cos(lambda), cos(phi)*sin(lambda), sin(phi)]
                    // Also from Snyder's Map Projection - A working manual, equation (5-3), page 149
                    if ((this.sinph0 * sinphi) + (this.cosph0 * cosphi * coslam) < -EPS10)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }

                    y = this.semiMajor * ((this.cosph0 * sinphi) - (this.sinph0 * cosphi * coslam));
                    break;
                case Mode.N_POLE:
                    coslam = -coslam;
                    if (Math.Abs(phi - this.Phi0) - EPS10 > HALFPI)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }

                    y = this.semiMajor * cosphi * coslam;
                    break;
                case Mode.S_POLE:
                    if (Math.Abs(phi - this.Phi0) - EPS10 > HALFPI)
                    {
                        throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
                    }

                    y = this.semiMajor * cosphi * coslam;
                    break;
            }

            x = this.semiMajor * cosphi * Math.Sin(lam - this.Lon_origin);

            // Set the variables to return
            lam = x;
            phi = y;
        }

        /// <summary>
        /// Method to convert a point (lon, lat) in radians to (x, y) in meters for ellipsoidal orthographic projections.
        /// </summary>
        /// <param name="lam">The longitude of the point in radians when entering, its x-ordinate in meters after exit.</param>
        /// <param name="phi">The latitude of the point in radians when entering, its y-ordinate in meters after exit.</param>
        private void OrthoEForward(ref double lam, ref double phi)
        {
            // From EPSG guidance note 7.2, March 2020, §3.3.5 Orthographic
            Sincos(phi, out double sinphi, out double cosphi);
            Sincos(lam - this.Lon_origin, out double sinlam, out double coslam);

            // Is the point visible from the projection plane ?
            // Same condition as in spherical case
            if ((this.sinph0 * sinphi) + (this.cosph0 * cosphi * coslam) < -EPS10)
            {
                throw new ArgumentOutOfRangeException($"Coordinate ({RadiansToDegrees(lam):F3}, {RadiansToDegrees(phi):F3}) is on the unprojected hemisphere");
            }

            double nu = this.semiMajor / Math.Sqrt(1.0 - (this.es * sinphi * sinphi));
            double x = nu * cosphi * sinlam;
            double y = (nu * ((sinphi * this.cosph0) - (cosphi * this.sinph0 * coslam))) +
                (this.es * ((this.nu0 * this.sinph0) - (nu * sinphi)) * this.cosph0);

            lam = x;
            phi = y;
        }
    }
}
