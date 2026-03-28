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
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Creates runtime math transforms from PROJ-style pipeline operation strings.
/// </summary>
internal static class ProjPipelineMathTransformFactory
{
    private static readonly char[] CommaSeparator = [','];
    private static readonly char[] OperationTokenSeparators = [' ', '\t'];
    private static readonly string[] HorizontalGridExtensions = [".gsb", ".tif", ".tiff"];
    private static readonly string[] VerticalGridExtensions = [".gtx", ".tif", ".tiff"];
    private static readonly string[] XyzGridExtensions = [".tif", ".tiff"];

    /// <summary>
    /// Tries to create an executable transform from a full operation or pipeline definition.
    /// </summary>
    /// <param name="operation">Operation text to parse.</param>
    /// <param name="transform">Created transform when parsing succeeds.</param>
    /// <param name="skipReason">Reason why transform creation was skipped.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreateMathTransform(string operation, [NotNullWhen(true)] out MathTransform? transform, out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (operation is null)
        {
            skipReason = "Operation string was null.";
            return false;
        }

        bool hasPipeline = ContainsPipelineProjection(operation);
        IReadOnlyList<Dictionary<string, string>>? pipelineStepArguments = null;
        if (hasPipeline
            && !TryParsePipelineStepArguments(operation, out pipelineStepArguments, out skipReason))
        {
            return false;
        }

        IReadOnlyList<Dictionary<string, string>> parsedPipelineSteps = hasPipeline
            ? ArgumentGuard.ThrowIfNull(pipelineStepArguments, nameof(pipelineStepArguments))
            : [];

        int stepCount = hasPipeline
            ? parsedPipelineSteps.Count
            : 1;
        PipelineExecutionContext? executionContext = hasPipeline
            ? new PipelineExecutionContext()
            : null;

        var stepTransforms = new List<MathTransform>(stepCount);
        for (int i = 0; i < stepCount; i++)
        {
            MathTransform? stepTransformCandidate;
            string? stepSkipReason;
            bool ok = hasPipeline
                ? TryCreateStepTransform(parsedPipelineSteps[i], executionContext, out stepTransformCandidate, out stepSkipReason)
                : TryCreateStepTransform(operation, executionContext, out stepTransformCandidate, out stepSkipReason);
            if (!ok)
            {
                skipReason = hasPipeline
                    ? "Pipeline step " + (i + 1).ToString(CultureInfo.InvariantCulture) + " failed: " + (stepSkipReason ?? "unknown reason")
                    : (stepSkipReason ?? "Unable to create transform.");
                return false;
            }

            stepTransforms.Add(ArgumentGuard.ThrowIfNull(stepTransformCandidate, nameof(stepTransformCandidate)));
        }

        if (stepTransforms.Count == 0)
        {
            skipReason = "Operation did not contain any executable step.";
            return false;
        }

        if (stepTransforms.Count == 1 && !hasPipeline)
        {
            transform = stepTransforms[0];
            return true;
        }

        if (hasPipeline)
        {
            PipelineExecutionContext pipelineExecutionContext = ArgumentGuard.ThrowIfNull(executionContext, nameof(executionContext));
            transform = new PipelineCompositeMathTransform(stepTransforms, pipelineExecutionContext);
        }
        else
        {
            transform = new CompositeMathTransform(stepTransforms);
        }

        return true;
    }

    private static bool TryCreateStepTransform(
        string operation,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryParseOperationArguments(operation, out Dictionary<string, string> args))
        {
            skipReason = "Unable to parse operation parameters.";
            return false;
        }

        return TryCreateStepTransform(args, executionContext, out transform, out skipReason);
    }

    private static bool TryCreateStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("proj", out string? projCode))
        {
            skipReason = "Operation is missing +proj.";
            return false;
        }

        bool omitForward = executionContext is not null && args.ContainsKey("omit_fwd");
        bool omitInverse = executionContext is not null && args.ContainsKey("omit_inv");

        if (projCode.Equals("latlong", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("longlat", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("latlon", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("lonlat", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("noop", StringComparison.OrdinalIgnoreCase))
        {
            transform = new IdentityMathTransform(3);
            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("geocent", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("cart", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateGeocentricCartesianTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("geoc", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateGeocentricLatitudeTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("geogoffset", StringComparison.OrdinalIgnoreCase))
        {
            if (!GeogOffsetMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("affine", StringComparison.OrdinalIgnoreCase))
        {
            if (!AffineRuntimeMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("push", StringComparison.OrdinalIgnoreCase))
        {
            PipelineExecutionContext? pushExecutionContext = executionContext;
            if (pushExecutionContext is null)
            {
                skipReason = "push operation requires a pipeline execution context.";
                return false;
            }

            if (!PipelineStackTransferMathTransform.TryCreatePush(args, pushExecutionContext, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("pop", StringComparison.OrdinalIgnoreCase))
        {
            PipelineExecutionContext? popExecutionContext = executionContext;
            if (popExecutionContext is null)
            {
                skipReason = "pop operation requires a pipeline execution context.";
                return false;
            }

            if (!PipelineStackTransferMathTransform.TryCreatePop(args, popExecutionContext, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            if (!SetMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("axisswap", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateAxisSwapTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("unitconvert", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateUnitConvertTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("hgridshift", StringComparison.OrdinalIgnoreCase)
            || projCode.Equals("gridshift", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateHorizontalGridShiftTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("vgridshift", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateVerticalGridShiftTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("xyzgridshift", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryCreateXyzGridShiftTransform(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("defmodel", StringComparison.OrdinalIgnoreCase))
        {
            if (!DefModelMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("deformation", StringComparison.OrdinalIgnoreCase))
        {
            if (!DeformationMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("tinshift", StringComparison.OrdinalIgnoreCase))
        {
            if (!TinShiftMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("topocentric", StringComparison.OrdinalIgnoreCase))
        {
            if (!TopocentricMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("vertoffset", StringComparison.OrdinalIgnoreCase))
        {
            if (!VertOffsetMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("helmert", StringComparison.OrdinalIgnoreCase))
        {
            if (!HelmertMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("molobadekas", StringComparison.OrdinalIgnoreCase))
        {
            if (!MolobadekasMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("molodensky", StringComparison.OrdinalIgnoreCase))
        {
            if (!MolodenskyMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("horner", StringComparison.OrdinalIgnoreCase))
        {
            if (!HornerMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("ob_tran", StringComparison.OrdinalIgnoreCase))
        {
            if (!ObTranMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("sch", StringComparison.OrdinalIgnoreCase))
        {
            if (!SchMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (projCode.Equals("spherical_cross_track_height", StringComparison.OrdinalIgnoreCase))
        {
            if (!SchMathTransform.TryCreate(args, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (TryCreateProjectionStepTransform(args, projCode, out transform, out skipReason))
        {
            transform = WrapWithOmitFlags(transform, omitForward, omitInverse);
            return true;
        }

        if (skipReason is not null)
        {
            return false;
        }

        skipReason = "Projection '" + projCode + "' is not part of the current builtins wave.";
        return false;
    }

    private static MathTransform WrapWithOmitFlags(MathTransform stepTransform, bool omitForward, bool omitInverse)
    {
        return omitForward || omitInverse
            ? new PipelineOmitMathTransform(stepTransform, omitForward, omitInverse)
            : stepTransform;
    }

    private static bool TryCreateAxisSwapTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        bool hasOrder = args.TryGetValue("order", out string? orderTokenCandidate) && !string.IsNullOrWhiteSpace(orderTokenCandidate);
        bool hasAxis = args.TryGetValue("axis", out string? axisTokenCandidate) && !string.IsNullOrWhiteSpace(axisTokenCandidate);
        if (hasOrder == hasAxis)
        {
            skipReason = "Axisswap requires exactly one of +order or +axis.";
            return false;
        }

        int[] order;
        if (hasOrder)
        {
            string orderToken = ArgumentGuard.ThrowIfNull(orderTokenCandidate, nameof(orderTokenCandidate));
            if (!TryParseAxisSwapOrder(orderToken, out order))
            {
                skipReason = "Unable to parse +order parameter for axisswap.";
                return false;
            }
        }
        else
        {
            string axisToken = ArgumentGuard.ThrowIfNull(axisTokenCandidate, nameof(axisTokenCandidate));
            if (!TryParseAxisOrder(axisToken, out order))
            {
                skipReason = "Unable to parse +axis parameter for axisswap.";
                return false;
            }
        }

        int dimension = order.Length;
        if (dimension is < 2 or > 4)
        {
            skipReason = "Axisswap supports only 2D, 3D or 4D coordinates in the current runtime.";
            return false;
        }

        int[] sourceIndices = [0, 1, 2, 3];
        int[] signs = [1, 1, 1, 1];
        for (int i = 0; i < dimension; i++)
        {
            int rawOrder = order[i];
            int sourceIndex = Math.Abs(rawOrder) - 1;
            if (sourceIndex < 0 || sourceIndex >= dimension)
            {
                skipReason = "Axisswap order references an out-of-range axis.";
                return false;
            }

            sourceIndices[i] = sourceIndex;
            signs[i] = rawOrder < 0 ? -1 : 1;
        }

        transform = new AxisSwapMathTransform(
            dimension,
            sourceIndices[0],
            signs[0],
            sourceIndices[1],
            signs[1],
            sourceIndices[2],
            signs[2],
            sourceIndices[3],
            signs[3]);

        return true;
    }

    private static bool TryCreateUnitConvertTransform(
        IDictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryResolveUnitScale(args, "xy_in", "xy_out", true, out double xyScale))
        {
            skipReason = "Unable to parse XY units for unitconvert.";
            return false;
        }

        if (!TryResolveUnitScale(args, "z_in", "z_out", false, out double zScale))
        {
            skipReason = "Unable to parse Z units for unitconvert.";
            return false;
        }

        transform = new UnitConvertMathTransform(3, xyScale, zScale);
        return true;
    }

    private static bool TryCreateGeocentricCartesianTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        if (!TryResolveGeocentricScale(args, out double toMeterScale, out skipReason))
        {
            return false;
        }

        // PROJ cart/geocent applies to_meter to cartesian output units. Keep internal
        // ellipsoid units aligned by scaling axis values accordingly.
        semiMajor /= toMeterScale;
        semiMinor /= toMeterScale;
        if (semiMajor <= 0d || semiMinor <= 0d || double.IsNaN(semiMajor) || double.IsInfinity(semiMajor) || double.IsNaN(semiMinor) || double.IsInfinity(semiMinor))
        {
            skipReason = "geocent/cart resolved ellipsoid axes must be finite and positive.";
            return false;
        }

        var geocentricParameters = new List<ProjectionParameter>(2)
        {
            new ProjectionParameter("semi_major", semiMajor),
            new ProjectionParameter("semi_minor", semiMinor),
        };

        transform = new GeocentricTransform(geocentricParameters, false);
        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryCreateGeocentricLatitudeTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        transform = new GeocentricLatitudeMathTransform(semiMajor, semiMinor, args.ContainsKey("inv"));
        return true;
    }

    private static bool TryCreateProjectionStepTransform(
        Dictionary<string, string> args,
        string projCode,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!TryBuildProjectionStepParameters(args, projCode, out List<ProjectionParameter>? parametersCandidate, out skipReason))
        {
            return false;
        }

        List<ProjectionParameter> parameters = ArgumentGuard.ThrowIfNull(parametersCandidate, nameof(parametersCandidate));

        try
        {
            transform = ProjectionsRegistry.CreateProjection(projCode, parameters);
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            skipReason = projCode + " projection could not be created with the parsed parameter set.";
            return false;
        }
        catch (InvalidOperationException)
        {
            skipReason = projCode + " projection operation could not be constructed for this step.";
            return false;
        }
        catch (TargetInvocationException)
        {
            skipReason = projCode + " projection constructor rejected the current parameter set.";
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryBuildProjectionStepParameters(
        Dictionary<string, string> args,
        string projCode,
        [NotNullWhen(true)] out List<ProjectionParameter>? parameters,
        out string? skipReason)
    {
        parameters = null;
        skipReason = null;

        if (!TryResolveProjectionEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        if (!TryResolveProjectionUnitFactor(args, out double unitFactor, out skipReason))
        {
            return false;
        }

        parameters = new List<ProjectionParameter>(10)
        {
            new ProjectionParameter("latitude_of_origin", 0d),
            new ProjectionParameter("central_meridian", 0d),
            new ProjectionParameter("scale_factor", 1d),
            new ProjectionParameter("false_easting", 0d),
            new ProjectionParameter("false_northing", 0d),
            new ProjectionParameter("semi_major", semiMajor),
            new ProjectionParameter("semi_minor", semiMinor),
            new ProjectionParameter("unit", unitFactor),
        };

        if (!TryApplyOptionalProjectionParameter(args, "lat_0", "latitude_of_origin", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lon_0", "central_meridian", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "x_0", "false_easting", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "y_0", "false_northing", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_1", "standard_parallel_1", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_2", "standard_parallel_2", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lonc", "longitude_of_center", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_ts", "latitude_true_scale", parameters, out skipReason)
            || !TryApplyOptionalProjectionParameter(args, "lat_ts", "lat_ts", parameters, out skipReason))
        {
            return false;
        }

        if (args.TryGetValue("k_0", out string? k0Token) && !string.IsNullOrWhiteSpace(k0Token))
        {
            if (!TryParseFiniteDouble(k0Token, out double k0))
            {
                skipReason = "Invalid value for +k_0.";
                return false;
            }

            SetOrAddProjectionParameter(parameters, "scale_factor", k0);
        }
        else if (args.TryGetValue("k", out string? kToken) && !string.IsNullOrWhiteSpace(kToken))
        {
            if (!TryParseFiniteDouble(kToken, out double k))
            {
                skipReason = "Invalid value for +k.";
                return false;
            }

            SetOrAddProjectionParameter(parameters, "scale_factor", k);
        }

        if (args.ContainsKey("south"))
        {
            SetOrAddProjectionParameter(parameters, "south", 1d);
        }

        if (!projCode.Equals("utm", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!TryGetZoneCentralMeridian(args, out double centralMeridian))
        {
            skipReason = "utm step requires a valid +zone parameter.";
            return false;
        }

        SetOrAddProjectionParameter(parameters, "latitude_of_origin", 0d);
        SetOrAddProjectionParameter(parameters, "central_meridian", centralMeridian);
        SetOrAddProjectionParameter(parameters, "scale_factor", 0.9996d);
        SetOrAddProjectionParameter(parameters, "false_easting", 500000d);
        SetOrAddProjectionParameter(parameters, "false_northing", args.ContainsKey("south") ? 10000000d : 0d);

        return true;
    }

    private static bool TryResolveProjectionUnitFactor(
        Dictionary<string, string> args,
        out double unitFactor,
        out string? skipReason)
    {
        unitFactor = 1d;
        skipReason = null;

        if (args.TryGetValue("to_meter", out string? toMeterToken) && !string.IsNullOrWhiteSpace(toMeterToken))
        {
            if (!TryParsePositiveScaleFactor(toMeterToken, out unitFactor))
            {
                skipReason = "Unable to parse +to_meter parameter for projection step.";
                return false;
            }
        }
        else if (args.TryGetValue("units", out string? unitsToken)
            && !string.IsNullOrWhiteSpace(unitsToken)
            && !TryResolveUnitFactor(unitsToken, out unitFactor))
        {
            skipReason = "Unable to parse +units parameter for projection step.";
            return false;
        }

        return true;
    }

    private static bool TryApplyOptionalProjectionParameter(
        Dictionary<string, string> args,
        string sourceKey,
        string targetName,
        List<ProjectionParameter> parameters,
        out string? skipReason)
    {
        skipReason = null;
        if (!args.TryGetValue(sourceKey, out string? token) || string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        if (!TryParseFiniteDouble(token, out double value))
        {
            skipReason = "Invalid value for +" + sourceKey + ".";
            return false;
        }

        SetOrAddProjectionParameter(parameters, targetName, value);
        return true;
    }

    private static void SetOrAddProjectionParameter(
        List<ProjectionParameter> parameters,
        string name,
        double value)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                parameters[i] = new ProjectionParameter(name, value);
                return;
            }
        }

        parameters.Add(new ProjectionParameter(name, value));
    }

    private static bool TryResolveProjectionEllipsoid(
        Dictionary<string, string> args,
        out double semiMajor,
        out double semiMinor,
        out string? skipReason)
    {
        semiMajor = 0d;
        semiMinor = 0d;
        skipReason = null;

        if (args.TryGetValue("r", out string? radiusToken)
            && TryParseFiniteDouble(radiusToken, out double radius)
            && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("a", out string? majorToken)
            && TryParseFiniteDouble(majorToken, out double major)
            && major > 0d)
        {
            semiMajor = major;
            if (args.TryGetValue("b", out string? minorToken)
                && TryParseFiniteDouble(minorToken, out double minor)
                && minor > 0d)
            {
                semiMinor = minor;
                return true;
            }

            if (args.TryGetValue("rf", out string? inverseFlatteningToken)
                && TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening)
                && inverseFlattening > 0d)
            {
                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            if (TryResolveKnownEllipsoid(ellps, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "utm received unsupported +ellps value.";
            return false;
        }

        if (args.TryGetValue("datum", out string? datum) && !string.IsNullOrWhiteSpace(datum))
        {
            if (TryResolveKnownEllipsoid(datum, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "utm received unsupported +datum value.";
            return false;
        }

        semiMajor = Ellipsoid.WGS84.SemiMajorAxis;
        semiMinor = Ellipsoid.WGS84.SemiMinorAxis;
        return true;
    }

    private static bool TryGetZoneCentralMeridian(Dictionary<string, string> args, out double centralMeridian)
    {
        centralMeridian = 0d;
        if (!args.TryGetValue("zone", out string? zoneToken) || string.IsNullOrWhiteSpace(zoneToken))
        {
            return false;
        }

        string digits = zoneToken.Trim();
        int zone = 0;
        int index = 0;
        while (index < digits.Length && char.IsDigit(digits[index]))
        {
            int digit = digits[index] - '0';
            if (zone > ((int.MaxValue - digit) / 10))
            {
                return false;
            }

            zone = (zone * 10) + digit;
            index++;
        }

        if (index == 0)
        {
            return false;
        }

        if (zone is < 1 or > 60)
        {
            return false;
        }

        centralMeridian = (zone * 6d) - 183d;
        return true;
    }

    private static bool TryResolveGeocentricScale(
        Dictionary<string, string> args,
        out double scale,
        out string? skipReason)
    {
        scale = 1d;
        skipReason = null;

        if (args.TryGetValue("units", out string? unitsToken)
            && !string.IsNullOrWhiteSpace(unitsToken)
            && !unitsToken.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            skipReason = "geocent/cart currently supports only +units=m.";
            return false;
        }

        if (args.TryGetValue("to_meter", out string? toMeterToken) && !string.IsNullOrWhiteSpace(toMeterToken))
        {
            if (!TryParsePositiveScaleFactor(toMeterToken, out scale))
            {
                skipReason = "Unable to parse +to_meter parameter for geocent/cart.";
                return false;
            }
        }

        return true;
    }

    private static bool TryParsePositiveScaleFactor(string token, out double scale)
    {
        scale = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsedScale))
        {
            if (parsedScale <= 0d || double.IsNaN(parsedScale) || double.IsInfinity(parsedScale))
            {
                return false;
            }

            scale = parsedScale;
            return true;
        }

#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        int slashIndex = token.IndexOf('/', StringComparison.Ordinal);
#else
        int slashIndex = token.IndexOf('/');
#endif
        if (slashIndex <= 0 || slashIndex >= token.Length - 1)
        {
            return false;
        }

        string numeratorToken = token.Substring(0, slashIndex).Trim();
        string denominatorToken = token.Substring(slashIndex + 1).Trim();
        if (!double.TryParse(numeratorToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double numerator)
            || !double.TryParse(denominatorToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double denominator))
        {
            return false;
        }

        if (denominator == 0d)
        {
            return false;
        }

        parsedScale = numerator / denominator;
        if (parsedScale <= 0d || double.IsNaN(parsedScale) || double.IsInfinity(parsedScale))
        {
            return false;
        }

        scale = parsedScale;
        return true;
    }

    private static bool TryCreateHorizontalGridShiftTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("grids", out string? gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Horizontal grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string>? gridPathsCandidate, out skipReason))
        {
            return false;
        }

        IReadOnlyList<string> gridPaths = ArgumentGuard.ThrowIfNull(gridPathsCandidate, nameof(gridPathsCandidate));

        if (!TryValidateGridExtensions(gridPaths, HorizontalGridExtensions, "horizontal", out skipReason))
        {
            return false;
        }

        try
        {
            bool hasGeoTiff = ContainsGeoTiffGrid(gridPaths);
            transform = hasGeoTiff
                ? (MathTransform)new GeoTiffHGridShiftMathTransform(gridPaths)
                : new Ntv2HGridShiftMathTransform(gridPaths);
        }
        catch (IOException ioException)
        {
            skipReason = "Unable to read horizontal grid: " + ioException.Message;
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = "Invalid horizontal grid data: " + dataException.Message;
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = "Invalid grid parameters: " + argumentException.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryCreateVerticalGridShiftTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("grids", out string? gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Vertical grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string>? gridPathsCandidate, out skipReason))
        {
            return false;
        }

        IReadOnlyList<string> gridPaths = ArgumentGuard.ThrowIfNull(gridPathsCandidate, nameof(gridPathsCandidate));

        if (!TryValidateGridExtensions(gridPaths, VerticalGridExtensions, "vertical", out skipReason))
        {
            return false;
        }

        double multiplier = -1d;
        if (args.TryGetValue("multiplier", out string? multiplierToken)
            && !double.TryParse(multiplierToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out multiplier))
        {
            skipReason = "Unable to parse +multiplier parameter for vgridshift.";
            return false;
        }

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
        {
            skipReason = "vgridshift +multiplier must be a finite numeric value.";
            return false;
        }

        try
        {
            bool hasGeoTiff = ContainsGeoTiffGrid(gridPaths);
            transform = hasGeoTiff
                ? (MathTransform)new GeoTiffVGridShiftMathTransform(gridPaths, multiplier)
                : new GtxVGridShiftMathTransform(gridPaths, multiplier);
        }
        catch (IOException ioException)
        {
            skipReason = "Unable to read vertical grid: " + ioException.Message;
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = "Invalid vertical grid data: " + dataException.Message;
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = "Invalid grid parameters: " + argumentException.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryCreateXyzGridShiftTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (!args.TryGetValue("grids", out string? gridsToken) || string.IsNullOrWhiteSpace(gridsToken))
        {
            skipReason = "Geocentric grid shift requires +grids.";
            return false;
        }

        if (!TryResolveGridPaths(gridsToken, out IReadOnlyList<string>? gridPathsCandidate, out skipReason))
        {
            return false;
        }

        IReadOnlyList<string> gridPaths = ArgumentGuard.ThrowIfNull(gridPathsCandidate, nameof(gridPathsCandidate));

        if (!TryValidateGridExtensions(gridPaths, XyzGridExtensions, "xyz", out skipReason))
        {
            return false;
        }

        bool gridRefIsInput = true;
        if (args.TryGetValue("grid_ref", out string? gridRefToken) && !string.IsNullOrWhiteSpace(gridRefToken))
        {
            if (gridRefToken.Equals("input_crs", StringComparison.OrdinalIgnoreCase))
            {
                gridRefIsInput = true;
            }
            else if (gridRefToken.Equals("output_crs", StringComparison.OrdinalIgnoreCase))
            {
                gridRefIsInput = false;
            }
            else
            {
                skipReason = "xyzgridshift +grid_ref must be 'input_crs' or 'output_crs'.";
                return false;
            }
        }

        double multiplier = 1d;
        if (args.TryGetValue("multiplier", out string? multiplierToken)
            && !double.TryParse(multiplierToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out multiplier))
        {
            skipReason = "Unable to parse +multiplier parameter for xyzgridshift.";
            return false;
        }

        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier))
        {
            skipReason = "xyzgridshift +multiplier must be a finite numeric value.";
            return false;
        }

        if (!TryResolveEllipsoid(args, out double semiMajor, out double semiMinor, out skipReason))
        {
            return false;
        }

        try
        {
            transform = new GeoTiffXyzGridShiftMathTransform(gridPaths, semiMajor, semiMinor, multiplier, gridRefIsInput);
        }
        catch (IOException ioException)
        {
            skipReason = "Unable to read xyz grid: " + ioException.Message;
            return false;
        }
        catch (InvalidDataException dataException)
        {
            skipReason = "Invalid xyz grid data: " + dataException.Message;
            return false;
        }
        catch (ArgumentException argumentException)
        {
            skipReason = "Invalid xyz grid parameters: " + argumentException.Message;
            return false;
        }

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    private static bool TryValidateGridExtensions(
        IReadOnlyList<string> gridPaths,
        IReadOnlyList<string> allowedExtensions,
        string gridFamilyName,
        out string? skipReason)
    {
        skipReason = null;
        string allowedList = string.Join("/", allowedExtensions);
        for (int i = 0; i < gridPaths.Count; i++)
        {
            string path = gridPaths[i];
            if (!IsPathWithAnyExtension(path, allowedExtensions))
            {
                skipReason = "Grid '" + Path.GetFileName(path) + "' is not a supported " + gridFamilyName + " grid format (" + allowedList + ").";
                return false;
            }
        }

        return true;
    }

    private static bool ContainsGeoTiffGrid(IReadOnlyList<string> gridPaths)
    {
        for (int i = 0; i < gridPaths.Count; i++)
        {
            if (IsGeoTiffPath(gridPaths[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGeoTiffPath(string path)
    {
        return IsPathWithAnyExtension(path, XyzGridExtensions);
    }

    private static bool IsPathWithAnyExtension(string path, IReadOnlyList<string> extensions)
    {
        string extension = Path.GetExtension(path);
        for (int i = 0; i < extensions.Count; i++)
        {
            if (extension.Equals(extensions[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveGridPaths(
        string gridsToken,
        [NotNullWhen(true)] out IReadOnlyList<string>? resolvedPaths,
        out string? skipReason)
    {
        resolvedPaths = [];
        skipReason = null;

        string[] entries = gridsToken.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0)
        {
            skipReason = "Horizontal grid shift requires at least one grid name in +grids.";
            return false;
        }

        var resolved = new List<string>(entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            string token = entries[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            bool isOptional = token.Length > 0 && token[0] == '@';
            string gridName = isOptional ? token.Substring(1) : token;
            if (string.IsNullOrWhiteSpace(gridName))
            {
                continue;
            }

            if (CoordinateTransformationFactory.TryResolveGridResourcePath(gridName, out string? resolvedPathCandidate))
            {
                resolved.Add(ArgumentGuard.ThrowIfNull(resolvedPathCandidate, nameof(resolvedPathCandidate)));
                continue;
            }

            if (!isOptional)
            {
                skipReason = "Required grid '" + gridName + "' was not found.";
                return false;
            }
        }

        if (resolved.Count == 0)
        {
            skipReason = "No grid from +grids could be resolved.";
            return false;
        }

        resolvedPaths = resolved;
        return true;
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
            && TryParseFiniteDouble(radiusToken, out double radius)
            && radius > 0d)
        {
            semiMajor = radius;
            semiMinor = radius;
            return true;
        }

        if (args.TryGetValue("a", out string? majorToken)
            && TryParseFiniteDouble(majorToken, out double major)
            && major > 0d)
        {
            semiMajor = major;
            if (args.TryGetValue("b", out string? minorToken)
                && TryParseFiniteDouble(minorToken, out double minor)
                && minor > 0d)
            {
                semiMinor = minor;
                return true;
            }

            if (args.TryGetValue("rf", out string? inverseFlatteningToken)
                && TryParseFiniteDouble(inverseFlatteningToken, out double inverseFlattening)
                && inverseFlattening > 0d)
            {
                semiMinor = (1d - (1d / inverseFlattening)) * major;
                return true;
            }

            semiMinor = major;
            return true;
        }

        if (args.TryGetValue("ellps", out string? ellps) && !string.IsNullOrWhiteSpace(ellps))
        {
            if (TryResolveKnownEllipsoid(ellps, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "xyzgridshift received unsupported +ellps value.";
            return false;
        }

        if (args.TryGetValue("datum", out string? datum) && !string.IsNullOrWhiteSpace(datum))
        {
            if (TryResolveKnownEllipsoid(datum, out semiMajor, out semiMinor))
            {
                return true;
            }

            skipReason = "xyzgridshift received unsupported +datum value.";
            return false;
        }

        skipReason = "xyzgridshift requires ellipsoid definition (+ellps, +datum, +r, +a/+b, or +a/+rf).";
        return false;
    }

    private static bool TryResolveKnownEllipsoid(string token, out double semiMajor, out double semiMinor)
    {
        semiMajor = 0d;
        semiMinor = 0d;

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

    private static bool TryResolveUnitScale(
        IDictionary<string, string> args,
        string inKey,
        string outKey,
        bool treatDegRadAsIdentity,
        out double scale)
    {
        scale = 1d;
        bool hasIn = args.TryGetValue(inKey, out string? inToken) && !string.IsNullOrWhiteSpace(inToken);
        bool hasOut = args.TryGetValue(outKey, out string? outToken) && !string.IsNullOrWhiteSpace(outToken);
        if (!hasIn && !hasOut)
        {
            return true;
        }

        if (hasIn != hasOut)
        {
            return false;
        }

        string inUnitToken = ArgumentGuard.ThrowIfNull(inToken, nameof(inToken));
        string outUnitToken = ArgumentGuard.ThrowIfNull(outToken, nameof(outToken));
        if (treatDegRadAsIdentity
            && ((inUnitToken.Equals("deg", StringComparison.OrdinalIgnoreCase) && outUnitToken.Equals("rad", StringComparison.OrdinalIgnoreCase))
                || (inUnitToken.Equals("rad", StringComparison.OrdinalIgnoreCase) && outUnitToken.Equals("deg", StringComparison.OrdinalIgnoreCase))))
        {
            scale = 1d;
            return true;
        }

        if (!TryResolveUnitFactor(inUnitToken, out double inFactor) || !TryResolveUnitFactor(outUnitToken, out double outFactor))
        {
            return false;
        }

        if (outFactor == 0d)
        {
            return false;
        }

        scale = inFactor / outFactor;
        return true;
    }

    private static bool TryResolveUnitFactor(string token, out double factor)
    {
        factor = 0d;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double numeric))
        {
            if (numeric <= 0d || double.IsInfinity(numeric) || double.IsNaN(numeric))
            {
                return false;
            }

            factor = numeric;
            return true;
        }

        if (token.Equals("mm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-3d;
            return true;
        }

        if (token.Equals("cm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-2d;
            return true;
        }

        if (token.Equals("dm", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e-1d;
            return true;
        }

        if (token.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1d;
            return true;
        }

        if (token.Equals("km", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1e3d;
            return true;
        }

        if (token.Equals("ft", StringComparison.OrdinalIgnoreCase))
        {
            factor = 0.3048d;
            return true;
        }

        if (token.Equals("rad", StringComparison.OrdinalIgnoreCase))
        {
            factor = 1d;
            return true;
        }

        if (token.Equals("deg", StringComparison.OrdinalIgnoreCase))
        {
            factor = Math.PI / 180d;
            return true;
        }

        if (token.Equals("grad", StringComparison.OrdinalIgnoreCase))
        {
            factor = Math.PI / 200d;
            return true;
        }

        return false;
    }

    private static bool TryParseAxisSwapOrder(string orderToken, out int[] order)
    {
        order = [];
        if (string.IsNullOrWhiteSpace(orderToken))
        {
            return false;
        }

        string[] segments = orderToken.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments.Length > 4)
        {
            return false;
        }

        int[] parsed = new int[segments.Length];
        var seen = new HashSet<int>();
        for (int i = 0; i < segments.Length; i++)
        {
            if (!int.TryParse(segments[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }

            int axis = Math.Abs(value);
            if (axis < 1 || axis > 4 || !seen.Add(axis))
            {
                return false;
            }

            parsed[i] = value;
        }

        order = parsed;
        return true;
    }

    private static bool TryParseAxisOrder(string axisToken, out int[] order)
    {
        order = [];
        if (string.IsNullOrWhiteSpace(axisToken))
        {
            return false;
        }

        string axis = axisToken.Trim();
        if (axis.Length < 2 || axis.Length > 4)
        {
            return false;
        }

        int[] parsed = new int[axis.Length];
        var seen = new HashSet<int>();
        for (int i = 0; i < axis.Length; i++)
        {
            char c = char.ToLowerInvariant(axis[i]);
            int mapped;
            switch (c)
            {
                case 'e':
                    mapped = 1;
                    break;
                case 'w':
                    mapped = -1;
                    break;
                case 'n':
                    mapped = 2;
                    break;
                case 's':
                    mapped = -2;
                    break;
                case 'u':
                    mapped = 3;
                    break;
                case 'd':
                    mapped = -3;
                    break;
                default:
                    return false;
            }

            int absMapped = Math.Abs(mapped);
            if (!seen.Add(absMapped))
            {
                return false;
            }

            parsed[i] = mapped;
        }

        order = parsed;
        return true;
    }

    private static bool TrySplitPipelineSteps(string operation, out IReadOnlyList<string> steps)
    {
        var parsedSteps = new List<string>();
        var currentStepTokens = new List<string>();
        bool inPipeline = false;

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            string normalized = token.Length > 0 && token[0] == '+'
                ? token.Substring(1)
                : token;

            if (normalized.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                continue;
            }

            if (normalized.Equals("step", StringComparison.OrdinalIgnoreCase))
            {
                inPipeline = true;
                if (currentStepTokens.Count > 0)
                {
                    parsedSteps.Add(string.Join(" ", currentStepTokens));
                    currentStepTokens.Clear();
                }

                continue;
            }

            if (!inPipeline)
            {
                continue;
            }

            currentStepTokens.Add(token);
        }

        if (currentStepTokens.Count > 0)
        {
            parsedSteps.Add(string.Join(" ", currentStepTokens));
        }

        steps = parsedSteps;
        return parsedSteps.Count > 0;
    }

    private static bool ContainsPipelineProjection(string operation)
    {
        if (operation is null)
        {
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token.Length == 0 || token[0] != '+')
            {
                continue;
            }

            string body = token.Substring(1);
            if (body.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParsePipelineStepArguments(
        string operation,
        [NotNullWhen(true)] out IReadOnlyList<Dictionary<string, string>>? steps,
        out string? skipReason)
    {
        steps = null;
        skipReason = null;

        if (operation is null)
        {
            skipReason = "Operation string was null.";
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        var globalArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentStepArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parsedSteps = new List<Dictionary<string, string>>();
        bool insidePipelineDefinition = false;
        bool insideStepSection = false;
        bool previousTokenWasStep = false;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token.Length == 0 || token[0] != '+')
            {
                continue;
            }

            string body = token.Substring(1);
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            int separatorIndex = body.IndexOf('=', StringComparison.Ordinal);
#else
            int separatorIndex = body.IndexOf('=');
#endif
            string key = separatorIndex < 0
                ? body
                : body.Substring(0, separatorIndex);
            string value = separatorIndex < 0
                ? "true"
                : body.Substring(separatorIndex + 1);
            if (key.Equals("proj", StringComparison.OrdinalIgnoreCase)
                && value.Equals("pipeline", StringComparison.OrdinalIgnoreCase))
            {
                insidePipelineDefinition = true;
                continue;
            }

            if (!insidePipelineDefinition)
            {
                continue;
            }

            if (key.Equals("step", StringComparison.OrdinalIgnoreCase))
            {
                if (!insideStepSection)
                {
                    insideStepSection = true;
                }
                else if (currentStepArgs.Count == 0)
                {
                    skipReason = "Pipeline contains an empty +step.";
                    return false;
                }
                else
                {
                    parsedSteps.Add(BuildPipelineStepArguments(globalArgs, currentStepArgs));
                    currentStepArgs.Clear();
                }

                previousTokenWasStep = true;
                continue;
            }

            previousTokenWasStep = false;
            if (!insideStepSection)
            {
                globalArgs[key] = value;
                continue;
            }

            currentStepArgs[key] = value;
        }

        if (!insidePipelineDefinition)
        {
            skipReason = "Operation is missing +proj=pipeline.";
            return false;
        }

        if (!insideStepSection)
        {
            skipReason = "Pipeline operation did not contain any +step definition.";
            return false;
        }

        if (currentStepArgs.Count > 0)
        {
            parsedSteps.Add(BuildPipelineStepArguments(globalArgs, currentStepArgs));
        }
        else if (previousTokenWasStep)
        {
            skipReason = "Pipeline operation ended with +step but no step parameters.";
            return false;
        }

        if (parsedSteps.Count == 0)
        {
            skipReason = "Pipeline operation did not contain any executable step.";
            return false;
        }

        steps = parsedSteps;
        return true;
    }

    private static Dictionary<string, string> BuildPipelineStepArguments(
        Dictionary<string, string> globalArgs,
        Dictionary<string, string> stepArgs)
    {
        var merged = new Dictionary<string, string>(globalArgs, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> step in stepArgs)
        {
            merged[step.Key] = step.Value;
        }

        merged.Remove("step");
        if (merged.TryGetValue("proj", out string? mergedProjCode)
            && mergedProjCode.Equals("pipeline", StringComparison.OrdinalIgnoreCase))
        {
            merged.Remove("proj");
        }

        return merged;
    }

    private static bool TryParseOperationArguments(string operation, out Dictionary<string, string> args)
    {
        args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (operation is null)
        {
            return false;
        }

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (token.Length == 0 || token[0] != '+')
            {
                continue;
            }

            string body = token.Substring(1);
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            int index = body.IndexOf('=', StringComparison.Ordinal);
#else
            int index = body.IndexOf('=');
#endif
            if (index < 0)
            {
                args[body] = "true";
            }
            else
            {
                string key = body.Substring(0, index);
                string value = body.Substring(index + 1);
                args[key] = value;
            }
        }

        return args.Count > 0;
    }
}
