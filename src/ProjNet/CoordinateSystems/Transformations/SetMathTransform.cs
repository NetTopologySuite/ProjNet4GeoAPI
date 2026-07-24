// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Implements PROJ's <c>set</c> runtime conversion by overriding selected coordinate components.
/// </summary>
/// <remarks>
/// The <c>set</c> operation replaces selected coordinate components with fixed
/// constants and leaves all other ordinates unchanged. This behavior is
/// intentionally idempotent, so PROJ's self-inverse convention is preserved for
/// non-identity instances.
/// </remarks>
/// <seealso href="https://proj.org/en/stable/operations/conversions/set.html">PROJ: set coordinate value.</seealso>
internal sealed class SetMathTransform : MathTransform
{
    private static readonly MathTransform SharedIdentityInverse = new IdentityMathTransform(3);

    private readonly bool hasV1;
    private readonly bool hasV2;
    private readonly bool hasV3;
    private readonly bool hasV4;

    private readonly double v1;
    private readonly double v2;
    private readonly double v3;
    private readonly double v4;

    private SetMathTransform(
        bool hasV1,
        double v1,
        bool hasV2,
        double v2,
        bool hasV3,
        double v3,
        bool hasV4,
        double v4)
    {
        this.hasV1 = hasV1;
        this.v1 = v1;
        this.hasV2 = hasV2;
        this.v2 = v2;
        this.hasV3 = hasV3;
        this.v3 = v3;
        this.hasV4 = hasV4;
        this.v4 = v4;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override bool Identity()
    {
        return !this.hasV1 && !this.hasV2 && !this.hasV3 && !this.hasV4;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this.Identity()
            ? SharedIdentityInverse
            : this;
    }

    /// <inheritdoc />
    public override void Invert()
    {
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.hasV1)
        {
            x = this.v1;
        }

        if (this.hasV2)
        {
            y = this.v2;
        }

        if (this.hasV3)
        {
            z = this.v3;
        }
    }

    /// <summary>
    /// Creates a <see cref="SetMathTransform"/> from parsed PROJ arguments.
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
            skipReason = "set arguments were null.";
            return false;
        }

        bool hasV1 = TryGetOptionalValue(args, "v_1", out double v1, out skipReason);
        if (skipReason is not null)
        {
            return false;
        }

        bool hasV2 = TryGetOptionalValue(args, "v_2", out double v2, out skipReason);
        if (skipReason is not null)
        {
            return false;
        }

        bool hasV3 = TryGetOptionalValue(args, "v_3", out double v3, out skipReason);
        if (skipReason is not null)
        {
            return false;
        }

        bool hasV4 = TryGetOptionalValue(args, "v_4", out double v4, out skipReason);
        if (skipReason is not null)
        {
            return false;
        }

        transform = hasV1 || hasV2 || hasV3 || hasV4
            ? new SetMathTransform(hasV1, v1, hasV2, v2, hasV3, v3, hasV4, v4)
            : new IdentityMathTransform(3);
        return true;
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        this.Transform(ref x, ref y, ref z);

        if (this.hasV4)
        {
            t = this.v4;
        }
    }

    private static bool TryGetOptionalValue(
        Dictionary<string, string> args,
        string key,
        out double value,
        out string? skipReason)
    {
        skipReason = null;
        value = 0d;
        if (!args.TryGetValue(key, out string? token))
        {
            return false;
        }

        if (!SpanParseUtility.TryParseFiniteDouble(token, out value))
        {
            skipReason = $"Invalid value for +{key}.";
            return false;
        }

        return true;
    }
}
