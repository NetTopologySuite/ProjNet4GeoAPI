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
    internal class SinusoidalProjection : MapProjection
    {
        private readonly double radius;
        private readonly double inverseRadius;
        private readonly bool isEllipsoidal;

        public SinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public SinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Sinusoidal";
            this.radius = this.semiMajor * this.scaleFactor;
            this.inverseRadius = 1d / this.radius;
            this.isEllipsoidal = this.es > 0d;
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new SinusoidalProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.centralMeridian);
            double phi = lat;

            if (this.isEllipsoidal)
            {
                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);
                lat = this.radius * this.Mlfn(phi, sinPhi, cosPhi);
                lon = this.radius * lambda * cosPhi / Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
                return;
            }

            lon = this.radius * lambda * Math.Cos(phi);
            lat = this.radius * phi;
        }

        /// <inheritdoc />
        protected override void MetersToRadians(ref double x, ref double y)
        {
            double xUnit = x * this.inverseRadius;
            double yUnit = y * this.inverseRadius;

            if (this.isEllipsoidal)
            {
                double phiEllipsoid = this.Inv_mlfn(yUnit);
                double absPhi = Math.Abs(phiEllipsoid);
                double lambdaEllipsoid;

                if (absPhi < HALFPI)
                {
                    double sinPhi = Math.Sin(phiEllipsoid);
                    lambdaEllipsoid = xUnit * Math.Sqrt(1d - (this.es * sinPhi * sinPhi)) / Math.Cos(phiEllipsoid);
                }
                else if ((absPhi - EPS10) < HALFPI)
                {
                    lambdaEllipsoid = 0d;
                }
                else
                {
                    throw new ArgumentException("Input data outside projection domain.");
                }

                x = Adjust_lon(this.centralMeridian + lambdaEllipsoid);
                y = phiEllipsoid;
                return;
            }

            double phiSphere = yUnit;
            double cosPhiSphere = Math.Cos(phiSphere);
            double lambdaSphere = Math.Abs(cosPhiSphere) <= EPS10 ? 0d : (xUnit / cosPhiSphere);

            x = Adjust_lon(this.centralMeridian + lambdaSphere);
            y = phiSphere;
        }
    }
}
