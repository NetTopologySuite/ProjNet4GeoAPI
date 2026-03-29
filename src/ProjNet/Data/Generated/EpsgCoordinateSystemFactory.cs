// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Data.Generated;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ProjNet.CoordinateSystems;
using ProjNet.Data;

/// <summary>
/// Represents the documented type.
/// </summary>
internal static class EpsgCoordinateSystemFactory
{
    private static readonly CoordinateSystem[] CoordinateSystemCache = new CoordinateSystem[EpsgGeneratedCatalog.CoordinateReferenceCount];
    private static readonly object CoordinateSystemCacheSync = new();
    private static readonly Lazy<Dictionary<int, EpsgUnitRecord>> UnitsByCode = new(BuildUnitsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgEllipsoidRecord>> EllipsoidsByCode = new(BuildEllipsoidsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgPrimeMeridianRecord>> PrimeMeridiansByCode = new(BuildPrimeMeridiansByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgGeodeticDatumRecord>> GeodeticDatumsByCode = new(BuildGeodeticDatumsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgVerticalDatumRecord>> VerticalDatumsByCode = new(BuildVerticalDatumsByCode, true);
    private static readonly Lazy<Dictionary<int, EpsgAxisRecord[]>> AxesByCoordinateSystemCode = new(BuildAxesByCoordinateSystemCode, true);

    /// <summary>
    /// Performs the documented operation.
    /// </summary>
    /// <returns>The computed value.</returns>
    internal static IEnumerable<CoordinateSystemEntry> GetCoordinateSystems()
    {
        for (int cacheIndex = 0; cacheIndex < EpsgGeneratedCatalog.CoordinateReferenceCount; cacheIndex++)
        {
            if (!EpsgGeneratedCatalog.TryGetCoordinateSridByCacheIndex(cacheIndex, out int srid))
            {
                continue;
            }

            CoordinateSystem? coordinateSystem = TryCreateCoordinateSystem(srid);
            if (coordinateSystem is null)
            {
                continue;
            }

            yield return new CoordinateSystemEntry(srid, coordinateSystem);
        }
    }

    private static CoordinateSystem? TryCreateCoordinateSystem(int srid)
    {
        if (!EpsgGeneratedCatalog.TryGetCoordinateReference(srid, out EpsgCoordinateReferenceRecord reference, out int cacheIndex))
        {
            return null;
        }

        CoordinateSystem? cached = CoordinateSystemCache[cacheIndex];
        if (cached is not null)
        {
            return cached;
        }

        CoordinateSystem? created = CreateCoordinateSystem(reference);
        if (created is null)
        {
            return null;
        }

        lock (CoordinateSystemCacheSync)
        {
            if (CoordinateSystemCache[cacheIndex] is null)
            {
                CoordinateSystemCache[cacheIndex] = created;
            }

            return CoordinateSystemCache[cacheIndex];
        }
    }

    private static CoordinateSystem? CreateCoordinateSystem(EpsgCoordinateReferenceRecord reference)
    {
        switch (reference.Kind)
        {
            case EpsgCoordinateSystemKind.Geographic2D:
                if (EpsgGeneratedCatalog.TryGetGeographicCrs(reference.RecordIndex, out EpsgGeographicCrsRecord geographicRecord))
                {
                    return CreateGeographic(geographicRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Geocentric:
                if (EpsgGeneratedCatalog.TryGetGeocentricCrs(reference.RecordIndex, out EpsgGeocentricCrsRecord geocentricRecord))
                {
                    return CreateGeocentric(geocentricRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Projected:
                if (EpsgGeneratedCatalog.TryGetProjectedCrs(reference.RecordIndex, out EpsgProjectedCrsRecord projectedRecord))
                {
                    return CreateProjected(projectedRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Vertical:
                if (EpsgGeneratedCatalog.TryGetVerticalCrs(reference.RecordIndex, out EpsgVerticalCrsRecord verticalRecord))
                {
                    return CreateVertical(verticalRecord);
                }

                return null;
            case EpsgCoordinateSystemKind.Compound:
                if (EpsgGeneratedCatalog.TryGetCompoundCrs(reference.RecordIndex, out EpsgCompoundCrsRecord compoundRecord))
                {
                    return CreateCompound(compoundRecord);
                }

                return null;
            default:
                return null;
        }
    }

    private static GeographicCoordinateSystem? CreateGeographic(EpsgGeographicCrsRecord record)
    {
        if (!TryCreateHorizontalDatum(record.DatumCode, out HorizontalDatum? datum))
        {
            return null;
        }

        if (!TryGetGeodeticDatumRecord(record.DatumCode, out EpsgGeodeticDatumRecord geodeticDatumRecord))
        {
            return null;
        }

        if (!TryCreatePrimeMeridian(geodeticDatumRecord.PrimeMeridianCode, out PrimeMeridian? primeMeridian))
        {
            return null;
        }

        List<AxisInfo>? axes = GetAxes(record.CoordinateSystemCode, 2);
        if (axes is null || axes.Count < 2)
        {
            return null;
        }

        if (!TryCreateAngularUnit(GetUnitCode(record.CoordinateSystemCode, 1), out AngularUnit? angularUnit))
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

    private static GeocentricCoordinateSystem? CreateGeocentric(EpsgGeocentricCrsRecord record)
    {
        if (!TryCreateHorizontalDatum(record.DatumCode, out HorizontalDatum? datum))
        {
            return null;
        }

        if (!TryGetGeodeticDatumRecord(record.DatumCode, out EpsgGeodeticDatumRecord geodeticDatumRecord))
        {
            return null;
        }

        if (!TryCreatePrimeMeridian(geodeticDatumRecord.PrimeMeridianCode, out PrimeMeridian? primeMeridian))
        {
            return null;
        }

        List<AxisInfo>? axes = GetAxes(record.CoordinateSystemCode, 3);
        if (axes is null || axes.Count < 3)
        {
            return null;
        }

        if (!TryCreateLinearUnit(GetUnitCode(record.CoordinateSystemCode, 1), out LinearUnit? linearUnit))
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

    private static ProjectedCoordinateSystem? CreateProjected(EpsgProjectedCrsRecord record)
    {
        var baseCoordinateSystem = TryCreateCoordinateSystem(record.BaseSrid) as GeographicCoordinateSystem;
        if (baseCoordinateSystem is null)
        {
            return null;
        }

        if (!TryCreateLinearUnit(GetUnitCode(record.CoordinateSystemCode, 1), out LinearUnit? linearUnit))
        {
            return null;
        }

        if (!EpsgGeneratedCatalog.TryGetConversion(record.ConversionCode, out EpsgConversionRecord conversion))
        {
            return null;
        }

        var parameters = new List<ProjectionParameter>(conversion.ParameterCount);
        for (int i = 0; i < conversion.ParameterCount; i++)
        {
            if (!EpsgGeneratedCatalog.TryGetConversionParameter(record.ConversionCode, i, out EpsgConversionParameterRecord parameter))
            {
                return null;
            }

            parameters.Add(new ProjectionParameter(NormalizeProjectionParameterName(parameter.Name), parameter.Value));
        }

        string projectionName = NormalizeProjectionMethodName(conversion.MethodName);
        var projection = new Projection(projectionName, parameters, projectionName, "EPSG", record.ConversionCode, string.Empty, string.Empty, string.Empty);

        List<AxisInfo>? axes = GetAxes(record.CoordinateSystemCode, 2);
        if (axes is null || axes.Count < 2)
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

    private static VerticalCoordinateSystem? CreateVertical(EpsgVerticalCrsRecord record)
    {
        if (!TryGetVerticalDatumRecord(record.DatumCode, out EpsgVerticalDatumRecord datumRecord))
        {
            return null;
        }

        List<AxisInfo>? axisInfo = GetAxes(record.CoordinateSystemCode, 1);
        if (axisInfo is null || axisInfo.Count == 0)
        {
            return null;
        }

        if (!TryCreateLinearUnit(GetUnitCode(record.CoordinateSystemCode, 1), out LinearUnit? linearUnit))
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

    private static CompoundCoordinateSystem? CreateCompound(EpsgCompoundCrsRecord record)
    {
        CoordinateSystem? horizontal = TryCreateCoordinateSystem(record.HorizontalSrid);
        CoordinateSystem? vertical = TryCreateCoordinateSystem(record.VerticalSrid);
        if (horizontal is null || vertical is null)
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

    private static bool TryCreateHorizontalDatum(int datumCode, [NotNullWhen(true)] out HorizontalDatum? datum)
    {
        datum = null;
        if (!TryGetGeodeticDatumRecord(datumCode, out EpsgGeodeticDatumRecord datumRecord))
        {
            return false;
        }

        if (!TryCreateEllipsoid(datumRecord.EllipsoidCode, out Ellipsoid? ellipsoid))
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

    private static bool TryCreateEllipsoid(int ellipsoidCode, [NotNullWhen(true)] out Ellipsoid? ellipsoid)
    {
        ellipsoid = null;
        if (!TryGetEllipsoidRecord(ellipsoidCode, out EpsgEllipsoidRecord record))
        {
            return false;
        }

        if (!TryCreateLinearUnit(record.UnitCode, out LinearUnit? linearUnit))
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

    private static bool TryCreatePrimeMeridian(int primeMeridianCode, [NotNullWhen(true)] out PrimeMeridian? primeMeridian)
    {
        primeMeridian = null;
        if (!TryGetPrimeMeridianRecord(primeMeridianCode, out EpsgPrimeMeridianRecord record))
        {
            return false;
        }

        if (!TryCreateAngularUnit(record.UnitCode, out AngularUnit? angularUnit))
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

    private static bool TryCreateLinearUnit(int unitCode, [NotNullWhen(true)] out LinearUnit? unit)
    {
        unit = null;
        if (!TryGetUnitRecord(unitCode, out EpsgUnitRecord record) || record.UnitType != 0)
        {
            return false;
        }

        unit = new LinearUnit(record.Factor, record.Name, "EPSG", record.Code, string.Empty, string.Empty, string.Empty);
        return true;
    }

    private static bool TryCreateAngularUnit(int unitCode, [NotNullWhen(true)] out AngularUnit? unit)
    {
        unit = null;
        if (!TryGetUnitRecord(unitCode, out EpsgUnitRecord record) || record.UnitType != 1)
        {
            return false;
        }

        unit = new AngularUnit(record.Factor, record.Name, "EPSG", record.Code, string.Empty, string.Empty, string.Empty);
        return true;
    }

    private static List<AxisInfo>? GetAxes(int coordinateSystemCode, int expectedCount, bool includeAll = false)
    {
        if (!AxesByCoordinateSystemCode.Value.TryGetValue(coordinateSystemCode, out EpsgAxisRecord[]? orderedAxes))
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
            EpsgAxisRecord axis = orderedAxes[i];
            axes.Add(new AxisInfo(axis.Name, (AxisOrientationEnum)axis.Orientation));
        }

        return axes;
    }

    private static int GetUnitCode(int coordinateSystemCode, int axisOrder)
    {
        if (!AxesByCoordinateSystemCode.Value.TryGetValue(coordinateSystemCode, out EpsgAxisRecord[]? axes))
        {
            return -1;
        }

        foreach (EpsgAxisRecord axis in axes)
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

    private static Dictionary<int, EpsgUnitRecord> BuildUnitsByCode()
    {
        var dictionary = new Dictionary<int, EpsgUnitRecord>(EpsgGeneratedCatalog.Units.Length);
        foreach (EpsgUnitRecord item in EpsgGeneratedCatalog.Units)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgEllipsoidRecord> BuildEllipsoidsByCode()
    {
        var dictionary = new Dictionary<int, EpsgEllipsoidRecord>(EpsgGeneratedCatalog.Ellipsoids.Length);
        foreach (EpsgEllipsoidRecord item in EpsgGeneratedCatalog.Ellipsoids)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgPrimeMeridianRecord> BuildPrimeMeridiansByCode()
    {
        var dictionary = new Dictionary<int, EpsgPrimeMeridianRecord>(EpsgGeneratedCatalog.PrimeMeridians.Length);
        foreach (EpsgPrimeMeridianRecord item in EpsgGeneratedCatalog.PrimeMeridians)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgGeodeticDatumRecord> BuildGeodeticDatumsByCode()
    {
        var dictionary = new Dictionary<int, EpsgGeodeticDatumRecord>(EpsgGeneratedCatalog.GeodeticDatums.Length);
        foreach (EpsgGeodeticDatumRecord item in EpsgGeneratedCatalog.GeodeticDatums)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgVerticalDatumRecord> BuildVerticalDatumsByCode()
    {
        var dictionary = new Dictionary<int, EpsgVerticalDatumRecord>(EpsgGeneratedCatalog.VerticalDatums.Length);
        foreach (EpsgVerticalDatumRecord item in EpsgGeneratedCatalog.VerticalDatums)
        {
            dictionary[item.Code] = item;
        }

        return dictionary;
    }

    private static Dictionary<int, EpsgAxisRecord[]> BuildAxesByCoordinateSystemCode()
    {
        var grouped = new Dictionary<int, List<EpsgAxisRecord>>();
        foreach (EpsgAxisRecord axis in EpsgGeneratedCatalog.Axes)
        {
            if (!grouped.TryGetValue(axis.CoordinateSystemCode, out List<EpsgAxisRecord>? axes))
            {
                axes = [];
                grouped[axis.CoordinateSystemCode] = axes;
            }

            axes.Add(axis);
        }

        var result = new Dictionary<int, EpsgAxisRecord[]>(grouped.Count);
        foreach (KeyValuePair<int, List<EpsgAxisRecord>> item in grouped)
        {
            item.Value.Sort((left, right) => left.AxisOrder.CompareTo(right.AxisOrder));
            result[item.Key] = item.Value.ToArray();
        }

        return result;
    }
}
