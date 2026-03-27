using System;
using System.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Wkt2;

namespace ProjNet.IO.CoordinateSystems
{
    /// <summary>
    /// Reads and parses WKT2 (OGC 18-010r7 / ISO 19162:2019) CRS definitions.
    /// </summary>
    public static class CoordinateSystemWkt2Reader
    {
        /// <summary>
        /// Parses WKT2 into a native WKT2 model.
        /// </summary>
        public static Wkt2CrsBase ParseCrs(string wkt)
        {
            if (string.IsNullOrWhiteSpace(wkt))
                throw new ArgumentNullException(nameof(wkt));

            using (TextReader reader = new StringReader(wkt))
            {
                var tokenizer = new WktStreamTokenizer(reader);
                tokenizer.NextToken();

                string rootKeyword = tokenizer.GetStringValue();
                switch (rootKeyword.ToUpperInvariant())
                {
                    case "GEOGCRS":
                    case "GEOGRAPHICCRS":
                        return ReadGeogCrs(rootKeyword, tokenizer);
                    case "PROJCRS":
                    case "PROJECTEDCRS":
                        return ReadProjCrs(rootKeyword, tokenizer);
                    case "VERTCRS":
                    case "VERTICALCRS":
                        return ReadVertCrs(rootKeyword, tokenizer);
                    case "COMPOUNDCRS":
                        return ReadCompoundCrs(rootKeyword, tokenizer);
                    case "BOUNDCRS":
                        return ReadBoundCrs(rootKeyword, tokenizer);
                    case "ENGCRS":
                    case "ENGINEERINGCRS":
                        return ReadEngCrs(rootKeyword, tokenizer);
                    case "PARAMETRICCRS":
                        return ReadParametricCrs(rootKeyword, tokenizer);
                    default:
                        throw new ArgumentException($"'{rootKeyword}' is not recognized as a supported WKT2 CRS.");
                }
            }
        }

        /// <summary>
        /// Parses WKT2 and converts to existing ProjNet model (normalized to ProjNet conventions).
        /// </summary>
        public static IInfo Parse(string wkt)
        {
            var crs = ParseCrs(wkt);
            switch (crs)
            {
                case Wkt2GeogCrs geog:
                    return Wkt2Conversions.ToProjNetGeographicCoordinateSystem(geog);
                case Wkt2ProjCrs proj:
                    return Wkt2Conversions.ToProjNetProjectedCoordinateSystem(proj);
                default:
                    throw new NotSupportedException($"WKT2 CRS model '{crs.GetType().Name}' is not supported for conversion.");
            }

        }

        private static Wkt2EngCrs ReadEngCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // ENGCRS["name", EDATUM/DATUM[...], CS[...], AXIS..., UNIT..., ID..., ...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2EngineeringDatum datum = null;
            Wkt2CoordinateSystem cs = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "EDATUM":
                    case "DATUM":
                        datum = ReadEngineeringDatum(element, tokenizer);
                        break;
                    case "CS":
                        cs = ReadCoordinateSystem(tokenizer);
                        break;
                    case "AXIS":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("cartesian", 2);
                        cs.Axes.Add(ReadAxis(tokenizer));
                        break;
                    case "LENGTHUNIT":
                    case "UNIT":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("cartesian", 2);
                        cs.Unit = ReadUnit(element, tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (datum == null)
                            throw new ArgumentException("ENGCRS is missing EDATUM/DATUM.");
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("cartesian", 2);

                        var crs = new Wkt2EngCrs(keyword, name, datum, cs)
                        {
                            Id = id,
                            Remark = remark
                        };
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2EngineeringDatum ReadEngineeringDatum(string keyword, WktStreamTokenizer tokenizer)
        {
            // EDATUM/DATUM["name", ID[...], REMARK[...]]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            Wkt2Id id = null;
            string remark = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2EngineeringDatum(keyword, name) { Id = id, Remark = remark };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2ParametricCrs ReadParametricCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // PARAMETRICCRS["name", PDATUM/DATUM[...], CS[parametric,1], AXIS..., (PARAMETRICUNIT|UNIT)...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2ParametricDatum datum = null;
            Wkt2CoordinateSystem cs = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "PDATUM":
                    case "DATUM":
                        datum = ReadParametricDatum(element, tokenizer);
                        break;
                    case "CS":
                        cs = ReadCoordinateSystem(tokenizer);
                        break;
                    case "AXIS":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("parametric", 1);
                        cs.Axes.Add(ReadAxis(tokenizer));
                        break;
                    case "PARAMETRICUNIT":
                    case "UNIT":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("parametric", 1);
                        cs.Unit = ReadUnit(element, tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (datum == null)
                            throw new ArgumentException("PARAMETRICCRS is missing PDATUM/DATUM.");
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("parametric", 1);

                        var crs = new Wkt2ParametricCrs(keyword, name, datum, cs)
                        {
                            Id = id,
                            Remark = remark
                        };
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2ParametricDatum ReadParametricDatum(string keyword, WktStreamTokenizer tokenizer)
        {
            // PDATUM/DATUM["name", ID[...], REMARK[...]]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            Wkt2Id id = null;
            string remark = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2ParametricDatum(keyword, name) { Id = id, Remark = remark };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2BoundCrs ReadBoundCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // BOUNDCRS["name", SOURCECRS[...], TARGETCRS[...], ABRIDGEDTRANSFORMATION[...], ID[..]?, REMARK[..]?]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2CrsBase sourceCrs = null;
            Wkt2CrsBase targetCrs = null;
            Wkt2AbridgedTransformation transformation = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "SOURCECRS":
                        sourceCrs = ReadBoundCrsChildCrs(tokenizer);
                        break;
                    case "TARGETCRS":
                        targetCrs = ReadBoundCrsChildCrs(tokenizer);
                        break;
                    case "ABRIDGEDTRANSFORMATION":
                        transformation = ReadAbridgedTransformation(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (sourceCrs == null)
                            throw new ArgumentException("BOUNDCRS is missing SOURCECRS.");
                        if (targetCrs == null)
                            throw new ArgumentException("BOUNDCRS is missing TARGETCRS.");
                        if (transformation == null)
                            throw new ArgumentException("BOUNDCRS is missing ABRIDGEDTRANSFORMATION.");

                        var crs = new Wkt2BoundCrs(keyword, name, sourceCrs, targetCrs, transformation)
                        {
                            Id = id,
                            Remark = remark
                        };
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2CrsBase ReadBoundCrsChildCrs(WktStreamTokenizer tokenizer)
        {
            // SOURCECRS[TARGETCRS] wraps a CRS inside its own brackets.
            var bracket = tokenizer.ReadOpener();
            tokenizer.NextToken();

            Wkt2CrsBase crs = null;
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "GEOGCRS":
                    case "GEOGRAPHICCRS":
                        crs = ReadGeogCrs(element, tokenizer);
                        break;
                    case "PROJCRS":
                    case "PROJECTEDCRS":
                        crs = ReadProjCrs(element, tokenizer);
                        break;
                    case "VERTCRS":
                    case "VERTICALCRS":
                        crs = ReadVertCrs(element, tokenizer);
                        break;
                    case "COMPOUNDCRS":
                        crs = ReadCompoundCrs(element, tokenizer);
                        break;
                    case "ENGCRS":
                    case "ENGINEERINGCRS":
                        crs = ReadEngCrs(element, tokenizer);
                        break;
                    case "PARAMETRICCRS":
                        crs = ReadParametricCrs(element, tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (crs == null)
                            throw new ArgumentException("SOURCECRS/TARGETCRS has no CRS.");
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2AbridgedTransformation ReadAbridgedTransformation(WktStreamTokenizer tokenizer)
        {
            // ABRIDGEDTRANSFORMATION["name", METHOD["..."], PARAMETER[...], ...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            string methodName = null;
            var parameters = new System.Collections.Generic.List<Wkt2Parameter>();
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "METHOD":
                        methodName = ReadMethodName(tokenizer);
                        break;
                    case "PARAMETER":
                        parameters.Add(ReadParameter(tokenizer));
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (string.IsNullOrWhiteSpace(methodName))
                            throw new ArgumentException("ABRIDGEDTRANSFORMATION is missing METHOD.");
                        var transform = new Wkt2AbridgedTransformation(name, methodName);
                        foreach (var p in parameters)
                            transform.Parameters.Add(p);
                        transform.Id = id;
                        transform.Remark = remark;
                        return transform;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2CompoundCrs ReadCompoundCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // COMPOUNDCRS["name", <component1>, <component2>, ... , ID[..]?, REMARK[..]?]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            var components = new System.Collections.Generic.List<Wkt2CrsBase>();
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "GEOGCRS":
                    case "GEOGRAPHICCRS":
                        components.Add(ReadGeogCrs(element, tokenizer));
                        break;
                    case "PROJCRS":
                    case "PROJECTEDCRS":
                        components.Add(ReadProjCrs(element, tokenizer));
                        break;
                    case "VERTCRS":
                    case "VERTICALCRS":
                        components.Add(ReadVertCrs(element, tokenizer));
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (components.Count == 0)
                            throw new ArgumentException("COMPOUNDCRS has no component CRS.");

                        var crs = new Wkt2CompoundCrs(keyword, name, components)
                        {
                            Id = id,
                            Remark = remark
                        };
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }

                tokenizer.NextToken();
            }
        }

        private static Wkt2VertCrs ReadVertCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // VERTCRS["name", VDATUM/DATUM[...], CS[vertical,1], AXIS[...], LENGTHUNIT[...], ID[...], ...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2VerticalDatum datum = null;
            Wkt2CoordinateSystem cs = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "VDATUM":
                    case "DATUM":
                        datum = ReadVerticalDatum(element, tokenizer);
                        break;
                    case "CS":
                        cs = ReadCoordinateSystem(tokenizer);
                        break;
                    case "AXIS":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("vertical", 1);
                        cs.Axes.Add(ReadAxis(tokenizer));
                        break;
                    case "LENGTHUNIT":
                    case "UNIT":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("vertical", 1);
                        cs.Unit = ReadUnit(element, tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);

                        if (datum == null)
                            throw new ArgumentException("VERTCRS is missing VDATUM/DATUM.");
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("vertical", 1);

                        var crs = new Wkt2VertCrs(keyword, name, datum, cs)
                        {
                            Id = id,
                            Remark = remark
                        };
                        return crs;

                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }

                tokenizer.NextToken();
            }
        }

        private static Wkt2VerticalDatum ReadVerticalDatum(string keyword, WktStreamTokenizer tokenizer)
        {
            // VDATUM/DATUM["name", ID[...], REMARK[...]]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            Wkt2Id id = null;
            string remark = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2VerticalDatum(keyword, name) { Id = id, Remark = remark };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2ProjCrs ReadProjCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // PROJCRS["name", BASEGEOGCRS[...]|GEOGCRS[...], CONVERSION[...], CS[...], AXIS..., UNIT..., ID...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2GeogCrs baseCrs = null;
            Wkt2Conversion conversion = null;
            Wkt2CoordinateSystem cs = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "BASEGEOGCRS":
                    case "BASEGEODCRS":
                        baseCrs = ReadBaseGeogCrs(tokenizer);
                        break;
                    case "GEOGCRS":
                    case "GEOGRAPHICCRS":
                        baseCrs = ReadGeogCrs(element, tokenizer);
                        break;
                    case "CONVERSION":
                        conversion = ReadConversion(tokenizer);
                        break;
                    case "CS":
                        cs = ReadCoordinateSystem(tokenizer);
                        break;
                    case "AXIS":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("cartesian", 2);
                        cs.Axes.Add(ReadAxis(tokenizer));
                        break;
                    case "LENGTHUNIT":
                    case "UNIT":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("cartesian", 2);
                        cs.Unit = ReadUnit(element, tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (baseCrs == null)
                            throw new ArgumentException("PROJCRS is missing BASEGEOGCRS/GEOGCRS.");
                        if (conversion == null)
                            throw new ArgumentException("PROJCRS is missing CONVERSION.");
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("cartesian", 2);

                        var crs = new Wkt2ProjCrs(keyword, name, baseCrs, conversion, cs)
                        {
                            Id = id,
                            Remark = remark
                        };
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }

                tokenizer.NextToken();
            }
        }

        private static Wkt2GeogCrs ReadBaseGeogCrs(WktStreamTokenizer tokenizer)
        {
            // BASEGEOGCRS["name", DATUM/TRF[...], PRIMEM[...]?, ID...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2GeodeticDatum datum = null;
            Wkt2PrimeMeridian primeMeridian = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "DATUM":
                    case "TRF":
                    case "GEODETICDATUM":
                        datum = ReadGeodeticDatum(element, tokenizer);
                        break;
                    case "PRIMEM":
                    case "PRIMEMERIDIAN":
                        primeMeridian = ReadPrimeMeridian(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (datum == null)
                            throw new ArgumentException("BASEGEOGCRS is missing DATUM/TRF.");

                        var cs = new Wkt2CoordinateSystem("ellipsoidal", 2);
                        var crs = new Wkt2GeogCrs("GEOGCRS", name, datum, cs)
                        {
                            PrimeMeridian = primeMeridian,
                            Id = id,
                            Remark = remark
                        };
                        return crs;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2Conversion ReadConversion(WktStreamTokenizer tokenizer)
        {
            // CONVERSION["name", METHOD["..."], PARAMETER[...], ...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            string methodName = null;
            var parameters = new System.Collections.Generic.List<Wkt2Parameter>();
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "METHOD":
                        methodName = ReadMethodName(tokenizer);
                        break;
                    case "PARAMETER":
                        parameters.Add(ReadParameter(tokenizer));
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (string.IsNullOrWhiteSpace(methodName))
                            throw new ArgumentException("CONVERSION is missing METHOD.");
                        var conversion = new Wkt2Conversion(name, methodName);
                        foreach (var p in parameters)
                            conversion.Parameters.Add(p);
                        conversion.Id = id;
                        conversion.Remark = remark;
                        return conversion;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static string ReadMethodName(WktStreamTokenizer tokenizer)
        {
            // METHOD["...", ID[...], REMARK[...], ...]
            var bracket = tokenizer.ReadOpener();
            string methodName = tokenizer.ReadDoubleQuotedWord();

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ID":
                    case "REMARK":
                    case ",":
                        SkipUnknownElement(tokenizer);
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return methodName;
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }

                tokenizer.NextToken();
            }
        }

        private static Wkt2Parameter ReadParameter(WktStreamTokenizer tokenizer)
        {
            // PARAMETER["name", value, (UNIT[...]?) (ID[...]?)]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double value = tokenizer.GetNumericValue();

            Wkt2Id id = null;
            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case "UNIT":
                    case "LENGTHUNIT":
                    case "ANGLEUNIT":
                    case "SCALEUNIT":
                    case "TIMEUNIT":
                        // parsed but not stored yet
                        ReadUnit(element, tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2Parameter(name, value) { Id = id };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2GeogCrs ReadGeogCrs(string keyword, WktStreamTokenizer tokenizer)
        {
            // GEOGCRS["name", DATUM/TRF[...], PRIMEM[...]?, CS[...], AXIS..., (cs unit), ... ID[...] ...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();

            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2GeodeticDatum datum = null;
            Wkt2PrimeMeridian primeMeridian = null;
            Wkt2CoordinateSystem cs = null;
            Wkt2Id id = null;
            string remark = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "DATUM":
                    case "TRF":
                    case "GEODETICDATUM":
                        datum = ReadGeodeticDatum(element, tokenizer);
                        break;

                    case "PRIMEM":
                    case "PRIMEMERIDIAN":
                        primeMeridian = ReadPrimeMeridian(tokenizer);
                        break;

                    case "CS":
                        cs = ReadCoordinateSystem(tokenizer);
                        break;

                    case "AXIS":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("ellipsoidal", 2);
                        cs.Axes.Add(ReadAxis(tokenizer));
                        break;

                    case "ANGLEUNIT":
                    case "LENGTHUNIT":
                    case "UNIT":
                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("ellipsoidal", 2);
                        cs.Unit = ReadUnit(element, tokenizer);
                        break;

                    case "REMARK":
                        remark = ReadRemark(tokenizer);
                        break;

                    case "ID":
                        id = ReadId(tokenizer);
                        break;

                    case ",":
                        break;

                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);

                        if (datum == null)
                            throw new ArgumentException("GEOGCRS is missing DATUM/TRF.");

                        if (cs == null)
                            cs = new Wkt2CoordinateSystem("ellipsoidal", 2);

                        var crs = new Wkt2GeogCrs(keyword, name, datum, cs)
                        {
                            PrimeMeridian = primeMeridian,
                            Id = id,
                            Remark = remark
                        };
                        return crs;

                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }

                tokenizer.NextToken();
            }
        }

        private static Wkt2GeodeticDatum ReadGeodeticDatum(string keyword, WktStreamTokenizer tokenizer)
        {
            // DATUM["name", ELLIPSOID[...], ...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            Wkt2Ellipsoid ellipsoid = null;
            Wkt2Id id = null;

            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ELLIPSOID":
                    case "SPHEROID":
                        ellipsoid = ReadEllipsoid(tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        if (ellipsoid == null)
                            throw new ArgumentException("DATUM/TRF missing ELLIPSOID.");
                        return new Wkt2GeodeticDatum(keyword, name, ellipsoid) { Id = id };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2Ellipsoid ReadEllipsoid(WktStreamTokenizer tokenizer)
        {
            // ELLIPSOID["name", a, invf, LENGTHUNIT[...], ID[...]]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double semiMajorAxis = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double invFlattening = tokenizer.GetNumericValue();

            Wkt2Unit lengthUnit = null;
            Wkt2Id id = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "LENGTHUNIT":
                    case "UNIT":
                        lengthUnit = ReadUnit(element, tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2Ellipsoid(name, semiMajorAxis, invFlattening) { LengthUnit = lengthUnit, Id = id };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2PrimeMeridian ReadPrimeMeridian(WktStreamTokenizer tokenizer)
        {
            // PRIMEM["name", longitude, (ANGLEUNIT[...]?) (ID[...]?) ]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double longitude = tokenizer.GetNumericValue();

            Wkt2Unit unit = null;
            Wkt2Id id = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ANGLEUNIT":
                    case "UNIT":
                        unit = ReadUnit(element, tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2PrimeMeridian(name, longitude) { AngleUnit = unit, Id = id };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2CoordinateSystem ReadCoordinateSystem(WktStreamTokenizer tokenizer)
        {
            // CS[ellipsoidal,2|3] (plus optional ID)
            var bracket = tokenizer.ReadOpener();
            tokenizer.NextToken();
            string csType = tokenizer.GetStringValue();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            int dimension = (int)tokenizer.GetNumericValue();

            Wkt2Id id = null;
            tokenizer.NextToken();
            while (tokenizer.GetStringValue() != "]" && tokenizer.GetStringValue() != ")")
            {
                if (tokenizer.GetStringValue().Equals("ID", StringComparison.OrdinalIgnoreCase))
                {
                    id = ReadId(tokenizer);
                }
                else
                {
                    SkipUnknownElement(tokenizer);
                }
                tokenizer.NextToken();
            }
            tokenizer.CheckCloser(bracket);

            return new Wkt2CoordinateSystem(csType, dimension) { Id = id };
        }

        private static Wkt2Axis ReadAxis(WktStreamTokenizer tokenizer)
        {
            // AXIS["name",direction,(ORDER[...])?,(UNIT[...]|ANGLEUNIT[...]|LENGTHUNIT[...])?,(ID[...])?]
            var bracket = tokenizer.ReadOpener();
            string axisName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            string direction = tokenizer.GetStringValue();

            int? order = null;
            Wkt2Unit unit = null;
            Wkt2Id id = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ORDER":
                        {
                            var b = tokenizer.ReadOpener();
                            tokenizer.NextToken();
                            order = (int)tokenizer.GetNumericValue();
                            tokenizer.ReadCloser(b);
                            break;
                        }
                    case "ANGLEUNIT":
                    case "LENGTHUNIT":
                    case "UNIT":
                        unit = ReadUnit(element, tokenizer);
                        break;
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2Axis(axisName, direction) { Order = order, Unit = unit, Id = id };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2Unit ReadUnit(string unitKeyword, WktStreamTokenizer tokenizer)
        {
            // ANGLEUNIT/LENGTHUNIT/UNIT["name",factor,(ID[...])...]
            var bracket = tokenizer.ReadOpener();
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double factor = tokenizer.GetNumericValue();

            Wkt2Id id = null;

            tokenizer.NextToken();
            while (true)
            {
                string element = tokenizer.GetStringValue();
                switch (element.ToUpperInvariant())
                {
                    case "ID":
                        id = ReadId(tokenizer);
                        break;
                    case ",":
                        break;
                    case "]":
                    case ")":
                        tokenizer.CheckCloser(bracket);
                        return new Wkt2Unit(unitKeyword, name, factor) { Id = id };
                    default:
                        SkipUnknownElement(tokenizer);
                        break;
                }
                tokenizer.NextToken();
            }
        }

        private static Wkt2Id ReadId(WktStreamTokenizer tokenizer)
        {
            // ID["EPSG",4326,("version")?,URI[...]*]
            if (!tokenizer.GetStringValue().Equals("ID", StringComparison.OrdinalIgnoreCase))
                tokenizer.ReadToken("ID");

            var bracket = tokenizer.ReadOpener();
            string authority = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();

            string code;
            if (tokenizer.GetTokenType() == TokenType.Number)
                code = ((long)tokenizer.GetNumericValue()).ToString();
            else
                code = tokenizer.ReadDoubleQuotedWord();

            string version = null;
            string uri = null;

            tokenizer.NextToken();
            while (tokenizer.GetStringValue() != "]" && tokenizer.GetStringValue() != ")")
            {
                if (tokenizer.GetStringValue() == ",")
                {
                }
                else if (tokenizer.GetStringValue().Equals("URI", StringComparison.OrdinalIgnoreCase))
                {
                    var u = tokenizer.ReadOpener();
                    string v = tokenizer.ReadDoubleQuotedWord();
                    tokenizer.ReadCloser(u);
                    uri = v;
                }
                else if (tokenizer.GetStringValue() == "\"")
                {
                    version = tokenizer.ReadDoubleQuotedWord();
                }
                else
                {
                    SkipUnknownElement(tokenizer);
                }

                tokenizer.NextToken();
            }

            tokenizer.CheckCloser(bracket);

            var id = new Wkt2Id(authority, code)
            {
                Version = version,
                Uri = uri
            };
            return id;
        }

        private static string ReadRemark(WktStreamTokenizer tokenizer)
        {
            // REMARK["..."]
            var bracket = tokenizer.ReadOpener();
            string remark = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadCloser(bracket);
            return remark;
        }

        private static void SkipUnknownElement(WktStreamTokenizer tokenizer)
        {
            if (tokenizer.GetStringValue() == ",")
                return;

            if (tokenizer.GetStringValue() == "[" || tokenizer.GetStringValue() == "(" || tokenizer.GetStringValue() == "]" || tokenizer.GetStringValue() == ")")
                return;

            var tokenType = tokenizer.GetTokenType();
            string current = tokenizer.GetStringValue();

            if (tokenType == TokenType.Number || current == "\"" || tokenType == TokenType.Word)
            {
                tokenizer.NextToken();
                if (tokenizer.GetStringValue() == "[" || tokenizer.GetStringValue() == "(")
                {
                    var bracket = tokenizer.GetStringValue() == "[" ? WktBracket.Square : WktBracket.Round;
                    int depth = 1;
                    while (depth > 0)
                    {
                        tokenizer.NextToken(false);
                        string sv = tokenizer.GetStringValue();
                        if (sv == "[" || sv == "(") depth++;
                        else if (sv == "]" || sv == ")") depth--;
                    }
                    tokenizer.CheckCloser(bracket);
                }
            }
        }
    }
}
