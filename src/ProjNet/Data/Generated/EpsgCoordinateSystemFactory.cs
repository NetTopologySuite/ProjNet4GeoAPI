namespace ProjNet.Data.Generated
{
    using System;
    using System.Collections.Generic;
    using ProjNet.CoordinateSystems;

    internal static class EpsgCoordinateSystemFactory
    {
        private static readonly CoordinateSystem[] CoordinateSystemCache = new CoordinateSystem[EpsgGeneratedCatalog.CoordinateReferenceCount];
        private static readonly object CoordinateSystemCacheSync = new object();

        internal static IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems()
        {
            for (var cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
            {
                if (!EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out var srid))
                {
                    continue;
                }

                var coordinateSystem = TryCreateCoordinateSystem(srid);
                if (coordinateSystem == null)
                {
                    continue;
                }

                yield return new KeyValuePair<int, CoordinateSystem>(srid, coordinateSystem);
            }
        }

        private static CoordinateSystem TryCreateCoordinateSystem(int srid)
        {
            if (!EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out var reference, out var cacheIndex))
            {
                return null;
            }

            var cached = CoordinateSystemCache[cacheIndex];
            if (cached != null)
            {
                return cached;
            }

            var created = CreateCoordinateSystem(reference);
            if (created == null)
            {
                return null;
            }

            lock (CoordinateSystemCacheSync)
            {
                if (CoordinateSystemCache[cacheIndex] == null)
                {
                    CoordinateSystemCache[cacheIndex] = created;
                }

                return CoordinateSystemCache[cacheIndex];
            }
        }

        private static CoordinateSystem CreateCoordinateSystem(EpsgCoordinateReferenceRecord reference)
        {
            switch (reference.Kind)
            {
                case EpsgCoordinateSystemKind.Geographic2D:
                    return CreateGeographic(EpsgGeneratedCatalog.GeographicCrs[reference.RecordIndex]);
                case EpsgCoordinateSystemKind.Geocentric:
                    return CreateGeocentric(EpsgGeneratedCatalog.GeocentricCrs[reference.RecordIndex]);
                case EpsgCoordinateSystemKind.Projected:
                    return CreateProjected(EpsgGeneratedCatalog.ProjectedCrs[reference.RecordIndex]);
                case EpsgCoordinateSystemKind.Vertical:
                    return CreateVertical(EpsgGeneratedCatalog.VerticalCrs[reference.RecordIndex]);
                case EpsgCoordinateSystemKind.Compound:
                    return CreateCompound(EpsgGeneratedCatalog.CompoundCrs[reference.RecordIndex]);
                default:
                    return null;
            }
        }

        private static GeographicCoordinateSystem CreateGeographic(EpsgGeographicCrsRecord record)
        {
            if (!TryCreateHorizontalDatum(record.DatumCode, out var datum))
            {
                return null;
            }

            if (!TryGetGeodeticDatumRecord(record.DatumCode, out var geodeticDatumRecord))
            {
                return null;
            }

            if (!TryCreatePrimeMeridian(geodeticDatumRecord.PrimeMeridianCode, out var primeMeridian))
            {
                return null;
            }

            var axes = GetAxes(record.CoordinateSystemCode, 2);
            if (axes == null || axes.Count < 2)
            {
                return null;
            }

            if (!TryCreateAngularUnit(GetUnitCode(record.CoordinateSystemCode, 1), out var angularUnit))
            {
                return null;
            }

            return new GeographicCoordinateSystem(
                angularUnit,
                datum,
                primeMeridian,
                axes,
                record.Name,
                "EPSG",
                record.Srid,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        private static GeocentricCoordinateSystem CreateGeocentric(EpsgGeocentricCrsRecord record)
        {
            if (!TryCreateHorizontalDatum(record.DatumCode, out var datum))
            {
                return null;
            }

            if (!TryGetGeodeticDatumRecord(record.DatumCode, out var geodeticDatumRecord))
            {
                return null;
            }

            if (!TryCreatePrimeMeridian(geodeticDatumRecord.PrimeMeridianCode, out var primeMeridian))
            {
                return null;
            }

            var axes = GetAxes(record.CoordinateSystemCode, 3);
            if (axes == null || axes.Count < 3)
            {
                return null;
            }

            if (!TryCreateLinearUnit(GetUnitCode(record.CoordinateSystemCode, 1), out var linearUnit))
            {
                return null;
            }

            return new GeocentricCoordinateSystem(
                datum,
                linearUnit,
                primeMeridian,
                axes,
                record.Name,
                "EPSG",
                record.Srid,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        private static ProjectedCoordinateSystem CreateProjected(EpsgProjectedCrsRecord record)
        {
            var baseCoordinateSystem = TryCreateCoordinateSystem(record.BaseSrid) as GeographicCoordinateSystem;
            if (baseCoordinateSystem == null)
            {
                return null;
            }

            if (!TryCreateLinearUnit(GetUnitCode(record.CoordinateSystemCode, 1), out var linearUnit))
            {
                return null;
            }

            if (!TryGetConversionRecord(record.ConversionCode, out var conversion))
            {
                return null;
            }

            var parameters = new List<ProjectionParameter>();
            for (var i = 0; i < conversion.ParameterCount; i++)
            {
                var parameter = EpsgGeneratedCatalog.ConversionParameters[conversion.ParameterStartIndex + i];
                parameters.Add(new ProjectionParameter(NormalizeProjectionParameterName(parameter.Name), parameter.Value));
            }

            var projectionName = NormalizeProjectionMethodName(conversion.MethodName);
            var projection = new Projection(projectionName, parameters, projectionName, "EPSG", record.ConversionCode, string.Empty, string.Empty, string.Empty);

            var axes = GetAxes(record.CoordinateSystemCode, 2);
            if (axes == null || axes.Count < 2)
            {
                return null;
            }

            return new ProjectedCoordinateSystem(
                baseCoordinateSystem.HorizontalDatum,
                baseCoordinateSystem,
                linearUnit,
                projection,
                axes,
                record.Name,
                "EPSG",
                record.Srid,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        private static VerticalCoordinateSystem CreateVertical(EpsgVerticalCrsRecord record)
        {
            if (!TryGetVerticalDatumRecord(record.DatumCode, out var datumRecord))
            {
                return null;
            }

            var axisInfo = GetAxes(record.CoordinateSystemCode, 1);
            if (axisInfo == null || axisInfo.Count == 0)
            {
                return null;
            }

            if (!TryCreateLinearUnit(GetUnitCode(record.CoordinateSystemCode, 1), out var linearUnit))
            {
                return null;
            }

            var datum = new VerticalDatum(DatumType.VD_GeoidModelDerived, datumRecord.Name, "EPSG", datumRecord.Code, string.Empty, string.Empty, string.Empty);

            return new VerticalCoordinateSystem(
                linearUnit,
                datum,
                axisInfo[0],
                record.Name,
                "EPSG",
                record.Srid,
                string.Empty,
                string.Empty,
                string.Empty);
        }


        private static string NormalizeProjectionMethodName(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return methodName;
            }

            var normalized = methodName
                .ToLowerInvariant()
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace("-", "_")
                .Replace("/", "_")
                .Replace(" ", "_")
                .Replace(".", "_")
                .Replace("__", "_");

            switch (normalized)
            {
                case "polar_stereographic_variant_a":
                case "polar_stereographic_variant_b":
                    return "Polar Stereographic";
                default:
                    return methodName;
            }
        }

        private static string NormalizeProjectionParameterName(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return parameterName;
            }

            var normalized = parameterName
                .ToLowerInvariant()
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace("-", "_")
                .Replace("/", "_")
                .Replace(" ", "_")
                .Replace(".", "_")
                .Replace("__", "_");

            switch (normalized)
            {
                case "longitude_of_natural_origin":
                case "longitude_of_false_origin":
                case "longitude_of_projection_centre":
                    return "central_meridian";
                case "latitude_of_natural_origin":
                case "latitude_of_false_origin":
                case "latitude_of_projection_centre":
                    return "latitude_of_origin";
                case "scale_factor_at_natural_origin":
                case "scale_factor_at_projection_centre":
                case "scale_factor_on_initial_line":
                    return "scale_factor";
                case "easting_at_false_origin":
                case "easting_at_projection_centre":
                    return "false_easting";
                case "northing_at_false_origin":
                case "northing_at_projection_centre":
                    return "false_northing";
                case "latitude_of_1st_standard_parallel":
                    return "standard_parallel_1";
                case "latitude_of_2nd_standard_parallel":
                    return "standard_parallel_2";
                default:
                    return normalized;
            }
        }

        private static CompoundCoordinateSystem CreateCompound(EpsgCompoundCrsRecord record)
        {
            var horizontal = TryCreateCoordinateSystem(record.HorizontalSrid);
            var vertical = TryCreateCoordinateSystem(record.VerticalSrid);
            if (horizontal == null || vertical == null)
            {
                return null;
            }

            return new CompoundCoordinateSystem(
                horizontal,
                vertical,
                record.Name,
                "EPSG",
                record.Srid,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        private static bool TryCreateHorizontalDatum(int datumCode, out HorizontalDatum datum)
        {
            datum = null;
            if (!TryGetGeodeticDatumRecord(datumCode, out var datumRecord))
            {
                return false;
            }

            if (!TryCreateEllipsoid(datumRecord.EllipsoidCode, out var ellipsoid))
            {
                return false;
            }

            datum = new HorizontalDatum(
                ellipsoid,
                null,
                DatumType.HD_Geocentric,
                datumRecord.Name,
                "EPSG",
                datumRecord.Code,
                string.Empty,
                string.Empty,
                string.Empty);
            return true;
        }

        private static bool TryCreateEllipsoid(int ellipsoidCode, out Ellipsoid ellipsoid)
        {
            ellipsoid = null;
            if (!TryGetEllipsoidRecord(ellipsoidCode, out var record))
            {
                return false;
            }

            if (!TryCreateLinearUnit(record.UnitCode, out var linearUnit))
            {
                return false;
            }

            ellipsoid = new Ellipsoid(
                record.SemiMajor,
                record.SemiMinor,
                record.InverseFlattening,
                record.IsInverseFlatteningDefinitive,
                linearUnit,
                record.Name,
                "EPSG",
                record.Code,
                string.Empty,
                string.Empty,
                string.Empty);

            return true;
        }

        private static bool TryCreatePrimeMeridian(int primeMeridianCode, out PrimeMeridian primeMeridian)
        {
            primeMeridian = null;
            if (!TryGetPrimeMeridianRecord(primeMeridianCode, out var record))
            {
                return false;
            }

            if (!TryCreateAngularUnit(record.UnitCode, out var angularUnit))
            {
                return false;
            }

            primeMeridian = new PrimeMeridian(
                record.Longitude,
                angularUnit,
                record.Name,
                "EPSG",
                record.Code,
                string.Empty,
                string.Empty,
                string.Empty);
            return true;
        }

        private static bool TryCreateLinearUnit(int unitCode, out LinearUnit unit)
        {
            unit = null;
            if (!TryGetUnitRecord(unitCode, out var record) || record.UnitType != 0)
            {
                return false;
            }

            unit = new LinearUnit(record.Factor, record.Name, "EPSG", record.Code, string.Empty, string.Empty, string.Empty);
            return true;
        }

        private static bool TryCreateAngularUnit(int unitCode, out AngularUnit unit)
        {
            unit = null;
            if (!TryGetUnitRecord(unitCode, out var record) || record.UnitType != 1)
            {
                return false;
            }

            unit = new AngularUnit(record.Factor, record.Name, "EPSG", record.Code, string.Empty, string.Empty, string.Empty);
            return true;
        }

        private static List<AxisInfo> GetAxes(int coordinateSystemCode, int expectedCount, bool includeAll = false)
        {
            var orderedAxes = new List<EpsgAxisRecord>();
            foreach (var axis in EpsgGeneratedCatalog.Axes)
            {
                if (axis.CoordinateSystemCode == coordinateSystemCode)
                {
                    orderedAxes.Add(axis);
                }
            }

            orderedAxes.Sort((left, right) => left.AxisOrder.CompareTo(right.AxisOrder));
            if (orderedAxes.Count < expectedCount)
            {
                return null;
            }

            var axisCount = includeAll ? orderedAxes.Count : expectedCount;
            var axes = new List<AxisInfo>(axisCount);
            for (var i = 0; i < axisCount; i++)
            {
                var axis = orderedAxes[i];
                axes.Add(new AxisInfo(axis.Name, (AxisOrientationEnum)axis.Orientation));
            }

            return axes;
        }

        private static int GetUnitCode(int coordinateSystemCode, int axisOrder)
        {
            foreach (var axis in EpsgGeneratedCatalog.Axes)
            {
                if (axis.CoordinateSystemCode == coordinateSystemCode && axis.AxisOrder == axisOrder)
                {
                    return axis.UnitCode;
                }
            }

            return -1;
        }

        private static bool TryGetUnitRecord(int code, out EpsgUnitRecord record)
        {
            foreach (var item in EpsgGeneratedCatalog.Units)
            {
                if (item.Code == code)
                {
                    record = item;
                    return true;
                }
            }

            record = default;
            return false;
        }

        private static bool TryGetEllipsoidRecord(int code, out EpsgEllipsoidRecord record)
        {
            foreach (var item in EpsgGeneratedCatalog.Ellipsoids)
            {
                if (item.Code == code)
                {
                    record = item;
                    return true;
                }
            }

            record = default;
            return false;
        }

        private static bool TryGetPrimeMeridianRecord(int code, out EpsgPrimeMeridianRecord record)
        {
            foreach (var item in EpsgGeneratedCatalog.PrimeMeridians)
            {
                if (item.Code == code)
                {
                    record = item;
                    return true;
                }
            }

            record = default;
            return false;
        }

        private static bool TryGetGeodeticDatumRecord(int code, out EpsgGeodeticDatumRecord record)
        {
            foreach (var item in EpsgGeneratedCatalog.GeodeticDatums)
            {
                if (item.Code == code)
                {
                    record = item;
                    return true;
                }
            }

            record = default;
            return false;
        }

        private static bool TryGetVerticalDatumRecord(int code, out EpsgVerticalDatumRecord record)
        {
            foreach (var item in EpsgGeneratedCatalog.VerticalDatums)
            {
                if (item.Code == code)
                {
                    record = item;
                    return true;
                }
            }

            record = default;
            return false;
        }

        private static bool TryGetConversionRecord(int code, out EpsgConversionRecord record)
        {
            foreach (var item in EpsgGeneratedCatalog.Conversions)
            {
                if (item.Code == code)
                {
                    record = item;
                    return true;
                }
            }

            record = default;
            return false;
        }
    }
}


