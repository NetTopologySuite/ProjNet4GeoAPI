// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Projections;

/// <summary>
/// Creates runtime math transforms from PROJ-style pipeline operation strings.
/// </summary>
internal static partial class ProjPipelineMathTransformFactory
{
    private const int MaxNestedPipelineDepth = 4;
    private static readonly char[] CommaSeparator = [','];
    private static readonly char[] OperationTokenSeparators = [' ', '\t'];
    private static readonly string[] HorizontalGridExtensions = [".gsb", ".tif", ".tiff"];
    private static readonly string[] VerticalGridExtensions = [".gtx", ".tif", ".tiff"];
    private static readonly string[] XyzGridExtensions = [".tif", ".tiff"];
    private static readonly Dictionary<string, TryCreateDispatchedStepTransform> StepTransformDispatch = new(StringComparer.OrdinalIgnoreCase)
    {
        ["latlong"] = WrapDirect(TryCreateGeographicIdentityTransform),
        ["longlat"] = WrapDirect(TryCreateGeographicIdentityTransform),
        ["latlon"] = WrapDirect(TryCreateGeographicIdentityTransform),
        ["lonlat"] = WrapDirect(TryCreateGeographicIdentityTransform),
        ["noop"] = TryCreateNoOpStepTransform,
        ["geocent"] = WrapDirect(TryCreateGeocentricCartesianTransform),
        ["cart"] = WrapDirect(TryCreateGeocentricCartesianTransform),
        ["geoc"] = WrapDirect(TryCreateGeocentricLatitudeTransform),
        ["geogoffset"] = WrapDirect(GeogOffsetMathTransform.TryCreate),
        ["affine"] = WrapDirect(AffineRuntimeMathTransform.TryCreate),
        ["push"] = TryCreatePushStepTransform,
        ["pop"] = TryCreatePopStepTransform,
        ["set"] = WrapDirect(SetMathTransform.TryCreate),
        ["axisswap"] = WrapDirect(TryCreateAxisSwapTransform),
        ["unitconvert"] = WrapDirect(TryCreateUnitConvertTransform),
        ["hgridshift"] = TryCreateHGridShiftStepTransform,
        ["gridshift"] = TryCreateGridShiftStepTransform,
        ["vgridshift"] = WrapDirect(TryCreateVerticalGridShiftTransform),
        ["xyzgridshift"] = WrapDirect(TryCreateXyzGridShiftTransform),
        ["defmodel"] = WrapDirect(DefModelMathTransform.TryCreate),
        ["deformation"] = WrapDirect(DeformationMathTransform.TryCreate),
        ["tinshift"] = WrapDirect(TinShiftMathTransform.TryCreate),
        ["topocentric"] = WrapDirect(TopocentricMathTransform.TryCreate),
        ["vertoffset"] = WrapDirect(VertOffsetMathTransform.TryCreate),
        ["helmert"] = WrapDirect(HelmertMathTransform.TryCreate),
        ["molobadekas"] = WrapDirect(MolobadekasMathTransform.TryCreate),
        ["molodensky"] = WrapDirect(MolodenskyMathTransform.TryCreate),
        ["horner"] = WrapDirect(HornerMathTransform.TryCreate),
        ["ob_tran"] = WrapDirect(ObTranMathTransform.TryCreate),
        ["sch"] = WrapDirect(SchMathTransform.TryCreate),
        ["spherical_cross_track_height"] = WrapDirect(SchMathTransform.TryCreate),
    };

    private delegate bool TryCreateDirectStepTransform(
        Dictionary<string, string> args,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason);

    private delegate bool TryCreateDispatchedStepTransform(
        Dictionary<string, string> args,
        PipelineExecutionContext? executionContext,
        out MathTransform? transform,
        out string? skipReason);

    /// <summary>
    /// Tries to create an executable transform from a full operation or pipeline definition.
    /// </summary>
    /// <param name="operation">Operation text to parse.</param>
    /// <param name="transform">Created transform when parsing succeeds.</param>
    /// <param name="skipReason">Reason why transform creation was skipped.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreateMathTransform(string operation, [NotNullWhen(true)] out MathTransform? transform, out string? skipReason)
        => TryCreateMathTransform(operation, 0, out transform, out skipReason);

    private static bool TryCreateMathTransform(string operation, int pipelineDepth, [NotNullWhen(true)] out MathTransform? transform, out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (operation is null)
        {
            skipReason = "Operation string was null.";
            return false;
        }

        if (pipelineDepth > MaxNestedPipelineDepth)
        {
            skipReason = $"Nested pipeline depth exceeded the supported maximum of {MaxNestedPipelineDepth.ToString(CultureInfo.InvariantCulture)}.";
            return false;
        }

        bool hasPipeline = ContainsPipelineProjection(operation);
        IReadOnlyList<Dictionary<string, string>>? pipelineStepArguments = null;
        IReadOnlyList<string>? nestedPipelineStepOperations = null;
        if (hasPipeline && ContainsNestedPipelineProjection(operation))
        {
            if (!TryParseNestedPipelineStepOperations(operation, out nestedPipelineStepOperations, out skipReason))
            {
                return false;
            }
        }
        else if (hasPipeline
            && !TryParsePipelineStepArguments(operation, out pipelineStepArguments, out _, out skipReason))
        {
            return false;
        }

        IReadOnlyList<Dictionary<string, string>> parsedPipelineSteps = hasPipeline && nestedPipelineStepOperations is null
            ? ArgumentGuard.ThrowIfNull(pipelineStepArguments, nameof(pipelineStepArguments))
            : [];

        int stepCount = hasPipeline
            ? nestedPipelineStepOperations?.Count ?? parsedPipelineSteps.Count
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
                ? nestedPipelineStepOperations is not null
                    ? TryCreateStepTransform(nestedPipelineStepOperations[i], executionContext, pipelineDepth, out stepTransformCandidate, out stepSkipReason)
                    : TryCreateStepTransform(parsedPipelineSteps[i], executionContext, out stepTransformCandidate, out stepSkipReason)
                : TryCreateStepTransform(operation, executionContext, pipelineDepth, out stepTransformCandidate, out stepSkipReason);
            if (!ok)
            {
                skipReason = hasPipeline
                    ? $"Pipeline step {(i + 1).ToString(CultureInfo.InvariantCulture)} failed: {stepSkipReason ?? "unknown reason"}"
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
        int pipelineDepth,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        if (ContainsPipelineProjection(operation))
        {
            return TryCreateMathTransform(operation, pipelineDepth + 1, out transform, out skipReason);
        }

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

        if (StepTransformDispatch.TryGetValue(projCode, out TryCreateDispatchedStepTransform? stepFactory))
        {
            if (!stepFactory(args, executionContext, out transform, out skipReason))
            {
                return false;
            }

            transform = WrapWithOmitFlags(ArgumentGuard.ThrowIfNull(transform, nameof(transform)), omitForward, omitInverse);
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

        skipReason = $"Projection '{projCode}' is not part of the current builtins wave.";
        return false;
    }

    private static MathTransform WrapWithOmitFlags(MathTransform stepTransform, bool omitForward, bool omitInverse)
    {
        return omitForward || omitInverse
            ? new PipelineOmitMathTransform(stepTransform, omitForward, omitInverse)
            : stepTransform;
    }

    private static TryCreateDispatchedStepTransform WrapDirect(TryCreateDirectStepTransform factory)
    {
        return (Dictionary<string, string> args, PipelineExecutionContext? _, out MathTransform? transform, out string? skipReason) =>
            factory(args, out transform, out skipReason);
    }
}
