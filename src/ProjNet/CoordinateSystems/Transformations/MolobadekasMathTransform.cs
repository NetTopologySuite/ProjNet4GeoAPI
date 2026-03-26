// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Implements PROJ's <c>molobadekas</c> runtime transform.
/// </summary>
[Serializable]
internal sealed class MolobadekasMathTransform : MathTransform
{
    private const double ArcSecondToRadians = Math.PI / (180d * 3600d);

    private readonly double translationX;
    private readonly double translationY;
    private readonly double translationZ;
    private readonly double scalePpm;
    private readonly double pivotX;
    private readonly double pivotY;
    private readonly double pivotZ;
    private readonly double rotationX;
    private readonly double rotationY;
    private readonly double rotationZ;
    private readonly double rotationM00;
    private readonly double rotationM01;
    private readonly double rotationM02;
    private readonly double rotationM10;
    private readonly double rotationM11;
    private readonly double rotationM12;
    private readonly double rotationM20;
    private readonly double rotationM21;
    private readonly double rotationM22;

    private bool isInverted;
    private MathTransform inverse;

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
        ValidateFiniteValue(translationX, nameof(translationX));
        ValidateFiniteValue(translationY, nameof(translationY));
        ValidateFiniteValue(translationZ, nameof(translationZ));
        ValidateFiniteValue(rotationXArcSeconds, nameof(rotationXArcSeconds));
        ValidateFiniteValue(rotationYArcSeconds, nameof(rotationYArcSeconds));
        ValidateFiniteValue(rotationZArcSeconds, nameof(rotationZArcSeconds));
        ValidateFiniteValue(scalePpm, nameof(scalePpm));
        ValidateFiniteValue(pivotX, nameof(pivotX));
        ValidateFiniteValue(pivotY, nameof(pivotY));
        ValidateFiniteValue(pivotZ, nameof(pivotZ));

        this.translationX = translationX;
        this.translationY = translationY;
        this.translationZ = translationZ;
        this.scalePpm = scalePpm;
        this.pivotX = pivotX;
        this.pivotY = pivotY;
        this.pivotZ = pivotZ;
        this.rotationX = rotationXArcSeconds * ArcSecondToRadians;
        this.rotationY = rotationYArcSeconds * ArcSecondToRadians;
        this.rotationZ = rotationZArcSeconds * ArcSecondToRadians;
        this.isInverted = isInverted;

        BuildRotationMatrix(
            this.rotationX,
            this.rotationY,
            this.rotationZ,
            isPositionVector,
            out this.rotationM00,
            out this.rotationM01,
            out this.rotationM02,
            out this.rotationM10,
            out this.rotationM11,
            out this.rotationM12,
            out this.rotationM20,
            out this.rotationM21,
            out this.rotationM22);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MolobadekasMathTransform"/> class
    /// as an inverted clone.
    /// </summary>
    /// <param name="source">Source instance to clone.</param>
    /// <param name="isInverted">Whether to apply inverse direction in the clone.</param>
    private MolobadekasMathTransform(MolobadekasMathTransform source, bool isInverted)
    {
        this.translationX = source.translationX;
        this.translationY = source.translationY;
        this.translationZ = source.translationZ;
        this.scalePpm = source.scalePpm;
        this.pivotX = source.pivotX;
        this.pivotY = source.pivotY;
        this.pivotZ = source.pivotZ;
        this.rotationX = source.rotationX;
        this.rotationY = source.rotationY;
        this.rotationZ = source.rotationZ;
        this.rotationM00 = source.rotationM00;
        this.rotationM01 = source.rotationM01;
        this.rotationM02 = source.rotationM02;
        this.rotationM10 = source.rotationM10;
        this.rotationM11 = source.rotationM11;
        this.rotationM12 = source.rotationM12;
        this.rotationM20 = source.rotationM20;
        this.rotationM21 = source.rotationM21;
        this.rotationM22 = source.rotationM22;
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
        return this.translationX == 0d
            && this.translationY == 0d
            && this.translationZ == 0d
            && this.rotationX == 0d
            && this.rotationY == 0d
            && this.rotationZ == 0d
            && this.scalePpm == 0d;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            this.inverse = new MolobadekasMathTransform(this, !this.isInverted);
        }

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
        out MathTransform transform,
        out string skipReason)
    {
        transform = default!;
        skipReason = default!;

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

    private static bool TryGetOptionalDouble(
        Dictionary<string, string> args,
        string key,
        out double value,
        out string skipReason)
    {
        value = 0d;
        skipReason = default!;
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

    private static void BuildRotationMatrix(
        double rotationX,
        double rotationY,
        double rotationZ,
        bool isPositionVector,
        out double r00,
        out double r01,
        out double r02,
        out double r10,
        out double r11,
        out double r12,
        out double r20,
        out double r21,
        out double r22)
    {
        r00 = 1d;
        r01 = rotationZ;
        r02 = -rotationY;
        r10 = -rotationZ;
        r11 = 1d;
        r12 = rotationX;
        r20 = rotationY;
        r21 = -rotationX;
        r22 = 1d;

        if (!isPositionVector)
        {
            return;
        }

        Swap(ref r01, ref r10);
        Swap(ref r02, ref r20);
        Swap(ref r12, ref r21);
    }

    private static void Swap(ref double left, ref double right)
    {
        double value = left;
        left = right;
        right = value;
    }

    private static void ValidateFiniteValue(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, value, "Molobadekas parameters must be finite.");
        }
    }

    private void TransformForward(ref double x, ref double y, ref double z)
    {
        double sourceX = x - this.pivotX;
        double sourceY = y - this.pivotY;
        double sourceZ = z - this.pivotZ;
        double scaleFactor = 1d + (this.scalePpm * 1e-6d);
        x = this.translationX + this.pivotX + (scaleFactor * ((this.rotationM00 * sourceX) + (this.rotationM01 * sourceY) + (this.rotationM02 * sourceZ)));
        y = this.translationY + this.pivotY + (scaleFactor * ((this.rotationM10 * sourceX) + (this.rotationM11 * sourceY) + (this.rotationM12 * sourceZ)));
        z = this.translationZ + this.pivotZ + (scaleFactor * ((this.rotationM20 * sourceX) + (this.rotationM21 * sourceY) + (this.rotationM22 * sourceZ)));
    }

    private void TransformInverse(ref double x, ref double y, ref double z)
    {
        double scaleFactor = 1d + (this.scalePpm * 1e-6d);
        double sourceX = (x - this.translationX - this.pivotX) / scaleFactor;
        double sourceY = (y - this.translationY - this.pivotY) / scaleFactor;
        double sourceZ = (z - this.translationZ - this.pivotZ) / scaleFactor;
        x = this.pivotX + (this.rotationM00 * sourceX) + (this.rotationM10 * sourceY) + (this.rotationM20 * sourceZ);
        y = this.pivotY + (this.rotationM01 * sourceX) + (this.rotationM11 * sourceY) + (this.rotationM21 * sourceZ);
        z = this.pivotZ + (this.rotationM02 * sourceX) + (this.rotationM12 * sourceY) + (this.rotationM22 * sourceZ);
    }
}
