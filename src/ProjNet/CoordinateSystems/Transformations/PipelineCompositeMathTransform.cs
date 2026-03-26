// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Executes a PROJ pipeline chain while preserving shared pipeline state between steps.
/// </summary>
[Serializable]
internal sealed class PipelineCompositeMathTransform : MathTransform
{
    private readonly MathTransform[] transforms;
    private readonly PipelineExecutionContext executionContext;
    private MathTransform inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineCompositeMathTransform"/> class.
    /// </summary>
    /// <param name="transforms">Ordered transform chain executed from first to last.</param>
    /// <param name="executionContext">Pipeline execution context shared across steps.</param>
    internal PipelineCompositeMathTransform(IReadOnlyList<MathTransform> transforms, PipelineExecutionContext executionContext)
    {
        transforms = ArgumentGuard.ThrowIfNull(transforms, nameof(transforms));
        this.executionContext = ArgumentGuard.ThrowIfNull(executionContext, nameof(executionContext));

        if (transforms.Count == 0)
        {
            ArgumentGuard.ThrowArgument("At least one math transform is required.", nameof(transforms));
        }

        this.transforms = new MathTransform[transforms.Count];
        for (int i = 0; i < transforms.Count; i++)
        {
            if (transforms[i] is null)
            {
                ArgumentGuard.ThrowArgument("Math transform list contains null element.", nameof(transforms));
            }

            this.transforms[i] = transforms[i];
        }

    }

    /// <inheritdoc />
    public override int DimSource => this.transforms[0].DimSource;

    /// <inheritdoc />
    public override int DimTarget => this.transforms[this.transforms.Length - 1].DimTarget;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool Identity()
    {
        for (int i = 0; i < this.transforms.Length; i++)
        {
            if (!this.transforms[i].Identity())
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (!(this.inverse is null))
        {
            return this.inverse;
        }

        var inverted = new MathTransform[this.transforms.Length];
        int output = 0;
        for (int i = this.transforms.Length - 1; i >= 0; i--)
        {
            inverted[output] = this.transforms[i].Inverse();
            output++;
        }

        this.inverse = new PipelineCompositeMathTransform(inverted, this.executionContext);
        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("Pipeline composite transform does not support in-place inversion.");
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

    private void TransformCore(ref double x, ref double y, ref double z, ref double t, bool includeTime)
    {
        this.executionContext.Clear();

        for (int i = 0; i < this.transforms.Length; i++)
        {
            if (includeTime)
            {
                this.transforms[i].Transform(ref x, ref y, ref z, ref t);
            }
            else
            {
                this.transforms[i].Transform(ref x, ref y, ref z);
            }
        }
    }
}
