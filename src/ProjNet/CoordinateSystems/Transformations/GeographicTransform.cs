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
	/// The GeographicTransform class is implemented on geographic transformation objects and
	/// implements datum transformations between geographic coordinate systems.
    /// </summary>
    [Serializable] 
    public class GeographicTransform : MathTransform
	{
		internal GeographicTransform(GeographicCoordinateSystem sourceGCS, GeographicCoordinateSystem targetGCS)
		{
			SourceGCS = sourceGCS;
			TargetGCS = targetGCS;
		}

        /// <summary>
        /// Gets or sets the source geographic coordinate system for the transformation.
        /// </summary>
        public GeographicCoordinateSystem SourceGCS { get; set; }

        /// <summary>
        /// Gets or sets the target geographic coordinate system for the transformation.
        /// </summary>
        public GeographicCoordinateSystem TargetGCS { get; set; }

        /// <summary>
        /// Returns the Well-known text for this object
        /// as defined in the simple features specification. [NOT IMPLEMENTED].
        /// </summary>
        public override string WKT
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Gets an XML representation of this object [NOT IMPLEMENTED].
		/// </summary>
		public override string XML
		{
			get
			{
				throw new NotImplementedException();
			}
		}

        /// <summary>
        /// DimSource
        /// </summary>
        public override int DimSource
        {
            get { return SourceGCS.Dimension; }
        }

        /// <summary>
        /// DimTarget
        /// </summary>
        public override int DimTarget
        {
            get { return TargetGCS.Dimension; }
        }
        
        /// <summary>
		/// Creates the inverse transform of this object.
		/// </summary>
		/// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
		/// <returns></returns>
		public override MathTransform Inverse()
		{
			throw new NotImplementedException();
		}

        /// <inheritdoc />
        public sealed override void Transform(ref double x, ref double y, ref double z)
        {
            x /= SourceGCS.AngularUnit.RadiansPerUnit;
            x -= SourceGCS.PrimeMeridian.Longitude / SourceGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
            x += TargetGCS.PrimeMeridian.Longitude / TargetGCS.PrimeMeridian.AngularUnit.RadiansPerUnit;
            x *= SourceGCS.AngularUnit.RadiansPerUnit;
        }

        /// <summary>
        /// Reverses the transformation
        /// </summary>
        public override void Invert()
		{
			throw new NotImplementedException();
		}
	}
}
