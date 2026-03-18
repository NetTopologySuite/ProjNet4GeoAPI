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

    [Serializable]
    internal class GnomonicProjection : MapProjection
    {
        private readonly double radius;
        private readonly double inverseRadius;
        private readonly double sinPhi0;
        private readonly double cosPhi0;

        public GnomonicProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public GnomonicProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Gnomonic";
            this.radius = this.semiMajor * this.scale_factor;
            this.inverseRadius = 1.0 / this.radius;
            Sincos(this.lat_origin, out this.sinPhi0, out this.cosPhi0);
        }

        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new GnomonicProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.central_meridian);
            double sinPhi = Math.Sin(lat);
            double cosPhi = Math.Cos(lat);
            double cosLambda = Math.Cos(lambda);

            double cosC = (this.sinPhi0 * sinPhi) + (this.cosPhi0 * cosPhi * cosLambda);
            if (cosC <= EPS10)
            {
                lon = double.NaN;
                lat = double.NaN;
                return;
            }

            double k = 1d / cosC;
            lon = this.radius * k * cosPhi * Math.Sin(lambda);
            lat = this.radius * k * ((this.cosPhi0 * sinPhi) - (this.sinPhi0 * cosPhi * cosLambda));
        }

        protected override void MetersToRadians(ref double x, ref double y)
        {
            double rho = Hypot(x, y);
            if (rho <= EPS10)
            {
                x = this.central_meridian;
                y = this.lat_origin;
                return;
            }

            double c = Math.Atan(rho * this.inverseRadius);
            double sinC = Math.Sin(c);
            double cosC = Math.Cos(c);

            double phi = Math.Asin(Clamp((cosC * this.sinPhi0) + ((y * sinC * this.cosPhi0) / rho), -1d, 1d));
            double lambda = Math.Atan2(x * sinC, (rho * this.cosPhi0 * cosC) - (y * this.sinPhi0 * sinC));

            x = Adjust_lon(this.central_meridian + lambda);
            y = phi;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
