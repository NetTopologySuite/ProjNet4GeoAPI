using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.IO.Wkt.Core;
using ProjNet.IO.Wkt.Tree;
using Unit = ProjNet.CoordinateSystems.Unit;

namespace ProjNet.Wkt
{
    /// <summary>
    /// WktToProjConverter - Visitor for converting Wkt to Proj objects.
    /// </summary>
    public class WktToProjConverter : IWktTraverseHandler
    {
        /// <summary>
        /// Subclass for holding conversion results.
        /// </summary>
        public class WktToProjRecord
        {
            /// <summary>
            /// The Wkt Object.
            /// </summary>
            public IWktObject WktObject { get; set; }

            /// <summary>
            /// The Proj4Net Object.
            /// </summary>
            public object ProjObject { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="wktObject"></param>
            /// <param name="projObject"></param>
            public WktToProjRecord(IWktObject wktObject, object projObject)
            {
                WktObject = wktObject;
                ProjObject = projObject;
            }
        }

        private Dictionary<IWktObject, object> table = new Dictionary<IWktObject, object>();

        private CoordinateSystemFactory factory;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="factory"></param>
        public WktToProjConverter(CoordinateSystemFactory factory = null)
        {
            this.factory = factory ?? new CoordinateSystemFactory();
        }


        /// <summary>
        /// Handler for WktAuthority.
        /// </summary>
        /// <param name="authority"></param>
        public void Handle(WktAuthority authority)
        {
            //table.Add(new WktToProjRecord(authority, null));
            table[authority] = null;
        }

        /// <summary>
        /// Handler for WktAxis.
        /// </summary>
        /// <param name="axis"></param>
        public void Handle(WktAxis axis)
        {
            var a = new AxisInfo(axis.Name, Map(axis.Direction));
            //table.Add(new WktToProjRecord(axis, a));
            table[axis] = a;
        }

        /// <summary>
        /// Handler for WktDatum.
        /// </summary>
        /// <param name="datum"></param>
        public void Handle(WktDatum datum)
        {
            //table.Add(new WktToProjRecord(datum, null));
            table[datum] = null;
        }

        /// <summary>
        /// Handler for WktEllipsoid.
        /// </summary>
        /// <param name="ellipsoid"></param>
        public void Handle(WktEllipsoid ellipsoid)
        {
            //table.Add(new WktToProjRecord(ellipsoid, null));
            table[ellipsoid] = null;
        }

        /// <summary>
        /// Handler for WktSpheroid.
        /// </summary>
        /// <param name="spheroid"></param>
        public void Handle(WktSpheroid spheroid)
        {
            //table.Add(new WktToProjRecord(spheroid, null));
            table[spheroid] = null;
        }

        /// <summary>
        /// Handler for WktExtension.
        /// </summary>
        /// <param name="extension"></param>
        public void Handle(WktExtension extension)
        {
            //table.Add(new WktToProjRecord(extension, null));
            table[extension] = null;
        }

        /// <summary>
        /// Handler for WktUnit.
        /// </summary>
        /// <param name="unit"></param>
        public void Handle(WktUnit unit)
        {
            string authName = unit.Authority?.Name;
            long? authCode = unit.Authority?.Code;

            var u = new Unit(unit.ConversionFactor, unit.Name, authName, authCode.GetValueOrDefault(),
                string.Empty, string.Empty, string.Empty);

            //table.Add(new WktToProjRecord(unit, u));
            table[unit] = u;
        }

        /// <inheritdoc/>
        public void Handle(WktParameter parameter)
        {
            var p = new ProjectionParameter(parameter.Name, parameter.Value);
            //table.Add(new WktToProjRecord(parameter, p));
            table[parameter] = p;
        }

        /// <inheritdoc/>
        public void Handle(WktParameterMathTransform pmt)
        {
            //table.Add(new WktToProjRecord(pmt, null));
            table[pmt] = null;
        }

        /// <inheritdoc/>
        public void Handle(WktPrimeMeridian meridian)
        {
            //table.Add(new WktToProjRecord(meridian, null));
            table[meridian] = null;
        }

        /// <inheritdoc/>
        public void Handle(WktProjection projection)
        {
            //table.Add(new WktToProjRecord(projection, null));
            table[projection] = null;
        }

        /// <inheritdoc/>
        public void Handle(WktToWgs84 toWgs84)
        {
            var wgs84 = new Wgs84ConversionInfo(toWgs84.DxShift, toWgs84.DyShift, toWgs84.DzShift, toWgs84.ExRotation,
                toWgs84.EyRotation, toWgs84.EzRotation, toWgs84.PpmScaling, toWgs84.Description);

            //table.Add(new WktToProjRecord(toWgs84, wgs84));i
            table[toWgs84] = wgs84;
        }

        /// <inheritdoc/>
        public void Handle(WktGeocentricCoordinateSystem cs)
        {
            //table.Add(new WktToProjRecord(cs, null));
            table[cs] = null;
        }

        /// <inheritdoc/>
        public void Handle(WktGeographicCoordinateSystem cs)
        {
            //table.Add(new WktToProjRecord(cs, null));
            table[cs] = null;
        }

        /// <inheritdoc/>
        public void Handle(WktProjectedCoordinateSystem cs)
        {
            //table.Add(new WktToProjRecord(cs, null));
            table[cs] = null;
        }

        /// <inheritdoc/>
        public void Handle(WktFittedCoordinateSystem cs)
        {
            //table.Add(new WktToProjRecord(cs, null));
            table[cs] = null;
        }


        /// <summary>
        /// Convert WktObject(s) to Proj CoordinateSystem.
        /// </summary>
        /// <param name="wktObject"></param>
        /// <returns></returns>
        public CoordinateSystem Convert(IWktObject wktObject)
        {
            // Make sure the table is empty before starting...
            table.Clear();

            // Travel the tree and make sure above Handle methods are called.
            wktObject.Traverse(this);

            // Recursive checking and filling the table...
            return (CoordinateSystem)FindOrCreateProjObject(wktObject);
        }

        private object FindOrCreateProjObject<T>(T wkt)
            where T : IWktObject
        {
            if (wkt == null)
                return null;

            object item = table[wkt];//.FirstOrDefault(x => x.WktObject.Equals(wkt));

            // Check if already exists
            if (item != null)
                return item;

            // Fill in the gaps where projObject is null;
            // Dispatch to Create methods...
            if (wkt is WktProjectedCoordinateSystem projcs)
                return Create(projcs);
            else if (wkt is WktGeographicCoordinateSystem geogcs)
                return Create(geogcs);
            else if (wkt is WktGeocentricCoordinateSystem geoccs)
                return Create(geoccs);
            else if (wkt is WktFittedCoordinateSystem fitcs)
                return Create(fitcs);
            else if (wkt is WktParameterMathTransform pmt)
                return Create(pmt);
            else if (wkt is WktProjection prj)
                return Create(prj);
            else if (wkt is WktDatum d)
                return Create(d);
            else if (wkt is WktEllipsoid e)
                return Create(e);
            else if (wkt is WktPrimeMeridian pm)
                return Create(pm);

            return null;
        }

        private object Create(WktProjectedCoordinateSystem projcs)
        {
            var gcs = (GeographicCoordinateSystem)FindOrCreateProjObject(projcs.GeographicCoordinateSystem);
            var p = (Projection) FindOrCreateProjObject(projcs.Projection);

            var u = (Unit) FindOrCreateProjObject(projcs.Unit);
            var lu = (LinearUnit) null;
            if (u != null)
            {
                lu = new LinearUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                    string.Empty, string.Empty);
            }

            var ax1 = (AxisInfo) FindOrCreateProjObject(projcs.Axes.ElementAtOrDefault(0));
            ax1 = ax1 ?? new AxisInfo("X", AxisOrientationEnum.East);

            var ax2 = (AxisInfo) FindOrCreateProjObject(projcs.Axes.ElementAtOrDefault(1));
            ax2 = ax2 ?? new AxisInfo("Y", AxisOrientationEnum.North);

            var result = new ProjectedCoordinateSystem(gcs.HorizontalDatum, gcs, lu, p, new List<AxisInfo>{ax1, ax2}, projcs.Name, projcs.Authority?.Name, projcs.Authority!=null ? projcs.Authority.Code : -1, string.Empty, string.Empty, string.Empty);

            return FillItem(projcs, result);
        }

        private object Create(WktGeographicCoordinateSystem geogcs)
        {
            var u = (Unit) FindOrCreateProjObject(geogcs.AngularUnit);
            AngularUnit au = null;
            if (u != null)
            {
                au = new AngularUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                    string.Empty, string.Empty);
            }

            var d = (HorizontalDatum) FindOrCreateProjObject(geogcs.HorizontalDatum);
            var pm = (PrimeMeridian) FindOrCreateProjObject(geogcs.PrimeMeridian);

            //This is default axis values if not specified.
            var ax1 = (AxisInfo) FindOrCreateProjObject(geogcs.Axes.ElementAtOrDefault(0));
            ax1 = ax1 ?? new AxisInfo("Lon", AxisOrientationEnum.East);
            var ax2 = (AxisInfo) FindOrCreateProjObject(geogcs.Axes.ElementAtOrDefault(1));
            ax2 = ax2 ?? new AxisInfo("Lat", AxisOrientationEnum.North);


            var result = this.factory.CreateGeographicCoordinateSystem(geogcs.Name, angularUnit:au, datum: d,
                primeMeridian: pm, axis0: ax1, axis1:ax2);

            if (geogcs.Authority != null)
            {
                result.Authority = geogcs.Authority.Name;
                result.AuthorityCode = geogcs.Authority.Code;
            }

            return FillItem(geogcs, result);
        }


        private object Create(WktFittedCoordinateSystem fitcs)
        {
            var baseCS = (ProjectedCoordinateSystem) FindOrCreateProjObject(fitcs.ProjectedCoordinateSystem);

            var toBaseTransform = (MathTransform) FindOrCreateProjObject(fitcs.ParameterMathTransform);

            var result = new FittedCoordinateSystem (baseCS, toBaseTransform, fitcs.Name, fitcs.Authority?.Name, fitcs.Authority!=null?fitcs.Authority.Code: -1, string.Empty, string.Empty, string.Empty);

            return FillItem(fitcs, result);
        }

        private object Create(WktGeocentricCoordinateSystem geoccs)
        {
            var u = (Unit) FindOrCreateProjObject(geoccs.Unit);
            var lu = (LinearUnit) null;
            if (u != null)
            {
                lu = new LinearUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                    string.Empty, string.Empty);
            }

            var d = (HorizontalDatum) FindOrCreateProjObject(geoccs.Datum);
            var pm = (PrimeMeridian) FindOrCreateProjObject(geoccs.PrimeMeridian);

            var result =
                this.factory.CreateGeocentricCoordinateSystem(geoccs.Name, datum: d, linearUnit: lu, primeMeridian: pm);

            if (geoccs.Authority != null)
            {
                result.Authority = geoccs.Authority.Name;
                result.AuthorityCode = geoccs.Authority.Code;
            }

            return FillItem(geoccs, result);
        }

        private object Create(WktProjection wkt)
        {
            var pp = new List<ProjectionParameter>();
            // Projection parameters come after Projection in WKT
            var subTable = table.Keys
                .SkipWhile(x => !(x is WktParameter))
                .TakeWhile(x => x is WktParameter)
                .ToList();

            if (subTable.Any())
            {
                foreach (var item in subTable)
                {
                    var p = (ProjectionParameter) FindOrCreateProjObject(item);
                    pp.Add(p);
                }
            }

            /*
             Factory method doesn't provide Authority and AuthorityCode and they are not settable afterward.
            var result =
                this.factory.CreateProjection(wkt.Name, string.Empty, pp);
            */
            long ac = -1;
            if (wkt.Authority != null)
            {
                ac = wkt.Authority.Code;
            }

            var result = new Projection(wkt.Name, pp, wkt.Name, wkt.Authority?.Name, ac, string.Empty,
                    string.Empty, string.Empty);

            return FillItem(wkt, result);
        }


        private object Create(WktDatum wkt)
        {
            var wgs84 = (Wgs84ConversionInfo) FindOrCreateProjObject(wkt.ToWgs84);
            var e = (Ellipsoid) FindOrCreateProjObject(wkt.Spheroid);

            var result = this.factory.CreateHorizontalDatum(wkt.Name, DatumType.HD_Geocentric, ellipsoid:e, toWgs84: wgs84);

            if (wkt.Authority != null)
            {
                result.Authority = wkt.Authority.Name;
                result.AuthorityCode = wkt.Authority.Code;
            }

            return FillItem(wkt, result);
        }


        private object Create(WktEllipsoid wkt)
        {
            var result = new Ellipsoid(wkt.SemiMajorAxis, 0.0, wkt.InverseFlattening, true, LinearUnit.Metre, wkt.Name, wkt.Authority?.Name, wkt.Authority!=null ? wkt.Authority.Code : -1, string.Empty, string.Empty, string.Empty);

            return FillItem(wkt, result);
        }


        private object Create(WktPrimeMeridian wkt)
        {
            var au = (AngularUnit) AngularUnit.Degrees;

            var result = this.factory.CreatePrimeMeridian(wkt.Name, angularUnit: au, longitude: wkt.Longitude);

            if (wkt.Authority != null)
            {
                result.Authority = wkt.Authority.Name;
                result.AuthorityCode = wkt.Authority.Code;
            }

            return FillItem(wkt, result);
        }


        private object FillItem(IWktObject wkt, object proj)
        {
            //int idx = table.FindIndex(x => x.WktObject.Equals(wkt));
            //table[idx].ProjObject = proj;
            table[wkt] = proj;
            return proj;
        }

        private object Create(WktParameterMathTransform input)
        {
            if (input.Name.Equals("Affine", StringComparison.InvariantCultureIgnoreCase))
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
                //tokenizer stands on the first PARAMETER
                var paramInfo = input;
                //manage required parameters - row, col
                var rowParam = paramInfo.Parameters.FirstOrDefault(x => x.Name == "num_row");
                var colParam = paramInfo.Parameters.FirstOrDefault(x => x.Name == "num_col");

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
                foreach (var param in paramInfo.Parameters)
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



        private AxisOrientationEnum Map(WktAxisDirectionEnum input)
        {
            // Numbers are equal so cast must succeed
            return (AxisOrientationEnum) input;
        }
    }
}
