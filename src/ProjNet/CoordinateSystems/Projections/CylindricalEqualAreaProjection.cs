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
    internal class CylindricalEqualAreaProjection : MapProjection
    {
        private readonly double radius;
        private readonly double inverseRadius;
        private readonly double cosStandardParallel;

        public CylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public CylindricalEqualAreaProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Cylindrical_Equal_Area";
            this.radius = this.semiMajor * this.scaleFactor;
            this.inverseRadius = 1d / this.radius;

            double standardParallel = DegreesToRadians(this.Parameters.GetOptionalParameterValue("standard_parallel_1", 0d, "lat_ts"));
            this.cosStandardParallel = Math.Cos(standardParallel);
            if (Math.Abs(this.cosStandardParallel) <= EPS10)
            {
                throw new ArgumentException("The standard parallel cannot be at the poles.");
            }
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new CylindricalEqualAreaProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.centralMeridian);
            lon = this.radius * lambda * this.cosStandardParallel;
            lat = this.radius * Math.Sin(lat) / this.cosStandardParallel;
        }

        /// <inheritdoc />
        protected override void MetersToRadians(ref double x, ref double y)
        {
            x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / this.cosStandardParallel));
            y = Math.Asin(Clamp((y * this.cosStandardParallel) * this.inverseRadius, -1d, 1d));
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
