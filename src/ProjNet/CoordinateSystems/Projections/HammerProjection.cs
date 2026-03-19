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
    internal class HammerProjection : MapProjection
    {
        private readonly double radius;
        private readonly double inverseRadius;
        private readonly double w;
        private readonly double m;
        private readonly double inverseM;

        public HammerProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public HammerProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Hammer";
            this.radius = this.semiMajor * this.scaleFactor;
            this.inverseRadius = 1d / this.radius;

            this.w = Math.Abs(this.Parameters.GetOptionalParameterValue("W", 0.5d, "w"));
            if (this.w <= 0d)
            {
                throw new ArgumentException("Invalid value for W: it should be > 0.");
            }

            this.m = Math.Abs(this.Parameters.GetOptionalParameterValue("M", 1d, "m"));
            if (this.m <= 0d)
            {
                throw new ArgumentException("Invalid value for M: it should be > 0.");
            }

            this.inverseM = 1d / this.m;
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new HammerProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = this.w * Adjust_lon(lon - this.centralMeridian);
            double cosPhi = Math.Cos(lat);
            double denominator = 1d + (cosPhi * Math.Cos(lambda));
            if (denominator == 0d)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            double d = Math.Sqrt(2d / denominator);
            lon = this.radius * ((this.m / this.w) * d * cosPhi * Math.Sin(lambda));
            lat = this.radius * (this.inverseM * d * Math.Sin(lat));
        }

        /// <inheritdoc />
        protected override void MetersToRadians(ref double x, ref double y)
        {
            double xUnit = x * this.inverseRadius;
            double yUnit = y * this.inverseRadius;

            double z = 1d - (0.25d * this.w * this.w * xUnit * xUnit) - (0.25d * yUnit * yUnit);
            if (z < 0d)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            z = Math.Sqrt(z);
            if (Math.Abs((2d * z * z) - 1d) < EPS10)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            double lambda = Math.Atan2(this.w * xUnit * z, (2d * z * z) - 1d) / this.w;
            double phi = Math.Asin(Clamp(z * yUnit, -1d, 1d));

            x = Adjust_lon(this.centralMeridian + lambda);
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
