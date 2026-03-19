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

    /// <summary>
    /// Represents the documented type.
    /// </summary>
    [Serializable]
    internal class HealpixProjection : MapProjection
    {
        private static readonly double Phi0 = Math.Asin(2d / 3d);
        private static readonly double QuarterPi = PI / 4d;
        private static readonly double HalfPi = PI / 2d;

        private readonly double radius;
        private readonly double inverseRadius;
        private readonly bool isEllipsoidal;
        private readonly double oneEs;
        private readonly double qp;
        private readonly double[] apa;
        private readonly double rotationRadians;

        public HealpixProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public HealpixProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "HEALPix";
            this.radius = this.semiMajor * this.scaleFactor;
            this.inverseRadius = 1d / this.radius;

            this.rotationRadians = DegreesToRadians(this.Parameters.GetOptionalParameterValue("rot_xy", 0d));
            this.isEllipsoidal = this.es > 0d;

            if (this.isEllipsoidal)
            {
                this.oneEs = 1d - this.es;
                this.qp = Qsfn(1d, this.e, this.oneEs);
                this.apa = Authset(this.es);
            }
            else
            {
                this.oneEs = 0d;
                this.qp = 0d;
                this.apa = null;
            }
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new HealpixProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.centralMeridian);
            double phi = this.isEllipsoidal ? GeographicToAuthalic(lat) : lat;

            ToHealpixSphere(lambda, phi, out double xUnit, out double yUnit);
            Rotate(ref xUnit, ref yUnit, -this.rotationRadians);

            lon = this.radius * xUnit;
            lat = this.radius * yUnit;
        }

        /// <inheritdoc />
        protected override void MetersToRadians(ref double x, ref double y)
        {
            double xUnit = x * this.inverseRadius;
            double yUnit = y * this.inverseRadius;

            Rotate(ref xUnit, ref yUnit, this.rotationRadians);
            FromHealpixSphere(xUnit, yUnit, out double lambda, out double phiAuthalic);

            x = Adjust_lon(this.centralMeridian + lambda);
            y = this.isEllipsoidal ? Authlat(phiAuthalic, this.apa) : phiAuthalic;
        }

        private static void ToHealpixSphere(double lambda, double phi, out double x, out double y)
        {
            if (Math.Abs(phi) <= Phi0)
            {
                x = lambda;
                y = (3d * PI / 8d) * Math.Sin(phi);
                return;
            }

            double sigma = Math.Sqrt(Math.Max(0d, 3d * (1d - Math.Abs(Math.Sin(phi)))));
            int capNumber = (int)Math.Floor((2d * lambda / PI) + 2d);
            if (capNumber < 0)
            {
                capNumber = 0;
            }
            else if (capNumber > 3)
            {
                capNumber = 3;
            }

            double lambdaCenter = (-3d * QuarterPi) + (HalfPi * capNumber);
            x = lambdaCenter + ((lambda - lambdaCenter) * sigma);
            y = Sign(phi) * QuarterPi * (2d - sigma);
        }

        private static void FromHealpixSphere(double x, double y, out double lambda, out double phi)
        {
            if (Math.Abs(y) <= QuarterPi)
            {
                lambda = x;
                phi = Math.Asin(Clamp((8d * y) / (3d * PI), -1d, 1d));
                return;
            }

            if (Math.Abs(y) < HalfPi)
            {
                int capNumber = (int)Math.Floor((2d * x / PI) + 2d);
                if (capNumber < 0)
                {
                    capNumber = 0;
                }
                else if (capNumber > 3)
                {
                    capNumber = 3;
                }

                double xCenter = (-3d * QuarterPi) + (HalfPi * capNumber);
                double tau = 2d - ((4d * Math.Abs(y)) / PI);
                if (Math.Abs(tau) <= EPS10)
                {
                    lambda = xCenter;
                    phi = Sign(y) * HalfPi;
                    return;
                }

                lambda = xCenter + ((x - xCenter) / tau);
                phi = Sign(y) * Math.Asin(Clamp(1d - ((tau * tau) / 3d), -1d, 1d));
                return;
            }

            lambda = -PI;
            phi = Sign(y) * HalfPi;
        }

        private static void Rotate(ref double x, ref double y, double angle)
        {
            if (Math.Abs(angle) <= EPS10)
            {
                return;
            }

            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            double xr = (x * cos) - (y * sin);
            double yr = (y * cos) + (x * sin);
            x = xr;
            y = yr;
        }

        private double GeographicToAuthalic(double phi)
        {
            double q = Qsfn(Math.Sin(phi), this.e, this.oneEs);
            return Math.Asin(Clamp(q / this.qp, -1d, 1d));
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
