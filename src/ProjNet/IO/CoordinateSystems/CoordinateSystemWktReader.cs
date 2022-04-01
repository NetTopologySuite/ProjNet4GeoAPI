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

// SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
/*
 *  Copyright (C) 2002 Urban Science Applications, Inc. 
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ProjNet.CoordinateSystems;

namespace ProjNet.IO.CoordinateSystems
{
    /// <summary>
    /// Creates an object based on the supplied Well Known Text (WKT).
    /// </summary>
    public static class CoordinateSystemWktReader
    {
        /// <summary>
        /// Reads and parses a WKT-formatted projection string.
        /// </summary>
        /// <param name="wkt">String containing WKT.</param>
        /// <returns>Object representation of the WKT.</returns>
        /// <exception cref="System.ArgumentException">If a token is not recognized.</exception>
        public static IInfo Parse(string wkt)
        {
            if (string.IsNullOrWhiteSpace(wkt))
                throw new ArgumentNullException("wkt");

            using (TextReader reader = new StringReader(wkt))
            {
                var tokenizer = new WktStreamTokenizer(reader);
                tokenizer.NextToken();
                string objectName = tokenizer.GetStringValue();
                switch (objectName)
                {
                    case "UNIT":
                        return ReadUnit(tokenizer);
                    case "SPHEROID":
                        return ReadEllipsoid(tokenizer);
                    case "DATUM":
                        return ReadHorizontalDatum(tokenizer);
                    case "PRIMEM":
                        return ReadPrimeMeridian(tokenizer);
                    case "VERT_CS":
                    case "GEOGCS":
                    case "PROJCS":
                    case "COMPD_CS":
                    case "GEOCCS":
                    case "FITTED_CS":
                    case "LOCAL_CS":
                        return ReadCoordinateSystem(wkt, tokenizer);
                    default:
                        throw new ArgumentException($"'{objectName}' is not recognized.");
                }
            }
        }

        /// <summary>
        /// Returns a IUnit given a piece of WKT.
        /// </summary>
        /// <param name="tokenizer">WktStreamTokenizer that has the WKT.</param>
        /// <returns>An object that implements the IUnit interface.</returns>
        private static IUnit ReadUnit(WktStreamTokenizer tokenizer)
        {
            var bracket = tokenizer.ReadOpener();
            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.ReadAuthority(out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            else 
                tokenizer.CheckCloser(bracket);

            return new Unit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        }
        /// <summary>
        /// Returns a <see cref="LinearUnit"/> given a piece of WKT.
        /// </summary>
        /// <param name="tokenizer">WktStreamTokenizer that has the WKT.</param>
        /// <returns>An object that implements the IUnit interface.</returns>
        private static LinearUnit ReadLinearUnit(WktStreamTokenizer tokenizer)
        {
            var bracket = tokenizer.ReadOpener();

            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.ReadAuthority(out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            else
                tokenizer.CheckCloser(bracket);

            return new LinearUnit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        }
        /// <summary>
        /// Returns a <see cref="AngularUnit"/> given a piece of WKT.
        /// </summary>
        /// <param name="tokenizer">WktStreamTokenizer that has the WKT.</param>
        /// <returns>An object that implements the IUnit interface.</returns>
        private static AngularUnit ReadAngularUnit(WktStreamTokenizer tokenizer)
        {
            var bracket = tokenizer.ReadOpener();

            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.ReadAuthority(out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            else
            {
                tokenizer.CheckCloser(bracket);
            }
            return new AngularUnit(unitsPerUnit, unitName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Returns a <see cref="AxisInfo"/> given a piece of WKT.
        /// </summary>
        /// <param name="tokenizer">WktStreamTokenizer that has the WKT.</param>
        /// <returns>An AxisInfo object.</returns>
        private static AxisInfo ReadAxis(WktStreamTokenizer tokenizer)
        {
            if (tokenizer.GetStringValue() != "AXIS")
                tokenizer.ReadToken("AXIS");
            var bracket = tokenizer.ReadOpener();
            string axisName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            string unitname = tokenizer.GetStringValue();
            tokenizer.ReadCloser(bracket);
            switch (unitname.ToUpperInvariant())
            {
                case "DOWN": return new AxisInfo(axisName, AxisOrientationEnum.Down);
                case "EAST": return new AxisInfo(axisName, AxisOrientationEnum.East);
                case "NORTH": return new AxisInfo(axisName, AxisOrientationEnum.North);
                case "OTHER": return new AxisInfo(axisName, AxisOrientationEnum.Other);
                case "SOUTH": return new AxisInfo(axisName, AxisOrientationEnum.South);
                case "UP": return new AxisInfo(axisName, AxisOrientationEnum.Up);
                case "WEST": return new AxisInfo(axisName, AxisOrientationEnum.West);
                default:
                    throw new ArgumentException("Invalid axis name '" + unitname + "' in WKT");
            }
        }

        private static CoordinateSystem ReadCoordinateSystem(string coordinateSystem, WktStreamTokenizer tokenizer)
        {
            switch (tokenizer.GetStringValue())
            {
                case "GEOGCS":
                    return ReadGeographicCoordinateSystem(tokenizer);
                case "PROJCS":
                    return ReadProjectedCoordinateSystem(tokenizer);
                case "FITTED_CS":
                    return ReadFittedCoordinateSystem (tokenizer);
                case "GEOCCS":
                    return ReadGeocentricCoordinateSystem(tokenizer);
                case "COMPD_CS":
                    return ReadCompoundCoordinateSystem(tokenizer);
                case "VERT_CS":
                    return ReadVerticalCoordinateSystem(tokenizer);
                case "LOCAL_CS":
                    throw new NotSupportedException($"{coordinateSystem} coordinate system is not supported.");
                default:
                    throw new InvalidOperationException($"{coordinateSystem} coordinate system is not recognized.");
            }
        }

        // Reads either 3, 6 or 7 parameter Bursa-Wolf values from TOWGS84 token
        private static Wgs84ConversionInfo ReadWGS84ConversionInfo(WktStreamTokenizer tokenizer)
        {
            //TOWGS84[0,0,0,0,0,0,0]
            var bracket = tokenizer.ReadOpener();
            var info = new Wgs84ConversionInfo();
            tokenizer.NextToken();
            info.Dx = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Dy = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Dz = tokenizer.GetNumericValue();
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                info.Ex = tokenizer.GetNumericValue();

                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                info.Ey = tokenizer.GetNumericValue();

                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                info.Ez = tokenizer.GetNumericValue();

                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == ",")
                {
                    tokenizer.NextToken();
                    info.Ppm = tokenizer.GetNumericValue();
                }
            }
            if (tokenizer.GetStringValue() != "]")
                tokenizer.ReadCloser(bracket);
            return info;
        }

        private static Ellipsoid ReadEllipsoid(WktStreamTokenizer tokenizer)
        {
            //SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double majorAxis = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double e = tokenizer.GetNumericValue();
            tokenizer.NextToken();
            string authority = string.Empty;
            long authorityCode = -1;
            if (tokenizer.GetStringValue() == ",") //Read authority
            {
                tokenizer.ReadAuthority(out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            var ellipsoid = new Ellipsoid(majorAxis, 0.0, e, true, LinearUnit.Metre, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
            return ellipsoid;
        }

        private static IProjection ReadProjection(WktStreamTokenizer tokenizer)
        {
            if (tokenizer.GetStringValue() != "PROJECTION")
                tokenizer.ReadToken("PROJECTION");
            var bracket = tokenizer.ReadOpener();
            string projectionName = tokenizer.ReadDoubleQuotedWord();
            string authority = string.Empty;
            long authorityCode = -1L;

            tokenizer.NextToken(true);
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.ReadAuthority(out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            else
                tokenizer.CheckCloser(bracket);

            tokenizer.ReadToken(",");//,
            tokenizer.ReadToken("PARAMETER");
            var paramList = new List<ProjectionParameter>();
            while (tokenizer.GetStringValue() == "PARAMETER")
            {
                bracket = tokenizer.ReadOpener();
                string paramName = tokenizer.ReadDoubleQuotedWord();
                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                double paramValue = tokenizer.GetNumericValue();
                tokenizer.ReadCloser(bracket);
                paramList.Add(new ProjectionParameter(paramName, paramValue));
                //tokenizer.ReadToken(",");
                //tokenizer.NextToken();
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == ",")
                {
                    tokenizer.NextToken();
                }
                else
                {
                    break;
                }
            }
            var projection = new Projection(projectionName, paramList, projectionName, authority, authorityCode, string.Empty, string.Empty, string.Empty);
            return projection;
        }

        private static ProjectedCoordinateSystem ReadProjectedCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            /*PROJCS[
                "OSGB 1936 / British National Grid",
                GEOGCS[
                    "OSGB 1936",
                    DATUM[...]
                    PRIMEM[...]
                    AXIS["Geodetic latitude","NORTH"]
                    AXIS["Geodetic longitude","EAST"]
                    AUTHORITY["EPSG","4277"]
                ],
                PROJECTION["Transverse Mercator"],
                PARAMETER["latitude_of_natural_origin",49],
                PARAMETER["longitude_of_natural_origin",-2],
                PARAMETER["scale_factor_at_natural_origin",0.999601272],
                PARAMETER["false_easting",400000],
                PARAMETER["false_northing",-100000],
                AXIS["Easting","EAST"],
                AXIS["Northing","NORTH"],
                AUTHORITY["EPSG","27700"]
            ]
            */
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("GEOGCS");
            var geographicCS = ReadGeographicCoordinateSystem(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("PROJECTION");
            var projection = ReadProjection(tokenizer);
            var unit = ReadLinearUnit(tokenizer);
            var axisInfo = new List<AxisInfo>(2);
            string authority = string.Empty;
            long authorityCode = -1;

            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                while (tokenizer.GetStringValue() == "AXIS")
                {
                    axisInfo.Add(ReadAxis(tokenizer));
                    tokenizer.NextToken();
                    if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                }
                if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                    tokenizer.ReadCloser(bracket);
                }
            }
            //This is default axis values if not specified.
            if (axisInfo.Count == 0)
            {
                axisInfo.Add(new AxisInfo("X", AxisOrientationEnum.East));
                axisInfo.Add(new AxisInfo("Y", AxisOrientationEnum.North));
            }
            var projectedCS = new ProjectedCoordinateSystem(geographicCS.HorizontalDatum, geographicCS, unit as LinearUnit, projection, axisInfo, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
            return projectedCS;
        }

        private static VerticalCoordinateSystem ReadVerticalCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            // VERT_CS["<name>", <vert datum>, <linear unit>, {<axis>,} {,< authority >}]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("VERT_DATUM");
            var verticalDatum = ReadVerticalDatum(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("UNIT");
            var linearUnit = ReadLinearUnit(tokenizer);

            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            AxisInfo info = null;
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AXIS")
                {
                    info = ReadAxis(tokenizer);
                    tokenizer.NextToken();
                }
                if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                    tokenizer.ReadCloser(bracket);
                }
            }

            //This is default axis values if not specified.
            if (info == null)
            {
                info = new AxisInfo("Up", AxisOrientationEnum.Up);
            }
            var verticalCs = new VerticalCoordinateSystem(linearUnit, verticalDatum, info, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
            return verticalCs;
        }

        private static CompoundCoordinateSystem ReadCompoundCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            // <compd cs> = COMPD_CS["<name>", <head cs>, <tail cs> {,<authority>}]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            var headcs = ReadCoordinateSystem(null, tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            var tailcs = ReadCoordinateSystem(null, tokenizer);

            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();

            if ( tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                if(tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                }
            }

            return new CompoundCoordinateSystem(headcs, tailcs, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        }
        private static GeocentricCoordinateSystem ReadGeocentricCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            /*
             * GEOCCS["<name>", <datum>, <prime meridian>, <linear unit> {,<axis>, <axis>, <axis>} {,<authority>}]
             */

            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("DATUM");
            var horizontalDatum = ReadHorizontalDatum(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("PRIMEM");
            var primeMeridian = ReadPrimeMeridian(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("UNIT");
            var linearUnit = ReadLinearUnit(tokenizer);

            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();

            var info = new List<AxisInfo>(3);
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                while (tokenizer.GetStringValue() == "AXIS")
                {
                    info.Add(ReadAxis(tokenizer));
                    tokenizer.NextToken();
                    if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                }
                if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                    tokenizer.ReadCloser(bracket);
                }
            }

            //This is default axis values if not specified.
            if (info.Count == 0)
            {
                info.Add(new AxisInfo("Geocentric X", AxisOrientationEnum.Other));
                info.Add(new AxisInfo("Geocentric Y", AxisOrientationEnum.Other));
                info.Add(new AxisInfo("Geocentric Z", AxisOrientationEnum.North));
            }

            return new GeocentricCoordinateSystem(horizontalDatum, linearUnit, primeMeridian, info, name, authority, authorityCode,
                string.Empty, string.Empty, string.Empty);
        }

        private static GeographicCoordinateSystem ReadGeographicCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            /*
            GEOGCS["OSGB 1936",
            DATUM["OSGB 1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG","6277"]]
            PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]]
            AXIS["Geodetic latitude","NORTH"]
            AXIS["Geodetic longitude","EAST"]
            AUTHORITY["EPSG","4277"]
            ]
            */
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("DATUM");
            var horizontalDatum = ReadHorizontalDatum(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("PRIMEM");
            var primeMeridian = ReadPrimeMeridian(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("UNIT");
            var angularUnit = ReadAngularUnit(tokenizer);

            string authority = string.Empty;
            long authorityCode = -1;
            tokenizer.NextToken();
            var info = new List<AxisInfo>(2);
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                while (tokenizer.GetStringValue() == "AXIS")
                {
                    info.Add(ReadAxis(tokenizer));
                    tokenizer.NextToken();
                    if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                }
                if (tokenizer.GetStringValue() == ",") tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                    tokenizer.ReadCloser(bracket);
                }
            }

            //This is default axis values if not specified.
            if (info.Count == 0)
            {
                info.Add(new AxisInfo("Lon", AxisOrientationEnum.East));
                info.Add(new AxisInfo("Lat", AxisOrientationEnum.North));
            }
            var geographicCS = new GeographicCoordinateSystem(angularUnit, horizontalDatum,
                    primeMeridian, info, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
            return geographicCS;
        }

        private static HorizontalDatum ReadHorizontalDatum(WktStreamTokenizer tokenizer)
        {
            //DATUM["OSGB 1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG","6277"]]
            Wgs84ConversionInfo wgsInfo = null;
            string authority = string.Empty;
            long authorityCode = -1;

            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("SPHEROID");
            var ellipsoid = ReadEllipsoid(tokenizer);
            tokenizer.NextToken();
            while (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "TOWGS84")
                {
                    wgsInfo = ReadWGS84ConversionInfo(tokenizer);
                    tokenizer.NextToken();
                }
                else if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                    tokenizer.ReadCloser(bracket);
                }
            }
            // make an assumption about the datum type.
            var horizontalDatum = new HorizontalDatum(ellipsoid, wgsInfo, DatumType.HD_Geocentric, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);

            return horizontalDatum;
        }

        private static VerticalDatum ReadVerticalDatum(WktStreamTokenizer tokenizer)
        {
            //<vert datum> = VERT_DATUM["<name>", <datum type> {,<authority>}]
            string authority = string.Empty;
            long authorityCode = -1;

            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            var datumType = (DatumType) tokenizer.GetNumericValue();
            tokenizer.NextToken();
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "AUTHORITY")
                {
                    tokenizer.ReadAuthority(out authority, out authorityCode);
                    tokenizer.ReadCloser(bracket);
                }
            }
            var verticalDatum = new VerticalDatum( datumType, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);

            return verticalDatum;
        }

        private static PrimeMeridian ReadPrimeMeridian(WktStreamTokenizer tokenizer)
        {
            //PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double longitude = tokenizer.GetNumericValue();

            tokenizer.NextToken();
            string authority = string.Empty;
            long authorityCode = -1;
            if (tokenizer.GetStringValue() == ",")
            {
                tokenizer.ReadAuthority(out authority, out authorityCode);
                tokenizer.ReadCloser(bracket);
            }
            else
                tokenizer.CheckCloser(bracket);

            // make an assumption about the Angular units - degrees.
            var primeMeridian = new PrimeMeridian(longitude, AngularUnit.Degrees, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);

            return primeMeridian;
        }

        private static FittedCoordinateSystem ReadFittedCoordinateSystem (WktStreamTokenizer tokenizer)
        {
            /*
             FITTED_CS[
                 "Local coordinate system MNAU (based on Gauss-Krueger)",
                 PARAM_MT[
                    "Affine",
                    PARAMETER["num_row",3],
                    PARAMETER["num_col",3],
                    PARAMETER["elt_0_0", 0.883485346527455],
                    PARAMETER["elt_0_1", -0.468458794848877],
                    PARAMETER["elt_0_2", 3455869.17937689],
                    PARAMETER["elt_1_0", 0.468458794848877],
                    PARAMETER["elt_1_1", 0.883485346527455],
                    PARAMETER["elt_1_2", 5478710.88035753],
                    PARAMETER["elt_2_2", 1],
                 ],
                 PROJCS["DHDN / Gauss-Kruger zone 3", GEOGCS["DHDN", DATUM["Deutsches_Hauptdreiecksnetz", SPHEROID["Bessel 1841", 6377397.155, 299.1528128, AUTHORITY["EPSG", "7004"]], TOWGS84[612.4, 77, 440.2, -0.054, 0.057, -2.797, 0.525975255930096], AUTHORITY["EPSG", "6314"]], PRIMEM["Greenwich", 0, AUTHORITY["EPSG", "8901"]], UNIT["degree", 0.0174532925199433, AUTHORITY["EPSG", "9122"]], AUTHORITY["EPSG", "4314"]], UNIT["metre", 1, AUTHORITY["EPSG", "9001"]], PROJECTION["Transverse_Mercator"], PARAMETER["latitude_of_origin", 0], PARAMETER["central_meridian", 9], PARAMETER["scale_factor", 1], PARAMETER["false_easting", 3500000], PARAMETER["false_northing", 0], AUTHORITY["EPSG", "31467"]]
                 AUTHORITY["CUSTOM","12345"]
             ]
            */
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord ();
            tokenizer.ReadToken (",");
            tokenizer.ReadToken ("PARAM_MT");
            var toBaseTransform = MathTransformWktReader.ReadMathTransform (tokenizer);
            tokenizer.ReadToken (",");
            tokenizer.NextToken ();
            var baseCS = ReadCoordinateSystem (null, tokenizer);

            string authority = string.Empty;
            long authorityCode = -1;

            var ct = tokenizer.NextToken ();
            while (ct != TokenType.Eol && ct != TokenType.Eof)
            {
                switch (tokenizer.GetStringValue ())
                {
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);

                        break;
                    case "AUTHORITY":
                        tokenizer.ReadAuthority (out authority, out authorityCode);
                        //tokenizer.ReadCloser(bracket);
                        break;
                }
                ct = tokenizer.NextToken ();
            }

            var fittedCS = new FittedCoordinateSystem (baseCS, toBaseTransform, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
            return fittedCS;
        }
    }
}
