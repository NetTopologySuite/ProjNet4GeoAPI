using System;
using System.Globalization;
using System.Text;
using ProjNet.CoordinateSystems.Wkt2;

namespace ProjNet.IO.CoordinateSystems
{
    /// <summary>
    /// Serializes WKT2 CRS model objects to WKT2 (OGC 18-010r7 / ISO 19162:2019) strings.
    /// </summary>
    public static class CoordinateSystemWkt2Writer
    {
        /// <summary>
        /// Writes a WKT2 string from a WKT2 CRS model.
        /// </summary>
        /// <param name="crs">The CRS model to serialize.</param>
        /// <returns>A WKT2 string.</returns>
        public static string Write(Wkt2CrsBase crs)
        {
            if (crs == null) throw new ArgumentNullException(nameof(crs));

            if (crs is Wkt2GeogCrs geog)
                return WriteGeogCrs(geog);
            if (crs is Wkt2ProjCrs proj)
                return WriteProjCrs(proj);
            if (crs is Wkt2VertCrs vert)
                return WriteVertCrs(vert);
            if (crs is Wkt2CompoundCrs compound)
                return WriteCompoundCrs(compound);
            if (crs is Wkt2BoundCrs bound)
                return WriteBoundCrs(bound);
            if (crs is Wkt2EngCrs eng)
                return WriteEngCrs(eng);
            if (crs is Wkt2ParametricCrs param)
                return WriteParametricCrs(param);

            throw new NotSupportedException($"WKT2 writer does not support '{crs.GetType().Name}'.");
        }

        private static string WriteEngCrs(Wkt2EngCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append("\",");

            sb.Append(WriteEngineeringDatum(crs.Datum));
            sb.Append(',');
            sb.Append(WriteCs(crs.CoordinateSystem));

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteEngineeringDatum(Wkt2EngineeringDatum datum)
        {
            var sb = new StringBuilder();
            sb.Append(datum.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(datum.Name));
            sb.Append('"');

            if (datum.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(datum.Id));
            }

            if (!string.IsNullOrWhiteSpace(datum.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(datum.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteParametricCrs(Wkt2ParametricCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append("\",");

            sb.Append(WriteParametricDatum(crs.Datum));
            sb.Append(',');
            sb.Append(WriteCs(crs.CoordinateSystem));

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteParametricDatum(Wkt2ParametricDatum datum)
        {
            var sb = new StringBuilder();
            sb.Append(datum.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(datum.Name));
            sb.Append('"');

            if (datum.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(datum.Id));
            }

            if (!string.IsNullOrWhiteSpace(datum.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(datum.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteBoundCrs(Wkt2BoundCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append("\",");

            sb.Append("SOURCECRS[");
            sb.Append(Write(crs.SourceCrs));
            sb.Append("],");

            sb.Append("TARGETCRS[");
            sb.Append(Write(crs.TargetCrs));
            sb.Append("],");

            sb.Append(WriteAbridgedTransformation(crs.Transformation));

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteAbridgedTransformation(Wkt2AbridgedTransformation transform)
        {
            var sb = new StringBuilder();
            sb.Append("ABRIDGEDTRANSFORMATION[\"");
            sb.Append(EscapeQuotedText(transform.Name));
            sb.Append("\",");
            sb.Append("METHOD[\"");
            sb.Append(EscapeQuotedText(transform.MethodName));
            sb.Append("\"]");

            foreach (var p in transform.Parameters)
            {
                sb.Append(',');
                sb.Append(WriteParameter(p));
            }

            if (transform.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(transform.Id));
            }

            if (!string.IsNullOrWhiteSpace(transform.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(transform.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteCompoundCrs(Wkt2CompoundCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append('"');

            foreach (var component in crs.Components)
            {
                sb.Append(',');
                sb.Append(Write(component));
            }

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteVertCrs(Wkt2VertCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append("\",");

            sb.Append(WriteVerticalDatum(crs.Datum));
            sb.Append(',');
            sb.Append(WriteCs(crs.CoordinateSystem));

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteVerticalDatum(Wkt2VerticalDatum datum)
        {
            var sb = new StringBuilder();
            sb.Append(datum.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(datum.Name));
            sb.Append("\"");

            if (datum.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(datum.Id));
            }

            if (!string.IsNullOrWhiteSpace(datum.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(datum.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteProjCrs(Wkt2ProjCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append("\",");

            // WKT2 has BASEGEOGCRS; we emit base CRS as GEOGCRS using the existing writer.
            sb.Append(WriteGeogCrs(crs.BaseCrs));
            sb.Append(',');
            sb.Append(WriteConversion(crs.Conversion));
            sb.Append(',');
            sb.Append(WriteCs(crs.CoordinateSystem));

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteConversion(Wkt2Conversion conversion)
        {
            var sb = new StringBuilder();
            sb.Append("CONVERSION[\"");
            sb.Append(EscapeQuotedText(conversion.Name));
            sb.Append("\",");
            sb.Append("METHOD[\"");
            sb.Append(EscapeQuotedText(conversion.MethodName));
            sb.Append("\"]");

            foreach (var p in conversion.Parameters)
            {
                sb.Append(',');
                sb.Append(WriteParameter(p));
            }

            if (conversion.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(conversion.Id));
            }

            if (!string.IsNullOrWhiteSpace(conversion.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(conversion.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteParameter(Wkt2Parameter parameter)
        {
            var sb = new StringBuilder();
            sb.Append("PARAMETER[\"");
            sb.Append(EscapeQuotedText(parameter.Name));
            sb.Append("\",");
            sb.Append(parameter.Value.ToString("R", CultureInfo.InvariantCulture));

            if (parameter.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(parameter.Id));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteGeogCrs(Wkt2GeogCrs crs)
        {
            var sb = new StringBuilder();
            sb.Append(crs.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(crs.Name));
            sb.Append("\",");

            sb.Append(WriteDatum(crs.Datum));

            if (crs.PrimeMeridian != null)
            {
                sb.Append(',');
                sb.Append(WritePrimeMeridian(crs.PrimeMeridian));
            }

            sb.Append(',');
            sb.Append(WriteCs(crs.CoordinateSystem));

            if (crs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(crs.Id));
            }

            if (!string.IsNullOrWhiteSpace(crs.Remark))
            {
                sb.Append(",REMARK[\"");
                sb.Append(EscapeQuotedText(crs.Remark));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteDatum(Wkt2GeodeticDatum datum)
        {
            var sb = new StringBuilder();

            sb.Append(datum.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(datum.Name));
            sb.Append("\",");
            sb.Append(WriteEllipsoid(datum.Ellipsoid));

            if (datum.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(datum.Id));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteEllipsoid(Wkt2Ellipsoid ellipsoid)
        {
            var sb = new StringBuilder();

            sb.Append("ELLIPSOID[\"");
            sb.Append(EscapeQuotedText(ellipsoid.Name));
            sb.Append("\",");
            sb.Append(ellipsoid.SemiMajorAxis.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(ellipsoid.InverseFlattening.ToString("R", CultureInfo.InvariantCulture));

            if (ellipsoid.LengthUnit != null)
            {
                sb.Append(',');
                sb.Append(WriteUnit(ellipsoid.LengthUnit));
            }

            if (ellipsoid.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(ellipsoid.Id));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WritePrimeMeridian(Wkt2PrimeMeridian pm)
        {
            var sb = new StringBuilder();

            sb.Append("PRIMEM[\"");
            sb.Append(EscapeQuotedText(pm.Name));
            sb.Append("\",");
            sb.Append(pm.Longitude.ToString("R", CultureInfo.InvariantCulture));

            if (pm.AngleUnit != null)
            {
                sb.Append(',');
                sb.Append(WriteUnit(pm.AngleUnit));
            }

            if (pm.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(pm.Id));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteCs(Wkt2CoordinateSystem cs)
        {
            var sb = new StringBuilder();

            sb.Append("CS[");
            sb.Append(cs.Type);
            sb.Append(',');
            sb.Append(cs.Dimension.ToString(CultureInfo.InvariantCulture));
            if (cs.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(cs.Id));
            }
            sb.Append(']');

            foreach (var axis in cs.Axes)
            {
                sb.Append(',');
                sb.Append(WriteAxis(axis));
            }

            if (cs.Unit != null)
            {
                sb.Append(',');
                sb.Append(WriteUnit(cs.Unit));
            }

            return sb.ToString();
        }

        private static string WriteAxis(Wkt2Axis axis)
        {
            var sb = new StringBuilder();

            sb.Append("AXIS[\"");
            sb.Append(EscapeQuotedText(axis.Name));
            sb.Append("\",");
            sb.Append(axis.Direction);

            if (axis.Order.HasValue)
            {
                sb.Append(",ORDER[");
                sb.Append(axis.Order.Value.ToString(CultureInfo.InvariantCulture));
                sb.Append(']');
            }

            if (axis.Unit != null)
            {
                sb.Append(',');
                sb.Append(WriteUnit(axis.Unit));
            }

            if (axis.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(axis.Id));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteUnit(Wkt2Unit unit)
        {
            var sb = new StringBuilder();

            sb.Append(unit.Keyword.ToUpperInvariant());
            sb.Append("[\"");
            sb.Append(EscapeQuotedText(unit.Name));
            sb.Append("\",");
            sb.Append(unit.ConversionFactor.ToString("R", CultureInfo.InvariantCulture));

            if (unit.Id != null)
            {
                sb.Append(',');
                sb.Append(WriteId(unit.Id));
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string WriteId(Wkt2Id id)
        {
            var sb = new StringBuilder();

            sb.Append("ID[\"");
            sb.Append(EscapeQuotedText(id.Authority));
            sb.Append("\",\"");
            sb.Append(EscapeQuotedText(id.Code));
            sb.Append("\"");

            if (!string.IsNullOrWhiteSpace(id.Version))
            {
                sb.Append(",\"");
                sb.Append(EscapeQuotedText(id.Version));
                sb.Append("\"");
            }

            if (!string.IsNullOrWhiteSpace(id.Uri))
            {
                sb.Append(",URI[\"");
                sb.Append(EscapeQuotedText(id.Uri));
                sb.Append("\"]");
            }

            sb.Append(']');
            return sb.ToString();
        }

        private static string EscapeQuotedText(string text)
        {
            // WKT2 escapes a quote inside a quoted string as double quote.
            return text.Replace("\"", "\"\"");
        }
    }
}
