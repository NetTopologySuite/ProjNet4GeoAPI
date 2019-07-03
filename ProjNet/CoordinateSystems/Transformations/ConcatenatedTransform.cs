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
using System.Linq;

namespace ProjNet.CoordinateSystems.Transformations
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable] 
    internal class ConcatenatedTransform : MathTransform, ICoordinateTransformationCore
	{
        /// <summary>
        /// 
        /// </summary>
        private MathTransform _inverse;
        private readonly List<ICoordinateTransformationCore> _coordinateTransformationList;

        /// <summary>
        /// 
        /// </summary>
		public ConcatenatedTransform()  
            {  _coordinateTransformationList = new List<ICoordinateTransformationCore>();}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transformList"></param>
        public ConcatenatedTransform(IEnumerable<ICoordinateTransformationCore> transformList)
            : this()
		{
			_coordinateTransformationList.AddRange(transformList);
		}


        /// <summary>
        /// 
        /// </summary>
		public IList<ICoordinateTransformationCore> CoordinateTransformationList
		{
			get { return _coordinateTransformationList; }
            /*
            set
			{
				_coordinateTransformationList = value;
				_inverse = null;
			}
             */
		}



        public override int DimSource
        {
            get { return (_coordinateTransformationList[0]).SourceCS.Dimension; }
        }

        public override int DimTarget
        {
            get { return _coordinateTransformationList[_coordinateTransformationList.Count-1].TargetCS.Dimension; }
        }


        /// <inheritdoc />
        public override void Transform(ref double x, ref double y, ref double z)
        {
            foreach (var ctc in _coordinateTransformationList)
            {
                if (ctc is CoordinateTransformation ct)
                    ct.MathTransform.Transform(ref x, ref y, ref z);
                else if (ctc is ConcatenatedTransform cct)
                    cct.Transform(ref x, ref y, ref z);
            }
        }

		/// <summary>
		/// Returns the inverse of this conversion.
		/// </summary>
		/// <returns>IMathTransform that is the reverse of the current conversion.</returns>
		public override MathTransform Inverse()
		{
			if (_inverse == null)
			{
				_inverse = Clone();
				_inverse.Invert();
			}
			return _inverse;
		}
		
		/// <summary>
		/// Reverses the transformation
		/// </summary>
		public override void Invert()
		{
			_coordinateTransformationList.Reverse();
            foreach (var ic in _coordinateTransformationList)
            {
                if (ic is CoordinateTransformation ct)
                    ct.MathTransform.Invert();
                else if (ic is ConcatenatedTransform cct)
                    cct.Invert();
            }
		}

		public ConcatenatedTransform Clone()
		{
			var clonedList = new List<ICoordinateTransformationCore>(_coordinateTransformationList.Count);
			foreach (var ct in _coordinateTransformationList)
				clonedList.Add(CloneCoordinateTransformation(ct));
			return new ConcatenatedTransform(clonedList);
		}

        private static readonly CoordinateTransformationFactory CoordinateTransformationFactory =
                new CoordinateTransformationFactory();

        private static ICoordinateTransformationCore CloneCoordinateTransformation(ICoordinateTransformationCore ict)
        {
            return CoordinateTransformationFactory.CreateFromCoordinateSystems(ict.SourceCS, ict.TargetCS);
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

        public CoordinateSystem SourceCS { get => CoordinateTransformationList[0].SourceCS; }

        public CoordinateSystem TargetCS { get => CoordinateTransformationList[CoordinateTransformationList.Count-1].TargetCS; }
    }
}
