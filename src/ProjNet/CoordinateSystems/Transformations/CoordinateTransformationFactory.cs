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
using System.Runtime.CompilerServices;
using ProjNet.CoordinateSystems.Projections;

namespace ProjNet.CoordinateSystems.Transformations
{
	/// <summary>
	/// Creates coordinate transformations.
	/// </summary>
	public class CoordinateTransformationFactory
	{
		#region ICoordinateTransformationFactory Members

		/// <summary>
		/// Creates a transformation between two coordinate systems.
		/// </summary>
		/// <remarks>
		/// This method will examine the coordinate systems in order to construct
		/// a transformation between them. This method may fail if no path between 
		/// the coordinate systems is found, using the normal failing behavior of 
		/// the DCP (e.g. throwing an exception).</remarks>
		/// <param name="sourceCS">Source coordinate system</param>
		/// <param name="targetCS">Target coordinate system</param>
		/// <returns></returns>		
		public ICoordinateTransformation CreateFromCoordinateSystems(CoordinateSystem sourceCS, CoordinateSystem targetCS)
		{
            ICoordinateTransformation trans;
            if (sourceCS is ProjectedCoordinateSystem && targetCS is GeographicCoordinateSystem) //Projected -> Geographic
                trans = Proj2Geog((ProjectedCoordinateSystem)sourceCS, (GeographicCoordinateSystem)targetCS);            
            else if (sourceCS is GeographicCoordinateSystem && targetCS is ProjectedCoordinateSystem) //Geographic -> Projected
				trans = Geog2Proj((GeographicCoordinateSystem)sourceCS, (ProjectedCoordinateSystem)targetCS);

            else if (sourceCS is GeographicCoordinateSystem && targetCS is GeocentricCoordinateSystem) //Geocentric -> Geographic
				trans = Geog2Geoc((GeographicCoordinateSystem)sourceCS, (GeocentricCoordinateSystem)targetCS);

            else if (sourceCS is GeocentricCoordinateSystem && targetCS is GeographicCoordinateSystem) //Geocentric -> Geographic
				trans = Geoc2Geog((GeocentricCoordinateSystem)sourceCS, (GeographicCoordinateSystem)targetCS);

            else if (sourceCS is ProjectedCoordinateSystem && targetCS is ProjectedCoordinateSystem) //Projected -> Projected
				trans = Proj2Proj((sourceCS as ProjectedCoordinateSystem), (targetCS as ProjectedCoordinateSystem));

            else if (sourceCS is GeocentricCoordinateSystem && targetCS is GeocentricCoordinateSystem) //Geocentric -> Geocentric
				trans = CreateGeoc2Geoc((GeocentricCoordinateSystem)sourceCS, (GeocentricCoordinateSystem)targetCS);

            else if (sourceCS is GeographicCoordinateSystem && targetCS is GeographicCoordinateSystem) //Geographic -> Geographic
				trans = CreateGeog2Geog(sourceCS as GeographicCoordinateSystem, targetCS as GeographicCoordinateSystem);
			else if (sourceCS is FittedCoordinateSystem) //Fitted -> Any
                trans = Fitt2Any ((FittedCoordinateSystem)sourceCS, targetCS);
            else if (targetCS is FittedCoordinateSystem) //Any -> Fitted 
                trans = Any2Fitt (sourceCS, (FittedCoordinateSystem)targetCS);
            else
				throw new NotSupportedException("No support for transforming between the two specified coordinate systems");
			
			//if (trans.MathTransform is ConcatenatedTransform) {
			//    List<ICoordinateTransformation> MTs = new List<ICoordinateTransformation>();
			//    SimplifyTrans(trans.MathTransform as ConcatenatedTransform, ref MTs);
			//    return new CoordinateTransformation(sourceCS,
			//        targetCS, TransformType.Transformation, new ConcatenatedTransform(MTs),
			//        string.Empty, string.Empty, -1, string.Empty, string.Empty);
			//}
			return trans;
		}
		#endregion

		private static void SimplifyTrans(ConcatenatedTransform mtrans, ref List<ICoordinateTransformationCore> MTs)
		{
			foreach(var t in mtrans.CoordinateTransformationList)
			{
				if(t is ConcatenatedTransform ct)
					SimplifyTrans(ct, ref MTs);
				else
					MTs.Add(t);
			}			
		}

		#region Methods for converting between specific systems

        private static CoordinateTransformation Geog2Geoc(GeographicCoordinateSystem source, GeocentricCoordinateSystem target)
        {
            var geocMathTransform = CreateCoordinateOperation(target);
            if (source.PrimeMeridian.EqualParams(target.PrimeMeridian))
            {
                return new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty);
            }

            var ct = new ConcatenatedTransform();
            ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Transformation, new PrimeMeridianTransform(source.PrimeMeridian, target.PrimeMeridian), string.Empty, string.Empty, -1, string.Empty, string.Empty));
            ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty));
            return new CoordinateTransformation(source, target, TransformType.Conversion, ct, string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }

        private static CoordinateTransformation Geoc2Geog(GeocentricCoordinateSystem source, GeographicCoordinateSystem target)
        {
            var geocMathTransform = CreateCoordinateOperation(source).Inverse();
            if (source.PrimeMeridian.EqualParams(target.PrimeMeridian))
            {
                return new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty);
            }

            var ct = new ConcatenatedTransform();
            ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Conversion, geocMathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty));
            ct.CoordinateTransformationList.Add(new CoordinateTransformation(source, target, TransformType.Transformation, new PrimeMeridianTransform(source.PrimeMeridian, target.PrimeMeridian), string.Empty, string.Empty, -1, string.Empty, string.Empty));
            return new CoordinateTransformation(source, target, TransformType.Conversion, ct, string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }
		
		private static CoordinateTransformation Proj2Proj(ProjectedCoordinateSystem source, ProjectedCoordinateSystem target)
		{
			var ct = new ConcatenatedTransform();
			var ctFac = new CoordinateTransformationFactory();
			//First transform from projection to geographic
			ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
			//Transform geographic to geographic:
		    var geogToGeog = ctFac.CreateFromCoordinateSystems(source.GeographicCoordinateSystem,
		                                                      target.GeographicCoordinateSystem);
            if (geogToGeog != null)
                ct.CoordinateTransformationList.Add(geogToGeog);
			//Transform to new projection
			ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));

			return new CoordinateTransformation(source,
				target, TransformType.Transformation, ct,
				string.Empty, string.Empty, -1, string.Empty, string.Empty);
		}		

        private static CoordinateTransformation Geog2Proj(GeographicCoordinateSystem source, ProjectedCoordinateSystem target)
        {
	        if (source.EqualParams(target.GeographicCoordinateSystem))
	        {
				var mathTransform = CreateCoordinateOperation(target.Projection, 
                    target.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid, target.LinearUnit);
		        return new CoordinateTransformation(source, target, TransformType.Transformation, mathTransform,
			        string.Empty, string.Empty, -1, string.Empty, string.Empty);
	        }

            // Geographic coordinatesystems differ - Create concatenated transform
            var ct = new ConcatenatedTransform();
            var ctFac = new CoordinateTransformationFactory();
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source,target.GeographicCoordinateSystem));
            ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(target.GeographicCoordinateSystem, target));
            return new CoordinateTransformation(source,
                target, TransformType.Transformation, ct,
                string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }

        private static CoordinateTransformation Proj2Geog(ProjectedCoordinateSystem source, GeographicCoordinateSystem target)
        {
            if (source.GeographicCoordinateSystem.EqualParams(target))
            {
                var mathTransform = CreateCoordinateOperation(source.Projection, source.GeographicCoordinateSystem.HorizontalDatum.Ellipsoid, source.LinearUnit).Inverse();
                return new CoordinateTransformation(source, target, TransformType.Transformation, mathTransform,
                    string.Empty, string.Empty, -1, string.Empty, string.Empty);
            }
            else
            {	// Geographic coordinatesystems differ - Create concatenated transform
                var ct = new ConcatenatedTransform();
                var ctFac = new CoordinateTransformationFactory();
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source, source.GeographicCoordinateSystem));
                ct.CoordinateTransformationList.Add(ctFac.CreateFromCoordinateSystems(source.GeographicCoordinateSystem, target));
                return new CoordinateTransformation(source,
                    target, TransformType.Transformation, ct,
                    string.Empty, string.Empty, -1, string.Empty, string.Empty);
            }
        }
		
		/// <summary>
		/// Geographic to geographic transformation
		/// </summary>
		/// <remarks>Adds a datum shift if necessary</remarks>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		private static ICoordinateTransformation CreateGeog2Geog(GeographicCoordinateSystem source, GeographicCoordinateSystem target)
		{
			if (source.HorizontalDatum.EqualParams(target.HorizontalDatum))
			{
				//No datum shift needed
				return new CoordinateTransformation(source,
					target, TransformType.Conversion, new GeographicTransform(source, target),
					string.Empty, string.Empty, -1, string.Empty, string.Empty);
			}

            //Create datum shift
            //Convert to geocentric, perform shift and return to geographic
            var ctFac = new CoordinateTransformationFactory();
            var cFac = new CoordinateSystemFactory();
            var sourceCentric = cFac.CreateGeocentricCoordinateSystem(source.HorizontalDatum.Name + " Geocentric",
                source.HorizontalDatum, LinearUnit.Metre, source.PrimeMeridian);
            var targetCentric = cFac.CreateGeocentricCoordinateSystem(target.HorizontalDatum.Name + " Geocentric", 
                target.HorizontalDatum, LinearUnit.Metre, source.PrimeMeridian);
            var ct = new ConcatenatedTransform();
            AddIfNotNull(ct, ctFac.CreateFromCoordinateSystems(source, sourceCentric));
            AddIfNotNull(ct, ctFac.CreateFromCoordinateSystems(sourceCentric, targetCentric));
            AddIfNotNull(ct, ctFac.CreateFromCoordinateSystems(targetCentric, target));
				
                
            return new CoordinateTransformation(source,
                target, TransformType.Transformation, ct,
                string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }

        private static void AddIfNotNull(ConcatenatedTransform concatTrans, ICoordinateTransformation trans)
        {
            if (trans != null)
                concatTrans.CoordinateTransformationList.Add(trans);
        }
		/// <summary>
		/// Geocentric to Geocentric transformation
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		private static CoordinateTransformation CreateGeoc2Geoc(GeocentricCoordinateSystem source, GeocentricCoordinateSystem target)
		{
			var ct = new ConcatenatedTransform();

			//Does source has a datum different from WGS84 and is there a shift specified?
			if (source.HorizontalDatum.Wgs84Parameters != null && !source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
				ct.CoordinateTransformationList.Add(
					new CoordinateTransformation(
					((target.HorizontalDatum.Wgs84Parameters == null || target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? target : GeocentricCoordinateSystem.WGS84),
					source, TransformType.Transformation,
						new DatumTransform(source.HorizontalDatum.Wgs84Parameters)
						, "", "", -1, "", ""));

			//Does target has a datum different from WGS84 and is there a shift specified?
			if (target.HorizontalDatum.Wgs84Parameters != null && !target.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly)
				ct.CoordinateTransformationList.Add(
					new CoordinateTransformation(
					((source.HorizontalDatum.Wgs84Parameters == null || source.HorizontalDatum.Wgs84Parameters.HasZeroValuesOnly) ? source : GeocentricCoordinateSystem.WGS84),
					target,
					TransformType.Transformation,
						new DatumTransform(target.HorizontalDatum.Wgs84Parameters).Inverse()
						, "", "", -1, "", ""));

            //If we don't have a transformation in this list, return null
		    if (ct.CoordinateTransformationList.Count == 0)
		        return null;
            //If we only have one shift, lets just return the datumshift from/to wgs84
            if (ct.CoordinateTransformationList.Count == 1)
				return new CoordinateTransformation(source, target, TransformType.ConversionAndTransformation, ((ICoordinateTransformation)ct.CoordinateTransformationList[0]).MathTransform, "", "", -1, "", "");
		    
            return new CoordinateTransformation(source, target, TransformType.ConversionAndTransformation, ct, "", "", -1, "", "");
		}

        /// <summary>
        /// Creates transformation from fitted coordinate system to the target one
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static CoordinateTransformation Fitt2Any (FittedCoordinateSystem source, CoordinateSystem target)
        {
            //transform from fitted to base system of fitted (which is equal to target)
            var mt = CreateFittedTransform (source);

            //case when target system is equal to base system of the fitted
            if (source.BaseCoordinateSystem.EqualParams (target))
            {
                //Transform form base system of fitted to target coordinate system
                return CreateTransform (source, target, TransformType.Transformation, mt);
            }

            //Transform form base system of fitted to target coordinate system
            var ct = new ConcatenatedTransform ();
            ct.CoordinateTransformationList.Add (CreateTransform (source, source.BaseCoordinateSystem, TransformType.Transformation, mt));

            //Transform form base system of fitted to target coordinate system
            var ctFac = new CoordinateTransformationFactory ();
            ct.CoordinateTransformationList.Add (ctFac.CreateFromCoordinateSystems (source.BaseCoordinateSystem, target));

            return CreateTransform (source, target, TransformType.Transformation, ct);
        }

        /// <summary>
        /// Creates transformation from source coordinate system to specified target system which is the fitted one
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static CoordinateTransformation Any2Fitt (CoordinateSystem source, FittedCoordinateSystem target)
        {
            //Transform form base system of fitted to target coordinate system - use invered math transform
            var invMt = CreateFittedTransform (target).Inverse ();

            //case when source system is equal to base system of the fitted
            if (target.BaseCoordinateSystem.EqualParams (source))
            {
                //Transform form base system of fitted to target coordinate system
                return CreateTransform (source, target, TransformType.Transformation, invMt);
            }

            var ct = new ConcatenatedTransform ();
            //First transform from source to base system of fitted
            var ctFac = new CoordinateTransformationFactory ();
            ct.CoordinateTransformationList.Add (ctFac.CreateFromCoordinateSystems (source, target.BaseCoordinateSystem));

            //Transform form base system of fitted to target coordinate system - use invered math transform
            ct.CoordinateTransformationList.Add (CreateTransform (target.BaseCoordinateSystem, target, TransformType.Transformation, invMt));

            return CreateTransform (source, target, TransformType.Transformation, ct);
        }

        private static MathTransform CreateFittedTransform (FittedCoordinateSystem fittedSystem)
        {
            //create transform From fitted to base and inverts it
            return fittedSystem.ToBaseTransform;

            //MathTransformFactory mtFac = new MathTransformFactory ();
            ////create transform From fitted to base and inverts it
            //return mtFac.CreateFromWKT (fittedSystem.ToBase ());

            throw new NotImplementedException ();
        }

        /// <summary>
        /// Creates an instance of CoordinateTransformation as an anonymous transformation without neither autohority nor code defined.
        /// </summary>
        /// <param name="sourceCS">Source coordinate system</param>
        /// <param name="targetCS">Target coordinate system</param>
        /// <param name="transformType">Transformation type</param>
        /// <param name="mathTransform">Math transform</param>
        private static CoordinateTransformation CreateTransform (CoordinateSystem sourceCS, CoordinateSystem targetCS, TransformType transformType, MathTransform mathTransform)
        {
            return new CoordinateTransformation (sourceCS, targetCS, transformType, mathTransform, string.Empty, string.Empty, -1, string.Empty, string.Empty);
        }
		#endregion

		private static MathTransform CreateCoordinateOperation(GeocentricCoordinateSystem geo)
		{
			var parameterList = new List<ProjectionParameter>(2);

		    var ellipsoid = geo.HorizontalDatum.Ellipsoid;
            //var toMeter = ellipsoid.AxisUnit.MetersPerUnit;
            if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_major")) == null)
                parameterList.Add(new ProjectionParameter("semi_major", /*toMeter * */ellipsoid.SemiMajorAxis));
            if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_minor")) == null)
                parameterList.Add(new ProjectionParameter("semi_minor", /*toMeter * */ellipsoid.SemiMinorAxis));

            return new GeocentricTransform(parameterList);
		}
		private static MathTransform CreateCoordinateOperation(IProjection projection, Ellipsoid ellipsoid, LinearUnit unit)
		{
			var parameterList = new List<ProjectionParameter>(projection.NumParameters);
			for (int i = 0; i < projection.NumParameters; i++)
				parameterList.Add(projection.GetParameter(i));

		    //var toMeter = 1d/ellipsoid.AxisUnit.MetersPerUnit;
            if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_major")) == null)
			    parameterList.Add(new ProjectionParameter("semi_major", /*toMeter * */ellipsoid.SemiMajorAxis));
            if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("semi_minor")) == null)
                parameterList.Add(new ProjectionParameter("semi_minor", /*toMeter * */ellipsoid.SemiMinorAxis));
            if (parameterList.Find((p) => p.Name.ToLowerInvariant().Replace(' ', '_').Equals("unit")) == null)
                parameterList.Add(new ProjectionParameter("unit", unit.MetersPerUnit));

            var operation = ProjectionsRegistry.CreateProjection(projection.ClassName, parameterList);
		    /*
            var mpOperation = operation as MapProjection;
            if (mpOperation != null && projection.AuthorityCode !=-1)
            {
                mpOperation.Authority = projection.Authority;
                mpOperation.AuthorityCode = projection.AuthorityCode;
            }
             */

		    return operation;
		    /*
            switch (projection.ClassName.ToLower(CultureInfo.InvariantCulture).Replace(' ', '_'))
			{
				case "mercator":
				case "mercator_1sp":
				case "mercator_2sp":
					//1SP
					transform = new Mercator(parameterList);
					break;
				case "transverse_mercator":
					transform = new TransverseMercator(parameterList);
					break;
				case "albers":
				case "albers_conic_equal_area":
					transform = new AlbersProjection(parameterList);
					break;
				case "krovak":
					transform = new KrovakProjection(parameterList);
					break;
                case "polyconic":
                    transform = new PolyconicProjection(parameterList);
                    break;
                case "lambert_conformal_conic":
				case "lambert_conformal_conic_2sp":
				case "lambert_conic_conformal_(2sp)":
					transform = new LambertConformalConic2SP(parameterList);
					break;
				default:
					throw new NotSupportedException(String.Format("Projection {0} is not supported.", projection.ClassName));
			}
			return transform;
             */
		}
	}
}

