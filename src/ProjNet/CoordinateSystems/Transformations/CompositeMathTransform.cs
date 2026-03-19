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

    [Serializable]
    internal sealed class CompositeMathTransform : MathTransform
    {
        private MathTransform[] transforms;
        private MathTransform inverse;

        internal CompositeMathTransform(IReadOnlyList<MathTransform> transforms)
        {
            if (transforms is null)
            {
                throw new ArgumentNullException(nameof(transforms));
            }

            if (transforms.Count == 0)
            {
                throw new ArgumentException("At least one math transform is required.", nameof(transforms));
            }

            this.transforms = new MathTransform[transforms.Count];
            for (int i = 0; i < transforms.Count; i++)
            {
                if (transforms[i] is null)
                {
                    throw new ArgumentException("Math transform list contains null element.", nameof(transforms));
                }

                this.transforms[i] = transforms[i];
            }
        }

        public override int DimSource => this.transforms[0].DimSource;

        public override int DimTarget => this.transforms[this.transforms.Length - 1].DimTarget;

        public override string WKT => throw new NotImplementedException();

        public override string XML => throw new NotImplementedException();

        public override bool Identity()
        {
            for (int i = 0; i < this.transforms.Length; i++)
            {
                if (!this.transforms[i].Identity())
                {
                    return false;
                }
            }

            return true;
        }

        public override MathTransform Inverse()
        {
            if (!(this.inverse is null))
            {
                return this.inverse;
            }

            var inverted = new MathTransform[this.transforms.Length];
            int output = 0;
            for (int i = this.transforms.Length - 1; i >= 0; i--)
            {
                inverted[output] = this.transforms[i].Inverse();
                output++;
            }

            this.inverse = new CompositeMathTransform(inverted);
            return this.inverse;
        }

        public override void Invert()
        {
            Array.Reverse(this.transforms);
            for (int i = 0; i < this.transforms.Length; i++)
            {
                this.transforms[i].Invert();
            }

            this.inverse = null;
        }

        public override void Transform(ref double x, ref double y, ref double z)
        {
            for (int i = 0; i < this.transforms.Length; i++)
            {
                this.transforms[i].Transform(ref x, ref y, ref z);
            }
        }
    }
}
