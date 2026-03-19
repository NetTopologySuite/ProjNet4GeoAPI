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
    /// Represents the documented type.
    /// </summary>
    [Serializable]
    internal sealed class UnitConvertMathTransform : MathTransform
    {
        private readonly int dimension;
        private double xyScale;
        private double zScale;

        internal UnitConvertMathTransform(double xyScale, double zScale)
            : this(3, xyScale, zScale)
        {
        }

        internal UnitConvertMathTransform(int dimension, double xyScale, double zScale)
        {
            this.dimension = ValidateDimension(dimension, nameof(dimension));
            ValidateScale(xyScale, nameof(xyScale));
            ValidateScale(zScale, nameof(zScale));

            this.xyScale = xyScale;
            this.zScale = zScale;
        }

        /// <inheritdoc />
        public override int DimSource => this.dimension;

        /// <inheritdoc />
        public override int DimTarget => this.dimension;

        /// <inheritdoc />
        public override string WKT => throw new NotImplementedException();

        /// <inheritdoc />
        public override string XML => throw new NotImplementedException();

        /// <inheritdoc />
        public override bool Identity()
        {
            bool xyIdentity = this.xyScale.Equals(1d);
            if (this.dimension < 3)
            {
                return xyIdentity;
            }

            return xyIdentity && this.zScale.Equals(1d);
        }

        /// <inheritdoc />
        public override MathTransform Inverse()
        {
            return new UnitConvertMathTransform(this.dimension, 1d / this.xyScale, 1d / this.zScale);
        }

        /// <inheritdoc />
        public override void Invert()
        {
            this.xyScale = 1d / this.xyScale;
            this.zScale = 1d / this.zScale;
        }

        /// <inheritdoc />
        public override void Transform(ref double x, ref double y, ref double z)
        {
            x *= this.xyScale;
            y *= this.xyScale;
            if (this.dimension > 2)
            {
                z *= this.zScale;
            }
        }

        private static int ValidateDimension(int dimension, string parameterName)
        {
            if (dimension < 2 || dimension > 3)
            {
                throw new ArgumentOutOfRangeException(parameterName, dimension, "Unit conversion dimension must be either 2 or 3.");
            }

            return dimension;
        }

        private static void ValidateScale(double scale, string parameterName)
        {
            if (scale <= 0d || double.IsNaN(scale) || double.IsInfinity(scale))
            {
                throw new ArgumentOutOfRangeException(parameterName, scale, "Scale must be finite and positive.");
            }
        }
    }
}
