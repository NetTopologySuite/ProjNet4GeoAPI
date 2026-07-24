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
    private static bool TryResolveGeoTiffHorizontalInterpolationOverride(
        Dictionary<string, string> args,
        bool useGridMetadataInterpolation,
        bool allowBiquadraticInterpolation,
        out bool? biquadraticInterpolationOverride,
        out string? skipReason)
    {
        biquadraticInterpolationOverride = useGridMetadataInterpolation ? null : false;
        if (!args.TryGetValue("interpolation", out string? interpolationToken) || string.IsNullOrWhiteSpace(interpolationToken))
        {
            skipReason = null;
            return true;
        }

        if (interpolationToken.Equals("bilinear", StringComparison.OrdinalIgnoreCase))
        {
            biquadraticInterpolationOverride = false;
            skipReason = null;
            return true;
        }

        if (interpolationToken.Equals("biquadratic", StringComparison.OrdinalIgnoreCase))
        {
            if (!allowBiquadraticInterpolation)
            {
                skipReason = "The hgridshift operation only supports bilinear interpolation for GeoTIFF grids.";
                return false;
            }

            biquadraticInterpolationOverride = true;
            skipReason = null;
            return true;
        }

        skipReason = "Horizontal GeoTIFF grid shift interpolation must be bilinear or biquadratic.";
        return false;
    }

    private static bool TryValidateNtv2HorizontalInterpolation(
        Dictionary<string, string> args,
        bool allowBiquadraticInterpolation,
        out string? skipReason)
    {
        if (!args.TryGetValue("interpolation", out string? interpolationToken) || string.IsNullOrWhiteSpace(interpolationToken))
        {
            skipReason = null;
            return true;
        }

        if (interpolationToken.Equals("bilinear", StringComparison.OrdinalIgnoreCase))
        {
            skipReason = null;
            return true;
        }

        if (interpolationToken.Equals("biquadratic", StringComparison.OrdinalIgnoreCase))
        {
            skipReason = allowBiquadraticInterpolation
                ? "Biquadratic interpolation is only supported for GeoTIFF gridshift inputs."
                : "The hgridshift operation only supports bilinear interpolation for NTv2 grids.";
            return false;
        }

        skipReason = "Horizontal grid shift interpolation must be bilinear or biquadratic.";
        return false;
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
                skipReason = $"Grid '{Path.GetFileName(path)}' is not a supported {gridFamilyName} grid format ({allowedList}).";
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
            string gridName = isOptional ? token[1..] : token;
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
                skipReason = $"Required grid '{gridName}' was not found.";
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
            int mapped = c switch
            {
                'e' => 1,
                'w' => -1,
                'n' => 2,
                's' => -2,
                'u' => 3,
                'd' => -3,
                _ => 0,
            };

            if (mapped == 0)
            {
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
                ? token[1..]
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

            string body = token[1..];
            if (body.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNestedPipelineProjection(string operation)
    {
        if (operation is null)
        {
            return false;
        }

        int pipelineProjectionCount = 0;
        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens)
        {
            if (token.Length > 0
                && token[0] == '+'
                && token[1..].Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                pipelineProjectionCount++;
                if (pipelineProjectionCount > 1)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryParsePipelineStepArguments(
        string operation,
        [NotNullWhen(true)] out IReadOnlyList<Dictionary<string, string>>? steps,
        out bool invertPipeline,
        out string? skipReason)
    {
        steps = null;
        invertPipeline = false;
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

            string body = token[1..];
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            int separatorIndex = body.IndexOf('=', StringComparison.Ordinal);
#else
            int separatorIndex = body.IndexOf('=');
#endif
            string key = separatorIndex < 0
                ? body
                : body[..separatorIndex];
            string value = separatorIndex < 0
                ? "true"
                : body[(separatorIndex + 1)..];
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

        invertPipeline = globalArgs.Remove("inv");
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

        if (invertPipeline)
        {
            parsedSteps = BuildInvertedPipelineSteps(parsedSteps);
        }

        steps = parsedSteps;
        return true;
    }

    private static bool TryParseNestedPipelineStepOperations(
        string operation,
        [NotNullWhen(true)] out IReadOnlyList<string>? steps,
        out string? skipReason)
    {
        steps = null;
        skipReason = null;

        string[] tokens = operation.Split(OperationTokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        var globalTokens = new List<string>();
        var parsedSteps = new List<string>();
        List<string>? currentStepTokens = null;
        bool insidePipelineDefinition = false;
        bool insideStepSection = false;
        bool previousTokenWasStep = false;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token.Length == 0 || token[0] != '+')
            {
                if (insideStepSection && currentStepTokens is not null)
                {
                    currentStepTokens.Add(token);
                }

                continue;
            }

            string body = token[1..];
            if (!insidePipelineDefinition)
            {
                if (body.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
                {
                    insidePipelineDefinition = true;
                }

                continue;
            }

            if (body.Equals("step", StringComparison.OrdinalIgnoreCase))
            {
                if (!insideStepSection)
                {
                    insideStepSection = true;
                    currentStepTokens = [];
                }
                else if (currentStepTokens is null || currentStepTokens.Count == 0)
                {
                    skipReason = "Pipeline contains an empty +step.";
                    return false;
                }
                else
                {
                    parsedSteps.Add(BuildPipelineStepOperation(globalTokens, currentStepTokens));
                    currentStepTokens = [];
                }

                previousTokenWasStep = true;
                continue;
            }

            if (!insideStepSection)
            {
                globalTokens.Add(token);
                continue;
            }

            currentStepTokens ??= [];
            currentStepTokens.Add(token);
            previousTokenWasStep = false;

            if (body.Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
            {
                for (int j = i + 1; j < tokens.Length; j++)
                {
                    currentStepTokens.Add(tokens[j]);
                }

                i = tokens.Length;
                break;
            }
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

        bool invertPipeline = RemoveGlobalInvToken(globalTokens);
        if (currentStepTokens is not null && currentStepTokens.Count > 0)
        {
            parsedSteps.Add(BuildPipelineStepOperation(globalTokens, currentStepTokens));
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

        if (invertPipeline)
        {
            parsedSteps = BuildInvertedPipelineStepOperations(parsedSteps);
        }

        steps = parsedSteps;
        return true;
    }

    private static List<Dictionary<string, string>> BuildInvertedPipelineSteps(List<Dictionary<string, string>> parsedSteps)
    {
        var invertedSteps = new List<Dictionary<string, string>>(parsedSteps.Count);
        for (int i = parsedSteps.Count - 1; i >= 0; i--)
        {
            var invertedStep = new Dictionary<string, string>(parsedSteps[i], StringComparer.OrdinalIgnoreCase);
            if (!invertedStep.Remove("inv"))
            {
                invertedStep["inv"] = "true";
            }

            invertedSteps.Add(invertedStep);
        }

        return invertedSteps;
    }

    private static List<string> BuildInvertedPipelineStepOperations(List<string> parsedSteps)
    {
        var invertedSteps = new List<string>(parsedSteps.Count);
        for (int i = parsedSteps.Count - 1; i >= 0; i--)
        {
            string step = parsedSteps[i];
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (step.Contains("+inv", StringComparison.OrdinalIgnoreCase))
            {
                invertedSteps.Add(step.Replace("+inv", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("  ", " ", StringComparison.Ordinal).Trim());
            }
#else
            if (step.IndexOf("+inv", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                invertedSteps.Add(step.Replace("+inv", string.Empty).Replace("  ", " ").Trim());
            }
#endif
            else
            {
                invertedSteps.Add($"+inv {step}");
            }
        }

        return invertedSteps;
    }

    private static Dictionary<string, string> BuildPipelineStepArguments(
        Dictionary<string, string> globalArgs,
        Dictionary<string, string> stepArgs)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> globalArg in globalArgs)
        {
            if (globalArg.Key.Equals("inv", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            merged[globalArg.Key] = globalArg.Value;
        }

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

    private static string BuildPipelineStepOperation(List<string> globalTokens, List<string> stepTokens)
    {
        string globalPrefix = string.Join(" ", globalTokens);
        string stepOperation = string.Join(" ", stepTokens);
        if (stepTokens.Count > 0
            && stepTokens[0].Length > 1
            && stepTokens[0][0] == '+'
            && stepTokens[0][1..].Equals("proj=pipeline", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(globalPrefix))
            {
                return stepOperation;
            }

            string nestedStepRemainder = stepTokens.Count > 1
                ? string.Join(" ", stepTokens.GetRange(1, stepTokens.Count - 1))
                : string.Empty;
            return string.IsNullOrEmpty(nestedStepRemainder)
                ? $"{stepTokens[0]} {globalPrefix}"
                : $"{stepTokens[0]} {globalPrefix} {nestedStepRemainder}";
        }

        return string.IsNullOrEmpty(globalPrefix)
            ? stepOperation
            : $"{globalPrefix} {stepOperation}";
    }

    private static bool RemoveGlobalInvToken(List<string> globalTokens)
    {
        for (int i = globalTokens.Count - 1; i >= 0; i--)
        {
            if (globalTokens[i].Equals("+inv", StringComparison.OrdinalIgnoreCase))
            {
                globalTokens.RemoveAt(i);
                return true;
            }
        }

        return false;
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

            string body = token[1..];
#if NET8_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            int index = body.IndexOf('=', StringComparison.Ordinal);
#else
            int index = body.IndexOf('=');
#endif
            if (index < 0)
            {
                args[body] = body;
            }
            else
            {
                string key = body[..index];
                string value = body[(index + 1)..];
                args[key] = value;
            }
        }

        return args.Count > 0;
    }
}
