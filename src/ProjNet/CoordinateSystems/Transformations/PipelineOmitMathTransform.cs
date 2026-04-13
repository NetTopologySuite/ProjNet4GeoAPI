// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Wraps a pipeline step and conditionally omits it for forward and inverse traversal.
/// </summary>
/// <remarks>
/// Pipeline omission wraps an inner step but can turn that step into an
/// effective identity in the forward direction, the inverse direction, or both.
/// As a result, <see cref="Identity()"/> is intentionally direction-dependent
/// and reflects whether the current forward traversal skips the wrapped step.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/pipeline.html">PROJ: pipeline operator.</seealso>
internal sealed class PipelineOmitMathTransform : MathTransform
{
    private readonly MathTransform inner;
    private readonly bool skipForward;
    private readonly bool skipInverse;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineOmitMathTransform"/> class.
    /// </summary>
    /// <param name="inner">Inner step transform.</param>
    /// <param name="skipForward">Whether to skip the step in forward traversal.</param>
    /// <param name="skipInverse">Whether to skip the step in inverse traversal.</param>
    internal PipelineOmitMathTransform(MathTransform inner, bool skipForward, bool skipInverse)
    {
        this.inner = ArgumentGuard.ThrowIfNull(inner, nameof(inner));
        this.skipForward = skipForward;
        this.skipInverse = skipInverse;
    }

    /// <inheritdoc />
    public override int DimSource => this.inner.DimSource;

    /// <inheritdoc />
    public override int DimTarget => this.inner.DimTarget;

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool Identity()
    {
        return this.skipForward || this.inner.Identity();
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new PipelineOmitMathTransform(this.inner.Inverse(), this.skipInverse, this.skipForward);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("Pipeline omit transform does not support in-place inversion.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.skipForward)
        {
            return;
        }

        this.inner.Transform(ref x, ref y, ref z);
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        if (this.skipForward)
        {
            return;
        }

        this.inner.Transform(ref x, ref y, ref z, ref t);
    }
}
