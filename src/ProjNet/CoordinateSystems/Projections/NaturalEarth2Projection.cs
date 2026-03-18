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
    internal class NaturalEarth2Projection : MapProjection
    {
        private const int Iterations = 12;

        private readonly double radius;
        private readonly double inverseRadius;

        public NaturalEarth2Projection(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        public NaturalEarth2Projection(IEnumerable<ProjectionParameter> parameters, MapProjection inverse)
            : base(parameters, inverse)
        {
            this.Name = "Natural_Earth_2";
            this.radius = this.semiMajor * this.scale_factor;
            this.inverseRadius = 1.0 / this.radius;
        }

        public override MathTransform Inverse()
        {
            if (this.inverse is null)
            {
                this.inverse = new NaturalEarth2Projection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = Adjust_lon(lon - this.central_meridian);
            double phi = lat;
            double phi2 = phi * phi;
            double phi4 = phi2 * phi2;
            double phi6 = phi4 * phi2;
            double phi8 = phi4 * phi4;
            double phi10 = phi8 * phi2;
            double phi12 = phi10 * phi2;
            double phi14 = phi12 * phi2;
            double phi16 = phi8 * phi8;

            double xScale = 0.84719 - (0.13063 * phi2) - (0.04515 * phi12) + (0.05494 * phi14) - (0.02326 * phi16) + (0.00331 * phi16 * phi2);
            double yScale = 1.01183 - (0.02625 * phi8) + (0.01926 * phi10) - (0.00396 * phi12);

            lon = this.radius * lambda * xScale;
            lat = this.radius * phi * yScale;
        }

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
                double phi12 = phi10 * phi2;

                double fy = (phi * (1.01183 - (0.02625 * phi8) + (0.01926 * phi10) - (0.00396 * phi12))) - yy;
                double fpy = 1.01183 - (9d * 0.02625 * phi8) + (11d * 0.01926 * phi10) - (13d * 0.00396 * phi12);

                double delta = fy / fpy;
                phi -= delta;
                if (Math.Abs(delta) < 1e-12)
                {
                    break;
                }
            }

            double phi2Final = phi * phi;
            double phi4Final = phi2Final * phi2Final;
            double phi8Final = phi4Final * phi4Final;
            double phi12Final = phi8Final * phi4Final;
            double phi14Final = phi12Final * phi2Final;
            double phi16Final = phi8Final * phi8Final;
            double phi18Final = phi16Final * phi2Final;

            double xScaleFinal = 0.84719 - (0.13063 * phi2Final) - (0.04515 * phi12Final) + (0.05494 * phi14Final) - (0.02326 * phi16Final) + (0.00331 * phi18Final);

            x = Adjust_lon(this.central_meridian + ((x * this.inverseRadius) / xScaleFinal));
            y = phi;
        }
    }
}
