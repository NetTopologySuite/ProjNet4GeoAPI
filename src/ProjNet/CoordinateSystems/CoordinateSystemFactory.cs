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
using System.Text;
using ProjNet.IO.CoordinateSystems;

namespace ProjNet.CoordinateSystems
{
    /// <summary>
    /// Builds up complex objects from simpler objects or values.
    /// </summary>
    /// <remarks>
    /// <para>CoordinateSystemFactory allows applications to make coordinate systems that 
    /// is very flexible, whereas the other factories are easier to use.</para>
    /// <para>So this Factory can be used to make 'special' coordinate systems.</para>
    /// <para>For example, the EPSG authority has codes for USA state plane coordinate systems 
    /// using the NAD83 datum, but these coordinate systems always use meters. EPSG does not 
    /// have codes for NAD83 state plane coordinate systems that use feet units. This factory
    /// lets an application create such a hybrid coordinate system.</para>
    /// </remarks>
    public class CoordinateSystemFactory 
    {
        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        public CoordinateSystemFactory() { }

        /// <summary>
        /// Creates a coordinate system object from an XML string.
        /// </summary>
        /// <param name="xml">XML representation for the spatial reference</param>
        /// <returns>The resulting spatial reference object</returns>
        public CoordinateSystem CreateFromXml(string xml)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a spatial reference object given its Well-known text representation.
        /// The output object may be either a <see cref="GeographicCoordinateSystem"/> or
        /// a <see cref="ProjectedCoordinateSystem"/>.
        /// </summary>
        /// <param name="WKT">The Well-known text representation for the spatial reference</param>
        /// <returns>The resulting spatial reference object</returns>
        public CoordinateSystem CreateFromWkt(string WKT)
        {
            var info = CoordinateSystemWktReader.Parse(WKT);
            return info as CoordinateSystem;
        }


        /// <summary>
        /// Creates a <see cref="CompoundCoordinateSystem"/> [NOT IMPLEMENTED].
        /// </summary>
        /// <param name="name">Name of compound coordinate system.</param>
        /// <param name="head">Head coordinate system</param>
        /// <param name="tail">Tail coordinate system</param>
        /// <returns>Compound coordinate system</returns>
        public CompoundCoordinateSystem CreateCompoundCoordinateSystem(string name, CoordinateSystem head, CoordinateSystem tail)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            return new CompoundCoordinateSystem(head, tail, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a <see cref="FittedCoordinateSystem"/>.
        /// </summary>
        /// <remarks>The units of the axes in the fitted coordinate system will be 
        /// inferred from the units of the base coordinate system. If the affine map
        /// performs a rotation, then any mixed axes must have identical units. For
        /// example, a (lat_deg,lon_deg,height_feet) system can be rotated in the 
        /// (lat,lon) plane, since both affected axes are in degrees. But you 
        /// should not rotate this coordinate system in any other plane.</remarks>
        /// <param name="name">Name of coordinate system</param>
        /// <param name="baseCoordinateSystem">Base coordinate system</param>
        /// <param name="toBaseWkt">WKT of the math transform to the base coordinate system</param>
        /// <param name="arAxes">Axiis of the fitted coordinate system</param>
        /// <returns>Fitted coordinate system</returns>
        public FittedCoordinateSystem CreateFittedCoordinateSystem(string name, CoordinateSystem baseCoordinateSystem, string toBaseWkt, List<AxisInfo> arAxes)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            var toBaseTransform = MathTransformWktReader.Parse(toBaseWkt);
            return new FittedCoordinateSystem(baseCoordinateSystem, toBaseTransform, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <inheritdoc cref="CreateFittedCoordinateSystem(string, CoordinateSystem, string, List{AxisInfo})"/>
        /// <param name="name">Name of coordinate system</param>
        /// <param name="baseCoordinateSystem">Base coordinate system</param>
        /// <param name="toBase">the math transform to the base coordinate system</param>
        /// <param name="arAxes">Axiis of the fitted coordinate system</param>
        /// <returns></returns>
        public FittedCoordinateSystem CreateFittedCoordinateSystem(string name, CoordinateSystem baseCoordinateSystem, Transformations.MathTransform toBase, List<AxisInfo> arAxes)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            return new FittedCoordinateSystem(baseCoordinateSystem, toBase, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /*
        /// <summary>
        /// Creates a <see cref="ILocalCoordinateSystem">local coordinate system</see>.
        /// </summary>
        /// <remarks>
        ///  The dimension of the local coordinate system is determined by the size of 
        /// the axis array. All the axes will have the same units. If you want to make 
        /// a coordinate system with mixed units, then you can make a compound 
        /// coordinate system from different local coordinate systems.
        /// </remarks>
        /// <param name="name">Name of local coordinate system</param>
        /// <param name="datum">Local datum</param>
        /// <param name="unit">Units</param>
        /// <param name="axes">Axis info</param>
        /// <returns>Local coordinate system</returns>
        public ILocalCoordinateSystem CreateLocalCoordinateSystem(string name, ILocalDatum datum, IUnit unit, List<AxisInfo> axes)
        {
            throw new NotImplementedException();
        }
        */
        /// <summary>
        /// Creates an <see cref="Ellipsoid"/> from radius values.
        /// </summary>
        /// <seealso cref="CreateFlattenedSphere"/>
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="semiMajorAxis"></param>
        /// <param name="semiMinorAxis"></param>
        /// <param name="linearUnit"></param>
        /// <returns>Ellipsoid</returns>
        public Ellipsoid CreateEllipsoid(string name, double semiMajorAxis, double semiMinorAxis, LinearUnit linearUnit)
        {
            double ivf = 0;
            if (semiMajorAxis != semiMinorAxis)
                ivf = semiMajorAxis / (semiMajorAxis - semiMinorAxis);
            return new Ellipsoid(semiMajorAxis, semiMinorAxis, ivf, false, linearUnit, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates an <see cref="Ellipsoid"/> from an major radius, and inverse flattening.
        /// </summary>
        /// <seealso cref="CreateEllipsoid"/>
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="semiMajorAxis">Semi major-axis</param>
        /// <param name="inverseFlattening">Inverse flattening</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <returns>Ellipsoid</returns>
        public Ellipsoid CreateFlattenedSphere(string name, double semiMajorAxis, double inverseFlattening, LinearUnit linearUnit)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            return new Ellipsoid(semiMajorAxis, -1, inverseFlattening, true, linearUnit, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a <see cref="ProjectedCoordinateSystem"/> using a projection object.
        /// </summary>
        /// <param name="name">Name of projected coordinate system</param>
        /// <param name="gcs">Geographic coordinate system</param>
        /// <param name="projection">Projection</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <param name="axis0">Primary axis</param>
        /// <param name="axis1">Secondary axis</param>
        /// <returns>Projected coordinate system</returns>
        public ProjectedCoordinateSystem CreateProjectedCoordinateSystem(string name, GeographicCoordinateSystem gcs, IProjection projection, LinearUnit linearUnit, AxisInfo axis0, AxisInfo axis1)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name", nameof(name));
            if (gcs == null)
                throw new ArgumentException("Geographic coordinate system was null", nameof(gcs));
            if (projection == null)
                throw new ArgumentException("Projection was null", nameof(projection));
            if (linearUnit == null)
                throw new ArgumentException("Linear unit was null");

            var info = new List<AxisInfo>(2);
            info.Add(axis0);
            info.Add(axis1);
            return new ProjectedCoordinateSystem(null, gcs, linearUnit, projection, info, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a <see cref="Projection"/>.
        /// </summary>
        /// <param name="name">Name of projection</param>
        /// <param name="wktProjectionClass">Projection class</param>
        /// <param name="parameters">Projection parameters</param>
        /// <returns>Projection</returns>
        public IProjection CreateProjection(string name, string wktProjectionClass, List<ProjectionParameter> parameters)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");
            if (parameters == null || parameters.Count == 0)
                throw new ArgumentException("Invalid projection parameters");

            return new Projection(wktProjectionClass, parameters, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates <see cref="HorizontalDatum"/> from ellipsoid and Bursa-World parameters.
        /// </summary>
        /// <remarks>
        /// Since this method contains a set of Bursa-Wolf parameters, the created 
        /// datum will always have a relationship to WGS84. If you wish to create a
        /// horizontal datum that has no relationship with WGS84, then you can 
        /// either specify a <see cref="DatumType">horizontalDatumType</see> of <see cref="DatumType.HD_Other"/>, or create it via WKT.
        /// </remarks>
        /// <param name="name">Name of ellipsoid</param>
        /// <param name="datumType">Type of datum</param>
        /// <param name="ellipsoid">Ellipsoid</param>
        /// <param name="toWgs84">Wgs84 conversion parameters</param>
        /// <returns>Horizontal datum</returns>
        public HorizontalDatum CreateHorizontalDatum(string name, DatumType datumType, Ellipsoid ellipsoid, Wgs84ConversionInfo toWgs84)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");
            if (ellipsoid == null)
                throw new ArgumentException("Ellipsoid was null");

            return new HorizontalDatum(ellipsoid, toWgs84, datumType, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a <see cref="PrimeMeridian"/>, relative to Greenwich.
        /// </summary>
        /// <param name="name">Name of prime meridian</param>
        /// <param name="angularUnit">Angular unit</param>
        /// <param name="longitude">Longitude</param>
        /// <returns>Prime meridian</returns>
        public PrimeMeridian CreatePrimeMeridian(string name, AngularUnit angularUnit, double longitude)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            return new PrimeMeridian(longitude, angularUnit, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a <see cref="GeographicCoordinateSystem"/>, which could be Lat/Lon or Lon/Lat.
        /// </summary>
        /// <param name="name">Name of geographical coordinate system</param>
        /// <param name="angularUnit">Angular units</param>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="primeMeridian">Prime meridian</param>
        /// <param name="axis0">First axis</param>
        /// <param name="axis1">Second axis</param>
        /// <returns>Geographic coordinate system</returns>
        public GeographicCoordinateSystem CreateGeographicCoordinateSystem(string name, AngularUnit angularUnit, HorizontalDatum datum, PrimeMeridian primeMeridian, AxisInfo axis0, AxisInfo axis1)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            var info = new List<AxisInfo>(2);
            info.Add(axis0);
            info.Add(axis1);
            return new GeographicCoordinateSystem(angularUnit, datum, primeMeridian, info, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /*
        /// <summary>
        /// Creates a <see cref="ILocalDatum"/>.
        /// </summary>
        /// <param name="name">Name of datum</param>
        /// <param name="datumType">Datum type</param>
        /// <returns></returns>
        public ILocalDatum CreateLocalDatum(string name, DatumType datumType)
        {
            throw new NotImplementedException();
        }
        */


        /// <summary>
        /// Creates a <see cref="VerticalDatum"/> from an enumerated type value.
        /// </summary>
        /// <param name="name">Name of datum</param>
        /// <param name="datumType">Type of datum</param>
        /// <returns>Vertical datum</returns>	
        public VerticalDatum CreateVerticalDatum(string name, DatumType datumType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            return new VerticalDatum(datumType, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }



        /// <summary>
        /// Creates a <see cref="VerticalCoordinateSystem"/> from a <see cref="VerticalDatum">datum</see> and <see cref="LinearUnit">linear units</see>.
        /// </summary>
        /// <param name="name">Name of vertical coordinate system</param>
        /// <param name="datum">Vertical datum</param>
        /// <param name="verticalUnit">Unit</param>
        /// <param name="axis">Axis info</param>
        /// <returns>Vertical coordinate system</returns>
        public VerticalCoordinateSystem CreateVerticalCoordinateSystem(string name, VerticalDatum datum, LinearUnit verticalUnit, AxisInfo axis)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            return new VerticalCoordinateSystem(verticalUnit, datum, axis, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates a <see cref="CreateGeocentricCoordinateSystem"/> from a <see cref="HorizontalDatum">datum</see>, 
        /// <see cref="LinearUnit">linear unit</see> and <see cref="PrimeMeridian"/>.
        /// </summary>
        /// <param name="name">Name of geocentric coordinate system</param>
        /// <param name="datum">Horizontal datum</param>
        /// <param name="linearUnit">Linear unit</param>
        /// <param name="primeMeridian">Prime meridian</param>
        /// <returns>Geocentric Coordinate System</returns>
        public GeocentricCoordinateSystem CreateGeocentricCoordinateSystem(string name, HorizontalDatum datum, LinearUnit linearUnit, PrimeMeridian primeMeridian)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Invalid name");

            var info = new List<AxisInfo>(3);
            info.Add(new AxisInfo("X", AxisOrientationEnum.Other));
            info.Add(new AxisInfo("Y", AxisOrientationEnum.Other));
            info.Add(new AxisInfo("Z", AxisOrientationEnum.Other));
            return new GeocentricCoordinateSystem(datum, linearUnit, primeMeridian, info, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
        }
    }
}
