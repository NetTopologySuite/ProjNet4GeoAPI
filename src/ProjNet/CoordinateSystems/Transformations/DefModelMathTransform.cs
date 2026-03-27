// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
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
[Serializable]
internal sealed class DefModelMathTransform : MathTransform
{
    private const int MaximumModelSizeInBytes = 10 * 1024 * 1024;
    private const int MaxInverseIterations = 10;
    private const double InverseHorizontalTolerance = 1e-12d;
    private const double InverseVerticalTolerance = 1e-3d;
    private const double MissingObservationEpoch = double.MaxValue;

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

    private bool isInverted;
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
            new ProjectionParameter("semi_major", semiMajor),
            new ProjectionParameter("semi_minor", semiMinor),
        };
        this.geocentricForward = new GeocentricTransform(parameters, false);
        this.geocentricInverse = (GeocentricTransform)this.geocentricForward.Inverse();

        ValidateCompatibility(model, this.isGeographicCrs, this.isHorizontalUnitDegree, this.isAddition);
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

    private enum DisplacementType : byte
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        ThreeDimensional = 3,
    }

    private enum InterpolationMethod : byte
    {
        Bilinear = 0,
        GeocentricBilinear = 1,
    }

    /// <summary>
    /// Represents a time-dependent scale-factor function.
    /// </summary>
    private interface ITimeFunction
    {
        /// <summary>
        /// Evaluates the scale factor for the supplied observation epoch.
        /// </summary>
        /// <param name="observationEpoch">Observation epoch expressed in decimal years.</param>
        /// <returns>The scale factor for the supplied epoch.</returns>
        double Evaluate(double observationEpoch);
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

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
        this.isInverted = !this.isInverted;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        ArgumentGuard.ThrowArgument("defmodel requires observation time (4D input).");
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
        skipReason = null;

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
            skipReason = "Cannot open " + modelToken + ".";
            return false;
        }

        string resolvedModelPath = ArgumentGuard.ThrowIfNull(resolvedModelPathCandidate, nameof(resolvedModelPathCandidate));

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        string jsonText;
        try
        {
            var fileInfo = new FileInfo(resolvedModelPath);
            if (!fileInfo.Exists)
            {
                skipReason = "Cannot open " + modelToken + ".";
                return false;
            }

            if (fileInfo.Length > MaximumModelSizeInBytes)
            {
                skipReason = "File " + modelToken + " is too large.";
                return false;
            }

            jsonText = File.ReadAllText(resolvedModelPath);
        }
        catch (IOException exception)
        {
            skipReason = "Cannot read " + modelToken + ": " + exception.Message;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            skipReason = "Cannot read " + modelToken + ": " + exception.Message;
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
            skipReason = "invalid model: " + exception.Message;
            return false;
        }
        catch (JsonException exception)
        {
            skipReason = "invalid model: " + exception.Message;
            return false;
        }
        catch (InvalidDataException exception)
        {
            skipReason = "invalid model: " + exception.Message;
            return false;
        }
        catch (ArgumentException exception)
        {
            skipReason = "invalid model: " + exception.Message;
            return false;
        }
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        if (!IsValidObservationEpoch(t))
        {
            ArgumentGuard.ThrowArgument("defmodel requires a valid observation epoch.");
        }

        if (!this.isInverted)
        {
            if (!this.TryForward(x, y, z, t, false, out double xOut, out double yOut, out double zOut))
            {
                ArgumentGuard.ThrowArgument("defmodel forward transformation failed.");
            }

            x = xOut;
            y = yOut;
            z = zOut;
            return;
        }

        if (!this.TryInverse(x, y, z, t, out double xInv, out double yInv, out double zInv))
        {
            ArgumentGuard.ThrowArgument("defmodel inverse transformation failed.");
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

    private static bool TryResolveEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor,
        out string? skipReason)
    {
        semiMajor = 0d;
        semiMinor = 0d;
        skipReason = null;

        if (args.TryGetValue("r", out string? radiusToken)
            && TryParseFiniteDouble(radiusToken, out double radius))
        {
            if (radius <= 0d)
            {
                skipReason = "defmodel +r must be positive.";
                return false;
            }

            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("a", out string? majorToken) && TryParseFiniteDouble(majorToken, out double major))
        {
            if (major <= 0d)
            {
                skipReason = "defmodel +a must be positive.";
                return false;
            }

            semiMajor = major;
            if (args.TryGetValue("b", out string? minorToken) && TryParseFiniteDouble(minorToken, out double minor))
            {
                if (minor <= 0d)
                {
                    skipReason = "defmodel +b must be positive.";
                    return false;
                }

                semiMinor = minor;
                return true;
            }

            if (args.TryGetValue("rf", out string? inverseFlatteningToken)
                && TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening))
            {
                if (inverseFlattening <= 0d)
                {
                    skipReason = "defmodel +rf must be positive.";
                    return false;
                }

                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellipsoidToken) && !string.IsNullOrWhiteSpace(ellipsoidToken))
        {
            if (TryResolveKnownEllipsoid(ellipsoidToken, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "defmodel received unsupported +ellps value.";
            return false;
        }

        if (args.TryGetValue("datum", out string? datumToken) && !string.IsNullOrWhiteSpace(datumToken))
        {
            if (TryResolveKnownEllipsoid(datumToken, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "defmodel received unsupported +datum value.";
            return false;
        }

        semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
        semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
        return true;
    }

    private static bool TryResolveKnownEllipsoid(string token, out double semiMajor, out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (token.Equals("wgs84", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
            semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
            return true;
        }

        if (token.Equals("grs80", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad83", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.GRS80.SemiMajorAxis;
            semiMinor = Ellipsoid.GRS80.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk66", StringComparison.OrdinalIgnoreCase)
            || token.Equals("nad27", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1866.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1866.SemiMinorAxis;
            return true;
        }

        if (token.Equals("clrk80", StringComparison.OrdinalIgnoreCase)
            || token.Equals("clrk80ign", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Clarke1880.SemiMajorAxis;
            semiMinor = Ellipsoid.Clarke1880.SemiMinorAxis;
            return true;
        }

        if (token.Equals("intl", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.International1924.SemiMajorAxis;
            semiMinor = Ellipsoid.International1924.SemiMinorAxis;
            return true;
        }

        if (token.Equals("sphere", StringComparison.OrdinalIgnoreCase))
        {
            semiMajor = Ellipsoid.Sphere.SemiMajorAxis;
            semiMinor = Ellipsoid.Sphere.SemiMinorAxis;
            return true;
        }

        return false;
    }

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }

    private static bool IsDefinitionCrsGeographic(string definitionCrs)
    {
        if (!TryParseEpsgCode(definitionCrs, out int epsgCode))
        {
            return true;
        }

        var services = new CoordinateSystemServices();
        if (!services.TryGetCoordinateSystem(epsgCode, out CoordinateSystem? coordinateSystem))
        {
            return true;
        }

        return coordinateSystem is GeographicCoordinateSystem;
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

        string suffix = crsToken.Substring(prefix.Length).Trim();
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
                throw new FormatException("components[" + index.ToString(CultureInfo.InvariantCulture) + "] must be an object.");
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
                Extent = ParseSpatialExtent(GetRequiredObject(componentElement, "extent"), "components[" + index.ToString(CultureInfo.InvariantCulture) + "].extent"),
                SpatialModelFileName = GetRequiredString(GetRequiredObject(componentElement, "spatial_model"), "filename"),
                TimeFunction = timeFunction,
            };
            components.Add(component);
            index++;
        }

        return components.ToArray();
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

            return new PiecewiseTimeFunction(beforeFirst, afterLast, tuples.ToArray());
        }

        if (timeFunctionType == "EXPONENTIAL")
        {
            double referenceEpoch = ParseIso8601ToDecimalYear(GetRequiredString(parameters, "reference_epoch"));
            string endEpochString = GetOptionalString(parameters, "end_epoch");
            double? endEpoch = string.IsNullOrWhiteSpace(endEpochString)
                ? (double?)null
                : ParseIso8601ToDecimalYear(endEpochString);
            double relaxationConstant = GetRequiredDouble(parameters, "relaxation_constant");
            if (relaxationConstant <= 0d)
            {
                throw new FormatException("Invalid value for relaxation_constant.");
            }

            return new ExponentialTimeFunction(
                referenceEpoch,
                endEpoch,
                relaxationConstant,
                GetRequiredDouble(parameters, "before_scale_factor"),
                GetRequiredDouble(parameters, "initial_scale_factor"),
                GetRequiredDouble(parameters, "final_scale_factor"));
        }

        throw new FormatException("Unsupported type of time function: " + timeFunctionType + ".");
    }

    private static TimeExtent ParseTimeExtent(JsonElement timeExtentObject)
    {
        double first = ParseIso8601ToDecimalYear(GetRequiredString(timeExtentObject, "first"));
        double last = ParseIso8601ToDecimalYear(GetRequiredString(timeExtentObject, "last"));
        if (last < first)
        {
            throw new FormatException("time_extent.last must be greater than or equal to time_extent.first.");
        }

        return new TimeExtent(first, last);
    }

    private static SpatialExtent ParseSpatialExtent(JsonElement extentObject, string context)
    {
        string type = NormalizeOptionalString(GetRequiredString(extentObject, "type"));
        if (type != "BBOX")
        {
            throw new FormatException(context + " only supports type=bbox.");
        }

        JsonElement parametersObject = GetRequiredObject(extentObject, "parameters");
        JsonElement bboxArray = GetRequiredArray(parametersObject, "bbox");
        if (bboxArray.GetArrayLength() != 4)
        {
            throw new FormatException(context + ".parameters.bbox must contain exactly 4 numeric values.");
        }

        double minX = GetArrayDouble(bboxArray, 0, context);
        double minY = GetArrayDouble(bboxArray, 1, context);
        double maxX = GetArrayDouble(bboxArray, 2, context);
        double maxY = GetArrayDouble(bboxArray, 3, context);
        if (maxX < minX || maxY < minY)
        {
            throw new FormatException(context + ".parameters.bbox has invalid ordering.");
        }

        return new SpatialExtent(minX, minY, maxX, maxY);
    }

    private static double GetArrayDouble(JsonElement arrayElement, int index, string context)
    {
        JsonElement value = arrayElement[index];
        if (value.ValueKind != JsonValueKind.Number)
        {
            throw new FormatException(context + ".parameters.bbox contains a non-numeric value.");
        }

        return value.GetDouble();
    }

    private static JsonElement GetRequiredObject(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException("Missing \"" + propertyName + "\" key.");
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("\"" + propertyName + "\" must be an object.");
        }

        return value;
    }

    private static JsonElement GetRequiredArray(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException("Missing \"" + propertyName + "\" key.");
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("\"" + propertyName + "\" must be an array.");
        }

        return value;
    }

    private static string GetRequiredString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException("Missing \"" + propertyName + "\" key.");
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("\"" + propertyName + "\" must be a string.");
        }

        string? text = value.GetString();
        if (text is null)
        {
            throw new FormatException("\"" + propertyName + "\" must not be null.");
        }

        return text;
    }

    private static string GetOptionalString(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            return string.Empty;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("\"" + propertyName + "\" must be a string.");
        }

        return value.GetString() ?? string.Empty;
    }

    private static double GetRequiredDouble(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            throw new FormatException("Missing \"" + propertyName + "\" key.");
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            throw new FormatException("\"" + propertyName + "\" must be numeric.");
        }

        return value.GetDouble();
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
            throw new FormatException("Wrong formatting / invalid date-time for " + value + ".");
        }

        if (value.Length != 20
            || value[4] != '-'
            || value[7] != '-'
            || value[10] != 'T'
            || value[13] != ':'
            || value[16] != ':'
            || value[19] != 'Z')
        {
            throw new FormatException("Wrong formatting / invalid date-time for " + value + ".");
        }

        int year = ParseEpochPart(value, 0, 4);
        int month = ParseEpochPart(value, 5, 2);
        int day = ParseEpochPart(value, 8, 2);
        int hour = ParseEpochPart(value, 11, 2);
        int minute = ParseEpochPart(value, 14, 2);
        int second = ParseEpochPart(value, 17, 2);

        if (year < 1582 || month < 1 || month > 12 || day < 1 || hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second >= 61)
        {
            throw new FormatException("Wrong formatting / invalid date-time for " + value + ".");
        }

        bool isLeapYear = ((year % 4) == 0 && (year % 100) != 0) || ((year % 400) == 0);
        int[] monthLengths = isLeapYear
            ? new[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }
            : new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        if (day > monthLengths[month - 1])
        {
            throw new FormatException("Wrong formatting / invalid date-time for " + value + ".");
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
            throw new FormatException("Wrong formatting / invalid date-time for " + value + ".");
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
                throw new InvalidDataException("Cannot resolve deformation model component grid '" + component.SpatialModelFileName + "'.");
            }

            string componentPath = ArgumentGuard.ThrowIfNull(componentPathCandidate, nameof(componentPathCandidate));

            if (component.DisplacementType == DisplacementType.Vertical)
            {
                IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> vertical = GeoTiffGridLoader.LoadVertical(componentPath);
                if (vertical.Count == 0)
                {
                    throw new InvalidDataException("No vertical grid data found in '" + component.SpatialModelFileName + "'.");
                }

                loaded.Add(new ComponentRuntime(
                    component,
                    null,
                    vertical.OrderBy(grid => grid.Area, Comparer<double>.Default).ToArray()));
                continue;
            }

            bool requireMetreUnits = !isHorizontalUnitDegree;
            IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> xyz = GeoTiffGridLoader.LoadXyz(componentPath, requireMetreUnits);
            if (xyz.Count == 0)
            {
                throw new InvalidDataException("No XYZ grid data found in '" + component.SpatialModelFileName + "'.");
            }

            loaded.Add(new ComponentRuntime(
                component,
                xyz.OrderBy(grid => grid.Area, Comparer<double>.Default).ToArray(),
                null));
        }

        return loaded.ToArray();
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
        bool isAddition)
    {
        if (!isGeographicCrs && isHorizontalUnitDegree)
        {
            ArgumentGuard.ThrowArgument("definition_crs = projected CRS and horizontal_offset_unit = degree are incompatible.");
        }

        if (!isGeographicCrs && !isAddition)
        {
            ArgumentGuard.ThrowArgument("definition_crs = projected CRS and horizontal_offset_method = geocentric are incompatible.");
        }

        if (isGeographicCrs)
        {
            return;
        }

        for (int i = 0; i < model.Components.Length; i++)
        {
            if (model.Components[i].InterpolationMethod == InterpolationMethod.GeocentricBilinear)
            {
                ArgumentGuard.ThrowArgument("definition_crs = projected CRS and interpolation_method = geocentric_bilinear are incompatible.");
            }
        }
    }

    private static bool IsValidObservationEpoch(double epoch)
    {
        return !double.IsNaN(epoch) && !double.IsInfinity(epoch) && epoch != MissingObservationEpoch;
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
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
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
        cell = default(InterpolationCell);
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
        return IsFinite(values.X00)
            && IsFinite(values.X01)
            && IsFinite(values.X10)
            && IsFinite(values.X11)
            && IsFinite(values.Y00)
            && IsFinite(values.Y01)
            && IsFinite(values.Y10)
            && IsFinite(values.Y11)
            && IsFinite(values.Z00)
            && IsFinite(values.Z01)
            && IsFinite(values.Z10)
            && IsFinite(values.Z11);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
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
        for (int i = 0; i < MaxInverseIterations; i++)
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

    [Serializable]
    private readonly struct SpatialExtent(double minX, double minY, double maxX, double maxY)
    {
        internal double MinX { get; } = minX;

        internal double MinY { get; } = minY;

        internal double MaxX { get; } = maxX;

        internal double MaxY { get; } = maxY;
    }

    [Serializable]
    private readonly struct TimeExtent(double first, double last)
    {
        internal double First { get; } = first;

        internal double Last { get; } = last;
    }

    [Serializable]
    private readonly struct InterpolationCell(
        int x0,
        int y0,
        int x1,
        int y1,
        double fractionX,
        double fractionY,
        double w00,
        double w01,
        double w10,
        double w11)
    {
        internal int X0 { get; } = x0;

        internal int Y0 { get; } = y0;

        internal int X1 { get; } = x1;

        internal int Y1 { get; } = y1;

        internal double FractionX { get; } = fractionX;

        internal double FractionY { get; } = fractionY;

        internal double W00 { get; } = w00;

        internal double W01 { get; } = w01;

        internal double W10 { get; } = w10;

        internal double W11 { get; } = w11;
    }

    [Serializable]
    private readonly struct XyzCornerValues(
        double x00,
        double x01,
        double x10,
        double x11,
        double y00,
        double y01,
        double y10,
        double y11,
        double z00,
        double z01,
        double z10,
        double z11)
    {
        internal double X00 { get; } = x00;

        internal double X01 { get; } = x01;

        internal double X10 { get; } = x10;

        internal double X11 { get; } = x11;

        internal double Y00 { get; } = y00;

        internal double Y01 { get; } = y01;

        internal double Y10 { get; } = y10;

        internal double Y11 { get; } = y11;

        internal double Z00 { get; } = z00;

        internal double Z01 { get; } = z01;

        internal double Z10 { get; } = z10;

        internal double Z11 { get; } = z11;
    }

    [Serializable]
    private sealed class ModelDefinition
    {
        internal string FileType { get; set; } = string.Empty;

        internal string FormatVersion { get; set; } = string.Empty;

        internal string SourceCrs { get; set; } = string.Empty;

        internal string TargetCrs { get; set; } = string.Empty;

        internal string DefinitionCrs { get; set; } = string.Empty;

        internal string HorizontalOffsetUnit { get; set; } = string.Empty;

        internal string VerticalOffsetUnit { get; set; } = string.Empty;

        internal string HorizontalOffsetMethod { get; set; } = string.Empty;

        internal SpatialExtent Extent { get; set; }

        internal TimeExtent TimeExtent { get; set; }

        internal ComponentDefinition[] Components { get; set; } = [];
    }

    [Serializable]
    private sealed class ComponentDefinition
    {
        internal string Description { get; set; } = string.Empty;

        internal DisplacementType DisplacementType { get; set; }

        internal InterpolationMethod InterpolationMethod { get; set; }

        internal SpatialExtent Extent { get; set; }

        internal string SpatialModelFileName { get; set; } = string.Empty;

        internal ITimeFunction TimeFunction { get; set; } = ConstantTimeFunction.Instance;
    }

    [Serializable]
    private sealed class ComponentRuntime
    {
        internal ComponentRuntime(
            ComponentDefinition definition,
            IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid>? xyzGrids,
            IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid>? verticalGrids)
        {
            this.Definition = definition;
            this.XyzGrids = xyzGrids ?? [];
            this.VerticalGrids = verticalGrids ?? [];
        }

        internal ComponentDefinition Definition { get; }

        internal IReadOnlyList<GeoTiffXyzGridShiftMathTransform.XyzGrid> XyzGrids { get; }

        internal IReadOnlyList<GeoTiffVGridShiftMathTransform.VerticalGrid> VerticalGrids { get; }
    }

    [Serializable]
    private sealed class ConstantTimeFunction : ITimeFunction
    {
        internal static readonly ConstantTimeFunction Instance = new();

        private ConstantTimeFunction()
        {
        }

        public double Evaluate(double observationEpoch)
        {
            _ = observationEpoch;
            return 1d;
        }
    }

    [Serializable]
    private sealed class VelocityTimeFunction(double referenceEpoch) : ITimeFunction
    {
        public double Evaluate(double observationEpoch)
        {
            return observationEpoch - referenceEpoch;
        }
    }

    [Serializable]
    private sealed class StepTimeFunction(double stepEpoch) : ITimeFunction
    {
        public double Evaluate(double observationEpoch)
        {
            return observationEpoch < stepEpoch ? 0d : 1d;
        }
    }

    [Serializable]
    private sealed class ReverseStepTimeFunction(double stepEpoch) : ITimeFunction
    {
        public double Evaluate(double observationEpoch)
        {
            return observationEpoch < stepEpoch ? -1d : 0d;
        }
    }

    [Serializable]
    private sealed class PiecewiseTimeFunction : ITimeFunction
    {
        private readonly string beforeFirst;
        private readonly string afterLast;
        private readonly EpochScaleTuple[] model;

        internal PiecewiseTimeFunction(string beforeFirst, string afterLast, EpochScaleTuple[] model)
        {
            this.beforeFirst = beforeFirst;
            this.afterLast = afterLast;
            this.model = model ?? [];
        }

        public double Evaluate(double observationEpoch)
        {
            if (this.model.Length == 0)
            {
                return 0d;
            }

            double firstEpoch = this.model[0].Epoch;
            if (observationEpoch < firstEpoch)
            {
                if (this.beforeFirst == "ZERO")
                {
                    return 0d;
                }

                if (this.beforeFirst == "CONSTANT" || this.model.Length == 1)
                {
                    return this.model[0].ScaleFactor;
                }

                double f1 = this.model[0].ScaleFactor;
                double secondEpoch = this.model[1].Epoch;
                double f2 = this.model[1].ScaleFactor;
                if (firstEpoch == secondEpoch)
                {
                    return f1;
                }

                return ((f1 * (secondEpoch - observationEpoch)) + (f2 * (observationEpoch - firstEpoch))) / (secondEpoch - firstEpoch);
            }

            for (int i = 1; i < this.model.Length; i++)
            {
                double epochIp1 = this.model[i].Epoch;
                if (observationEpoch < epochIp1)
                {
                    double epochI = this.model[i - 1].Epoch;
                    double factorIp1 = this.model[i].ScaleFactor;
                    double factorI = this.model[i - 1].ScaleFactor;
                    return ((factorI * (epochIp1 - observationEpoch)) + (factorIp1 * (observationEpoch - epochI))) / (epochIp1 - epochI);
                }
            }

            if (this.afterLast == "ZERO")
            {
                return 0d;
            }

            if (this.afterLast == "CONSTANT" || this.model.Length == 1)
            {
                return this.model[this.model.Length - 1].ScaleFactor;
            }

            double previousEpoch = this.model[this.model.Length - 2].Epoch;
            double previousFactor = this.model[this.model.Length - 2].ScaleFactor;
            double lastEpoch = this.model[this.model.Length - 1].Epoch;
            double lastFactor = this.model[this.model.Length - 1].ScaleFactor;
            if (previousEpoch == lastEpoch)
            {
                return lastFactor;
            }

            return ((previousFactor * (lastEpoch - observationEpoch)) + (lastFactor * (observationEpoch - previousEpoch))) / (lastEpoch - previousEpoch);
        }

        [Serializable]
        internal readonly struct EpochScaleTuple(double epoch, double scaleFactor)
        {
            internal double Epoch { get; } = epoch;

            internal double ScaleFactor { get; } = scaleFactor;
        }
    }

    [Serializable]
    private sealed class ExponentialTimeFunction : ITimeFunction
    {
        private readonly double referenceEpoch;
        private readonly double? endEpoch;
        private readonly double relaxationConstant;
        private readonly double beforeScaleFactor;
        private readonly double initialScaleFactor;
        private readonly double finalScaleFactor;

        internal ExponentialTimeFunction(
            double referenceEpoch,
            double? endEpoch,
            double relaxationConstant,
            double beforeScaleFactor,
            double initialScaleFactor,
            double finalScaleFactor)
        {
            this.referenceEpoch = referenceEpoch;
            this.endEpoch = endEpoch;
            this.relaxationConstant = relaxationConstant;
            this.beforeScaleFactor = beforeScaleFactor;
            this.initialScaleFactor = initialScaleFactor;
            this.finalScaleFactor = finalScaleFactor;
        }

        public double Evaluate(double observationEpoch)
        {
            if (observationEpoch < this.referenceEpoch)
            {
                return this.beforeScaleFactor;
            }

            double clampedEpoch = observationEpoch;
            if (this.endEpoch.HasValue)
            {
                clampedEpoch = Math.Min(clampedEpoch, this.endEpoch.Value);
            }

            return this.initialScaleFactor
                + ((this.finalScaleFactor - this.initialScaleFactor)
                    * (1d - Math.Exp(-(clampedEpoch - this.referenceEpoch) / this.relaxationConstant)));
        }
    }
}
