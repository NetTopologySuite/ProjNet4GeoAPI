// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ProjNet.CoordinateSystems.Transformations.Numerics;

/// <summary>
/// Implements PROJ's <c>molobadekas</c> runtime transform.
/// </summary>
/// <remarks>
/// <para>Molodensky-Badekas is a 10-parameter similarity transform that augments the
/// standard Helmert model with a rotation pivot. It is commonly used when rotations are
/// defined about a local network centroid rather than the geocentric origin.</para>
/// <para>The formulation was independently verified against IOGP, "Geomatics Guidance
/// Note 7, part 2: Coordinate Conversions and Transformations including Formulas"
/// (publication 373-7-2, 2019), EPSG methods 1034 and 1061. The
/// <c>X' = T + P + (1 + s) * R * (X - P)</c> structure, including the explicit pivot
/// point translation, matches the implementation here.</para>
/// </remarks>
/// <seealso href="https://epsg.io/1034-method">EPSG method 1034: Molodensky-Badekas (geocentric domain).</seealso>
/// <seealso href="https://epsg.io/1061-method">EPSG method 1061: Molodensky-Badekas (geographic domain).</seealso>
internal sealed class MolobadekasMathTransform : MathTransform
{
    private const double ArcSecondToRadians = Math.PI / (180d * 3600d);

    private readonly Vector3D translation;
    private readonly double scalePpm;
    private readonly Vector3D pivot;
    private readonly Vector3D rotationRadians;
    private readonly Matrix3x3 rotationMatrix;

    private bool isInverted;
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="MolobadekasMathTransform"/> class.
    /// </summary>
    /// <param name="translationX">X translation (metres).</param>
    /// <param name="translationY">Y translation (metres).</param>
    /// <param name="translationZ">Z translation (metres).</param>
    /// <param name="rotationXArcSeconds">X rotation (arc-seconds).</param>
    /// <param name="rotationYArcSeconds">Y rotation (arc-seconds).</param>
    /// <param name="rotationZArcSeconds">Z rotation (arc-seconds).</param>
    /// <param name="scalePpm">Scale (ppm).</param>
    /// <param name="pivotX">Reference point X (metres).</param>
    /// <param name="pivotY">Reference point Y (metres).</param>
    /// <param name="pivotZ">Reference point Z (metres).</param>
    /// <param name="isPositionVector">Whether rotations use position-vector convention.</param>
    /// <param name="isInverted">Whether the transform runs in inverse direction.</param>
    private MolobadekasMathTransform(
        double translationX,
        double translationY,
        double translationZ,
        double rotationXArcSeconds,
        double rotationYArcSeconds,
        double rotationZArcSeconds,
        double scalePpm,
        double pivotX,
        double pivotY,
        double pivotZ,
        bool isPositionVector,
        bool isInverted)
    {
        ArgumentGuard.ThrowIfNotFinite(translationX, nameof(translationX), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(translationY, nameof(translationY), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(translationZ, nameof(translationZ), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(rotationXArcSeconds, nameof(rotationXArcSeconds), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(rotationYArcSeconds, nameof(rotationYArcSeconds), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(rotationZArcSeconds, nameof(rotationZArcSeconds), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(scalePpm, nameof(scalePpm), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(pivotX, nameof(pivotX), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(pivotY, nameof(pivotY), "Molobadekas parameters must be finite.");
        ArgumentGuard.ThrowIfNotFinite(pivotZ, nameof(pivotZ), "Molobadekas parameters must be finite.");

        this.translation = new Vector3D(translationX, translationY, translationZ);
        this.scalePpm = scalePpm;
        this.pivot = new Vector3D(pivotX, pivotY, pivotZ);
        this.rotationRadians = new Vector3D(
            rotationXArcSeconds * ArcSecondToRadians,
            rotationYArcSeconds * ArcSecondToRadians,
            rotationZArcSeconds * ArcSecondToRadians);
        this.isInverted = isInverted;

        this.rotationMatrix = BuildRotationMatrix(this.rotationRadians, isPositionVector);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MolobadekasMathTransform"/> class
    /// as an inverted clone.
    /// </summary>
    /// <param name="source">Source instance to clone.</param>
    /// <param name="isInverted">Whether to apply inverse direction in the clone.</param>
    private MolobadekasMathTransform(MolobadekasMathTransform source, bool isInverted)
    {
        this.translation = source.translation;
        this.scalePpm = source.scalePpm;
        this.pivot = source.pivot;
        this.rotationRadians = source.rotationRadians;
        this.rotationMatrix = source.rotationMatrix;
        this.isInverted = isInverted;
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
        return this.translation.X == 0d
            && this.translation.Y == 0d
            && this.translation.Z == 0d
            && this.rotationRadians.X == 0d
            && this.rotationRadians.Y == 0d
            && this.rotationRadians.Z == 0d
            && this.scalePpm == 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new MolobadekasMathTransform(this, !this.isInverted);

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Invert()
    {
        this.isInverted = !this.isInverted;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.isInverted)
        {
            this.TransformInverse(ref x, ref y, ref z);
            return;
        }

        this.TransformForward(ref x, ref y, ref z);
    }

    /// <summary>
    /// Creates a <see cref="MolobadekasMathTransform"/> from parsed PROJ arguments.
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
            skipReason = "molobadekas arguments were null.";
            return false;
        }

        if (!args.TryGetValue("convention", out string? conventionToken)
            || string.IsNullOrWhiteSpace(conventionToken))
        {
            skipReason = "molobadekas: missing 'convention' argument";
            return false;
        }

        bool isPositionVector;
        if (conventionToken.Equals("position_vector", StringComparison.OrdinalIgnoreCase))
        {
            isPositionVector = true;
        }
        else if (conventionToken.Equals("coordinate_frame", StringComparison.OrdinalIgnoreCase))
        {
            isPositionVector = false;
        }
        else
        {
            skipReason = "molobadekas: invalid value for 'convention' argument";
            return false;
        }

        if (!TryGetOptionalDouble(args, "x", out double translationX, out skipReason)
            || !TryGetOptionalDouble(args, "y", out double translationY, out skipReason)
            || !TryGetOptionalDouble(args, "z", out double translationZ, out skipReason)
            || !TryGetOptionalDouble(args, "s", out double scalePpm, out skipReason)
            || !TryGetOptionalDouble(args, "rx", out double rotationXArcSeconds, out skipReason)
            || !TryGetOptionalDouble(args, "ry", out double rotationYArcSeconds, out skipReason)
            || !TryGetOptionalDouble(args, "rz", out double rotationZArcSeconds, out skipReason)
            || !TryGetOptionalDouble(args, "px", out double pivotX, out skipReason)
            || !TryGetOptionalDouble(args, "py", out double pivotY, out skipReason)
            || !TryGetOptionalDouble(args, "pz", out double pivotZ, out skipReason))
        {
            return false;
        }

        if (scalePpm <= -1e6d)
        {
            skipReason = "molobadekas: invalid value for s.";
            return false;
        }

        bool hasAnyParameters = translationX != 0d
            || translationY != 0d
            || translationZ != 0d
            || scalePpm != 0d
            || rotationXArcSeconds != 0d
            || rotationYArcSeconds != 0d
            || rotationZArcSeconds != 0d
            || pivotX != 0d
            || pivotY != 0d
            || pivotZ != 0d;

        transform = hasAnyParameters
            ? new MolobadekasMathTransform(
                translationX,
                translationY,
                translationZ,
                rotationXArcSeconds,
                rotationYArcSeconds,
                rotationZArcSeconds,
                scalePpm,
                pivotX,
                pivotY,
                pivotZ,
                isPositionVector,
                false)
            : new IdentityMathTransform(3);

        if (args.ContainsKey("inv"))
        {
            transform = transform.Inverse();
        }

        return true;
    }

    /// <summary>
    /// Creates a Molodensky-Badekas transform from resolved numeric parameters.
    /// </summary>
    /// <param name="translationX">X translation in metres.</param>
    /// <param name="translationY">Y translation in metres.</param>
    /// <param name="translationZ">Z translation in metres.</param>
    /// <param name="rotationXArcSeconds">X rotation in arc-seconds.</param>
    /// <param name="rotationYArcSeconds">Y rotation in arc-seconds.</param>
    /// <param name="rotationZArcSeconds">Z rotation in arc-seconds.</param>
    /// <param name="scalePpm">Scale difference in ppm.</param>
    /// <param name="pivotX">Pivot X ordinate in metres.</param>
    /// <param name="pivotY">Pivot Y ordinate in metres.</param>
    /// <param name="pivotZ">Pivot Z ordinate in metres.</param>
    /// <param name="isPositionVector"><see langword="true"/> for the position-vector convention.</param>
    /// <param name="isInverted"><see langword="true"/> to create the inverse direction.</param>
    /// <returns>The created transform.</returns>
    internal static MathTransform Create(
        double translationX,
        double translationY,
        double translationZ,
        double rotationXArcSeconds,
        double rotationYArcSeconds,
        double rotationZArcSeconds,
        double scalePpm,
        double pivotX,
        double pivotY,
        double pivotZ,
        bool isPositionVector,
        bool isInverted = false)
    {
        bool hasAnyParameters = translationX != 0d
            || translationY != 0d
            || translationZ != 0d
            || scalePpm != 0d
            || rotationXArcSeconds != 0d
            || rotationYArcSeconds != 0d
            || rotationZArcSeconds != 0d
            || pivotX != 0d
            || pivotY != 0d
            || pivotZ != 0d;

        MathTransform transform = hasAnyParameters
            ? new MolobadekasMathTransform(
                translationX,
                translationY,
                translationZ,
                rotationXArcSeconds,
                rotationYArcSeconds,
                rotationZArcSeconds,
                scalePpm,
                pivotX,
                pivotY,
                pivotZ,
                isPositionVector,
                false)
            : new IdentityMathTransform(3);
        return isInverted ? transform.Inverse() : transform;
    }

    private static bool TryGetOptionalDouble(
        Dictionary<string, string> args,
        string key,
        out double value,
        out string? skipReason)
    {
        value = 0d;
        skipReason = null;
        if (!args.TryGetValue(key, out string? token))
        {
            return true;
        }

        if (!TryParseFiniteDouble(token, out value))
        {
            skipReason = $"Invalid value for +{key}.";
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

    private static Matrix3x3 BuildRotationMatrix(Vector3D rotation, bool isPositionVector)
    {
        var coordinateFrameMatrix = new Matrix3x3(
            1d,
            rotation.Z,
            -rotation.Y,
            -rotation.Z,
            1d,
            rotation.X,
            rotation.Y,
            -rotation.X,
            1d);

        return isPositionVector ? coordinateFrameMatrix.Transpose() : coordinateFrameMatrix;
    }

    private void TransformForward(ref double x, ref double y, ref double z)
    {
        Vector3D source = new Vector3D(x, y, z) - this.pivot;
        double scaleFactor = 1d + (this.scalePpm * 1e-6d);
        Vector3D transformed = this.translation + this.pivot + ((this.rotationMatrix * source) * scaleFactor);
        x = transformed.X;
        y = transformed.Y;
        z = transformed.Z;
    }

    private void TransformInverse(ref double x, ref double y, ref double z)
    {
        double scaleFactor = 1d + (this.scalePpm * 1e-6d);
        Vector3D source = (new Vector3D(x, y, z) - this.translation - this.pivot) / scaleFactor;
        Vector3D transformed = this.pivot + (this.rotationMatrix.Transpose() * source);
        x = transformed.X;
        y = transformed.Y;
        z = transformed.Z;
    }
}
