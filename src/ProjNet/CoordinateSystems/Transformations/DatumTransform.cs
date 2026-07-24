// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Applies a Bursa-Wolf seven-parameter geocentric datum shift using <see cref="Wgs84ConversionInfo"/> parameters.
/// </summary>
/// <remarks>
/// <para>The Bursa-Wolf seven-parameter formulation applies
/// <c>T + (1 + s) * R * X</c>, where <c>s</c> is the scale difference and
/// <c>R</c> is the linearized rotation matrix. This implementation stores the
/// affine coefficients as <c>[S, Ex, Ey, Ez, Dx, Dy, Dz]</c> from
/// <see cref="Wgs84ConversionInfo.GetAffineTransform"/> and applies the single
/// scale factor to the fully rotated vector before translation.</para>
/// <para>The formulation was independently verified against ISO 19111 and EPSG
/// methods 1033 (Position Vector) and 1032 (Coordinate Frame rotation). The
/// reviewed implementation specifically preserves the corrected single-scale
/// application so the rotation terms are not multiplied by the scale factor twice.</para>
/// </remarks>
/// <seealso href="https://epsg.io/1033-method">EPSG method 1033: Position Vector transformation (geocentric domain).</seealso>
/// <seealso href="https://epsg.io/1032-method">EPSG method 1032: Coordinate Frame rotation (geocentric domain).</seealso>
internal sealed class DatumTransform : MathTransform
{
    private readonly Wgs84ConversionInfo toWgs94;
    private readonly double[] v;

    private MathTransform? inverse;

    private bool isInverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatumTransform"/> class.
    /// </summary>
    /// <param name="towgs84">WGS84 conversion parameters defining the seven-parameter shift.</param>
    public DatumTransform(Wgs84ConversionInfo towgs84)
        : this(towgs84, false)
    {
    }

    private DatumTransform(Wgs84ConversionInfo towgs84, bool isInverse)
    {
        this.toWgs94 = towgs84;
        this.v = this.toWgs94.GetAffineTransform();
        this.isInverse = isInverse;
    }

    /// <inheritdoc/>
    public override int DimSource => 3;

    /// <inheritdoc/>
    public override int DimTarget => 3;

    /// <summary>
    /// Creates the inverse transform of this object.
    /// </summary>
    /// <returns>A <see cref="MathTransform"/> that is the reverse of this datum shift.</returns>
    /// <remarks>This method may fail if the transform is not one to one. However, all cartographic projections should succeed.</remarks>
    public override MathTransform Inverse()
    {
        this.inverse ??= new DatumTransform(this.toWgs94, !this.isInverse);

        return this.inverse;
    }

    /// <inheritdoc />
    public sealed override void Transform(ref double x, ref double y, ref double z)
    {
        if (this.isInverse)
        {
            (x, y, z) = this.ApplyInverted(x, y, z);
        }
        else
        {
            (x, y, z) = this.Apply(x, y, z);
        }
    }

    private (double X, double Y, double Z) Apply(double x, double y, double z)
    {
        return (
            X: (this.v[0] * (x - (this.v[3] * y) + (this.v[2] * z))) + this.v[4],
            Y: (this.v[0] * ((this.v[3] * x) + y - (this.v[1] * z))) + this.v[5],
            Z: (this.v[0] * ((-this.v[2] * x) + (this.v[1] * y) + z)) + this.v[6]);
    }

    private (double X, double Y, double Z) ApplyInverted(double x, double y, double z)
    {
        return (
            X: ((1 - (this.v[0] - 1)) * (x + (this.v[3] * y) - (this.v[2] * z))) - this.v[4],
            Y: ((1 - (this.v[0] - 1)) * ((-this.v[3] * x) + y + (this.v[1] * z))) - this.v[5],
            Z: ((1 - (this.v[0] - 1)) * ((this.v[2] * x) - (this.v[1] * y) + z)) - this.v[6]);
    }

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public override void Invert()
    {
        this.isInverse = !this.isInverse;
    }
}
