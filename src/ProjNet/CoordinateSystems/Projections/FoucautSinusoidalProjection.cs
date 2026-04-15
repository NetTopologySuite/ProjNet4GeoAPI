// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.CoordinateSystems.Projections;

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

/// <summary>
/// Implements the spherical Foucaut Sinusoidal projection (<c>fouc_s</c>).
/// </summary>
/// <remarks>
/// Foucaut Sinusoidal is a spherical pseudocylindrical projection that blends sinusoidal
/// behavior with a configurable Foucaut weighting parameter <c>n</c> in the range
/// <c>[0, 1]</c>. The implementation evaluates
/// <c>x = λ * cos(φ) / (n + (1 - n) * cos(φ))</c> and
/// <c>y = n * φ + (1 - n) * sin(φ)</c>, with an iterative inverse when
/// <c>n != 0</c>.
/// </remarks>
internal sealed class FoucautSinusoidalProjection : MapProjection
{
    private const int MaximumIterations = 10;

    private readonly double n;
    private readonly double n1;

    /// <summary>
    /// Initializes a new instance of the <see cref="FoucautSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    public FoucautSinusoidalProjection(IEnumerable<ProjectionParameter> parameters)
        : this(parameters, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FoucautSinusoidalProjection"/> class.
    /// </summary>
    /// <param name="parameters">Projection parameters.</param>
    /// <param name="inverse">Inverse transform instance when cloning.</param>
    public FoucautSinusoidalProjection(IEnumerable<ProjectionParameter> parameters, MapProjection? inverse)
        : base(parameters, inverse)
    {
        this.Name = "Foucaut_Sinusoidal";
        this.n = this.Parameters.GetOptionalParameterValue("n", 0d);
        if (this.n < 0d || this.n > 1d)
        {
            ArgumentGuard.ThrowArgument("Invalid value for n: it should be in [0,1] range.");
        }

        this.n1 = 1d - this.n;
    }

    /// <inheritdoc />
    public override MathTransform Inverse()
    {
        this.inverse ??= new FoucautSinusoidalProjection(this.Parameters.ToProjectionParameter(), this);

        return this.inverse;
    }

    /// <inheritdoc />
    protected override void RadiansToMeters(ref double lon, ref double lat)
    {
        double lambda = Adjust_lon(lon - this.centralMeridian);
        double t = Math.Cos(lat);
        double denominator = this.n + (this.n1 * t);
        if (Math.Abs(denominator) <= Eps10)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        lon = this.SphericalRadius * (lambda * t / denominator);
        lat = this.SphericalRadius * ((this.n * lat) + (this.n1 * Math.Sin(lat)));
    }

    /// <inheritdoc />
    protected override void MetersToRadians(ref double x, ref double y)
    {
        double xx = x * this.InverseSphericalRadius;
        double yy = y * this.InverseSphericalRadius;
        double phi = Asinz(yy);
        if (this.n != 0d)
        {
            phi = yy;
            int i = MaximumIterations;
            for (; i > 0; i--)
            {
                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);
                double denominator = this.n + (this.n1 * cosPhi);
                if (Math.Abs(denominator) <= Eps10)
                {
                    break;
                }

                double v = ((this.n * phi) + (this.n1 * sinPhi) - yy) / denominator;
                phi -= v;
                if (Math.Abs(v) < Eps7)
                {
                    break;
                }
            }

            if (i == 0)
            {
                phi = yy < 0d ? -HalfPi : HalfPi;
            }
        }

        double cos = Math.Cos(phi);
        if (Math.Abs(cos) <= Eps10)
        {
            throw new System.InvalidOperationException("Input data outside projection domain.");
        }

        double lambda = xx * (this.n + (this.n1 * cos)) / cos;
        x = Adjust_lon(this.centralMeridian + lambda);
        y = phi;
    }
}
