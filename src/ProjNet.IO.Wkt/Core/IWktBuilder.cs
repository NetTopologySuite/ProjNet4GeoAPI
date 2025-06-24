using System.Collections.Generic;
using Pidgin;

namespace ProjNet.IO.Wkt.Core
{
    /// <summary>
    /// Interface for building/creating all Wkt related objects.
    /// </summary>
    public interface IWktBuilder
    {
        /// <summary>
        /// Building the Authority object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="code"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildAuthority(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            int code,
            char rightDelimiter
            );

        /// <summary>
        /// Build the Axis object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildAxis(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            string direction,
            char rightDelimiter);


        /// <summary>
        /// Build the Extension object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildExtension(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            string direction,
            char rightDelimiter);


        /// <summary>
        /// Build the ToWgs84 object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="dxShift"></param>
        /// <param name="dyShift"></param>
        /// <param name="dzShift"></param>
        /// <param name="exRotation"></param>
        /// <param name="eyRotation"></param>
        /// <param name="ezRotation"></param>
        /// <param name="ppmScaling"></param>
        /// <param name="description"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildToWgs84(
            int offset,
            string keyword,
            char leftDelimiter,
            double dxShift,
            double dyShift,
            double dzShift,
            double exRotation,
            double eyRotation,
            double ezRotation,
            double ppmScaling,
            string description,
            char rightDelimiter);


        /// <summary>
        /// Build the Projection object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildProjection(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object authority,
            char rightDelimiter);



        /// <summary>
        /// Build the (Projection) Parameter.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildProjectionParameter(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double value,
            char rightDelimiter);

        /// <summary>
        /// Build the Ellipsoid object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="semiMajorAxis"></param>
        /// <param name="inverseFlattening"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildEllipsoid(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double semiMajorAxis,
            double inverseFlattening,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build the Spheroid object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="semiMajorAxis"></param>
        /// <param name="inverseFlattening"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildSpheroid(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double semiMajorAxis,
            double inverseFlattening,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build the primemeridian object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="longitude"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildPrimeMeridian(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double longitude,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build a Unit object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="factor"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildUnit(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double factor,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build a LinearUnit object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="factor"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildLinearUnit(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double factor,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build an AngularUnit object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="factor"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildAngularUnit(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double factor,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build the Datum object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="spheroid"></param>
        /// <param name="toWgs84"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildDatum(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object spheroid,
            object toWgs84,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build the GeographicCoordinateSystem object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="datum"></param>
        /// <param name="meridian"></param>
        /// <param name="unit"></param>
        /// <param name="axes"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildGeographicCoordinateSystem(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object datum,
            object meridian,
            object unit,
            IEnumerable<object> axes,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build a GeocentricCoordinateSystem object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="datum"></param>
        /// <param name="meridian"></param>
        /// <param name="unit"></param>
        /// <param name="axes"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildGeocentricCoordinateSystem(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object datum,
            object meridian,
            object unit,
            IEnumerable<object> axes,
            object authority,
            char rightDelimiter);


        /// <summary>
        /// Build the ProjectedCoordiniateSystem object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="geogcs"></param>
        /// <param name="projection"></param>
        /// <param name="parameters"></param>
        /// <param name="unit"></param>
        /// <param name="axes"></param>
        /// <param name="extension"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildProjectedCoordinateSystem(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object geogcs,
            object projection,
            object parameters,
            object unit,
            IEnumerable<object> axes,
            object extension,
            object authority,
            char rightDelimiter);



        /// <summary>
        /// Build the MathTransform Parameter.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildParameter(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            double value,
            char rightDelimiter);

        /// <summary>
        /// Build a ParameterMathTransform object. (Part of FittedCoordinateSystem).
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildParameterMathTransform(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object parameters,
            char rightDelimiter);


        /// <summary>
        /// Build the FittedCoordinateSystem object.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="keyword"></param>
        /// <param name="leftDelimiter"></param>
        /// <param name="name"></param>
        /// <param name="pmt"></param>
        /// <param name="projcs"></param>
        /// <param name="authority"></param>
        /// <param name="rightDelimiter"></param>
        /// <returns></returns>
        object BuildFittedCoordinateSystem(
            int offset,
            string keyword,
            char leftDelimiter,
            string name,
            object pmt,
            object projcs,
            object authority,
            char rightDelimiter);
    }
}
