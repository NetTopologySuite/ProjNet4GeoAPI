// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjNet.CoordinateSystems;

/// <summary>
/// Implements PROJ's <c>defmodel</c> runtime transform.
/// </summary>
internal sealed partial class DefModelMathTransform : MathTransform
{
    private const int MaximumModelSizeInBytes = 10 * 1024 * 1024;
    private const double InverseHorizontalTolerance = 1e-12d;
    private const double InverseVerticalTolerance = 1e-3d;

    private readonly string modelPath;
    private readonly double semiMajor;
    private readonly double semiMinor;
    private readonly double eccentricitySquared;
    private readonly bool isGeographicCrs;
    private readonly bool isHorizontalUnitDegree;
    private readonly bool isAddition;
    private readonly SpatialExtent globalExtent;
    private readonly TimeExtent timeExtent;
    private readonly ComponentRuntime[] components;
    private readonly GeocentricTransform geocentricForward;
    private readonly GeocentricTransform geocentricInverse;

    private readonly bool isInverted;
    private MathTransform? inverse;

    private DefModelMathTransform(
        string modelPath,
        ModelDefinition model,
        double semiMajor,
        double semiMinor,
        bool isGeographicCrs,
        bool isInverted)
    {
        this.modelPath = modelPath;
        this.semiMajor = semiMajor;
        this.semiMinor = semiMinor;
        this.eccentricitySquared = 1d - ((semiMinor * semiMinor) / (semiMajor * semiMajor));
        this.isGeographicCrs = isGeographicCrs;
        this.isHorizontalUnitDegree = string.Equals(model.HorizontalOffsetUnit, "DEGREE", StringComparison.Ordinal);
        this.isAddition = string.Equals(model.HorizontalOffsetMethod, "ADDITION", StringComparison.Ordinal);
        this.globalExtent = model.Extent;
        this.timeExtent = model.TimeExtent;
        this.components = LoadComponents(model, modelPath, this.isHorizontalUnitDegree);
        this.isInverted = isInverted;

        var parameters = new List<ProjectionParameter>
        {
            new("semi_major", semiMajor),
            new("semi_minor", semiMinor),
        };
        this.geocentricForward = new GeocentricTransform(parameters, false);
        this.geocentricInverse = (GeocentricTransform)this.geocentricForward.Inverse();

        ValidateCompatibility(model, this.isGeographicCrs, this.isHorizontalUnitDegree, this.isAddition, nameof(model));
    }

    private DefModelMathTransform(DefModelMathTransform source, bool isInverted)
    {
        this.modelPath = source.modelPath;
        this.semiMajor = source.semiMajor;
        this.semiMinor = source.semiMinor;
        this.eccentricitySquared = source.eccentricitySquared;
        this.isGeographicCrs = source.isGeographicCrs;
        this.isHorizontalUnitDegree = source.isHorizontalUnitDegree;
        this.isAddition = source.isAddition;
        this.globalExtent = source.globalExtent;
        this.timeExtent = source.timeExtent;
        this.components = source.components;
        this.geocentricForward = source.geocentricForward;
        this.geocentricInverse = source.geocentricInverse;
        this.isInverted = isInverted;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override bool Identity() => false;

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.inverse ??= new DefModelMathTransform(this, !this.isInverted);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("DefModelMathTransform is immutable. Use Inverse() to obtain inverted transform.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        throw new NotSupportedException("defmodel requires observation time (4D input).");
    }

    /// <summary>
    /// Creates a <see cref="DefModelMathTransform"/> from parsed PROJ arguments.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreate(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (args is null)
        {
            skipReason = "defmodel arguments were null.";
            return false;
        }

        if (!args.TryGetValue("model", out string? modelToken) || string.IsNullOrWhiteSpace(modelToken))
        {
            skipReason = "defmodel requires +model.";
            return false;
        }

        if (!TryResolveModelPath(modelToken, out string? resolvedModelPathCandidate))
        {
            skipReason = $"Cannot open {modelToken}.";
            return false;
        }

        string resolvedModelPath = ArgumentGuard.ThrowIfNull(resolvedModelPathCandidate, nameof(resolvedModelPathCandidate));

        if (!ProjEllipsoidResolver.TryResolveEllipsoidOrDefault(
            args,
            operationName: "defmodel",
            allowClarke1880Ign: true,
            allowBessel: false,
            out double semiMajor,
            out double semiMinor,
            out skipReason))
        {
            return false;
        }

        string jsonText;
        try
        {
            var fileInfo = new FileInfo(resolvedModelPath);
            if (!fileInfo.Exists)
            {
                skipReason = $"Cannot open {modelToken}.";
                return false;
            }

            if (fileInfo.Length > MaximumModelSizeInBytes)
            {
                skipReason = $"File {modelToken} is too large.";
                return false;
            }

            jsonText = File.ReadAllText(resolvedModelPath);
        }
        catch (IOException exception)
        {
            skipReason = $"Cannot read {modelToken}: {exception.Message}";
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            skipReason = $"Cannot read {modelToken}: {exception.Message}";
            return false;
        }

        try
        {
            ModelDefinition model = ParseModel(jsonText);
            bool isGeographicCrs = IsDefinitionCrsGeographic(model.DefinitionCrs);
            transform = new DefModelMathTransform(resolvedModelPath, model, semiMajor, semiMinor, isGeographicCrs, false);
            if (args.ContainsKey("inv"))
            {
                transform = transform.Inverse();
            }

            return true;
        }
        catch (FormatException exception)
        {
            skipReason = $"invalid model: {exception.Message}";
            return false;
        }
        catch (JsonException exception)
        {
            skipReason = $"invalid model: {exception.Message}";
            return false;
        }
        catch (InvalidDataException exception)
        {
            skipReason = $"invalid model: {exception.Message}";
            return false;
        }
        catch (ArgumentException exception)
        {
            skipReason = $"invalid model: {exception.Message}";
            return false;
        }
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        if (!TransformationMath.IsValidObservationEpoch(t, TransformationMath.MissingObservationEpoch))
        {
            ArgumentGuard.ThrowArgument("defmodel requires a valid observation epoch.", nameof(t));
        }

        if (!this.isInverted)
        {
            if (!this.TryForward(x, y, z, t, false, out double xOut, out double yOut, out double zOut))
            {
                TransformationThrowHelper.ThrowInvalidOperation("defmodel forward transformation failed.");
            }

            x = xOut;
            y = yOut;
            z = zOut;
            return;
        }

        if (!this.TryInverse(x, y, z, t, out double xInv, out double yInv, out double zInv))
        {
            TransformationThrowHelper.ThrowInvalidOperation("defmodel inverse transformation failed.");
        }

        x = xInv;
        y = yInv;
        z = zInv;
    }

    private static bool TryResolveModelPath(string modelToken, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(modelToken))
        {
            return false;
        }

        string normalized = NormalizePathToken(modelToken);
        if (TryGetExistingPath(normalized, out resolvedPath))
        {
            return true;
        }

        string appBaseCandidate = Path.Combine(AppContext.BaseDirectory, normalized);
        if (TryGetExistingPath(appBaseCandidate, out resolvedPath))
        {
            return true;
        }

        if (CoordinateTransformationFactory.TryResolveGridResourcePath(modelToken, out resolvedPath))
        {
            return true;
        }

        if (!string.Equals(normalized, modelToken, StringComparison.Ordinal)
            && CoordinateTransformationFactory.TryResolveGridResourcePath(normalized, out resolvedPath))
        {
            return true;
        }

        string fileName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            string fixtureCandidate = Path.Combine(AppContext.BaseDirectory, "Fixtures", "defmodel", fileName);
            if (TryGetExistingPath(fixtureCandidate, out resolvedPath))
            {
                return true;
            }

            if (CoordinateTransformationFactory.TryResolveGridResourcePath(fileName, out resolvedPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetExistingPath(string candidate, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (!Path.IsPathRooted(candidate))
        {
            candidate = Path.GetFullPath(candidate);
        }

        if (!File.Exists(candidate))
        {
            return false;
        }

        resolvedPath = candidate;
        return true;
    }

    private static string NormalizePathToken(string token)
    {
        return token.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }

    private static bool IsDefinitionCrsGeographic(string definitionCrs)
    {
        if (!TryParseEpsgCode(definitionCrs, out int epsgCode))
        {
            return true;
        }

        var services = new CoordinateSystemServices();
        return !services.TryGetCoordinateSystem(epsgCode, out CoordinateSystem? coordinateSystem) || coordinateSystem is GeographicCoordinateSystem;
    }

    private static bool TryParseEpsgCode(string crsToken, out int epsgCode)
    {
        epsgCode = 0;
        if (string.IsNullOrWhiteSpace(crsToken))
        {
            return false;
        }

        const string prefix = "EPSG:";
        if (!crsToken.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string suffix = crsToken[prefix.Length..].Trim();
        return int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out epsgCode)
            && epsgCode > 0;
    }

    private static ModelDefinition ParseModel(string jsonText)
    {
        using var document = JsonDocument.Parse(jsonText);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Model root must be a JSON object.");
        }

        var model = new ModelDefinition
        {
            FileType = GetRequiredString(root, "file_type"),
            FormatVersion = GetRequiredString(root, "format_version"),
            SourceCrs = GetRequiredString(root, "source_crs"),
            TargetCrs = GetRequiredString(root, "target_crs"),
            DefinitionCrs = GetRequiredString(root, "definition_crs"),
            HorizontalOffsetUnit = NormalizeOptionalString(GetOptionalString(root, "horizontal_offset_unit")),
            VerticalOffsetUnit = NormalizeOptionalString(GetOptionalString(root, "vertical_offset_unit")),
            HorizontalOffsetMethod = NormalizeOptionalString(GetOptionalString(root, "horizontal_offset_method")),
            Extent = ParseSpatialExtent(GetRequiredObject(root, "extent"), "extent"),
            TimeExtent = ParseTimeExtent(GetRequiredObject(root, "time_extent")),
            Components = ParseComponents(GetRequiredArray(root, "components")),
        };

        if (!string.Equals(model.SourceCrs, model.DefinitionCrs, StringComparison.Ordinal))
        {
            throw new FormatException("source_crs != definition_crs is not currently supported.");
        }

        ValidateModelMetadata(model);
        return model;
    }

    private static void ValidateModelMetadata(ModelDefinition model)
    {
        if (!string.IsNullOrEmpty(model.HorizontalOffsetUnit)
            && !string.Equals(model.HorizontalOffsetUnit, "METRE", StringComparison.Ordinal)
            && !string.Equals(model.HorizontalOffsetUnit, "DEGREE", StringComparison.Ordinal))
        {
            throw new FormatException("Unsupported value for horizontal_offset_unit.");
        }

        if (!string.IsNullOrEmpty(model.VerticalOffsetUnit)
            && !string.Equals(model.VerticalOffsetUnit, "METRE", StringComparison.Ordinal))
        {
            throw new FormatException("Unsupported value for vertical_offset_unit.");
        }

        if (!string.IsNullOrEmpty(model.HorizontalOffsetMethod)
            && !string.Equals(model.HorizontalOffsetMethod, "ADDITION", StringComparison.Ordinal)
            && !string.Equals(model.HorizontalOffsetMethod, "GEOCENTRIC", StringComparison.Ordinal))
        {
            throw new FormatException("Unsupported value for horizontal_offset_method.");
        }

        for (int i = 0; i < model.Components.Length; i++)
        {
            ComponentDefinition component = model.Components[i];
            if (component.DisplacementType == DisplacementType.Horizontal
                || component.DisplacementType == DisplacementType.ThreeDimensional)
            {
                if (string.IsNullOrEmpty(model.HorizontalOffsetUnit))
                {
                    throw new FormatException("horizontal_offset_unit must be defined for horizontal/3d components.");
                }

                if (string.IsNullOrEmpty(model.HorizontalOffsetMethod))
                {
                    throw new FormatException("horizontal_offset_method must be defined for horizontal/3d components.");
                }
            }

            if (component.DisplacementType == DisplacementType.Vertical
                || component.DisplacementType == DisplacementType.ThreeDimensional)
            {
                if (string.IsNullOrEmpty(model.VerticalOffsetUnit))
                {
                    throw new FormatException("vertical_offset_unit must be defined for vertical/3d components.");
                }
            }

            if (string.Equals(model.HorizontalOffsetUnit, "DEGREE", StringComparison.Ordinal)
                && component.InterpolationMethod != InterpolationMethod.Bilinear)
            {
                throw new FormatException("horizontal_offset_unit = degree requires interpolation_method = bilinear.");
            }
        }

        if (string.Equals(model.HorizontalOffsetUnit, "DEGREE", StringComparison.Ordinal)
            && !string.Equals(model.HorizontalOffsetMethod, "ADDITION", StringComparison.Ordinal))
        {
            throw new FormatException("horizontal_offset_unit = degree requires horizontal_offset_method = addition.");
        }
    }

    private static ComponentDefinition[] ParseComponents(JsonElement componentsArray)
    {
        var components = new List<ComponentDefinition>(componentsArray.GetArrayLength());
        int index = 0;
        foreach (JsonElement componentElement in componentsArray.EnumerateArray())
        {
            if (componentElement.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException($"components[{index.ToString(CultureInfo.InvariantCulture)}] must be an object.");
            }

            string displacementTypeToken = NormalizeOptionalString(GetRequiredString(componentElement, "displacement_type"));
            DisplacementType displacementType = ParseDisplacementType(displacementTypeToken);
            InterpolationMethod interpolationMethod = ParseInterpolationMethod(
                NormalizeOptionalString(GetRequiredString(GetRequiredObject(componentElement, "spatial_model"), "interpolation_method")));

            JsonElement timeFunctionObject = GetRequiredObject(componentElement, "time_function");
            string timeFunctionType = NormalizeOptionalString(GetRequiredString(timeFunctionObject, "type"));
            ITimeFunction timeFunction = ParseTimeFunction(timeFunctionType, timeFunctionObject);

            var component = new ComponentDefinition
            {
                Description = GetOptionalString(componentElement, "description"),
                DisplacementType = displacementType,
                InterpolationMethod = interpolationMethod,
                Extent = ParseSpatialExtent(GetRequiredObject(componentElement, "extent"), $"components[{index.ToString(CultureInfo.InvariantCulture)}].extent"),
                SpatialModelFileName = GetRequiredString(GetRequiredObject(componentElement, "spatial_model"), "filename"),
                TimeFunction = timeFunction,
            };
            components.Add(component);
            index++;
        }

        return [.. components];
    }

    private static DisplacementType ParseDisplacementType(string token)
    {
        return token switch
        {
            "NONE" => DisplacementType.None,
            "HORIZONTAL" => DisplacementType.Horizontal,
            "VERTICAL" => DisplacementType.Vertical,
            "3D" => DisplacementType.ThreeDimensional,
            _ => throw new FormatException("Unsupported value for displacement_type."),
        };
    }

    private static InterpolationMethod ParseInterpolationMethod(string token)
    {
        return token switch
        {
            "BILINEAR" => InterpolationMethod.Bilinear,
            "GEOCENTRIC_BILINEAR" => InterpolationMethod.GeocentricBilinear,
            _ => throw new FormatException("Unsupported value for interpolation_method."),
        };
    }

    private static ITimeFunction ParseTimeFunction(string timeFunctionType, JsonElement timeFunctionObject)
    {
        if (timeFunctionType == "CONSTANT")
        {
            return ConstantTimeFunction.Instance;
        }

        JsonElement parameters = GetRequiredObject(timeFunctionObject, "parameters");
        if (timeFunctionType == "VELOCITY")
        {
            return new VelocityTimeFunction(ParseIso8601ToDecimalYear(GetRequiredString(parameters, "reference_epoch")));
        }

        if (timeFunctionType == "STEP")
        {
            return new StepTimeFunction(ParseIso8601ToDecimalYear(GetRequiredString(parameters, "step_epoch")));
        }

        if (timeFunctionType == "REVERSE_STEP")
        {
            return new ReverseStepTimeFunction(ParseIso8601ToDecimalYear(GetRequiredString(parameters, "step_epoch")));
        }

        if (timeFunctionType == "PIECEWISE")
        {
            string beforeFirst = NormalizeOptionalString(GetRequiredString(parameters, "before_first"));
            if (beforeFirst != "ZERO" && beforeFirst != "CONSTANT" && beforeFirst != "LINEAR")
            {
                throw new FormatException("Unsupported value for before_first.");
            }

            string afterLast = NormalizeOptionalString(GetRequiredString(parameters, "after_last"));
            if (afterLast != "ZERO" && afterLast != "CONSTANT" && afterLast != "LINEAR")
            {
                throw new FormatException("Unsupported value for after_last.");
            }

            JsonElement modelArray = GetRequiredArray(parameters, "model");
            var tuples = new List<PiecewiseTimeFunction.EpochScaleTuple>(modelArray.GetArrayLength());
            foreach (JsonElement tupleElement in modelArray.EnumerateArray())
            {
                if (tupleElement.ValueKind != JsonValueKind.Object)
                {
                    throw new FormatException("piecewise model element must be an object.");
                }

                tuples.Add(new PiecewiseTimeFunction.EpochScaleTuple(
                    ParseIso8601ToDecimalYear(GetRequiredString(tupleElement, "epoch")),
                    GetRequiredDouble(tupleElement, "scale_factor")));
            }

            return new PiecewiseTimeFunction(beforeFirst, afterLast, [.. tuples]);
        }

        if (timeFunctionType == "EXPONENTIAL")
        {
            double referenceEpoch = ParseIso8601ToDecimalYear(GetRequiredString(parameters, "reference_epoch"));
            string endEpochString = GetOptionalString(parameters, "end_epoch");
            double? endEpoch = string.IsNullOrWhiteSpace(endEpochString)
                ? null
                : ParseIso8601ToDecimalYear(endEpochString);
            double relaxationConstant = GetRequiredDouble(parameters, "relaxation_constant");
            return relaxationConstant <= 0d
                ? throw new FormatException("Invalid value for relaxation_constant.")
                : (ITimeFunction)new ExponentialTimeFunction(
                referenceEpoch,
                endEpoch,
                relaxationConstant,
                GetRequiredDouble(parameters, "before_scale_factor"),
                GetRequiredDouble(parameters, "initial_scale_factor"),
                GetRequiredDouble(parameters, "final_scale_factor"));
        }

        throw new FormatException($"Unsupported type of time function: {timeFunctionType}.");
    }

    private static TimeExtent ParseTimeExtent(JsonElement timeExtentObject)
    {
        double first = ParseIso8601ToDecimalYear(GetRequiredString(timeExtentObject, "first"));
        double last = ParseIso8601ToDecimalYear(GetRequiredString(timeExtentObject, "last"));
        return last < first
            ? throw new FormatException("time_extent.last must be greater than or equal to time_extent.first.")
            : new TimeExtent(first, last);
    }

    private static SpatialExtent ParseSpatialExtent(JsonElement extentObject, string context)
    {
        string type = NormalizeOptionalString(GetRequiredString(extentObject, "type"));
        if (type != "BBOX")
        {
            throw new FormatException($"{context} only supports type=bbox.");
        }

        JsonElement parametersObject = GetRequiredObject(extentObject, "parameters");
        JsonElement bboxArray = GetRequiredArray(parametersObject, "bbox");
        if (bboxArray.GetArrayLength() != 4)
        {
            throw new FormatException($"{context}.parameters.bbox must contain exactly 4 numeric values.");
        }

        double minX = GetArrayDouble(bboxArray, 0, context);
        double minY = GetArrayDouble(bboxArray, 1, context);
        double maxX = GetArrayDouble(bboxArray, 2, context);
        double maxY = GetArrayDouble(bboxArray, 3, context);
        return maxX < minX || maxY < minY
            ? throw new FormatException($"{context}.parameters.bbox has invalid ordering.")
            : new SpatialExtent(minX, minY, maxX, maxY);
    }

    private static double GetArrayDouble(JsonElement arrayElement, int index, string context)
    {
        JsonElement value = arrayElement[index];
        return value.ValueKind != JsonValueKind.Number
            ? throw new FormatException($"{context}.parameters.bbox contains a non-numeric value.")
            : value.GetDouble();
    }

    private static JsonElement GetRequiredObject(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException($"Missing \"{propertyName}\" key.");
        }

        return value.ValueKind != JsonValueKind.Object ? throw new FormatException($"\"{propertyName}\" must be an object.") : value;
    }

    private static JsonElement GetRequiredArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException($"Missing \"{propertyName}\" key.");
        }

        return value.ValueKind != JsonValueKind.Array ? throw new FormatException($"\"{propertyName}\" must be an array.") : value;
    }

    private static string GetRequiredString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException($"Missing \"{propertyName}\" key.");
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"\"{propertyName}\" must be a string.");
        }

        string? text = value.GetString();
        return text ?? throw new FormatException($"\"{propertyName}\" must not be null.");
    }

    private static string GetOptionalString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            return string.Empty;
        }

        return value.ValueKind != JsonValueKind.String
            ? throw new FormatException($"\"{propertyName}\" must be a string.")
            : value.GetString() ?? string.Empty;
    }

    private static double GetRequiredDouble(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException($"Missing \"{propertyName}\" key.");
        }

        return value.ValueKind != JsonValueKind.Number
            ? throw new FormatException($"\"{propertyName}\" must be numeric.")
            : value.GetDouble();
    }

    private static string NormalizeOptionalString(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().ToUpperInvariant();
    }

    private static double ParseIso8601ToDecimalYear(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"Wrong formatting / invalid date-time for {value}.");
        }

        if (value.Length != 20
            || value[4] != '-'
            || value[7] != '-'
            || value[10] != 'T'
            || value[13] != ':'
            || value[16] != ':'
            || value[19] != 'Z')
        {
            throw new FormatException($"Wrong formatting / invalid date-time for {value}.");
        }

        int year = ParseEpochPart(value, 0, 4);
        int month = ParseEpochPart(value, 5, 2);
        int day = ParseEpochPart(value, 8, 2);
        int hour = ParseEpochPart(value, 11, 2);
        int minute = ParseEpochPart(value, 14, 2);
        int second = ParseEpochPart(value, 17, 2);

        if (year < 1582 || month < 1 || month > 12 || day < 1 || hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second >= 61)
        {
            throw new FormatException($"Wrong formatting / invalid date-time for {value}.");
        }

        bool isLeapYear = ((year % 4) == 0 && (year % 100) != 0) || ((year % 400) == 0);
        int[] monthLengths = isLeapYear
            ? [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
            : [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        if (day > monthLengths[month - 1])
        {
            throw new FormatException($"Wrong formatting / invalid date-time for {value}.");
        }

        int dayInYear = day - 1;
        for (int monthIndex = 1; monthIndex < month; monthIndex++)
        {
            dayInYear += monthLengths[monthIndex - 1];
        }

        double numerator = (dayInYear * 86400d) + (hour * 3600d) + (minute * 60d) + second;
        double denominator = (isLeapYear ? 366d : 365d) * 86400d;
        return year + (numerator / denominator);
    }

    private static int ParseEpochPart(string value, int startIndex, int length)
    {
#if NETSTANDARD2_0
        if (!int.TryParse(value.Substring(startIndex, length), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
#else
        if (!int.TryParse(value.AsSpan(startIndex, length), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
#endif
        {
            throw new FormatException($"Wrong formatting / invalid date-time for {value}.");
        }

        return parsed;
    }

    private static ComponentRuntime[] LoadComponents(ModelDefinition model, string modelPath, bool isHorizontalUnitDegree)
    {
        string modelDirectory = Path.GetDirectoryName(modelPath) ?? string.Empty;
        var loaded = new List<ComponentRuntime>(model.Components.Length);
        for (int i = 0; i < model.Components.Length; i++)
        {
            ComponentDefinition component = model.Components[i];
            if (component.DisplacementType == DisplacementType.None)
            {
                loaded.Add(new ComponentRuntime(component, null, null));
                continue;
            }

            if (!TryResolveComponentPath(component.SpatialModelFileName, modelDirectory, out string? componentPathCandidate))
            {
                throw new InvalidDataException($"Cannot resolve deformation model component grid '{component.SpatialModelFileName}'.");
            }

            string componentPath = ArgumentGuard.ThrowIfNull(componentPathCandidate, nameof(componentPathCandidate));

            if (component.DisplacementType == DisplacementType.Vertical)
            {
                IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> vertical = GeoTiffGridLoader.LoadVertical(componentPath);
                if (vertical.Count == 0)
                {
                    throw new InvalidDataException($"No vertical grid data found in '{component.SpatialModelFileName}'.");
                }

                loaded.Add(new ComponentRuntime(
                    component,
                    null,
                    [.. vertical.OrderBy(grid => grid.Area, Comparer<double>.Default)]));
                continue;
            }

            bool requireMetreUnits = !isHorizontalUnitDegree;
            IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> xyz = GeoTiffGridLoader.LoadXyz(componentPath, requireMetreUnits);
            if (xyz.Count == 0)
            {
                throw new InvalidDataException($"No XYZ grid data found in '{component.SpatialModelFileName}'.");
            }

            loaded.Add(new ComponentRuntime(
                component,
                [.. xyz.OrderBy(grid => grid.Area, Comparer<double>.Default)],
                null));
        }

        return [.. loaded];
    }

    private static bool TryResolveComponentPath(string fileName, string modelDirectory, [NotNullWhen(true)] out string? resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        string normalized = NormalizePathToken(fileName);
        if (TryGetExistingPath(normalized, out resolvedPath))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(modelDirectory))
        {
            string modelRelativeCandidate = Path.Combine(modelDirectory, normalized);
            if (TryGetExistingPath(modelRelativeCandidate, out resolvedPath))
            {
                return true;
            }
        }

        string appBaseCandidate = Path.Combine(AppContext.BaseDirectory, normalized);
        if (TryGetExistingPath(appBaseCandidate, out resolvedPath))
        {
            return true;
        }

        if (CoordinateTransformationFactory.TryResolveGridResourcePath(fileName, out resolvedPath))
        {
            return true;
        }

        if (!string.Equals(normalized, fileName, StringComparison.Ordinal)
            && CoordinateTransformationFactory.TryResolveGridResourcePath(normalized, out resolvedPath))
        {
            return true;
        }

        string baseName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(baseName))
        {
            if (!string.IsNullOrWhiteSpace(modelDirectory)
                && TryGetExistingPath(Path.Combine(modelDirectory, baseName), out resolvedPath))
            {
                return true;
            }

            string fixtureCandidate = Path.Combine(AppContext.BaseDirectory, "Fixtures", "defmodel", "tests", baseName);
            if (TryGetExistingPath(fixtureCandidate, out resolvedPath))
            {
                return true;
            }

            if (CoordinateTransformationFactory.TryResolveGridResourcePath(baseName, out resolvedPath))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateCompatibility(
        ModelDefinition model,
        bool isGeographicCrs,
        bool isHorizontalUnitDegree,
        bool isAddition,
        string modelParamName)
    {
        if (!isGeographicCrs && isHorizontalUnitDegree)
        {
            ArgumentGuard.ThrowArgument("definition_crs = projected CRS and horizontal_offset_unit = degree are incompatible.", modelParamName);
        }

        if (!isGeographicCrs && !isAddition)
        {
            ArgumentGuard.ThrowArgument("definition_crs = projected CRS and horizontal_offset_method = geocentric are incompatible.", modelParamName);
        }

        if (isGeographicCrs)
        {
            return;
        }

        for (int i = 0; i < model.Components.Length; i++)
        {
            if (model.Components[i].InterpolationMethod == InterpolationMethod.GeocentricBilinear)
            {
                ArgumentGuard.ThrowArgument("definition_crs = projected CRS and interpolation_method = geocentric_bilinear are incompatible.", modelParamName);
            }
        }
    }

    private static bool BboxCheck(
        ref double x,
        ref double y,
        bool forInverseComputation,
        SpatialExtent extent,
        double epsilon,
        double extraMarginForInverse)
    {
        if (x >= extent.MinX - epsilon
            && x <= extent.MaxX + epsilon
            && y >= extent.MinY - epsilon
            && y <= extent.MaxY + epsilon)
        {
            return true;
        }

        if (!forInverseComputation)
        {
            return false;
        }

        bool xOk = false;
        if (x >= extent.MinX - epsilon && x <= extent.MaxX + epsilon)
        {
            xOk = true;
        }
        else if (x > extent.MinX - extraMarginForInverse && x < extent.MinX)
        {
            x = extent.MinX;
            xOk = true;
        }
        else if (x < extent.MaxX + extraMarginForInverse && x > extent.MaxX)
        {
            x = extent.MaxX;
            xOk = true;
        }

        bool yOk = false;
        if (y >= extent.MinY - epsilon && y <= extent.MaxY + epsilon)
        {
            yOk = true;
        }
        else if (y > extent.MinY - extraMarginForInverse && y < extent.MinY)
        {
            y = extent.MinY;
            yOk = true;
        }
        else if (y < extent.MaxY + extraMarginForInverse && y > extent.MaxY)
        {
            y = extent.MaxY;
            yOk = true;
        }

        return xOk && yOk;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return value < minimum ? minimum : value > maximum ? maximum : value;
    }

    private static bool TryFindXyzGrid(
        IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> grids,
        double x,
        double y,
        [NotNullWhen(true)] out GeoTiffXyzGridShiftMathTransform.XyzGrid? grid)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            if (grids[i].Contains(x, y))
            {
                grid = grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private static bool TryFindVerticalGrid(
        IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> grids,
        double x,
        double y,
        [NotNullWhen(true)] out GeoTiffVGridShiftMathTransform.VerticalGrid? grid)
    {
        for (int i = 0; i < grids.Count; i++)
        {
            if (grids[i].Contains(x, y))
            {
                grid = grids[i];
                return true;
            }
        }

        grid = null;
        return false;
    }

    private static bool TryInterpolateVerticalShift(
        GeoTiffVGridShiftMathTransform.VerticalGrid grid,
        double x,
        double y,
        out double value)
    {
        value = 0d;
        if (!TryGetInterpolationCell(grid, x, y, out InterpolationCell cell))
        {
            return false;
        }

        double v00 = grid.GetValue(cell.X0, cell.Y0);
        double v01 = grid.GetValue(cell.X0, cell.Y1);
        double v10 = grid.GetValue(cell.X1, cell.Y0);
        double v11 = grid.GetValue(cell.X1, cell.Y1);

        bool nd00 = grid.IsNoData(v00);
        bool nd01 = grid.IsNoData(v01);
        bool nd10 = grid.IsNoData(v10);
        bool nd11 = grid.IsNoData(v11);
        if (!nd00 && !nd01 && !nd10 && !nd11)
        {
            value = Bilinear(v00, v01, v10, v11, cell.W00, cell.W01, cell.W10, cell.W11);
            return true;
        }

        double weightedSum = 0d;
        double weightSum = 0d;
        if (!nd00)
        {
            weightedSum += v00 * cell.W00;
            weightSum += cell.W00;
        }

        if (!nd01)
        {
            weightedSum += v01 * cell.W01;
            weightSum += cell.W01;
        }

        if (!nd10)
        {
            weightedSum += v10 * cell.W10;
            weightSum += cell.W10;
        }

        if (!nd11)
        {
            weightedSum += v11 * cell.W11;
            weightSum += cell.W11;
        }

        if (weightSum == 0d)
        {
            return false;
        }

        value = weightedSum / weightSum;
        return true;
    }

    private static bool TryGetInterpolationCell(
        BaseGeoGrid grid,
        double x,
        double y,
        out InterpolationCell cell)
    {
        cell = default;
        if (!grid.TryMapToGridCoordinates(x, y, out double gridX, out double gridY))
        {
            return false;
        }

        int x0 = (int)Math.Floor(gridX);
        int y0 = (int)Math.Floor(gridY);
        double fractionX = gridX - x0;
        double fractionY = gridY - y0;
        if (!TryNormalizeInterpolationCell(grid.Width, ref x0, ref fractionX)
            || !TryNormalizeInterpolationCell(grid.Height, ref y0, ref fractionY))
        {
            return false;
        }

        int x1 = x0 + 1;
        int y1 = y0 + 1;
        double xy = fractionX * fractionY;
        double w00 = 1d - fractionX - fractionY + xy;
        double w10 = fractionX - xy;
        double w01 = fractionY - xy;
        double w11 = xy;
        cell = new InterpolationCell(x0, y0, x1, y1, fractionX, fractionY, w00, w01, w10, w11);
        return true;
    }

    private static bool TryNormalizeInterpolationCell(int size, ref int index, ref double fraction)
    {
        if (index < 0)
        {
            if (index == -1 && fraction > 1d - (10d * 1e-5d))
            {
                index = 0;
                fraction = 0d;
                return true;
            }

            return false;
        }

        if (index + 1 < size)
        {
            return true;
        }

        if (index + 1 == size && fraction < 10d * 1e-5d)
        {
            index = size - 2;
            fraction = 1d;
            return true;
        }

        return false;
    }

    private static bool TryReadXyzCornerValues(
        GeoTiffXyzGridShiftMathTransform.XyzGrid grid,
        InterpolationCell cell,
        out XyzCornerValues values)
    {
        values = new XyzCornerValues(
            grid.GetXShift(cell.X0, cell.Y0),
            grid.GetXShift(cell.X0, cell.Y1),
            grid.GetXShift(cell.X1, cell.Y0),
            grid.GetXShift(cell.X1, cell.Y1),
            grid.GetYShift(cell.X0, cell.Y0),
            grid.GetYShift(cell.X0, cell.Y1),
            grid.GetYShift(cell.X1, cell.Y0),
            grid.GetYShift(cell.X1, cell.Y1),
            grid.GetZShift(cell.X0, cell.Y0),
            grid.GetZShift(cell.X0, cell.Y1),
            grid.GetZShift(cell.X1, cell.Y0),
            grid.GetZShift(cell.X1, cell.Y1));
        return TransformationMath.IsFinite(values.X00)
            && TransformationMath.IsFinite(values.X01)
            && TransformationMath.IsFinite(values.X10)
            && TransformationMath.IsFinite(values.X11)
            && TransformationMath.IsFinite(values.Y00)
            && TransformationMath.IsFinite(values.Y01)
            && TransformationMath.IsFinite(values.Y10)
            && TransformationMath.IsFinite(values.Y11)
            && TransformationMath.IsFinite(values.Z00)
            && TransformationMath.IsFinite(values.Z01)
            && TransformationMath.IsFinite(values.Z10)
            && TransformationMath.IsFinite(values.Z11);
    }

    private static bool TryInterpolateGeocentricBilinear(
        GeoTiffXyzGridShiftMathTransform.XyzGrid grid,
        InterpolationCell cell,
        XyzCornerValues values,
        double latitudeDegrees,
        out double eastOffset,
        out double northOffset)
    {
        eastOffset = 0d;
        northOffset = 0d;

        GetGridCoordinate(grid, cell.X0, cell.Y0, out double lon00, out double lat00);
        GetGridCoordinate(grid, cell.X1, cell.Y0, out double lon10, out _);
        GetGridCoordinate(grid, cell.X0, cell.Y1, out _, out double lat01);

        double resXDegrees = NormalizeLongitudeDelta(lon10 - lon00);
        if (Math.Abs(resXDegrees) < 1e-14d)
        {
            return false;
        }

        double halfResXRadians = DegreesToRadians(0.5d * resXDegrees);
        double sinHalfResX = Math.Sin(halfResXRadians);
        double cosHalfResX = Math.Cos(halfResXRadians);
        double phi0 = DegreesToRadians(lat00);
        double phi1 = DegreesToRadians(lat01);
        double sinPhi0 = Math.Sin(phi0);
        double cosPhi0 = Math.Cos(phi0);
        double sinPhi1 = Math.Sin(phi1);
        double cosPhi1 = Math.Cos(phi1);

        ConvertEnToGeocentricAtCorner(-sinHalfResX, cosHalfResX, sinPhi0, cosPhi0, values.X00, values.Y00, out double dX00, out double dY00, out double dZ00);
        ConvertEnToGeocentricAtCorner(-sinHalfResX, cosHalfResX, sinPhi1, cosPhi1, values.X01, values.Y01, out double dX01, out double dY01, out double dZ01);
        ConvertEnToGeocentricAtCorner(sinHalfResX, cosHalfResX, sinPhi0, cosPhi0, values.X10, values.Y10, out double dX10, out double dY10, out double dZ10);
        ConvertEnToGeocentricAtCorner(sinHalfResX, cosHalfResX, sinPhi1, cosPhi1, values.X11, values.Y11, out double dX11, out double dY11, out double dZ11);

        double dX = Bilinear(dX00, dX01, dX10, dX11, cell.W00, cell.W01, cell.W10, cell.W11);
        double dY = Bilinear(dY00, dY01, dY10, dY11, cell.W00, cell.W01, cell.W10, cell.W11);
        double dZ = Bilinear(dZ00, dZ01, dZ10, dZ11, cell.W00, cell.W01, cell.W10, cell.W11);

        double lambdaRelativeToCellCenter = (cell.FractionX - 0.5d) * DegreesToRadians(resXDegrees);
        double sinLambda = Math.Sin(lambdaRelativeToCellCenter);
        double cosLambda = Math.Cos(lambdaRelativeToCellCenter);
        double latitudeRadians = DegreesToRadians(latitudeDegrees);
        double sinLatitude = Math.Sin(latitudeRadians);
        double cosLatitude = Math.Cos(latitudeRadians);
        eastOffset = (-dX * sinLambda) + (dY * cosLambda);
        northOffset = (((-dX * cosLambda) - (dY * sinLambda)) * sinLatitude) + (dZ * cosLatitude);
        return true;
    }

    private static void ConvertEnToGeocentricAtCorner(
        double sinLambda,
        double cosLambda,
        double sinPhi,
        double cosPhi,
        double eastOffset,
        double northOffset,
        out double deltaX,
        out double deltaY,
        out double deltaZ)
    {
        double northOffsetSinPhi = northOffset * sinPhi;
        deltaX = (-eastOffset * sinLambda) - (northOffsetSinPhi * cosLambda);
        deltaY = (eastOffset * cosLambda) - (northOffsetSinPhi * sinLambda);
        deltaZ = northOffset * cosPhi;
    }

    private static double Bilinear(
        double value00,
        double value01,
        double value10,
        double value11,
        double weight00,
        double weight01,
        double weight10,
        double weight11)
    {
        return (value00 * weight00)
            + (value01 * weight01)
            + (value10 * weight10)
            + (value11 * weight11);
    }

    private static void GetGridCoordinate(BaseGeoGrid grid, int x, int y, out double longitude, out double latitude)
    {
        longitude = (grid.A * x) + (grid.B * y) + grid.C;
        latitude = (grid.D * x) + (grid.E * y) + grid.F;
    }

    private static double NormalizeLongitudeDelta(double deltaDegrees)
    {
        double delta = deltaDegrees;
        while (delta > 180d)
        {
            delta -= 360d;
        }

        while (delta < -180d)
        {
            delta += 360d;
        }

        return delta;
    }

    private static void DeltaEastingNorthingToLongLat(
        double cosPhi,
        double eastOffset,
        double northOffset,
        double semiMajor,
        double semiMinor,
        double eccentricitySquared,
        out double deltaLambdaRadians,
        out double deltaPhiRadians)
    {
        double oneMinusX = eccentricitySquared * (1d - (cosPhi * cosPhi));
        double x = 1d - oneMinusX;
        double sqrtX = Math.Sqrt(x);
        deltaLambdaRadians = eastOffset * sqrtX / (semiMajor * cosPhi);
        deltaPhiRadians = northOffset * semiMajor * sqrtX * x / (semiMinor * semiMinor);
    }

    private bool TryForward(
        double x,
        double y,
        double z,
        double observationEpoch,
        bool forInverseComputation,
        out double xOut,
        out double yOut,
        out double zOut)
    {
        xOut = x;
        yOut = y;
        zOut = double.IsNaN(z) ? 0d : z;
        double xWorking = x;
        double yWorking = y;

        double epsilon = this.isGeographicCrs ? 1e-10d : 1e-5d;
        double globalMinX = this.globalExtent.MinX;
        double globalMaxX = this.globalExtent.MaxX;
        if (this.isGeographicCrs)
        {
            while (xWorking < globalMinX - epsilon)
            {
                xWorking += 360d;
            }

            while (xWorking > globalMaxX + epsilon)
            {
                xWorking -= 360d;
            }
        }

        double globalExtraMargin = this.isGeographicCrs ? 0.1d : 10000d;
        if (!BboxCheck(ref xWorking, ref yWorking, forInverseComputation, this.globalExtent, epsilon, globalExtraMargin))
        {
            return false;
        }

        if (observationEpoch < this.timeExtent.First || observationEpoch > this.timeExtent.Last)
        {
            return false;
        }

        double longitudeOffsetDegrees = 0d;
        double latitudeOffsetDegrees = 0d;
        double eastingOffset = 0d;
        double northingOffset = 0d;
        double verticalOffset = 0d;

        for (int i = 0; i < this.components.Length; i++)
        {
            ComponentRuntime component = this.components[i];
            if (component.Definition.DisplacementType == DisplacementType.None)
            {
                continue;
            }

            double xForGrid = xWorking;
            double yForGrid = yWorking;
            if (!BboxCheck(ref xForGrid, ref yForGrid, forInverseComputation, component.Definition.Extent, epsilon, 0d))
            {
                continue;
            }

            xForGrid = Clamp(xForGrid, component.Definition.Extent.MinX, component.Definition.Extent.MaxX);
            yForGrid = Clamp(yForGrid, component.Definition.Extent.MinY, component.Definition.Extent.MaxY);

            double timeFactor = component.Definition.TimeFunction.Evaluate(observationEpoch);
            if (timeFactor == 0d)
            {
                continue;
            }

            if (component.Definition.DisplacementType == DisplacementType.Vertical)
            {
                if (!TryFindVerticalGrid(component.VerticalGrids, xForGrid, yForGrid, out GeoTiffVGridShiftMathTransform.VerticalGrid? verticalGridCandidate))
                {
                    continue;
                }

                GeoTiffVGridShiftMathTransform.VerticalGrid verticalGrid = ArgumentGuard.ThrowIfNull(verticalGridCandidate, nameof(verticalGridCandidate));
                if (!TryInterpolateVerticalShift(verticalGrid, xForGrid, yForGrid, out double verticalComponent))
                {
                    return false;
                }

                verticalOffset += timeFactor * verticalComponent;
                continue;
            }

            if (!TryFindXyzGrid(component.XyzGrids, xForGrid, yForGrid, out GeoTiffXyzGridShiftMathTransform.XyzGrid? xyzGridCandidate))
            {
                continue;
            }

            GeoTiffXyzGridShiftMathTransform.XyzGrid xyzGrid = ArgumentGuard.ThrowIfNull(xyzGridCandidate, nameof(xyzGridCandidate));
            if (!TryGetInterpolationCell(xyzGrid, xForGrid, yForGrid, out InterpolationCell cell))
            {
                continue;
            }

            if (!TryReadXyzCornerValues(xyzGrid, cell, out XyzCornerValues cornerValues))
            {
                return false;
            }

            if (component.Definition.DisplacementType == DisplacementType.ThreeDimensional)
            {
                double zComponent = Bilinear(cornerValues.Z00, cornerValues.Z01, cornerValues.Z10, cornerValues.Z11, cell.W00, cell.W01, cell.W10, cell.W11);
                verticalOffset += timeFactor * zComponent;
            }

            if (this.isHorizontalUnitDegree)
            {
                double longitudeComponent = Bilinear(cornerValues.X00, cornerValues.X01, cornerValues.X10, cornerValues.X11, cell.W00, cell.W01, cell.W10, cell.W11);
                double latitudeComponent = Bilinear(cornerValues.Y00, cornerValues.Y01, cornerValues.Y10, cornerValues.Y11, cell.W00, cell.W01, cell.W10, cell.W11);
                longitudeOffsetDegrees += timeFactor * longitudeComponent;
                latitudeOffsetDegrees += timeFactor * latitudeComponent;
                continue;
            }

            if (component.Definition.InterpolationMethod == InterpolationMethod.Bilinear)
            {
                double eastComponent = Bilinear(cornerValues.X00, cornerValues.X01, cornerValues.X10, cornerValues.X11, cell.W00, cell.W01, cell.W10, cell.W11);
                double northComponent = Bilinear(cornerValues.Y00, cornerValues.Y01, cornerValues.Y10, cornerValues.Y11, cell.W00, cell.W01, cell.W10, cell.W11);
                eastingOffset += timeFactor * eastComponent;
                northingOffset += timeFactor * northComponent;
                continue;
            }

            if (!TryInterpolateGeocentricBilinear(xyzGrid, cell, cornerValues, yWorking, out double eastGeocentricComponent, out double northGeocentricComponent))
            {
                return false;
            }

            eastingOffset += timeFactor * eastGeocentricComponent;
            northingOffset += timeFactor * northGeocentricComponent;
        }

        xOut = xWorking;
        yOut = yWorking;
        zOut += verticalOffset;

        if (this.isHorizontalUnitDegree)
        {
            xOut += longitudeOffsetDegrees;
            yOut += latitudeOffsetDegrees;
            return true;
        }

        if (this.isAddition && !this.isGeographicCrs)
        {
            xOut += eastingOffset;
            yOut += northingOffset;
            return true;
        }

        if (this.isAddition)
        {
            double cosPhi = Math.Cos(DegreesToRadians(yWorking));
            if (Math.Abs(cosPhi) < 1e-16d)
            {
                return false;
            }

            DeltaEastingNorthingToLongLat(cosPhi, eastingOffset, northingOffset, this.semiMajor, this.semiMinor, this.eccentricitySquared, out double dLamRadians, out double dPhiRadians);
            xOut += RadiansToDegrees(dLamRadians);
            yOut += RadiansToDegrees(dPhiRadians);
            return true;
        }

        double lambdaRadians = DegreesToRadians(xWorking);
        double phiRadians = DegreesToRadians(yWorking);
        double sinLambda = Math.Sin(lambdaRadians);
        double cosLambda = Math.Cos(lambdaRadians);
        double sinPhi = Math.Sin(phiRadians);
        double cosPhiForGeocentric = Math.Cos(phiRadians);
        double dnSinPhi = northingOffset * sinPhi;
        double deltaX = (-eastingOffset * sinLambda) - (dnSinPhi * cosLambda);
        double deltaY = (eastingOffset * cosLambda) - (dnSinPhi * sinLambda);
        double deltaZ = northingOffset * cosPhiForGeocentric;

        double geocentricX = xWorking;
        double geocentricY = yWorking;
        double geocentricZ = 0d;
        this.geocentricForward.Transform(ref geocentricX, ref geocentricY, ref geocentricZ);
        geocentricX += deltaX;
        geocentricY += deltaY;
        geocentricZ += deltaZ;
        this.geocentricInverse.Transform(ref geocentricX, ref geocentricY, ref geocentricZ);
        xOut = geocentricX;
        yOut = geocentricY;
        return true;
    }

    private bool TryInverse(
        double x,
        double y,
        double z,
        double observationEpoch,
        out double xOut,
        out double yOut,
        out double zOut)
    {
        xOut = x;
        yOut = y;
        zOut = z;
        for (int i = 0; i < TransformationMath.MaxInverseIterations; i++)
        {
            if (!this.TryForward(xOut, yOut, zOut, observationEpoch, true, out double xNew, out double yNew, out double zNew))
            {
                return false;
            }

            double dx = xNew - x;
            double dy = yNew - y;
            double dz = zNew - z;
            xOut -= dx;
            yOut -= dy;
            zOut -= dz;
            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < InverseHorizontalTolerance && Math.Abs(dz) < InverseVerticalTolerance)
            {
                return true;
            }
        }

        return false;
    }
}
