// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical August Epicycloidal projection (<c>august</c>).
/// </summary>
/// <remarks>
/// Inverse projection is not supported.
/// </remarks>
internal class AugustProjection : MapProjection
{
    private const double M = 1.333333333333333d;

    private readonly double radius;

    /// <summary>
    /// Initializes a new instance of the <see cref="AugustProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public AugustProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AugustProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public AugustProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "August_Epicycloidal";
        this.radius = this.semiMajor * this.scaleFactor;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new AugustProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = Math.Tan(0.5d * lat);
        double c1 = Math.Sqrt(1d - (t * t));
        lambda *= 0.5d;
        double c = 1d + (c1 * Math.Cos(lambda));
        if (Math.Abs(c) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double x1 = Math.Sin(lambda) * c1 / c;
        double y1 = t / c;
        double x12 = x1 * x1;
        double y12 = y1 * y1;

        lon = this.radius * (M * x1 * (3d + x12 - (3d * y12)));
        lat = this.radius * (M * y1 * (3d + (3d * x12) - y12));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        throw new InvalidOperationException("August Epicycloidal does not support inverse projection in this wave.");
    }
}
