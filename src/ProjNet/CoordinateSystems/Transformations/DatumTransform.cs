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
    /// Transformation for applying.
    /// </summary>
    [Serializable]
    internal class DatumTransform : MathTransform
    {
        private MathTransform inverse;
        private readonly Wgs84ConversionInfo toWgs94;
        readonly double[] v;

        private bool isInverse;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatumTransform"/> class.
        /// </summary>
        /// <param name="towgs84">The towgs84 parameter.</param>
        public DatumTransform(Wgs84ConversionInfo towgs84) : this(towgs84, false)
        {
        }

        private DatumTransform(Wgs84ConversionInfo towgs84, bool isInverse)
        {
            this.toWgs94 = towgs84;
            this.v = this.toWgs94.GetAffineTransform();
            this.isInverse = isInverse;
        }

        /// <summary>
        /// Gets a Well-Known text representation of this object.
        /// </summary>
        /// <value>The value.</value>
        public override string WKT
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets an XML representation of this object.
        /// </summary>
        /// <value>The value.</value>
        public override string XML
        {
            get { throw new NotImplementedException(); }
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
        /// Creates the inverse transform of this object.
        /// </summary>
        /// <returns>The transformation result.</returns>
        /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
        public override MathTransform Inverse()
        {
            if (this.inverse == null)
            {
                this.inverse = new DatumTransform(this.toWgs94, !this.isInverse);
            }

            return this.inverse;
        }

        /// <inheritdoc />
        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            if (this.isInverse)
            {
                (x, y, z) = this.ApplyInverted(x, y, z);
            }
            else
            {
                (x, y, z) = this.Apply(x, y, z);
            }
        }

        private (double x, double y, double z) Apply(double x, double y, double z)
        {
            return (
                x: (this.v[0] * (x - (this.v[3] * y) + (this.v[2] * z))) + this.v[4],
                y: (this.v[0] * ((this.v[3] * x) + y - (this.v[1] * z))) + this.v[5],
                z: (this.v[0] * ((-this.v[2] * x) + (this.v[1] * y) + z)) + this.v[6]);
        }

        private (double x, double y, double z) ApplyInverted(double x, double y, double z)
        {
            return (
                x: ((1 - (this.v[0] - 1)) * (x + (this.v[3] * y) - (this.v[2] * z))) - this.v[4],
                y: ((1 - (this.v[0] - 1)) * ((-this.v[3] * x) + y + (this.v[1] * z))) - this.v[5],
                z: ((1 - (this.v[0] - 1)) * ((this.v[2] * x) - (this.v[1] * y) + z)) - this.v[6]);
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
