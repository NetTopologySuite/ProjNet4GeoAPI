using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.IO.Wkt
{
    /// <summary>
    /// WktToProjBuilder
    /// </summary>
    public partial class WktToProjBuilder
    {
        private readonly CoordinateSystemFactory factory;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WktToProjBuilder()
        {
            factory = new CoordinateSystemFactory();
        }


        internal class Authority
        {
            public string Name { get; set; }

            public int Code { get; set; }
        }


        internal class NameVisitor : WktCrsBaseVisitor<string>
        {
            public static readonly NameVisitor Instance = new NameVisitor();

            public override string VisitName(WktCrsParser.NameContext context)
            {
                return context.GetText().Trim(new char[] {'\"', ' '});
            }
        }


        internal class ValueVisitor : WktCrsBaseVisitor<double>
        {
            public static readonly ValueVisitor Instance = new ValueVisitor();

            public override double VisitValue(WktCrsParser.ValueContext context)
            {
                string valueStr = context.GetText();
                if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                    return d;

                return double.NaN;
            }
        }


        /// <summary>
        /// LongitudeVisitor
        /// </summary>
        public class LongitudeVisitor : WktCrsBaseVisitor<double>
        {
            /// <summary>
            /// VisitLongitude
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            public override double VisitLongitude(WktCrsParser.LongitudeContext context)
            {
                string valueStr = context.GetText();
                if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
                    return d;

                return double.NaN;
            }
        }

        internal class AuthorityNameVisitor : WktCrsBaseVisitor<string>
        {
            public static readonly AuthorityNameVisitor Instance = new AuthorityNameVisitor();

            public override string VisitAuthorityName(WktCrsParser.AuthorityNameContext context)
            {
                return context.GetText().Trim(new char[] {'\"', ' '});
            }
        }


        internal class AuthorityCodeVisitor : WktCrsBaseVisitor<int>
        {
            public static readonly AuthorityCodeVisitor Instance = new AuthorityCodeVisitor();

            public override int VisitCode(WktCrsParser.CodeContext context)
            {
                if (!context.IsEmpty)
                {
                    string codeStr = context.GetText();
                    codeStr = codeStr.Trim(new char[] {' ', '\"'});
                    if (codeStr.Contains("_"))
                    {
                        codeStr = codeStr.Substring(0, codeStr.IndexOf('_'));
                    }

                    if (!string.IsNullOrWhiteSpace(codeStr) && int.TryParse(codeStr, NumberStyles.Any,
                            CultureInfo.InvariantCulture, out int nmbr))
                    {
                        return nmbr;
                    }
                }

                return -1;
            }
        }


        internal class ProjTextVisitor : WktCrsBaseVisitor<string>
        {
            public override string VisitProjtext(WktCrsParser.ProjtextContext context)
            {
                string str = context.GetText();
                str = str.Trim(new char[] {' ', '\"'});
                return str;
            }
        }

        internal class AxisOrientVisitor : WktCrsBaseVisitor<AxisOrientationEnum>
        {
            public override AxisOrientationEnum VisitAxisOrient(WktCrsParser.AxisOrientContext context)
            {

                if (!context.IsEmpty)
                {
                    string direction = context.GetText().Trim(new char[] {' ', '\"'});
                    if (Enum.TryParse(direction, true, out AxisOrientationEnum enumVal))
                    {
                        return enumVal;
                    }
                }

                return AxisOrientationEnum.Other;
            }
        }


        internal class AuthorityVisitor : WktCrsBaseVisitor<Authority>
        {
            public static readonly AuthorityVisitor Instance = new AuthorityVisitor();

            public override Authority VisitAuthority([NotNull] WktCrsParser.AuthorityContext context)
            {
                string authName = string.Empty;
                var authNameCtx = context.authorityName();
                if (authNameCtx != null)
                {
                    var visitor = AuthorityNameVisitor.Instance;
                    authName = visitor.VisitAuthorityName(authNameCtx);
                }

                int code = -1;
                var authCodeCtx = context.code();
                if (authCodeCtx != null)
                {
                    var visitor = AuthorityCodeVisitor.Instance;
                    code = visitor.VisitCode(authCodeCtx);
                }

                return new Authority {Name = authName, Code = code};
            }
        }

        internal class AxisVisitor : WktCrsBaseVisitor<AxisInfo>
        {
            public static readonly AxisVisitor Instance = new AxisVisitor();

            public override AxisInfo VisitAxis(WktCrsParser.AxisContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                var orient = AxisOrientationEnum.Other;
                var axisOrientCtx = context.axisOrient();
                if (axisOrientCtx != null)
                {
                    var visitor = new AxisOrientVisitor();
                    orient = visitor.VisitAxisOrient(axisOrientCtx);
                }

                return new AxisInfo(name, orient);
            }
        }

        internal class ExtensionVisitor : WktCrsBaseVisitor<(string, string)>
        {
            public static readonly ExtensionVisitor Instance = new ExtensionVisitor();

            public override (string, string) VisitExtension([NotNull] WktCrsParser.ExtensionContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                string projText = string.Empty;
                var projCtx = context.projtext();
                if (projCtx != null)
                {
                    var visitor = new ProjTextVisitor();
                    projText = visitor.VisitProjtext(projCtx);
                }

                // No ProjNet object for Extension so returning a tuple.
                return (name, projText);
            }
        }


        internal class ToWgs84Visitor : WktCrsBaseVisitor<Wgs84ConversionInfo>
        {
            public static readonly ToWgs84Visitor Instance = new ToWgs84Visitor();

            public override Wgs84ConversionInfo VisitTowgs84(WktCrsParser.Towgs84Context context)
            {
                double dx = 0.0d;
                var dxCtx = context.dx();
                if (!dxCtx.IsEmpty && double.TryParse(dxCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double dxResult))
                    dx = dxResult;

                double dy = 0.0d;
                var dyCtx = context.dy();
                if (!dyCtx.IsEmpty && double.TryParse(dyCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double dyResult))
                    dy = dyResult;

                double dz = 0.0d;
                var dzCtx = context.dz();
                if (!dzCtx.IsEmpty && double.TryParse(dzCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double dzResult))
                    dz = dzResult;

                double ex = 0.0d;
                var exCtx = context.ex();
                if (!exCtx.IsEmpty && double.TryParse(exCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double exResult))
                    ex = exResult;

                double ey = 0.0d;
                var eyCtx = context.ey();
                if (!eyCtx.IsEmpty && double.TryParse(eyCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double eyResult))
                    ey = eyResult;

                double ez = 0.0d;
                var ezCtx = context.ez();
                if (!ezCtx.IsEmpty && double.TryParse(ezCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double ezResult))
                    ez = ezResult;

                double ppm = 0.0d;
                var ppmCtx = context.ppm();
                if (ppmCtx.IsEmpty && double.TryParse(ppmCtx.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double ppmResult))
                    ppm = ppmResult;

                return new Wgs84ConversionInfo(dx, dy, dz, ex, ey, ez, ppm);
            }
        }


        internal class ProjectionVisitor : WktCrsBaseVisitor<Projection>
        {
            public static readonly ProjectionVisitor Instance = new ProjectionVisitor();

            public override Projection VisitProjection(WktCrsParser.ProjectionContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                return new Projection(name, new List<ProjectionParameter>(), name, authority?.Name,
                    authority != null ? authority.Code : -1, string.Empty, string.Empty, string.Empty);
            }
        }

        internal class ProjectionParameterVisitor : WktCrsBaseVisitor<ProjectionParameter>
        {
            public static readonly ProjectionParameterVisitor Instance = new ProjectionParameterVisitor();

            public override ProjectionParameter VisitParameter(WktCrsParser.ParameterContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                double value = double.NaN;
                var valueCtx = context.value();
                if (valueCtx != null)
                {
                    var visitor = ValueVisitor.Instance;
                    value = visitor.VisitValue(valueCtx);
                }

                return new ProjectionParameter(name, value);
            }
        }

        internal class ParameterVisitor : WktCrsBaseVisitor<Parameter>
        {
            public static readonly ParameterVisitor Instance = new ParameterVisitor();

            public override Parameter VisitParameter(WktCrsParser.ParameterContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                double value = double.NaN;
                var valueCtx = context.value();
                if (valueCtx != null)
                {
                    var visitor = ValueVisitor.Instance;
                    value = visitor.VisitValue(valueCtx);
                }

                return new Parameter(name, value);
            }
        }

        internal class SemiMajorAxisVisitor : WktCrsBaseVisitor<double>
        {
            public static readonly SemiMajorAxisVisitor Instance = new SemiMajorAxisVisitor();

            public override double VisitSemiMajorAxis(WktCrsParser.SemiMajorAxisContext context)
            {
                if (!context.IsEmpty && double.TryParse(context.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d))
                    return d;
                return double.NaN;
            }
        }

        internal class InverseFlatteningVisitor : WktCrsBaseVisitor<double>
        {
            public static readonly InverseFlatteningVisitor Instance = new InverseFlatteningVisitor();

            public override double VisitInverseFlattening(WktCrsParser.InverseFlatteningContext context)
            {
                if (!context.IsEmpty && double.TryParse(context.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d))
                    return d;
                return double.NaN;
            }
        }


        internal class SpheroidVisitor : WktCrsBaseVisitor<Ellipsoid>
        {
            public static readonly SpheroidVisitor Instance = new SpheroidVisitor();

            public override Ellipsoid VisitSpheroid(WktCrsParser.SpheroidContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                double semiMajorAxis = double.NaN;
                var smaCtx = context.semiMajorAxis();
                if (smaCtx != null)
                {
                    var visitor = new SemiMajorAxisVisitor();
                    semiMajorAxis = visitor.VisitSemiMajorAxis(smaCtx);
                }

                double inverseFlattening = double.NaN;
                var invfCtx = context.inverseFlattening();
                if (invfCtx != null)
                {
                    var visitor = new InverseFlatteningVisitor();
                    inverseFlattening = visitor.VisitInverseFlattening(invfCtx);
                }

                Authority authority = null;
                var authCtx = context.authority();
                if (authCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.Visit(authCtx);
                }

                return new Ellipsoid(semiMajorAxis, 0.0, inverseFlattening, true, LinearUnit.Metre, name,
                    authority?.Name, authority != null ? authority.Code : -1, string.Empty, string.Empty, string.Empty);

            }
        }


        internal class ConversionFactorVisitor : WktCrsBaseVisitor<double>
        {
            public override double VisitConversionFactor(WktCrsParser.ConversionFactorContext context)
            {
                if (!context.IsEmpty && double.TryParse(context.GetText(), NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double d))
                    return d;
                return double.NaN;
            }
        }


        internal class DatumVisitor : WktCrsBaseVisitor<HorizontalDatum>
        {
            public static readonly DatumVisitor Instance = new DatumVisitor();

            private readonly CoordinateSystemFactory factory = new CoordinateSystemFactory();

            public override HorizontalDatum VisitDatum(WktCrsParser.DatumContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                Ellipsoid spheroid = null;
                var spheroidCtx = context.spheroid();
                if (spheroidCtx != null)
                {
                    var visitor = SpheroidVisitor.Instance;
                    spheroid = visitor.VisitSpheroid(spheroidCtx);
                }

                Wgs84ConversionInfo wgs84 = null;
                var towgs84Ctx = context.towgs84();
                if (towgs84Ctx != null && towgs84Ctx.Length>0)
                {
                    var visitor = new ToWgs84Visitor();
                    wgs84 = visitor.VisitTowgs84(towgs84Ctx[0]);
                }

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null && authorityCtx.Length>0)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx[0]);
                }

                var result = this.factory.CreateHorizontalDatum(
                    name, DatumType.HD_Geocentric, ellipsoid: spheroid, toWgs84: wgs84);

                if (authority != null)
                {
                    result.Authority = authority.Name;
                    result.AuthorityCode = authority.Code;
                }

                return result;
            }
        }

        internal class UnitVisitor : WktCrsBaseVisitor<Unit>
        {
            public override Unit VisitUnit(WktCrsParser.UnitContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                double factor = double.NaN;
                var cfCtx = context.conversionFactor();
                if (cfCtx != null)
                {
                    var visitor = new ConversionFactorVisitor();
                    factor = visitor.VisitConversionFactor(cfCtx);
                }

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                return new Unit(factor, name, authority?.Name, authority != null ? authority.Code : -1, string.Empty,
                    string.Empty, string.Empty);
            }
        }

        internal class PrimemVisitor : WktCrsBaseVisitor<PrimeMeridian>
        {
            public static readonly PrimemVisitor Instance = new PrimemVisitor();

            private readonly CoordinateSystemFactory factory = new CoordinateSystemFactory();

            public override PrimeMeridian VisitPrimem(WktCrsParser.PrimemContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                double longitude = double.NaN;
                var ltCtx = context.longitude();
                if (ltCtx != null)
                {
                    var visitor = new LongitudeVisitor();
                    longitude = visitor.VisitLongitude(ltCtx);
                }

                var au = AngularUnit.Degrees;
                var unitCtx = context.unit();
                if (unitCtx != null)
                {
                    var visitor = new UnitVisitor();
                    var unit = visitor.VisitUnit(unitCtx);
                    if (unit != null && unit.Name.Equals("degree"))
                    {
                        au = new AngularUnit(unit.ConversionFactor, unit.Name, unit.Authority, unit.AuthorityCode,
                            string.Empty, string.Empty, string.Empty);
                    }
                }

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                var result = this.factory.CreatePrimeMeridian(name, angularUnit: au, longitude: longitude);

                if (authority is Authority authObj)
                {
                    result.AuthorityCode = authObj.Code;
                    result.Authority = authObj.Name;
                }

                return result;
            }
        }


        internal class GeographicCoordinateSystemVisitor : WktCrsBaseVisitor<GeographicCoordinateSystem>
        {
            public static readonly GeographicCoordinateSystemVisitor Instance = new GeographicCoordinateSystemVisitor();

            private readonly CoordinateSystemFactory factory = new CoordinateSystemFactory();

            public override GeographicCoordinateSystem VisitGeogcs(WktCrsParser.GeogcsContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                var au = AngularUnit.Degrees;
                var unitCtx = context.unit();
                if (unitCtx != null)
                {
                    var visitor = new UnitVisitor();
                    var unit = visitor.VisitUnit(unitCtx);
                    if (unit != null && unit.Name.Equals("degree"))
                    {
                        au = new AngularUnit(unit.ConversionFactor, unit.Name, unit.Authority, unit.AuthorityCode,
                            string.Empty, string.Empty, string.Empty);
                    }
                }

                HorizontalDatum datum = null;
                var datumCtx = context.datum();
                if (datumCtx != null)
                {
                    var visitor = DatumVisitor.Instance;
                    datum = visitor.VisitDatum(datumCtx);
                }

                PrimeMeridian pm = null;
                var pmCtx = context.primem();
                if (pmCtx != null)
                {
                    var visitor = PrimemVisitor.Instance;
                    pm = visitor.VisitPrimem(pmCtx);
                }

                //This is default axis values if not specified.
                var axVisitor = new AxisVisitor();
                var axisCtx = context.axis();
                var ax1 = axisCtx.Length > 0
                    ? axVisitor.VisitAxis(axisCtx[0])
                    : new AxisInfo("Lon", AxisOrientationEnum.East);
                var ax2 = axisCtx.Length > 1
                    ? axVisitor.VisitAxis(axisCtx[1])
                    : new AxisInfo("Lat", AxisOrientationEnum.North);

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                var result = this.factory.CreateGeographicCoordinateSystem(name, au, (HorizontalDatum) datum,
                    (PrimeMeridian) pm, axis0: ax1, axis1: ax2);

                if (authority is Authority authObj)
                {
                    result.AuthorityCode = authObj.Code;
                    result.Authority = authObj.Name;
                }

                return result;
            }
        }

        internal class GeocentricCoordinateSystemVisitor : WktCrsBaseVisitor<GeocentricCoordinateSystem>
        {
            public static readonly GeocentricCoordinateSystemVisitor Instance = new GeocentricCoordinateSystemVisitor();

            private readonly CoordinateSystemFactory factory = new CoordinateSystemFactory();

            public override GeocentricCoordinateSystem VisitGeoccs(WktCrsParser.GeoccsContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                HorizontalDatum datum = null;
                var datumCtx = context.datum();
                if (datumCtx != null)
                {
                    var visitor = DatumVisitor.Instance;
                    datum = visitor.VisitDatum(datumCtx);
                }

                PrimeMeridian meridian = null;
                var pmCtx = context.primem();
                if (pmCtx != null)
                {
                    var visitor = PrimemVisitor.Instance;
                    meridian = visitor.VisitPrimem(pmCtx);
                }

                var lu = (LinearUnit) null;
                var unitCtx = context.unit();
                if (unitCtx != null)
                {
                    var visitor = new UnitVisitor();
                    var u = visitor.VisitUnit(unitCtx);
                    if (u != null)
                    {
                        lu = new LinearUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                            string.Empty, string.Empty);
                    }
                }

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                var result =
                    this.factory.CreateGeocentricCoordinateSystem(name, (HorizontalDatum) datum, lu,
                        (PrimeMeridian) meridian);

                if (authority is Authority authObj)
                {
                    result.AuthorityCode = authObj.Code;
                    result.Authority = authObj.Name;
                }

                return result;
            }
        }


        internal class ProjectedCoordinateSystemVisitor : WktCrsBaseVisitor<ProjectedCoordinateSystem>
        {
            public static readonly ProjectedCoordinateSystemVisitor Instance = new ProjectedCoordinateSystemVisitor();

            public override ProjectedCoordinateSystem VisitProjcs(WktCrsParser.ProjcsContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                GeographicCoordinateSystem gcs = null;
                var gcsCtx = context.geogcs();
                if (gcsCtx != null)
                {
                    var visitor = GeographicCoordinateSystemVisitor.Instance;
                    gcs = visitor.VisitGeogcs(gcsCtx);
                }

                var lu = (LinearUnit) null;
                var unitCtx = context.unit();
                if (unitCtx != null)
                {
                    var visitor = new UnitVisitor();
                    var u = visitor.VisitUnit(unitCtx);
                    if (u != null)
                    {
                        lu = new LinearUnit(u.ConversionFactor, u.Name, u.Authority, u.AuthorityCode, string.Empty,
                            string.Empty, string.Empty);
                    }
                }

                Projection p = null;
                var projCtx = context.projection();
                if (projCtx != null)
                {
                    var visitor = new ProjectionVisitor();
                    p = visitor.VisitProjection(projCtx);
                }


                var axVisitor = new AxisVisitor();
                var axisCtx = context.axis();
                var ax1 = axisCtx.Length > 0
                    ? axVisitor.VisitAxis(axisCtx[0])
                    : new AxisInfo("X", AxisOrientationEnum.East);
                var ax2 = axisCtx.Length > 1
                    ? axVisitor.VisitAxis(axisCtx[1])
                    : new AxisInfo("Y", AxisOrientationEnum.North);
                var aa = new List<AxisInfo> {ax1, ax2};

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                var result = new ProjectedCoordinateSystem(gcs.HorizontalDatum, gcs, lu, p,
                    aa, name, authority?.Name, authority != null ? authority.Code : -1, string.Empty, string.Empty,
                    string.Empty);

                return result;
            }
        }

        internal class ParamsMathTransformVisitor : WktCrsBaseVisitor<AffineTransform>
        {
            public static readonly ParamsMathTransformVisitor Instance = new ParamsMathTransformVisitor();

            public override AffineTransform VisitParamsmt(WktCrsParser.ParamsmtContext context)
            {
                string name = "";
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                var parameters = new List<Parameter>();
                var paramCtx = context.parameter();
                if (paramCtx != null)
                {
                    var visitor = new ParameterVisitor();
                    parameters = paramCtx.Select(pc => visitor.VisitParameter(pc)).ToList();
                }

                if (name.Equals("Affine", StringComparison.InvariantCultureIgnoreCase) && parameters.Any())
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


                    var p = parameters;
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
        }


        internal class FittedCoordinateSystemVisitor : WktCrsBaseVisitor<FittedCoordinateSystem>
        {
            public static readonly FittedCoordinateSystemVisitor Instance = new FittedCoordinateSystemVisitor();

            public override FittedCoordinateSystem VisitFittedcs(WktCrsParser.FittedcsContext context)
            {
                string name = string.Empty;
                var nameCtx = context.name();
                if (nameCtx != null)
                {
                    var visitor = NameVisitor.Instance;
                    name = visitor.VisitName(nameCtx);
                }

                MathTransform mathTransform = null;
                var pmtCtx = context.paramsmt();
                if (pmtCtx != null)
                {
                    var visitor = new ParamsMathTransformVisitor();
                    mathTransform = visitor.VisitParamsmt(pmtCtx);
                }

                ProjectedCoordinateSystem baseCS = null;
                var projcsCtx = context.projcs();
                if (projcsCtx != null)
                {
                    var visitor = new ProjectedCoordinateSystemVisitor();
                    baseCS = visitor.VisitProjcs(projcsCtx);
                }

                Authority authority = null;
                var authorityCtx = context.authority();
                if (authorityCtx != null)
                {
                    var visitor = AuthorityVisitor.Instance;
                    authority = visitor.VisitAuthority(authorityCtx);
                }

                var result = new FittedCoordinateSystem(baseCS, mathTransform, name, authority?.Name,
                    authority != null ? authority.Code : -1, string.Empty, string.Empty, string.Empty);

                return result;
            }
        }


        internal class WktCrsVisitor : WktCrsBaseVisitor<CoordinateSystem>
        {
            public static readonly WktCrsVisitor Instance = new WktCrsVisitor();

            public override CoordinateSystem VisitWkt(WktCrsParser.WktContext context)
            {
                var projcsCtx = context.projcs();
                if (projcsCtx != null)
                {
                    var visitor = ProjectedCoordinateSystemVisitor.Instance;
                    return visitor.VisitProjcs(projcsCtx);
                }

                var gcsCtx = context.geogcs();
                if (gcsCtx != null)
                {
                    var visitor = GeographicCoordinateSystemVisitor.Instance;
                    return visitor.VisitGeogcs(gcsCtx);
                }

                var ccsCtx = context.geoccs();
                if (ccsCtx != null)
                {
                    var visitor = GeocentricCoordinateSystemVisitor.Instance;
                    return visitor.VisitGeoccs(ccsCtx);
                }

                //if (context.compdcs() != null)
                    //return (CoordinateSystem) Visit(context.compdcs());
                    var fcsCtx = context.fittedcs();
                if (fcsCtx != null)
                {
                    var visitor = FittedCoordinateSystemVisitor.Instance;
                    return visitor.VisitFittedcs(fcsCtx);
                }
                /*
                else if (context.localcs()!=null)
                    return (CoordinateSystem) Visit(context.localcs());
                else if (context.vertcs()!=null)
                    return (CoordinateSystem) Visit(context.vertcs());
                    */

                return base.VisitWkt(context);
            }
        }

        private static WktCrsLexer cachedLexer = null;
        private static WktCrsParser cachedParser = null;


        /// <summary>
        /// ParseAndBuild
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static CoordinateSystem ParseAndBuild(string input)
        {
            try
            {
                var stream = CharStreams.fromString(input);
                var lexer = cachedLexer;
                if (lexer == null)
                {
                    lexer = new WktCrsLexer(stream);
                    cachedLexer = lexer;
                }
                else
                {
                    lexer.SetInputStream(stream);
                }

                var tokens = new CommonTokenStream(lexer);

                var parser = cachedParser;
                if (parser == null)
                {
                    parser = new WktCrsParser(tokens);
                    parser.BuildParseTree = true;
                    cachedParser = parser;
                }
                else
                {
                    //parser.BuildParseTree = false;
                    parser.TokenStream = tokens;
                }

                parser.Interpreter.PredictionMode = Antlr4.Runtime.Atn.PredictionMode.SLL;

                bool doProfile = false;
                parser.Profile = doProfile;

                var wktCtx = parser.wkt();
                if (wktCtx != null)
                {
                    var result = WktCrsVisitor.Instance.VisitWkt(wktCtx);

                    if (doProfile)
                    {
                        Console.WriteLine("Profile results: \n" + GetProfileInfo(parser));
                    }

                    return result;
                }
            }
            catch (RecognitionException)
            {
                return null;
            }
            catch (ParseCanceledException)
            {
                return null;
            }
            return null;
        }


        private static string GetProfileInfo(WktCrsParser parser)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0,-35}", "rule");
            sb.AppendFormat("{0,-15}", "time");
            sb.AppendFormat("{0,-15}", "invocations");
            sb.AppendFormat("{0,-15}", "lookahead");
            sb.AppendFormat("{0,-15}", "lookahead(max)");
            sb.AppendFormat("{0,-15}", "ambiguities");
            sb.AppendFormat("{0,-15}", "errors");
            sb.AppendLine();
            foreach (var decisionInfo in parser.ParseInfo.getDecisionInfo())
            {
                var ds = parser.Atn.GetDecisionState(decisionInfo.decision);
                string rule = parser.RuleNames[ds.ruleIndex];
                if (decisionInfo.timeInPrediction > 0)
                {
                    sb.AppendFormat("{0,-35}", rule);
                    sb.AppendFormat("{0,-15}", decisionInfo.timeInPrediction);
                    sb.AppendFormat("{0,-15}", decisionInfo.invocations);
                    sb.AppendFormat("{0,-15}", decisionInfo.SLL_TotalLook);
                    sb.AppendFormat("{0,-15}", decisionInfo.SLL_MaxLook);
                    sb.AppendFormat("{0,-15}", decisionInfo.ambiguities.Count);
                    sb.AppendFormat("{0,-15}", decisionInfo.errors.Count);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

    }
}
