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
            default:
                throw new NotSupportedException($"PROJJSON writing is not supported for coordinate system type '{coordinateSystem.GetType().Name}'.");
        }
    }

    private static void WriteGeographicCoordinateSystem(Utf8JsonWriter writer, GeographicCoordinateSystem coordinateSystem)
    {
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

    private static void WriteCoordinateSystemDefinition(Utf8JsonWriter writer, string subtype, CoordinateSystem coordinateSystem)
    {
        writer.WriteStartObject();
        writer.WriteString("subtype", subtype);
        writer.WritePropertyName("axis");
        writer.WriteStartArray();
        for (int i = 0; i < coordinateSystem.Dimension; i++)
        {
            WriteAxis(writer, coordinateSystem.GetAxis(i), coordinateSystem.GetUnits(i));
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteAxis(Utf8JsonWriter writer, AxisInfo axisInfo, IUnit unit)
    {
        writer.WriteStartObject();
        writer.WriteString("name", axisInfo.Name);
        writer.WriteString("direction", GetAxisDirection(axisInfo.Orientation));
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

    private static string GetAxisDirection(AxisOrientationEnum orientation)
    {
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
