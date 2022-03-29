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
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjNet.CoordinateSystems
{
	/// <summary>
	/// A 2D cartographic coordinate system.
    /// </summary>
    [Serializable] 
    public class ProjectedCoordinateSystem : HorizontalCoordinateSystem
	{
		/// <summary>
		/// Initializes a new instance of a projected coordinate system
		/// </summary>
		/// <param name="datum">Horizontal datum</param>
		/// <param name="geographicCoordinateSystem">Geographic coordinate system</param>
		/// <param name="linearUnit">Linear unit</param>
		/// <param name="projection">Projection</param>
		/// <param name="axisInfo">Axis info</param>
		/// <param name="name">Name</param>
		/// <param name="authority">Authority name</param>
		/// <param name="code">Authority-specific identification code.</param>
		/// <param name="alias">Alias</param>
		/// <param name="abbreviation">Abbreviation</param>
		/// <param name="remarks">Provider-supplied remarks</param>
		internal ProjectedCoordinateSystem(HorizontalDatum datum, GeographicCoordinateSystem geographicCoordinateSystem,
			LinearUnit linearUnit, IProjection projection, List<AxisInfo> axisInfo,
			string name, string authority, long code, string alias,
			string remarks, string abbreviation)
			: base(datum, axisInfo, name, authority, code, alias, remarks, abbreviation)
		{
			GeographicCoordinateSystem = geographicCoordinateSystem;
			LinearUnit = linearUnit;
			Projection = projection;
		}

		#region Predefined projected coordinate systems

		/// <summary>
		/// Universal Transverse Mercator - WGS84
		/// </summary>
		/// <param name="zone">UTM zone</param>
		/// <param name="zoneIsNorth">true of Northern hemisphere, false if southern</param>
		/// <returns>UTM/WGS84 coordsys</returns>
		public static ProjectedCoordinateSystem WGS84_UTM(int zone, bool zoneIsNorth)
		{
			var pInfo = new List<ProjectionParameter>();
			pInfo.Add(new ProjectionParameter("latitude_of_origin", 0));
			pInfo.Add(new ProjectionParameter("central_meridian", zone * 6 - 183));
			pInfo.Add(new ProjectionParameter("scale_factor", 0.9996));
			pInfo.Add(new ProjectionParameter("false_easting", 500000));
			pInfo.Add(new ProjectionParameter("false_northing", zoneIsNorth ? 0 : 10000000));
			//IProjection projection = cFac.CreateProjection("UTM" + Zone.ToString() + (ZoneIsNorth ? "N" : "S"), "Transverse_Mercator", parameters);
			var proj = new Projection("Transverse_Mercator", pInfo, "UTM" + zone.ToString(CultureInfo.InvariantCulture) + (zoneIsNorth ? "N" : "S"),
				"EPSG", 32600 + zone + (zoneIsNorth ? 0 : 100), string.Empty, string.Empty, string.Empty);
			var axes = new List<AxisInfo>
			    {
			        new AxisInfo("East", AxisOrientationEnum.East),
			        new AxisInfo("North", AxisOrientationEnum.North)
			    };
		    return new ProjectedCoordinateSystem(CoordinateSystems.HorizontalDatum.WGS84,
				CoordinateSystems.GeographicCoordinateSystem.WGS84, CoordinateSystems.LinearUnit.Metre, proj, axes,
				"WGS 84 / UTM zone " + zone.ToString(CultureInfo.InvariantCulture) + (zoneIsNorth ? "N" : "S"), "EPSG", 32600 + zone + (zoneIsNorth ? 0 : 100),
				string.Empty, "Large and medium scale topographic mapping and engineering survey.", string.Empty);
		}

	    /// <summary>
	    /// Gets a WebMercator coordinate reference system
	    /// </summary>
	    public static ProjectedCoordinateSystem WebMercator
	    {
	        get
	        {
                var pInfo = new List<ProjectionParameter>
                    {
                        /*
                        new ProjectionParameter("semi_major", 6378137.0),
                        new ProjectionParameter("semi_minor", 6378137.0),
                        new ProjectionParameter("scale_factor", 1.0),
                         */
                        new ProjectionParameter("latitude_of_origin", 0.0),
                        new ProjectionParameter("central_meridian", 0.0),
                        new ProjectionParameter("false_easting", 0.0),
                        new ProjectionParameter("false_northing", 0.0)
                    };

                var proj = new Projection("Popular Visualisation Pseudo-Mercator", pInfo, "Popular Visualisation Pseudo-Mercator", "EPSG", 3856,
                    "Pseudo-Mercator", string.Empty, string.Empty);
                
                var axes = new List<AxisInfo>
			    {
			        new AxisInfo("East", AxisOrientationEnum.East),
			        new AxisInfo("North", AxisOrientationEnum.North)
			    };
                
                return new ProjectedCoordinateSystem(CoordinateSystems.HorizontalDatum.WGS84,
                    CoordinateSystems.GeographicCoordinateSystem.WGS84, CoordinateSystems.LinearUnit.Metre, proj, axes,
                    "WGS 84 / Pseudo-Mercator", "EPSG", 3857, "WGS 84 / Popular Visualisation Pseudo-Mercator", 
                    "Certain Web mapping and visualisation applications." +
                    "Uses spherical development of ellipsoidal coordinates. Relative to an ellipsoidal development errors of up to 800 metres in position and 0.7 percent in scale may arise. It is not a recognised geodetic system: see WGS 84 / World Mercator (CRS code 3395).",
                    "WebMercator");
            }
	    }

        /// <summary>
        /// Gets or sets the GeographicCoordinateSystem.
        /// </summary>
        public GeographicCoordinateSystem GeographicCoordinateSystem { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="LinearUnit">LinearUnits</see>. The linear unit must be the same as the <see cref="CoordinateSystem"/> units.
        /// </summary>
        public LinearUnit LinearUnit { get; set; }

        /// <summary>
        /// Gets units for dimension within coordinate system. Each dimension in 
        /// the coordinate system has corresponding units.
        /// </summary>
        /// <param name="dimension">Dimension</param>
        /// <returns>Unit</returns>
        public override IUnit GetUnits(int dimension)
		{
			return LinearUnit;
		}

        /// <summary>
        /// Gets or sets the projection
        /// </summary>
        public IProjection Projection { get; set; }

        /// <summary>
        /// Returns the Well-known text for this object
        /// as defined in the simple features specification.
        /// </summary>
        public override string WKT
		{
			get
			{
				var sb = new StringBuilder();
                sb.AppendFormat("PROJCS[\"{0}\", {1}, {2}", Name, GeographicCoordinateSystem.WKT, Projection.WKT);
				for(int i=0;i<Projection.NumParameters;i++)
					sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat, ", {0}", Projection.GetParameter(i).WKT);
				sb.AppendFormat(", {0}", LinearUnit.WKT);
                //Skip axis info if they contain default values
				if (AxisInfo.Count != 2 ||
					AxisInfo[0].Name != "X" || AxisInfo[0].Orientation != AxisOrientationEnum.East ||
					AxisInfo[1].Name != "Y" || AxisInfo[1].Orientation != AxisOrientationEnum.North)
					for (int i = 0; i < AxisInfo.Count; i++)
						sb.AppendFormat(", {0}", GetAxis(i).WKT);
                //Skip authority and code if not defined
                if (!string.IsNullOrWhiteSpace(Authority) && AuthorityCode > 0)
                    sb.AppendFormat(", AUTHORITY[\"{0}\", \"{1}\"]", Authority, AuthorityCode);
                sb.Append("]");
				return sb.ToString();
			}
		}

		/// <summary>
		/// Gets an XML representation of this object.
		/// </summary>
		public override string XML
		{
			get
			{
				var sb = new StringBuilder();
				sb.AppendFormat(CultureInfo.InvariantCulture.NumberFormat,
					"<CS_CoordinateSystem Dimension=\"{0}\"><CS_ProjectedCoordinateSystem>{1}",
					Dimension, InfoXml);
				foreach (var ai in AxisInfo)
					sb.Append(ai.XML);

				sb.AppendFormat("{0}{1}{2}</CS_ProjectedCoordinateSystem></CS_CoordinateSystem>",
					GeographicCoordinateSystem.XML, LinearUnit.XML, Projection.XML);
				return sb.ToString();
			}
		}

		/// <summary>
		/// Checks whether the values of this instance is equal to the values of another instance.
		/// Only parameters used for coordinate system are used for comparison.
		/// Name, abbreviation, authority, alias and remarks are ignored in the comparison.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>True if equal</returns>
		public override bool EqualParams(object obj)
		{
			if (!(obj is ProjectedCoordinateSystem pcs))
				return false;

            if (pcs.Dimension != Dimension)
				return false;
			for (int i = 0; i < pcs.Dimension; i++)
			{
				if(pcs.GetAxis(i).Orientation != GetAxis(i).Orientation)
					return false;
				if (!pcs.GetUnits(i).EqualParams(GetUnits(i)))
					return false;
			}

			return	pcs.GeographicCoordinateSystem.EqualParams(GeographicCoordinateSystem) && 
					pcs.HorizontalDatum.EqualParams(HorizontalDatum) &&
					pcs.LinearUnit.EqualParams(LinearUnit) &&
					pcs.Projection.EqualParams(Projection);
		}

		#endregion
	}
}
