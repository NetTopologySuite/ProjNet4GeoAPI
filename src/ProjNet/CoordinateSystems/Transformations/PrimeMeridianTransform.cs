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

namespace ProjNet.CoordinateSystems.Transformations
{

    /// <summary>
    /// Adjusts target Prime Meridian
    /// </summary>
    [Serializable]
    internal class PrimeMeridianTransform : MathTransform
    {
        #region class variables

        private bool _isInverted;
        private readonly PrimeMeridian _source;
        private readonly PrimeMeridian _target;
        #endregion class variables

        #region constructors & finalizers
        /// <summary>
        /// Creates instance prime meridian transform
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public PrimeMeridianTransform(PrimeMeridian source, PrimeMeridian target)
        {
            if (!source.AngularUnit.EqualParams(target.AngularUnit))
            {
                throw new NotImplementedException("The method or operation is not implemented.");  
            }
            _source = source;
            _target = target;            
        }


        #endregion constructors & finalizers

        #region public properties
        /// <summary>
        /// Gets a Well-Known text representation of this affine math transformation.
        /// </summary>
        /// <value></value>
        public override string WKT
        {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }
        /// <summary>
        /// Gets an XML representation of this affine transformation.
        /// </summary>
        /// <value></value>
        public override string XML
        {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Gets the dimension of input points.
        /// </summary>
        public override int DimSource { get { return 3; } }

        /// <summary>
        /// Gets the dimension of output points.
        /// </summary>
        public override int DimTarget { get { return 3; } }
        #endregion public properties

        #region public methods

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            return new PrimeMeridianTransform(_target, _source);
        }

        /// <inheritdoc />
        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            if (_isInverted)
                x += _target.Longitude - _source.Longitude;
            else
                x += _source.Longitude - _target.Longitude;
        }

        /// <inheritdoc />
        protected sealed override void TransformCore(Span<double> xs, Span<double> ys, Span<double> zs,
            int strideX, int strideY, int strideZ)
        {
            double addend = _isInverted
                ? _target.Longitude - _source.Longitude
                : _source.Longitude - _target.Longitude;
            AddInPlace(xs, strideX, addend);
        }

        /// <inheritdoc />
        public override void Invert()
        {
            _isInverted = !_isInverted;
        }

        #endregion public methods
    }
}
