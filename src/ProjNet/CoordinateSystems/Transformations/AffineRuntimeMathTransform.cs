// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Implements PROJ's <c>affine</c> runtime transform.
/// </summary>
[Serializable]
internal sealed class AffineRuntimeMathTransform : MathTransform
{
    private readonly Vector3D offset;
    private readonly double tOffset;
    private readonly Matrix3x3 spatialMatrix;
    private readonly double tScale;
    private MathTransform? inverse;

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
        ArgumentGuard.ThrowIfNotFinite(xOffset, nameof(xOffset), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(yOffset, nameof(yOffset), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(zOffset, nameof(zOffset), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(tOffset, nameof(tOffset), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s11, nameof(s11), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s12, nameof(s12), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s13, nameof(s13), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s21, nameof(s21), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s22, nameof(s22), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s23, nameof(s23), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s31, nameof(s31), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s32, nameof(s32), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(s33, nameof(s33), "Affine parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(tScale, nameof(tScale), "Affine parameters must be finite.");

        this.offset = new Vector3D(xOffset, yOffset, zOffset);
        this.tOffset = tOffset;
        this.spatialMatrix = new Matrix3x3(s11, s12, s13, s21, s22, s23, s31, s32, s33);
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
        [NotNullWhen(true)] out MathTransform? transform,
        out string? skipReason)
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
            if (!affineTransform.TryCreateInverse(out MathTransform? inverseTransformCandidate, out skipReason))
            {
                return false;
            }

            transform = ArgumentGuard.ThrowIfNull(inverseTransformCandidate, nameof(inverseTransformCandidate));
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
        out string? skipReason)
    {
        skipReason = null;
        value = defaultValue;
        if (!args.TryGetValue(key, out string? token))
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

    private static bool TryInvertSpatialMatrix(in Matrix3x3 matrix, out Matrix3x3 inverseMatrix)
    {
        inverseMatrix = Matrix3x3.Zero;

        double c11 = (matrix.M11 * matrix.M22) - (matrix.M12 * matrix.M21);
        double c12 = -((matrix.M10 * matrix.M22) - (matrix.M12 * matrix.M20));
        double c13 = (matrix.M10 * matrix.M21) - (matrix.M11 * matrix.M20);
        double c21 = -((matrix.M01 * matrix.M22) - (matrix.M02 * matrix.M21));
        double c22 = (matrix.M00 * matrix.M22) - (matrix.M02 * matrix.M20);
        double c23 = -((matrix.M00 * matrix.M21) - (matrix.M01 * matrix.M20));
        double c31 = (matrix.M01 * matrix.M12) - (matrix.M02 * matrix.M11);
        double c32 = -((matrix.M00 * matrix.M12) - (matrix.M02 * matrix.M10));
        double c33 = (matrix.M00 * matrix.M11) - (matrix.M01 * matrix.M10);

        double determinant = (matrix.M00 * c11) + (matrix.M01 * c12) + (matrix.M02 * c13);
        if (Math.Abs(determinant) < 1e-30d || double.IsNaN(determinant) || double.IsInfinity(determinant))
        {
            return false;
        }

        double inverseDeterminant = 1d / determinant;
        inverseMatrix = new Matrix3x3(
            c11 * inverseDeterminant,
            c21 * inverseDeterminant,
            c31 * inverseDeterminant,
            c12 * inverseDeterminant,
            c22 * inverseDeterminant,
            c32 * inverseDeterminant,
            c13 * inverseDeterminant,
            c23 * inverseDeterminant,
            c33 * inverseDeterminant);
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
        Vector3D transformed = this.offset + (this.spatialMatrix * new Vector3D(x, y, z));
        x = transformed.X;
        y = transformed.Y;
        z = transformed.Z;
    }

    /// <inheritdoc />
    public override bool Identity()
    {
        return this.offset.X == 0d
            && this.offset.Y == 0d
            && this.offset.Z == 0d
            && this.tOffset == 0d
            && this.spatialMatrix.M00 == 1d
            && this.spatialMatrix.M01 == 0d
            && this.spatialMatrix.M02 == 0d
            && this.spatialMatrix.M10 == 0d
            && this.spatialMatrix.M11 == 1d
            && this.spatialMatrix.M12 == 0d
            && this.spatialMatrix.M20 == 0d
            && this.spatialMatrix.M21 == 0d
            && this.spatialMatrix.M22 == 1d
            && this.tScale == 1d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            if (!this.TryCreateInverse(out MathTransform? inverseTransformCandidate, out string? error))
            {
                throw new InvalidOperationException(error);
            }

            this.inverse = ArgumentGuard.ThrowIfNull(inverseTransformCandidate, nameof(inverseTransformCandidate));
        }

        return ArgumentGuard.ThrowIfNull(this.inverse, nameof(this.inverse));
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

    private bool TryCreateInverse([NotNullWhen(true)] out MathTransform? inverseTransform, out string? error)
    {
        inverseTransform = null;
        error = null;

        if (!TryInvertSpatialMatrix(this.spatialMatrix, out Matrix3x3 inverseMatrix))
        {
            error = "affine: transformation matrix is not invertible.";
            return false;
        }

        if (this.tScale == 0d)
        {
            error = "affine: +tscale must be non-zero for inverse usage.";
            return false;
        }

        Vector3D inverseOffset = -(inverseMatrix * this.offset);
        double inverseTScale = 1d / this.tScale;
        double inverseTOffset = -(this.tOffset * inverseTScale);

        inverseTransform = new AffineRuntimeMathTransform(
            inverseOffset.X,
            inverseOffset.Y,
            inverseOffset.Z,
            inverseTOffset,
            inverseMatrix.M00,
            inverseMatrix.M01,
            inverseMatrix.M02,
            inverseMatrix.M10,
            inverseMatrix.M11,
            inverseMatrix.M12,
            inverseMatrix.M20,
            inverseMatrix.M21,
            inverseMatrix.M22,
            inverseTScale);
        return true;
    }
}
