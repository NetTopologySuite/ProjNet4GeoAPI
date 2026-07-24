// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical General Sinusoidal series projection (<c>gn_sinu</c>).
/// </summary>
/// <remarks>
/// General Sinusoidal is a parameterized spherical pseudocylindrical base described by
/// Snyder for several related map projections. The parameters <c>m</c> and <c>n</c> control
/// whether the auxiliary latitude is obtained directly through <c>asin(n * sin(φ))</c>
/// or by solving <c>m * φ' + sin(φ') = n * sin(φ)</c> iteratively, after which the
/// implementation applies the scaled relations
/// <c>x = cX * λ * (m + cos(φ'))</c> and <c>y = cY * φ'</c>.
/// </remarks>
internal class GeneralSinusoidalProjection : MapProjection
{
    private const int MaximumIterations = 8;

    private readonly double m;
    private readonly double n;
    private readonly double cX;
    private readonly double cY;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public GeneralSinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneralSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public GeneralSinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "General_Sinusoidal";
        this.n = this.Parameters.GetParameterValue("n");
        this.m = this.Parameters.GetParameterValue("m");

        if (this.n <= 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for n: it should be > 0.", nameof(parameters));
        }

        if (this.m < 0d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for m: it should be >= 0.", nameof(parameters));
        }

        this.cY = Math.Sqrt((this.m + 1d) / this.n);
        this.cX = this.cY / (this.m + 1d);
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new GeneralSinusoidalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double phi = lat;
        if (this.m == 0d)
        {
            phi = this.n != 1d ? Asinz(this.n * Math.Sin(phi)) : phi;
        }
        else
        {
            double k = this.n * Math.Sin(phi);
            int i = MaximumIterations;
            for (; i > 0; i--)
            {
                double denominator = this.m + Math.Cos(phi);
                if (Math.Abs(denominator) <= Eps10)
                {
                    break;
                }

                double v = ((this.m * phi) + Math.Sin(phi) - k) / denominator;
                phi -= v;
                if (Math.Abs(v) < Eps7)
                {
                    break;
                }
            }

            if (i == 0)
            {
                ProjectionThrowHelper.ThrowOutsideProjectionDomain();
            }
        }

        lon = this.SphericalRadius * this.cX * lambda * (this.m + Math.Cos(phi));
        lat = this.SphericalRadius * this.cY * phi;
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double phiNormalized = yy / this.cY;
        double phi = this.m != 0d
            ? Asinz(((this.m * phiNormalized) + Math.Sin(phiNormalized)) / this.n)
            : (this.n != 1d ? Asinz(Math.Sin(phiNormalized) / this.n) : phiNormalized);
        double denominator = this.cX * (this.m + Math.Cos(phiNormalized));
        if (Math.Abs(denominator) <= Eps10)
        {
            ProjectionThrowHelper.ThrowOutsideProjectionDomain();
        }

        double lambda = xx / denominator;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
