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

    /// <summary>
    /// Adjusts target Prime Meridian.
    /// </summary>
    [Serializable]
    internal class PrimeMeridianTransform : MathTransform
    {
        private readonly PrimeMeridian source;
        private readonly PrimeMeridian target;
        private bool isInverted;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimeMeridianTransform"/> class.
        /// Creates instance prime meridian transform.
        /// </summary>
        /// <param name="source">The source parameter.</param>
        /// <param name="target">The target parameter.</param>
        public PrimeMeridianTransform(PrimeMeridian source, PrimeMeridian target)
        {
            if (!source.AngularUnit.EqualParams(target.AngularUnit))
            {
                throw new NotImplementedException("The method or operation is not implemented.");
            }

            this.source = source;
            this.target = target;
        }

        /// <summary>
        /// Gets a Well-Known text representation of this affine math transformation.
        /// </summary>
        /// <value>The value.</value>
        public override string WKT
        {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Gets an XML representation of this affine transformation.
        /// </summary>
        /// <value>The value.</value>
        public override string XML
        {
            get { throw new NotImplementedException("The method or operation is not implemented."); }
        }

        /// <summary>
        /// Gets the dimension of input points.
        /// </summary>
        public override int DimSource
        {
            get { return 3; }
        }

        /// <summary>
        /// Gets the dimension of output points.
        /// </summary>
        public override int DimTarget
        {
            get { return 3; }
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            return new PrimeMeridianTransform(this.target, this.source);
        }

        /// <inheritdoc />
        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            if (this.isInverted)
            {
                x += this.target.Longitude - this.source.Longitude;
            }
            else
            {
                x += this.source.Longitude - this.target.Longitude;
            }
        }

        /// <inheritdoc />
        protected sealed override void TransformCore(
            Span<double> xs,
            Span<double> ys,
            Span<double> zs,
            int strideX,
            int strideY,
            int strideZ)
        {
            double addend = this.isInverted
                ? this.target.Longitude - this.source.Longitude
                : this.source.Longitude - this.target.Longitude;
            AddInPlace(xs, strideX, addend);
        }

        /// <inheritdoc />
        public override void Invert()
        {
            this.isInverted = !this.isInverted;
        }
    }
}
