// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Implements PROJ pipeline stack transfer steps (<c>push</c> and <c>pop</c>).
/// </summary>
[Serializable]
internal sealed class PipelineStackTransferMathTransform : MathTransform
{
    private readonly bool isPush;
    private readonly bool[] enabledOrdinateFlags;
    private readonly PipelineExecutionContext executionContext;

    private PipelineStackTransferMathTransform(bool isPush, bool[] enabledOrdinateFlags, PipelineExecutionContext executionContext)
    {
        this.isPush = isPush;
        this.enabledOrdinateFlags = enabledOrdinateFlags;
        this.executionContext = executionContext;
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
    public override bool Identity()
    {
        return false;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return new PipelineStackTransferMathTransform(!this.isPush, (bool[])this.enabledOrdinateFlags.Clone(), this.executionContext);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("Pipeline stack transfer transform does not support in-place inversion.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        double t = 0d;
        this.TransformCore(ref x, ref y, ref z, ref t, includeTime: false);
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        this.TransformCore(ref x, ref y, ref z, ref t, includeTime: true);
    }

    /// <summary>
    /// Creates a runtime <c>push</c> stack transfer transform.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="executionContext">Pipeline execution context.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreatePush(
        Dictionary<string, string> args,
        PipelineExecutionContext executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        return TryCreate(args, executionContext, isPush: true, out transform, out skipReason);
    }

    /// <summary>
    /// Creates a runtime <c>pop</c> stack transfer transform.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="executionContext">Pipeline execution context.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreatePop(
        Dictionary<string, string> args,
        PipelineExecutionContext executionContext,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        return TryCreate(args, executionContext, isPush: false, out transform, out skipReason);
    }

    private static bool TryCreate(
        Dictionary<string, string> args,
        PipelineExecutionContext executionContext,
        bool isPush,
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
    {
        transform = null;
        skipReason = null;

        if (args is null)
        {
            skipReason = "Pipeline stack transfer arguments were null.";
            return false;
        }

        if (!TryParseEnabledOrdinateFlags(args, out bool[] enabledOrdinateFlags, out skipReason))
        {
            return false;
        }

        if (executionContext is null)
        {
            transform = new IdentityMathTransform(3);
            return true;
        }

        transform = new PipelineStackTransferMathTransform(isPush, enabledOrdinateFlags, executionContext);
        return true;
    }

    private static bool TryParseEnabledOrdinateFlags(
        Dictionary<string, string> args,
        out bool[] enabledOrdinateFlags,
        out string? skipReason)
    {
        enabledOrdinateFlags = [false, false, false, false];
        skipReason = null;

        if (args.TryGetValue("v_1", out string? v1Token) && !IsBooleanFlag(v1Token))
        {
            skipReason = "push/pop does not accept values for +v_1; use +v_1 as a flag.";
            return false;
        }

        if (args.TryGetValue("v_2", out string? v2Token) && !IsBooleanFlag(v2Token))
        {
            skipReason = "push/pop does not accept values for +v_2; use +v_2 as a flag.";
            return false;
        }

        if (args.TryGetValue("v_3", out string? v3Token) && !IsBooleanFlag(v3Token))
        {
            skipReason = "push/pop does not accept values for +v_3; use +v_3 as a flag.";
            return false;
        }

        if (args.TryGetValue("v_4", out string? v4Token) && !IsBooleanFlag(v4Token))
        {
            skipReason = "push/pop does not accept values for +v_4; use +v_4 as a flag.";
            return false;
        }

        enabledOrdinateFlags[0] = args.ContainsKey("v_1");
        enabledOrdinateFlags[1] = args.ContainsKey("v_2");
        enabledOrdinateFlags[2] = args.ContainsKey("v_3");
        enabledOrdinateFlags[3] = args.ContainsKey("v_4");

        if (!enabledOrdinateFlags[0] && !enabledOrdinateFlags[1] && !enabledOrdinateFlags[2] && !enabledOrdinateFlags[3])
        {
            skipReason = "push/pop requires at least one of +v_1, +v_2, +v_3 or +v_4.";
            return false;
        }

        return true;
    }

    private static bool IsBooleanFlag(string token)
    {
        return string.IsNullOrEmpty(token)
            || token.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private void TransformCore(ref double x, ref double y, ref double z, ref double t, bool includeTime)
    {
        if (this.isPush)
        {
            if (this.enabledOrdinateFlags[0])
            {
                this.executionContext.Push(0, x);
            }

            if (this.enabledOrdinateFlags[1])
            {
                this.executionContext.Push(1, y);
            }

            if (this.enabledOrdinateFlags[2])
            {
                this.executionContext.Push(2, z);
            }

            if (includeTime && this.enabledOrdinateFlags[3])
            {
                this.executionContext.Push(3, t);
            }

            return;
        }

        if (this.enabledOrdinateFlags[0] && this.executionContext.TryPop(0, out double xValue))
        {
            x = xValue;
        }

        if (this.enabledOrdinateFlags[1] && this.executionContext.TryPop(1, out double yValue))
        {
            y = yValue;
        }

        if (this.enabledOrdinateFlags[2] && this.executionContext.TryPop(2, out double zValue))
        {
            z = zValue;
        }

        if (includeTime && this.enabledOrdinateFlags[3] && this.executionContext.TryPop(3, out double tValue))
        {
            t = tValue;
        }
    }
}
