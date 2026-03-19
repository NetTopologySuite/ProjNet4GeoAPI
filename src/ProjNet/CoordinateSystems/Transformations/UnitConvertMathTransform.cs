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

    [Serializable]
    internal sealed class UnitConvertMathTransform : MathTransform
    {
        private readonly double xyScale;
        private readonly double zScale;

        internal UnitConvertMathTransform(double xyScale, double zScale)
        {
            ValidateScale(xyScale, nameof(xyScale));
            ValidateScale(zScale, nameof(zScale));

            this.xyScale = xyScale;
            this.zScale = zScale;
        }

        public override int DimSource => 3;

        public override int DimTarget => 3;

        public override string WKT => throw new NotImplementedException();

        public override string XML => throw new NotImplementedException();

        public override MathTransform Inverse()
        {
            return new UnitConvertMathTransform(1d / this.xyScale, 1d / this.zScale);
        }

        public override void Invert()
        {
            throw new NotSupportedException("Unit conversion inversion should be performed via Inverse().");
        }

        public override void Transform(ref double x, ref double y, ref double z)
        {
            x *= this.xyScale;
            y *= this.xyScale;
            z *= this.zScale;
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
