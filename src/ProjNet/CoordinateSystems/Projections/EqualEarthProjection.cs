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
    internal class EqualEarthProjection : MapProjection
    {
        private const double A1 = 1.340264;
        private const double A2 = -0.081106;
        private const double A3 = 0.000893;
        private const double A4 = 0.003796;
        private const int Iterations = 12;

        private static readonly double M = Math.Sqrt(3.0) * 0.5;

        private readonly double radius;
        private readonly double inverseRadius;

        public EqualEarthProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public EqualEarthProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Equal_Earth";
            this.radius = this.semiMajor * this.scale_factor;
            this.inverseRadius = 1.0 / this.radius;
        }

        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new EqualEarthProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.central_meridian);
            double sinPhi = Math.Sin(lat);
            double theta = Math.Asin(Clamp(M * sinPhi, -1d, 1d));

            double theta2 = theta * theta;
            double theta6 = theta2 * theta2 * theta2;
            double denominator = A1 + (3d * A2 * theta2) + (theta6 * ((7d * A3) + (9d * A4 * theta2)));

            lon = this.radius * lambda * Math.Cos(theta) / (M * denominator);
            lat = this.radius * theta * (A1 + (A2 * theta2) + (theta6 * (A3 + (A4 * theta2))));
        }

        protected override void MetersToRadians(ref double x, ref double y)
        {
            double theta = y * this.inverseRadius;

            for (int i = 0; i < Iterations; i++)
            {
                double theta2 = theta * theta;
                double theta6 = theta2 * theta2 * theta2;
                double value = theta * (A1 + (A2 * theta2) + (theta6 * (A3 + (A4 * theta2)))) - (y * this.inverseRadius);
                double derivative = A1 + (3d * A2 * theta2) + (theta6 * ((7d * A3) + (9d * A4 * theta2)));
                double delta = value / derivative;
                theta -= delta;
                if (Math.Abs(delta) < 1e-12)
                {
                    break;
                }
            }

            double theta2Final = theta * theta;
            double theta6Final = theta2Final * theta2Final * theta2Final;
            double denominatorFinal = A1 + (3d * A2 * theta2Final) + (theta6Final * ((7d * A3) + (9d * A4 * theta2Final)));
            double cosTheta = Math.Cos(theta);

            if (Math.Abs(cosTheta) <= EPS10)
            {
                x = this.central_meridian;
            }
            else
            {
                x = Adjust_lon(this.central_meridian + ((x * this.inverseRadius) * M * denominatorFinal / cosTheta));
            }

            y = Math.Asin(Clamp(Math.Sin(theta) / M, -1d, 1d));
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
