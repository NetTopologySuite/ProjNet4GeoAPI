using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Pidgin;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt.Core;

using static ProjNet.IO.Wkt.Core.WktParser;


namespace ProjNet.Wkt
{

    public class Authority
    {
        public string Name { get; set; }

        public int Code { get; set; }
    }


    public class WktToProjBuilder : IWktBuilder
    {
        private readonly CoordinateSystemFactory factory;
        private readonly Func<string, Result<char, CoordinateSystem>> Parser;


        public WktToProjBuilder()
        {
            factory = new CoordinateSystemFactory();

            Parser = WktParserResolver<CoordinateSystem>(this);
        }

        public object BuildAuthority(int start, string keyword, char leftDelimiter, string name, int code,
            char rightDelimiter)
        {
            return new Authority{Name = name, Code =code};
        }

        public object BuildAxis(int offset, string keyword, char leftDelimiter, string name, string direction,
            char rightDelimiter)
        {
            var orientationEnum = (AxisOrientationEnum)Enum.Parse(typeof(AxisOrientationEnum), direction, true);
            return new AxisInfo(name, orientationEnum);
        }

        public object BuildExtension(int offset, string keyword, char leftDelimiter, string name, string direction,
            char rightDelimiter)
        {
            return name;
        }

        public object BuildToWgs84(int offset, string keyword, char leftDelimiter, double dxShift, double dyShift, double dzShift,
            double exRotation, double eyRotation, double ezRotation, double ppmScaling, string description, char rightDelimiter)
        {
            return new Wgs84ConversionInfo(dxShift, dyShift, dzShift, exRotation, eyRotation, ezRotation, ppmScaling, description);
        }

        public object BuildProjection(int offset, string keyword, char leftDelimiter, string name, object authority,
            char rightDelimiter)
        {
            var result = new Projection(name, null, name, string.Empty, -1, string.Empty,
                    string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildProjectionParameter(int offset, string keyword, char leftDelimiter, string name, double value,
            char rightDelimiter)
        {
            return new ProjectionParameter(name, value);
        }

        public object BuildParameter(int offset, string keyword, char leftDelimiter, string name, double value,
            char rightDelimiter)
        {
            return new Parameter(name, value);
        }

        public object BuildEllipsoid(int offset, string keyword, char leftDelimiter, string name, double semiMajorAxis,
            double inverseFlattening, object authority, char rightDelimiter)
        {

            var result = new Ellipsoid(semiMajorAxis, 0.0, inverseFlattening, true, LinearUnit.Metre, name,
                string.Empty, -1, string.Empty, string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }

            return result;
        }

        public object BuildSpheroid(int offset, string keyword, char leftDelimiter, string name, double semiMajorAxis,
            double inverseFlattening, object authority, char rightDelimiter)
        {

            var result = new Ellipsoid(semiMajorAxis, 0.0, inverseFlattening, true, LinearUnit.Metre, name,
                string.Empty, -1, string.Empty, string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildPrimeMeridian(int offset, string keyword, char leftDelimiter, string name, double longitude,
            object authority, char rightDelimiter)
        {
            var au = (AngularUnit) AngularUnit.Degrees;

            var result = this.factory.CreatePrimeMeridian(name, angularUnit: au, longitude: longitude);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildUnit(int offset, string keyword, char leftDelimiter, string name, double factor, object authority,
            char rightDelimiter)
        {

            var result = new ProjNet.CoordinateSystems.Unit(factor,name, string.Empty, -1 , string.Empty, string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildLinearUnit(int offset, string keyword, char leftDelimiter, string name, double factor,
            object authority, char rightDelimiter)
        {
            var result = new ProjNet.CoordinateSystems.Unit(factor,name, string.Empty, -1, string.Empty, string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildAngularUnit(int offset, string keyword, char leftDelimiter, string name, double factor,
            object authority, char rightDelimiter)
        {


            var result = new ProjNet.CoordinateSystems.Unit(factor,name, string.Empty, -1, string.Empty, string.Empty, string.Empty);
            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildDatum(int offset, string keyword, char leftDelimiter, string name, object spheroid, object toWgs84,
            object authority, char rightDelimiter)
        {
            var result = this.factory.CreateHorizontalDatum(name, DatumType.HD_Geocentric, ellipsoid:(Ellipsoid)spheroid, toWgs84: (Wgs84ConversionInfo)toWgs84);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildGeographicCoordinateSystem(int offset, string keyword, char leftDelimiter, string name, object datum,
            object meridian, object unit, IEnumerable<object> axes, object authority, char rightDelimiter)
        {

            var u = (ProjNet.CoordinateSystems.Unit) unit;
            AngularUnit au = null;
            if (u != null)
            {
                au = new AngularUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                    string.Empty, string.Empty);
            }

            IList<AxisInfo> a = axes.Cast<AxisInfo>().ToList();
            //This is default axis values if not specified.
            var ax1 = a.ElementAtOrDefault(0);
            ax1 = ax1 ?? new AxisInfo("Lon", AxisOrientationEnum.East);
            var ax2 = a.ElementAtOrDefault(1);
            ax2 = ax2 ?? new AxisInfo("Lat", AxisOrientationEnum.North);


            var result = this.factory.CreateGeographicCoordinateSystem(name, au, (HorizontalDatum)datum,
                (PrimeMeridian)meridian, axis0: ax1, axis1:ax2);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildGeocentricCoordinateSystem(int offset, string keyword, char leftDelimiter, string name, object datum,
            object meridian, object unit, IEnumerable<object> axes, object authority, char rightDelimiter)
        {
            var u = (ProjNet.CoordinateSystems.Unit) unit;
            var lu = (LinearUnit) null;
            if (u != null)
            {
                lu = new LinearUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                    string.Empty, string.Empty);
            }

            var result =
                this.factory.CreateGeocentricCoordinateSystem(name, (HorizontalDatum)datum, lu, (PrimeMeridian)meridian);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }

        public object BuildProjectedCoordinateSystem(int offset, string keyword, char leftDelimiter, string name, object geogcs,
            object projection, object parameters, object unit, IEnumerable<object> axes, object extension, object authority, char rightDelimiter)
        {
            var gcs = (GeographicCoordinateSystem) geogcs;
            var u = (ProjNet.CoordinateSystems.Unit) unit;
            var lu = (LinearUnit) null;
            if (u != null)
            {
                lu = new LinearUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                    string.Empty, string.Empty);
            }


            var pp = ((List<object>) parameters).Cast<ProjectionParameter>().ToList();
            var p = (Projection) projection;
            p.Parameters = new List<ProjectionParameter>();
            p.Parameters.AddRange(pp);

            var a = axes.Cast<AxisInfo>().ToList();
            var ax1 = (AxisInfo)a.ElementAtOrDefault(0);
            ax1 = ax1 ?? new AxisInfo("X", AxisOrientationEnum.East);

            var ax2 = (AxisInfo) axes.ElementAtOrDefault(1);
            ax2 = ax2 ?? new AxisInfo("Y", AxisOrientationEnum.North);

            var aa = new List<AxisInfo> {ax1, ax2};

            var result = new ProjectedCoordinateSystem(gcs.HorizontalDatum, gcs, lu, p, aa, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }




        public object BuildParameterMathTransform(int offset, string keyword, char leftDelimiter, string name, object parameters,
            char rightDelimiter)
        {
            if (name.Equals("Affine", StringComparison.InvariantCultureIgnoreCase))
            {
                /*
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
                        PARAMETER["elt_2_2", 1]
                     ]
                */

                var p = ((List<object>) parameters).Cast<Parameter>().ToList();
                var rowParam = p.FirstOrDefault(x => x.Name == "num_row");
                var colParam = p.FirstOrDefault(x => x.Name == "num_col");

                if (rowParam == null)
                {
                    throw new ArgumentNullException(nameof(rowParam),
                        "Affine transform does not contain 'num_row' parameter");
                }

                if (colParam == null)
                {
                    throw new ArgumentNullException(nameof(colParam),
                        "Affine transform does not contain 'num_col' parameter");
                }

                int rowVal = (int) rowParam.Value;
                int colVal = (int) colParam.Value;

                if (rowVal <= 0)
                {
                    throw new ArgumentException("Affine transform contains invalid value of 'num_row' parameter");
                }

                if (colVal <= 0)
                {
                    throw new ArgumentException("Affine transform contains invalid value of 'num_col' parameter");
                }

                //creates working matrix;
                double[,] matrix = new double[rowVal, colVal];

                //simply process matrix values - no elt_ROW_COL parsing
                foreach (var param in p)
                {
                    if (param == null || param.Name == null)
                    {
                        continue;
                    }

                    switch (param.Name)
                    {
                        case "num_row":
                        case "num_col":
                            break;
                        case "elt_0_0":
                            matrix[0, 0] = param.Value;
                            break;
                        case "elt_0_1":
                            matrix[0, 1] = param.Value;
                            break;
                        case "elt_0_2":
                            matrix[0, 2] = param.Value;
                            break;
                        case "elt_0_3":
                            matrix[0, 3] = param.Value;
                            break;
                        case "elt_1_0":
                            matrix[1, 0] = param.Value;
                            break;
                        case "elt_1_1":
                            matrix[1, 1] = param.Value;
                            break;
                        case "elt_1_2":
                            matrix[1, 2] = param.Value;
                            break;
                        case "elt_1_3":
                            matrix[1, 3] = param.Value;
                            break;
                        case "elt_2_0":
                            matrix[2, 0] = param.Value;
                            break;
                        case "elt_2_1":
                            matrix[2, 1] = param.Value;
                            break;
                        case "elt_2_2":
                            matrix[2, 2] = param.Value;
                            break;
                        case "elt_2_3":
                            matrix[2, 3] = param.Value;
                            break;
                        case "elt_3_0":
                            matrix[3, 0] = param.Value;
                            break;
                        case "elt_3_1":
                            matrix[3, 1] = param.Value;
                            break;
                        case "elt_3_2":
                            matrix[3, 2] = param.Value;
                            break;
                        case "elt_3_3":
                            matrix[3, 3] = param.Value;
                            break;
                    }
                }

                //use "matrix" constructor to create transformation matrix
                return new AffineTransform(matrix);
            }

            return null;
        }

        public object BuildFittedCoordinateSystem(int offset, string keyword, char leftDelimiter, string name, object pmt,
            object projcs, object authority, char rightDelimiter)
        {
            var baseCS = (ProjectedCoordinateSystem) projcs;

            var toBaseTransform = (MathTransform) pmt;

            var result = new FittedCoordinateSystem (baseCS, toBaseTransform, name, string.Empty, -1, string.Empty, string.Empty, string.Empty);

            if (authority is Authority authObj)
            {
                result.AuthorityCode = authObj.Code;
                result.Authority = authObj.Name;
            }


            return result;
        }



        public Result<char, CoordinateSystem> Parse(string wkt)
        {
            return Parser(wkt);
        }


    }
}
