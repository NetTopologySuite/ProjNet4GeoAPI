// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Reorders and optionally flips coordinate ordinates according to axis mapping rules.
/// </summary>
/// <remarks>
/// This transform is a pure signed permutation of the input ordinates. Each
/// output axis selects one source ordinate and optionally multiplies it by
/// <c>-1</c>, so the overall mapping is equivalent to a permutation matrix with
/// diagonal sign changes.
/// </remarks>
internal sealed class AxisSwapMathTransform : MathTransform
{
    private readonly int dimension;
    private int xSourceIndex;
    private int ySourceIndex;
    private int zSourceIndex;
    private int tSourceIndex;
    private int xSign;
    private int ySign;
    private int zSign;
    private int tSign;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxisSwapMathTransform"/> class.
    /// </summary>
    /// <param name="dimension">Coordinate dimension (2, 3 or 4).</param>
    /// <param name="xSourceIndex">Source ordinate index used for X output.</param>
    /// <param name="xSign">Sign multiplier for X output.</param>
    /// <param name="ySourceIndex">Source ordinate index used for Y output.</param>
    /// <param name="ySign">Sign multiplier for Y output.</param>
    /// <param name="zSourceIndex">Source ordinate index used for Z output.</param>
    /// <param name="zSign">Sign multiplier for Z output.</param>
    /// <param name="tSourceIndex">Source ordinate index used for T output.</param>
    /// <param name="tSign">Sign multiplier for T output.</param>
    internal AxisSwapMathTransform(
        int dimension,
        int xSourceIndex,
        int xSign,
        int ySourceIndex,
        int ySign,
        int zSourceIndex,
        int zSign,
        int tSourceIndex,
        int tSign)
    {
        this.dimension = ValidateDimension(dimension, nameof(dimension));
        this.xSourceIndex = ValidateSourceIndex(xSourceIndex, nameof(xSourceIndex));
        this.ySourceIndex = ValidateSourceIndex(ySourceIndex, nameof(ySourceIndex));
        this.zSourceIndex = ValidateSourceIndex(zSourceIndex, nameof(zSourceIndex));
        this.tSourceIndex = ValidateSourceIndex(tSourceIndex, nameof(tSourceIndex));

        this.xSign = ValidateSign(xSign, nameof(xSign));
        this.ySign = ValidateSign(ySign, nameof(ySign));
        this.zSign = ValidateSign(zSign, nameof(zSign));
        this.tSign = ValidateSign(tSign, nameof(tSign));
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

        bool xyzIdentity = xyIdentity
            && this.zSourceIndex == 2
            && this.zSign == 1;

        return this.dimension < 4
            ? xyzIdentity
            : xyzIdentity
            && this.tSourceIndex == 3
            && this.tSign == 1;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        int[] sourceIndices = [this.xSourceIndex, this.ySourceIndex, this.zSourceIndex, this.tSourceIndex];
        int[] targetSigns = [this.xSign, this.ySign, this.zSign, this.tSign];

        int[] inverseSourceIndices = [0, 1, 2, 3];
        int[] inverseSigns = [1, 1, 1, 1];
        for (int targetIndex = 0; targetIndex < this.dimension; targetIndex++)
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
            inverseSigns[2],
            inverseSourceIndices[3],
            inverseSigns[3]);
    }

    /// <inheritdoc />
    public override void Invert()
    {
        int[] sourceIndices = [this.xSourceIndex, this.ySourceIndex, this.zSourceIndex, this.tSourceIndex];
        int[] targetSigns = [this.xSign, this.ySign, this.zSign, this.tSign];

        int[] inverseSourceIndices = [0, 1, 2, 3];
        int[] inverseSigns = [1, 1, 1, 1];
        for (int targetIndex = 0; targetIndex < this.dimension; targetIndex++)
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
        this.tSourceIndex = inverseSourceIndices[3];
        this.tSign = inverseSigns[3];
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        double sourceX = x;
        double sourceY = y;
        double sourceZ = z;
        x = GetSourceValue(this.xSourceIndex, sourceX, sourceY, sourceZ, 0d) * this.xSign;
        y = GetSourceValue(this.ySourceIndex, sourceX, sourceY, sourceZ, 0d) * this.ySign;
        if (this.dimension > 2)
        {
            z = GetSourceValue(this.zSourceIndex, sourceX, sourceY, sourceZ, 0d) * this.zSign;
        }
    }

    /// <inheritdoc />
    internal override void Transform(ref double x, ref double y, ref double z, ref double t)
    {
        double sourceX = x;
        double sourceY = y;
        double sourceZ = z;
        double sourceT = t;

        x = GetSourceValue(this.xSourceIndex, sourceX, sourceY, sourceZ, sourceT) * this.xSign;
        y = GetSourceValue(this.ySourceIndex, sourceX, sourceY, sourceZ, sourceT) * this.ySign;
        if (this.dimension > 2)
        {
            z = GetSourceValue(this.zSourceIndex, sourceX, sourceY, sourceZ, sourceT) * this.zSign;
        }

        if (this.dimension > 3)
        {
            t = GetSourceValue(this.tSourceIndex, sourceX, sourceY, sourceZ, sourceT) * this.tSign;
        }
    }

    private static int ValidateDimension(int dimension, string parameterName)
    {
        if (dimension is < 2 or > 4)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, dimension, "Axis swap dimension must be either 2, 3 or 4.");
        }

        return dimension;
    }

    private static int ValidateSourceIndex(int sourceIndex, string parameterName)
    {
        if (sourceIndex is < 0 or > 3)
        {
            ArgumentGuard.ThrowArgumentOutOfRange(parameterName, sourceIndex, "Axis source index must be 0, 1, 2 or 3.");
        }

        return sourceIndex;
    }

    private static double GetSourceValue(int sourceIndex, double x, double y, double z, double t)
    {
        return sourceIndex switch
        {
            0 => x,
            1 => y,
            2 => z,
            3 => t,
            _ => throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Axis source index must be between 0 and 3."),
        };
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
