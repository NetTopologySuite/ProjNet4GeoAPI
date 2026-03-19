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
    internal class BonneProjection : MapProjection
    {
        private readonly double radius;
        private readonly double inverseRadius;
        private readonly double standardParallel;
        private readonly double sineStandardParallel;
        private readonly double cotStandardParallel;
        private readonly double meridianDistanceAtStandardParallel;
        private readonly double reducedCosphiOverSinphiAtStandardParallel;
        private readonly bool isEllipsoidal;

        public BonneProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public BonneProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Bonne";
            this.radius = this.semiMajor * this.scale_factor;
            this.inverseRadius = 1d / this.radius;
            this.standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("lat_1", RadiansToDegrees(this.lat_origin), "standard_parallel_1"));

            if (Math.Abs(this.standardParallel) <= EPS10)
            {
                throw new ArgumentException("Invalid value for lat_1: |lat_1| should be > 0.");
            }

            this.sineStandardParallel = Math.Sin(this.standardParallel);
            this.isEllipsoidal = this.es > 0d;

            if (this.isEllipsoidal)
            {
                double cosStandardParallel = Math.Cos(this.standardParallel);
                double denominator = Math.Sqrt(1d - (this.es * this.sineStandardParallel * this.sineStandardParallel)) * this.sineStandardParallel;
                this.reducedCosphiOverSinphiAtStandardParallel = cosStandardParallel / denominator;
                this.meridianDistanceAtStandardParallel = this.Mlfn(this.standardParallel, this.sineStandardParallel, cosStandardParallel);
                this.cotStandardParallel = 0d;
                return;
            }

            this.cotStandardParallel = (Math.Abs(Math.Abs(this.standardParallel) - HALF_PI) <= EPS10) ? 0d : (1d / Math.Tan(this.standardParallel));
            this.meridianDistanceAtStandardParallel = 0d;
            this.reducedCosphiOverSinphiAtStandardParallel = 0d;
        }

        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new BonneProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.central_meridian);
            double phi = lat;

            if (!this.isEllipsoidal)
            {
                double rhoSphere = this.cotStandardParallel + this.standardParallel - phi;
                if (Math.Abs(rhoSphere) <= EPS10)
                {
                    lon = 0d;
                    lat = 0d;
                    return;
                }

                double angularTermSphere = lambda * Math.Cos(phi) / rhoSphere;
                lon = this.radius * rhoSphere * Math.Sin(angularTermSphere);
                lat = this.radius * (this.cotStandardParallel - (rhoSphere * Math.Cos(angularTermSphere)));
                return;
            }

            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);
            double rho = this.reducedCosphiOverSinphiAtStandardParallel + this.meridianDistanceAtStandardParallel - this.Mlfn(phi, sinPhi, cosPhi);
            if (Math.Abs(rho) <= EPS10)
            {
                lon = 0d;
                lat = 0d;
                return;
            }

            double angularDenominatorEllipsoid = rho * Math.Sqrt(1d - (this.es * sinPhi * sinPhi));
            double angularTermEllipsoid = (cosPhi * lambda) / angularDenominatorEllipsoid;
            lon = this.radius * rho * Math.Sin(angularTermEllipsoid);
            lat = this.radius * (this.reducedCosphiOverSinphiAtStandardParallel - (rho * Math.Cos(angularTermEllipsoid)));
        }

        protected override void MetersToRadians(ref double x, ref double y)
        {
            double xUnit = x * this.inverseRadius;
            double yUnit = y * this.inverseRadius;

            if (!this.isEllipsoidal)
            {
                double translatedY = this.cotStandardParallel - yUnit;
                double rhoSphere = Sign(this.standardParallel) * Hypot(xUnit, translatedY);
                double phiSphere = this.cotStandardParallel + this.standardParallel - rhoSphere;
                double absPhiSphere = Math.Abs(phiSphere);
                if (absPhiSphere > HALF_PI)
                {
                    throw new ArgumentException("Input data outside projection domain.");
                }

                double lambdaSphere;
                if (HALF_PI - absPhiSphere <= EPS10)
                {
                    lambdaSphere = 0d;
                }
                else
                {
                    double scale = rhoSphere / Math.Cos(phiSphere);
                    lambdaSphere = this.standardParallel > 0d
                        ? scale * Math.Atan2(xUnit, translatedY)
                        : scale * Math.Atan2(-xUnit, -translatedY);
                }

                x = Adjust_lon(this.central_meridian + lambdaSphere);
                y = phiSphere;
                return;
            }

            double translatedEllipsoidalY = this.reducedCosphiOverSinphiAtStandardParallel - yUnit;
            double rho = Sign(this.standardParallel) * Hypot(xUnit, translatedEllipsoidalY);
            double phi = this.Inv_mlfn(this.reducedCosphiOverSinphiAtStandardParallel + this.meridianDistanceAtStandardParallel - rho);
            double absPhi = Math.Abs(phi);

            double lambda = 0d;
            if (absPhi < HALF_PI)
            {
                double sinPhi = Math.Sin(phi);
                double scale = (rho * Math.Sqrt(1d - (this.es * sinPhi * sinPhi))) / Math.Cos(phi);
                lambda = this.standardParallel > 0d
                    ? scale * Math.Atan2(xUnit, translatedEllipsoidalY)
                    : scale * Math.Atan2(-xUnit, -translatedEllipsoidalY);
            }
            else if ((absPhi - HALF_PI) > EPS10)
            {
                throw new ArgumentException("Input data outside projection domain.");
            }

            x = Adjust_lon(this.central_meridian + lambda);
            y = phi;
        }
    }
}
