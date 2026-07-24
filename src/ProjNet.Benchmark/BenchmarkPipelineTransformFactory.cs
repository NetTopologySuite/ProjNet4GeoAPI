// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Benchmark;

using System;
using System.Reflection;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Creates pipeline transforms for benchmark scenarios by calling the internal runtime pipeline factory.
/// </summary>
internal static class BenchmarkPipelineTransformFactory
{
    private static readonly MethodInfo CreatePipelineTransformMethod = ResolveCreatePipelineTransformMethod();

    /// <summary>
    /// Creates a runtime pipeline transform for the supplied PROJ operation string.
    /// </summary>
    /// <param name="operation">The PROJ pipeline or single-step operation string to resolve.</param>
    /// <returns>The created math transform.</returns>
    public static MathTransform Create(string operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        object?[] arguments = [operation, null, null];
        bool ok = (bool)(CreatePipelineTransformMethod.Invoke(null, arguments) ?? false);
        if (!ok)
        {
            throw new InvalidOperationException(arguments[2] as string ?? "Pipeline transform creation failed.");
        }

        return arguments[1] as MathTransform
            ?? throw new InvalidOperationException("Pipeline transform factory returned null transform.");
    }

    private static MethodInfo ResolveCreatePipelineTransformMethod()
    {
        Type pipelineFactoryType = typeof(MathTransform).Assembly.GetType(
            "ProjNet.CoordinateSystems.Transformations.ProjPipelineMathTransformFactory",
            throwOnError: true)
            ?? throw new InvalidOperationException("Unable to resolve ProjPipelineMathTransformFactory type.");

        return pipelineFactoryType.GetMethod(
            "TryCreateMathTransform",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types:
            [
                typeof(string),
                typeof(MathTransform).MakeByRefType(),
                typeof(string).MakeByRefType(),
            ],
            modifiers: null)
            ?? throw new InvalidOperationException("Unable to resolve ProjPipelineMathTransformFactory.TryCreateMathTransform.");
    }
}
