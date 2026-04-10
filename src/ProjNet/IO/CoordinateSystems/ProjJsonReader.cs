// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.CoordinateSystems;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Creates an object based on the supplied PROJJSON text.
/// </summary>
public static class ProjJsonReader
{
    /// <summary>
    /// Reads and parses a PROJJSON-formatted text.
    /// </summary>
    /// <param name="json">String containing PROJJSON.</param>
    /// <returns>Object representation of the PROJJSON text.</returns>
    public static IInfo Parse(string json) => Parse(json.AsSpan());

    /// <summary>
    /// Reads and parses a PROJJSON-formatted text from a character span.
    /// </summary>
    /// <param name="json">Character span containing PROJJSON.</param>
    /// <returns>Object representation of the PROJJSON text.</returns>
    public static IInfo Parse(ReadOnlySpan<char> json)
    {
        if (json.IsEmpty || IsWhitespaceOnly(json))
        {
            ArgumentGuard.ThrowArgumentNull(nameof(json));
        }

        using var document = JsonDocument.Parse(json.ToString());
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            ArgumentGuard.ThrowArgument("PROJJSON root must be an object.", nameof(json));
        }

        return ReadInfo(document.RootElement);
    }

    private static bool IsWhitespaceOnly(ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (!char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static IInfo ReadInfo(JsonElement element)
    {
        string type = GetRequiredString(element, "type");
        if (string.Equals(type, "GeographicCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadGeographicCoordinateSystem(element);
        }

        if (string.Equals(type, "GeodeticCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadGeodeticCoordinateSystem(element);
        }

        if (string.Equals(type, "ProjectedCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadProjectedCoordinateSystem(element);
        }

        if (string.Equals(type, "DerivedGeographicCRS", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "DerivedGeodeticCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadDerivedGeodeticCoordinateSystem(element);
        }

        if (string.Equals(type, "DerivedProjectedCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadDerivedProjectedCoordinateSystem(element);
        }

        if (string.Equals(type, "BoundCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadBoundCoordinateSystem(element);
        }

        if (string.Equals(type, "VerticalCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadVerticalCoordinateSystem(element);
        }

        if (string.Equals(type, "CompoundCRS", StringComparison.OrdinalIgnoreCase))
        {
            return ReadCompoundCoordinateSystem(element);
        }

        throw new NotSupportedException($"PROJJSON type '{type}' is not supported.");
    }

    private static GeographicCoordinateSystem ReadGeographicCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        HorizontalDatum horizontalDatum = ReadHorizontalDatumOrEnsemble(element);
        PrimeMeridian primeMeridian = element.TryGetProperty("prime_meridian", out JsonElement primeMeridianElement)
            ? ReadPrimeMeridian(primeMeridianElement)
            : PrimeMeridian.Greenwich;

        ReadCoordinateSystemDefinition(
            GetRequiredProperty(element, "coordinate_system"),
            out string coordinateSystemType,
            out int coordinateSystemDimension,
            out List<AxisInfo> axisInfo,
            out AngularUnit? angularUnit,
            out _);

        if (!string.Equals(coordinateSystemType, "ellipsoidal", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON coordinate system subtype '{coordinateSystemType}' is not supported for GeographicCRS.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("PROJJSON GeographicCRS dimensions other than 2 are not supported.");
        }

        if (angularUnit is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON GeographicCRS is missing axis angular units.");
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        return new GeographicCoordinateSystem(
            angularUnit,
            horizontalDatum,
            primeMeridian,
            axisInfo,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CoordinateSystem ReadGeodeticCoordinateSystem(JsonElement element)
    {
        JsonElement coordinateSystemElement = GetRequiredProperty(element, "coordinate_system");
        string coordinateSystemType = GetRequiredString(coordinateSystemElement, "subtype");
        if (string.Equals(coordinateSystemType, "ellipsoidal", StringComparison.OrdinalIgnoreCase))
        {
            return ReadGeographicCoordinateSystem(element);
        }

        if (string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            return ReadGeocentricCoordinateSystem(element);
        }

        throw new NotSupportedException($"PROJJSON GeodeticCRS coordinate system subtype '{coordinateSystemType}' is not supported.");
    }

    private static GeocentricCoordinateSystem ReadGeocentricCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        HorizontalDatum horizontalDatum = ReadHorizontalDatumOrEnsemble(element);
        PrimeMeridian primeMeridian = element.TryGetProperty("prime_meridian", out JsonElement primeMeridianElement)
            ? ReadPrimeMeridian(primeMeridianElement)
            : PrimeMeridian.Greenwich;

        ReadCoordinateSystemDefinition(
            GetRequiredProperty(element, "coordinate_system"),
            out string coordinateSystemType,
            out int coordinateSystemDimension,
            out List<AxisInfo> axisInfo,
            out _,
            out LinearUnit? linearUnit);

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON coordinate system subtype '{coordinateSystemType}' is not supported for geocentric GeodeticCRS.");
        }

        if (coordinateSystemDimension != 3)
        {
            throw new NotSupportedException("PROJJSON cartesian GeodeticCRS dimensions other than 3 are not supported.");
        }

        if (linearUnit is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON geocentric GeodeticCRS is missing axis linear units.");
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        return new GeocentricCoordinateSystem(
            horizontalDatum,
            linearUnit,
            primeMeridian,
            axisInfo,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static ProjectedCoordinateSystem ReadProjectedCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        GeographicCoordinateSystem baseCrs = ReadGeographicCoordinateSystem(GetRequiredProperty(element, "base_crs"));
        Projection conversion = ReadConversion(GetRequiredProperty(element, "conversion"));

        ReadCoordinateSystemDefinition(
            GetRequiredProperty(element, "coordinate_system"),
            out string coordinateSystemType,
            out int coordinateSystemDimension,
            out List<AxisInfo> axisInfo,
            out _,
            out LinearUnit? linearUnit);

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON coordinate system subtype '{coordinateSystemType}' is not supported for ProjectedCRS.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("PROJJSON ProjectedCRS dimensions other than 2 are not supported.");
        }

        if (linearUnit is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON ProjectedCRS is missing axis linear units.");
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        return new ProjectedCoordinateSystem(
            baseCrs.HorizontalDatum,
            baseCrs,
            linearUnit,
            conversion,
            axisInfo,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static FittedCoordinateSystem ReadDerivedGeodeticCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        CoordinateSystem baseCoordinateSystem = ReadCoordinateSystemElement(GetRequiredProperty(element, "base_crs"), "base_crs");
        if (baseCoordinateSystem is not GeographicCoordinateSystem baseGeographicCoordinateSystem)
        {
            throw new NotSupportedException("PROJJSON derived geodetic CRS currently supports only geographic base CRS definitions.");
        }

        Projection conversion = ReadConversion(GetRequiredProperty(element, "conversion"));
        AffineTransform transform = DerivedCoordinateSystemSupport.CreateAffineTransform(conversion);

        ReadCoordinateSystemDefinition(
            GetRequiredProperty(element, "coordinate_system"),
            out string coordinateSystemType,
            out int coordinateSystemDimension,
            out List<AxisInfo> axisInfo,
            out AngularUnit? angularUnit,
            out _);

        if (!string.Equals(coordinateSystemType, "ellipsoidal", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON derived geodetic coordinate-system subtype '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("PROJJSON derived geodetic CRS dimensions other than 2 are not supported.");
        }

        if (angularUnit is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON derived geodetic CRS is missing axis angular units.");
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        return CreateDerivedCoordinateSystem(name, baseGeographicCoordinateSystem, transform, axisInfo, authority, authorityCode);
    }

    private static FittedCoordinateSystem ReadDerivedProjectedCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        CoordinateSystem baseCoordinateSystem = ReadCoordinateSystemElement(GetRequiredProperty(element, "base_crs"), "base_crs");
        if (baseCoordinateSystem is not ProjectedCoordinateSystem baseProjectedCoordinateSystem)
        {
            throw new NotSupportedException("PROJJSON derived projected CRS currently supports only projected base CRS definitions.");
        }

        Projection conversion = ReadConversion(GetRequiredProperty(element, "conversion"));
        AffineTransform transform = DerivedCoordinateSystemSupport.CreateAffineTransform(conversion);

        ReadCoordinateSystemDefinition(
            GetRequiredProperty(element, "coordinate_system"),
            out string coordinateSystemType,
            out int coordinateSystemDimension,
            out List<AxisInfo> axisInfo,
            out _,
            out LinearUnit? linearUnit);

        if (!string.Equals(coordinateSystemType, "cartesian", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON derived projected coordinate-system subtype '{coordinateSystemType}' is not supported.");
        }

        if (coordinateSystemDimension != 2)
        {
            throw new NotSupportedException("PROJJSON derived projected CRS dimensions other than 2 are not supported.");
        }

        if (linearUnit is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON derived projected CRS is missing axis linear units.");
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        return CreateDerivedCoordinateSystem(name, baseProjectedCoordinateSystem, transform, axisInfo, authority, authorityCode);
    }

    private static BoundCoordinateSystem ReadBoundCoordinateSystem(JsonElement element)
    {
        CoordinateSystem sourceCoordinateSystem = ReadCoordinateSystemElement(GetRequiredProperty(element, "source_crs"), "source_crs");
        CoordinateSystem targetCoordinateSystem = ReadCoordinateSystemElement(GetRequiredProperty(element, "target_crs"), "target_crs");
        BoundTransformation transformation = ReadBoundTransformation(GetRequiredProperty(element, "transformation"), sourceCoordinateSystem);

        string name = GetOptionalString(element, "name") ?? sourceCoordinateSystem.Name;
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new BoundCoordinateSystem(
            sourceCoordinateSystem,
            targetCoordinateSystem,
            transformation,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static VerticalCoordinateSystem ReadVerticalCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        VerticalDatum verticalDatum = ReadVerticalDatumOrEnsemble(element);

        ReadCoordinateSystemDefinition(
            GetRequiredProperty(element, "coordinate_system"),
            out string coordinateSystemType,
            out int coordinateSystemDimension,
            out List<AxisInfo> axisInfo,
            out _,
            out LinearUnit? linearUnit);

        if (!string.Equals(coordinateSystemType, "vertical", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON coordinate system subtype '{coordinateSystemType}' is not supported for VerticalCRS.");
        }

        if (coordinateSystemDimension != 1 || axisInfo.Count != 1)
        {
            throw new NotSupportedException("PROJJSON VerticalCRS dimensions other than 1 are not supported.");
        }

        if (linearUnit is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON VerticalCRS is missing axis linear units.");
        }

        verticalDatum = ApplyVerticalDatumTypeForAxis(verticalDatum, axisInfo[0]);
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new VerticalCoordinateSystem(
            linearUnit,
            verticalDatum,
            axisInfo[0],
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static CompoundCoordinateSystem ReadCompoundCoordinateSystem(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        JsonElement componentsElement = GetRequiredProperty(element, "components");
        if (componentsElement.ValueKind != JsonValueKind.Array)
        {
            ArgumentGuard.ThrowArgument("PROJJSON CompoundCRS components must be an array.");
        }

        var components = new List<CoordinateSystem>();
        foreach (JsonElement componentElement in componentsElement.EnumerateArray())
        {
            if (ReadInfo(componentElement) is not CoordinateSystem coordinateSystem)
            {
                throw new NotSupportedException("PROJJSON CompoundCRS components must be coordinate reference systems.");
            }

            components.Add(coordinateSystem);
        }

        if (components.Count < 2)
        {
            ArgumentGuard.ThrowArgument("PROJJSON CompoundCRS must contain at least two components.");
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        CoordinateSystem head = components[0];
        CoordinateSystem tail = components[1];
        var combined = new CompoundCoordinateSystem(head, tail, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        for (int i = 2; i < components.Count; i++)
        {
            combined = new CompoundCoordinateSystem(combined, components[i], name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
        }

        return combined;
    }

    private static BoundTransformation ReadBoundTransformation(JsonElement element, CoordinateSystem sourceCoordinateSystem)
    {
        if (element.TryGetProperty("source_crs", out JsonElement transformationSourceCrsElement))
        {
            CoordinateSystem transformationSourceCoordinateSystem = ReadCoordinateSystemElement(transformationSourceCrsElement, "transformation.source_crs");
            if (!transformationSourceCoordinateSystem.EqualParams(sourceCoordinateSystem))
            {
                throw new NotSupportedException("PROJJSON BoundCRS transformations with an overriding source_crs are not supported.");
            }
        }

        string methodName = GetRequiredString(GetRequiredProperty(element, "method"), "name");
        JsonElement parametersElement = GetRequiredProperty(element, "parameters");
        if (parametersElement.ValueKind != JsonValueKind.Array)
        {
            ArgumentGuard.ThrowArgument("PROJJSON BoundCRS transformation parameters must be an array.");
        }

        var parameters = new Wgs84ConversionInfo();
        bool hasNumericParameters = false;
        string? parameterFileName = null;

        foreach (JsonElement parameterElement in parametersElement.EnumerateArray())
        {
            string parameterName = GetRequiredString(parameterElement, "name");
            JsonElement valueElement = GetRequiredProperty(parameterElement, "value");

            if (valueElement.ValueKind == JsonValueKind.Number)
            {
                BoundCoordinateSystemSupport.AssignTransformationParameter(parameterName, valueElement.GetDouble(), parameters);
                hasNumericParameters = true;
                continue;
            }

            if (valueElement.ValueKind != JsonValueKind.String)
            {
                ArgumentGuard.ThrowArgument("PROJJSON BoundCRS transformation parameter values must be numbers or strings.");
            }

            string candidateParameterFileName = valueElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(candidateParameterFileName))
            {
                ArgumentGuard.ThrowArgument("PROJJSON BoundCRS transformation parameter file references must be non-empty.");
            }

            if (parameterFileName is not null
                && !BoundCoordinateSystemSupport.AreEquivalentParameterFileReferences(parameterFileName, candidateParameterFileName))
            {
                throw new NotSupportedException("PROJJSON BoundCRS transformations with multiple parameter files are not supported.");
            }

            parameterFileName = candidateParameterFileName;
        }

        return BoundCoordinateSystemSupport.CreateBoundTransformation(
            methodName,
            hasNumericParameters ? parameters : null,
            parameterFileName);
    }

    private static Projection ReadConversion(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        string className = GetRequiredString(GetRequiredProperty(element, "method"), "name");

        var parameters = new List<ProjectionParameter>();
        if (element.TryGetProperty("parameters", out JsonElement parametersElement))
        {
            if (parametersElement.ValueKind != JsonValueKind.Array)
            {
                ArgumentGuard.ThrowArgument("PROJJSON conversion parameters must be an array.");
            }

            foreach (JsonElement parameterElement in parametersElement.EnumerateArray())
            {
                string parameterName = NormalizeProjectionParameterName(GetRequiredString(parameterElement, "name"));
                double value = GetRequiredDouble(parameterElement, "value");
                parameters.Add(new ProjectionParameter(parameterName, value));
            }
        }

        ReadIdentifier(element, out string authority, out long authorityCode);
        return new Projection(className, parameters, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static CoordinateSystem ReadCoordinateSystemElement(JsonElement element, string propertyName)
    {
        if (ReadInfo(element) is not CoordinateSystem coordinateSystem)
        {
            throw new NotSupportedException($"PROJJSON {propertyName} must be a coordinate reference system.");
        }

        return coordinateSystem;
    }

    private static FittedCoordinateSystem CreateDerivedCoordinateSystem(
        string name,
        CoordinateSystem baseCoordinateSystem,
        AffineTransform transform,
        List<AxisInfo> axisInfo,
        string authority,
        long authorityCode)
    {
        var fittedCoordinateSystem = new FittedCoordinateSystem(
            baseCoordinateSystem,
            transform,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
        fittedCoordinateSystem.AxisInfo = new List<AxisInfo>(axisInfo);
        return fittedCoordinateSystem;
    }

    private static void ReadCoordinateSystemDefinition(
        JsonElement element,
        out string coordinateSystemType,
        out int coordinateSystemDimension,
        out List<AxisInfo> axisInfo,
        out AngularUnit? angularUnit,
        out LinearUnit? linearUnit)
    {
        coordinateSystemType = GetRequiredString(element, "subtype");
        angularUnit = null;
        linearUnit = null;
        axisInfo = [];

        JsonElement axisArray = GetRequiredProperty(element, "axis");
        if (axisArray.ValueKind != JsonValueKind.Array)
        {
            ArgumentGuard.ThrowArgument("PROJJSON coordinate system axis definition must be an array.");
        }

        foreach (JsonElement axisElement in axisArray.EnumerateArray())
        {
            axisInfo.Add(ReadAxis(axisElement, out AngularUnit? axisAngularUnit, out LinearUnit? axisLinearUnit));
            angularUnit = MergeAngularUnit(angularUnit, axisAngularUnit);
            linearUnit = MergeLinearUnit(linearUnit, axisLinearUnit);
        }

        coordinateSystemDimension = axisInfo.Count;
    }

    private static AxisInfo ReadAxis(JsonElement element, out AngularUnit? angularUnit, out LinearUnit? linearUnit)
    {
        string axisName = GetRequiredString(element, "name");
        AxisOrientationEnum orientation = ParseAxisOrientation(GetRequiredString(element, "direction"));
        angularUnit = null;
        linearUnit = null;

        if (element.TryGetProperty("unit", out JsonElement unitElement))
        {
            if (IsAngularUnit(unitElement))
            {
                angularUnit = ReadAngularUnit(unitElement);
            }
            else if (IsLinearUnit(unitElement))
            {
                linearUnit = ReadLinearUnit(unitElement);
            }
        }

        return new AxisInfo(axisName, orientation);
    }

    private static HorizontalDatum ReadHorizontalDatumOrEnsemble(JsonElement element)
    {
        bool hasDatum = element.TryGetProperty("datum", out JsonElement datumElement);
        bool hasDatumEnsemble = element.TryGetProperty("datum_ensemble", out JsonElement datumEnsembleElement);
        if (hasDatum == hasDatumEnsemble)
        {
            ArgumentGuard.ThrowArgument("PROJJSON geodetic CRS must contain exactly one of datum or datum_ensemble.");
        }

        return hasDatum
            ? ReadHorizontalDatum(datumElement)
            : ReadHorizontalDatumEnsemble(datumEnsembleElement);
    }

    private static VerticalDatum ReadVerticalDatumOrEnsemble(JsonElement element)
    {
        bool hasDatum = element.TryGetProperty("datum", out JsonElement datumElement);
        bool hasDatumEnsemble = element.TryGetProperty("datum_ensemble", out JsonElement datumEnsembleElement);
        if (hasDatum == hasDatumEnsemble)
        {
            ArgumentGuard.ThrowArgument("PROJJSON vertical CRS must contain exactly one of datum or datum_ensemble.");
        }

        return hasDatum
            ? ReadVerticalDatum(datumElement)
            : ReadVerticalDatumEnsemble(datumEnsembleElement);
    }

    private static HorizontalDatum ReadHorizontalDatum(JsonElement element)
    {
        string datumType = GetOptionalString(element, "type") ?? "GeodeticReferenceFrame";
        if (!string.Equals(datumType, "GeodeticReferenceFrame", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(datumType, "DynamicGeodeticReferenceFrame", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON datum type '{datumType}' is not supported for geodetic CRS.");
        }

        string name = GetRequiredString(element, "name");
        Ellipsoid ellipsoid = ReadEllipsoid(GetRequiredProperty(element, "ellipsoid"));
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new HorizontalDatum(ellipsoid, null, DatumType.HD_Geocentric, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static HorizontalDatum ReadHorizontalDatumEnsemble(JsonElement element)
    {
        DatumEnsemble ensemble = ReadDatumEnsemble(element, requireEllipsoid: true, "geodetic CRS");
        Ellipsoid ellipsoid = ArgumentGuard.ThrowIfNull(ensemble.Ellipsoid, nameof(ensemble));
        return new HorizontalDatum(ellipsoid, null, DatumType.HD_Geocentric, ensemble.Name, ensemble.Authority, ensemble.AuthorityCode, string.Empty, string.Empty, string.Empty)
        {
            Ensemble = ensemble,
        };
    }

    private static VerticalDatum ReadVerticalDatum(JsonElement element)
    {
        string datumType = GetOptionalString(element, "type") ?? "VerticalReferenceFrame";
        if (!string.Equals(datumType, "VerticalReferenceFrame", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(datumType, "DynamicVerticalReferenceFrame", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON datum type '{datumType}' is not supported for vertical CRS.");
        }

        string name = GetRequiredString(element, "name");
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new VerticalDatum(DatumType.VD_GeoidModelDerived, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static VerticalDatum ReadVerticalDatumEnsemble(JsonElement element)
    {
        DatumEnsemble ensemble = ReadDatumEnsemble(element, requireEllipsoid: false, "vertical CRS");
        return new VerticalDatum(DatumType.VD_GeoidModelDerived, ensemble.Name, ensemble.Authority, ensemble.AuthorityCode, string.Empty, string.Empty, string.Empty)
        {
            Ensemble = ensemble,
        };
    }

    private static VerticalDatum ApplyVerticalDatumTypeForAxis(VerticalDatum verticalDatum, AxisInfo axisInfo)
    {
        axisInfo = ArgumentGuard.ThrowIfNull(axisInfo, nameof(axisInfo));

        DatumType datumType = axisInfo.Orientation == AxisOrientationEnum.Down
            ? DatumType.VD_Depth
            : DatumType.VD_GeoidModelDerived;
        if (verticalDatum.DatumType == datumType)
        {
            return verticalDatum;
        }

        return new VerticalDatum(
            datumType,
            verticalDatum.Name,
            verticalDatum.Authority,
            verticalDatum.AuthorityCode,
            verticalDatum.Alias,
            verticalDatum.Remarks,
            verticalDatum.Abbreviation)
        {
            Ensemble = verticalDatum.Ensemble,
        };
    }

    private static DatumEnsemble ReadDatumEnsemble(JsonElement element, bool requireEllipsoid, string context)
    {
        string datumType = GetOptionalString(element, "type") ?? "DatumEnsemble";
        if (!string.Equals(datumType, "DatumEnsemble", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON datum ensemble type '{datumType}' is not supported for {context}.");
        }

        string name = GetRequiredString(element, "name");
        JsonElement membersElement = GetRequiredProperty(element, "members");
        if (membersElement.ValueKind != JsonValueKind.Array)
        {
            ArgumentGuard.ThrowArgument("PROJJSON datum_ensemble members must be an array.");
        }

        var members = new List<DatumEnsembleMember>();
        foreach (JsonElement memberElement in membersElement.EnumerateArray())
        {
            members.Add(ReadDatumEnsembleMember(memberElement));
        }

        Ellipsoid? ellipsoid = element.TryGetProperty("ellipsoid", out JsonElement ellipsoidElement)
            ? ReadEllipsoid(ellipsoidElement)
            : null;
        if (requireEllipsoid && ellipsoid is null)
        {
            ArgumentGuard.ThrowArgument("PROJJSON datum_ensemble for geodetic CRS is missing an ellipsoid.");
        }

        string accuracyToken = GetRequiredString(element, "accuracy");
        if (!double.TryParse(accuracyToken, NumberStyles.Any, CultureInfo.InvariantCulture, out double accuracy))
        {
            ArgumentGuard.ThrowArgument($"Invalid PROJJSON datum_ensemble accuracy '{accuracyToken}'.");
        }

        ArgumentGuard.ThrowIfNotFinite(accuracy, nameof(element), "PROJJSON datum_ensemble accuracy must be finite.");
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new DatumEnsemble(name, members, accuracy, ellipsoid, authority, authorityCode);
    }

    private static DatumEnsembleMember ReadDatumEnsembleMember(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new DatumEnsembleMember(name, authority, authorityCode);
    }

    private static Ellipsoid ReadEllipsoid(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        double semiMajorAxis = GetRequiredDouble(element, "semi_major_axis");
        bool hasInverseFlattening = element.TryGetProperty("inverse_flattening", out JsonElement inverseFlatteningElement);
        bool hasSemiMinorAxis = element.TryGetProperty("semi_minor_axis", out JsonElement semiMinorAxisElement);
        if (!hasInverseFlattening && !hasSemiMinorAxis)
        {
            ArgumentGuard.ThrowArgument("PROJJSON ellipsoid requires either inverse_flattening or semi_minor_axis.");
        }

        LinearUnit axisUnit = element.TryGetProperty("unit", out JsonElement unitElement)
            ? ReadLinearUnit(unitElement)
            : LinearUnit.Metre;
        double inverseFlattening = hasInverseFlattening ? GetDouble(inverseFlatteningElement) : 0d;
        double semiMinorAxis = hasSemiMinorAxis ? GetDouble(semiMinorAxisElement) : 0d;
        bool isIvfDefinitive = hasInverseFlattening;

        ReadIdentifier(element, out string authority, out long authorityCode);
        return new Ellipsoid(
            semiMajorAxis,
            semiMinorAxis,
            inverseFlattening,
            isIvfDefinitive,
            axisUnit,
            name,
            authority,
            authorityCode,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    private static PrimeMeridian ReadPrimeMeridian(JsonElement element)
    {
        string name = GetRequiredString(element, "name");
        double longitude = GetRequiredDouble(element, "longitude");
        AngularUnit angularUnit = element.TryGetProperty("unit", out JsonElement unitElement)
            ? ReadAngularUnit(unitElement)
            : AngularUnit.Degrees;

        ReadIdentifier(element, out string authority, out long authorityCode);
        return new PrimeMeridian(longitude, angularUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static AngularUnit ReadAngularUnit(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            string unitName = element.GetString() ?? string.Empty;
            return unitName.ToUpperInvariant() switch
            {
                "DEGREE" => AngularUnit.Degrees,
                _ => throw new NotSupportedException($"PROJJSON angular unit '{unitName}' is not supported."),
            };
        }

        string type = GetOptionalString(element, "type") ?? "AngularUnit";
        if (!string.Equals(type, "AngularUnit", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(type, "Unit", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON angular unit type '{type}' is not supported.");
        }

        string name = GetRequiredString(element, "name");
        double radiansPerUnit = GetRequiredDouble(element, "conversion_factor");
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new AngularUnit(radiansPerUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static LinearUnit ReadLinearUnit(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            string unitName = element.GetString() ?? string.Empty;
            return unitName.ToUpperInvariant() switch
            {
                "METRE" or "METER" => LinearUnit.Metre,
                _ => throw new NotSupportedException($"PROJJSON linear unit '{unitName}' is not supported."),
            };
        }

        string type = GetOptionalString(element, "type") ?? "LinearUnit";
        if (!string.Equals(type, "LinearUnit", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(type, "Unit", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"PROJJSON linear unit type '{type}' is not supported.");
        }

        string name = GetRequiredString(element, "name");
        double metersPerUnit = GetRequiredDouble(element, "conversion_factor");
        ReadIdentifier(element, out string authority, out long authorityCode);
        return new LinearUnit(metersPerUnit, name, authority, authorityCode, string.Empty, string.Empty, string.Empty);
    }

    private static void ReadIdentifier(JsonElement element, out string authority, out long authorityCode)
    {
        authority = string.Empty;
        authorityCode = -1;

        if (element.TryGetProperty("id", out JsonElement idElement))
        {
            ReadSingleIdentifier(idElement, out authority, out authorityCode);
            return;
        }

        if (!element.TryGetProperty("ids", out JsonElement idsElement) || idsElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        JsonElement? selectedIdentifier = null;
        foreach (JsonElement candidate in idsElement.EnumerateArray())
        {
            selectedIdentifier ??= candidate;
            string? candidateAuthority = GetOptionalString(candidate, "authority");
            if (string.Equals(candidateAuthority, "EPSG", StringComparison.OrdinalIgnoreCase))
            {
                selectedIdentifier = candidate;
                break;
            }
        }

        if (selectedIdentifier.HasValue)
        {
            ReadSingleIdentifier(selectedIdentifier.Value, out authority, out authorityCode);
        }
    }

    private static void ReadSingleIdentifier(JsonElement element, out string authority, out long authorityCode)
    {
        authority = GetRequiredString(element, "authority");
        JsonElement codeElement = GetRequiredProperty(element, "code");
        authorityCode = codeElement.ValueKind switch
        {
            JsonValueKind.Number => codeElement.GetInt64(),
            JsonValueKind.String => long.TryParse(codeElement.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out long parsedCode)
                ? parsedCode
                : -1,
            _ => -1,
        };
    }

    private static string NormalizeProjectionParameterName(string parameterName) => ProjectionParameterNameNormalizer.Normalize(parameterName);

    private static AxisOrientationEnum ParseAxisOrientation(string orientationToken)
    {
        return orientationToken.ToUpperInvariant() switch
        {
            "NORTH" => AxisOrientationEnum.North,
            "SOUTH" => AxisOrientationEnum.South,
            "EAST" => AxisOrientationEnum.East,
            "WEST" => AxisOrientationEnum.West,
            "UP" => AxisOrientationEnum.Up,
            "DOWN" => AxisOrientationEnum.Down,
            "GEOCENTRICX" => AxisOrientationEnum.Other,
            "GEOCENTRICY" => AxisOrientationEnum.East,
            "GEOCENTRICZ" => AxisOrientationEnum.North,
            _ => ArgumentGuard.ThrowArgument<AxisOrientationEnum>($"Invalid PROJJSON axis orientation '{orientationToken}'."),
        };
    }

    private static AngularUnit? MergeAngularUnit(AngularUnit? current, AngularUnit? candidate)
    {
        if (current is null)
        {
            return candidate;
        }

        if (candidate is null)
        {
            return current;
        }

        if (!current.EqualParams(candidate))
        {
            ArgumentGuard.ThrowArgument("PROJJSON axis angular units must match.");
        }

        return current;
    }

    private static LinearUnit? MergeLinearUnit(LinearUnit? current, LinearUnit? candidate)
    {
        if (current is null)
        {
            return candidate;
        }

        if (candidate is null)
        {
            return current;
        }

        if (!current.EqualParams(candidate))
        {
            ArgumentGuard.ThrowArgument("PROJJSON axis linear units must match.");
        }

        return current;
    }

    private static bool IsAngularUnit(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return string.Equals(element.GetString(), "degree", StringComparison.OrdinalIgnoreCase);
        }

        string type = GetOptionalString(element, "type") ?? string.Empty;
        return string.Equals(type, "AngularUnit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "Unit", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLinearUnit(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return string.Equals(element.GetString(), "metre", StringComparison.OrdinalIgnoreCase)
                || string.Equals(element.GetString(), "meter", StringComparison.OrdinalIgnoreCase);
        }

        string type = GetOptionalString(element, "type") ?? string.Empty;
        return string.Equals(type, "LinearUnit", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "Unit", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        JsonElement property = GetRequiredProperty(element, propertyName);
        if (property.ValueKind != JsonValueKind.String)
        {
            ArgumentGuard.ThrowArgument($"PROJJSON property '{propertyName}' must be a string.");
        }

        return property.GetString() ?? string.Empty;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static double GetRequiredDouble(JsonElement element, string propertyName)
    {
        return GetDouble(GetRequiredProperty(element, propertyName));
    }

    private static double GetDouble(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number)
        {
            ArgumentGuard.ThrowArgument("PROJJSON numeric property must be a number.");
        }

        return element.GetDouble();
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            ArgumentGuard.ThrowArgument($"PROJJSON property '{propertyName}' is required.");
        }

        return property;
    }
}
