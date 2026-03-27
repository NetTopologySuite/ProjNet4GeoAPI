// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Miller Cylindrical projection (<c>mill</c>).
/// </summary>
/// <remarks>
/// A compromise cylindrical projection that reduces the high-latitude area exaggeration
/// of the Mercator projection by compressing the latitude formula. Poles cannot be projected.
/// </remarks>
[Serializable]
internal class MillerCylindricalProjection : MapProjection
{
    private readonly double radius;
    private readonly double inverseRadius;

    /// <summary>
    /// Initializes a new instance of the <see cref="MillerCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public MillerCylindricalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MillerCylindricalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public MillerCylindricalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Miller_Cylindrical";
        this.radius = this.semiMajor * this.scaleFactor;
        this.inverseRadius = 1d / this.radius;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new MillerCylindricalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        if (double.IsNaN(lon) || double.IsNaN(lat))
        {
            lon = double.NaN;
            lat = double.NaN;
            return;
        }

        if (Math.Abs(Math.Abs(lat) - HalfPi) <= Epsln)
        {
            ArgumentGuard.ThrowArgument("Transformation cannot be computed at the poles.");
        }

        double lambda = Adjust_lon(lon - this.centralMeridian);
        lon = this.radius * lambda;
        lat = this.radius * 1.25d * Math.Log(Math.Tan(FortPi + (0.4d * lat)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        x = Adjust_lon(this.centralMeridian + (x * this.inverseRadius));
        y = 2.5d * (Math.Atan(Math.Exp((0.8d * y) * this.inverseRadius)) - FortPi);
    }
}
