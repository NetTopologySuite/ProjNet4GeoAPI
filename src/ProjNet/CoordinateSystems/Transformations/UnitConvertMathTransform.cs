// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Represents the documented type.
/// </summary>
[Serializable]
internal sealed class UnitConvertMathTransform : MathTransform
{
    private readonly int dimension;
    private double xyScale;
    private double zScale;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitConvertMathTransform"/> class for 3D coordinates.
    /// </summary>
    /// <param name="xyScale">Scale factor applied to X and Y ordinates.</param>
    /// <param name="zScale">Scale factor applied to Z ordinate.</param>
    internal UnitConvertMathTransform(double xyScale, double zScale)
        : this(3, xyScale, zScale)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitConvertMathTransform"/> class.
    /// </summary>
    /// <param name="dimension">Coordinate dimension (2 or 3).</param>
    /// <param name="xyScale">Scale factor applied to X and Y ordinates.</param>
    /// <param name="zScale">Scale factor applied to Z ordinate when dimension is 3.</param>
    internal UnitConvertMathTransform(int dimension, double xyScale, double zScale)
    {
        this.dimension = ValidateDimension(dimension, nameof(dimension));
        ValidateScale(xyScale, nameof(xyScale));
        ValidateScale(zScale, nameof(zScale));

        this.xyScale = xyScale;
        this.zScale = zScale;
    }

    /// <inheritdoc />
    public override int DimSource => this.dimension;

    /// <inheritdoc />
    public override int DimTarget => this.dimension;

    /// <inheritdoc />
    public override string WKT => throw new NotImplementedException();

    /// <inheritdoc />
    public override string XML => throw new NotImplementedException();

    /// <inheritdoc />
    public override bool Identity()
    {
        bool xyIdentity = this.xyScale.Equals(1d);
        if (this.dimension < 3)
        {
            return xyIdentity;
        }

        return xyIdentity && this.zScale.Equals(1d);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return new UnitConvertMathTransform(this.dimension, 1d / this.xyScale, 1d / this.zScale);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        this.xyScale = 1d / this.xyScale;
        this.zScale = 1d / this.zScale;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        x *= this.xyScale;
        y *= this.xyScale;
        if (this.dimension > 2)
        {
            z *= this.zScale;
        }
    }

    private static int ValidateDimension(int dimension, string parameterName)
    {
        if (dimension < 2 || dimension > 3)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, dimension, "Unit conversion dimension must be either 2 or 3.");
        }

        return dimension;
    }

    private static void ValidateScale(double scale, string parameterName)
    {
        if (scale <= 0d || double.IsNaN(scale) || double.IsInfinity(scale))
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, scale, "Scale must be finite and positive.");
        }
    }
}
