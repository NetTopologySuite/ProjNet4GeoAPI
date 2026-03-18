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
    internal class MollweideProjection : MapProjection
    {
        private const int Iterations = 12;

        private static readonly double Sqrt2 = Math.Sqrt(2d);

        private readonly double radius;
        private readonly double inverseRadius;

        public MollweideProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public MollweideProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Mollweide";
            this.radius = this.semiMajor * this.scale_factor;
            this.inverseRadius = 1.0 / this.radius;
        }

        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new MollweideProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.central_meridian);
            double theta;

            if (Math.Abs(Math.Abs(lat) - HALF_PI) < 1e-12)
            {
                theta = Sign(lat) * HALF_PI;
            }
            else
            {
                theta = lat;
                double target = PI * Math.Sin(lat);

                for (int i = 0; i < Iterations; i++)
                {
                    double twoTheta = 2d * theta;
                    double delta = ((twoTheta + Math.Sin(twoTheta)) - target) / (2d + (2d * Math.Cos(twoTheta)));
                    theta -= delta;
                    if (Math.Abs(delta) < 1e-12)
                    {
                        break;
                    }
                }
            }

            lon = this.radius * (2d * Sqrt2 / PI) * lambda * Math.Cos(theta);
            lat = this.radius * Sqrt2 * Math.Sin(theta);
        }

        protected override void MetersToRadians(ref double x, ref double y)
        {
            double theta = Math.Asin(Clamp((y * this.inverseRadius) / Sqrt2, -1d, 1d));
            double cosTheta = Math.Cos(theta);

            if (Math.Abs(cosTheta) <= EPS10)
            {
                x = this.central_meridian;
            }
            else
            {
                x = Adjust_lon(this.central_meridian + ((x * this.inverseRadius) * PI / (2d * Sqrt2 * cosTheta)));
            }

            y = Math.Asin(Clamp(((2d * theta) + Math.Sin(2d * theta)) / PI, -1d, 1d));
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

