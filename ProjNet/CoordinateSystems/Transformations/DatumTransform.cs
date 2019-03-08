// Copyright 2006 - Morten Nielsen (www.iter.dk)
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
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;

namespace ProjNet.CoordinateSystems.Transformations
{
	/// <summary>
	/// Transformation for applying 
    /// </summary>
    [Serializable] 
    internal class DatumTransform : MathTransform
	{
		protected IMathTransform _inverse;
		private readonly Wgs84ConversionInfo _toWgs94;
		double[] v;

		private bool _isInverse;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatumTransform"/> class.
        /// </summary>
        /// <param name="towgs84"></param>
        public DatumTransform(Wgs84ConversionInfo towgs84) : this(towgs84,false)
		{
		}

		private DatumTransform(Wgs84ConversionInfo towgs84, bool isInverse)
		{
			_toWgs94 = towgs84;
			v = _toWgs94.GetAffineTransform();
			_isInverse = isInverse;
		}
        /// <summary>
        /// Gets a Well-Known text representation of this object.
        /// </summary>
        /// <value></value>
		public override string WKT
		{
			get { throw new NotImplementedException(); }
		}

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        /// <value></value>
		public override string XML
		{
			get { throw new NotImplementedException(); }
		}

        public override int DimSource
        {
            get {  return 3; }
        }

        public override int DimTarget
        {
            get { return 3; }
        }

        /// <summary>
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
		public override IMathTransform Inverse()
		{
			if (_inverse == null)
				_inverse = new DatumTransform(_toWgs94,!_isInverse);
			return _inverse;
		}

        public override (double x, double y, double z) Transform(double x, double y, double z)
        {
            if (_isInverse)
            {
                return ApplyInverted(x, y, z);
            }
            else
            {
                return Apply(x, y, z);
            }
        }

        private (double x, double y, double z) Apply(double x, double y, double z)
        {
            return (
                x: v[0] * (x - v[3] * y + v[2] * z) + v[4],
                y: v[0] * (v[3] * x + y - v[1] * z) + v[5],
                z: v[0] * (-v[2] * x + v[1] * y + z) + v[6]);
        }

        private (double x, double y, double z) ApplyInverted(double x, double y, double z)
        {
            return (
                x: (1 - (v[0] - 1)) * (x + v[3] * y - v[2] * z) - v[4],
                y: (1 - (v[0] - 1)) * (-v[3] * x + y + v[1] * z) - v[5],
                z: (1 - (v[0] - 1)) * (v[2] * x - v[1] * y + z) - v[6]);
        }

        /// <summary>
        /// Reverses the transformation
        /// </summary>
		public override void Invert()
		{
			_isInverse = !_isInverse;
		}
	}
}
