// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Globalization;
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
    private static readonly AngularUnit ArcSecondUnit = new(4.84813681109535993589914102357e-6, "arc-second", "EPSG", 9104, "arcsec", string.Empty, "=pi/648000 radians.");

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
            case BoundCoordinateSystem boundCoordinateSystem:
                WriteBoundCoordinateSystem(writer, boundCoordinateSystem);
                break;
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
            case FittedCoordinateSystem fittedCoordinateSystem:
                WriteDerivedCoordinateSystem(writer, fittedCoordinateSystem);
                break;
            default:
                throw new NotSupportedException($"PROJJSON writing is not supported for coordinate system type '{coordinateSystem.GetType().Name}'.");
        }
    }

    private static void WriteGeographicCoordinateSystem(Utf8JsonWriter writer, GeographicCoordinateSystem coordinateSystem)
    {
        if (TryWriteLegacyBoundCoordinateSystem(writer, coordinateSystem))
        {
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("type", "GeographicCRS");
        writer.WriteString("name", coordinateSystem.Name);

        WriteHorizontalDatumProperty(writer, coordinateSystem.HorizontalDatum);

        writer.WritePropertyName("prime_meridian");
        WritePrimeMeridian(writer, coordinateSystem.PrimeMeridian);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "ellipsoidal", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteGeocentricCoordinateSystem(Utf8JsonWriter writer, GeocentricCoordinateSystem coordinateSystem)
    {
        if (TryWriteLegacyBoundCoordinateSystem(writer, coordinateSystem))
        {
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("type", "GeodeticCRS");
        writer.WriteString("name", coordinateSystem.Name);

        WriteHorizontalDatumProperty(writer, coordinateSystem.HorizontalDatum);

        writer.WritePropertyName("prime_meridian");
        WritePrimeMeridian(writer, coordinateSystem.PrimeMeridian);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "Cartesian", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteProjectedCoordinateSystem(Utf8JsonWriter writer, ProjectedCoordinateSystem coordinateSystem)
    {
        if (TryWriteLegacyBoundCoordinateSystem(writer, coordinateSystem))
        {
            return;
        }

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

    private static void WriteDerivedCoordinateSystem(Utf8JsonWriter writer, FittedCoordinateSystem coordinateSystem)
    {
        Projection derivingConversion = DerivedCoordinateSystemSupport.CreateAffineConversion(
            coordinateSystem.ToBaseTransform,
            DerivedCoordinateSystemSupport.DefaultDerivingConversionName);

        switch (coordinateSystem.BaseCoordinateSystem)
        {
            case GeographicCoordinateSystem geographicCoordinateSystem:
                if (BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(geographicCoordinateSystem) is not null)
                {
                    throw new NotSupportedException("PROJJSON derived geographic CRS output does not support base CRS definitions that expand to BoundCRS.");
                }

                writer.WriteStartObject();
                writer.WriteString("type", "DerivedGeographicCRS");
                writer.WriteString("name", coordinateSystem.Name);
                writer.WritePropertyName("base_crs");
                WriteGeographicCoordinateSystem(writer, geographicCoordinateSystem);
                writer.WritePropertyName("conversion");
                WriteDerivedAffineConversion(writer, derivingConversion, geographicCoordinateSystem.AngularUnit, null);
                writer.WritePropertyName("coordinate_system");
                WriteCoordinateSystemDefinition(writer, "ellipsoidal", coordinateSystem);
                WriteIdentifier(writer, coordinateSystem);
                writer.WriteEndObject();
                break;
            case ProjectedCoordinateSystem projectedCoordinateSystem:
                if (BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(projectedCoordinateSystem) is not null)
                {
                    throw new NotSupportedException("PROJJSON derived projected CRS output does not support base CRS definitions that expand to BoundCRS.");
                }

                writer.WriteStartObject();
                writer.WriteString("type", "DerivedProjectedCRS");
                writer.WriteString("name", coordinateSystem.Name);
                writer.WritePropertyName("base_crs");
                WriteProjectedCoordinateSystem(writer, projectedCoordinateSystem);
                writer.WritePropertyName("conversion");
                WriteDerivedAffineConversion(writer, derivingConversion, null, projectedCoordinateSystem.LinearUnit);
                writer.WritePropertyName("coordinate_system");
                WriteCoordinateSystemDefinition(writer, "Cartesian", coordinateSystem);
                WriteIdentifier(writer, coordinateSystem);
                writer.WriteEndObject();
                break;
            default:
                throw new NotSupportedException("PROJJSON derived CRS writing currently supports only affine transforms based on two-dimensional geographic or projected coordinate systems.");
        }
    }

    private static void WriteVerticalCoordinateSystem(Utf8JsonWriter writer, VerticalCoordinateSystem coordinateSystem)
    {
        if (TryWriteLegacyBoundCoordinateSystem(writer, coordinateSystem))
        {
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("type", "VerticalCRS");
        writer.WriteString("name", coordinateSystem.Name);

        WriteVerticalDatumProperty(writer, coordinateSystem.VerticalDatum);

        writer.WritePropertyName("coordinate_system");
        WriteCoordinateSystemDefinition(writer, "vertical", coordinateSystem);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static bool TryWriteLegacyBoundCoordinateSystem(Utf8JsonWriter writer, CoordinateSystem coordinateSystem)
    {
        BoundCoordinateSystem? boundCoordinateSystem = BoundCoordinateSystemSupport.CreateLegacyBoundCoordinateSystemForSerialization(coordinateSystem);
        if (boundCoordinateSystem is null)
        {
            return false;
        }

        WriteBoundCoordinateSystem(writer, boundCoordinateSystem);
        return true;
    }

    private static void WriteBoundCoordinateSystem(Utf8JsonWriter writer, BoundCoordinateSystem coordinateSystem)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "BoundCRS");
        writer.WriteString("name", coordinateSystem.Name);

        writer.WritePropertyName("source_crs");
        WriteBoundCoordinateSystemComponent(writer, coordinateSystem.SourceCoordinateSystem);

        writer.WritePropertyName("target_crs");
        WriteBoundCoordinateSystemComponent(writer, coordinateSystem.TargetCoordinateSystem);

        writer.WritePropertyName("transformation");
        WriteBoundTransformation(writer, coordinateSystem.SourceCoordinateSystem, coordinateSystem.TargetCoordinateSystem, coordinateSystem.Transformation);

        WriteIdentifier(writer, coordinateSystem);
        writer.WriteEndObject();
    }

    private static void WriteBoundCoordinateSystemComponent(Utf8JsonWriter writer, CoordinateSystem coordinateSystem)
    {
        if (coordinateSystem is BoundCoordinateSystem boundCoordinateSystem)
        {
            WriteBoundCoordinateSystem(writer, boundCoordinateSystem);
            return;
        }

        WriteCoordinateSystem(writer, BoundCoordinateSystemSupport.CreateCoordinateSystemWithoutLegacyBoundMetadata(coordinateSystem));
    }

    private static void WriteBoundTransformation(
        Utf8JsonWriter writer,
        CoordinateSystem sourceCoordinateSystem,
        CoordinateSystem targetCoordinateSystem,
        BoundTransformation transformation)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "AbridgedTransformation");
        writer.WriteString("name", $"{sourceCoordinateSystem.Name} to {targetCoordinateSystem.Name}");

        writer.WritePropertyName("method");
        WriteMethod(writer, transformation.MethodName);

        writer.WritePropertyName("parameters");
        writer.WriteStartArray();
        if (transformation.UsesParameterFile)
        {
            WriteBoundFileParameter(
                writer,
                "Geoid (height correction) model file",
                ArgumentGuard.ThrowIfNull(transformation.ParameterFileName, nameof(transformation.ParameterFileName)));
        }
        else if (transformation.Wgs84Parameters is not null)
        {
            WriteBoundTransformationParameters(writer, transformation.MethodName, transformation.Wgs84Parameters);
        }
        else
        {
            throw new NotSupportedException("BoundCRS transformations must define either numeric parameters or a parameter file.");
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteBoundTransformationParameters(Utf8JsonWriter writer, string methodName, Wgs84ConversionInfo parameters)
    {
        if (IsGeocentricTranslationsMethod(methodName))
        {
            WriteBoundLinearParameter(writer, "X-axis translation", parameters.Dx);
            WriteBoundLinearParameter(writer, "Y-axis translation", parameters.Dy);
            WriteBoundLinearParameter(writer, "Z-axis translation", parameters.Dz);
            return;
        }

        if (IsPositionVectorMethod(methodName) || IsCoordinateFrameRotationMethod(methodName))
        {
            WriteBoundLinearParameter(writer, "X-axis translation", parameters.Dx);
            WriteBoundLinearParameter(writer, "Y-axis translation", parameters.Dy);
            WriteBoundLinearParameter(writer, "Z-axis translation", parameters.Dz);
            WriteBoundAngularParameter(writer, "X-axis rotation", parameters.Ex);
            WriteBoundAngularParameter(writer, "Y-axis rotation", parameters.Ey);
            WriteBoundAngularParameter(writer, "Z-axis rotation", parameters.Ez);
            WriteBoundScaleParameter(writer, "Scale difference", parameters.Ppm);
            return;
        }

        throw new NotSupportedException($"BoundCRS transformation method '{methodName}' is not supported.");
    }

    private static void WriteBoundLinearParameter(Utf8JsonWriter writer, string name, double value)
    {
        writer.WriteStartObject();
        writer.WriteString("name", name);
        writer.WriteNumber("value", value);
        writer.WritePropertyName("unit");
        WriteLinearUnit(writer, LinearUnit.Metre);
        writer.WriteEndObject();
    }

    private static void WriteBoundAngularParameter(Utf8JsonWriter writer, string name, double value)
    {
        writer.WriteStartObject();
        writer.WriteString("name", name);
        writer.WriteNumber("value", value);
        writer.WritePropertyName("unit");
        WriteAngularUnit(writer, ArcSecondUnit);
        writer.WriteEndObject();
    }

    private static void WriteBoundScaleParameter(Utf8JsonWriter writer, string name, double value)
    {
        writer.WriteStartObject();
        writer.WriteString("name", name);
        writer.WriteNumber("value", value);
        writer.WritePropertyName("unit");
        WriteScaleUnit(writer, "parts per million", 1e-6);
        writer.WriteEndObject();
    }

    private static void WriteBoundFileParameter(Utf8JsonWriter writer, string name, string value)
    {
        writer.WriteStartObject();
        writer.WriteString("name", name);
        writer.WriteString("value", value);
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

    private static void WriteHorizontalDatumProperty(Utf8JsonWriter writer, HorizontalDatum horizontalDatum)
    {
        if (horizontalDatum.Ensemble is not null)
        {
            writer.WritePropertyName("datum_ensemble");
            WriteDatumEnsemble(writer, horizontalDatum.Ensemble);
            return;
        }

        writer.WritePropertyName("datum");
        WriteHorizontalDatum(writer, horizontalDatum);
    }

    private static void WriteVerticalDatumProperty(Utf8JsonWriter writer, VerticalDatum verticalDatum)
    {
        if (verticalDatum.Ensemble is not null)
        {
            writer.WritePropertyName("datum_ensemble");
            WriteDatumEnsemble(writer, verticalDatum.Ensemble);
            return;
        }

        writer.WritePropertyName("datum");
        WriteVerticalDatum(writer, verticalDatum);
    }

    private static void WriteDatumEnsemble(Utf8JsonWriter writer, DatumEnsemble datumEnsemble)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "DatumEnsemble");
        writer.WriteString("name", datumEnsemble.Name);

        writer.WritePropertyName("members");
        writer.WriteStartArray();
        for (int i = 0; i < datumEnsemble.Members.Count; i++)
        {
            WriteDatumEnsembleMember(writer, datumEnsemble.Members[i]);
        }

        writer.WriteEndArray();

        if (datumEnsemble.Ellipsoid is not null)
        {
            writer.WritePropertyName("ellipsoid");
            WriteEllipsoid(writer, datumEnsemble.Ellipsoid);
        }

        writer.WriteString("accuracy", datumEnsemble.Accuracy.ToString("G17", CultureInfo.InvariantCulture));
        WriteIdentifier(writer, datumEnsemble.Authority, datumEnsemble.AuthorityCode);
        writer.WriteEndObject();
    }

    private static void WriteDatumEnsembleMember(Utf8JsonWriter writer, DatumEnsembleMember member)
    {
        writer.WriteStartObject();
        writer.WriteString("name", member.Name);
        WriteIdentifier(writer, member.Authority, member.AuthorityCode);
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

    private static void WriteDerivedAffineConversion(Utf8JsonWriter writer, IProjection conversion, AngularUnit? angularUnit, LinearUnit? linearUnit)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "Conversion");
        writer.WriteString(
            "name",
            string.IsNullOrWhiteSpace(conversion.Name) ? DerivedCoordinateSystemSupport.DefaultDerivingConversionName : conversion.Name);

        writer.WritePropertyName("method");
        WriteMethod(writer, conversion.ClassName);

        writer.WritePropertyName("parameters");
        writer.WriteStartArray();
        for (int i = 0; i < conversion.NumParameters; i++)
        {
            WriteDerivedAffineParameter(writer, conversion.GetParameter(i), angularUnit, linearUnit);
        }

        writer.WriteEndArray();
        WriteIdentifier(writer, conversion);
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
            WriteScaleUnit(writer, "unity", 1d, "EPSG", 9201);
        }

        writer.WriteEndObject();
    }

    private static void WriteDerivedAffineParameter(Utf8JsonWriter writer, ProjectionParameter parameter, AngularUnit? angularUnit, LinearUnit? linearUnit)
    {
        writer.WriteStartObject();
        writer.WriteString("name", parameter.Name);
        writer.WriteNumber("value", parameter.Value);

        writer.WritePropertyName("unit");
        if (parameter.Name is "A0" or "B0")
        {
            if (angularUnit is not null)
            {
                WriteAngularUnit(writer, angularUnit);
            }
            else if (linearUnit is not null)
            {
                WriteLinearUnit(writer, linearUnit);
            }
            else
            {
                throw new NotSupportedException("Derived affine conversion parameters require either an angular or linear translation unit.");
            }
        }
        else
        {
            WriteScaleUnit(writer, "unity", 1d, "EPSG", 9201);
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

    private static void WriteScaleUnit(Utf8JsonWriter writer, string name, double conversionFactor, string? authority = null, long authorityCode = -1)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "ScaleUnit");
        writer.WriteString("name", name);
        writer.WriteNumber("conversion_factor", conversionFactor);
        if (!string.IsNullOrWhiteSpace(authority) && authorityCode > 0)
        {
            writer.WritePropertyName("id");
            writer.WriteStartObject();
            writer.WriteString("authority", authority);
            writer.WriteNumber("code", authorityCode);
            writer.WriteEndObject();
        }

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

    private static bool IsGeocentricTranslationsMethod(string methodName)
    {
        return methodName.StartsWith("Geocentric translations", StringComparison.Ordinal);
    }

    private static bool IsPositionVectorMethod(string methodName)
    {
        return methodName.StartsWith("Position Vector transformation", StringComparison.Ordinal);
    }

    private static bool IsCoordinateFrameRotationMethod(string methodName)
    {
        return methodName.StartsWith("Coordinate Frame rotation", StringComparison.Ordinal);
    }

    private static void WriteIdentifier(Utf8JsonWriter writer, IInfo info)
    {
        WriteIdentifier(writer, info.Authority, info.AuthorityCode);
    }

    private static void WriteIdentifier(Utf8JsonWriter writer, string authority, long authorityCode)
    {
        if (string.IsNullOrWhiteSpace(authority) || authorityCode <= 0)
        {
            return;
        }

        writer.WritePropertyName("id");
        writer.WriteStartObject();
        writer.WriteString("authority", authority);
        writer.WriteNumber("code", authorityCode);
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
