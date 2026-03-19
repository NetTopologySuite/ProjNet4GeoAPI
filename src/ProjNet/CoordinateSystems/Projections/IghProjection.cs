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
    internal class IghProjection : MapProjection
    {
        private const double EpsLn = 1e-10;
        private const int MollweideIterations = 12;

        private static readonly double Sqrt2 = Math.Sqrt(2d);
        private static readonly double PhiBoundary = DegreesToRadians(40d + (44d / 60d) + (11.8d / 3600d));

        private static readonly double D20 = DegreesToRadians(20d);
        private static readonly double D30 = DegreesToRadians(30d);
        private static readonly double D40 = DegreesToRadians(40d);
        private static readonly double D50 = DegreesToRadians(50d);
        private static readonly double D60 = DegreesToRadians(60d);
        private static readonly double D80 = DegreesToRadians(80d);
        private static readonly double D100 = DegreesToRadians(100d);
        private static readonly double D140 = DegreesToRadians(140d);
        private static readonly double D160 = DegreesToRadians(160d);
        private static readonly double D180 = DegreesToRadians(180d);

        private readonly double radius;
        private readonly double inverseRadius;
        private readonly double dy0;
        private readonly ZoneDefinition[] zones;

        public IghProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public IghProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Interrupted_Goode_Homolosine";
            this.radius = this.semiMajor * this.scaleFactor;
            this.inverseRadius = 1d / this.radius;

            MollweideForwardUnit(0d, PhiBoundary, out _, out double mollweideBoundaryY);
            this.dy0 = PhiBoundary - mollweideBoundaryY;
            this.zones = CreateZones(this.dy0);
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new IghProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.centralMeridian);
            int zoneIndex = DetermineForwardZone(lat, lambda);
            ZoneDefinition zone = this.zones[zoneIndex];

            double localLambda = lambda - zone.Lambda0;
            double xUnit;
            double yUnit;
            if (zone.IsMollweide)
            {
                MollweideForwardUnit(localLambda, lat, out xUnit, out yUnit);
            }
            else
            {
                xUnit = localLambda * Math.Cos(lat);
                yUnit = lat;
            }

            lon = this.radius * (zone.X0 + xUnit);
            lat = this.radius * (zone.Y0 + yUnit);
        }

        /// <inheritdoc />
        protected override void MetersToRadians(ref double x, ref double y)
        {
            double xUnit = x * this.inverseRadius;
            double yUnit = y * this.inverseRadius;
            int zoneIndex = DetermineInverseZone(xUnit, yUnit, this.dy0);
            if (zoneIndex < 0)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            ZoneDefinition zone = this.zones[zoneIndex];
            double localX = xUnit - zone.X0;
            double localY = yUnit - zone.Y0;

            double lambdaLocal;
            double phi;
            if (zone.IsMollweide)
            {
                MollweideInverseUnit(localX, localY, out lambdaLocal, out phi);
            }
            else
            {
                phi = localY;
                double cosPhi = Math.Cos(phi);
                lambdaLocal = Math.Abs(cosPhi) <= EPS10 ? 0d : (localX / cosPhi);
            }

            double lambda = lambdaLocal + zone.Lambda0;
            if (!IsPointInZone(zoneIndex, lambda, phi))
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            x = Adjust_lon(this.centralMeridian + lambda);
            y = phi;
        }

        private static ZoneDefinition[] CreateZones(double dy0)
        {
            return new[]
            {
                new ZoneDefinition(true, -D100, -D100, dy0),  // 1
                new ZoneDefinition(true, D30, D30, dy0),      // 2
                new ZoneDefinition(false, -D100, -D100, 0d),  // 3
                new ZoneDefinition(false, D30, D30, 0d),      // 4
                new ZoneDefinition(false, -D160, -D160, 0d),  // 5
                new ZoneDefinition(false, -D60, -D60, 0d),    // 6
                new ZoneDefinition(false, D20, D20, 0d),      // 7
                new ZoneDefinition(false, D140, D140, 0d),    // 8
                new ZoneDefinition(true, -D160, -D160, -dy0), // 9
                new ZoneDefinition(true, -D60, -D60, -dy0),   // 10
                new ZoneDefinition(true, D20, D20, -dy0),     // 11
                new ZoneDefinition(true, D140, D140, -dy0),   // 12
            };
        }

        private static int DetermineForwardZone(double phi, double lambda)
        {
            if (phi >= PhiBoundary)
            {
                return lambda <= -D40 ? 0 : 1;
            }

            if (phi >= 0d)
            {
                return lambda <= -D40 ? 2 : 3;
            }

            if (phi >= -PhiBoundary)
            {
                if (lambda <= -D100)
                {
                    return 4;
                }

                if (lambda <= -D20)
                {
                    return 5;
                }

                return lambda <= D80 ? 6 : 7;
            }

            if (lambda <= -D100)
            {
                return 8;
            }

            if (lambda <= -D20)
            {
                return 9;
            }

            return lambda <= D80 ? 10 : 11;
        }

        private static int DetermineInverseZone(double x, double y, double dy0)
        {
            double y90 = dy0 + Sqrt2;
            if (y > (y90 + EpsLn) || y < (-y90 - EpsLn))
            {
                return -1;
            }

            if (y >= PhiBoundary)
            {
                return x <= -D40 ? 0 : 1;
            }

            if (y >= 0d)
            {
                return x <= -D40 ? 2 : 3;
            }

            if (y >= -PhiBoundary)
            {
                if (x <= -D100)
                {
                    return 4;
                }

                if (x <= -D20)
                {
                    return 5;
                }

                return x <= D80 ? 6 : 7;
            }

            if (x <= -D100)
            {
                return 8;
            }

            if (x <= -D20)
            {
                return 9;
            }

            return x <= D80 ? 10 : 11;
        }

        private static bool IsPointInZone(int zoneIndex, double lambda, double phi)
        {
            switch (zoneIndex)
            {
                case 0:
                    return ((lambda >= -D180 - EpsLn) && (lambda <= -D40 + EpsLn))
                        || (((lambda >= -D40 - EpsLn) && (lambda <= -DegreesToRadians(10d) + EpsLn))
                            && ((phi >= D60 - EpsLn) && (phi <= HALFPI + EpsLn)));
                case 1:
                    return ((lambda >= -D40 - EpsLn) && (lambda <= D180 + EpsLn))
                        || (((lambda >= -D180 - EpsLn) && (lambda <= -D160 + EpsLn))
                            && ((phi >= D50 - EpsLn) && (phi <= HALFPI + EpsLn)))
                        || (((lambda >= -DegreesToRadians(50d) - EpsLn) && (lambda <= -D40 + EpsLn))
                            && ((phi >= D60 - EpsLn) && (phi <= HALFPI + EpsLn)));
                case 2:
                    return (lambda >= -D180 - EpsLn) && (lambda <= -D40 + EpsLn);
                case 3:
                    return (lambda >= -D40 - EpsLn) && (lambda <= D180 + EpsLn);
                case 4:
                case 8:
                    return (lambda >= -D180 - EpsLn) && (lambda <= -D100 + EpsLn);
                case 5:
                case 9:
                    return (lambda >= -D100 - EpsLn) && (lambda <= -D20 + EpsLn);
                case 6:
                case 10:
                    return (lambda >= -D20 - EpsLn) && (lambda <= D80 + EpsLn);
                case 7:
                case 11:
                    return (lambda >= D80 - EpsLn) && (lambda <= D180 + EpsLn);
                default:
                    return false;
            }
        }

        private static void MollweideForwardUnit(double lambda, double phi, out double x, out double y)
        {
            double theta;
            if (Math.Abs(Math.Abs(phi) - HALFPI) < 1e-12)
            {
                theta = Sign(phi) * HALFPI;
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

        private struct ZoneDefinition
        {
            public ZoneDefinition(bool isMollweide, double lambda0, double x0, double y0)
            {
                this.IsMollweide = isMollweide;
                this.Lambda0 = lambda0;
                this.X0 = x0;
                this.Y0 = y0;
            }

            public bool IsMollweide { get; }

            public double Lambda0 { get; }

            public double X0 { get; }

            public double Y0 { get; }
        }
    }
}
