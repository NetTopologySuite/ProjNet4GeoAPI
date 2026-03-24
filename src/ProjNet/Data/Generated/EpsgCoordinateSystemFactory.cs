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
//
// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

namespace ProjNet.Data.Generated;

using System;
using System.Collections.Generic;
using ProjNet;
using ProjNet.CoordinateSystems;

/// <summary>
/// Represents the documented type.
/// </summary>
internal static class EpsgCoordinateSystemFactory
{
    private static readonly CoordinateSystem[] CoordinateSystemCache = new CoordinateSystem[EpsgGeneratedCatalog.CoordinateReferenceCount];
    private static readonly object CoordinateSystemCacheSync = new object();
    private static readonly Lazy<Dictionary<int, EpsgUnitRecord>> UnitsByCode = new Lazy<Dictionary<int, EpsgUnitRecord>>(BuildUnitsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgEllipsoidRecord>> EllipsoidsByCode = new Lazy<Dictionary<int, EpsgEllipsoidRecord>>(BuildEllipsoidsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgPrimeMeridianRecord>> PrimeMeridiansByCode = new Lazy<Dictionary<int, EpsgPrimeMeridianRecord>>(BuildPrimeMeridiansByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgGeodeticDatumRecord>> GeodeticDatumsByCode = new Lazy<Dictionary<int, EpsgGeodeticDatumRecord>>(BuildGeodeticDatumsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgVerticalDatumRecord>> VerticalDatumsByCode = new Lazy<Dictionary<int, EpsgVerticalDatumRecord>>(BuildVerticalDatumsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgConversionRecord>> ConversionsByCode = new Lazy<Dictionary<int, EpsgConversionRecord>>(BuildConversionsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgAxisRecord[]>> AxesByCoordinateSystemCode = new Lazy<Dictionary<int, EpsgAxisRecord[]>>(BuildAxesByCoordinateSystemCode, true);

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    internal static IEnumerable<KeyValuePair<int, CoordinateSystem>> GetCoordinateSystems()
    {
        for (int cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
        {
            if (!EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int srid))
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
        if (!EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out var reference, out int cacheIndex))
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
                if (EpsgGeneratedCatalog.TryGetGeographicCrs(reference.RecordIndex, out var geographicRecord))
                {
                    return CreateGeographic(geographicRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Geocentric:
                if (EpsgGeneratedCatalog.TryGetGeocentricCrs(reference.RecordIndex, out var geocentricRecord))
                {
                    return CreateGeocentric(geocentricRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Projected:
                if (EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out var projectedRecord))
                {
                    return CreateProjected(projectedRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Vertical:
                if (EpsgGeneratedCatalog.TryGetVerticalCrs(reference.RecordIndex, out var verticalRecord))
                {
                    return CreateVertical(verticalRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Compound:
                if (EpsgGeneratedCatalog.TryGetCompoundCrs(reference.RecordIndex, out var compoundRecord))
                {
                    return CreateCompound(compoundRecord);
                }

                return null;
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
        for (int i = 0; i < conversion.ParameterCount; i++)
        {
            var parameter = EpsgGeneratedCatalog.ConversionParameters[conversion.ParameterStartIndex + i];
            parameters.Add(new ProjectionParameter(NormalizeProjectionParameterName(parameter.Name), parameter.Value));
        }

        string projectionName = NormalizeProjectionMethodName(conversion.MethodName);
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

        string normalized = methodName.ToLowerInvariant();
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "(", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ")", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "-", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "/", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, " ", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ".", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "__", "_");

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

        string normalized = parameterName.ToLowerInvariant();
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "(", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ")", string.Empty);
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "-", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "/", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, " ", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, ".", "_");
        normalized = StringCompatibility.ReplaceOrdinal(normalized, "__", "_");

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
        if (!AxesByCoordinateSystemCode.Value.TryGetValue(coordinateSystemCode, out var orderedAxes))
        {
            return null;
        }

        if (orderedAxes.Length < expectedCount)
        {
            return null;
        }

        int axisCount = includeAll ? orderedAxes.Length : expectedCount;
        var axes = new List<AxisInfo>(axisCount);
        for (int i = 0; i < axisCount; i++)
        {
            var axis = orderedAxes[i];
            axes.Add(new AxisInfo(axis.Name, (AxisOrientationEnum)axis.Orientation));
        }

        return axes;
    }

    private static int GetUnitCode(int coordinateSystemCode, int axisOrder)
    {
        if (!AxesByCoordinateSystemCode.Value.TryGetValue(coordinateSystemCode, out var axes))
        {
            return -1;
        }

        foreach (var axis in axes)
        {
            if (axis.AxisOrder == axisOrder)
            {
                return axis.UnitCode;
            }
        }

        return -1;
    }

    private static bool TryGetUnitRecord(int code, out EpsgUnitRecord record)
    {
        return UnitsByCode.Value.TryGetValue(code, out record);
    }

    private static bool TryGetEllipsoidRecord(int code, out EpsgEllipsoidRecord record)
    {
        return EllipsoidsByCode.Value.TryGetValue(code, out record);
    }

    private static bool TryGetPrimeMeridianRecord(int code, out EpsgPrimeMeridianRecord record)
    {
        return PrimeMeridiansByCode.Value.TryGetValue(code, out record);
    }

    private static bool TryGetGeodeticDatumRecord(int code, out EpsgGeodeticDatumRecord record)
    {
        return GeodeticDatumsByCode.Value.TryGetValue(code, out record);
    }

    private static bool TryGetVerticalDatumRecord(int code, out EpsgVerticalDatumRecord record)
    {
        return VerticalDatumsByCode.Value.TryGetValue(code, out record);
    }

    private static bool TryGetConversionRecord(int code, out EpsgConversionRecord record)
    {
        return ConversionsByCode.Value.TryGetValue(code, out record);
    }

    private static Dictionary<int, EpsgUnitRecord> BuildUnitsByCode()
    {
        var dictionary = new Dictionary<int, EpsgUnitRecord>(EpsgGeneratedCatalog.Units.Length);
        foreach (var item in EpsgGeneratedCatalog.Units)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgEllipsoidRecord> BuildEllipsoidsByCode()
    {
        var dictionary = new Dictionary<int, EpsgEllipsoidRecord>(EpsgGeneratedCatalog.Ellipsoids.Length);
        foreach (var item in EpsgGeneratedCatalog.Ellipsoids)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgPrimeMeridianRecord> BuildPrimeMeridiansByCode()
    {
        var dictionary = new Dictionary<int, EpsgPrimeMeridianRecord>(EpsgGeneratedCatalog.PrimeMeridians.Length);
        foreach (var item in EpsgGeneratedCatalog.PrimeMeridians)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgGeodeticDatumRecord> BuildGeodeticDatumsByCode()
    {
        var dictionary = new Dictionary<int, EpsgGeodeticDatumRecord>(EpsgGeneratedCatalog.GeodeticDatums.Length);
        foreach (var item in EpsgGeneratedCatalog.GeodeticDatums)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgVerticalDatumRecord> BuildVerticalDatumsByCode()
    {
        var dictionary = new Dictionary<int, EpsgVerticalDatumRecord>(EpsgGeneratedCatalog.VerticalDatums.Length);
        foreach (var item in EpsgGeneratedCatalog.VerticalDatums)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgConversionRecord> BuildConversionsByCode()
    {
        var dictionary = new Dictionary<int, EpsgConversionRecord>(EpsgGeneratedCatalog.Conversions.Length);
        foreach (var item in EpsgGeneratedCatalog.Conversions)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgAxisRecord[]> BuildAxesByCoordinateSystemCode()
    {
        var grouped = new Dictionary<int, List<EpsgAxisRecord>>();
        foreach (var axis in EpsgGeneratedCatalog.Axes)
        {
            if (!grouped.TryGetValue(axis.CoordinateSystemCode, out var axes))
            {
                axes = new List<EpsgAxisRecord>();
                grouped[axis.CoordinateSystemCode] = axes;
            }

            axes.Add(axis);
        }

        var result = new Dictionary<int, EpsgAxisRecord[]>(grouped.Count);
        foreach (var item in grouped)
        {
            item.Value.Sort((left, right) => left.AxisOrder.CompareTo(right.AxisOrder));
            result[item.Key] = item.Value.ToArray();
        }

        return result;
    }
}
