// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;

/// <summary>
/// Composes multiple math transforms into a single sequential transform.
/// </summary>
[Serializable]
internal sealed class CompositeMathTransform : MathTransform
{
    private MathTransform[] transforms;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeMathTransform"/> class.
    /// </summary>
    /// <param name="transforms">Ordered transform chain executed from first to last.</param>
    internal CompositeMathTransform(IReadOnlyList<MathTransform> transforms)
    {
        transforms = ArgumentGuard.ThrowIfNull(transforms, nameof(transforms));
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

        this.inverse = new CompositeMathTransform(inverted);
        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        Array.Reverse(this.transforms);
        for (int i = 0; i < this.transforms.Length; i++)
        {
            this.transforms[i].Invert();
        }

        this.inverse = null;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        for (int i = 0; i < this.transforms.Length; i++)
        {
            this.transforms[i].Transform(ref x, ref y, ref z);
        }
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        for (int i = 0; i < this.transforms.Length; i++)
        {
            this.transforms[i].Transform(ref x, ref y, ref z, ref t);
        }
    }
}
