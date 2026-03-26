// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

#nullable enable annotations
namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Reorders and optionally flips coordinate ordinates according to axis mapping rules.
/// </summary>
[Serializable]
internal sealed class AxisSwapMathTransform : MathTransform
{
    private readonly int dimension;
    private int xSourceIndex;
    private int ySourceIndex;
    private int zSourceIndex;
    private int xSign;
    private int ySign;
    private int zSign;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxisSwapMathTransform"/> class.
    /// </summary>
    /// <param name="dimension">Coordinate dimension (2 or 3).</param>
    /// <param name="xSourceIndex">Source ordinate index used for X output.</param>
    /// <param name="xSign">Sign multiplier for X output.</param>
    /// <param name="ySourceIndex">Source ordinate index used for Y output.</param>
    /// <param name="ySign">Sign multiplier for Y output.</param>
    /// <param name="zSourceIndex">Source ordinate index used for Z output.</param>
    /// <param name="zSign">Sign multiplier for Z output.</param>
    internal AxisSwapMathTransform(
        int dimension,
        int xSourceIndex,
        int xSign,
        int ySourceIndex,
        int ySign,
        int zSourceIndex,
        int zSign)
    {
        this.dimension = ValidateDimension(dimension, nameof(dimension));
        this.xSourceIndex = ValidateSourceIndex(xSourceIndex, nameof(xSourceIndex));
        this.ySourceIndex = ValidateSourceIndex(ySourceIndex, nameof(ySourceIndex));
        this.zSourceIndex = ValidateSourceIndex(zSourceIndex, nameof(zSourceIndex));

        this.xSign = ValidateSign(xSign, nameof(xSign));
        this.ySign = ValidateSign(ySign, nameof(ySign));
        this.zSign = ValidateSign(zSign, nameof(zSign));
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
        bool xyIdentity = this.xSourceIndex == 0
            && this.xSign == 1
            && this.ySourceIndex == 1
            && this.ySign == 1;

        if (this.dimension < 3)
        {
            return xyIdentity;
        }

        return xyIdentity
            && this.zSourceIndex == 2
            && this.zSign == 1;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        int[] sourceIndices = [this.xSourceIndex, this.ySourceIndex, this.zSourceIndex];
        int[] targetSigns = [this.xSign, this.ySign, this.zSign];

        int[] inverseSourceIndices = [0, 1, 2];
        int[] inverseSigns = [1, 1, 1];
        for (int targetIndex = 0; targetIndex < 3; targetIndex++)
        {
            int sourceIndex = sourceIndices[targetIndex];
            inverseSourceIndices[sourceIndex] = targetIndex;
            inverseSigns[sourceIndex] = targetSigns[targetIndex];
        }

        return new AxisSwapMathTransform(
            this.dimension,
            inverseSourceIndices[0],
            inverseSigns[0],
            inverseSourceIndices[1],
            inverseSigns[1],
            inverseSourceIndices[2],
            inverseSigns[2]);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        int[] sourceIndices = [this.xSourceIndex, this.ySourceIndex, this.zSourceIndex];
        int[] targetSigns = [this.xSign, this.ySign, this.zSign];

        int[] inverseSourceIndices = [0, 1, 2];
        int[] inverseSigns = [1, 1, 1];
        for (int targetIndex = 0; targetIndex < 3; targetIndex++)
        {
            int sourceIndex = sourceIndices[targetIndex];
            inverseSourceIndices[sourceIndex] = targetIndex;
            inverseSigns[sourceIndex] = targetSigns[targetIndex];
        }

        this.xSourceIndex = inverseSourceIndices[0];
        this.xSign = inverseSigns[0];
        this.ySourceIndex = inverseSourceIndices[1];
        this.ySign = inverseSigns[1];
        this.zSourceIndex = inverseSourceIndices[2];
        this.zSign = inverseSigns[2];
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        double[] source = [x, y, z];
        x = source[this.xSourceIndex] * this.xSign;
        y = source[this.ySourceIndex] * this.ySign;
        if (this.dimension > 2)
        {
            z = source[this.zSourceIndex] * this.zSign;
        }
    }

    private static int ValidateDimension(int dimension, string parameterName)
    {
        if (dimension is < 2 or > 3)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, dimension, "Axis swap dimension must be either 2 or 3.");
        }

        return dimension;
    }

    private static int ValidateSourceIndex(int sourceIndex, string parameterName)
    {
        if (sourceIndex is < 0 or > 2)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, sourceIndex, "Axis source index must be 0, 1 or 2.");
        }

        return sourceIndex;
    }

    private static int ValidateSign(int sign, string parameterName)
    {
        if (sign != -1 && sign != 1)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, sign, "Axis sign must be either -1 or 1.");
        }

        return sign;
    }
}
