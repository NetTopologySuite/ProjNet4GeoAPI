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

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;

namespace ProjNet.CoordinateSystems.Transformations
{
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
		private const double COS_67P5 = 0.38268343236508977;    /* cosine of 67.5 degrees */
		private const double AD_C = 1.0026000;                  /* Toms region 1 constant */

        /// <summary>
        /// 
        /// </summary>
        private bool _isInverse;
        /// <summary>
        /// 
        /// </summary>
        private MathTransform _inverse;

        /// <summary>
        /// Eccentricity squared : (a^2 - b^2)/a^2
        /// </summary>
        private readonly double _es;

        /// <summary>
        /// major axis
        /// </summary>
		private readonly double _semiMajor;

        /// <summary>
        /// Minor axis
        /// </summary>
		private readonly double _semiMinor;
        /*
        private double ab;				// Semi_major / semi_minor
		private double ba;				// Semi_minor / semi_major
         */
        private readonly double _ses;             // Second eccentricity squared : (a^2 - b^2)/b^2    

        /// <summary>
        /// 
        /// </summary>
        private List<ProjectionParameter> _parameters;


		/// <summary>
		/// Initializes a geocentric projection object
		/// </summary>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		/// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
		public GeocentricTransform(List<ProjectionParameter> parameters, bool isInverse) : this(parameters)
		{
			_isInverse = isInverse;
		}

		/// <summary>
		/// Initializes a geocentric projection object
		/// </summary>
		/// <param name="parameters">List of parameters to initialize the projection.</param>
		internal GeocentricTransform(List<ProjectionParameter> parameters)
		{
			_parameters = parameters;
			_semiMajor = _parameters.Find(delegate(ProjectionParameter par)
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
				_parameters = _parameters;
#pragma warning restore 1717

				return par.Name.Equals("semi_major", StringComparison.OrdinalIgnoreCase);
			}).Value;

			_semiMinor = _parameters.Find(delegate(ProjectionParameter par)
			{
#pragma warning disable 1717
				_parameters = _parameters; // See explanation above.
#pragma warning restore 1717
				return par.Name.Equals("semi_minor", StringComparison.OrdinalIgnoreCase);
			}).Value;

			_es = 1.0 - (_semiMinor * _semiMinor) / (_semiMajor * _semiMajor); //e^2
			_ses = (Math.Pow(_semiMajor, 2) - Math.Pow(_semiMinor, 2)) / Math.Pow(_semiMinor, 2);
			//ba = _semiMinor / _semiMajor;
			//ab = _semiMajor / _semiMinor;
		}

        public override int DimSource
        {
            get { return 3; }
        }

        public override int DimTarget
        {
            get { return 3; }
        }

        /// <summary>
		/// Returns the inverse of this conversion.
		/// </summary>
		/// <returns>IMathTransform that is the reverse of the current conversion.</returns>
		public override MathTransform Inverse()
		{
			if (_inverse == null)
				_inverse = new GeocentricTransform(this._parameters, !_isInverse);
			return _inverse;
		}

        /// <summary>
        /// Converts a point (lon, lat, z) in degrees to (x, y, z) in meters
        /// </summary>
        /// <param name="lon">The longitude in degree</param>
        /// <param name="lat">The latitude in degree</param>
        /// <param name="z">The z-ordinate value</param>
        private void DegreesToMeters(ref double lon, ref double lat, ref double z)
        {
            lon = DegreesToRadians(lon);
            lat = DegreesToRadians(lat);
            z = double.IsNaN(z) ? 0 : z;

            double v = _semiMajor / Math.Sqrt(1 - _es * Math.Pow(Math.Sin(lat), 2));
            double x = (v + z) * Math.Cos(lat) * Math.Cos(lon);
            double y = (v + z) * Math.Cos(lat) * Math.Sin(lon);
            z = ((1 - _es) * v + z) * Math.Sin(lat);

            lon = x;
            lat = y;
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="x">The x-ordinate when entering, the longitude value upon exit.</param>
        /// <param name="y">The y-ordinate when entering, the latitude value upon exit.</param>
        /// <param name="z">The z-ordinate value</param>
        private void MetersToDegrees(ref double x, ref double y, ref double z)
        {
            bool At_Pole = false; // indicates whether location is in polar region */

            double lon;
            double lat = 0;
            double Height;
            if (x != 0.0)
                lon = Math.Atan2(y, x);
            else
            {
                if (y > 0)
                    lon = Math.PI / 2;
                else if (y < 0)
                    lon = -Math.PI * 0.5;
                else
                {
                    At_Pole = true;
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
                        z = -_semiMinor;
                        return;
                    }
                }
            }

            double W2 = x * x + y * y; // Square of distance from Z axis
            double W = Math.Sqrt(W2); // distance from Z axis
            double T0 = z * AD_C; // initial estimate of vertical component
            double S0 = Math.Sqrt(T0 * T0 + W2); //initial estimate of horizontal component
            double Sin_B0 = T0 / S0; //sin(B0), B0 is estimate of Bowring aux variable
            double Cos_B0 = W / S0; //cos(B0)
            double Sin3_B0 = Math.Pow(Sin_B0, 3);
            double T1 = z + _semiMinor * _ses * Sin3_B0; //corrected estimate of vertical component
            double Sum = W - _semiMajor * _es * Cos_B0 * Cos_B0 * Cos_B0; //numerator of cos(phi1)
            double S1 = Math.Sqrt(T1 * T1 + Sum * Sum); //corrected estimate of horizontal component
            double Sin_p1 = T1 / S1; //sin(phi1), phi1 is estimated latitude
            double Cos_p1 = Sum / S1; //cos(phi1)
            double Rn = _semiMajor / Math.Sqrt(1.0 - _es * Sin_p1 * Sin_p1); //Earth radius at location
            if (Cos_p1 >= COS_67P5)
                Height = W / Cos_p1 - Rn;
            else if (Cos_p1 <= -COS_67P5)
                Height = W / -Cos_p1 - Rn;
            else Height = z / Sin_p1 + Rn * (_es - 1.0);
            if (!At_Pole)
                lat = Math.Atan(Sin_p1 / Cos_p1);

            x = RadiansToDegrees(lon);
            y = RadiansToDegrees(lat);
            z = Height;
        }

        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            if (_isInverse)
                MetersToDegrees(ref x, ref y, ref z);
            else
                DegreesToMeters(ref x, ref y, ref z);
        }

        /// <summary>
		/// Reverses the transformation
		/// </summary>
		public override void Invert()
		{
			_isInverse = !_isInverse;
		}

        /// <summary>
        /// Gets a Well-Known text representation of this object.
        /// </summary>
        /// <value></value>
		public override string WKT
		{
			get { throw new NotImplementedException("The method or operation is not implemented."); }
		}
        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        /// <value></value>
		public override string XML
		{
			get { throw new NotImplementedException("The method or operation is not implemented."); }
		}
	}
}
