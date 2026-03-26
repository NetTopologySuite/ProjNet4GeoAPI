// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Implements PROJ's <c>affine</c> runtime transform.
/// </summary>
[Serializable]
internal sealed class AffineRuntimeMathTransform : MathTransform
{
    private readonly double xOffset;
    private readonly double yOffset;
    private readonly double zOffset;
    private readonly double tOffset;
    private readonly double s11;
    private readonly double s12;
    private readonly double s13;
    private readonly double s21;
    private readonly double s22;
    private readonly double s23;
    private readonly double s31;
    private readonly double s32;
    private readonly double s33;
    private readonly double tScale;
    private MathTransform inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="AffineRuntimeMathTransform"/> class.
    /// </summary>
    /// <param name="xOffset">Offset applied to X.</param>
    /// <param name="yOffset">Offset applied to Y.</param>
    /// <param name="zOffset">Offset applied to Z.</param>
    /// <param name="tOffset">Offset applied to T.</param>
    /// <param name="s11">Spatial matrix term S11.</param>
    /// <param name="s12">Spatial matrix term S12.</param>
    /// <param name="s13">Spatial matrix term S13.</param>
    /// <param name="s21">Spatial matrix term S21.</param>
    /// <param name="s22">Spatial matrix term S22.</param>
    /// <param name="s23">Spatial matrix term S23.</param>
    /// <param name="s31">Spatial matrix term S31.</param>
    /// <param name="s32">Spatial matrix term S32.</param>
    /// <param name="s33">Spatial matrix term S33.</param>
    /// <param name="tScale">Time scale multiplier.</param>
    private AffineRuntimeMathTransform(
        double xOffset,
        double yOffset,
        double zOffset,
        double tOffset,
        double s11,
        double s12,
        double s13,
        double s21,
        double s22,
        double s23,
        double s31,
        double s32,
        double s33,
        double tScale)
    {
        ValidateFiniteValue(xOffset, nameof(xOffset));
        ValidateFiniteValue(yOffset, nameof(yOffset));
        ValidateFiniteValue(zOffset, nameof(zOffset));
        ValidateFiniteValue(tOffset, nameof(tOffset));
        ValidateFiniteValue(s11, nameof(s11));
        ValidateFiniteValue(s12, nameof(s12));
        ValidateFiniteValue(s13, nameof(s13));
        ValidateFiniteValue(s21, nameof(s21));
        ValidateFiniteValue(s22, nameof(s22));
        ValidateFiniteValue(s23, nameof(s23));
        ValidateFiniteValue(s31, nameof(s31));
        ValidateFiniteValue(s32, nameof(s32));
        ValidateFiniteValue(s33, nameof(s33));
        ValidateFiniteValue(tScale, nameof(tScale));

        this.xOffset = xOffset;
        this.yOffset = yOffset;
        this.zOffset = zOffset;
        this.tOffset = tOffset;
        this.s11 = s11;
        this.s12 = s12;
        this.s13 = s13;
        this.s21 = s21;
        this.s22 = s22;
        this.s23 = s23;
        this.s31 = s31;
        this.s32 = s32;
        this.s33 = s33;
        this.tScale = tScale;
    }

    /// <inheritdoc />
    public override int DimSource => 3;

    /// <inheritdoc />
    public override int DimTarget => 3;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <summary>
    /// Creates an <see cref="AffineRuntimeMathTransform"/> from parsed PROJ arguments.
    /// </summary>
    /// <param name="args">Parsed PROJ argument dictionary.</param>
    /// <param name="transform">Created transform instance on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when a transform was created.</returns>
    internal static bool TryCreate(
        Dictionary<string, string> args,
        out MathTransform transform,
        out string skipReason)
    {
        transform = null;
        skipReason = null;

        if (args is null)
        {
            skipReason = "affine arguments were null.";
            return false;
        }

        if (!TryGetOptionalDouble(args, "xoff", 0d, out double xOffset, out skipReason)
            || !TryGetOptionalDouble(args, "yoff", 0d, out double yOffset, out skipReason)
            || !TryGetOptionalDouble(args, "zoff", 0d, out double zOffset, out skipReason)
            || !TryGetOptionalDouble(args, "toff", 0d, out double tOffset, out skipReason)
            || !TryGetOptionalDouble(args, "s11", 1d, out double s11, out skipReason)
            || !TryGetOptionalDouble(args, "s12", 0d, out double s12, out skipReason)
            || !TryGetOptionalDouble(args, "s13", 0d, out double s13, out skipReason)
            || !TryGetOptionalDouble(args, "s21", 0d, out double s21, out skipReason)
            || !TryGetOptionalDouble(args, "s22", 1d, out double s22, out skipReason)
            || !TryGetOptionalDouble(args, "s23", 0d, out double s23, out skipReason)
            || !TryGetOptionalDouble(args, "s31", 0d, out double s31, out skipReason)
            || !TryGetOptionalDouble(args, "s32", 0d, out double s32, out skipReason)
            || !TryGetOptionalDouble(args, "s33", 1d, out double s33, out skipReason)
            || !TryGetOptionalDouble(args, "tscale", 1d, out double tScale, out skipReason))
        {
            return false;
        }

        var affine = new AffineRuntimeMathTransform(
            xOffset,
            yOffset,
            zOffset,
            tOffset,
            s11,
            s12,
            s13,
            s21,
            s22,
            s23,
            s31,
            s32,
            s33,
            tScale);

        transform = affine.Identity()
            ? new IdentityMathTransform(3)
            : affine;

        if (args.ContainsKey("inv")
            && !transform.Identity()
            && transform is AffineRuntimeMathTransform affineTransform)
        {
            if (!affineTransform.TryCreateInverse(out MathTransform inverseTransform, out skipReason))
            {
                return false;
            }

            transform = inverseTransform;
        }

        return true;
    }

    /// <summary>
    /// Tries to read a finite optional double argument.
    /// </summary>
    /// <param name="args">Parsed argument dictionary.</param>
    /// <param name="key">Argument key without leading plus sign.</param>
    /// <param name="defaultValue">Fallback value when key does not exist.</param>
    /// <param name="value">Parsed numeric value on success.</param>
    /// <param name="skipReason">Failure reason when creation is not possible.</param>
    /// <returns><see langword="true"/> when parsing succeeded or the key is absent.</returns>
    private static bool TryGetOptionalDouble(
        Dictionary<string, string> args,
        string key,
        double defaultValue,
        out double value,
        out string skipReason)
    {
        skipReason = null;
        value = defaultValue;
        if (!args.TryGetValue(key, out string token))
        {
            return true;
        }

        if (!TryParseFiniteDouble(token, out value))
        {
            skipReason = "Invalid value for +" + key + ".";
            return false;
        }

        return true;
    }

    private static bool TryParseFiniteDouble(string token, out double value)
    {
        value = 0d;
        return !string.IsNullOrWhiteSpace(token)
            && double.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value)
            && !double.IsInfinity(value);
    }

    private static void ValidateFiniteValue(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, value, "Affine parameters must be finite.");
        }
    }

    private static bool TryInvertSpatialMatrix(
        double m11,
        double m12,
        double m13,
        double m21,
        double m22,
        double m23,
        double m31,
        double m32,
        double m33,
        out double i11,
        out double i12,
        out double i13,
        out double i21,
        out double i22,
        out double i23,
        out double i31,
        out double i32,
        out double i33)
    {
        i11 = 0d;
        i12 = 0d;
        i13 = 0d;
        i21 = 0d;
        i22 = 0d;
        i23 = 0d;
        i31 = 0d;
        i32 = 0d;
        i33 = 0d;

        double c11 = (m22 * m33) - (m23 * m32);
        double c12 = -((m21 * m33) - (m23 * m31));
        double c13 = (m21 * m32) - (m22 * m31);
        double c21 = -((m12 * m33) - (m13 * m32));
        double c22 = (m11 * m33) - (m13 * m31);
        double c23 = -((m11 * m32) - (m12 * m31));
        double c31 = (m12 * m23) - (m13 * m22);
        double c32 = -((m11 * m23) - (m13 * m21));
        double c33 = (m11 * m22) - (m12 * m21);

        double determinant = (m11 * c11) + (m12 * c12) + (m13 * c13);
        if (Math.Abs(determinant) < 1e-30d || double.IsNaN(determinant) || double.IsInfinity(determinant))
        {
            return false;
        }

        double inverseDeterminant = 1d / determinant;
        i11 = c11 * inverseDeterminant;
        i12 = c21 * inverseDeterminant;
        i13 = c31 * inverseDeterminant;
        i21 = c12 * inverseDeterminant;
        i22 = c22 * inverseDeterminant;
        i23 = c32 * inverseDeterminant;
        i31 = c13 * inverseDeterminant;
        i32 = c23 * inverseDeterminant;
        i33 = c33 * inverseDeterminant;
        return true;
    }

    /// <summary>
    /// Adds this transform's affine offsets and coefficients to a coordinate.
    /// </summary>
    /// <param name="x">X ordinate to transform.</param>
    /// <param name="y">Y ordinate to transform.</param>
    /// <param name="z">Z ordinate to transform.</param>
    private void ApplySpatialTransform(ref double x, ref double y, ref double z)
    {
        double sourceX = x;
        double sourceY = y;
        double sourceZ = z;
        x = this.xOffset + (this.s11 * sourceX) + (this.s12 * sourceY) + (this.s13 * sourceZ);
        y = this.yOffset + (this.s21 * sourceX) + (this.s22 * sourceY) + (this.s23 * sourceZ);
        z = this.zOffset + (this.s31 * sourceX) + (this.s32 * sourceY) + (this.s33 * sourceZ);
    }

    /// <inheritdoc />
    public override bool Identity()
    {
        return this.xOffset == 0d
            && this.yOffset == 0d
            && this.zOffset == 0d
            && this.tOffset == 0d
            && this.s11 == 1d
            && this.s12 == 0d
            && this.s13 == 0d
            && this.s21 == 0d
            && this.s22 == 1d
            && this.s23 == 0d
            && this.s31 == 0d
            && this.s32 == 0d
            && this.s33 == 1d
            && this.tScale == 1d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            if (!this.TryCreateInverse(out MathTransform inverseTransform, out string error))
            {
                throw new InvalidOperationException(error);
            }

            this.inverse = inverseTransform;
        }

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        throw new NotSupportedException("Affine runtime transform does not support in-place inversion.");
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        this.ApplySpatialTransform(ref x, ref y, ref z);
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        this.ApplySpatialTransform(ref x, ref y, ref z);
        t = this.tOffset + (this.tScale * t);
    }

    private bool TryCreateInverse(out MathTransform inverseTransform, out string error)
    {
        inverseTransform = null;
        error = null;

        if (!TryInvertSpatialMatrix(
                this.s11,
                this.s12,
                this.s13,
                this.s21,
                this.s22,
                this.s23,
                this.s31,
                this.s32,
                this.s33,
                out double i11,
                out double i12,
                out double i13,
                out double i21,
                out double i22,
                out double i23,
                out double i31,
                out double i32,
                out double i33))
        {
            error = "affine: transformation matrix is not invertible.";
            return false;
        }

        if (this.tScale == 0d)
        {
            error = "affine: +tscale must be non-zero for inverse usage.";
            return false;
        }

        double inverseXOffset = -((i11 * this.xOffset) + (i12 * this.yOffset) + (i13 * this.zOffset));
        double inverseYOffset = -((i21 * this.xOffset) + (i22 * this.yOffset) + (i23 * this.zOffset));
        double inverseZOffset = -((i31 * this.xOffset) + (i32 * this.yOffset) + (i33 * this.zOffset));
        double inverseTScale = 1d / this.tScale;
        double inverseTOffset = -(this.tOffset * inverseTScale);

        inverseTransform = new AffineRuntimeMathTransform(
            inverseXOffset,
            inverseYOffset,
            inverseZOffset,
            inverseTOffset,
            i11,
            i12,
            i13,
            i21,
            i22,
            i23,
            i31,
            i32,
            i33,
            inverseTScale);
        return true;
    }
}
