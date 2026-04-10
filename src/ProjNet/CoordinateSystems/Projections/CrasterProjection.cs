// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Craster Parabolic projection (<c>crast</c>).
/// </summary>
/// <remarks>
/// Craster Parabolic is a spherical equal-area pseudocylindrical projection introduced by
/// J. E. E. Craster in 1929. The implementation uses the one-third latitude form
/// <c>x = Xm * λ * (2 * cos(2 * φ / 3) - 1)</c> and
/// <c>y = Ym * sin(φ / 3)</c>.
/// </remarks>
internal sealed class CrasterProjection : MapProjection
{
    private const double Xm = 0.97720502380583984317d;
    private const double Rxm = 1.02332670794648848847d;
    private const double Ym = 3.06998012383946546542d;
    private const double Rym = 0.32573500793527994772d;
    private const double Third = ProjectionConstants.OneThird;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrasterProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public CrasterProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CrasterProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public CrasterProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Craster";
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new CrasterProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phiThird = lat * Third;
        double x = Xm * lambda * ((2d * Math.Cos(phiThird + phiThird)) - 1d);
        double y = Ym * Math.Sin(phiThird);
        lon = this.SphericalRadius * x;
        lat = this.SphericalRadius * y;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double phi = 3d * Asinz(yy * Rym);
        double denominator = (2d * Math.Cos((phi + phi) * Third)) - 1d;
        if (Math.Abs(denominator) <= Eps10)
        {
            ArgumentGuard.ThrowArgument("Input data outside projection domain.");
        }

        double lambda = (xx * Rxm) / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
