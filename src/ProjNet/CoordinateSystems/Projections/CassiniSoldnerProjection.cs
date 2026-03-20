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
    internal class CassiniSoldnerProjection : MapProjection
    {
        // ReSharper disable InconsistentNaming
        private const double One6th = 0.16666666666666666666d;      // C1
        private const double One120th = 0.00833333333333333333d;    // C2
        private const double One24th = 0.04166666666666666666d;     // C3
        private const double One3rd = 0.33333333333333333333d;      // C4
        private const double One15th = 0.06666666666666666666d;     // C5

        // ReSharper restore InconsistentNaming
        private readonly double cFactor;
        private readonly double m0;
        private readonly double reciprocalSemiMajor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CassiniSoldnerProjection"/> class.
        /// </summary>
        /// <param name="parameters">Projection parameters.</param>
        public CassiniSoldnerProjection(IEnumerable<ProjectionParameter> parameters) : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassiniSoldnerProjection"/> class.
        /// </summary>
        /// <param name="parameters">Projection parameters.</param>
        /// <param name="inverse">Inverse transform instance when cloning.</param>
        public CassiniSoldnerProjection(IEnumerable<ProjectionParameter> parameters, CassiniSoldnerProjection inverse)
            : base(parameters, inverse)
        {
            this.Authority = "EPSG";
            this.AuthorityCode = 9806;
            this.Name = "Cassini_Soldner";

            this.cFactor = this.es / (1 - this.es);
            this.m0 = this.Mlfn(this.latOrigin, Math.Sin(this.latOrigin), Math.Cos(this.latOrigin));
            this.reciprocalSemiMajor = 1d / this.semiMajor;
        }

        /// <inheritdoc/>
        public override MathTransform Inverse()
        {
            if (this.inverse == null)
            {
                this.inverse = new CassiniSoldnerProjection(this.Parameters.ToProjectionParameter(), this);
            }

            return this.inverse;
        }

        // protected override double[] RadiansToMeters(double[] lonlat)
        // {
        //    var lambda = lonlat[0] - central_meridian;
        //    var phi = lonlat[1];

        // double sinPhi, cosPhi; // sin and cos value
        //    sincos(phi, out sinPhi, out cosPhi);

        // var y = mlfn(phi, sinPhi, cosPhi);
        //    var n = 1.0d / Math.Sqrt(1 - _es * sinPhi * sinPhi);
        //    var tn = Math.Tan(phi);
        //    var t = tn * tn;
        //    var a1 = lambda * cosPhi;
        //    var a2 = a1 * a1;
        //    var c = _cFactor * Math.Pow(cosPhi, 2.0d);

        // var x = n * a1 * (1.0d - a2 * t * (One6th - (8.0d - t + 8.0d * c) * a2 * One120th));
        //    y -= _m0 - n * tn * a2 * (0.5d + (5.0d - t + 6.0d * c) * a2 * One24th);

        // return lonlat.Length == 2
        //               ? new[] {_semiMajor*x, _semiMajor*y}
        //               : new[] {_semiMajor*x, _semiMajor*y, lonlat[2]};
        // }
        /// <inheritdoc/>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            double lambda = lon - this.centralMeridian;
            double phi = lat;

            double sinPhi, cosPhi; // sin and cos value
            Sincos(phi, out sinPhi, out cosPhi);

            double y = this.Mlfn(phi, sinPhi, cosPhi);
            double n = 1.0d / Math.Sqrt(1 - (this.es * sinPhi * sinPhi));
            double tn = Math.Tan(phi);
            double t = tn * tn;
            double a1 = lambda * cosPhi;
            double a2 = a1 * a1;
            double c = this.cFactor * Math.Pow(cosPhi, 2.0d);

            double x = n * a1 * (1.0d - (a2 * t * (One6th - ((8.0d - t + (8.0d * c)) * a2 * One120th))));
            y -= this.m0 - (n * tn * a2 * (0.5d + ((5.0d - t + (6.0d * c)) * a2 * One24th)));

            lon = x * this.semiMajor;
            lat = y * this.semiMajor;
        }

        // protected override double[] MetersToRadians(double[] p)
        // {

        // var x = p[0] * _reciprocalSemiMajor;
        //    var y = p[1] * _reciprocalSemiMajor;
        //    var phi1 = Phi1(_m0 + y);

        // var tn = Math.Tan(phi1);
        //    var t = tn * tn;
        //    var n = Math.Sin(phi1);
        //    var r = 1.0d / (1.0d - _es * n * n);
        //    n = Math.Sqrt(r);
        //    r *= (1.0d - _es) * n;
        //    var dd = x / n;
        //    var d2 = dd * dd;

        // var phi = phi1 - (n * tn / r) * d2 * (.5 - (1.0 + 3.0 * t) * d2 * One24th);
        //    var lambda = dd * (1.0 + t * d2 * (-One3rd + (1.0 + 3.0 * t) * d2 * One15th)) / Math.Cos(phi1);
        //    lambda = adjust_lon(lambda + central_meridian);

        // return p.Length == 2
        //               ? new[] {lambda, phi}
        //               : new[] {lambda, phi, p[2]};
        // }
        /// <inheritdoc/>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            x *= this.reciprocalSemiMajor;
            y *= this.reciprocalSemiMajor;
            double phi1 = this.Phi1(this.m0 + y);

            double tn = Math.Tan(phi1);
            double t = tn * tn;
            double n = Math.Sin(phi1);
            double r = 1.0d / (1.0d - (this.es * n * n));
            n = Math.Sqrt(r);
            r *= (1.0d - this.es) * n;
            double dd = x / n;
            double d2 = dd * dd;

            y = phi1 - ((n * tn / r) * d2 * (.5 - ((1.0 + (3.0 * t)) * d2 * One24th)));
            double lambda = dd * (1.0 + (t * d2 * (-One3rd + ((1.0 + (3.0 * t)) * d2 * One15th)))) / Math.Cos(phi1);
            x = Adjust_lon(lambda + this.centralMeridian);
        }

        private double Phi1(double arg)
        {
            const int maxIter = 10;
            const double eps = 1e-11;

            double k = 1.0d / (1.0d - this.es);

            double phi = arg;
            for (int i = maxIter; i > 0; --i)
            { // rarely goes over 2 iterations
                double sinPhi = Math.Sin(phi);
                double t = 1.0d - (this.es * sinPhi * sinPhi);
                t = (this.Mlfn(phi, sinPhi, Math.Cos(phi)) - arg) * (t * Math.Sqrt(t)) * k;
                phi -= t;
                if (Math.Abs(t) < eps)
                {
                    return phi;
                }
            }

            throw new ArgumentException("Convergence error.");
        }
    }
}


