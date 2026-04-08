// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ProjNet;
using ProjNet.CoordinateSystems;

/// <summary>
/// Writes coordinate systems as PROJJSON.
/// </summary>
public static class ProjJsonWriter
{
    /// <summary>
    /// Writes a coordinate system to an existing <see cref="Utf8JsonWriter"/>.
    /// </summary>
    /// <param name="writer">The JSON writer to write to.</param>
    /// <param name="coordinateSystem">The coordinate system to serialize.</param>
    public static void WriteTo(Utf8JsonWriter writer, CoordinateSystem coordinateSystem)
    {
        ArgumentGuard.ThrowIfNull(writer, nameof(writer));
        ArgumentGuard.ThrowIfNull(coordinateSystem, nameof(coordinateSystem));

        WriteCoordinateSystem(writer, coordinateSystem);
    }

    /// <summary>
    /// Serializes a coordinate system to PROJJSON text.
    /// </summary>
    /// <param name="coordinateSystem">The coordinate system to serialize.</param>
    /// <returns>The serialized PROJJSON text.</returns>
    public static string ToJson(CoordinateSystem coordinateSystem)
    {
        ArgumentGuard.ThrowIfNull(coordinateSystem, nameof(coordinateSystem));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCoordinateSystem(writer, coordinateSystem);
            writer.Flush();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteCoordinateSystem(Utf8JsonWriter writer, CoordinateSystem coordinateSystem)
    {
        switch (coordinateSystem)
        {
            case GeographicCoordinateSystem geographicCoordinateSystem:
                WriteGeographicCoordinateSystem(writer, geographicCoordinateSystem);
                break;
            case GeocentricCoordinateSystem geocentricCoordinateSystem:
                WriteGeocentricCoordinateSystem(writer, geocentricCoordinateSystem);
                break;
            case ProjectedCoordinateSystem projectedCoordinateSystem:
                WriteProjectedCoordinateSystem(writer, projectedCoordinateSystem);
                break;
            case VerticalCoordinateSystem verticalCoordinateSystem:
                WriteVerticalCoordinateSystem(writer, verticalCoordinateSystem);
                break;
            case CompoundCoordinateSystem compoundCoordinateSystem:
                WriteCompoundCoordinateSystem(writer, compoundCoordinateSystem);
                break;
            default:
                throw new NotSupportedException($"PROJJSON writing is not supported for coordinate system type '{coordinateSystem.GetType().Name}'.");
        }
    }

    private static void WriteGeographicCoordinateSystem(Utf8JsonWriter writer, GeographicCoordinateSystem coordinateSystem)
    {
        ThrowIfBoundHorizontalDatumRequiresBoundCrs(coordinateSystem.HorizontalDatum, nameof(GeographicCoordinateSystem));

        writer.WriteStartObject();
        writer.WriteString("type", "GeographicCRS");
        writer.WriteString("name", coordinateSystem.Name);

        writer.WritePropertyName("datum");
        WriteHorizontalDatum(writer, coordinateSystem.HorizontalDatum);

        writer.WritePropertyName("prime_meridian");
        WritePrimeMeridian(writer, coordinateSystem.PrimeMeridian);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "ellipsoidal", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteGeocentricCoordinateSystem(Utf8JsonWriter writer, GeocentricCoordinateSystem coordinateSystem)
    {
        ThrowIfBoundHorizontalDatumRequiresBoundCrs(coordinateSystem.HorizontalDatum, nameof(GeocentricCoordinateSystem));

        writer.WriteStartObject();
        writer.WriteString("type", "GeodeticCRS");
        writer.WriteString("name", coordinateSystem.Name);

        writer.WritePropertyName("datum");
        WriteHorizontalDatum(writer, coordinateSystem.HorizontalDatum);

        writer.WritePropertyName("prime_meridian");
        WritePrimeMeridian(writer, coordinateSystem.PrimeMeridian);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "Cartesian", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteProjectedCoordinateSystem(Utf8JsonWriter writer, ProjectedCoordinateSystem coordinateSystem)
    {
        ThrowIfBoundHorizontalDatumRequiresBoundCrs(coordinateSystem.GeographicCoordinateSystem.HorizontalDatum, nameof(ProjectedCoordinateSystem));

        writer.WriteStartObject();
        writer.WriteString("type", "ProjectedCRS");
        writer.WriteString("name", coordinateSystem.Name);

        writer.WritePropertyName("base_crs");
        WriteGeographicCoordinateSystem(writer, coordinateSystem.GeographicCoordinateSystem);

        writer.WritePropertyName("conversion");
        WriteConversion(
            writer,
            coordinateSystem.Projection,
            coordinateSystem.GeographicCoordinateSystem.AngularUnit,
            coordinateSystem.LinearUnit);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "Cartesian", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteVerticalCoordinateSystem(Utf8JsonWriter writer, VerticalCoordinateSystem coordinateSystem)
    {
        ThrowIfBoundVerticalMetadataRequiresBoundCrs(coordinateSystem);

        writer.WriteStartObject();
        writer.WriteString("type", "VerticalCRS");
        writer.WriteString("name", coordinateSystem.Name);

        writer.WritePropertyName("datum");
        WriteVerticalDatum(writer, coordinateSystem.VerticalDatum);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "vertical", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteCompoundCoordinateSystem(Utf8JsonWriter writer, CompoundCoordinateSystem coordinateSystem)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "CompoundCRS");
        writer.WriteString("name", coordinateSystem.Name);

        writer.WritePropertyName("components");
        writer.WriteStartArray();
        WriteCompoundComponents(writer, coordinateSystem);
        writer.WriteEndArray();

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteCoordinateSystemDefinition(Utf8JsonWriter writer, string subtype, CoordinateSystem coordinateSystem)
    {
        writer.WriteStartObject();
        writer.WriteString("subtype", subtype);
        writer.WritePropertyName("axis");
        writer.WriteStartArray();
        for (int i = 0; i < coordinateSystem.Dimension; i++)
        {
            WriteAxis(writer, coordinateSystem, i, coordinateSystem.GetAxis(i), coordinateSystem.GetUnits(i));
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteAxis(Utf8JsonWriter writer, CoordinateSystem coordinateSystem, int axisIndex, AxisInfo axisInfo, IUnit unit)
    {
        writer.WriteStartObject();
        writer.WriteString("name", axisInfo.Name);
        writer.WriteString("direction", GetAxisDirection(coordinateSystem, axisIndex, axisInfo.Orientation));
        writer.WritePropertyName("unit");
        WriteUnit(writer, unit);
        writer.WriteEndObject();
    }

    private static void WriteHorizontalDatum(Utf8JsonWriter writer, HorizontalDatum horizontalDatum)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "GeodeticReferenceFrame");
        writer.WriteString("name", horizontalDatum.Name);
        writer.WritePropertyName("ellipsoid");
        WriteEllipsoid(writer, horizontalDatum.Ellipsoid);
        WriteIdentifier(writer, horizontalDatum);
        writer.WriteEndObject();
    }

    private static void WriteEllipsoid(Utf8JsonWriter writer, Ellipsoid ellipsoid)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "Ellipsoid");
        writer.WriteString("name", ellipsoid.Name);
        writer.WriteNumber("semi_major_axis", ellipsoid.SemiMajorAxis);
        if (ellipsoid.IsIvfDefinitive && !double.IsNaN(ellipsoid.InverseFlattening) && !double.IsInfinity(ellipsoid.InverseFlattening))
        {
            writer.WriteNumber("inverse_flattening", ellipsoid.InverseFlattening);
        }
        else
        {
            writer.WriteNumber("semi_minor_axis", ellipsoid.SemiMinorAxis);
        }

        writer.WritePropertyName("unit");
        WriteLinearUnit(writer, ellipsoid.AxisUnit);
        WriteIdentifier(writer, ellipsoid);
        writer.WriteEndObject();
    }

    private static void WritePrimeMeridian(Utf8JsonWriter writer, PrimeMeridian primeMeridian)
    {
        writer.WriteStartObject();
        writer.WriteString("name", primeMeridian.Name);
        writer.WriteNumber("longitude", primeMeridian.Longitude);
        writer.WritePropertyName("unit");
        WriteAngularUnit(writer, primeMeridian.AngularUnit);
        WriteIdentifier(writer, primeMeridian);
        writer.WriteEndObject();
    }

    private static void WriteVerticalDatum(Utf8JsonWriter writer, VerticalDatum verticalDatum)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "VerticalReferenceFrame");
        writer.WriteString("name", verticalDatum.Name);
        WriteIdentifier(writer, verticalDatum);
        writer.WriteEndObject();
    }

    private static void WriteConversion(Utf8JsonWriter writer, IProjection projection, AngularUnit angularUnit, LinearUnit linearUnit)
    {
        string methodKey = ProjectionSerializationSupport.NormalizeMethodKey(projection.ClassName);
        string methodName = ProjectionSerializationSupport.GetMethodName(projection.ClassName);
        string conversionName = string.IsNullOrWhiteSpace(projection.Name) || projection.Name.Equals(projection.ClassName, StringComparison.OrdinalIgnoreCase)
            ? methodName
            : projection.Name;

        writer.WriteStartObject();
        writer.WriteString("type", "Conversion");
        writer.WriteString("name", conversionName);

        writer.WritePropertyName("method");
        WriteMethod(writer, methodName);

        writer.WritePropertyName("parameters");
        writer.WriteStartArray();
        for (int i = 0; i < projection.NumParameters; i++)
        {
            WriteProjectionParameter(writer, methodKey, projection.GetParameter(i), angularUnit, linearUnit);
        }

        writer.WriteEndArray();
        WriteIdentifier(writer, projection);
        writer.WriteEndObject();
    }

    private static void WriteMethod(Utf8JsonWriter writer, string methodName)
    {
        writer.WriteStartObject();
        writer.WriteString("name", methodName);
        writer.WriteEndObject();
    }

    private static void WriteProjectionParameter(Utf8JsonWriter writer, string methodKey, ProjectionParameter parameter, AngularUnit angularUnit, LinearUnit linearUnit)
    {
        writer.WriteStartObject();
        writer.WriteString("name", ProjectionSerializationSupport.GetParameterName(methodKey, parameter.Name));
        writer.WriteNumber("value", parameter.Value);

        if (ProjectionSerializationSupport.ParameterUsesAngularUnit(parameter.Name))
        {
            writer.WritePropertyName("unit");
            WriteAngularUnit(writer, angularUnit);
        }
        else if (ProjectionSerializationSupport.ParameterUsesLinearUnit(parameter.Name))
        {
            writer.WritePropertyName("unit");
            WriteLinearUnit(writer, linearUnit);
        }
        else if (ProjectionSerializationSupport.ParameterUsesScaleUnit(parameter.Name))
        {
            writer.WritePropertyName("unit");
            WriteScaleUnit(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteUnit(Utf8JsonWriter writer, IUnit unit)
    {
        switch (unit)
        {
            case AngularUnit angularUnit:
                WriteAngularUnit(writer, angularUnit);
                break;
            case LinearUnit linearUnit:
                WriteLinearUnit(writer, linearUnit);
                break;
            default:
                throw new NotSupportedException($"PROJJSON writing is not supported for unit type '{unit.GetType().Name}'.");
        }
    }

    private static void WriteAngularUnit(Utf8JsonWriter writer, AngularUnit angularUnit)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "AngularUnit");
        writer.WriteString("name", angularUnit.Name);
        writer.WriteNumber("conversion_factor", angularUnit.RadiansPerUnit);
        WriteIdentifier(writer, angularUnit);
        writer.WriteEndObject();
    }

    private static void WriteLinearUnit(Utf8JsonWriter writer, LinearUnit linearUnit)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "LinearUnit");
        writer.WriteString("name", linearUnit.Name);
        writer.WriteNumber("conversion_factor", linearUnit.MetersPerUnit);
        WriteIdentifier(writer, linearUnit);
        writer.WriteEndObject();
    }

    private static void WriteScaleUnit(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "ScaleUnit");
        writer.WriteString("name", "unity");
        writer.WriteNumber("conversion_factor", 1);
        writer.WritePropertyName("id");
        writer.WriteStartObject();
        writer.WriteString("authority", "EPSG");
        writer.WriteNumber("code", 9201);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteCompoundComponents(Utf8JsonWriter writer, CompoundCoordinateSystem coordinateSystem)
    {
        WriteCompoundComponent(writer, coordinateSystem.HeadCoordinateSystem);
        WriteCompoundComponent(writer, coordinateSystem.TailCoordinateSystem);
    }

    private static void WriteCompoundComponent(Utf8JsonWriter writer, CoordinateSystem coordinateSystem)
    {
        if (coordinateSystem is CompoundCoordinateSystem nested)
        {
            WriteCompoundComponents(writer, nested);
            return;
        }

        WriteCoordinateSystem(writer, coordinateSystem);
    }

    private static void ThrowIfBoundHorizontalDatumRequiresBoundCrs(HorizontalDatum horizontalDatum, string coordinateSystemTypeName)
    {
        if (horizontalDatum.Wgs84Parameters is not null)
        {
            throw new NotSupportedException($"PROJJSON writing for '{coordinateSystemTypeName}' with retained WGS84 conversion metadata is not implemented. A BoundCRS writer is required to preserve that transformation.");
        }
    }

    private static void ThrowIfBoundVerticalMetadataRequiresBoundCrs(VerticalCoordinateSystem coordinateSystem)
    {
        if (coordinateSystem.BoundGridTransformation is not null)
        {
            throw new NotSupportedException("PROJJSON writing for vertical coordinate systems with retained bound-grid metadata is not implemented. A BoundCRS writer is required to preserve that transformation.");
        }
    }

    private static void WriteIdentifier(Utf8JsonWriter writer, IInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Authority) || info.AuthorityCode <= 0)
        {
            return;
        }

        writer.WritePropertyName("id");
        writer.WriteStartObject();
        writer.WriteString("authority", info.Authority);
        writer.WriteNumber("code", info.AuthorityCode);
        writer.WriteEndObject();
    }

    private static string GetAxisDirection(CoordinateSystem coordinateSystem, int axisIndex, AxisOrientationEnum orientation)
    {
        if (coordinateSystem is GeocentricCoordinateSystem)
        {
            return axisIndex switch
            {
                0 => "geocentricX",
                1 => "geocentricY",
                2 => "geocentricZ",
                _ => throw new NotSupportedException($"PROJJSON writing is not supported for geocentric axis index '{axisIndex}'."),
            };
        }

        return orientation switch
        {
            AxisOrientationEnum.North => "north",
            AxisOrientationEnum.South => "south",
            AxisOrientationEnum.East => "east",
            AxisOrientationEnum.West => "west",
            AxisOrientationEnum.Up => "up",
            AxisOrientationEnum.Down => "down",
            _ => throw new NotSupportedException($"PROJJSON writing is not supported for axis orientation '{orientation}'."),
        };
    }
}
