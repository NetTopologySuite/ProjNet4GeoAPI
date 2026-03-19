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
    internal class GoodeProjection : MapProjection
    {
        private const int MollweideIterations = 12;
        private const double YCor = 0.05280;
        private const double PhiLim = 0.71093078197902358062;
        private static readonly double Sqrt2 = Math.Sqrt(2d);

        private readonly double radius;
        private readonly double inverseRadius;

        public GoodeProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public GoodeProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Goode_Homolosine";
            this.radius = this.semiMajor * this.scale_factor;
            this.inverseRadius = 1d / this.radius;
        }

        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new GoodeProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.central_meridian);
            double phi = lat;

            double xUnit;
            double yUnit;

            if (Math.Abs(phi) <= PhiLim)
            {
                xUnit = lambda * Math.Cos(phi);
                yUnit = phi;
            }
            else
            {
                MollweideForwardUnit(lambda, phi, out xUnit, out yUnit);
                yUnit -= phi >= 0d ? YCor : -YCor;
            }

            lon = this.radius * xUnit;
            lat = this.radius * yUnit;
        }

        protected override void MetersToRadians(ref double x, ref double y)
        {
            double xUnit = x * this.inverseRadius;
            double yUnit = y * this.inverseRadius;

            double lambda;
            double phi;
            if (Math.Abs(yUnit) <= PhiLim)
            {
                phi = yUnit;
                double cosPhi = Math.Cos(phi);
                lambda = Math.Abs(cosPhi) <= EPS10 ? 0d : (xUnit / cosPhi);
            }
            else
            {
                double correctedY = yUnit + (yUnit >= 0d ? YCor : -YCor);
                MollweideInverseUnit(xUnit, correctedY, out lambda, out phi);
            }

            x = Adjust_lon(this.central_meridian + lambda);
            y = phi;
        }

        private static void MollweideForwardUnit(double lambda, double phi, out double x, out double y)
        {
            double theta;
            if (Math.Abs(Math.Abs(phi) - HALF_PI) < 1e-12)
            {
                theta = Sign(phi) * HALF_PI;
            }
            else
            {
                theta = phi;
                double target = PI * Math.Sin(phi);
                for (int i = 0; i < MollweideIterations; i++)
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

            x = (2d * Sqrt2 / PI) * lambda * Math.Cos(theta);
            y = Sqrt2 * Math.Sin(theta);
        }

        private static void MollweideInverseUnit(double x, double y, out double lambda, out double phi)
        {
            double theta = Math.Asin(Clamp(y / Sqrt2, -1d, 1d));
            double cosTheta = Math.Cos(theta);

            if (Math.Abs(cosTheta) <= EPS10)
            {
                lambda = 0d;
            }
            else
            {
                lambda = x * PI / (2d * Sqrt2 * cosTheta);
            }

            phi = Math.Asin(Clamp(((2d * theta) + Math.Sin(2d * theta)) / PI, -1d, 1d));
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
