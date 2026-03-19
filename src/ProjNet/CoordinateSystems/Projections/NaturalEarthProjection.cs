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
    internal class NaturalEarthProjection : MapProjection
    {
        private const int Iterations = 12;

        private readonly double radius;
        private readonly double inverseRadius;

        public NaturalEarthProjection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public NaturalEarthProjection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Natural_Earth";
            this.radius = this.semiMajor * this.scaleFactor;
            this.inverseRadius = 1.0 / this.radius;
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new NaturalEarthProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.centralMeridian);
            double phi = lat;
            double phi2 = phi * phi;
            double phi4 = phi2 * phi2;
            double phi6 = phi4 * phi2;
            double phi8 = phi4 * phi4;
            double phi10 = phi8 * phi2;
            double phi12 = phi10 * phi2;

            double xScale = 0.8707 - (0.131979 * phi2) - (0.013791 * phi4) + (0.003971 * phi10) - (0.001529 * phi12);
            double yScale = 1.007226 + (0.015085 * phi2) - (0.044475 * phi6) + (0.028874 * phi8) - (0.005916 * phi10);

            lon = this.radius * lambda * xScale;
            lat = this.radius * phi * yScale;
        }

        /// <inheritdoc />
        protected override void MetersToRadians(ref double x, ref double y)
        {
            double yy = y * this.inverseRadius;
            double phi = yy;

            for (int i = 0; i < Iterations; i++)
            {
                double phi2 = phi * phi;
                double phi4 = phi2 * phi2;
                double phi6 = phi4 * phi2;
                double phi8 = phi4 * phi4;
                double phi10 = phi8 * phi2;

                double fy = (phi * (1.007226 + (0.015085 * phi2) - (0.044475 * phi6) + (0.028874 * phi8) - (0.005916 * phi10))) - yy;
                double fpy = 1.007226 + (3d * 0.015085 * phi2) - (7d * 0.044475 * phi6) + (9d * 0.028874 * phi8) - (11d * 0.005916 * phi10);

                double delta = fy / fpy;
                phi -= delta;
                if (Math.Abs(delta) < 1e-12)
                {
                    break;
                }
            }

            double phi2Final = phi * phi;
            double phi4Final = phi2Final * phi2Final;
            double phi10Final = phi4Final * phi4Final * phi2Final;
            double phi12Final = phi10Final * phi2Final;
            double xScaleFinal = 0.8707 - (0.131979 * phi2Final) - (0.013791 * phi4Final) + (0.003971 * phi10Final) - (0.001529 * phi12Final);

            x = Adjust_lon(this.centralMeridian + ((x * this.inverseRadius) / xScaleFinal));
            y = phi;
        }
    }
}
