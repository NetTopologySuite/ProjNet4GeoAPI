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
	/// Describes a coordinate transformation. This class only describes a 
	/// coordinate transformation, it does not actually perform the transform 
	/// operation on points. To transform points you must use a <see cref="MathTransform"/>.
    /// </summary>
    [Serializable] 
    public class CoordinateTransformation : ICoordinateTransformation
    {
		/// <summary>
		/// Initializes an instance of a CoordinateTransformation
		/// </summary>
		/// <param name="sourceCS">Source coordinate system</param>
		/// <param name="targetCS">Target coordinate system</param>
		/// <param name="transformType">Transformation type</param>
		/// <param name="mathTransform">Math transform</param>
		/// <param name="name">Name of transform</param>
		/// <param name="authority">Authority</param>
		/// <param name="authorityCode">Authority code</param>
		/// <param name="areaOfUse">Area of use</param>
		/// <param name="remarks">Remarks</param>
		internal CoordinateTransformation(CoordinateSystem sourceCS, CoordinateSystem targetCS, TransformType transformType, MathTransform mathTransform, 
										string name, string authority, long authorityCode, string areaOfUse, string remarks)
		{
			TargetCS = targetCS;
			SourceCS = sourceCS;
			TransformType = transformType;
			MathTransform = mathTransform;
			Name = name;
			Authority = authority;
			AuthorityCode = authorityCode;
			AreaOfUse = areaOfUse;
			Remarks = remarks;			
		}



        #region ICoordinateTransformation Members

        /// <summary>
        /// Human readable description of domain in source coordinate system.
        /// </summary>		
        public string AreaOfUse { get; }

        /// <summary>
        /// Authority which defined transformation and parameter values.
        /// </summary>
        /// <remarks>
        /// An Authority is an organization that maintains definitions of Authority Codes. For example the European Petroleum Survey Group (EPSG) maintains a database of coordinate systems, and other spatial referencing objects, where each object has a code number ID. For example, the EPSG code for a WGS84 Lat/Lon coordinate system is ‘4326’
        /// </remarks>
        public string Authority { get; }

        /// <summary>
        /// Code used by authority to identify transformation. An empty string is used for no code.
        /// </summary>
        /// <remarks>The AuthorityCode is a compact string defined by an Authority to reference a particular spatial reference object. For example, the European Survey Group (EPSG) authority uses 32 bit integers to reference coordinate systems, so all their code strings will consist of a few digits. The EPSG code for WGS84 Lat/Lon is ‘4326’.</remarks>
        public long AuthorityCode { get; }

        /// <summary>
        /// Gets math transform.
        /// </summary>
        public MathTransform MathTransform { get; }

        /// <summary>
        /// Name of transformation.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the provider-supplied remarks.
        /// </summary>
        public string Remarks { get; }

        /// <summary>
        /// Source coordinate system.
        /// </summary>
        public CoordinateSystem SourceCS { get; }

        /// <summary>
        /// Target coordinate system.
        /// </summary>
        public CoordinateSystem TargetCS { get; }

        /// <summary>
        /// Semantic type of transform. For example, a datum transformation or a coordinate conversion.
        /// </summary>
        public TransformType TransformType { get; }

        #endregion
    }
}
