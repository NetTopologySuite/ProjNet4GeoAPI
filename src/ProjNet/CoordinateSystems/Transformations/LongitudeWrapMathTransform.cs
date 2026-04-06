// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Transformations;

using System;

/// <summary>
/// Normalizes longitudes around a configured wrap center.
/// </summary>
/// <remarks>
/// The transform leaves all ordinates unchanged except longitude, which is mapped
/// into the half-open interval [wrapCenter - 180, wrapCenter + 180).
/// </remarks>
internal sealed class LongitudeWrapMathTransform : MathTransform
{
    private readonly double wrapCenterDegrees;

    /// <summary>
    /// Initializes a new instance of the <see cref="LongitudeWrapMathTransform"/> class.
    /// </summary>
    /// <param name="wrapCenterDegrees">Longitude wrap center in degrees.</param>
    internal LongitudeWrapMathTransform(double wrapCenterDegrees)
    {
        ArgumentGuard.ThrowIfNotFinite(wrapCenterDegrees, nameof(wrapCenterDegrees), "Longitude wrap center must be finite.");
        this.wrapCenterDegrees = wrapCenterDegrees;
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
        return false;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        return this;
    }

    /// <inheritdoc />
    public override void Invert()
    {
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        x = WrapLongitude(x, this.wrapCenterDegrees);
    }

    private static double WrapLongitude(double longitudeDegrees, double wrapCenterDegrees)
    {
        double wrapped = longitudeDegrees;
        double lowerBound = wrapCenterDegrees - 180d;
        double upperBound = wrapCenterDegrees + 180d;

        while (wrapped < lowerBound)
        {
            wrapped += 360d;
        }

        while (wrapped >= upperBound)
        {
            wrapped -= 360d;
        }

        return wrapped;
    }
}
