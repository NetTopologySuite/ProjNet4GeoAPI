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
namespace ProjNet.CoordinateSystems.Transformations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// <para>Latitude, Longitude and ellipsoidal height in terms of a 3-dimensional geographic system
    /// may by expressed in terms of a geocentric (earth centered) Cartesian coordinate reference system
    /// X, Y, Z with the Z axis corresponding to the earth's rotation axis positive northwards, the X
    /// axis through the intersection of the prime meridian and equator, and the Y axis through
    /// the intersection of the equator with longitude 90 degrees east. The geographic and geocentric
    /// systems are based on the same geodetic datum.</para>
    /// <para>Geocentric coordinate reference systems are conventionally taken to be defined with the X
    /// axis through the intersection of the Greenwich meridian and equator. This requires that the equivalent
    /// geographic coordinate reference systems based on a non-Greenwich prime meridian should first be
    /// transformed to their Greenwich equivalent. Geocentric coordinates X, Y and Z take their units from
    /// the units of the ellipsoid axes (a and b). As it is conventional for X, Y and Z to be in metres,
    /// if the ellipsoid axis dimensions are given in another linear unit they should first be converted
    /// to metres.</para>
    /// </remarks>
    [Serializable]
    internal class GeocentricTransform : MathTransform
    {
        private const double COS67P5 = 0.38268343236508977;    /* cosine of 67.5 degrees */
        private const double ADC = 1.0026000;                  /* Toms region 1 constant */

        /// <summary>
        /// Indicates whether this instance runs in inverse mode.
        /// </summary>
        private bool isInverse;

        /// <summary>
        /// Cached inverse transform.
        /// </summary>
        private MathTransform inverse;

        /// <summary>
        /// Eccentricity squared : (a^2 - b^2)/a^2.
        /// </summary>
        private readonly double es;

        /// <summary>
        /// major axis.
        /// </summary>
        private readonly double semiMajor;

        /// <summary>
        /// Minor axis.
        /// </summary>
        private readonly double semiMinor;
        /*
        private double ab;              // Semi_major / semi_minor
        private double ba;              // Semi_minor / semi_major
         */
        private readonly double ses;             // Second eccentricity squared : (a^2 - b^2)/b^2

        /// <summary>
        /// Projection parameters used to initialize the transform.
        /// </summary>
        private List<ProjectionParameter> parameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeocentricTransform"/> class.
        /// Initializes a geocentric projection object.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
        public GeocentricTransform(List<ProjectionParameter> parameters, bool isInverse) : this(parameters)
        {
            this.isInverse = isInverse;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeocentricTransform"/> class.
        /// Initializes a geocentric projection object.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        internal GeocentricTransform(List<ProjectionParameter> parameters)
        {
            this.parameters = parameters;
            this.semiMajor = this.parameters.Find(delegate (ProjectionParameter par)
            {
                // Do not remove the following lines containing "_Parameters = _Parameters;"
                // There is an issue deploying code with anonymous delegates to
                // SQLCLR because they're compiled using a writable static field
                // (which is not allowed in SQLCLR SAFE mode).
                // To workaround this, we will use a harmless reference to the
                // _Parameters field inside the anonymous delegate code making
                // the compiler generates a private nested class with a function
                // that is used as the delegate.
                // For details, see http://www.hedgate.net/articles/2006/01/27/troubles-with-shared-state-and-anonymous-delegates-in-sqlclr
#pragma warning disable 1717
                this.parameters = this.parameters;
#pragma warning restore 1717

                return par.Name.Equals("semi_major", StringComparison.OrdinalIgnoreCase);
            }).Value;

            this.semiMinor = this.parameters.Find(delegate (ProjectionParameter par)
            {
#pragma warning disable 1717
                this.parameters = this.parameters; // See explanation above.
#pragma warning restore 1717
                return par.Name.Equals("semi_minor", StringComparison.OrdinalIgnoreCase);
            }).Value;

            this.es = 1.0 - ((this.semiMinor * this.semiMinor) / (this.semiMajor * this.semiMajor)); // e^2
            this.ses = (Math.Pow(this.semiMajor, 2) - Math.Pow(this.semiMinor, 2)) / Math.Pow(this.semiMinor, 2);

            // ba = _semiMinor / _semiMajor;
            // ab = _semiMajor / _semiMinor;
        }

        /// <inheritdoc/>
        public override int DimSource
        {
            get { return 3; }
        }

        /// <inheritdoc/>
        public override int DimTarget
        {
            get { return 3; }
        }

        /// <summary>
        /// Gets a Well-Known text representation of this object.
        /// </summary>
        /// <value>The value.</value>
        public override string WKT
        {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        /// <value>The value.</value>
        public override string XML
        {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Returns the inverse of this conversion.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current conversion.</returns>
        public override MathTransform Inverse()
        {
            if (this.inverse == null)
            {
                this.inverse = new GeocentricTransform(this.parameters, !this.isInverse);
            }

            return this.inverse;
        }

        /// <summary>
        /// Converts a point (lon, lat, z) in degrees to (x, y, z) in meters.
        /// </summary>
        /// <param name="lon">The longitude in degree.</param>
        /// <param name="lat">The latitude in degree.</param>
        /// <param name="z">The z-ordinate value.</param>
        private void DegreesToMeters(ref double lon, ref double lat, ref double z)
        {
            lon = DegreesToRadians(lon);
            lat = DegreesToRadians(lat);
            z = double.IsNaN(z) ? 0 : z;

            double v = this.semiMajor / Math.Sqrt(1 - (this.es * Math.Pow(Math.Sin(lat), 2)));
            double x = (v + z) * Math.Cos(lat) * Math.Cos(lon);
            double y = (v + z) * Math.Cos(lat) * Math.Sin(lon);
            z = (((1 - this.es) * v) + z) * Math.Sin(lat);

            lon = x;
            lat = y;
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
        /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
        /// <param name="z">The z-ordinate value.</param>
        private void MetersToDegrees(ref double x, ref double y, ref double z)
        {
            bool at_Pole = false; // indicates whether location is in polar region */

            double lon;
            double lat = 0;
            double height;
            if (x != 0.0)
            {
                lon = Math.Atan2(y, x);
            }
            else
            {
                if (y > 0)
                {
                    lon = Math.PI / 2;
                }
                else if (y < 0)
                {
                    lon = -Math.PI * 0.5;
                }
                else
                {
                    at_Pole = true;
                    lon = 0.0;
                    if (z > 0.0)
                    {
                        /* north pole */
                        lat = Math.PI * 0.5;
                    }
                    else if (z < 0.0)
                    {
                        /* south pole */
                        lat = -Math.PI * 0.5;
                    }
                    else
                    {
                        /* center of earth */
                        lon = RadiansToDegrees(lon);
                        lat = RadiansToDegrees(Math.PI * 0.5);
                        x = lon;
                        y = lat;
                        z = -this.semiMinor;
                        return;
                    }
                }
            }

            double w2 = (x * x) + (y * y); // Square of distance from Z axis
            double w = Math.Sqrt(w2); // distance from Z axis
            double t0 = z * ADC; // initial estimate of vertical component
            double s0 = Math.Sqrt((t0 * t0) + w2); // initial estimate of horizontal component
            double sin_B0 = t0 / s0; // sin(B0), B0 is estimate of Bowring aux variable
            double cos_B0 = w / s0; // cos(B0)
            double sin3_B0 = Math.Pow(sin_B0, 3);
            double t1 = z + (this.semiMinor * this.ses * sin3_B0); // corrected estimate of vertical component
            double sum = w - (this.semiMajor * this.es * cos_B0 * cos_B0 * cos_B0); // numerator of cos(phi1)
            double s1 = Math.Sqrt((t1 * t1) + (sum * sum)); // corrected estimate of horizontal component
            double sin_p1 = t1 / s1; // sin(phi1), phi1 is estimated latitude
            double cos_p1 = sum / s1; // cos(phi1)
            double rn = this.semiMajor / Math.Sqrt(1.0 - (this.es * sin_p1 * sin_p1)); // Earth radius at location
            if (cos_p1 >= COS67P5)
            {
                height = (w / cos_p1) - rn;
            }
            else if (cos_p1 <= -COS67P5)
            {
                height = (w / -cos_p1) - rn;
            }
            else
            {
                height = (z / sin_p1) + (rn * (this.es - 1.0));
            }

            if (!at_Pole)
            {
                lat = Math.Atan(sin_p1 / cos_p1);
            }

            x = RadiansToDegrees(lon);
            y = RadiansToDegrees(lat);
            z = height;
        }

        /// <inheritdoc/>
        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            if (this.isInverse)
            {
                this.MetersToDegrees(ref x, ref y, ref z);
            }
            else
            {
                this.DegreesToMeters(ref x, ref y, ref z);
            }
        }

        /// <summary>
        /// Reverses the transformation.
        /// </summary>
        public override void Invert()
        {
            this.isInverse = !this.isInverse;
        }
    }
}
